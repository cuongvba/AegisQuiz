# QUY CHUẨN BẮT BUỘC: PHÂN LẬP & XỬ LÝ LỖI ĐỌC ĐỀ DOCX VÀ TOÁN HỌC (DOCX & MATH FAULT ISOLATION DIRECTIVE)

> **Mã quy chuẩn**: `DOCX_MATH_FAULT_ISOLATION_RULE_v2026`  
> **Phạm vi áp dụng**: Toàn bộ hệ thống AegisQuiz (Backend .NET 10, Frontend React 19/TS, Module Docx Parser, MathType WMF Engine, MathRenderer).  
> **Nguyên tắc tối thượng**: **KHI GẶP LỖI ĐỌC ĐỀ TỪ FILE DOCX HOẶC HIỂN THỊ CÔNG THỨC TOÁN, TUYỆT ĐỐI KHÔNG ĐƯỢC KIỂM TRA TOÀN BỘ HỆ THỐNG. BẮT BUỘC ĐỐI CHIẾU MA TRẬN PHÂN LẬP 1 BƯỚC ĐỂ CAN THIỆP ĐÚNG 1 ĐIỂM DUY NHẤT.**

---

## I. MA TRẬN PHÂN LẬP LỖI 1 BƯỚC (1-STEP FAULT ISOLATION MATRIX)

Khi nhận diện triệu chứng lỗi trực quan trên màn hình (View), kỹ sư hoặc AI Assistant **CHỈ ĐƯỢC PHÉP** can thiệp vào đúng tệp tin tương ứng:

```
[Màn hình xuất hiện lỗi] ──> [Đối chiếu Triệu chứng] ──> [Can thiệp đúng 1 tệp tin]
```

### 1. Nhóm A: Lỗi sai ký tự toán học (Symbol / Greek / Unicode)
* **Triệu chứng nhận diện**:
  - Dấu phẩy trên biến thành ký hiệu cent: $A^{\cent}, B^{\cent}$ thay vì $A', B'$.
  - Dấu tích phân biến thành chữ $\text{ò}$.
  - Dấu vuông góc biến thành chữ $\text{Å}$.
  - Ký hiệu thuộc/không thuộc biến thành chữ $\text{Î}, \text{Ï}$.
  - Ký hiệu vector biến thành chữ `uur` hoặc `ABuur`.
* **Quy luật xử lý**: Lỗi thiếu ánh xạ mã byte nhị phân của font `Symbol` hoặc font `MT Extra` sang Unicode.
* **Điểm can thiệp DUY NHẤT**:
  👉 **Bổ sung 1 dòng JSON vào Registry (Zero-Code):**
  [`Backend/src/AegisQuiz.Infrastructure/Config/math_dictionary_registry.json`](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Config/math_dictionary_registry.json)  
  *Tuyệt đối không sửa đổi thuật toán cốt lõi của Backend hay Frontend.*

---

### 2. Nhóm B: Lỗi hình dạng che phủ ("Viên thuốc đen" / Black Pill Defect)
* **Triệu chứng nhận diện**:
  - Xuất hiện khối đa giác màu đen xì che lấp toàn bộ hoặc một phần công thức toán học.
* **Quy luật xử lý**: WMF chứa record metadata OLE/Escape (`0x0538 META_POLYPOLYGON` hoặc `0x0626 META_ESCAPE`) bị render nhầm thành hình học.
* **Điểm can thiệp DUY NHẤT**:
  👉 **Kiểm tra bộ lọc record WMF trong:**
  [`Backend/src/AegisQuiz.Infrastructure/Services/MathTypeWmfEngine.cs`](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/MathTypeWmfEngine.cs#L861-L865)

---

### 3. Nhóm C: Lỗi kích thước & khoảng trắng ("Thanh trắng khổng lồ" / Blank White Bar)
* **Triệu chứng nhận diện**:
  - Xuất hiện các khối/thanh hình chữ nhật màu trắng kéo dài $100\%$ chiều ngang dòng chữ, bên trong hoàn toàn trống rỗng không có chữ.
  - Công thức inline biến mất hoặc bị ẩn.
* **Quy luật xử lý**:
  - Lỗi Index capture group trong Regex tọa độ dẫn đến giá trị `NaN` (ví dụ `yMatch[2]` thay vì `yMatch[1]`).
  - Giá trị `NaN` làm hỏng `viewBox` của SVG (`viewBox="... NaN ... NaN"`), khiến trình duyệt từ chối render và mất chiều cao hợp lệ.
* **Điểm can thiệp DUY NHẤT**:
  👉 **Kiểm tra chỉ số nhóm Capture Regex trong:**
  [`Frontend/src/shared/lib/quiz/wmfDecoder.ts`](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Frontend/src/shared/lib/quiz/wmfDecoder.ts#L530-L545)

---

### 4. Nhóm D: Lỗi lệch trục dòng & bố cục hiển thị (Typography & Vertical Alignment)
* **Triệu chứng nhận diện**:
  - Công thức inline ($x$, $y$, $MN$) bị rơi xuống quá thấp so với dòng chữ xung quanh.
  - Công thức bay lơ lửng trên đầu dòng hoặc bị ép xuống dòng mới vô cớ.
* **Quy luật xử lý**: Sai lệch hệ số `vAlign` hoặc thiếu thuộc tính `inline-block`.
* **Điểm can thiệp DUY NHẤT**:
  👉 **Tinh chỉnh công thức tính vAlign trong:**
  [`Frontend/src/components/common/MathRenderer.tsx`](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Frontend/src/components/common/MathRenderer.tsx#L64)

---

### 5. Nhóm E: Lỗi máy trạng thái cắt vụn câu hỏi (Classifier & Rule Studio)
* **Triệu chứng nhận diện**:
  - Phương án lựa chọn A, B, C, D bị mất chữ cái đầu (VD: $A = 2$ bị cắt mất $A$).
  - Thân câu hỏi bị ngắt ngang làm đôi hoặc lời giải bị nuốt vào thân câu.
* **Quy luật xử lý**: Regex nhận diện mốc câu hỏi nuốt mất phần ký tự toán học.
* **Điểm can thiệp DUY NHẤT**:
  👉 **Cấu hình lại mẫu Regex trong:**
  [`Backend/src/AegisQuiz.Infrastructure/Config/parsing_rules_registry.json`](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Config/parsing_rules_registry.json)

---

### 6. Nhóm F: Lạm dụng WMF & Rút gọn thông minh sang Text/LaTeX Inline
* **Triệu chứng nhận diện**:
  - Đề thi chứa quá nhiều WMF cho các biến đơn giản ($x, y$), đoạn thẳng ($MN$), điểm tọa độ ($A(1;2;3)$), mặt phẳng ($Oxyz$), hàm số ($y=f(x)$) làm dung lượng DB phình to gấp 100 lần.
  - Cần chuyển WMF đơn giản về Text KaTeX inline `$formula$`, chỉ giữ WMF phức tạp (phân số tầng, ma trận, tích phân có cận) ở dạng Vector SVG.
* **Quy luật xử lý**:
  - WMF đơn giản: 1 dòng ngang ($\Delta Y \le 0.55 \times \text{baseFs}$), không có nét vẽ hình học `Polyline/Polygon/Raster`, số lệnh `LineTo == 0`.
  - Phân tích và sinh mã KaTeX inline với nhận diện số mũ `^{...}`, chỉ số dưới `_{...}`, ánh xạ Symbol ($A'$, $\pm$, $\in$, $\le$, $\ge$, $\pi$, $\alpha$, $\beta$, $\Delta$).
* **Điểm can thiệp DUY NHẤT**:
  👉 **Hàm `TryExtractSimpleMathText` trong:**
  [`Backend/src/AegisQuiz.Infrastructure/Services/MathTypeWmfEngine.cs`](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/MathTypeWmfEngine.cs)
  👉 **Và nơi gọi tích hợp trong:**
  [`Backend/src/AegisQuiz.Infrastructure/Services/DocxParserService.cs`](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/DocxParserService.cs#L309)

---

## II. 4 NGUYÊN TẮC BẤT DI BẤT DỊCH TRONG THIẾT KẾ & KIỂM THỬ MÃ NGUỒN

1. **Nguyên tắc "Fail-Safe Không Lan Truyền":**
   Mọi hàm tính toán kích thước, viewBox, lineFactor BẮT BUỘC phải có guard chống `NaN`:
   ```typescript
   const safeLineFactor = (lineFactor && !isNaN(lineFactor) && lineFactor > 0) ? lineFactor : 1.0;
   heightEm = Math.max(1.20, safeLineFactor * baseUnit);
   if (isNaN(heightEm)) heightEm = 1.45;
   ```
2. **Nguyên tắc "Zero-Code Mở Rộng":**
   Ký tự mới, font mới, định dạng đề thi mới phải mở rộng qua JSON Registry, không được hardcode thêm `if/else` trong service.
3. **Nguyên tắc "Thanh tra Trực tiếp":**
   Khi cần kiểm tra một chuỗi công thức bị lỗi, sử dụng công cụ có sẵn `MathFormulaInspectorModal` để thanh tra dữ liệu tại chỗ trong 5 giây thay vì debug toàn bộ hệ thống.
4. **Nguyên tắc "Kiểm thử Khoanh vùng & Xác nhận trước khi chạy Full Suite":**
   Khi can thiệp sửa lỗi Docx hoặc Math, **CHỈ ĐƯỢC CHẠY KIỂM THỬ KHOANH VÙNG** cho module liên quan (ví dụ: `dotnet test ... --filter "FullyQualifiedName~Math"`). **TUYỆT ĐỐI KHÔNG TỰ Ý CHẠY TOÀN BỘ BỘ TEST SUITE** của hệ thống nếu chưa hỏi và nhận được xác nhận trực tiếp từ người dùng.

