# Wanna Sit Here?

## Tổng quan về Game

**Wanna Sit Here?** là trò chơi giải đố sắp xếp chỗ ngồi, nơi mỗi vị khách mang theo những yêu cầu riêng. Một người muốn ở cạnh món ăn yêu thích, người khác không chịu được hàng xóm ồn ào. Công việc của bạn là tìm ra cách bố trí để tất cả cùng hài lòng.

Với thao tác kéo thả và đổi chỗ, người chơi từng bước đưa khách từ hàng chờ vào bàn. Câu đố nằm ở cách các yêu cầu tác động lẫn nhau: đưa một người đến đúng ghế có thể giải quyết nhiều vấn đề cùng lúc, nhưng cũng có thể làm một người đang vui trở nên khó chịu. Số lượt di chuyển có hạn khiến thứ tự sắp xếp trở thành một phần của lời giải.

Repository này là phiên bản **Refactor** của [WannaSitHere](https://github.com/gcc-dtung/WannaSitHere), được phát triển bằng Unity và C#. Phiên bản này tiếp tục phát triển ý tưởng giải đố chỗ ngồi với luật về người ngồi cạnh, món ăn và các vật phẩm hỗ trợ.

| Đặc điểm | Nội dung |
| --- | --- |
| Thể loại | Puzzle · Casual · Giải đố logic |
| Hình thức | Game 2D, chơi theo từng màn |
| Tương tác chính | Kéo thả nhân vật, di chuyển và hoán đổi chỗ ngồi |
| Thử thách | Thỏa mãn đồng thời yêu cầu của mọi người trong số lượt giới hạn |

## Một màn chơi diễn ra như thế nào?

Bạn bắt đầu bằng việc quan sát bàn ăn: những ghế nào còn trống, đồ ăn nằm ở đâu và từng vị khách muốn gì. Từ đó, bạn chọn vị trí cho những người có yêu cầu khó đáp ứng, sắp xếp các nhân vật còn lại và theo dõi phản ứng của cả bàn sau mỗi thao tác.

Vòng chơi gồm năm bước:

1. **Đọc yêu cầu:** xác định thứ nhân vật thích và muốn tránh.
2. **Chọn chỗ:** tìm ghế có hàng xóm và món ăn phù hợp.
3. **Kéo thả hoặc đổi chỗ:** thực hiện một nước đi, tiêu tốn lượt nếu vị trí thay đổi hợp lệ.
4. **Quan sát phản hồi:** kiểm tra ai đã hài lòng và ai còn điều kiện chưa đạt.
5. **Hoàn thiện cách sắp xếp:** tiếp tục điều chỉnh cho đến khi mọi người đều vui hoặc hết lượt.

## Không gian giải đố

### Bàn ăn

Bàn ăn là nơi các yêu cầu của nhân vật được đánh giá. Có ba loại ô tạo nên bố cục của mỗi màn:

| Loại ô | Vai trò trong câu đố |
| --- | --- |
| **Ghế — Seat** | Chỗ ngồi cho một nhân vật. Có thể đưa người vào ghế trống hoặc đổi chỗ với người đang ngồi. |
| **Đồ ăn — Food** | Món ăn cố định để người ngồi cạnh yêu thích hoặc tránh né. Không thể đặt người lên ô này. |
| **Ô chặn — Block** | Vị trí không thể ngồi, làm giới hạn những phương án sắp xếp. |

Đồ ăn không di chuyển theo thao tác của người chơi. Bạn phải tìm cách bố trí khách quanh những vị trí đã có, chẳng hạn dành ghế cạnh Hamburger cho người thích món này và tránh xếp người ghét Hamburger vào đó.

### Hàng chờ

Hàng chờ giữ những nhân vật chưa có chỗ ngồi hoàn chỉnh và có thể được dùng làm nơi tạm chuyển người trong quá trình sắp xếp. Các yêu cầu ngồi cạnh nhau không được xét tại đây.

Người ở hàng chờ luôn mang trạng thái **Normal**. Vì vậy, dù bố cục trên bàn đã hợp lý, màn chơi vẫn chưa hoàn thành nếu còn bất kỳ ai trong hàng chờ.

## Mỗi vị khách muốn gì?

### Đặc điểm và sở thích

Mỗi nhân vật có một đặc điểm để những người khác nhận biết khi xét người ngồi cạnh: **Cool, Sick, Dirty, Loud, Quiet hoặc Elegant**.

Đặc điểm không quyết định sở thích. Một người Quiet có thể thích Hamburger, muốn ngồi cạnh người Elegant hoặc ghét ở gần người Loud. Mỗi nhân vật có tối đa **hai điều kiện** trong một màn, và cả hai phải cùng được đáp ứng.

Điều kiện có thể nhắm đến:

- **Đồ ăn:** muốn ngồi cạnh hoặc tránh một món cụ thể.
- **Người:** muốn ngồi cạnh hoặc tránh người có một đặc điểm cụ thể.

### Thích và ghét

| Điều kiện | Khi nào được thỏa mãn? |
| --- | --- |
| **Like — Thích** | Có ít nhất một đối tượng phù hợp ở ô liền kề. |
| **Hate — Ghét** | Không có đối tượng cần tránh ở bất kỳ ô liền kề nào. |

Các ô liền kề được xét theo **trên, dưới, trái và phải** trên bàn ăn. Ô chéo góc không được tính là ngồi cạnh. Nhân vật ở hàng chờ cũng không được tính là hàng xóm của người trên bàn.

Ví dụ, một người **thích Hamburger và ghét Loud** cần một ghế cạnh Hamburger, đồng thời không có người Loud ở cả bốn phía. Đáp ứng được yêu cầu về món ăn mà vẫn ngồi cạnh người Loud thì nhân vật chưa hài lòng.

Quan hệ này không tự động có tính hai chiều. A muốn ngồi cạnh B không có nghĩa B cũng muốn ngồi cạnh A; mỗi người có bộ yêu cầu riêng cần được kiểm tra.

### Một ví dụ về quyết định đổi chỗ

Giả sử A thích Hamburger và ghét người Loud. B mang đặc điểm Loud đang ngồi ở ghế cạnh Hamburger, trong khi C mang đặc điểm Quiet đang ở một vị trí khác.

Đưa A đến gần Hamburger mới giải quyết được một phần câu đố. Bạn còn phải bố trí B sao cho không liền kề A, rồi kiểm tra xem vị trí mới có đáp ứng yêu cầu của B và C hay không. Một lần đổi chỗ tốt có thể giúp nhiều người cùng hài lòng; một lần đổi chỗ thiếu cân nhắc có thể làm mất điều kiện đã đạt trước đó.

### Chỗ nào cũng được

**Can Sit Anywhere** là điều kiện đặc biệt cho phép nhân vật hài lòng ở bất kỳ ghế nào trên bàn, không cần xét người hoặc món ăn xung quanh.

Nhân vật có điều kiện này vẫn phải được đưa ra khỏi hàng chờ. Họ cũng giữ nguyên đặc điểm của mình, nên vẫn có thể ảnh hưởng đến yêu cầu của những người ngồi cạnh.

## Di chuyển, đổi chỗ và lượt đi

Kéo nhân vật đến một ghế trống để di chuyển. Nếu ghế đích đã có người, hai nhân vật sẽ hoán đổi vị trí. Một lần đổi chỗ tiêu tốn một lượt, dù cả hai người đều thay đổi chỗ ngồi.

| Thao tác | Chi phí |
| --- | --- |
| Chuyển sang một ghế trống khác | 1 lượt |
| Đổi chỗ với nhân vật khác | 1 lượt |
| Chuyển giữa bàn ăn và hàng chờ | 1 lượt |
| Chuyển giữa hai ghế khác nhau trong hàng chờ | 1 lượt |
| Thả lại đúng vị trí ban đầu | Không mất lượt |
| Thả vào ô đồ ăn hoặc ô chặn | Không tạo nước đi hợp lệ, không mất lượt |

Giới hạn lượt khiến việc tìm được bố cục cuối cùng và tìm được đường đi đến bố cục đó đều quan trọng. Đổi chỗ đúng lúc có thể tiết kiệm thao tác; liên tục chuyển người qua hàng chờ sẽ làm giảm số lượt còn lại.

## Đọc cảm xúc để tìm lời giải

Sau mỗi nước đi thành công, game đánh giá lại toàn bộ nhân vật. Biểu cảm cho biết bố cục hiện tại đã phù hợp đến đâu:

| Trạng thái | Ý nghĩa |
| --- | --- |
| **Normal** | Nhân vật còn ở hàng chờ. |
| **Angry** | Nhân vật đã vào bàn nhưng còn ít nhất một yêu cầu chưa được đáp ứng. |
| **Happy** | Nhân vật đã vào bàn và tất cả yêu cầu đều được đáp ứng. |

Một người Happy vẫn có thể trở lại Angry nếu bạn di chuyển người hàng xóm mà họ thích hoặc đưa đến cạnh họ một người họ muốn tránh. Vì vậy, biểu cảm là phản hồi cho cách sắp xếp hiện tại, không phải dấu hiệu một nhân vật đã được giải quyết vĩnh viễn.

## Khi nào thắng hoặc thua?

**Bạn thắng khi hàng chờ không còn người và mọi nhân vật trên bàn đều Happy.** Nếu hết lượt khi chưa đạt mục tiêu này, màn chơi kết thúc với kết quả thua.

Nước đi cuối cùng vẫn có thể mang lại chiến thắng: nếu thao tác đó làm tất cả hài lòng khi số lượt vừa về 0, game tính là hoàn thành màn.

Sau chiến thắng, người chơi có thể nhận thưởng và tiếp tục màn kế tiếp. Khi thua, chơi lại là cơ hội thử một cách bố trí hoặc thứ tự di chuyển khác.

## Booster: thêm lựa chọn khi gặp khó

Game có ba booster. Để sử dụng, màn chơi phải còn đang diễn ra, còn lượt và kho phải có vật phẩm tương ứng. Một vật phẩm chỉ bị tiêu hao khi sử dụng thành công.

### More Moves — Thêm 3 lượt

Bổ sung **3 lượt di chuyển** cho màn hiện tại. Đây là lựa chọn khi bạn đã tìm ra hướng giải nhưng cần thêm thao tác để hoàn thiện bàn ăn. Cần dùng trước khi hết lượt.

### Undo — Quay lại nước đi đã ghi nhận

Khôi phục vị trí trước nước đi gần nhất trong lịch sử và trả lại **1 lượt**. Với một lần hoán đổi, cả hai người đều trở về vị trí trước đó.

Lịch sử hỗ trợ các nước đi trên bàn và giữa bàn với hàng chờ. **Di chuyển hoàn toàn trong hàng chờ không được ghi nhận**, nên Undo không hoàn tác trực tiếp thao tác này. Booster cần có nước đi trong lịch sử để sử dụng.

### Remove — Gỡ yêu cầu của một người

Thay toàn bộ điều kiện của một nhân vật bằng **Can Sit Anywhere**. Người đó vẫn ở trong màn và giữ đặc điểm của mình, nhưng không còn cần vị trí cụ thể để đáp ứng các sở thích cũ.

Game chọn ngẫu nhiên mục tiêu theo thứ tự:

1. Ưu tiên một người Angry trên bàn, còn điều kiện và chưa có Can Sit Anywhere.
2. Nếu không có mục tiêu phù hợp trên bàn, xét người còn điều kiện trong hàng chờ và chưa có Can Sit Anywhere.

Bạn không chọn mục tiêu thủ công. Nếu không có ai phù hợp, booster không được dùng. Nếu người được chọn đang ở hàng chờ, bạn vẫn phải đưa họ vào ghế trên bàn.

## Điều tạo nên thử thách

Độ phức tạp của câu đố đến từ cách các yếu tố kết hợp: số ghế có thể sử dụng, vị trí món ăn cố định, đặc điểm của khách, những cặp yêu cầu thích/ghét và số lượt được phép di chuyển.

Một vài cách tiếp cận hữu ích:

- **Xếp người ít lựa chọn trước:** người có hai yêu cầu hoặc cần một món ăn ở vị trí hiếm thường khó tìm chỗ hơn.
- **Kiểm tra cả hai người:** trước khi ghép một cặp hàng xóm, hãy đọc yêu cầu của cả hai.
- **Quan sát vùng bị ảnh hưởng:** đổi chỗ hai người còn làm thay đổi hàng xóm ở vị trí cũ và mới.
- **Tận dụng hoán đổi:** một thao tác có thể sửa được hai chỗ ngồi cùng lúc.
- **Tính trước số lượt:** hàng chờ giúp tổ chức lại bàn nhưng mỗi lần di chuyển vẫn có chi phí.

## Tiến trình và trải nghiệm

Các màn giải đố được kết nối với hệ thống **Gold, Gem, kho booster, cửa hàng và phần thưởng**. Người chơi có thể nhận thưởng hoàn thành màn và tham gia điểm danh; tiến trình cùng tài nguyên được lưu giữa các phiên chơi.

Hướng dẫn trong game giới thiệu thao tác và cơ chế. Biểu cảm, chuyển động kéo thả, âm thanh và hiệu ứng giúp người chơi nhận biết kết quả hành động. Phần cài đặt âm thanh cho phép điều chỉnh âm lượng nhạc và hiệu ứng theo sở thích.

## Reference

### Dự án gốc

[**WannaSitHere — gcc-dtung**](https://github.com/gcc-dtung/WannaSitHere) là dự án gốc mà phiên bản refactor này tiếp nối. Repository gốc có phần giới thiệu, video gameplay và thông tin đội ngũ thực hiện để tham khảo về xuất phát điểm của trò chơi.

### Tài liệu thiết kế

[**WannaSitHere — Raw Game Design Document**](WannaSitHere_RawGDD.md) mô tả các thành phần và cơ chế gameplay của dự án.

README này ưu tiên luật đã triển khai trong mã nguồn. Một số mô tả trong bản thiết kế có thể chưa đồng bộ, chẳng hạn di chuyển giữa các ghế trong hàng chờ hiện vẫn tốn một lượt.
