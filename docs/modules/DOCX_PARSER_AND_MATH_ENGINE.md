# 📐 Bộ Bóc Tách Đề Thi Word & Xử Lý Công Thức Toán (DOCX & Math Engine)

Tài liệu này mô tả kỹ thuật xử lý và bóc tách tự động đề thi từ định dạng Microsoft Word (`.docx`), chuyển đổi công thức toán Office Math (OMML) sang LaTeX, và giải mã hình ảnh đồ họa vector WMF sang PNG/SVG trong **AegisQuiz**.

---

## 1. Vấn Đề Kỹ Thuật Khi Bóc Tách Đề Thi Word

Trong thực tế tại các trường học Việt Nam:
- Hàng triệu đề thi môn Toán, Vật Lý, Hóa Học được lưu trữ dưới dạng file `.docx`.
- Các công thức toán học được gõ bằng **MathType** (nhúng dưới dạng ảnh vector WMF - Windows Metafile) hoặc **Microsoft Equation** (OMML - Office Open XML Math).
- Nếu chỉ đọc text thuần túy (Plain Text), các công thức toán như phân số $\frac{a}{b}$, căn bậc hai $\sqrt{x}$, tích phân $\int$, ma trận sẽ bị biến thành ký tự rác hoặc mất hẳn.

Hệ thống **AegisQuiz** xây dựng một pipeline chuyên sâu để giải quyết bài toán này:

```
[File .docx tải lên]
       │
       ▼
[Giải nén cấu trúc OpenXML / word/document.xml]
       │
       ├─► [Đoạn văn có công thức OMML (<m:oMath>)] ──► [Chuyển đổi sang chuẩn LaTeX $...$]
       │
       ├─► [Đoạn nhúng hình ảnh WMF (<w:drawing>)] ──► [Giải mã Vector WMF sang ảnh PNG/SVG]
       │
       ▼
[Nhận diện cấu trúc Câu hỏi: Câu 1, Câu 2... và Phương án: A., B., C., D.]
       │
       ▼
[Tách nội dung đề bài, đáp án và lời giải]
       │
       ▼
[Lưu trữ câu hỏi hoàn chỉnh vào PostgreSQL Database]
```

---

## 2. Chuyển Đổi OMML Sang LaTeX

File `document.xml` chứa các thẻ XML toán học dạng:
```xml
<m:oMath>
  <m:f>
    <m:num><m:r><m:t>1</m:t></m:r></m:num>
    <m:den><m:r><m:t>2</m:t></m:r></m:den>
  </m:f>
</m:oMath>
```

Bộ parser của AegisQuiz áp dụng XSLT Transform hoặc AST Walker duyệt qua cây XML để ánh xạ:
- Thẻ `<m:f>` (Fraction) ➔ `\frac{num}{den}`
- Thẻ `<m:rad>` (Radical) ➔ `\sqrt{deg}{base}`
- Thẻ `<m:sSup>` (Superscript) ➔ `base^{sup}`
- Thẻ `<m:sSub>` (Subscript) ➔ `base_{sub}`
- Kết quả đầu ra là chuỗi LaTeX hoàn chỉnh: `$\frac{1}{2}$`.

---

## 3. Giải Mã Ảnh Vector WMF Sang PNG/SVG

Các file MathType cũ thường lưu trữ công thức dưới định dạng nhị phân **Windows Metafile (WMF)**:
- Thư viện chuyển đổi đồ họa đọc header của stream WMF (`0x9AC6CDD7` hoặc `0x00010000`).
- Vẽ lại các vector path (đường cong Bézier, glyphs font ký hiệu toán học Symbol/MT Extra).
- Xuất ra file hình ảnh PNG chuẩn sắc nét hoặc SVG vector có thể co giãn mượt mà trên giao diện Web.

---

## 4. Kiểm Thử Đảm Bảo Chất Lượng (Unit Testing)

Hệ thống được kiểm thử tự động trong `AegisQuiz.UnitTests/DeToanParserTests.cs`:
- Đảm bảo bóc tách chính xác 50 câu hỏi từ đề thi mẫu `DeToan.docx` và `DeToanGiaiChiTiet.docx`.
- Giữ nguyên vẹn 100% công thức đạo hàm, tích phân, hình học không gian.
- Nhận diện chính xác 4 đáp án A, B, C, D và gạch chân đáp án đúng của giáo viên.
