# Wanna Sit Here — Refactor

Unity project cho gameplay sắp xếp người vào ghế (Puzzle Seat Sorting). Người chơi kéo và đổi chỗ các nhân vật giữa các ô ghế; mỗi nhân vật có các điều kiện thích/ghét người hoặc đồ ăn lân cận. Level thắng khi mọi điều kiện đều được thỏa mãn trước khi hết lượt đi.

Dự án sử dụng **Unity `6000.3.18f1`** và áp dụng kiến trúc phân tầng rõ ràng (**Clean / Layered Architecture**) kết hợp **Event-Driven Architecture (ScriptableObject Event Channels)**.

---

## 🏗 Cấu trúc Kiến trúc & Assembly Definitions

Mã nguồn trong `Assets/Game` được phân tách chặt chẽ theo từng Assembly Definition (`.asmdef`) nhằm ngăn chặn circular dependency và đảm bảo tính đóng gói:

```text
Assets/Game/
├── Core/            (Game.Core)        Rule gameplay, dữ liệu runtime, logic thuần C# (Không phụ thuộc ai)
├── Events/          (Game.Events)      ScriptableObject Event Channels để decouple giao tiếp
├── Data/            (Game.Data)        ScriptableObjects cấu hình level, nhân vật và kinh tế (Data Authoring)
├── App/             (Game.App)         Điều phối game (LevelManager), lưu/tải dữ liệu (SaveLoadManager)
├── View/            (Game.View)        MonoBehaviour, UI, VFX, Audio, Input, Render và Animation
├── Bootstrap/       (Game.Bootstrap)   Composition Root (GameManager, LevelBootstrapper)
├── Editor/          (Game.Editor)      Custom Inspector, Cheat Tool và Tooling editor
└── Tests/           (Game.Tests.*)     Unit tests (EditMode)
```

### Sơ đồ phụ thuộc giữa các tầng (Dependency Flow):

```text
Game.Core (Board, Conditions, Levels, People, Economy, Booster)
   ▲          ▲                     ▲                   ▲
   │          │                     │                   │
Game.Events   Game.Data          Game.App               │
   ▲                                ▲                   │
   │                                │                   │
   └──────────────┬─────────────────┴───────────── Game.View
                  │                                     ▲
                  │                                     │
                  └────────────────────────────── Game.Bootstrap
```

- **`Game.Core`** là trung tâm domain: Hoàn toàn không phụ thuộc bất kỳ layer nào khác, không dùng MonoBehaviour.
- **`Game.View`** không được phép tham chiếu trực tiếp đến `Game.Data`. Việc đọc data cấu hình và chuyển đổi thành runtime state thuộc trách nhiệm của `Game.Bootstrap`.
- **`Game.Bootstrap`** là nơi duy nhất biết toàn bộ các layer để làm nhiệm vụ ráp nối (Dependency Injection / Composition Root).

---

## 📦 Chi tiết các Layer & Trách nhiệm

### 1. `Game.Core` (Domain Layer)
Chứa các thực thể, luật chơi và trạng thái lúc runtime:
- **`Board`**: `Grid<T>`, `CellRuntimeData`, `CellType` (Seat, Block, Food), `Food` (Any, Hamburger, FrenchFries), `GridId` (MainGrid, WaitGrid).
- **`People`**: `PersonRuntimeData`, `PersonState` (Normal, Happy, Angry), `PersonTrait`. Hỗ trợ `ClearConditions()`, `ReplaceConditions()` và sự kiện `OnConditionsCleared`.
- **`Conditions`**: `ConditionRuntimeData`, `ConditionChecker`, `LevelConditionEvaluator`. `Like + Food.Any` là condition `CanSitAnywhere`, luôn đúng khi ngồi trên MainGrid.
- **`Levels`**: `LevelRuntimeData` (quản lý số lượt đi còn lại `CurrentMove`, event `OnMoveChanged`).
- **`Economy`**: `Inventory` (Gold, Gem, Remove, Undo, MoreMoves), `Reward`, `ItemType`, `EconomyManager`.
- **`Booster`**:
  - `Booster`: Lớp cơ sở áp dụng **Template Method Pattern** (`TryUse()` kiểm tra `CanUse()` trước khi gọi `Execute()`, chỉ phát `OnBoosterUsed` khi thành công).
  - `MoreMoveBooster`: Cộng thêm số lượt đi vào level hiện tại.
  - `UndoBooster`: Rút nước đi gần nhất từ `MoveHistory`, hoàn trả nhân vật về vị trí cũ và hoàn lại 1 lượt đi.
  - `RemoveBooster`: Thay toàn bộ điều kiện của nhân vật bằng `CanSitAnywhere` (ưu tiên người đang Angry trên MainGrid, fallback sang WaitGrid); bỏ qua Person đã có condition này.
  - `MoveRecord` & `MoveHistory`: Cấu trúc Stack-based lưu trữ snapshot các bước di chuyển phục vụ hoàn tác.

### 2. `Game.Events` (Event-Driven Messaging)
Hệ thống Event Channel dựa trên ScriptableObject giúp tách rời hoàn toàn View, UI và Logic:
- **`EventChannelSO`**: Base abstract class chứa `event Action OnRaised` và method `Raise()`.
- **`VoidEventChannelSO`**: Event không mang payload:
  - Game Flow: `OnPlayGame`, `OnWinLevelEvent`, `OnLoseLevelEvent`, `OnNextLevel`, `OnRestartLevel`.
  - Rewards: `OnClaimWinReward`, `OnClaimAdsReward`, `OnClaimDailyReward`, `OnClaimWeeklyReward`.
  - Shop: `OnBuyRemove`, `OnBuyUndo`, `OnBuyMoreMoves`.
  - **In-Game Booster Use**: `OnUseMoreMoves`, `OnUseUndo`, `OnUseRemove`.
- **`EventChannelSO<T>`**: Base abstract class cho event có kèm dữ liệu payload:
  - `OnItemReceiveSO` (`EventChannelSO<Reward>`): Bắn khi người chơi được cộng vật phẩm.
  - `OnItemSpendSO` (`EventChannelSO<Reward>`): Bắn khi người chơi chi tiêu hoặc giao dịch thất bại.

### 3. `Game.Data` (Configuration Layer)
ScriptableObject dành cho Game Designer cấu hình trên Inspector:
- `LevelDataSO`: Cấu hình bố cục ô, ghế, vật cản và người trong level. Hỗ trợ chuyển đổi sang `LevelRuntimeData` qua `ToRuntimeData()`.
- `PersonDataSO`, `ConditionDataSO`, `CellDataSO`.
- `EconomyConfigSO`: Cấu hình thưởng thắng màn (`levelWinReward`), thưởng xem quảng cáo (`levelAdsWinReward`), điểm danh (`dailyReward`, `weeklyReward`), giá shop.
- `GameConfig`: Chứa các hằng số game như `MORE_MOVE_AMOUNT = 3`, `MAX_CONDITION_PER_PERSON = 2`.

### 4. `Game.App` (Application Layer)
- **`LevelManager`**:
  - Quản lý logic điều phối di chuyển của người trong level (`TryMovePerson`).
  - Ghi nhận lịch sử di chuyển vào `MoveHistory`: **chỉ bỏ qua (skip) khi di chuyển nội bộ trong hàng chờ** (`WaitGrid -> WaitGrid`); các lượt đi từ hàng chờ lên bàn cờ (`WaitGrid -> MainGrid`) hoặc giữa các ghế bàn cờ đều được lưu lại để hỗ trợ hoàn tác.
  - Kiểm tra điều kiện thắng/thua (`CheckAllPersonConditions`) và bắn `OnWinEvent` / `OnLoseEvent`.
- **`SaveLoadManager`**: Quản lý đọc/ghi tiến trình người chơi và tài sản vào file JSON (`data.json`) tại `Application.persistentDataPath`.
- **`GameData`**: DTO lưu trữ level hiện tại, số lượng Gold, Gem, số lượng 3 loại Booster (`currentRemove`, `currentUndo`, `currentMoreMoves`).

### 5. `Game.View` (Presentation & UI Layer)
- **`Board`**:
  - `GridManager`: Sinh cell từ prefab, định vị toạ độ viewport, lưu bản đồ tra cứu `_cellViewMap` (`CellRuntimeData -> CellView`), hỗ trợ hoàn tác hiển thị qua `RevertMoveView(MoveRecord)`.
  - `CellView`: Hiển thị ô ghế/đồ ăn, liên kết với `PersonView`.
- **`People`**:
  - `PersonMover`: Tính toán va chạm và di chuyển tween bằng PrimeTween; hỗ trợ `RevertMove(sourceCell, targetCell)` để diễn hoạt hoàn tác người về vị trí cũ.
  - `PersonDragManager`: Nhận input kéo thả từ người chơi.
  - `PersonView`: Hiển thị sprite nhân vật và biểu cảm theo trạng thái.
  - `PersonTooltip`: Hiển thị điều kiện; tự động lắng nghe `OnConditionsCleared` để làm mới text và resize khung khi danh sách điều kiện được thay đổi bằng booster.
- **`UI`**:
  - `UIManager`: UI Facade quản lý các panel (`MainMenu`, `InGameUI`, `WinPanel`, `LosePanel`), khởi tạo sub-views.
  - `InventoryView`: Hiển thị số dư tiền/gem, tự động cập nhật và chạy animation số nhảy mượt mà qua PrimeTween khi `Inventory.OnInventoryUpdate` kích hoạt.
  - `BoosterSlotView`: Component quản lý từng nút booster trong gameplay, hiển thị số lượng từ `Inventory`, disable nút khi số lượng = 0, phát event channel khi click.
  - `LevelView`: Hiển thị số lượt đi còn lại (`moveText`) và liên kết 3 slot booster (`moreMoveSlot`, `undoSlot`, `removeSlot`), tự động đồng bộ qua `BindBoosters(Inventory)`.
  - `LevelEndPanel`, `LevelEndText`: Hiệu ứng mở panel kết thúc màn và chữ nhảy (scale pop).
  - `ButtonPunchShake`: Script visual tạo hiệu ứng nảy nút (Punch Scale) khi click.
  - `ButtonEventRaiser`: Component logic gắn trên button để phát event channel sau một khoảng delay tùy chọn.
  - `TransitionController`: Hiệu ứng chuyển cảnh Circle Cutout Wipe giữa các màn chơi.

### 6. `Game.Bootstrap` (Composition Root)
- **`GameManager`**: MonoBehaviour điều phối vòng đời chính:
  - Lắng nghe các event Game Flow (Play, Win, Lose, Next, Restart), Rewards, Shop.
  - Lắng nghe 3 sự kiện sử dụng booster in-game (`HandleUseMoreMoves`, `HandleUseUndo`, `HandleUseRemove`).
  - Quản lý việc lưu game (`SaveGame`) và đồng bộ `Inventory`.
- **`LevelBootstrapper`**: POCO class hỗ trợ nạp level theo index, reset grid cũ (`ClearGrids()`), khởi tạo data runtime và kết nối `LevelView`.

### 7. `Game.Editor` (Editor Tooling)
- **`CheatToolWindow`** (`Tools > Cheat Tool` hoặc `Window > Cheat Tool`): Cửa sổ Editor cho phép xem và can thiệp nhanh dữ liệu save game: chỉnh sửa Level, Gold, Gem, và số lượng 3 loại Booster ngay trong Editor.

---

## ⚡ Hệ Thống 3 Booster Gameplay

Hệ thống Booster hỗ trợ người chơi giải quyết các tình huống khó khăn trong màn chơi:

| Booster | Tác Dụng | Quy Tắc Thực Thi & Giới Hạn |
|---|---|---|
| **More Moves** | Cộng thêm lượt đi (`+3` moves theo `GameConfig.MORE_MOVE_AMOUNT`). | Chỉ dùng khi màn chơi đang diễn ra (`!IsOutOfMove`). Trừ 1 item trong kho khi dùng thành công. |
| **Undo** | Hoàn tác lại nước đi vừa thực hiện. Hoán đổi nhân vật về vị trí cũ (có tween bay mượt mà) và hoàn lại `+1` move. | - Hỗ trợ cả di chuyển giữa các ghế trên bàn cờ và di chuyển từ hàng chờ (`WaitLine`) lên ghế.<br>- **Bỏ qua (skip)** các lượt di chuyển nội bộ trong hàng chờ (`WaitLine -> WaitLine`).<br>- Tự động đánh giá lại trạng thái Happy/Angry của tất cả nhân vật sau khi hoàn tác. |
| **Remove** | Thay toàn bộ điều kiện của 1 nhân vật ngẫu nhiên bằng `CanSitAnywhere`. | - **Ưu tiên 1**: Chọn ngẫu nhiên 1 người đang **Angry** trên bàn cờ (`MainGrid`) có điều kiện và chưa có `CanSitAnywhere`.<br>- **Ưu tiên 2**: Nếu không có ai Angry, chọn ngẫu nhiên 1 người trên hàng chờ (`WaitGrid`) có điều kiện và chưa có `CanSitAnywhere`.<br>- Person đã có `CanSitAnywhere` không thể bị chọn. Person được thay điều kiện sẽ được đánh giá lại và Happy khi đang ở MainGrid. |

---

## 🔄 Các Luồng Hoạt Động Chính (Game Flows)

### 1. Khởi động Game (App Start & Initialization)
1. `GameManager.Awake()` nạp `GameData` từ `SaveLoadManager`.
2. Tạo mới đối tượng `Inventory` từ dữ liệu đã lưu.
3. Gọi `UIManager.Initialize(_inventory)` để truyền dữ liệu xuống `InventoryView` hiển thị số tiền/gem ban đầu.
4. Tạo `LevelBootstrapper`.
5. Đăng ký lắng nghe toàn bộ các kênh Event Channel trong `OnEnable()`.

### 2. Bắt đầu màn chơi (Main Menu → In-Game)
1. Người chơi bấm nút **Play**:
   - `ButtonPunchShake` thực hiện hiệu ứng rung nảy nút.
   - `ButtonEventRaiser` chờ 0.25s rồi bắn event `OnPlayGame`.
2. `UIManager` ẩn `MainMenu`, hiển thị `InGameUI`.
3. `GameManager.HandlePlayGame()`:
   - Gọi `LevelBootstrapper.LoadLevel()` dọn sạch grid cũ, sinh ghế và người, kết nối `LevelView.BindData()`.
   - Gọi `LevelView.BindBoosters(_inventory)` để liên kết số lượng booster hiện có và cập nhật trạng thái các nút booster.

### 3. Kéo thả nhân vật & Đánh giá luật chơi
1. Người chơi kéo `PersonView` $\rightarrow$ `PersonDragManager` gọi `PersonMover`.
2. Khi thả tay, `PersonMover` tìm `CellView` gần nhất và gọi `GridManager.TryMovePerson()`.
3. `LevelManager` xác thực nước đi:
   - Nếu hợp lệ: Hoán đổi vị trí nhân vật, trừ 1 lượt đi (`CurrentMove - 1`).
   - Nếu không phải di chuyển nội bộ hàng chờ (`WaitGrid -> WaitGrid`), ghi nhận vào `MoveHistory`.
   - Cập nhật lại biểu cảm nhân vật qua `LevelConditionEvaluator.UpdateAllPersonStates()`.
4. Nếu tất cả nhân vật đều thỏa mãn điều kiện $\rightarrow$ bắn `OnWinLevelEvent`.
5. Nếu chưa hoàn thành và hết lượt đi (`IsOutOfMove`) $\rightarrow$ bắn `OnLoseLevelEvent`.

### 4. Sử dụng Booster trong màn chơi
1. Người chơi bấm nút booster bất kỳ (`BoosterSlotView`):
   - Nút phát sự kiện tương ứng (`OnUseMoreMoves`, `OnUseUndo`, hoặc `OnUseRemove`).
2. `GameManager` tiếp nhận xử lý:
   - Kiểm tra xem người chơi có đang trong level và còn lượt đi hay không.
   - Kiểm tra xem số dư booster trong `Inventory` có đủ $\ge 1$ không.
   - Khởi tạo instance booster tương ứng và gọi `TryUse()`.
   - Nếu thực thi thành công: Trừ 1 item qua `_inventory.TrySpendItem()`, đồng bộ hiển thị View (Undo revert tween hoặc Remove condition refresh), đánh giá lại điều kiện và lưu game tự động.

### 5. Thắng / Thua & Nhận thưởng
- **Thắng (`OnWinLevelEvent`)**:
  - `GameManager` tăng `currentLevel++` và lưu game.
  - `UIManager` hiển thị `LevelWinPanel` với animation chữ và hiệu ứng mở dần.
  - Người chơi bấm **Nhận thưởng (40 Gold)** $\rightarrow$ bắn `OnClaimWinReward` $\rightarrow$ `GameManager` lấy thưởng từ `EconomyConfigSO.levelWinReward`, cộng vào `Inventory`, bắn `OnItemReceive` để UI nhảy số và lưu game.
  - Người chơi bấm **Màn tiếp theo** $\rightarrow$ bắn `OnNextLevel` $\rightarrow$ load màn chơi mới.
- **Thua (`OnLoseLevelEvent`)**:
  - `UIManager` hiển thị `LevelLosePanel`.
  - Người chơi bấm **Chơi lại** $\rightarrow$ bắn `OnRestartLevel` $\rightarrow$ load lại màn chơi hiện tại.

### 6. Kinh tế & Cửa hàng (Shop Transaction)
- Khi bấm mua vật phẩm (búa gỡ ghế, lượt đi, hoàn tác):
  - Nút bấm phát các Void Event như `OnBuyRemove`, `OnBuyUndo`, `OnBuyMoreMoves`.
  - `GameManager` chạy giao dịch an toàn qua `TryPurchase(cost, item)`:
    - Nếu đủ tiền trong `Inventory`: Trừ chi phí $\rightarrow$ Cộng vật phẩm $\rightarrow$ Bắn `OnItemReceive` $\rightarrow$ Lưu game.
    - Nếu không đủ tiền: Không cộng vật phẩm $\rightarrow$ Bắn `OnItemSpend` thông báo thất bại.

---

## 🎨 Quy chuẩn Thiết kế UI Button

Để tách biệt hoàn toàn giữa **Hiệu ứng Hình ảnh** và **Logic Nghiệp vụ**, mỗi UI Button trong game áp dụng mô hình phân tách component rõ ràng:

1. **Hiệu ứng visual (`ButtonPunchShake`)**:
   - Đăng ký vào sự kiện `Button.onClick`.
   - Chỉ chịu trách nhiệm tween scale/rung nút qua PrimeTween.
   - **Tuyệt đối không gọi hay phụ thuộc bất kỳ hàm logic nào**.

2. **Phát sự kiện logic (`ButtonEventRaiser` hoặc `BoosterSlotView`)**:
   - Gắn component phát sự kiện riêng biệt.
   - Thiết lập danh sách các ScriptableObject `EventChannelSO` cần kích hoạt.

---

## 🧪 Kiểm thử Đơn vị (Unit Testing)

Tất cả các bài kiểm tra được viết dưới dạng **EditMode Tests** trong [`Assets/Game/Tests/DomainTests.cs`](file:///d:/Projects/WannaSitHere-Refactor/Assets/Game/Tests/DomainTests.cs):
- **Board & Grid**: Kiểm tra set/get trong và ngoài biên của `Grid<T>`.
- **Condition Evaluator**: Kiểm tra chính xác các logic Like/Hate đối với đồ ăn (`Food`) và tính cách nhân vật (`PersonTrait`), bao gồm `CanSitAnywhere` với `Food.Any`.
- **Economy**: Kiểm tra nạp/trừ tài nguyên, clamp giá trị $\ge 0$ trong `Inventory`, chuỗi điểm danh và reset shop ngày trong `EconomyManager`.
- **MoveHistory**: Kiểm tra cơ chế stack LIFO và clear history khi chuyển màn.
- **Boosters**:
  - `MoreMoveBooster`: Kiểm tra cộng đúng số move và kích hoạt event.
  - `UndoBooster`: Kiểm tra đảo ngược vị trí người giữa 2 ô ghế, hoàn lại 1 lượt đi; kiểm tra hoàn tác người từ bàn cờ về đúng ô chờ ở hàng chờ (`WaitGrid`); kiểm tra an toàn khi history trống.
  - `RemoveBooster`: Kiểm tra ưu tiên thay điều kiện của người Angry trên MainGrid trước; fallback sang người ở WaitGrid, thay toàn bộ bằng `CanSitAnywhere`, và bỏ qua người đã có condition này.
  - `PersonRuntimeData`: Kiểm tra `ClearConditions()` xóa sạch danh sách, `ReplaceConditions()` thay danh sách và kích hoạt sự kiện `OnConditionsCleared`.
- **LevelManager Move Recording**:
  - Di chuyển giữa các ghế trên MainGrid $\rightarrow$ **Được ghi nhận**.
  - Di chuyển từ WaitLine lên MainGrid $\rightarrow$ **Được ghi nhận**.
  - Di chuyển nội bộ WaitLine sang WaitLine $\rightarrow$ **Không bị ghi nhận (Skip)**.

Để chạy kiểm thử: Mở Unity Editor $\rightarrow$ `Window > General > Test Runner` $\rightarrow$ Chạy tab **EditMode**.
