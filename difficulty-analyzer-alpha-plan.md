# Plan: Difficulty Analyzer theo mô hình α

## 1. Mục tiêu và phạm vi

Nguồn công thức: `Difficulty Formula.md`.

Nâng cấp Unity Editor tool hiện có tại `Tools/Wanna Sit Here/Difficulty Analyzer` để tính:

```text
Difficulty(level, M, α) = 100 × (1 − W(s0, M; α))
```

Kết quả phải kèm α, Move Limit và phiên bản mô hình. Đây là xác suất thất bại theo mô hình hành vi đã chọn, chưa phải độ khó được hiệu chỉnh bằng dữ liệu người chơi.

Giả định triển khai:

- Giữ benchmark gameplay hiện tại: move hoặc swap hợp lệ đều tốn một move; không dùng booster hay tutorial gate; loại thao tác không đổi chỗ và không tốn move.
- Dùng `LevelDataSO.levelMove` làm M; adjacency do người dùng xác nhận trong tool vì không nằm trong LevelDataSO.
- α được nhập trong [0, 1]. Đề xuất mặc định 0,5 để dùng thử, chưa coi là đại diện người chơi trung bình.
- Chọn đều giữa các Good action khi nhánh suy luận được chọn; nhánh ngẫu nhiên chọn đều giữa mọi raw legal action.
- Graph đầy đủ, khả nghịch và chi phí mỗi action bằng 1 là điều kiện của cách phân loại ba nhóm hiện tại.

Không sửa runtime gameplay, level asset hay công thức nguồn. Không thêm mô phỏng gần đúng, calibration, batch level hoặc một tool độc lập. File `difficulty-analyzer-plan.md` hiện cũng chứa mô tả công thức nên được giữ nguyên; plan triển khai nằm riêng trong file này.

## 2. Hiện trạng và phần tái sử dụng

| Thành phần | Hiện trạng | Thay đổi cần làm |
| --- | --- | --- |
| `DifficultyAnalysisJob.cs` | Enumerate state graph, gộp successor giữ multiplicity, reverse BFS, DP; policy `exp(-β × cost)` với β = 1 | Giữ graph/BFS; thay policy và phép cộng DP bằng công thức α |
| `DifficultyAnalysisResult.cs` | Score, win/fail, β, thống kê, fingerprint, solution | Thay β bằng α; thêm model ID/version; thống nhất Good/Neutral/Bad |
| `DifficultyAnalyzerWindow.cs` | Chọn level/adjacency, analyze/cancel, progress, kết quả, JSON/CSV | Nhập α, hiển thị mô hình mới, cập nhật stale detection và export |
| `DifficultyAnalyzerTests.cs` | 5 test, gồm kỳ vọng Boltzmann và kiểm tra số raw action | Thay kỳ vọng cũ; bổ sung các test xác suất và trường hợp biên |

Các file trên nằm trong `Assets/Game/Editor/Difficulty/`, riêng test nằm trong `Assets/Game/Tests/`. Giữ assembly và cấu trúc hiện có; không chuyển thư mục theo sơ đồ tổng quát của AGENTS.md.

Graph vẫn mở rộng goal để bảo toàn cấu trúc khả nghịch cho BFS. Chỉ trong DP, goal mới là trạng thái hấp thụ: đã thắng thì xác suất thắng luôn bằng 1.

## 3. Thiết kế thuật toán

### 3.1. Khoảng cách và số action

1. Xây toàn bộ graph reachable từ trạng thái ban đầu như hiện tại.
2. Reverse BFS từ mọi goal để tính d(s).
3. Với mỗi non-goal có d hữu hạn, phân loại cạnh theo `d(next) − d(s)`: −1 là Good, 0 là Neutral, +1 là Bad.
4. Tính G, N, B, L bằng tổng **multiplicity**, không dùng số successor duy nhất. Hai thao tác swap khác nguồn nhưng tới cùng state vẫn là hai raw action trong nhánh ngẫu nhiên.

Có thể giữ mã cost nội bộ 0/1/2 đang có tương ứng Good/Neutral/Bad để giảm thay đổi. Không dùng cost đó làm trọng số exponential nữa. Lưu G và L theo state để không phải đếm lại ở mỗi vòng DP; chỉ lưu thêm N/B khi cần cho báo cáo.

### 3.2. Policy α

Với mỗi raw action tại non-goal giải được:

```text
p(action) = α / G + (1 − α) / L     nếu Good
p(action) =         (1 − α) / L     nếu Neutral hoặc Bad
```

Với cạnh gộp có multiplicity k:

```text
p(edge) = k × p(action)
```

Công thức này tương đương công thức nhóm trong tài liệu:

```text
PG = α + (1 − α) × G/L
PN =     (1 − α) × N/L
PB =     (1 − α) × B/L
```

Dùng chung một hàm tính xác suất cạnh nhỏ, thuần C# cho initial action profile và DP để tránh hai nơi lệch nhau. Không cần framework policy hoặc giữ chế độ β trong UI. Tổng xác suất cạnh phải bằng 1 trong sai số số thực; không chia lại cho tổng trọng số như Boltzmann để che lỗi tính policy.

### 3.3. Dynamic programming

```text
W(goal, m) = 1                         với mọi m ≥ 0
W(non-goal, 0) = 0
W(s, m) = Σ p(edge) × W(next, m − 1)   với m > 0
```

Giữ hai rolling buffer bằng double và cơ chế chia việc theo editor tick. Thay tích lũy numerator/denominator hiện tại bằng tổng xác suất; reset accumulator đúng khi hoàn tất từng state và bảo toàn khi yield giữa các cạnh.

Độ phức tạp sau khi có graph: BFS O(V + E), DP O(M × (V + E)); bộ nhớ O(V + E), không lưu bảng V × M. Giai đoạn dựng graph vẫn phụ thuộc số raw action được duyệt.

### 3.4. Trường hợp biên và giới hạn

- Kiểm tra α hữu hạn và trong [0,1] tại API, kể cả khi UI dùng slider. Reject NaN, Infinity và giá trị ngoài miền.
- Goal được xử lý trước điều kiện hết move; goal với M = 0 vẫn score 0. Policy ở goal hiển thị N/A vì không cần chọn tiếp.
- Nếu d(s0) > M: win = 0, score = 100, giữ thông tin shortest solution.
- Nếu không có goal reachable: trạng thái `Unsolvable`, win = 0, score = 100 sau khi đã duyệt graph đầy đủ.
- Dead state có d = ∞ hoặc non-goal không có action: W = 0; không chia cho G = 0 và không gán PG = α.
- Non-goal có d hữu hạn bắt buộc G > 0. Nếu không, báo graph không nhất quán, không trả score.
- Nếu graph vi phạm khả nghịch/unit cost hoặc có cạnh không thể phân loại trong benchmark này: báo unsupported/invalid rõ ràng; không tự ép vào Bad. Không mở rộng công thức cho luật irreversible trong đợt này.
- Giữ giới hạn hiện tại: 25.000 state, 750.000 cạnh gộp, 5.000.000 raw action được duyệt, 30 giây/job, khoảng 8 ms/tick. Cancel hoặc vượt giới hạn không được xuất score chính xác từ graph dở dang.
- Chỉ clamp sai số làm tròn rất nhỏ ở kết quả cuối; sai lệch xác suất đáng kể phải lộ qua validation/test.

## 4. UI, kết quả và export

Luồng sử dụng:

1. Chọn LevelDataSO và xác nhận adjacency.
2. Nhập α bằng slider kèm ô số, mặc định đề xuất 0,5. Giải thích ngắn: 0 = chọn ngẫu nhiên; 1 = luôn chọn Good.
3. Analyze → theo dõi progress hoặc Cancel.
4. Đọc Difficulty 0–100, win/fail %, α, Move Limit, d(s0), slack, Good/Neutral/Bad và shortest solution.
5. Export JSON/CSV khi kết quả hoàn tất, có score và chưa stale.

Phân biệt rõ tỷ lệ raw action G/L, N/L, B/L với xác suất lựa chọn PG, PN, PB. Không mô tả alpha là xác suất chọn Good: xác suất chọn Good còn bao gồm phần ngẫu nhiên chọn trúng.

Schema mới dùng `ModelId = AlphaGoodMoveMixture`, `ModelVersion = 1`, `Alpha`, cùng các trường kết quả đang cần. Đổi tên trường Optimal/Regressive sang Good/Bad đồng bộ trong code, UI, test và export; rà toàn repo trước khi đổi để cập nhật mọi consumer. Đây là phiên bản schema mới, không đổi nhãn dữ liệu β cũ thành α.

Fingerprint phải bao gồm toàn bộ input hiện có, α, model ID/version. Snapshot α khi tạo job; thay input trong lúc chạy hoặc sau khi chạy làm kết quả stale, không làm đổi policy giữa chừng. Export ghi α thực sự đã dùng của result, dùng định dạng số invariant culture cho CSV. Điểm từ β và α không được coi là cùng một thang benchmark đã hiệu chỉnh.

## 5. Trình tự triển khai và kiểm chứng

### Bước 1 — Chốt contract và test policy

- Thêm Alpha và model metadata vào result/job; cập nhật các caller cùng lúc để project vẫn compile.
- Viết test công thức trực tiếp và đầu vào không hợp lệ trước khi thay policy.
- Verify: compiler pass; các kỳ vọng toán học dưới đây có test độc lập với DP.

### Bước 2 — Thay policy trong solver

- Đếm raw G/L, dùng chung hàm xác suất, thay initial profile và DP.
- Xử lý goal/dead state và giữ trạng thái accumulator khi yield.
- Verify: test end-to-end của level nhỏ, multiplicity, cycle, các endpoint α và giới hạn move đều pass.

### Bước 3 — Nối UI và export

- Thêm điều khiển α; bỏ β; cập nhật nhãn, result schema, fingerprint, JSON/CSV và thông báo hoàn thành.
- Verify: đổi α tạo kết quả mới; đổi input làm stale; cancel không để score cũ trông như kết quả của job mới; export khớp UI.

### Bước 4 — Regression và hướng dẫn dùng

- Chạy toàn bộ EditMode tests và kiểm tra Unity compile/console. Chạy convention lint nếu script có trong checkout; ghi rõ nếu thiếu.
- Analyze Level1, Level2, Level3 và TestLevel với adjacency bốn hướng ở α = 0, 0,5, 1. Lưu bảng α, M, d0, score, state/edge count và thời gian để làm baseline mới.
- Verify: α = 1 cho score 0 khi M ≥ d0; level đã thắng score 0; không dùng các số đo β cũ làm expected score mới.
- Cập nhật hướng dẫn sử dụng ngắn, ghi rõ ý nghĩa α và giới hạn graph đầy đủ.

## 6. Ma trận test bắt buộc

| Trường hợp | Kết quả mong đợi |
| --- | --- |
| G=1, N=1, B=2, α=0,5 | Xác suất từng action: 0,625; 0,125; 0,125; 0,125 |
| G=2, N=1, B=1, α=0,5 | Mỗi Good 0,375; mỗi action còn lại 0,125; tổng 1 |
| α=0 | Mỗi raw action xác suất 1/L, cạnh gộp k/L |
| α=1 | Chỉ Good có xác suất; chia đều 1/G |
| Một Good, một Neutral, M=1, α=0,5 | Win 0,75; Difficulty 25, thay expected Boltzmann hiện tại |
| Cạnh gộp và raw action enumeration | Cùng xác suất và score trên fixture chưa thắng có swap multiplicity > 1 |
| Graph nhỏ có cycle, M nhỏ | Rolling DP khớp oracle enumerate mọi chuỗi action tới M hoặc tới goal |
| Goal ngay từ đầu, M=0 | Win 1; Difficulty 0; policy N/A |
| Non-goal M=0 hoặc M<d0 | Win 0; Difficulty 100 |
| Không có reachable goal / không có legal action | Win 0; không NaN, Infinity hay chia 0 |
| α ngoài miền, NaN, Infinity | InvalidInput, không score |
| Tăng M với α cố định | Win không giảm trong tolerance |
| Job nhiều tick và job chạy liên tục trên cùng input | Cùng score; accumulator không rò giữa state |
| Cancel / resource limit | Không có score/export được coi là hoàn tất |
| Đổi α, level data, adjacency hoặc model version | Fingerprint đổi; kết quả cũ stale |
| JSON/CSV | Model, α, M, score và xác suất khớp result; không còn field β |

Không dùng giả định “score luôn giảm khi tăng α” làm test tổng quát nếu chưa chứng minh cho graph đang xét. Điều kiện endpoint α=1 và đơn điệu theo M có kỳ vọng rõ ràng hơn.

## 7. Mở rộng sau bản đầu: so sánh nhiều α

Tài liệu công thức khuyến nghị xem nhiều mức suy luận. Sau khi bản một α được kiểm chứng, có thể thêm nút Compare với các mức 0; 0,25; 0,5; 0,75; 1 và bảng α/win/difficulty. Dựng graph và BFS một lần, chạy DP tuần tự cho từng α, tái sử dụng buffer; cần progress/cancel và ngân sách thời gian rõ cho cả lượt so sánh.

Phần này không chặn bản đầu và chưa cần thêm cache graph tồn tại giữa các lần sửa level, biểu đồ hoặc hệ thống cấu hình profile.

## 8. Điều kiện hoàn thành bản đầu

- Cửa sổ hiện tại tính và export đúng mô hình α, không còn policy β trong đường tính mới.
- Người dùng biết score gắn với α nào; các tỷ lệ action và xác suất lựa chọn được phân biệt.
- Test toán học, end-to-end, regression và kiểm tra Unity hoàn tất; mọi kiểm tra chưa chạy phải được báo rõ.
- Có baseline mới cho level thật; runtime và dữ liệu level không bị sửa ngoài phạm vi.
- Không trả số giả khi graph chưa hoàn chỉnh hoặc input không được mô hình hỗ trợ.
