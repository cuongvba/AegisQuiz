# Hướng Dẫn Triển Khai Go-Live AegisQuiz (Dehoc.vn) Trên Coolify VPS

Tài liệu này hướng dẫn chi tiết từng bước triển khai hệ thống **AegisQuiz** lên VPS thông qua giao diện quản trị **Coolify (v4.3.19)** tại địa chỉ `https://panel.dehoc.vn/projects`.

---

## 1. Chuẩn Bị Tên Miền Hệ Sinh Thái `dehoc.vn` & Phân Hệ `daotao.dehoc.vn`

Hệ sinh thái `dehoc.vn` được thiết kế chuyên biệt hóa:
- **`dehoc.vn` / `www.dehoc.vn`**: Cổng thông tin trung tâm / Landing Page / Hệ sinh thái chung.
- **`daotao.dehoc.vn`**: Toàn bộ Nền tảng Khảo thí & Đào tạo AegisQuiz (Frontend SPA + Backend APIs + Realtime Hubs).
- **Khi đăng nhập / đăng xuất**: Hệ thống tự động chuyển hướng về `https://daotao.dehoc.vn` (hoặc `https://daotao.dehoc.vn/login`).

Đảm bảo các bản ghi DNS tại nhà cung cấp tên miền của bạn (PA Vietnam, Viettel, Cloudflare...) đã trỏ về IP của VPS:

| Loại Bản Ghi (Type) | Tên (Host / Name) | Giá Trị (Value / IP Address) | Mục Đích |
|---|---|---|---|
| **A** | `daotao` | `[IP_VPS_CỦA_BẠN]` | Phân hệ Khảo thí & Đào tạo AegisQuiz |
| **A** | `@` (hoặc `dehoc.vn`) | `[IP_VPS_CỦA_BẠN]` | Cổng trung tâm hệ sinh thái Dehoc |
| **A** | `panel` *(đã có)* | `[IP_VPS_CỦA_BẠN]` | Trang quản trị Coolify |

> *Kiểm tra DNS*: Mở terminal chạy: `ping daotao.dehoc.vn` -> nếu ra đúng IP máy chủ VPS là DNS đã sẵn sàng.

---

## 2. Cách Triển Khai Trên Giao Diện Coolify (`panel.dehoc.vn`)

### Cách 1: Triển khai qua Docker Compose Trực Tiếp

1. **Bước 1: Tạo Resource Mới Trong Project**:
   - Truy cập: `https://panel.dehoc.vn/projects`.
   - Chọn project `AegisQuiz` (môi trường: `production`).
   - Chọn **`Docker Compose`**.

2. **Bước 2: Cấu Hình Docker Compose**:
   - Copy toàn bộ nội dung từ [`docker-compose.prod.yml`](../docker-compose.prod.yml) và dán vào ô Compose. Bấm **Save**.

3. **Bước 3: Cấu Hình Tên Miền Cho Frontend**:
   - Trong danh sách các Services bên trong Compose:
     - Chọn service **`frontend`** -> tại mục **Domains**: nhập **`https://daotao.dehoc.vn`**.
     - Cổng dịch vụ (Port): nhập `80` (vì container `frontend` chạy Nginx alpine trên cổng 80).
   - Bấm **Save**.

4. **Bước 4: Bấm Deploy**:
   - Bấm nút **`Deploy`** màu tím.
   - Coolify sẽ cấp phát SSL Let's Encrypt tự động cho `https://daotao.dehoc.vn`.

---

### Cách 2: Triển khai Liên kết Qua Git Repository (Tự động CI/CD) — KHUYÊN DÙNG

Phương án này giúp bạn kết nối trực tiếp Coolify với GitHub. Mỗi khi bạn cập nhật code và `git push`, Coolify sẽ tự động build lại và cập nhật hệ thống `dehoc.vn` mà không cần thao tác thủ công.

#### Bước 2.1: Đẩy mã nguồn lên GitHub (`cuongvba/AegisQuiz`)

Kho lưu trữ cục bộ tại máy của bạn đã được khởi tạo và commit đầy đủ mã nguồn (`Initial commit`). Bạn chỉ cần xuất bản lên GitHub theo 1 trong 2 cách cực nhanh sau:

* **Lựa chọn A (1-Click qua GitHub Desktop - Siêu tiện lợi)**:
  1. Mở ứng dụng **GitHub Desktop** trên máy của bạn.
  2. Chọn menu **File** -> **Add Local Repository...** (hoặc phím tắt `Ctrl + O`).
  3. Chọn thư mục: `D:\Cuong\DuAn\mybank\AegisQuiz`.
  4. Bấm nút **`Publish repository`** ở thanh trên cùng.
  5. Đặt tên: `AegisQuiz`, tích chọn `Keep this code private` (để bảo mật mã nguồn doanh nghiệp).
  6. Bấm nút xanh **`Publish Repository`** -> Toàn bộ mã nguồn sẽ được đồng bộ lên `https://github.com/cuongvba/AegisQuiz`.

* **Lựa chọn B (Qua trình duyệt Web & dòng lệnh Git)**:
  1. Truy cập [https://github.com/new](https://github.com/new).
  2. Repository name: `AegisQuiz`.
  3. Chọn: **Private**.
  4. *Không* tích chọn bất kỳ mục nào khác (README, .gitignore - vì máy bạn đã có sẵn).
  5. Bấm **Create repository**.
  6. Mở PowerShell và chạy lệnh sau để đẩy code:
     ```powershell
     & "C:\Users\cuongnguyenviet8.CORP\AppData\Local\GitHubDesktop\app-3.6.5\resources\app\git\cmd\git.exe" push -u origin main
     ```

---

#### Bước 2.2: Kết nối Git Repository trên Coolify (`panel.dehoc.vn`)

1. **Ủy quyền Coolify truy cập GitHub**:
   - Tại Coolify (`https://panel.dehoc.vn`), vào menu **Sources** (hoặc **Keys & Tokens** -> **Git Sources**).
   - Chọn **Add GitHub App**: Bấm nút liên kết với tài khoản GitHub `cuongvba` và cấp quyền cho repo `AegisQuiz`.
   *(Hoặc nếu dùng Deploy Key: Coolify sẽ cung cấp một SSH Public Key, bạn chỉ cần copy và paste vào GitHub Repo -> Settings -> Deploy Keys).*

2. **Tạo Application từ Git**:
   - Vào dự án của bạn trên Coolify -> Bấm **`+ New`** -> Chọn **`Git Repository`**.
   - Chọn repository: **`cuongvba/AegisQuiz`** và branch: **`main`**.
   - **Build Pack**: Chọn **`Docker Compose`**.
   - **Docker Compose Location**: Nhập `/docker-compose.prod.yml` (hoặc để mặc định nếu tệp ở thư mục gốc).

3. **Cấu hình Domain & Port**:
   - Trong danh sách các service của Compose:
     - Chọn service **`frontend`**.
     - Tại mục **Domains**: nhập **`https://daotao.dehoc.vn`**.
     - Cổng dịch vụ (Port): nhập `80`.
   - Bấm **Save**.

4. **Kích hoạt Auto-Deploy (Tự động triển khai khi Push code)**:
   - Tích chọn mục **`Auto-deploy`** (hoặc Webhook).
   - Bấm nút **`Deploy`** màu tím để tiến hành build và khởi chạy phiên bản đầu tiên! Coolify sẽ tự động xin chứng chỉ SSL Let's Encrypt cho `https://daotao.dehoc.vn`.

---

## 3. Kiểm Tra Hệ Thống Sau Khi Go-Live

Sau khi Coolify báo trạng thái xanh lá cây `Healthy` / `Running`:

1. **Kiểm tra Trang Chủ & Giao Diện Khảo Thí**:
   - Truy cập: `https://daotao.dehoc.vn` -> Kiểm tra ổ khóa SSL bảo mật màu xanh.
   - Truy cập: `https://daotao.dehoc.vn/practice` -> Kiểm tra thanh điều khiển thông minh **Smart LaunchPad** và các chế độ thi.
2. **Kiểm tra Đăng Nhập / Đăng Xuất**:
   - Đăng nhập tại `https://daotao.dehoc.vn/login` -> Tự động chuyển hướng về `https://daotao.dehoc.vn/`.
   - Bấm **Đăng xuất** -> Tự động quay về `https://daotao.dehoc.vn/login`.
3. **Kiểm tra Không Gian Đội Nhóm (Team Workspace)**:
   - Bấm mục menu **`Đội nhóm 👥`** trên thanh Header (hoặc vào `https://daotao.dehoc.vn/team`).
   - Kiểm tra giao diện Quản lý ngân hàng câu hỏi của Team (`/admin/questions?scope=TEAM`) và nút Luyện thi cùng Team.
3. **Kiểm tra Nạp Câu Hỏi Bằng AI (UCIS v3.0)**:
   - Tải thử file Word/Excel vào ngân hàng câu hỏi, kiểm tra hệ thống tự động sinh mã chủ đề SSAE ngắn gọn và lưu trữ thành công.
4. **Kiểm tra Real-time Hubs & Healthcheck**:
   - Truy cập: `https://dehoc.vn/health` -> Trả về `Healthy`.
   - Vào phòng thi `https://dehoc.vn/quiz/secure-exam/practice` hoặc đấu trường `https://dehoc.vn/arena` -> Kiểm tra kết nối SignalR WebSockets thông suốt (không bị gián đoạn hay fallback SPA).

---

## 4. Công Cụ Kiểm Toán Pre-Flight Tự Động Trước Khi Push Code

Trước khi push mã nguồn hoặc bấm Deploy trên Coolify, bạn có thể chạy kịch bản kiểm toán tự động 1-Click:

- **Trên Windows (PowerShell)**:
  ```powershell
  powershell -ExecutionPolicy Bypass -File scripts/verify-golive.ps1
  ```
- **Trên Linux / VPS**:
  ```bash
  chmod +x scripts/verify-golive.sh
  ./scripts/verify-golive.sh
  ```

Kịch bản sẽ tự động xác minh:
- Nginx Reverse Proxy (`/hubs/`, `/health`, WebSocket Upgrades)
- File cấu hình bảo mật `appsettings.Production.json`
- Đầy đủ Healthchecks và ports trong `docker-compose.prod.yml`
- 100% Bộ kiểm thử Unit Tests backend (142/142 tests)
- Gói biên dịch Release Backend .NET 10
- Gói bundle Frontend React 19 + Vite
- Cấu trúc IRT Adaptive Testing Service

