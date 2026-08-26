# Wanna Sit Here — Refactor

Unity project cho gameplay sắp xếp người vào ghế. Người chơi kéo một nhân vật giữa các ô ghế; mỗi nhân vật có các điều kiện thích/ghét người hoặc đồ ăn lân cận. Level thắng khi mọi điều kiện đều thỏa mãn trước khi hết lượt.

Project dùng Unity `6000.3.18f1`. Mở `Assets/Scenes/SampleScene.unity` rồi nhấn Play để chạy level mẫu.

## Cấu trúc code

Mã nguồn gameplay nằm trong `Assets/Game` và được chia theo assembly definition (`.asmdef`), không chỉ theo folder. Điều này để Unity compiler chặn dependency ngược giữa các phần của game.

```text
Game.Core
├── Board          Grid, cell và các kiểu dữ liệu của board
├── Conditions     Điều kiện thích/ghét và bộ đánh giá điều kiện
├── Levels         Trạng thái level khi runtime
└── People         Runtime state của nhân vật

Game.Data          ScriptableObject dùng để thiết kế level trong Inspector
Game.View          MonoBehaviour, input, view, UI, animation và event channel
Game.Events        ScriptableObject event channel
Game.Bootstrap     Điểm khởi tạo scene và wiring dependency
Game.Editor        Custom Inspector chỉ chạy trong Unity Editor
Game.Tests.EditMode EditMode tests cho Core
```

Chiều dependency cho phép:

```text
Game.Bootstrap ──> Game.Data ───> Game.Core
       └─────────> Game.View ───> Game.Core, Game.Events

Game.Editor ──────> Game.Data
Game.Tests.EditMode ───────> Game.Core
```

`Game.Core` không được tham chiếu `Game.Data`, `Game.View`, `Game.Events` hoặc `Game.Bootstrap`. `Game.View` cũng không được tham chiếu `Game.Data`; việc biến asset thiết kế thành runtime state thuộc về Bootstrap.

## Vai trò và giới hạn namespace

### `Game.Core.*`

Đây là luật và trạng thái gameplay chạy trong level:

- `Game.Core.Board`: `Grid<T>`, `CellRuntimeData`, `CellType`, `Food`, `GridId`.
- `Game.Core.People`: `PersonRuntimeData`, trait và state của người.
- `Game.Core.Conditions`: condition data, `ConditionChecker`, `LevelConditionEvaluator`.
- `Game.Core.Levels`: `LevelRuntimeData`, số lượt còn lại và hai grid của level.

Core được phép thay đổi runtime state và đánh giá rule. Core không được instantiate prefab, đọc input, gọi tween, thao tác UI, phát ScriptableObject event channel, hoặc đọc `ScriptableObject` thiết kế level.

Lưu ý: Core hiện vẫn dùng một vài value type của Unity (`Vector2`, `Vector2Int`) và giữ `Sprite` trong một số runtime data. Đây là giới hạn kỹ thuật còn lại của code hiện tại; không nên thêm UI/MonoBehaviour vào Core. Nếu cần tách Core thành pure C# về sau, thay `Sprite` bằng visual ID và tách thông tin layout khỏi `Grid<T>`.

### `Game.Data.*`

Đây là dữ liệu mà designer cấu hình trong Inspector:

- `Game.Data.People`: `PersonDataSO`, `GameConfig`.
- `Game.Data.Board`: `CellDataSO`.
- `Game.Data.Conditions`: `ConditionDataSO`.
- `Game.Data.Levels`: `LevelDataSO`.
- Các class có trách nhiệm chuyển dữ liệu data sang Core qua `ToRuntimeData()`.
- `Game.Data.People.GameConfig` chứa validation dành cho data authoring, ví dụ giới hạn condition của một người.

Data được phép phụ thuộc Core và Unity `ScriptableObject`. Nó không được xử lý drag, thay đổi trạng thái level đang chơi hoặc hiển thị UI.

### `Game.View.*`

Đây là Unity-facing layer:

- `Game.View.Board`: tạo cell trong scene, bind data và điều phối move với view.
- `Game.View.People`: hiển thị nhân vật, animation di chuyển và đồng bộ view sau swap.
- `Game.View.Input`: nhận drag event từ Unity EventSystem.
- `Game.View.UI`: hiển thị số lượt, panel kết quả và text effect.

View được phép dùng `MonoBehaviour`, `Instantiate`, Physics, camera, TMP và PrimeTween. Nó gọi Core để đọc/thay đổi gameplay state, nhưng không nên tạo thêm rule gameplay trong view hoặc UI.

### `Game.Events`

Chứa ScriptableObject event channels (`VoidEventChannelSO`, `SinglePayloadChannelSO`, `DoublePayloadChannelSO`) để decoupled giao tiếp giữa view và các hệ thống khác.

### `Game.Bootstrap`

`LevelBootstrapper` là entry point của scene. Nó nhận `LevelDataSO`, tạo `LevelRuntimeData`, sau đó khởi tạo `GridManager` và tạo hai grid. Bootstrap là nơi duy nhất được phép biết cả Data lẫn View.

### `Game.Editor`

Chứa tooling chỉ chạy trong Unity Editor. Hiện có `LevelDataSOEditor`, custom inspector để chỉnh grid của `LevelDataSO`. Không đưa `UnityEditor` hoặc code editor vào runtime assembly.

### `Game.Tests.EditMode`

Chứa NUnit test cho Core. Test ở đây có thể tham chiếu Core, nhưng không tham chiếu View hoặc Bootstrap trừ khi một test thật sự cần Unity scene.

## Flow khi chạy game

### 1. Khởi tạo level

1. `LevelBootstrapper.Start()` lấy `LevelDataSO` từ scene.
2. `LevelDataSO.ToRuntimeData()` chuyển các `CellDataSO` và `PersonDataSO` thành `LevelRuntimeData` cùng hai `Grid<CellRuntimeData>`.
3. `GridManager.Initialize()` giữ runtime level và bind `LevelView` để hiển thị số lượt.
4. `GridManager.CreateMainGrid()` và `CreateWaitGrid()` instantiate cell prefab; `CellView.BindData()` tạo `PersonView` cho các ô có người mặc định.

### 2. Kéo và thả người

1. `PersonDragManager` nhận `OnBeginDrag`, `OnDrag`, `OnEndDrag` từ Unity EventSystem.
2. Nó gọi `PersonMoveManager` để lưu vị trí gốc, tween theo con trỏ và tìm `CellView` đang overlap lúc thả.
3. `PersonMoveManager.MoveToCell()` gọi `GridManager.TryMovePerson()`.
4. `GridManager` kiểm tra target có phải ghế hợp lệ không, đổi `CurrentPerson` giữa source/target, rồi trừ một lượt.
5. Nếu move thành công, View tween các `PersonView` tới cell mới và cập nhật reference cell hiện tại.

### 3. Đánh giá kết quả

1. Sau mỗi move, `GridManager` gọi `LevelConditionEvaluator.AreAllPersonConditionsSatisfied()`.
2. Evaluator lấy các cell lân cận và dùng `ConditionChecker` để đánh giá từng điều kiện like/hate.
3. Mỗi `PersonRuntimeData` được cập nhật `Normal`, `Happy` hoặc `Angry`; `PersonView` lắng nghe event state để đổi face sprite.
4. Nếu tất cả người đều happy, `OnWinEvent` được raise. Nếu hết lượt mà chưa thỏa, `OnLoseEvent` được raise.
5. `UIManager` lắng nghe event channel, mở `LevelEndPanel` và yêu cầu `LevelEndText` hiển thị thông báo thắng/thua.

## Quy ước khi thêm code

- Bắt đầu bằng câu hỏi: code này là rule/state, dữ liệu thiết kế, Unity view, scene wiring hay editor tooling?
- Đặt code vào assembly có trách nhiệm tương ứng; không tạo namespace `Shared`, `Common` hoặc `Manager` chung chung.
- Chỉ expose `public` khi type cần dùng qua assembly khác; ưu tiên `internal` cho implementation nội bộ.
- Nếu gameplay orchestration không còn cần Unity object, tách nó từ View sang một `Game.Application` assembly mới thay vì để `GridManager` tiếp tục phình to.
