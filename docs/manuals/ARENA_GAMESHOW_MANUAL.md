# 📖 Cẩm Nang Sử Dụng: Đấu Trường Gameshow Trí Tuệ AegisQuiz

> **Tài liệu hướng dẫn toàn diện dành cho Giáo viên, Giảng viên, Ban Tổ Chức (Host), Trọng Tài và Học sinh - Sinh viên (Thí sinh)** khi tham gia tổ chức hoặc thi đấu trên nền tảng AegisQuiz Arena.

---

## 🎯 1. Giới Thiệu Chung & Mục Đích Sử Dụng

**AegisQuiz Arena** là phân hệ thi đấu đối kháng trực tiếp, biến các buổi học, kỳ thi phong trào, hội trại thanh niên hoặc ngày hội tri thức của nhà trường và doanh nghiệp thành các **Gameshow truyền hình chuyên nghiệp** với hiệu ứng âm thanh, đồ họa và cơ chế bấm chuông phản xạ chuẩn xác từng mili-giây.

### Cách Truy Cập Đấu Trường:
1. Đăng nhập vào hệ sinh thái **AegisQuiz**.
2. Trên thanh điều hướng Header, nhấp vào mục **"Đấu trường ⚡"** (hoặc truy cập trực tiếp URL: `http://<domain>/arena`).
3. Bạn sẽ bước vào **Sảnh Đấu Trường (Gameshow Hub)** nơi trưng bày đầy đủ 6 thể thức thi đấu.

---

## 👨‍🏫 2. Hướng Dẫn Dành Cho Ban Tổ Chức (Host / Trọng Tài)

### 2.1. Khởi Tạo Phòng Thi Đấu Mới
1. Tại Sảnh Đấu Trường, duyệt qua danh sách các Gameshow và chọn thể thức mong muốn (ví dụ: *Đường Lên Đỉnh Olympia* hoặc *Rung Chuông Vàng*).
2. Nhập **Tên phòng thi đấu** (ví dụ: *"Chung Kết Rung Chuông Vàng Khối 12"* hoặc *"Tranh Hùng Olympia Chi Đoàn 10A1"*).
3. Nhấp nút **"Tạo Phòng & Vào Thi Đấu ➔"**.
4. Hệ thống sẽ cấp ngay một mã phòng duy nhất gồm 6 ký tự (ví dụ: `UKJMJI`).

### 2.2. Mời Thí Sinh Tham Gia
- **Cách 1**: Gửi trực tiếp đường dẫn phòng: `http://<domain>/arena/<GAME_CODE>/<ROOM_ID>`.
- **Cách 2**: Đọc to Mã Phòng (`Room ID`) để thí sinh nhập tại ô tìm phòng ở Sảnh Đấu Trường.
- Số lượng thí sinh tham gia cùng lúc:
  - Olympia: 4 thí sinh chính thức + khán giả cổ vũ.
  - University Challenge: 8 thí sinh (chia 2 đội A và B, mỗi đội 4 người).
  - Rung Chuông Vàng: Lên tới 100 thí sinh trực tiếp trên sàn đấu.
  - Chiếc Nón Kỳ Diệu: 3 - 6 người chơi.

### 2.3. Bảng Điều Khiển Của Trọng Tài (Stage Controller)
Ở phía trên cùng màn hình phòng đấu, Host có thanh điều phối tiến trình:
- **Chuyển vòng thi**: Ví dụ ở Olympia, bấm nút `Sang Vòng 2: Vượt CNV ➔`, `Sang Vòng 3: Tăng Tốc ➔`, `Sang Vòng 4: Về Đích ➔`.
- **Kích hoạt sự kiện**: Bấm kích hoạt `Thầy Cô Cứu Trợ 🎈` (Rung Chuông Vàng) hoặc mở quyền đoán ô chữ chướng ngại vật.

---

## 🎮 3. Hướng Dẫn Chi Tiết Luật Chơi & Chiến Thuật 6 Gameshow

---

### 🏛️ 3.1. Đường Lên Đỉnh Olympia (Olympia Supreme)

Mô phỏng chuẩn xác 100% format 4 vòng thi của chương trình truyền hình huyền thoại:

```
[Vòng 1: Khởi Động] ➔ [Vòng 2: Vượt Chướng Ngại Vật] ➔ [Vòng 3: Tăng Tốc] ➔ [Vòng 4: Về Đích]
```

#### Vòng 1: Khởi Động (Speed Sprint)
- **Thời gian**: 60 giây dồn dập.
- **Cách chơi**: Thí sinh trả lời liên tục các câu hỏi trắc nghiệm hoặc nhập đáp án trực tiếp. Mỗi câu đúng được **+10 điểm**, không bị trừ điểm khi trả lời sai.

#### Vòng 2: Vượt Chướng Ngại Vật (CNV Matrix)
- **Cấu trúc**: Gồm 4 hàng ngang tương ứng với 4 mảnh ghép và 1 hình ảnh/ẩn số trung tâm.
- **Luật chơi**: Trả lời đúng mỗi câu hàng ngang được **+10 điểm** và mở ra một góc hình ảnh gợi ý.
- **Bấm chuông giải Chướng Ngại Vật**:
  - Bất kỳ lúc nào, thí sinh có thể bấm nút **"🚩 BẤM CHUÔNG TRẢ LỜI CHƯỚNG NGẠI VẬT"**.
  - **Đoán đúng**: Nhận ngay **80 điểm** (nếu bấm khi chưa mở hết hàng ngang) hoặc **40 điểm**.
  - **Đoán sai**: Thí sinh lập tức bị **MẤT QUYỀN CHƠI** toàn bộ phần còn lại của Vòng 2.

#### Vòng 3: Tăng Tốc (Millisecond Reflex)
- **Cơ chế tính điểm theo tốc độ phản xạ**:
  - Người trả lời đúng **nhanh nhất**: **+40 điểm**.
  - Người trả lời đúng **thứ hai**: **+30 điểm**.
  - Người trả lời đúng **thứ ba**: **+20 điểm**.
  - Người trả lời đúng **thứ tư**: **+10 điểm**.

#### Vòng 4: Về Đích & Ngôi Sao Hy Vọng (The Final Climb)
- Thí sinh được chọn các gói câu hỏi (20 điểm, 30 điểm).
- **Ngôi sao hy vọng (⭐)**: Thí sinh bấm nút đặt Ngôi sao hy vọng trước khi đọc câu hỏi:
  - Trả lời đúng: **Nhân đôi số điểm câu hỏi (+40đ hoặc +60đ)**.
  - Trả lời sai: **Bị trừ 50% số điểm của câu hỏi** và nhường quyền cho các bạn khác bấm chuông cướp điểm!
- **Bấm chuông giành quyền trả lời (Buzzer)**: Khi thí sinh chính trả lời sai, các thí sinh còn lại bấm chuông đỏ: trả lời đúng lấy điểm của bạn, trả lời sai bị trừ 50% số điểm câu đó.

---

### 🔔 3.2. Rung Chuông Vàng (Golden Bell Mega-Grid)

Đấu trường sinh tồn học đường với quy mô ma trận lên đến 100 thí sinh:

1. **Sàn đấu 100 ghế số**: Mỗi học sinh được cấp 1 vị trí ghế số trên sàn đấu ma trận.
2. **Loại trực tiếp (Sudden Death)**: Mỗi câu hỏi có 15 giây suy nghĩ. Sau khi hết giờ, thí sinh nào có đáp án sai lập tức phải rời sàn đấu (ô ghế chuyển sang màu đỏ mờ).
3. **Bảng Mica điện tử**: Thí sinh nhập câu trả lời vào ô bảng viết điện tử và bấm nút **"GIƠ BẢNG NỘP ĐÁP ÁN"**.
4. **Minigame Thầy Cô Cứu Trợ (Teacher Rescue Mission)**:
   - Khi số lượng thí sinh trên sàn đấu còn lại quá ít (dưới 10 người), Ban Tổ chức bấm nút **"Thầy Cô Cứu Trợ 🎈"**.
   - Các thầy cô tham gia trò chơi vận động (ném bóng vào rổ, chuyền bóng bằng thìa...). Số bóng trúng đích tương ứng số lượng học sinh được "hồi sinh" trở lại sàn đấu.
5. **Rung Chuông Vàng**: Thí sinh duy nhất trụ lại đến câu số 20/50 sẽ bước lên đỉnh vinh quang rung chiếc chuông vàng danh giá.

---

### 🎡 3.3. Chiếc Nón Kỳ Diệu (Quantum Lucky Wheel)

Gameshow giải trí và ngôn ngữ kinh điển với vòng quay vật lý 60fps chân thực:

1. **Quay Nón Vật Lý**: Người chơi đến lượt bấm nút **"🎡 QUAY NÓN (LỰC VẬT LÝ)"**. Bánh xe Canvas sẽ quay và giảm tốc dần theo quán tính cơ học, kim chỉ dừng ở ô nào thì người chơi nhận cơ hội tương ứng:
   - Ô điểm: `100`, `200`, `300`, `400`, `500`, `800`, `1000`.
   - `NHÂN ĐÔI`: Nhân 2 toàn bộ số điểm đang có nếu đoán đúng chữ cái.
   - `CHIA ĐÔI`: Giảm 50% số điểm hiện tại.
   - `MẤT LƯỢT`: Mất quyền chơi lượt hiện tại, chuyển sang người kế tiếp.
   - `MAY MẮN`: Được lật mở miễn phí 1 chữ cái tùy chọn.
2. **Đoán Chữ Cái**: Chọn chữ cái từ bảng ký tự tiếng Việt A-Z. Nếu có chữ cái trong từ khóa, các ô chữ 3D sẽ lật mở và người chơi được cộng điểm = (Điểm ô nón) x (Số lượng chữ cái xuất hiện).
3. **Đoán Toàn Bộ Ô Chữ**: Nếu đã suy luận ra toàn bộ câu châm ngôn, bấm **"⭐ ĐOÁN TOÀN BỘ Ô CHỮ"** để giành chiến thắng chung cuộc kèm **+2,000 điểm** thưởng.

---

### 🎓 3.4. University Challenge (Đại Học Đỉnh Cao)

Đấu trường đỉnh cao mô phỏng cuộc đối đầu giữa hai trường đại học danh tiếng (ví dụ: ĐH Bách Khoa vs ĐH Ngoại Thương):

1. **Cơ cấu đội tuyển**: Mỗi trường cử ra 1 đội tuyển gồm **4 thành viên** ngồi đối diện nhau.
2. **Câu hỏi Khởi động cá nhân (Starter Question - 10 điểm)**:
   - MC đọc câu hỏi học thuật quốc tế chuyên sâu.
   - **Tuyệt đối không được trao đổi nhóm**.
   - Thí sinh nào biết đáp án bấm **"🔔 STARTER BUZZER"** để giành quyền trả lời.
   - *Lưu ý*: Bấm chuông ngắt lời MC mà trả lời sai sẽ bị **phạt trừ 5 điểm (-5 pts)**.
3. **Bộ 3 câu hỏi Đội nhóm (Bonus Set - 15 điểm mỗi câu)**:
   - Đội nào giành chiến thắng ở câu Starter sẽ được nhận bộ 3 câu hỏi chuyên sâu Bonus.
   - Lúc này micro của đội được mở trong **15 giây** để 4 thành viên cùng thảo luận và Đội trưởng đưa ra câu trả lời chính thức.

---

### 🇺🇸 3.5. Jeopardy! American Matrix

Format truyền hình trí tuệ ăn khách bậc nhất nước Mỹ với bảng điểm ma trận:

1. **Ma trận 30 ô điểm**: Gồm 6 lĩnh vực tri thức (Lịch sử, Khoa học, Toán học, Văn học, Công nghệ, Địa lý) và 5 mức tiền thưởng ($200, $400, $600, $800, $1000).
2. **Chọn ô câu hỏi**: Thí sinh được quyền chọn chủ đề và độ khó tùy ý (ví dụ: *"Khoa học 600 đô"*).
3. **Quy tắc Câu Hỏi Ngược (Reverse Question Rule)**:
   - Hệ thống đưa ra dữ kiện dạng câu khẳng định: *"Nhà bác học đã tìm ra Định luật Vạn vật Hấp dẫn khi quan sát một quả táo rơi."*
   - Thí sinh bắt buộc phải trả lời dưới dạng **CÂU HỎI**:
     - ✅ Đúng: *"Ai là Isaac Newton?"* hoặc *"Là Isaac Newton?"*
     - ❌ Sai: Chỉ nói *"Isaac Newton"* (không đúng format Jeopardy).

---

### ⚡ 3.6. Nhanh Như Chớp (Lightning Incline)

Gameshow thử thách tốc độ phản xạ và sự tỉnh táo trước các câu đố mẹo hóc búa:

1. **Cỗ máy leo dốc 10 bậc đứng**: Người chơi ngồi trên cỗ máy trượt dốc nghiêng với 10 nấc thang phát sáng.
2. **Thời gian**: 120 giây (2 phút) đếm ngược.
3. **Quy tắc Sinh tử**:
   - Trả lời đúng: Cỗ máy nâng người chơi **lên +1 bậc**.
   - **Chỉ cần trả lời sai hoặc nói "Bỏ qua"**: Cỗ máy lập tức **tụt dốc trượt thẳng về vạch số 0**!
4. **Mục tiêu**: Phải giữ vững chuỗi tâm lý bình tĩnh để trả lời đúng **10 câu liên tiếp** để chạm tới đỉnh vinh quang.

---

## 📺 4. Kinh Nghiệm Trình Chiếu Trên Hội Trường & Máy Chiếu

1. **Toàn màn hình (Full Screen)**: Nhấn phím `F11` trên bàn phím để chuyển trình duyệt sang chế độ toàn màn hình, ẩn thanh bookmark và taskbar giúp không gian thi đấu trông hệt như trường quay truyền hình thực tế.
2. **Hệ thống âm thanh**: Kết nối máy tính trình chiếu với loa hội trường. Âm thanh còi chuông cướp điểm, tiếng nan nón quay tạch tạch và âm thanh tụt dốc của hệ thống sẽ tạo nên bầu không khí vô cùng sôi động.
3. **Đường truyền mạng**: Thí sinh thi đấu trực tiếp nên sử dụng mạng Wi-Fi riêng hoặc 4G/5G để đảm bảo độ trễ bấm chuông luôn dưới 50 mili-giây.

---

## 🎨 5. Hướng Dẫn Sử Dụng Arena Studio: Bộ Công Cụ Sáng Tạo Gameshow No-Code (Figma / Canva của Đấu Trường Trí Tuệ)

Nếu bạn là Giáo viên, Bí thư Đoàn trường, Trưởng phòng Đào tạo hoặc Host sự kiện muốn tạo ra một Gameshow độc nhất vô nhị mang bản sắc riêng mà **không cần viết bất kỳ dòng code nào**:

### 5.1. Truy Cập Arena Studio
1. Tại Sảnh Đấu Trường (`/arena`), bấm vào nút **"🎨 TỰ TẠO GAMESHOW (ARENA STUDIO)"** hoặc truy cập trực tiếp đường dẫn `/arena/studio`.
2. Giao diện thiết kế phong cách Figma / Canva sẽ mở ra với 2 khu vực:
   - **Cột Trái (Toolbox)**: Toàn bộ bảng điều khiển cấu hình luật chơi, giao diện, phao cứu trợ.
   - **Cột Phải (Live Simulator Canvas)**: Mô phỏng sàn đấu thời gian thực. Bất kỳ thay đổi nào bên trái đều lập tức cập nhật lên sàn đấu bên phải.

### 5.2. Nạp Nhanh Các Mẫu Bản Quyền (1-Click Presets)
Trên thanh công cụ phía trên, bạn có thể nạp ngay các mẫu kinh điển:
- 🏆 **Ai Là Triệu Phú (Millionaire)**: Thang leo dốc 15 câu hỏi, 3 mốc an toàn, 4 phao cứu sinh (50:50, AI Socrates, Khán giả, Đổi câu).
- 🔥 **Kẻ Săn Mồi (The Chase)**: Đấu trí rượt đuổi nghẹt thở với Kẻ Săn Mồi AI trên thang dốc thời gian.
- 🧩 **Đuổi Hình Bắt Chữ (Visual Charades)**: Lật mở từng mảnh ghép để đoán từ ngữ ẩn sau bức tranh.
- 👥 **Đấu Trí 1 vs 100 (The Mob)**: 1 thí sinh trung tâm đối đầu sàn đấu 100 người loại trực tiếp.

### 5.3. Tùy Biến 5 Khối Nguyên Tử Trò Chơi
1. **Bố Cục Sân Khấu (Layout Shell)**: Chọn giữa *Thang Leo Dốc*, *Bục Thí Sinh 3D*, *Ma Trận Ghế Số*, *Bánh Xe Quay*, *Lật Mảnh Ghép*, hoặc *Ma Trận Chủ Đề*.
2. **Cơ Chế Tranh Lượt (Contention Mode)**: *Bấm Chuông Microsecond* (ai nhanh được nói), *Đồng Loạt Cùng Làm*, hoặc *Lần Lượt Theo Lượt*.
3. **Luật Điểm & Sinh Tử**: Bật tùy chọn *Tụt Dốc Về 0 Khi Sai*, *Đột Tử (Sai 1 câu rời sàn)*, hoặc *Cộng Thưởng Tốc Độ*.
4. **Phao Cứu Sinh (Lifelines)**: Tích chọn bật/tắt các quyền trợ giúp như 50:50, Gia sư AI Socrates, Thăm dò khán giả, Thầy cô ném bóng cứu trợ.

### 5.4. Xuất Bản & Khởi Chạy
1. Bấm nút **"XUẤT BẢN GAMESHOW ➔"** ở góc phải màn hình.
2. Hệ thống sẽ lưu trữ và hiển thị Gameshow mới ngay lập tức trên Sảnh Đấu Trường (`/arena`) trong danh mục **"Đấu Trường Tự Thiết Kế"**.
3. Bạn có thể bấm **"VÀO ĐẤU TRƯỜNG ➔"** để tạo phòng thi và mời thí sinh tham gia tranh tài ngay lập tức!

