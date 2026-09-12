# Hướng Dẫn Triển Khai Go-Live AegisQuiz (Dehoc.vn) Trên Coolify VPS

Tài liệu này hướng dẫn chi tiết từng bước triển khai hệ thống **AegisQuiz** lên VPS thông qua giao diện quản trị **Coolify (v4.3.19)** tại địa chỉ `https://panel.dehoc.vn/projects`.

---

## 1. Chuẩn Bị Tên Miền `dehoc.vn`

Trước khi bắt đầu, hãy đảm bảo các bản ghi DNS tại nhà cung cấp tên miền của bạn (PA Vietnam, Viettel, Mắt Bão, Cloudflare...) đã trỏ về IP của VPS:

| Loại Bản Ghi (Type) | Tên (Host / Name) | Giá Trị (Value / IP Address) |
|---|---|---|
| **A** | `@` (hoặc `dehoc.vn`) | `[IP_VPS_CỦA_BẠN]` |
| **A** | `www` | `[IP_VPS_CỦA_BẠN]` |
| **A** | `panel` *(đã có)* | `[IP_VPS_CỦA_BẠN]` |

> *Kiểm tra DNS*: Mở terminal máy tính chạy: `ping dehoc.vn` -> nếu ra đúng IP máy chủ là DNS đã sẵn sàng.

---

## 2. Cách Triển Khai Trên Giao Diện Coolify (`panel.dehoc.vn`)

Có 2 cách triển khai siêu nhanh trên Coolify:

### Cách 1: Triển khai qua Docker Compose Trực Tiếp (Khuyên Dùng — 3 Phút)

1. **Bước 1: Tạo Resource Mới Trong Project**:
   - Truy cập: `https://panel.dehoc.vn/projects`.
   - Bấm vào project hiện có (ví dụ: `My first project`) hoặc bấm **`+ New Project`** đặt tên `AegisQuiz`.
   - Chọn môi trường: `production`.
   - Bấm nút **`+ New`** (hoặc `Add Resource`) -> Chọn **`Docker Compose`**.

2. **Bước 2: Cấu Hình Docker Compose**:
   - Tại ô nhập cấu hình Docker Compose, copy toàn bộ nội dung từ tệp [`docker-compose.prod.yml`](../docker-compose.prod.yml) trong mã nguồn và dán vào.
   - Bấm **Save**.

3. **Bước 3: Cấu Hình Tên Miền (Domains)**:
   - Trong danh sách các Services bên trong Compose:
     - Chọn service **`frontend`** -> tại mục **Domains**: nhập `https://dehoc.vn, https://www.dehoc.vn`.
     *(Coolify sẽ tự động cấu hình Traefik Reverse Proxy và tự động cấp chứng chỉ SSL HTTPS Let's Encrypt hoàn toàn miễn phí!)*
     - Cổng dịch vụ (Port): nhập `80` (vì `frontend` container chạy Nginx alpine trên cổng 80).
   - Bấm **Save**.

4. **Bước 4: Bấm Deploy**:
   - Bấm nút **`Deploy`** màu tím ở góc trên bên phải.
   - Coolify sẽ tự động:
     1. Build Frontend container (Node 22 -> Nginx alpine).
     2. Build Backend container (.NET 9 Web API Release).
     3. Khởi chạy PostgreSQL 17, Redis 7, IRT Engine, Keycloak.
     4. Kích hoạt chứng chỉ SSL HTTPS `https://dehoc.vn`.

---

### Cách 2: Triển khai Liên kết Qua Git Repository (Tự động CI/CD)

Nếu mã nguồn dự án của bạn đã được push lên GitHub hoặc GitLab:

1. Tại Coolify `panel.dehoc.vn/projects` -> Bấm **`+ New`** -> Chọn **`Git Repository`** (GitHub / Private Repository).
2. Chọn repository `AegisQuiz` và branch `main`.
3. Chọn kiểu build: **`Docker Compose`** và chỉ định tệp `docker-compose.prod.yml`.
4. Điền domain: `https://dehoc.vn`.
5. Bấm **Deploy**. Mỗi khi bạn `git push` code mới, Coolify sẽ tự động build và cập nhật phiên bản mới nhất mà không gián đoạn dịch vụ!

---

## 3. Kiểm Tra Hệ Thống Sau Khi Go-Live

Sau khi Coolify báo trạng thái xanh lá cây `Healthy` / `Running`:

1. **Kiểm tra Trang Chủ & Giao Diện Khảo Thí**:
   - Truy cập: `https://dehoc.vn` -> Kiểm tra ổ khóa SSL bảo mật màu xanh.
   - Truy cập: `https://dehoc.vn/practice` -> Kiểm tra thanh điều khiển thông minh **Smart LaunchPad** và các chế độ thi.
2. **Kiểm tra Không Gian Đội Nhóm (Team Workspace)**:
   - Bấm mục menu **`Đội nhóm 👥`** trên thanh Header (hoặc vào `https://dehoc.vn/team`).
   - Kiểm tra giao diện Quản lý ngân hàng câu hỏi của Team (`/admin/questions?scope=TEAM`) và nút Luyện thi cùng Team.
3. **Kiểm tra Nạp Câu Hỏi Bằng AI (UCIS v3.0)**:
   - Tải thử file Word/Excel vào ngân hàng câu hỏi, kiểm tra hệ thống tự động sinh mã chủ đề SSAE ngắn gọn và lưu trữ thành công.
