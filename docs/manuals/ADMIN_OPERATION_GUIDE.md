# 💼 Cẩm Nang Vận Hành & Quản Trị Hệ Thống (Admin Operations Guide)

Tài liệu này dành cho **Quản trị viên hệ thống (Admins), Trưởng bộ môn khảo thí và Cán bộ quản lý đào tạo** sử dụng các tính năng tại phân hệ Quản Trị (`/admin`).

---

## 1. Truy Cập Khu Vực Quản Trị (`/admin`)

- Tài khoản phải có quyền `role: "Admin"` hoặc `role: "Examiner"`.
- Nếu tài khoản thường cố gắng truy cập, `ProtectedRoute` sẽ tự động chuyển hướng về trang đăng nhập hoặc hiển thị thông báo từ chối truy cập.

---

## 2. Quản Lý Ngân Hàng Câu Hỏi (`/admin/questions`)

### 2.1. Thêm Mới Câu Hỏi Thủ Công
- Nhập nội dung câu hỏi (hỗ trợ trình soạn thảo công thức LaTeX và Markdown).
- Nhập 4 phương án A, B, C, D và chọn đáp án chính xác.
- Thiết lập các tham số IRT: Độ khó $b$ (-3.0 đến +3.0), Độ phân biệt $a$, Độ đoán mò $c$.

### 2.2. Nhập Đề Thi Hàng Loạt Bằng File Word (.docx)
- Nhấp nút **"Import từ file Word (.docx)"**.
- Chọn file đề thi (ví dụ: `DeToan.docx` hoặc `CauhoiDocx.docx`).
- Hệ thống tự động phân tích:
  - Tách từng câu hỏi dựa vào cấu trúc `Câu 1:`, `Câu 2:`.
  - Giải mã ảnh công thức WMF/EMF thành PNG sắc nét.
  - Chuyển đổi công thức toán OMML sang chuẩn LaTeX.
- Quản trị viên xem trước bảng kết quả bóc tách, chỉnh sửa nếu cần và nhấp **"Lưu vào Ngân hàng Câu hỏi"**.

---

## 3. Quản Lý Kỳ Thi & Đề Thi (`/admin/exams`)

- **Tạo kỳ thi mới**: Đặt tên kỳ thi, thời lượng làm bài (phút), số lượng câu hỏi, điểm đạt (%).
- **Cấu hình an ninh**:
  - `Bật Chế Độ Phòng Thi Bảo Mật (Secure Mode)`: Buộc thí sinh phải dùng webcam, chặn chuyển tab và khóa toàn màn hình.
  - `Trộn thứ tự câu hỏi và phương án`: Mỗi thí sinh sẽ nhận một mã đề hoán vị ngẫu nhiên.

---

## 4. Quản Lý Doanh Thu, Quỹ Tài Trợ & Tri Ân (`/admin/payments`)

Hệ thống điều phối tài chính đa kênh toàn cầu được tổ chức thành 3 phân hệ trực quan:

### 4.1. Tab 1: Báo Cáo Doanh Thu (Revenue Analytics)
- Theo dõi tổng doanh thu thời gian thực từ các gói thuê bao (Pro, Enterprise, Trường học).
- Phân tích tỷ trọng dòng tiền theo cổng thanh toán: **PayOS (VietQR nội địa)**, **Stripe (Thẻ quốc tế)**, **Alipay/WeChat (Đông Bắc Á)** và **Polygon Web3 (USDT/USDC)**.
- Tra soát mã đơn hàng (`OrderCode`), mã định danh thanh toán và trạng thái thành công/chờ xử lý.

### 4.2. Tab 2: Nhà Tài Trợ & Tri Ân (Community Donors & Gratitude CRM)
- **Thống kê Quỹ Mời Cafe**: Tổng số tiền ủng hộ tự nguyện và tổng lượt mời cafe từ cộng đồng.
- **Bảng Vinh Danh Nhà Hảo Tâm**: Hiển thị Mã đơn, Tên người ủng hộ, Email, Số tiền, Danh hiệu danh dự (`☕ Người Gieo Mầm Tri Thức`, `🍰 Người Tiếp Lửa Thầm Lặng`...) và Lời chúc gửi đến đội ngũ.
- **Phát Hành Mã Tri Ân (Voucher)**: Quản lý mã quà tặng `AEGIS-THANKS-xxxx` được tự động cấp cho người đóng góp.
- **1-Click Sao Chép Email**: Nhấp nút **"📋 Sao chép email gửi Tri ân"** để trích xuất nhanh toàn bộ danh bạ email nhà hảo tâm, phục vụ các chiến dịch gửi thư cảm ơn, tri ân ngày lễ hoặc tặng voucher khóa học độc quyền.

### 4.3. Tab 3: Cấu Hình Tài Khoản Thụ Hưởng (Gateway Configuration)
- Cấu hình thông tin thụ hưởng trực tiếp vào CSDL PostgreSQL (Zero Hardcode):
  - **Ngân hàng Napas VietQR**: Mã BIN (VD: `970405` - Agribank), Tên ngân hàng, Số tài khoản (`8888912600688`), Tên chủ tài khoản (`NGUYEN VIET CUONG`).
  - **Cổng PayOS**: Client ID, API Key, Checksum Key.
  - **Cổng Quốc tế & Web3**: Stripe Secret Key, Alipay Partner ID, Địa chỉ Ví Web3 Polygon.

---

## 5. Quản Trị Phân Tầng Tổ Chức & Danh Mục Ngành Động (Kỳ Quan 16)

Hệ thống cung cấp trung tâm điều phối quản trị thích ứng đa quy mô thông qua nút **"🏛️ Phân Tầng & Ngành Động"** trực tiếp tại trang Quản lý Ngân hàng Câu hỏi (`/admin/questions`).

### 5.1. Tab 1: Quản Trị Cây Phân Cấp Đơn Vị (5-Tier Hierarchy Tree)
- **Mục đích**: Phục vụ phân vùng ngân hàng đề thi và phân tầng quản trị từ Hội sở xuống Chi nhánh.
- **Thao tác**:
  - Nhấp **"Thêm Đơn Vị Gốc (HQ)"** để tạo nút gốc (ví dụ: `HO` - Hội sở chính Agribank).
  - Tại từng đơn vị, nhấp **"Thêm con"** để phân nhánh cấp dưới:
    - *Cấp 1: Hội sở chính / Ban Lãnh đạo (HeadOffice)*.
    - *Cấp 2: Khu vực / Phân hiệu / Vùng (Region)*.
    - *Cấp 3: Chi nhánh / Trường thành viên (Branch)*.
    - *Cấp 4: Phòng ban / Khoa chuyên môn (Department)*.
    - *Cấp 5: Lớp học / Tổ nhóm nghiệp vụ (Classroom)*.
  - Hệ thống tự động tính đường dẫn phả hệ `HierarchyPath` để tối ưu truy vấn đệ quy tức thì.

### 5.2. Tab 2: Quản Trị Danh Mục Ngành Động (Dynamic Domain Taxonomy)
- **Mục đích**: Cho phép từng Tenant chỉ hiển thị những ngành nghề mình hoạt động, tránh làm rối mắt người dùng.
- **Thao tác**:
  - Nhấp nút **ĐANG BẬT / TẮT** trên từng thẻ ngành để kích hoạt hoặc ẩn ngành đó khỏi hệ thống.
  - Đổi tên hiển thị riêng theo thuật ngữ nội bộ (ví dụ: đổi `BANKING` thành `Nghiệp vụ Agribank`).
  - Nhấp **"Tạo Ngành Đặc Thù Mới"** để bổ sung lĩnh vực độc quyền của tổ chức (ví dụ: `AGRI_SPECIAL` - Tín dụng Nông nghiệp Nông thôn).
  - Nhấp **"Lưu Thay Đổi"** để áp dụng ngay lập tức cho toàn bộ giao diện làm bài thi và bộ lọc.

### 5.3. Tab 3: Bộ Tọa Độ Tri Thức & Từ Điển Nhận Diện AI
- **Mục đích**: Dạy cho AI nhận diện tự động chức danh, cấp bậc và tiêu chuẩn khi nhập file Word/Excel.
- **Thao tác**:
  - Chọn Lĩnh vực (Ngành) và Trục tọa độ cần cấu hình (`TARGET_LEVEL`, `ISSUING_ORG`, `BENCHMARK_STANDARD`, `ASSESSMENT_PURPOSE`).
  - Nhập Tên tọa độ (ví dụ: `Tín dụng Khách hàng Doanh nghiệp`).
  - Nhập danh sách các từ đồng nghĩa / viết tắt cách nhau bởi dấu phẩy (ví dụ: `KHDN, TDDN, Corporate Credit`).
  - Nhấp **"Thêm Tọa Độ Mới"**. Kể từ thời điểm này, khi upload file có chứa các từ khóa trên, AI sẽ tự động gán đúng tọa độ tri thức mà không cần người dùng chọn tay!
