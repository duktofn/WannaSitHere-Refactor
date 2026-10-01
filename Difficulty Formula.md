  

# Dependency-Aware Person Placement Model

## 1. Mục tiêu

Model dùng để mô tả và hỗ trợ việc tìm lời giải cho các Level có `Like Person Condition`.

Điểm đặc biệt của `Like Person` là tập vị trí hợp lệ của một Person không chỉ phụ thuộc vào layout tĩnh của Level, mà còn phụ thuộc vào **vị trí hiện tại của những Person mang Trait được target**.

Ví dụ:

```
A:
- Like Cool
- Hate Hamburger
```

Nếu chưa có Person `Cool` nào trên `MainGrid`, hiện tại A chưa thể thỏa `Like Cool`.

Tuy nhiên điều này không có nghĩa A không có lời giải.

Nó chỉ có nghĩa:

> Điều kiện `Like Cool` của A hiện chưa được thỏa và vị trí cuối cùng của A vẫn phụ thuộc vào placement của một Person có Trait `Cool`.

Do đó cần tách hai khái niệm:

```
Condition Evaluation
```

và:

```
Dependency Resolution
```

---

# 2. Gameplay Condition Evaluation

Gameplay runtime vẫn sử dụng rule hiện tại của game.

Với Person (i) đang ở một Seat (x) trên `MainGrid`:

\exists j :  
Trait_j=T  
\land  
Position_j(s)\in Adj(x)  
]

Trong đó:

- (T) là `PersonTrait` target.
    
- (Adj(x)) là tập các Cell cardinal-adjacent của Seat (x).
    
- Chỉ Person trên `MainGrid` tham gia adjacency.
    

Condition `Like` được thỏa khi có ít nhất một adjacent target; `Hate` được thỏa khi không có adjacent target. Đây là semantics hiện tại của Condition system.

Ví dụ:

```
A Like Cool

A ở Seat 5
B(Cool) ở Seat 6
```

và:

```
6 ∈ Adj(5)
```

thì:

[  
LikeCool(A,s)=true  
]

Ngược lại, nếu không có `Cool Person` adjacent:

[  
LikeCool(A,s)=false  
]

Person A có thể vẫn nằm trên MainGrid nhưng ở trạng thái `Angry`.

Model không cần thêm giá trị `Unresolved` vào chính Condition runtime.

---

# 3. Dependency Resolution

`Unresolved` nên được xem là **metadata của solver**, không phải kết quả Condition gameplay.

Với một Trait (T), định nghĩa:

{  
P_j  
\mid  
Trait_j=T  
\land  
P_j\in MainGrid  
}  
]

Nếu:

[  
|Targets_T(s)|=0  
]

thì requirement `Like T` được xem là:

[  
Dependency_T(s)=Unresolved  
]

Nếu:

[  
|Targets_T(s)|>0  
]

thì:

[  
Dependency_T(s)=Resolved  
]

Lưu ý:

```
Resolved ≠ Satisfied
```

Ví dụ:

```
A Like Cool

B(Cool) đã ở MainGrid
nhưng B không đứng cạnh A
```

ta có:

```
Dependency Like Cool = Resolved
Condition Like Cool  = Unsatisfied
A State              = Angry
```

Đây là sự khác biệt quan trọng giữa **dependency availability** và **Condition satisfaction**.

---

# 4. Dependency theo Trait

`Like Person` hiện target `PersonTrait`, không target một Person cụ thể.

Ví dụ:

```
A Like Cool

B = Cool
C = Cool
D = Cool
```

Dependency của A không phải:

```
A → B
```

mà là:

```
A → Cool Requirement
```

với các provider:

```
Cool Requirement
├── B
├── C
└── D
```

A chỉ cần ít nhất một provider xuất hiện trên `MainGrid` để dependency được resolve.

Formal:

[  
Resolved(A,Cool,s)  
\iff  
|Targets_{Cool}(s)|>0  
]

Do đó đây là quan hệ:

```
OR giữa các provider cùng Trait
```

---

# 5. Nhiều Like Person Condition

Giả sử:

```
A:
- Like Cool
- Like Sick
```

Dependency set của A là:

[  
Dep(A)={Cool,Sick}  
]

Dependency của A được resolve hoàn toàn khi:

HasTarget(Cool,s)  
\land  
HasTarget(Sick,s)  
]

Trong đó:

|Targets_T(s)|>0  
]

Ví dụ:

```
B(Cool) → MainGrid
C(Sick) → WaitGrid
```

thì:

```
Like Cool dependency = Resolved
Like Sick dependency = Unresolved
```

và:

[  
Resolved_A(s)=false  
]

Sau khi C được đưa lên MainGrid:

[  
Resolved_A(s')=true  
]

---

# 6. Condition-local Candidate Seats

Khi đã có target trên MainGrid, ta có thể tính tập Seat thỏa một `Like Person Condition`.

Với Trait (T):

\bigcup_{p\in Targets_T(s)}  
AdjacentSeats(p)  
]

Ví dụ:

```
B(Cool) ở Seat 3
C(Cool) ở Seat 8
```

và:

```
AdjacentSeats(B) = {2, 4, 6}
AdjacentSeats(C) = {7, 9}
```

thì:

{2,4,6,7,9}  
]

Nếu A có:

```
Like Cool
Like Sick
```

thì candidate seats theo hai Condition là:

[  
V_{LikeCool}(s)  
]

và:

[  
V_{LikeSick}(s)  
]

Candidate intersection:

V_{LikeCool}(s)  
\cap  
V_{LikeSick}(s)  
]

Ví dụ:

[  
V_{LikeCool}={2,3,4}  
]

[  
V_{LikeSick}={3,4,5}  
]

thì:

[  
V_A^{constraint}={3,4}  
]

---

# 7. Static và Dynamic Constraint

Có thể chia Condition thành hai nhóm để solver xử lý dễ hơn.

|Condition|Loại dependency|
|---|---|
|`Like Food`|Static|
|`Hate Food`|Static|
|`CanSitAnywhere`|Static|
|`Like Person`|Dynamic|
|`Hate Person`|Dynamic|

Food nằm cố định trên MainGrid nên constraint liên quan Food có thể tính trực tiếp từ layout.

Ví dụ:

```
A:
- Like Cool
- Hate Hamburger
```

Giả sử:

{1,2,4,5,7}  
]

nhưng chưa có `Cool Person` trên MainGrid.

Solver có thể lưu:

```
Known Constraints
    Hate Hamburger → {1,2,4,5,7}

Pending Dependencies
    Like Cool
```

Sau khi có Cool:

[  
V_{LikeCool}={2,3,5}  
]

ta có:

V_{HateHamburger}  
\cap  
V_{LikeCool}  
]

# [

{1,2,4,5,7}  
\cap  
{2,3,5}  
]

[  
\boxed{  
V_A^{constraint}={2,5}  
}  
]

---

# 8. Ready và Deferred Person

Từ dependency metadata, solver có thể chia Person thành:

```
Ready
Deferred
```

Định nghĩa:

\bigwedge_{T\in Dep(i)}  
HasTarget(T,s)  
]

Nếu Person không có `Like Person Condition`:

[  
Ready_i(s)=true  
]

Ví dụ:

```
A Like Cool
B Trait = Cool
C Hate Hamburger
```

Initial state:

```
A → WaitGrid
B → WaitGrid
C → WaitGrid
```

ta có:

```
A = Deferred
B = Ready
C = Ready
```

Nếu B được đưa lên MainGrid:

```
s0 → s1
```

thì:

```
A:
Deferred → Ready
```

---

# 9. Ready không phải Move Restriction

Điểm quan trọng nhất của model:

> `Ready` chỉ là thông tin phục vụ planning, không phải luật cấm Move.

Không nên định nghĩa:

[  
Candidates(s)={i\mid Ready_i(s)=true}  
]

nếu solver cần tìm **mọi lời giải hợp lệ**.

Thay vào đó:

ReadyCandidates(s)  
\cup  
DeferredCandidates(s)  
]

và solver có thể ưu tiên:

[  
ReadyCandidates

>   

DeferredCandidates  
]

nhưng vẫn được phép thử Deferred Person.

Lý do là gameplay cho phép Person tồn tại trên MainGrid trong trạng thái `Angry`. MainGrid không yêu cầu Person phải `Happy` ngay khi được đặt; Person chỉ cần tất cả Condition được thỏa ở trạng thái thắng cuối cùng.