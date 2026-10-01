# Difficulty Analyzer — mô hình Condition / Dependency / Move

Mở **Tools > Wanna Sit Here > Difficulty Analyzer**, chọn LevelDataSO hoặc dùng Level đang chọn, bấm **Tính độ khó**.

## Công thức V1

`Difficulty = 100 × [0.50 C + 0.30 Dp + 0.20 M]`

- `C = 0.80 T + 0.20 O`, `O = P/S`, `T = mean(1 - Ri)`.
- `Ri = mean_seats(Qi(x))`, `Qi(x) = product(qc(x))`.
- Food tính trực tiếp trên bố cục; Person dùng `P_none = C(S-1-Kx, N_target) / C(S-1, N_target)` theo từng Seat, không dùng degree trung bình. Không đếm chính Person đang xét.
- Like Food Any là CanSitAnywhere. Hate Food Any yêu cầu không cạnh bất kỳ Food nào, theo ConditionChecker hiện tại.
- Điều kiện trùng cùng mục tiêu chỉ nhân một lần. Like/Hate cùng mục tiêu là mâu thuẫn. Các điều kiện khác dùng xấp xỉ tích của V1; bộ giải kiểm tra đồng thời tất cả điều kiện để chứng minh lời giải.
- `Dp = 0.25 L + 0.30 H + 0.45 Y`, `L = N_like/(2P)`, `H = dmax/(dmax+1)`, `Y = P_cycle/P`.
- Phân tầng Dependency đồng thời: VÀ giữa TargetTrait, HOẶC giữa các Person có trait tương ứng ở tầng trước. Không tạo Dependency từ Hate Person.
- Chu trình tính trên đồ thị yêu cầu còn lại sau phân tầng. Chỉ đếm Person thực sự thuộc thành phần liên thông mạnh có chu trình; không đếm Person chỉ phụ thuộc vào chu trình. Các lựa chọn đã có provider phân tầng được không tạo cạnh chu trình.
- `M = 0.70 Bt + 0.30 R`, `Bt = M*/LevelMove`, `R = clamp((M* - WaitPeople)/P, 0, 1)`.

## Bộ giải và trạng thái

Bộ giải duyệt các cách xếp cuối cùng thỏa tất cả Conditions. Với Move/Swap trực tiếp giữa Seat và không có hạn chế ở trạng thái trung gian, chi phí tới một cách xếp bằng số Person phải đổi vị trí trừ số chu trình hoán vị chiếm Seat ban đầu. Lấy chi phí nhỏ nhất trên tất cả cách xếp hợp lệ để xác định M*.

Phân tích chạy theo lát tối đa khoảng 8 ms trên mỗi Editor update. Ngân sách node giới hạn công việc; thanh tiến độ thể hiện ngân sách đã dùng, không phải phần trăm toàn bộ không gian đã duyệt.

- **Complete**: đã chứng minh M*, có điểm tổng. Nếu đạt cận dưới WaitPeople thì M* đã chính xác ngay cả khi chưa duyệt hết các cách xếp.
- **Partial**: tắt bộ giải; chỉ có Condition và Dependency.
- **LimitExceeded**: hết ngân sách, chưa chứng minh M*. Không tạo điểm tổng từ ước lượng. BestFoundMoves là cận trên M*, SolutionsFound là cận dưới số lời giải.
- **Unsolvable**: quá ít Seat, mục tiêu Like không tồn tại, Conditions mâu thuẫn/không có Seat khả dụng, không có state Win, hoặc không đủ LevelMove. Không có điểm độ khó.
- **InvalidInput**: dữ liệu Level hoặc thiết lập không hợp lệ.
- **Cancelled**: đã hủy; không xuất điểm từ kết quả đang duyệt.

Nếu LevelMove = 0 và state ban đầu đã Win thì M* = 0, áp lực Move được quy ước bằng 0. LevelMove âm không hợp lệ. Không gán tier dễ/khó trước khi hiệu chỉnh bằng dữ liệu người chơi.

## GUI và báo cáo

- **Tổng quan**: điểm tổng, ba nhóm đóng góp và bảy chỉ số nguyên tử, M*, Move dư, lỗi và cảnh báo.
- **Person / Condition**: Ri/Ti, xác suất trung bình từng Condition, trạng thái ban đầu và tầng Dependency.
- **Bàn / Dependency**: heatmap Q(x) của Person đang chọn; hover để xem Kx, qc(x), ô kề và tọa độ. Hiển thị provider theo trait, chu trình, nhánh phụ thuộc và cách xếp tốt nhất.
- **Báo cáo**: copy hoặc xuất TXT/JSON/CSV có dữ liệu theo từng Person, Condition, Seat, công thức và thông tin bộ giải.

Báo cáo đánh dấu lỗi thời khi Level, asset tham chiếu hoặc thiết lập đổi; cần tính lại trước khi xuất. Tool không sửa Level asset.

## Kiểm tra

EditMode fixture `Game.Tests.EditMode.DifficultyAnalyzerTests` kiểm tra công thức, xác suất hypergeometric, Dependency VÀ/HOẶC, chu trình và nhánh phụ thuộc, rule Hate Food Any, vô nghiệm, Move budget, báo cáo và chi phí Move/Swap so với BFS độc lập.
