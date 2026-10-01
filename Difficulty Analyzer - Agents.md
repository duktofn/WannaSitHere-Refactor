# Difficulty Analyzer — hướng dẫn cho Agents

## Mục đích và phạm vi

Dùng tool để đo độ khó của `LevelDataSO`, giải thích nguồn tạo độ khó, kiểm tra khả năng hoàn thành trong `LevelMove`, và so sánh Level trước/sau một thay đổi đã được yêu cầu.

Phân tích là thao tác chỉ đọc. Không tự sửa Level, Person, Condition hoặc trọng số chỉ để đạt một điểm mong muốn. Nếu nhiệm vụ yêu cầu chỉnh Level, ghi kết quả trước thay đổi, chỉ sửa dữ liệu thuộc phạm vi nhiệm vụ, rồi phân tích lại.

Hướng dẫn này mô tả implementation hiện tại: `ModelId = ConditionDependencyMove`, `ModelVersion = 2`. Khi dùng, đối chiếu với `DifficultyAnalysisJob.CurrentModelId` và `CurrentModelVersion`; không dùng báo cáo A/B/C/D/F cũ để so sánh với mô hình này.

## Điểm vào và nguồn kiểm chứng

| Thành phần | Vị trí / API |
| --- | --- |
| Cửa sổ GUI | `Tools > Wanna Sit Here > Difficulty Analyzer` |
| API phân tích | `Assets/Game/Editor/Difficulty/DifficultyAnalysisJob.cs` |
| Settings, trạng thái, schema kết quả | `Assets/Game/Editor/Difficulty/DifficultyAnalysisResult.cs` |
| GUI và xuất báo cáo | `Assets/Game/Editor/Difficulty/DifficultyAnalyzerWindow.cs` |
| Báo cáo TXT | `DifficultyAnalysisReport.Text(result)` |
| Báo cáo CSV | `DifficultyAnalysisReport.Csv(result)` |
| Báo cáo JSON | `JsonUtility.ToJson(result, true)` |
| Tests | `Assets/Game/Tests/DifficultyAnalyzerTests.cs` |
| Hướng dẫn người dùng | [Difficulty Analyzer.md](Difficulty%20Analyzer.md) |

Code analyzer thuộc assembly Editor; không gọi API này từ runtime hoặc player build.

## Quy trình chuẩn

1. Đọc `AGENTS.md` và xác định chính xác Level asset cần phân tích. Nếu chỉ được yêu cầu kiểm tra, giữ nguyên asset.
2. Xác nhận Unity Editor đang mở đúng project bằng `Application.dataPath`; đọc Console để biết lỗi compile hiện có. Dùng Unity MCP đã kết nối khi có sẵn.
3. Chọn settings: mặc định `UseSolver = true`, `MaxNodes = 2000000`. `MaxNodes` phải lớn hơn 0.
4. Chạy phân tích bằng GUI hoặc API bất đồng bộ bên dưới. Chờ job kết thúc trước khi đọc điểm tổng.
5. Đọc `Status`, `Problems`, `Warnings`, `HasScore`, `HasExactMoves`, `SolverExact` trước khi diễn giải các con số.
6. Xác nhận `InputFingerprint` còn khớp với Level và settings hiện tại trước khi xuất hoặc so sánh báo cáo.
7. Trả kết quả kèm nguyên nhân chính, độ chắc chắn của bộ giải và giới hạn còn lại. Nếu sửa Level, lưu cả kết quả trước và sau.

Nếu Unity MCP không khả dụng, có thể đọc source và báo cáo đã xuất, nhưng phải ghi rõ chưa chạy analyzer trên dữ liệu hiện tại. Không tự tính một điểm thay thế rồi trình bày như kết quả của tool.

## Sử dụng GUI

1. Mở menu tool, kéo `LevelDataSO` vào trường **Level**, hoặc chọn asset trong Project rồi bấm **Dùng Level đang chọn**.
2. Mở **Thiết lập bộ giải** để bật **Chạy bộ giải Move/Swap** và đặt ngân sách node.
3. Bấm **Tính độ khó**. Thanh tiến độ biểu thị phần ngân sách node đã dùng, không biểu thị phần trăm không gian lời giải đã duyệt.
4. Xem các tab:

| Tab | Dùng để |
| --- | --- |
| Tổng quan | Đọc điểm, đóng góp của ba nhóm, M*, Move dư, lỗi và cảnh báo |
| Person / Condition | Tìm Person có Ri thấp/Ti cao, xem Condition và tầng Dependency |
| Bàn / Dependency | Chọn Person để xem heatmap Q(x); hover để đọc q từng Condition, Kx và ô kề; xem provider theo trait và chu trình |
| Báo cáo | Copy hoặc xuất TXT, JSON, CSV |

Nếu dữ liệu đã đổi, tính lại trước khi xuất. Nút **Hủy phân tích** hoặc đóng cửa sổ sẽ dừng job; kết quả đang duyệt không được dùng làm điểm tổng.

## Sử dụng API qua Unity MCP

Ưu tiên gọi API công khai thay vì thao tác field private của cửa sổ hoặc mô phỏng click bằng tọa độ. Không cần mở scene hay vào Play Mode để phân tích Level asset.

Ví dụ dưới đây dành cho `Unity_RunCommand` của Unity MCP hiện có. Thay `levelPath` bằng đường dẫn asset đã xác minh. Đây là code chạy tạm trong Editor, không phải script cần thêm vào project.

```csharp
using UnityEngine;
using UnityEditor;
using Game.Data.Levels;
using Game.Editor.Difficulty;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        const string levelPath = "Assets/Data/Level/Level 3.asset";
        var level = AssetDatabase.LoadAssetAtPath<LevelDataSO>(levelPath);
        if (level == null)
        {
            result.LogError("Không tìm thấy LevelDataSO: " + levelPath);
            return;
        }

        var settings = new DifficultyAnalysisSettings
        {
            UseSolver = true,
            MaxNodes = 2000000
        };
        var job = new DifficultyAnalysisJob(level, settings);
        EditorApplication.CallbackFunction update = null;

        void Stop()
        {
            EditorApplication.update -= update;
            AssemblyReloadEvents.beforeAssemblyReload -= Stop;
            job.Cancel();
        }

        void Publish()
        {
            var analysis = job.Result;
            bool stale = analysis.InputFingerprint !=
                DifficultyAnalysisJob.ComputeInputFingerprint(level, settings);
            Debug.Log("Difficulty Analyzer | " + levelPath + " | stale=" + stale);
            Debug.Log(DifficultyAnalysisReport.Text(analysis));
            Debug.Log(JsonUtility.ToJson(analysis, true));
        }

        if (!job.IsRunning)
        {
            Publish();
            result.Log("Phân tích kết thúc ngay; xem kết quả trong Console.");
            return;
        }

        update = () =>
        {
            job.Tick();
            if (job.IsRunning) return;
            Stop();
            Publish();
        };
        AssemblyReloadEvents.beforeAssemblyReload += Stop;
        EditorApplication.update += update;
        result.Log("Đã bắt đầu phân tích; đọc Console khi job hoàn tất.");
    }
}
```

Lệnh MCP trả về khi job được đăng ký, không phải khi bộ giải hoàn tất. Đọc Console sau đó để lấy báo cáo; không kết luận từ thông báo “đã bắt đầu”. Nếu assembly reload khi đang chạy, ví dụ sẽ hủy job; cần chạy lại sau khi Editor ổn định.

`Tick()` dành khoảng 8 ms cho mỗi lần gọi. Không dùng `while (job.IsRunning) job.Tick()` trong một lệnh Editor để phân tích Level lớn hoặc nhiều Level: vòng lặp đó giữ main thread cho tới khi toàn bộ job kết thúc.

Khi phân tích nhiều Level, tìm asset bằng `AssetDatabase.FindAssets("t:LevelDataSO")`, xác minh danh sách và chạy từng job theo Editor update. Ghi đường dẫn asset cho mỗi kết quả vì nhiều asset có thể trùng tên. Không khởi chạy lại job đang chạy hoặc tự tăng ngân sách vô hạn.

## Cách đọc trạng thái và độ chính xác

| Trạng thái | Ý nghĩa | Hành động của Agent |
| --- | --- | --- |
| Running | Đang duyệt | Chờ; chưa báo điểm tổng |
| Complete | Có điểm và M* đã được chứng minh | Đọc `HasScore`, kiểm tra fingerprint và giải thích đóng góp |
| Partial | Tắt bộ giải | Báo Condition/Dependency; chưa có M* và điểm tổng |
| LimitExceeded | Hết ngân sách, chưa chứng minh M* | Báo giới hạn; có thể tăng ngân sách và chạy lại nếu cần |
| Unsolvable | Không có lời giải hoặc không đủ Move theo kiểm tra hiện tại | Báo `Problems`; không quy thành Difficulty = 100 |
| InvalidInput | Level/settings không hợp lệ | Nêu lỗi dữ liệu; sửa khi nằm trong phạm vi nhiệm vụ |
| Cancelled | Đã hủy | Không dùng dữ liệu duyệt dở làm kết quả cuối |

Các cờ phải được hiểu riêng:

- `HasScore`: chỉ khi true mới dùng `DifficultyScore`. Giá trị 0 mặc định khi false không phải “Level dễ”.
- `Valid`: chưa có problem được phát hiện; khi solver chưa hoàn tất, cờ này không tự chứng minh Level có lời giải.
- `HasExactMoves`: M* đã được chứng minh. Có thể true dù `SolverExact = false`, nếu bộ giải đã tìm được lời giải đạt cận dưới `WaitPeople`.
- `SolverExact`: đã duyệt hết không gian cách xếp. Khi false, `SolutionsFound` chỉ là cận dưới số lời giải.
- `BestFoundMoves >= 0`: có cách xếp hợp lệ đã tìm thấy; nếu chưa có `HasExactMoves`, đây chỉ là cận trên M*. Giá trị -1 nghĩa là chưa tìm được cách xếp.

Hết ngân sách và chưa tìm được lời giải không đồng nghĩa vô nghiệm. Đọc `Problems` để phân biệt với các trường hợp đã được chứng minh không thể hoàn thành, ví dụ `WaitPeople > LevelMove`.

## Công thức và mapping field

```text
Difficulty = 100 × (0.50C + 0.30Dp + 0.20M)
C  = 0.80T + 0.20O
Dp = 0.25L + 0.30H + 0.45Y
M  = 0.70Bt + 0.30R
```

| Ký hiệu | Field | Cách đọc |
| --- | --- | --- |
| P | `N` | Tổng số Person, gồm MainGrid và WaitGrid |
| S | `S` | Số Seat MainGrid |
| B | `M` | LevelMove; field `M` của result là ngân sách, không phải điểm Move |
| W trong công thức Move | `WaitPeople` | Số Person ban đầu ở WaitGrid; field `W` của result là số Seat WaitGrid |
| T | `Tightness` | Trung bình Ti = 1 - Ri |
| O | `Occupancy` | P/S; càng cao càng ít Seat dư |
| L | `LikeDensity` | Số Like Person Conditions / (2P) |
| H | `DependencyDepth` | dmax/(dmax+1); dmax là `MaxDependencyDepth` |
| Y | `CycleRatio` | `CyclePeople`/P |
| Bt | `BudgetTightness` | M*/LevelMove |
| R | `Rearrangement` | Clamp((M* - WaitPeople)/P, 0, 1) |
| C / Dp / M | `ConditionComplexity` / `DependencyComplexity` / `MovePressure` | Điểm nhóm trong khoảng 0–1 cho Level hợp lệ |
| Đóng góp | `ConditionContribution` / `DependencyContribution` / `MoveContribution` | Đơn vị điểm trên thang 100; tổng bằng `DifficultyScore` khi `HasScore` |
| M* / Move dư | `MinMoves` / `Slack` | Chỉ diễn giải khi `HasExactMoves` |

Trọng số hiện cố định trong implementation. Không áp dụng freedom multiplier, unique bonus, log-permutation hoặc các hệ số A/B/C/D/F cũ.

## Giải thích nguyên nhân độ khó

- **Condition:** xem `People[].ExpectedSeatRatio` (Ri), `People[].Tightness` (Ti) và `SeatProbabilities`. `Conditions[].Probability` là q trung bình trên Seat, không phải xác suất Person ban đầu Happy. `ConditionProbabilities` cho q từng Condition; `JointProbability` cho Q(x).
- **Dependency:** VÀ giữa các TargetTrait của một Person, HOẶC giữa các Person có trait tương ứng. Hate Person không tạo cạnh Dependency. Tầng 0 không cần Like Person; tầng tiếp theo chỉ dùng provider đã phân tầng trước đó.
- **Chu trình:** `CyclePeople` chỉ đếm Person thực sự thuộc chu trình sau phân tầng. `BlockedPeople` còn gồm nhánh phụ thuộc vào chu trình. Kiểm tra `InDependencyCycle`; không coi mọi `DependencyLayer = -1` là nằm trong chu trình. Chu trình không tự đồng nghĩa vô nghiệm.
- **Move:** Bt cao nghĩa là ít lượt dự phòng; R cao nghĩa là cần nhiều thao tác sắp xếp lại ngoài việc đưa Person từ WaitGrid lên MainGrid.

## Những giới hạn cần giữ khi diễn giải

1. Kề bốn hướng; không xét đường chéo. Food xét chính xác từ bố cục. Person dùng xác suất hypergeometric theo Kx của từng Seat và loại chính Person đang xét khỏi số provider.
2. Like Food Any là CanSitAnywhere. Hate Food Any yêu cầu không cạnh bất kỳ Food nào, theo rule gameplay hiện tại.
3. Điều kiện trùng cùng mục tiêu chỉ tính một lần trong tích Q(x); Like/Hate cùng mục tiêu là mâu thuẫn. Các điều kiện còn lại dùng xấp xỉ tích V1. Ri > 0 không chứng minh mọi Person có thể đồng thời thỏa; cần bộ giải.
4. M* dựa trên Move/Swap trực tiếp giữa các Seat, không phụ thuộc khoảng cách và không hạn chế trạng thái trung gian. `BestAssignment` là cách xếp cuối, không phải chuỗi thao tác để replay.
5. LevelMove = 0 với M* = 0 dùng quy ước áp lực Move bằng 0. LevelMove âm không hợp lệ.
6. Không tự gán nhãn dễ/khó hoặc dự đoán tỷ lệ Win từ điểm trước khi hiệu chỉnh bằng dữ liệu người chơi. So sánh bằng cùng model/version và bằng kết quả còn khớp dữ liệu.

## Xuất và kiểm tra báo cáo

Ưu tiên JSON cho xử lý tự động, TXT cho giải thích, CSV cho bảng đối chiếu. CSV hiện là dạng `key,value` với field/list được trải phẳng, không phải một hàng cho mỗi Level.

Trước khi sử dụng báo cáo qua API, kiểm tra:

```csharp
bool current = analysis.InputFingerprint ==
    DifficultyAnalysisJob.ComputeInputFingerprint(level, settings);
```

Nếu không khớp, chạy lại; không chỉnh fingerprint để tái sử dụng điểm cũ. Với input bị từ chối từ sớm, fingerprint có thể chưa được tạo; báo lỗi thay vì coi đó là báo cáo có điểm hiện hành.

Khi so sánh trước/sau, dùng cùng model/version và settings để dễ kiểm tra lại, lưu đường dẫn asset, fingerprint và trạng thái cho mỗi lần chạy. Chỉ tính chênh lệch điểm khi cả hai lần có `HasScore = true`.

## Kiểm tra khi sửa implementation

Không cần chạy lại toàn bộ tests nếu nhiệm vụ chỉ đọc hoặc xuất báo cáo. Khi sửa công thức, solver hoặc GUI:

1. Kiểm tra Unity compile và Console sau import. Phân biệt lỗi thật với log lỗi được test dự kiến phát ra.
2. Chạy EditMode fixture `Game.Tests.EditMode.DifficultyAnalyzerTests`; mở rộng sang assembly `Game.Tests.EditMode` khi cần kiểm tra tích hợp.
3. Dùng Test Runner GUI hoặc `TestRunnerApi.Execute(...)` ở chế độ bất đồng bộ. Không đặt `runSynchronously = true` qua MCP trong project này: lượt chạy đồng bộ đã làm Editor kẹt ở bước chuẩn bị tests.
4. Nếu chạy qua API, đăng ký callback `ICallbacks.RunFinished`, lưu XML bằng `TestRunnerApi.SaveResultToFile(...)`, rồi đọc số Passed/Failed từ XML. Lệnh “started” không phải bằng chứng tests đạt.
5. Kiểm tra các tab GUI khi có kết quả Complete và khi có lỗi/Partial/LimitExceeded; kiểm tra cảnh báo dữ liệu lỗi thời và xuất báo cáo.
6. Nếu tạo PR, chạy convention lint theo `AGENTS.md` khi script có trong checkout; nếu thiếu, báo rõ chưa chạy được. Không tự tạo script thay thế và gọi đó là lint chuẩn.

Nếu Editor kẹt, dừng gửi thêm lệnh và thông báo tình trạng. Không đóng cưỡng bức hoặc khởi động lại Editor khi chưa có xác nhận chấp nhận rủi ro mất thay đổi chưa lưu.

## Mẫu bàn giao kết quả

```text
Level: <đường dẫn asset>
Model: <ModelId> v<ModelVersion>
Status: <Status>; dữ liệu hiện hành: <có/không>
Điểm: <DifficultyScore>/100 hoặc “chưa có điểm tổng”
Đóng góp: Condition <...>, Dependency <...>, Move <...> điểm
M*: <MinMoves nếu HasExactMoves>; LevelMove: <M>; Move dư: <Slack>
Bộ giải: HasExactMoves=<...>, SolverExact=<...>, Nodes=<...>
Nguyên nhân chính: <Person/Condition/trait/tọa độ liên quan>
Problems / Warnings: <nội dung hoặc không có>
Báo cáo: <đường dẫn đã lưu, nếu có>
Kiểm tra đã thực hiện: <compile/tests/GUI hoặc giới hạn chưa kiểm tra>
```
