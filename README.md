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
├── Editor/          (Game.Editor)      Custom Inspector và Tooling editor
└── Tests/           (Game.Tests.*)     Unit tests (EditMode)
```

### Sơ đồ phụ thuộc giữa các tầng (Dependency Flow):

```text
Game.Core (Board, Conditions, Levels, People, Economy)
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
- **`Board`**: `Grid<T>`, `CellRuntimeData`, `CellType` (Seat, Block,...), `Food`, `GridId` (MainGrid, WaitGrid).
- **`People`**: `PersonRuntimeData`, `PersonState` (Normal, Happy, Angry).
- **`Conditions`**: `ConditionRuntimeData`, `ConditionChecker`, `LevelConditionEvaluator`.
- **`Levels`**: `LevelRuntimeData` (quản lý số lượt đi còn lại `CurrentMove`, event `OnMoveChanged`).
- **`Economy`**: `Inventory` (Gold, Gem, Remove, Undo, MoreMoves), `Reward`, `ItemType`.

### 2. `Game.Events` (Event-Driven Messaging)
Hệ thống Event Channel dựa trên ScriptableObject giúp tách rời hoàn toàn View, UI và Logic:
- **`EventChannelSO`**: Base abstract class chứa `event Action OnRaised` và method `Raise()`.
- **`VoidEventChannelSO`**: Event không mang payload (PlayGame, Win, Lose, NextLevel, RestartLevel, ClaimRewards, BuyItems,...).
- **`EventChannelSO<T>`**: Base abstract class cho event có kèm dữ liệu payload.
  - `OnItemReceiveSO` (`EventChannelSO<Reward>`): Bắn khi người chơi được cộng vật phẩm.
  - `OnItemSpendSO` (`EventChannelSO<Reward>`): Bắn khi người chơi chi tiêu hoặc giao dịch thất bại.

### 3. `Game.Data` (Configuration Layer)
ScriptableObject dành cho Game Designer cấu hình trên Inspector:
- `LevelDataSO`: Cấu hình bố cục ô, ghế, vật cản và người trong level. Hỗ trợ chuyển đổi sang `LevelRuntimeData` qua `ToRuntimeData()`.
- `PersonDataSO`, `ConditionDataSO`, `CellDataSO`.
- `EconomyConfigSO`: Cấu hình thưởng thắng màn (`levelWinReward`), thưởng xem quảng cáo (`levelAdsWinReward`), điểm danh (`dailyReward`, `weeklyReward`), giá shop.

### 4. `Game.App` (Application Layer)
- **`LevelManager`**: Quản lý logic điều phối di chuyển của người trong level (`TryMovePerson`), kiểm tra điều kiện thắng/thua (`CheckAllPersonConditions`) và bắn `OnWinEvent` / `OnLoseEvent`.
- **`SaveLoadManager`**: Quản lý đọc/ghi tiến trình người chơi và tài sản vào file JSON (`data.json`) tại `Application.persistentDataPath`.
- **`GameData`**: DTO lưu trữ level hiện tại, số lượng Gold, Gem, Booster items.

### 5. `Game.View` (Presentation & UI Layer)
- **`Board`**: `GridManager` (tạo cell từ prefab, định vị toạ độ viewport), `CellView`, `FoodTooltips`.
- **`People`**: `PersonMover` (tính toán va chạm và di chuyển tween), `PersonDragManager` (nhận input kéo thả), `PersonView`.
- **`UI`**:
  - `UIManager`: UI Facade quản lý panel (MainMenu, InGameUI, WinPanel, LosePanel), khởi tạo sub-views.
  - `InventoryView`: Hiển thị số dư tiền/gem, tự động cập nhật và chạy animation số nhảy mượt mà qua PrimeTween khi `Inventory.OnInventoryUpdate` kích hoạt.
  - `LevelView`: Hiển thị số lượt đi còn lại.
  - `LevelEndPanel`, `LevelEndText`: Hiệu ứng mở panel kết thúc màn và chữ nhảy (scale pop).
  - `ButtonPunchShake`: Script thuần visual tạo hiệu ứng nảy nút (Punch Scale) khi click.
  - `ButtonEventRaiser`: Component logic gắn trên button để phát event channel sau một khoảng delay tùy chọn.
  - `TransitionController`: Hiệu ứng chuyển cảnh Circle Cutout Wipe giữa các màn chơi.

### 6. `Game.Bootstrap` (Composition Root)
- **`GameManager`**: MonoBehaviour điều phối vòng đời chính, lắng nghe các event Game Flow (Play, Win, Lose, Next, Restart), Rewards và Shop. Quản lý việc lưu game (`SaveGame`) và khởi tạo `Inventory`.
- **`LevelBootstrapper`**: POCO class hỗ trợ nạp level theo index, reset grid cũ (`ClearGrids()`), khởi tạo data runtime và kết nối `LevelView`.

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
3. `GameManager.HandlePlayGame()` yêu cầu `LevelBootstrapper.LoadLevel()`:
   - Dọn sạch các grid cũ qua `GridManager.ClearGrids()`.
   - Chuyển `LevelDataSO` sang `LevelRuntimeData`.
   - Sinh các GameObject ghế và nhân vật tương ứng trên màn hình.
   - Gắn dữ liệu số lượt đi vào `LevelView`.

### 3. Kéo thả nhân vật & Đánh giá luật chơi
1. Người chơi kéo `PersonView` $\rightarrow$ `PersonDragManager` gọi `PersonMover`.
2. Khi thả tay, `PersonMover` tìm `CellView` gần nhất và gọi `GridManager.TryMovePerson()`.
3. `LevelManager` xác thực nước đi:
   - Nếu ô đích hợp lệ: Hoán đổi vị trí nhân vật, trừ 1 lượt đi (`CurrentMove - 1`).
   - Cập nhật lại biểu cảm nhân vật qua `LevelConditionEvaluator.UpdateAllPersonStates()`.
4. Nếu tất cả nhân vật đều thỏa mãn điều kiện $\rightarrow$ bắn `OnWinLevelEvent`.
5. Nếu chưa hoàn thành và hết lượt đi (`IsOutOfMove`) $\rightarrow$ bắn `OnLoseLevelEvent`.

### 4. Thắng / Thua & Nhận thưởng
- **Thắng (`OnWinLevelEvent`)**:
  - `GameManager` tăng `currentLevel++` và lưu game.
  - `UIManager` hiển thị `LevelWinPanel` với animation chữ và hiệu ứng mở dần.
  - Người chơi bấm **Nhận thưởng (40 Gold)** $\rightarrow$ bắn `OnClaimWinReward` $\rightarrow$ `GameManager` lấy thưởng từ `EconomyConfigSO.levelWinReward`, cộng vào `Inventory`, bắn `OnItemReceive` để UI nhảy số và lưu game.
  - Người chơi bấm **Màn tiếp theo** $\rightarrow$ bắn `OnNextLevel` $\rightarrow$ load màn chơi mới.
- **Thua (`OnLoseLevelEvent`)**:
  - `UIManager` hiển thị `LevelLosePanel`.
  - Người chơi bấm **Chơi lại** $\rightarrow$ bắn `OnRestartLevel` $\rightarrow$ load lại màn chơi hiện tại.

### 5. Kinh tế & Cửa hàng (Shop Transaction)
- Khi bấm mua vật phẩm (búa gỡ ghế, lượt đi, hoàn tác):
  - Nút bấm phát các Void Event như `OnBuyRemove`, `OnBuyUndo`, `OnBuyMoreMoves`.
  - `GameManager` chạy giao dịch an toàn qua `TryPurchase(cost, item)`:
    - Nếu đủ tiền trong `Inventory`: Trừ chi phí $\rightarrow$ Cộng vật phẩm $\rightarrow$ Bắn `OnItemReceive` $\rightarrow$ Lưu game.
    - Nếu không đủ tiền: Không cộng vật phẩm $\rightarrow$ Bắn `OnItemSpend` thông báo thất bại.

---

## 🎨 Quy chuẩn Thiết kế UI Button

Để tách biệt hoàn toàn giữa **Hiệu ứng Hình ảnh** và **Logic Nghiệp vụ**, mỗi UI Button trong game áp dụng mô hình 2 component độc lập trên cùng GameObject:

1. **`ButtonPunchShake`** (Pure Visual):
   - Đăng ký vào sự kiện `Button.onClick`.
   - Chỉ chịu trách nhiệm tween scale/rung nút qua PrimeTween.
   - **Tuyệt đối không gọi hay phụ thuộc bất kỳ hàm logic nào**.

2. **`ButtonEventRaiser`** (Logic Dispatcher):
   - Đăng ký vào sự kiện `Button.onClick` (mục riêng biệt).
   - Thiết lập danh sách các ScriptableObject `EventChannelSO` cần kích hoạt và độ trễ (`delay`) nếu cần đợi visual chạy xong.

---

## 🧪 Kiểm thử & Phát triển (Testing & Guidelines)

- **EditMode Tests**: Chạy qua Unity Test Runner (`Window > General > Test Runner`) để kiểm tra logic tính toán điều kiện của `LevelConditionEvaluator` và `Inventory` mà không cần chạy Scene.
- **Quy tắc sửa đổi code**:
  - Domain / Rule mới $\rightarrow$ viết trong `Game.Core`.
  - Không kéo thả chéo các dependency vi phạm quy định của Assembly Definition.
  - Không lạm dụng Singleton; ưu tiên giao tiếp qua Event Channels hoặc Dependency Injection tại `GameManager`.
