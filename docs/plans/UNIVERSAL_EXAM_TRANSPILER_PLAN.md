# 🏛️ KẾ HOẠCH TRIỂN KHAI: UNIVERSAL EXAM TRANSPILER (CHUẨN EDTECH QUỐC TẾ)
> **Phiên bản:** 1.0.0 — Golden Master Architectural Plan  
> **Tổng Công Trình Sư:** Lead System Analyst & IT Architect Ecosystem AegisQuiz  
> **Mục tiêu tối thượng:** Đưa AegisQuiz trở thành Bộ chuyển dịch Đề thi Vạn năng (Universal Exam Transpiler) số 1 thế giới, cho phép xuất đề thi nhập từ file ra mọi định dạng chuẩn công nghiệp (**Moodle XML, IMS QTI 2.1/3.0, SCORM 1.2/2004, Moodle GIFT, Azota, Excel**) Plug-and-Play vào Canvas, Moodle, TalentLMS, Blackboard mà không gặp bất kỳ rào cản kỹ thuật nào.

---

## I. NĂM NGUYÊN TẮC VÀNG VÀ RÀNG BUỘC KIẾN TRÚC (THE 5 GOLDEN LAWS)
1. **Bảo toàn ngữ nghĩa 9 Dạng Câu hỏi (Semantic Fidelity)**:
   - Các dạng phức tạp như `MULTI_TRUE_FALSE` (Đúng/Sai 4 ý), `ORDERING`, `MATCHING`, `SHORT_ANSWER` khi chuyển sang Moodle/Canvas phải tự động ánh xạ thông minh (Smart Semantic Mapping) sang dạng tương đương (Cloze Sub-Questions, Multi-Choice, Matching) để giữ nguyên cấu trúc và thang điểm đánh giá.
2. **Khép kín Tài nguyên (Self-Contained Asset Encapsulation - Zero Broken Links)**:
   - Hình ảnh, sơ đồ hình học, đồ thị hàm số và vector WMF phải được nhúng trực tiếp dạng Base64 (đối với Moodle XML) hoặc đóng gói chuẩn hóa trong cấu trúc `resources/` kèm `imsmanifest.xml` (đối với QTI 2.1/3.0 và SCORM Zip Package).
3. **Bảo tồn Công thức Toán Học (MathML / LaTeX Preservation)**:
   - Toàn bộ công thức MathType/LaTeX được chuẩn hóa sạch thành `<span class="math-tex">\(...\)</span>` (MathJax/KaTeX syntax) hoặc MathML `<math>` chuẩn W3C, hiển thị sắc nét 60fps trên mọi trình duyệt.
4. **Tuân thủ Tuyệt đối OCP & CLEAR Architecture**:
   - Tầng `Application` định nghĩa `ITranspilerExportStrategy` và `IUniversalExamTranspilerService`.
   - Tầng `Infrastructure` hiện thực các Strategies độc lập. Thêm định dạng mới chỉ cần thêm Strategy class mới, 0ms sửa đổi code cũ.
5. **Zero Hard-code & Chuẩn Hóa i18n 5 Ngôn ngữ**:
   - Mọi nhãn định dạng, mô tả và cấu hình MIME type nạp động. Frontend cung cấp bộ chọn định dạng trực quan hỗ trợ cả 5 ngôn ngữ (`vi`, `en`, `ja`, `ko`, `ar`).

---

## II. MA TRẬN ÁNH XẠ NGỮ NGHĨA 9 DẠNG CÂU HỎI (SEMANTIC TRANSPILATION MATRIX)

| Dạng Câu Hỏi AegisQuiz | Chuẩn Moodle XML | Chuẩn IMS QTI 2.1/3.0 (Canvas) | Chuẩn Moodle GIFT | Chuẩn SCORM 1.2/2004 |
| :--- | :--- | :--- | :--- | :--- |
| **SINGLE (Trắc nghiệm đơn)** | `<question type="multichoice">` (`single="true"`) | `<choiceInteraction maxChoices="1">` | `{=Đúng ~Sai ~Sai}` | Micro-Widget SingleChoice JS |
| **MULTI (Nhiều đáp án)** | `<question type="multichoice">` (`single="false"`) | `<choiceInteraction maxChoices="N">` | `{~%50%Đúng1 ~%50%Đúng2 ~%-100%Sai}` | Micro-Widget MultiChoice JS |
| **TRUE_FALSE (Đúng/Sai đơn)** | `<question type="truefalse">` | `<choiceInteraction maxChoices="1">` | `{TRUE}` / `{FALSE}` | Micro-Widget TrueFalse JS |
| **MULTI_TRUE_FALSE (4 ý GDPT)** | `<question type="cloze">` (4 Multichoice Sub-questions) | 4 x `<inlineChoiceInteraction>` | Fallback sang 4 câu TF liên tiếp | Micro-Widget MultiTrueFalse JS |
| **MATCHING (Nối cặp)** | `<question type="matching">` | `<matchInteraction>` | `{=Trái1 -> Phải1 =Trái2 -> Phải2}` | Micro-Widget Matching JS |
| **ORDERING (Sắp xếp)** | Fallback `<question type="matching">` (Thứ tự 1..N) | `<orderInteraction>` | Fallback Matching format | Micro-Widget Ordering JS |
| **SHORT_ANSWER** | `<question type="shortanswer">` | `<textEntryInteraction>` | `{=Đáp án chuẩn =Đáp án phụ}` | Micro-Widget ShortAnswer JS |
| **FILL_BLANK** | `<question type="cloze">` | `<textEntryInteraction>` | Cloze syntax `{1:SHORTANSWER:=Đáp án}` | Micro-Widget FillBlank JS |
| **ESSAY (Tự luận)** | `<question type="essay">` | `<extendedTextInteraction>` | `{}` | Micro-Widget Essay JS |

---

## III. KẾ HOẠCH HÀNH ĐỘNG TRIỂN KHAI (3 PHA CHIẾN LƯỢC)

### 📌 PHA 1: THIẾT KẾ KIẾN TRÚC LÕI & MOODLE XML STRATEGY (Triển khai ngay)
- [x] Soạn thảo Living Documentation chuẩn hóa kiến trúc.
- [ ] Thiết kế `ITranspilerExportStrategy` và `IUniversalExamTranspilerService` trong `AegisQuiz.Application`.
- [ ] Hiện thực `MoodleXmlExportStrategy.cs` hỗ trợ toàn diện:
  - 9 dạng câu hỏi với cơ chế Cloze thông minh cho `MULTI_TRUE_FALSE`.
  - Nhúng ảnh Base64 tự động trong thẻ `<file encoding="base64">`.
  - Hỗ trợ công thức LaTeX chuẩn `<span class="math-tex">\(...\)</span>`.
- [ ] Hiện thực `MoodleGiftExportStrategy.cs` xuất file văn bản `.gift.txt`.
- [ ] Đăng ký Dependency Injection tại `Program.cs`.
- [ ] Mở rộng `ConvertController.cs` cung cấp endpoints:
  - `GET /api/v1/convert/export-formats`
  - `POST /api/v1/convert/preview-transpile` (Xuất trực tiếp từ preview AST)
  - `POST /api/v1/convert/file-transpile` (Chuyển đổi 1-click từ Word/PDF sang Moodle XML/GIFT)

### 📌 PHA 2: IMS QTI 2.1 / 3.0 PACKAGE STRATEGY (Canvas LMS & Blackboard)
- [ ] Hiện thực `Qti30PackageExportStrategy.cs`:
  - Tạo cấu trúc thư mục ảo IMS Content Package (Zip).
  - Tự động sinh `imsmanifest.xml` chuẩn 1EdTech.
  - Tách ảnh ra thư mục `resources/` và ánh xạ đường dẫn URL tương đối.
  - Sinh file assessment XML dạng `qti.xml` tương thích hoàn toàn Canvas Quiz Engine.

### 📌 PHA 3: NÂNG CẤP GIAO DIỆN PREVIEW VÀ EXPORT MODAL TRÊN FRONTEND
- [ ] Bổ sung menu xổ xuống "Xuất bản Chuẩn Quốc Tế" trong [DocxPreviewModal.tsx](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Frontend/src/pages/admin/questions/components/DocxPreviewModal.tsx):
  - 🎓 Moodle XML (Kéo thả 1 chạm vào Moodle)
  - 🎨 Canvas / QTI 2.1 Package (Nhập vào Canvas LMS)
  - 📝 Moodle GIFT Format (Văn bản nhẹ)
  - 📊 Excel Đa Định Dạng (Native, Azota, GDPT 2025)
- [ ] Tích hợp thông báo Toast và theo dõi tiến trình tải xuống mượt mà.
- [ ] Kiểm thử kiểm chứng biên dịch `npm run build` và Docker container.

---
*Tài liệu này là cam kết kỹ thuật bất biến, được kiểm duyệt bởi Tổng Công Trình Sư AegisQuiz.*
