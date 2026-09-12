# AEGIS LIVING VISUAL DOCUMENTATION STANDARD (ALDS v1.0)
## Chuẩn Mực Quản Trị Tri Thức Kỹ Thuật Số Sống Động & Căn Cứ Thực Nghiệm Thị Giác Toàn Hệ Thống

---

## I. TỔNG QUAN & TẦM NHÌN CHIẾN LƯỢC

Trong kỷ nguyên phát triển phần mềm được hỗ trợ bởi Trí tuệ Nhân tạo (AI-Assisted Engineering), tốc độ thay đổi và tái cấu trúc mã nguồn diễn ra nhanh hơn gấp hàng chục lần so với trước đây. Mô hình tài liệu kỹ thuật truyền thống (Dead Specifications) — vốn chỉ bao gồm chữ viết khô khan, bản thiết kế tĩnh và hoàn toàn tách rời khỏi mã nguồn đang chạy — đã trở thành **"rào cản lớn nhất"** đối với chất lượng dự án:
- Tài liệu nhanh chóng lỗi thời sau vài phiên bản (Documentation Drift).
- Đội ngũ quản trị, kiểm toán và khách hàng không thể xác minh tính chân thực của tài liệu nếu không mở code hoặc tự tay thiết lập môi trường phức tạp.
- Thiếu các căn cứ thị giác trực quan làm bằng chứng nghiệm thu (Proof-of-Work).

**Aegis Living Visual Documentation Standard (ALDS v1.0)** ra đời nhằm xóa bỏ hoàn toàn khoảng trống này. ALDS biến toàn bộ tài liệu của **AegisQuiz** từ những trang văn bản tĩnh thành các **"Hồ Sơ Nghiệm Thu Thực Chiến Sống Động" (Living Acceptance Dossiers)**, nơi kiến trúc lý thuyết, mã nguồn thực thi, bằng chứng kiểm thử định lượng và căn cứ thị giác hòa quyện làm một.

---

## II. 5 TRỤ CỘT BẮT BUỘC CỦA CHUẨN ALDS v1.0

Tất cả tài liệu kỹ thuật, tài liệu module, đặc tả kiến trúc và sổ tay vận hành trong hệ thống AegisQuiz bắt buộc phải cấu trúc tuần tự theo **5 Trụ Cột Tri Thức Sống**:

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                 AEGIS LIVING VISUAL DOCUMENTATION STANDARD (ALDS v1.0)           │
└────────────────────────────────────────┬─────────────────────────────────────────┘
                                         │
 ┌───────────────────────────────────────┴─────────────────────────────────────────┐
 │ TRỤ CỘT 1: ARCHITECTURAL BLUEPRINT & FORMAL SPECIFICATION                       │
 │ • Tầm nhìn bài toán nghiệp vụ, mô hình toán học / bản thể học (Ontology).       │
 │ • Sơ đồ luồng dữ liệu Mermaid tương tác thời gian thực.                         │
 └───────────────────────────────────────┬─────────────────────────────────────────┘
                                         │
 ┌───────────────────────────────────────┴─────────────────────────────────────────┐
 │ TRỤ CỘT 2: LIVE CODE & API CONTRACT VERIFIABILITY                               │
 │ • Clickable links trỏ trực tiếp đến file mã nguồn thật (Backend/Frontend).      │
 │ • DTOs, Interface, Endpoint signatures phản ánh 100% code đang chạy.           │
 └───────────────────────────────────────┬─────────────────────────────────────────┘
                                         │
 ┌───────────────────────────────────────┴─────────────────────────────────────────┐
 │ TRỤ CỘT 3: QUANTITATIVE TEST PROOF & BENCHMARKS (CHỈ SỐ THỰC NGHIỆM ĐỊNH LƯỢNG)  │
 │ • Báo cáo kiểm thử tự động (Unit/Integration Test pass rate, execution logs).   │
 │ • Thống kê tải thực tế trên dữ liệu thật (VD: 19/19 files .xlsx, >2000 câu hỏi).│
 └───────────────────────────────────────┬─────────────────────────────────────────┘
                                         │
 ┌───────────────────────────────────────┴─────────────────────────────────────────┐
 │ TRỤ CỘT 4: VISUAL PROOF-OF-WORK (CĂN CỨ THỊ GIÁC BẤT KHẢ CHỐI CÃI)              │
 │ • Ảnh chụp màn hình thực tế (UI Screenshots) từ phiên chạy trên trình duyệt thật.│
 │ • Ghi nhận các bước thao tác (Modal, Dropdown, Toast xanh lá, Lưới dữ liệu).   │
 └───────────────────────────────────────┬─────────────────────────────────────────┘
                                         │
 ┌───────────────────────────────────────┴─────────────────────────────────────────┐
 │ TRỤ CỘT 5: ZERO-REGRESSION AUDIT TRAIL & CHANGELOG                              │
 │ • Nhật ký nâng cấp phiên bản kèm mốc thời gian, mã băm commit và trạng thái.    │
 └─────────────────────────────────────────────────────────────────────────────────┘
```

---

## III. QUY ĐỊNH CHI TIẾT TỪNG TRỤ CỘT

### 1. Trụ Cột 1: Bản Thiết Kế Kiến Trúc & Sơ Đồ Mermaid Tương Tác
- Bắt buộc phải có ít nhất 1 sơ đồ trực quan biểu diễn luồng dữ liệu hoặc cấu trúc phân tầng.
- Định dạng sơ đồ: **GitHub Flavored Markdown Mermaid** (`sequenceDiagram`, `flowchart TD/LR`, hoặc `classDiagram`).
- Nhãn trong node Mermaid phải rõ ràng, tránh sử dụng mã thô hoặc ký tự đặc biệt gây lỗi render.

### 2. Trụ Cột 2: Xác Minh Mã Nguồn Động (Live Code Links)
- Tuyệt đối không trích dẫn tên file dạng văn bản trơn (Plain text) như `ExcelParserService.cs`.
- **Bắt buộc định dạng clickable file link** theo chuẩn URI:
  - Đúng: `[ExcelParserService.cs](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Excel/ExcelParserService.cs)`
  - Có thể trỏ đến dòng cụ thể: `[DetectCognitiveHeader](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Excel/ExcelParserService.cs#L85-L160)`
- Mọi DTO hoặc Interface trong tài liệu phải được đồng bộ chính xác với code C# / TypeScript hiện hành.

### 3. Trụ Cột 3: Chỉ Số Thử Nghiệm Định Lượng (Quantitative Test Proof)
- Mọi tài liệu phải nêu rõ kết quả kiểm thử tự động của module tương ứng:
  * Số lượng bài test đạt yêu cầu (VD: `125/125 Unit Tests Passed - 100%`).
  * Danh sách các ca kiểm thử điển hình (Key Test Cases).
  * Hiệu năng thực nghiệm (Latency đo bằng mili-giây, thông lượng xử lý bản ghi).

### 4. Trụ Cột 4: Căn Cứ Thị Giác Bất Khả Chối Cãi (Visual Proof-of-Work)
Đây là trụ cột mang tính cách mạng phân định tài liệu chuẩn ALDS v1.0:
- **Vị trí lưu trữ ảnh**:
  Toàn bộ ảnh chụp thực nghiệm phải được lưu trữ trong thư mục:
  `AegisQuiz/docs/assets/walkthroughs/<module-name>/`
- **Quy chuẩn ảnh chụp**:
  * Chụp trực tiếp từ trình duyệt đang chạy trên hệ thống thật (VD: `http://localhost:3000`).
  * Định dạng: `.png` hoặc `.webp`.
  * Ảnh phải rõ nét, độ phân giải tối thiểu 1280px chiều rộng.
  * Phải ghi lại các khoảnh khắc quan trọng: Khởi động giao diện, Thao tác thay đổi tham số, Phản hồi hệ thống (Toast, Banner thông báo), Kết quả dữ liệu cuối cùng.
- **Cú pháp nhúng ảnh trong Markdown**:
  ```markdown
  ![Mô tả chi tiết nội dung ảnh](../assets/walkthroughs/<module-name>/<ten_anh>.png)
  *Hình X: Chú thích rõ ràng ngữ cảnh và ý nghĩa của bức ảnh trong quy trình nghiệm thu.*
  ```

### 5. Trụ Cột 5: Vết Kiểm Toán & Lịch Sử Phiên Bản (Audit Trail)
Mỗi tài liệu phải kết thúc bằng một bảng lịch sử nâng cấp chuẩn hóa:

| Phiên Bản | Ngày Cập Nhật | Tác Giả / Agent | Nội Dung Nâng Cấp | Trạng Thái Kiểm Chứng |
|:---:|:---:|:---:|---|:---:|
| `v1.0` | 2026-08-15 | System Core | Khởi tạo tài liệu kiến trúc ban đầu. | Verified |
| `v4.0` | 2026-09-07 | Antigravity AI | Bổ sung UCTE v4.0, Self-Enrichment, Multi-row Fusion & Ảnh kiểm chứng. | **100% Verified (125/125 tests)** |

---

## IV. BẢNG DANH MỤC TÀI LIỆU HỆ THỐNG ÁP DỤNG CHUẨN ALDS

| Nhóm Tài Liệu | Tệp Tin Mục Tiêu | Trạng Thái Áp Dụng ALDS v1.0 |
|---|---|:---:|
| **Flagship Tiên Phong** | [UNIVERSAL_COGNITIVE_EXCEL_INGESTION_STUDIO.md](file:///d:/Cuong/DuAn/mybank/AegisQuiz/docs/modules/UNIVERSAL_COGNITIVE_EXCEL_INGESTION_STUDIO.md) | **ĐÃ HOÀN TẤT ĐỈNH CAO (FLAGSHIP)** |
| **Khảo Thí Cốt Lõi** | [IRT_ADAPTIVE_TESTING.md](file:///d:/Cuong/DuAn/mybank/AegisQuiz/docs/modules/IRT_ADAPTIVE_TESTING.md) | Kế hoạch đợt 2 |
| **Khảo Thí Cốt Lõi** | [DOCX_PARSER_AND_MATH_ENGINE.md](file:///d:/Cuong/DuAn/mybank/AegisQuiz/docs/modules/DOCX_PARSER_AND_MATH_ENGINE.md) | Kế hoạch đợt 2 |
| **Nghiệp Vụ Chuyên Ngành** | [NATIONAL_DRIVING_LICENSE_MODULE.md](file:///d:/Cuong/DuAn/mybank/AegisQuiz/docs/modules/NATIONAL_DRIVING_LICENSE_MODULE.md) | Kế hoạch đợt 2 |
| **Hạ Tầng & Bảo Mật** | [SYSTEM_ARCHITECTURE.md](file:///d:/Cuong/DuAn/mybank/AegisQuiz/docs/architecture/SYSTEM_ARCHITECTURE.md) | Kế hoạch đợt 3 |
| **Hạ Tầng & Bảo Mật** | [SECURITY_AND_PROCTORING.md](file:///d:/Cuong/DuAn/mybank/AegisQuiz/docs/architecture/SECURITY_AND_PROCTORING.md) | Kế hoạch đợt 3 |
| **Sổ Tay Vận Hành** | [ADMIN_OPERATION_GUIDE.md](file:///d:/Cuong/DuAn/mybank/AegisQuiz/docs/manuals/ADMIN_OPERATION_GUIDE.md) | Kế hoạch đợt 4 |
| **Sổ Tay Vận Hành** | [ARENA_GAMESHOW_MANUAL.md](file:///d:/Cuong/DuAn/mybank/AegisQuiz/docs/manuals/ARENA_GAMESHOW_MANUAL.md) | Kế hoạch đợt 4 |

---

## V. CHẾ TÀI & QUY ĐỊNH THỰC THI (ENFORCEMENT POLICY)

1. Quy chuẩn ALDS v1.0 được nhúng trực tiếp vào cấu hình nhân tác tử tại [.agents/rules/documentation_standard.md](file:///d:/Cuong/DuAn/mybank/.agents/rules/documentation_standard.md).
2. Bất kỳ Pull Request (PR), bản chuyển giao (Handover) hoặc bản cập nhật tính năng mới nào không đính kèm đầy đủ 5 Trụ Cột ALDS v1.0 sẽ bị hệ thống tự động từ chối sáp nhập (Merge Rejection).
3. Đảm bảo toàn bộ tài liệu kỹ thuật của AegisQuiz luôn là tài liệu sống, có giá trị pháp lý và giá trị thuyết phục cao nhất trước mọi hội đồng kiểm toán và khách hàng doanh nghiệp quốc tế.
