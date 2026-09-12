# 🛡️ AegisQuiz — Nền Tảng Khảo Thí & Đấu Trường Gameshow Trí Tuệ Thế Hệ Mới

> **Hệ sinh thái khảo thí thông minh Enterprise AI kết hợp Đấu Trường Gameshow Học Thuật (Arena Gameshow Ecosystem)** chuẩn quốc tế, trang bị mô hình IRT thích ứng (Adaptive Testing), bộ bóc tách đề thi tài liệu thông minh (Docx/WMF Math Parser) và kiến trúc cắm rút Gameshow Plugin mở rộng không giới hạn.  
> **Chủ nhiệm Kiến trúc & Trí tuệ Toàn cầu:** No.1 — Lead System Analyst, Master Living Documentarian, IT Architect, AI, Design, Education & Economic Specialist.

---

## 🚀 Những Đột Phá Công Nghệ Mới Nhất (2026 Supreme Breakthroughs)

Chi tiết xem tại tài liệu chuyên sâu: 📖 **[RECENT_SYSTEM_BREAKTHROUGHS_2026.md](docs/modules/RECENT_SYSTEM_BREAKTHROUGHS_2026.md)**

1. ⚡ **Động Cơ Nhận Thức Siêu Vi Bóc Tách Tọa Độ File (Universal Cognitive Sniffer)**: Tự động quét hàng biểu ngữ (Banner Rows) và tiêu đề file (ví dụ: `1. Tín dụng KHDN.xlsx`) để bóc tách 100% **Hệ 5 Trục Tọa Độ Tri Thức** (`DomainCode`, `TargetLevel`, `AssessmentPurpose`, `IssuingOrg`, `BenchmarkYear`, `BenchmarkStandard`) và sinh nhãn dán thông minh (Smart Tags).
2. 🛡️ **Kiến Trúc Bảo Toàn Thứ Tự Đáp Án Gốc (Decoupled Option Shuffler)**: Tuyệt đối giữ nguyên 100% thứ tự các phương án A, B, C, D gốc khi duyệt đề tại trang Quản trị viên; chỉ xáo trộn ngẫu nhiên thông minh bảo vệ ngữ nghĩa khi thí sinh tham gia phòng thi.
3. 🌳 **Cơ Chế Xóa Cây Phân Cấp An Toàn Tuyệt Đối**: Xóa đệ quy từ lá lên gốc (Leaf-to-Root), tháo gỡ hoàn toàn quan hệ cha-con tự tham chiếu, dồn câu hỏi an toàn về `GENERAL` và miễn nhiễm lỗi khóa ngoại dưới cơ chế Multi-Tenancy bằng `.IgnoreQueryFilters()`.
4. ✨ **Bộ Đôi Modal Tự Động Điền & 1-Click "⚡ Nhận diện tự động AI"**: Toàn bộ modal nhập liệu mở ra đã có sẵn thông tin chính xác, đi kèm nút nhận diện AI tức thì với trạng thái `✓ AI Ready`.
5. 🌐 **Kiến Trúc Quản Trị Đa Tầng Tiệm Tiến & Ngành Động Thích Ứng (Kỳ Quan 16)**: Phổ thích ứng từ Cá nhân/Nhóm (Express Mode - 1-Click) đến Tập đoàn toàn cầu (Enterprise Mode - Cây phân cấp 5 tầng `HeadOffice -> Region -> Branch -> Department -> Classroom`), Động cơ Quản lý Ngành động 3 lớp và bộ tọa độ tri thức kèm từ điển AI.
6. 🛡️ **Kiểm Toán Toàn Diện Backend Đạt Cảnh Giới Tối Cao (127/127 Tests Passed - 100%)**: Khóa cứng bảo mật `System.Security.Cryptography.Xml 10.0.11`, triệt tiêu 100% cảnh báo EF Core ChangeTracker qua `ValueComparer`, tích hợp trọn vẹn cầu nối khảo thí thích ứng CAT/IRT 3-PL (`IrtClientService`). Chi tiết xem tại: 📖 **[BACKEND_WORLD_CLASS_SUPREME_AUDIT_REPORT.md](docs/architecture/BACKEND_WORLD_CLASS_SUPREME_AUDIT_REPORT.md)**.

---

## 🌟 Tổng Quan Hệ Thống

**AegisQuiz** được thiết kế nhằm đáp ứng cả hai nhu cầu cốt lõi trong giáo dục và đào tạo hiện đại:
1. **Khảo thí & Luyện thi thông minh (Assessment & Adaptive Testing)**: Luyện thi phân hóa trình độ bằng thuật toán **Item Response Theory (IRT)** 3 tham số, phòng thi trực tuyến bảo mật chống gian lận (Secure Exam Room), trích xuất đề thi tự động từ file Word chứa công thức toán LaTeX phức tạp.
2. **Đấu trường Gameshow Tri Thức Trực Tuyến (Arena Gameshow Platform)**: Kiến trúc Plugin đa nền tảng cho phép học sinh, sinh viên và doanh nghiệp tổ chức các cuộc thi mô phỏng chuẩn xác các gameshow hàng đầu như **Đường Lên Đỉnh Olympia**, **Rung Chuông Vàng**, **Chiếc Nón Kỳ Diệu**, **University Challenge**, **Jeopardy!**, **Nhanh Như Chớp** với khả năng cướp chuông mili-giây và đồ họa Studio trực quan.

---

## 🏛️ Kiến Trúc Hệ Thống (Architecture Overview)

```
                       ┌──────────────────────────────────────────────────┐
                       │     Frontend Web App (React 19 + TypeScript)     │
                       │   Vite • TailwindCSS • Web Audio • HTML5 Canvas  │
                       └────────────────────────┬─────────────────────────┘
                                                │
                          HTTPS REST / WebSocket SignalR
                                                │
                       ┌────────────────────────▼─────────────────────────┐
                       │       AegisQuiz.API (.NET 10 Web API)            │
                       │   Controllers • SignalR ArenaHub • Middleware    │
                       └───────────────┬──────────────────┬───────────────┘
                                       │                  │
                ┌──────────────────────▼──────┐    ┌──────▼──────────────────────────┐
                │  AegisQuiz.Application      │    │ Python IRT Microservice         │
                │  DTOs, Core Business Rules  │    │ FastAPI • Item Response Theory  │
                └──────────────┬──────────────┘    │ Tham số độ khó & Khả năng theta │
                               │                   └─────────────────────────────────┘
                ┌──────────────▼──────────────┐
                │  AegisQuiz.Infrastructure   │
                │  • ArenaRoomManager         │
                │  • IArenaGamePlugin Engine  │
                │  • Docx Math/Formula Parser │
                │  • PostgreSQL / InMemory RAM│
                └─────────────────────────────┘
```

---

## 🧩 Phân Hệ Đấu Trường Gameshow (Arena Plugin System)

Hệ thống sở hữu cơ chế cắm rút **Plugin Architecture** cho phép mở rộng bất kỳ gameshow nào chỉ bằng cách thực thi interface `IArenaGamePlugin`. 

Hiện tại, hệ thống đã trang bị sẵn 6 Gameshow kinh điển:

| Gameshow | Mô Tả & Quy Tắc Nổi Bật | Công Nghệ Trực Quan |
| :--- | :--- | :--- |
| **Đường Lên Đỉnh Olympia** | 4 vòng thi (Khởi động tốc độ, Vượt chướng ngại vật ma trận 4 hàng ngang + ẩn số 80đ, Tăng tốc 40-30-20-10, Về đích & Ngôi sao hy vọng x2 điểm). | 4 Bục thí sinh 3D Podium, chuông buzzer cướp điểm đỏ rực. |
| **Rung Chuông Vàng** | Đấu trường sinh tồn 100 thí sinh trực tiếp (Battle Royale). Sai 1 câu rời sàn đấu. Minigame Thầy Cô cứu trợ hồi sinh thí sinh. | Sơ đồ Mega-Grid 100 ghế ma trận, bảng mica điện tử giơ đáp án. |
| **Chiếc Nón Kỳ Diệu** | Vòng quay vật lý Canvas 60fps có lực quán tính, dừng ngẫu nhiên tại các ô điểm (100 - 1000, Mất lượt, Nhân đôi, May mắn). Đoán chữ cái & giải toàn bộ ô chữ. | Vòng quay Canvas nan đa sắc, bảng lật ô chữ 3D thẻ từ. |
| **University Challenge** | Đối kháng liên trường đại học 4 vs 4 (Bách Khoa vs Ngoại Thương). Chuông Starter 10đ và bộ 3 câu hỏi Bonus 15đ có 15 giây thảo luận micro. | 2 Khối bục trường đại học 4 ghế, phân chia Starter/Bonus. |
| **Jeopardy! American Matrix** | Bảng ma trận 30 ô điểm (6 chủ đề x 5 mốc điểm $200 - $1000). Luật bắt buộc trả lời theo cấu trúc **câu hỏi ngược**: *"Ai là...?"*, *"Là gì...?"*. | Bảng điểm LED điện tử xanh dương phong cách truyền hình Mỹ. |
| **Nhanh Như Chớp** | Cỗ máy leo dốc nghiêng 10 bậc đứng trong 2 phút. Đúng leo +1 nấc, sai 1 câu lập tức **tụt dốc về vạch số 0**. Chinh phục 10 câu đúng liên tiếp. | Thang leo dốc 10 bậc phát sáng theo thời gian thực. |

---

## 🚀 Hướng Dẫn Cài Đặt & Chạy Phát Triển (Getting Started)

### Yêu Cầu Môi Trường:
- **.NET SDK**: Phiên bản 10.0 hoặc mới hơn.
- **Node.js**: Phiên bản 20.x trở lên & **npm**.
- **Python**: 3.10+ (cho dịch vụ IRT Adaptive Testing - tùy chọn).

### 1. Khởi Chạy Backend .NET API:
```bash
cd Backend/src/AegisQuiz.API
dotnet run
```
*API Swagger & SignalR Hub sẽ khởi chạy tại: `http://localhost:5000` (hoặc cấu hình trong `launchSettings.json`).*

### 2. Khởi Chạy Frontend Client:
```bash
cd Frontend
npm install
npm run dev
```
*Giao diện người dùng sẽ chạy tại: `http://localhost:3000`.*

### 3. Chạy Unit Tests:
```bash
cd Backend/tests/AegisQuiz.UnitTests
dotnet test
```

---

## 📚 Trung Tâm Tài Liệu Hệ Thống (Documentation Hub)

Toàn bộ tài liệu chi tiết từ thiết kế kiến trúc, lược đồ dữ liệu, phân hệ chuyên sâu đến đặc tả API và cẩm nang vận hành được lưu trữ tập trung tại thư mục **[`docs/`](docs/README.md)**:

- 🌌 **[ĐẠI ĐẶC TẢ HỆ THỐNG THỐNG NHẤT TOÀN DIỆN (UNIFIED MASTER SPECIFICATION v4.0)](docs/AEGISQUIZ_UNIFIED_MASTER_SPECIFICATION.md)**: Tác phẩm chuẩn tắc thống nhất tối cao tích hợp toàn diện Triết lý, Kiến trúc 4 tầng, 7 Kỳ quan công nghệ (kèm ví dụ vui nhộn), Đại sảnh 6 Gameshow kinh điển và Sổ tay vận hành đa đối tượng!
- 👑 **[Tài Liệu Kỹ Thuật Hệ Thống Chuẩn Quốc Tế v2.0 (78KB)](docs/AEGISQUIZ_SYSTEM_DOCUMENT_V2.0.md)**: Bản thiết kế Software Architecture Document (SAD) toàn diện với 9 đột phá công nghệ, phân tích bài toán, bảo mật và mở rộng.
- 🌐 **[Bản Thiết Kế Tối Thượng Chuẩn Quốc Tế v3.0](docs/AEGISQUIZ_GLOBAL_SUPREMACY_BLUEPRINT_V3.0.md)**: Bản thiết kế 4 Trụ Cột: Military-Grade IT, AI Neuroscience CAT/IRT, Obsessive Gamification, Platform Economics.
- 🗺️ **[Mục Lục Điều Hướng Toàn Diện docs/README.md](docs/README.md)**: Bản đồ điều hướng và chỉ mục của toàn bộ hệ thống.
- 🏛️ **Kiến Trúc & Bảo Mật**:
  - [Kiến Trúc Tổng Thể Hệ Thống (System Architecture)](docs/architecture/SYSTEM_ARCHITECTURE.md)
  - [Lược Đồ Cơ Sở Dữ Liệu & Entity Models (Data Models)](docs/architecture/DATA_MODELS.md)
  - [Phòng Thi Bảo Mật & Chống Gian Lận (Security & Proctoring)](docs/architecture/SECURITY_AND_PROCTORING.md)
- 🧩 **Các Phân Hệ Chuyên Sâu**:
  - [Đấu Trường Gameshow Plugin Engine](docs/modules/ARENA_PLUGIN_ENGINE.md) & [Arena Plugin Developer Guide](docs/ARENA_PLUGIN_DEVELOPER_GUIDE.md)
  - [Bộ Bóc Tách Đề Thi Word & Xử Lý Công Thức Toán (DOCX & Math Engine)](docs/modules/DOCX_PARSER_AND_MATH_ENGINE.md)
  - [Khảo Thí Thích Ứng Máy Tính & Mô Hình IRT 3PL](docs/modules/IRT_ADAPTIVE_TESTING.md)
- 🔌 **Đặc Tả Giao Thức API**:
  - [Đặc Tả Toàn Diện REST API Endpoints](docs/api/REST_API_REFERENCE.md)
  - [Đặc Tả Giao Thức SignalR WebSockets Real-time](docs/api/WEBSOCKET_SIGNALR_PROTOCOLS.md)
- 📖 **Cẩm Nang Người Dùng**:
  - [Cẩm Nang Thi Đấu & Tổ Chức Gameshow Đấu Trường](docs/manuals/ARENA_GAMESHOW_MANUAL.md)
  - [Cẩm Nang Dành Cho Học Sinh / Sinh Viên](docs/manuals/LEARNER_USER_MANUAL.md)
  - [Cẩm Nang Vận Hành & Quản Trị Hệ Thống (Admin Guide)](docs/manuals/ADMIN_OPERATION_GUIDE.md)
- 🚀 **DevOps & Vận Hành**:
  - [Hướng Dẫn Triển Khai Docker, Nginx & Môi Trường Sản Xuất](docs/devops/DEPLOYMENT_AND_OPERATIONS.md)

---

## 🛡️ Bản Quyền & Phát Triển

Phát triển bởi đội ngũ kỹ thuật AegisQuiz. Bảo lưu mọi quyền theo giấy phép nội bộ dự án.
