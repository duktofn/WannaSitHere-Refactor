# Mô hình tính Difficulty

## Tổng quan

Mô hình trả lời một câu hỏi: **nếu một Player với khả năng suy luận cho trước chơi Level này, xác suất họ thắng trong giới hạn số Move là bao nhiêu?** Xác suất thua càng cao thì Level càng khó.

Cách làm gồm 4 bước:

1. Với mỗi State, phân loại các legal Move thành Good / Neutral / Bad dựa trên việc Move đó đưa Player lại gần hay xa Goal.
2. Dùng tham số `α` để mô hình hóa việc Player nhận ra Move tốt tới mức nào, từ đó ra xác suất Player chọn từng Move.
3. Tính xác suất thắng `W(s, m)` bằng cách đi qua toàn bộ State Graph.
4. Đổi xác suất thua thành điểm Difficulty trên thang 0–100.

---

## 1. Khoảng cách tới Goal

Với mỗi State `s`, định nghĩa:

$$ d(s) = \text{số Move ít nhất để đi từ } s \text{ tới một State thắng} $$

`d(s)` cho biết State đó "còn xa Goal bao nhiêu". Goal có `d = 0`.

Một legal Move đưa Board từ `s` sang `s'`. So sánh `d(s')` với `d(s)` để phân loại Move:

| Loại             | Điều kiện          | Ý nghĩa                                  |
| ---------------- | ------------------ | ---------------------------------------- |
| **Good Move**    | $d(s') = d(s) - 1$ | Nằm trên một đường đi ngắn nhất tới Goal |
| **Neutral Move** | $d(s') = d(s)$     | Không tiến, không lùi                    |
| **Bad Move**     | $d(s') = d(s) + 1$ | Đi xa Goal hơn                           |

---

## 2. Tỷ lệ Move tại mỗi State

Tại State `s`, gọi:

- `L(s)`: tổng số Legal Move
- `G(s)`: số Good Move
- `N(s)`: số Neutral Move
- `B(s)`: số Bad Move

$$ L(s) = G(s) + N(s) + B(s) $$

Tỷ lệ từng loại:

$$ g(s) = \frac{G(s)}{L(s)}, \qquad n(s) = \frac{N(s)}{L(s)}, \qquad b(s) = \frac{B(s)}{L(s)} $$

và:

$$ g(s) + n(s) + b(s) = 1 $$

**Ý nghĩa:** `g(s)`, `n(s)`, `b(s)` chính là xác suất chọn từng loại Move nếu Player chọn ngẫu nhiên đều trong các Legal Move. Ví dụ State có nhiều Bad Move và rất ít Good Move thì `b(s)` lớn, `g(s)` nhỏ, Player chọn bừa sẽ dễ đi sai. Đây là nguồn gốc của độ khó.

---

## 3. Điều chỉnh theo khả năng suy luận của Player

Định nghĩa tham số:

$$ \alpha \in [0, 1] $$

- `α = 0`: Player không phân biệt được Move tốt/xấu, chọn theo phân bố Legal Move (tức là ngẫu nhiên đều).
- `α = 1`: Player luôn nhận ra và chọn Good Move.
- `0 < α < 1`: Player mạnh ở mức tương ứng.

**Cách hiểu `α`:** tại mỗi lượt, với xác suất `α` Player "nhìn ra" đúng Move tốt và chọn nó. Với xác suất `1 − α` Player không nhìn ra và chọn ngẫu nhiên như trường hợp `α = 0`.

Xác suất Player chọn từng **loại** Move:

**Good Move**

$$ P_G(s) = \alpha + (1 - \alpha), g(s) $$

Gồm phần "nhìn ra" (`α`) cộng phần "chọn ngẫu nhiên nhưng trúng Good" (`(1 − α)·g(s)`).

**Neutral Move**

$$ P_N(s) = (1 - \alpha), n(s) $$

**Bad Move**

$$ P_B(s) = (1 - \alpha), b(s) $$

Neutral và Bad chỉ được chọn khi Player không nhìn ra, nên chỉ có phần ngẫu nhiên.

Ba xác suất luôn cộng lại bằng 1:

$$ P_G(s) + P_N(s) + P_B(s) = 1 $$

---

## 4. Xác suất của một Move cụ thể

Bước 3 cho xác suất theo **loại**. Để có xác suất cho **từng Move**, giả định các Move cùng loại có xác suất được chọn như nhau. Với Move `a` tại State `s`:

$$ P(a \mid s) = \begin{cases} \dfrac{P_G(s)}{G(s)} & \text{nếu } a \text{ là Good} \[8pt] \dfrac{P_N(s)}{N(s)} & \text{nếu } a \text{ là Neutral} \[8pt] \dfrac{P_B(s)}{B(s)} & \text{nếu } a \text{ là Bad} \end{cases} $$

Các trường hợp mẫu số bằng `0` được bỏ qua, vì khi đó không tồn tại Move thuộc loại tương ứng.

### Ví dụ minh họa

State `s` có `L = 4` Legal Move: `G = 1`, `N = 1`, `B = 2`. Player có `α = 0.5`.

Tỷ lệ: `g = 1/4 = 0.25`, `n = 0.25`, `b = 2/4 = 0.5`.

Xác suất theo loại:

- `P_G = 0.5 + 0.5 × 0.25 = 0.625`
- `P_N = 0.5 × 0.25 = 0.125`
- `P_B = 0.5 × 0.5 = 0.25`

Xác suất từng Move:

|Move|Cách tính|`P(a\|s)`|
|---|---|---|
|Good|0.625 / 1|0.625|
|Neutral|0.125 / 1|0.125|
|Bad #1|0.25 / 2|0.125|
|Bad #2|0.25 / 2|0.125|

Tổng = 1.

---

## 5. Xác suất thắng từ một State

Định nghĩa:

$$ W(s, m) = \text{xác suất thắng từ State } s \text{ khi còn } m \text{ Move} $$

### Điều kiện biên

Nếu `s` đã là State thắng:

$$ W(s, m) = 1 $$

Nếu đã hết Move nhưng `s` chưa phải State thắng:

$$ W(s, 0) = 0 $$

Kiểm tra State thắng trước, rồi mới kiểm tra hết Move. Nhờ vậy nếu Player thắng đúng ở Move cuối cùng thì vẫn tính là thắng.

### Công thức truy hồi

Với các trường hợp còn lại:

$$ W(s, m) = \sum_{a \in A(s)} P(a \mid s); W\big(T(s, a),, m - 1\big) $$

Trong đó:

- `A(s)`: tập toàn bộ Legal Move tại `s`
- `P(a|s)`: xác suất Player chọn Move `a` (tính ở bước 4)
- `T(s, a)`: State mới sau khi thực hiện Move `a`

**Cách hiểu:** đứng ở `s`, Player chọn Move `a` với xác suất `P(a|s)`, sau đó còn `m − 1` Move để thắng từ State mới. Xác suất thắng tại `s` là tổng của các nhánh, mỗi nhánh được nhân với xác suất đi vào nhánh đó.

**Cách tính:** `W(·, m)` chỉ phụ thuộc `W(·, m − 1)`, nên tính lần lượt từ `m = 0` lên `m = M` cho toàn bộ State, không cần tính lặp lại (dynamic programming).

---

## 6. Difficulty của Level

Gọi:

- `s₀`: Initial State
- `M`: Move Limit của Level

Xác suất thắng Level:

$$ WinRate = W(s_0, M) $$

Xác suất thua:

$$ FailRate = 1 - W(s_0, M) $$

Difficulty Score:

$$ \boxed{Difficulty = 100,\big[1 - W(s_0, M)\big]} $$

Ví dụ: `WinRate = 0.3` thì `FailRate = 0.7`, `Difficulty = 70`.

---

## 7. Flow tổng

```text
State s
  ↓
Tính d(s)
  ↓
Phân loại Legal Move: Good / Neutral / Bad
  ↓
Tính g(s), n(s), b(s)
  ↓
Áp dụng Player Reasoning α
  ↓
Tính P(a|s)
  ↓
Tính W(s, m) trên toàn State Graph
  ↓
WinRate = W(s₀, MoveLimit)
  ↓
FailRate = 1 - WinRate
  ↓
Difficulty = FailRate × 100
```

---

## 8. Công thức tổng hợp

Khoảng cách và phân loại:

$$ d(s) = ShortestDistance(s, Goal) $$

Tỷ lệ Move:

$$ g(s) = \frac{G(s)}{L(s)}, \qquad n(s) = \frac{N(s)}{L(s)}, \qquad b(s) = \frac{B(s)}{L(s)} $$

Xác suất theo loại (có `α`):

$$ P_G(s) = \alpha + (1 - \alpha), g(s), \qquad P_N(s) = (1 - \alpha), n(s), \qquad P_B(s) = (1 - \alpha), b(s) $$

Xác suất từng Move:

$$ P(a \mid s) = \begin{cases} \dfrac{P_G(s)}{G(s)} & a \in Good \[8pt] \dfrac{P_N(s)}{N(s)} & a \in Neutral \[8pt] \dfrac{P_B(s)}{B(s)} & a \in Bad \end{cases} $$

Xác suất thắng:

$$ W(s, m) = \sum_{a \in A(s)} P(a \mid s); W\big(T(s, a),, m - 1\big) $$

với:

$$ W(Goal, m) = 1, \qquad W(s, 0) = 0 ;; (s \neq Goal) $$

Kết quả:

$$ \boxed{Difficulty = 100,\big[1 - W(s_0, MoveLimit)\big]} $$

---

## 9. Lưu ý về giả định của mô hình

- **Move có thể quay lại được?** Cách chia Good / Neutral / Bad ở mục 1 chỉ đủ nếu `d(s')` luôn nằm trong `d(s) − 1`, `d(s)` hoặc `d(s) + 1`. Nếu Level có Move không thể hoàn tác (làm `d(s')` tăng hơn 1 hoặc thành vô hạn), cần quyết định gộp chúng vào Bad hay tạo loại riêng.
- **State không thể thắng (dead State):** khi `d(s)` vô hạn thì không có Good Move, nhưng công thức vẫn cho `P_G(s) = α`, nên phần xác suất này không gán được cho Move nào và tổng `P(a|s)` chỉ còn `1 − α`. Cách xử lý đơn giản: gán `W(s, m) = 0` cho các State này.
- **Chia đều trong cùng loại:** đây là giả định đơn giản hóa ở mục 4. Player thật có thể ưu tiên một số Move (ví dụ Move gần vị trí vừa đi) hơn các Move cùng loại.
- **`α` là tham số:** nên tính Difficulty với vài mức `α` để thấy Level khó thế nào với Player yếu và Player giỏi.