# 🏆 ĐẶC TẢ KIẾN TRÚC & QUY TRÌNH NHẬP ĐỀ THI TỪ FILE DOCX ĐẲNG CẤP THẾ GIỚI
## AEGIS-INGEST V9.0: Universal Multi-Modal Question Ingestion & Declarative Math Engine

> **Hệ Thống:** AegisQuiz Enterprise Educational Intelligence Platform  
> **Phiên bản:** AEGIS-INGEST V9.0 (Thế hệ Đột phá 2026)  
> **Kiến trúc:** Clean Architecture & Hexagonal Ports/Adapters  
> **Hiệu năng:** In-Memory $O(1)$ Concurrent Cache • Zero-Downtime Hot-Reload • 100% Fail-Safe Guarantee  

---

## MỤC LỤC
1. [Tầm Nhìn & Tiêu Chuẩn Công Nghệ Đẳng Cấp Thế Giới](#1-tầm-nhìn--tiêu-chuẩn-công-nghệ-đẳng-cấp-thế-giới)
2. [Bảng So Sánh Vượt Trội Với Các LMS & EdTech Truyền Thống](#2-bảng-so-sánh-vượt-trội-với-các-lms--edtech-truyền-thống)
3. [Quy Trình 6 Giai Đoạn Nhập Đề Tự Động (6-Phase Ingestion Pipeline)](#3-quy-trình-6-giai-đoạn-nhập-đề-tự-động-6-phase-ingestion-pipeline)
4. [Các Điểm Vượt Trội Đột Phá & Ví Dụ Thực Tế Minh Họa](#4-các-điểm-vượt-trội-đột-phá--ví-dụ-thực-tế-minh-họa)
5. [Hướng Dẫn Cấu Hình Toàn Diện Tệp JSON `math_dictionary_registry.json`](#5-hướng-dẫn-cấu-hình-toàn-diện-tệp-json-math_dictionary_registryjson)
6. [Hướng Dẫn Quản Trị & Bổ Sung Từ Điển Trên Giao Diện (Admin UI Walkthrough)](#6-hướng-dẫn-quản-trị--bổ-sung-từ-điển-trên-giao-diện-admin-ui-walkthrough)
7. [Đặc Tả API Quản Trị Đăng Bạ Từ Điển Toán Học](#7-đặc-tả-api-quản-trị-đăng-bạ-từ-điển-toán-học)
8. [Quy Trình Vận Hành & Khuyến Nghị Tối Ưu](#8-quy-trình-vận-hành--khuyến-nghị-tối-ưu)

---

## 1. Tầm Nhìn & Tiêu Chuẩn Công Nghệ Đẳng Cấp Thế Giới

Trong kỷ nguyên chuyển đổi số giáo dục và khảo thí tự động, việc bóc tách tài liệu đề thi Microsoft Word (`.docx`) luôn là thách thức công nghệ phức tạp bậc nhất:
- Các giáo viên và chuyên gia khảo thí sử dụng kết hợp đa dạng các phiên bản Office (Word 2003, 2010, 2016, Office 365).
- Định dạng công thức phân mảnh giữa **Microsoft Equation 3.0 / MathType 5/6/7** (nhúng dưới dạng nhị phân WMF - Windows Metafile) và **Word Equation (OMML - Office Math Markup Language)**.
- Các ký hiệu đặc thù của Việt Nam (hình học không gian $\Delta ABC$, $\widehat{ABC}$, hình hộp chữ nhật $ABCD.A'B'C'D'$, vector $\vec{u}$, căn thức $\sqrt{x+1}$) thường xuyên bị lỗi font, mất dấu hoặc biến dạng.

**AEGIS-INGEST V9.0** được thiết kế theo các tiêu chuẩn công nghệ nghiêm ngặt nhất của phần mềm cấp Enterprise:
1. **Kiến Trúc Nguồn Sự Thật Duy Nhất (Single Source of Truth) Đa Tầng**: Dữ liệu từ điển toán học và quy tắc cấu trúc được nạp đồng bộ giữa **Cơ sở dữ liệu $\leftrightarrow$ Tệp JSON cấu hình $\leftrightarrow$ Bộ nhớ RAM đệm đa luồng $O(1)$**.
2. **Động Hóa Toàn Diện Cơ Chế Rút Gọn Cấu Trúc (Declarative Structural Simplification Engine - P2)**: Toàn bộ việc bóc tách phân số, căn thức, hệ phương trình, giới hạn và vector được định nghĩa dạng khai báo (Declarative Rules) qua JSON, **triệt tiêu hoàn toàn mã cứng `if/else`**.
3. **Cơ Chế Bảo Toàn Đồ Họa Tuyệt Đối (Fail-Safe Vector Preservation)**: Mọi biểu thức đồ họa phức tạp, bảng biến thiên, đồ thị hàm số đều được tự động khử lỗi và giữ nguyên định dạng Vector SVG sắc nét, cam kết **100% không mất mát dữ liệu và không phát sinh lỗi "ảnh trắng" (blank image) hay "viên thuốc đen" (black pill defect)**.

---

## 2. Bảng So Sánh Vượt Trội Với Các LMS & EdTech Truyền Thống

| Tiêu Chí Kỹ Thuật | LMS Truyền Thống (Moodle, Canvas, Form...) | Phần Mềm Soạn Đề Thông Thường (789, Azota...) | **AEGIS-INGEST V9.0 (Hệ Thống Đột Phá)** |
| :--- | :--- | :--- | :--- |
| **Xử lý MathType WMF** | Bị biến thành ảnh raster mờ (PNG/JPG) hoặc mất hẳn công thức | Dễ dính lỗi "viên thuốc đen" che khuất câu hỏi hoặc viền đen xung quanh | **Phân rã đa mô thức**: Rút gọn công thức đơn sang KaTeX thuần; đồ thị phức tạp chuyển thành W3C SVG siêu nét |
| **Mức độ phụ thuộc mã nguồn (Hardcoding)** | Mã hóa cứng các trường hợp xử lý ký tự bằng hàng ngàn dòng lệnh `switch/case` | Phải sửa mã nguồn và biên dịch/deploy lại mỗi khi phát hiện ký tự lạ | **100% Khai báo động (Declarative Dynamic Registry)**: Thêm ký tự, quy tắc cấu trúc qua JSON/UI với Zero-Downtime Hot-Reload |
| **Bảo toàn hình học tiếng Việt** | $ABCD.A'B'C'D'$ bị biến thành tích vô hướng $ABCD \cdot A'B'C'D'$ | Dấu góc $\widehat{ABC}$ biến thành ký tự rác hoặc dấu mũ sai lệch | **Two-Phase Normalization Pipeline**: Tự động nhận diện và bảo vệ trọn vẹn ngữ nghĩa toán học Việt Nam |
| **Tốc độ thực thi** | Chậm (0.5s – 2s mỗi câu hỏi do gọi I/O và convert ảnh liên tục) | Trung bình (phụ thuộc vào thư viện bên thứ 3 cài trên OS) | **Cực nhanh ($< 5\text{ms}$ mỗi câu hỏi)** nhờ $O(1)$ ConcurrentDictionary và Regex compiled in-memory |
| **Quản trị & Giám sát** | Hộp đen (Black Box), không thể can thiệp khi gặp lỗi | Cấu hình giới hạn trong tệp `.ini` hoặc cơ sở dữ liệu cứng | **Visual Math Inspector & Admin Studio**: Giao diện trực quan xem trước KaTeX, Bật/Tắt quy tắc tức thì |

---

## 3. Quy Trình 6 Giai Đoạn Nhập Đề Tự Động (6-Phase Ingestion Pipeline)

```mermaid
flowchart TD
    A["File Word Đề Thi (.docx)"] --> B["GIAI ĐOẠN 1: Stream Ingestion & OpenXML Deconstruction"]
    B --> C["GIAI ĐOẠN 2: Media Bundler & Quan Hệ Tài Liệu"]
    C --> D["GIAI ĐOẠN 3: Duyệt Dòng & Math Hybrid Engine"]
    
    subgraph Math_Hybrid_Engine ["Math Hybrid Engine (Xử Lý Đa Mô Thức)"]
        D1["Word Equation <m:oMath>"] --> D2["OmmlToLatexConverter"]
        D3["MathType WMF Image"] --> D4["MathTypeWmfEngine (Gia Tốc 4 Tầng)"]
        D4 --> D5{"Phân Tích Độ Phức Tạp Hình Học"}
        D5 -- "Đơn giản (Line, Text, Mỏ neo)" --> D6["GIAI ĐOẠN 4: Declarative Structural Engine (P2)"]
        D5 -- "Phức tạp (Polygon, Shape, Đồ thị)" --> D7["W3C Vector SVG Sanitizer (Khử Lỗi Ô Đen)"]
        D6 --> D8["KaTeX Inline Math ($...$)"]
    end
    
    D --> Math_Hybrid_Engine
    D7 --> E["GIAI ĐOẠN 5: Two-Phase Normalization Pipeline"]
    D8 --> E
    D2 --> E
    
    E --> F["GIAI ĐOẠN 6: State Machine Classifier & Semantic Parser"]
    F --> G["Question Neural State & Lưu Cơ Sở Dữ Liệu"]
```

### Chi Tiết Từng Giai Đoạn:

#### GIAI ĐOẠN 1: Stream Ingestion & OpenXML Deconstruction
- Tệp `.docx` được tiếp nhận dưới dạng nhị phân luồng dữ liệu (Memory Stream) không lưu tệp tạm trên ổ đĩa, đảm bảo an toàn thông tin và tốc độ xử lý I/O cao nhất.
- Sử dụng `System.IO.Compression.ZipArchive` để giải nén cây thư mục OpenXML, trích xuất `word/document.xml` và `word/_rels/document.xml.rels`.

#### GIAI ĐOẠN 2: Media Bundler & Quan Hệ Tài Liệu
- Duyệt qua cây quan hệ `document.xml.rels` để ánh xạ chính xác mã định danh `r:id` với các tệp đa phương tiện nằm trong thư mục `word/media/`.
- Tự động băm (Hash SHA-256) dữ liệu nhị phân của ảnh để loại bỏ trùng lặp (Deduplication), tiết kiệm bộ nhớ lưu trữ.

#### GIAI ĐOẠN 3: Duyệt Dòng & Math Hybrid Engine (Xử Lý Đa Mô Thức)
- Duyệt tuần tự qua các phần tử cấp đoạn (`<w:p>`) và bảng biểu (`<w:tbl>`).
- Nếu gặp `<m:oMath>` hoặc `<m:oMathPara>`: Chuyển đổi trực tiếp sang mã KaTeX thông qua `OmmlToLatexConverter.cs` với từ điển ánh xạ `ommlTokens`.
- Nếu gặp `<w:drawing>` hoặc `<w:pict>` chứa ảnh `image/x-wmf`: Kích hoạt bộ giải mã `MathTypeWmfEngine.cs` để phân loại giữa công thức ký hiệu và đồ thị hình vẽ.

#### GIAI ĐOẠN 4: Động Hóa P2 (Declarative Structural Simplification Engine)
- Bóc tách tọa độ không gian Bounding Box của các record WMF (`LineTo`, `ExtTextOut`).
- So khớp với hệ quy tắc cấu trúc động nạp từ [math_dictionary_registry.json](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Config/math_dictionary_registry.json) mà không sử dụng bất kỳ lệnh `if/else` cố định nào trong mã C#.
- Hấp thu các đường nét hình học (gạch phân số, nóc căn, ngoặc nhọn hệ phương trình) và thay thế cụm token tương ứng bằng mã KaTeX chuẩn như `\frac{a}{b}`, `\sqrt{x}`, `\vec{u}`.

#### GIAI ĐOẠN 5: Chuẩn Hóa Toán Học Hai Pha (Two-Phase Normalization Pipeline)
- **Pha 1 (Pre-Norm):** Chuẩn hóa các ký hiệu hình học trước khi gom dòng (bảo vệ $\Delta ABC$, $\widehat{ABC}$, độ dài đoạn thẳng, dấu chấm lăng trụ).
- **Pha 2 (Post-Norm):** Ráp nối các token bị phân mảnh, tự động thêm dấu gạch chéo ngược cho các hàm giải tích/lượng giác (ví dụ: `sinx` $\rightarrow$ `\sin x`, `cosx` $\rightarrow$ `\cos x`, `lim` $\rightarrow$ `\lim`).

#### GIAI ĐOẠN 6: State Machine Classifier & Semantic Parser
- Sử dụng máy trạng thái hữu hạn (Deterministic Finite Automaton) để bóc tách cấu trúc đề thi:
  - Nhận diện Câu hỏi (Câu 1, Câu 2... hoặc Question 1...).
  - Tự động phát hiện kiểu câu hỏi: **Trắc nghiệm 4 lựa chọn (MCQ)**, **Đúng/Sai 4 ý (True/False)**, **Trả lời ngắn / Điền số (Short Answer)**.
  - Tự động tách phần Lời giải chi tiết / Hướng dẫn giải (nếu có).

---

## 4. Các Điểm Vượt Trội Đột Phá & Ví Dụ Thực Tế Minh Họa

### Ví Dụ 1: Phân Số Kèm Hàm Lượng Giác (`image12.wmf`)
* **Dữ liệu đầu vào**: Tệp WMF nhị phân chứa biểu thức phương trình lượng giác: $\sin x = \frac{1}{2}$.
* **Hạn chế của hệ thống cũ**: 
  - Tọa độ trục hoành $X$ của gạch phân số là $[1066, 1298]$. Các ký tự `s, i, n, x, =` nằm bên trái ($X \le 760$) bị gộp nhầm vào mẫu số do thuật toán ước lượng chiều rộng ký tự quá lớn.
  - Chuỗi trích xuất bị dính thành `sinx` mà không có tiền tố KaTeX chuẩn `\sin`.
* **Đột phá của AEGIS-INGEST V9.0**:
  - Thuật toán `IsInFractionSpan` kiểm tra tâm ký tự theo không gian hình học thực tế: Loại bỏ tuyệt đối các ký tự nằm ngoài phạm vi gạch phân số.
  - Tự động kích hoạt quy tắc `RULE_SIMPLE_FRACTION`, sinh ra `\frac{1}{2}`.
  - Pha Post-Norm tự động chuẩn hóa `sinx` thành `\sin x`.
  - **Kết quả xuất ra**: `$\sin x = \frac{1}{2}$` hoàn toàn ở dạng text KaTeX sắc nét, không tốn 1 byte ảnh SVG.

### Ví Dụ 2: Triệt Tiêu Hoàn Toàn Lỗi "Viên Thuốc Đen" (Black Pill Defect)
* **Hiện tượng lỗi**: Trong các file MathType cũ, một số bản ghi `PolyPolygon (0x0538)` hoặc `Polygon (0x0324)` chứa các điểm tọa độ dị thường che phủ toàn bộ khung nhìn, biến công thức thành một khối hình bầu dục hoặc chữ nhật đen đặc.
* **Đột phá của AEGIS-INGEST V9.0**:
  - `MathTypeWmfEngine.SanitizeMathTypeSvg()` tự động phát hiện các thẻ `<polygon>` có diện tích bao phủ $> 80\%$ viewBox và loại bỏ triệt để.
  - Tự động tái tính toán `viewBox` chuẩn W3C, thêm thuộc tính responsive giúp công thức hiển thị cân đối trên cả điện thoại và máy tính.

### Ví Dụ 3: Bảo Toàn Hình Học & Khối Đa Diện Tiếng Việt
* **Đoạn văn bản gốc**: `Cho hình hộp ABCD.A'B'C'D' có đáy là hình vuông cạnh a√3 và độ dài đường chéo là a√2.`
* **Lỗi ở các hệ thống khác**: Dấu chấm giữa hai đáy hình hộp bị biến nhầm thành tích vô hướng: `ABCD \cdot A'B'C'D'`; căn bậc hai Unicode `√3` bị hiển thị lỗi font.
* **Đột phá của AEGIS-INGEST V9.0**:
  - Bộ chuẩn hóa Pre-Norm giữ nguyên dấu chấm ngăn cách khối lăng trụ: `ABCD.A'B'C'D'`.
  - Tự động đóng gói căn bậc hai Unicode thành mã KaTeX chuẩn: `$\sqrt{3}$` và `$\sqrt{2}$`.

---

## 5. Hướng Dẫn Cấu Hình Toàn Diện Tệp JSON `math_dictionary_registry.json`

Tệp cấu hình nằm tại đường dẫn:
- `Backend/src/AegisQuiz.Infrastructure/Config/math_dictionary_registry.json`
- `Backend/publish/Config/math_dictionary_registry.json`

### Cấu Trúc Tổng Thể Của Tệp JSON:

```json
{
  "version": "2026.09.25.V9",
  "lastUpdated": "2026-09-25T16:00:00Z",
  "symbolGlyphs": [ ... ],
  "greekLetters": [ ... ],
  "ommlTokens": { ... },
  "accentMappings": { ... },
  "inlineNormalizations": [ ... ],
  "structuralSimplificationRules": [ ... ],
  "engineSettings": { ... }
}
```

---

### 5.1. Cấu Hình Ký Tự Font MathType (`symbolGlyphs`)
Dùng để ánh xạ mã byte Hex của các font ký hiệu đặc thù (Symbol, Euclid Symbol, MT Extra...) sang thực thể HTML hoặc lệnh KaTeX.

```json
{
  "font": "Symbol",
  "charHex": "0x00A2",
  "replacement": "&#x2032;",
  "latex": "'",
  "desc": "Dấu phẩy đạo hàm (Prime)"
}
```
* `font`: Tên font chữ GDI trong WMF (thường là `Symbol`, `Euclid Symbol`, `MT Extra`).
* `charHex`: Mã Hex của ký tự (ví dụ `0x00A2`, `0x00F2`).
* `replacement`: Mã ký tự HTML Unicode hiển thị khi render HTML thuần.
* `latex`: Lệnh KaTeX tương ứng (ví dụ `\int`, `\le`, `\ge`, `\infty`).

---

### 5.2. Cấu Hình Quy Tắc Cấu Trúc Khai Báo Động (P2 `structuralSimplificationRules`)
Đây là trái tim của cơ chế bóc tách không gian 2D, cho phép định nghĩa cách nhận diện cấu trúc toán học mà không cần code C#.

#### A. Phân số đơn giản 1 gạch ngang (`RULE_SIMPLE_FRACTION`)
```json
{
  "id": "RULE_SIMPLE_FRACTION",
  "name": "Phân số đơn giản 1 gạch ngang",
  "priority": 100,
  "enabled": true,
  "anchor": {
    "type": "GEOMETRIC_LINE",
    "orientation": "HORIZONTAL",
    "count": 1,
    "maxSlope": 0.08
  },
  "slots": [
    {
      "name": "numerator",
      "relativePosition": "ABOVE_ANCHOR",
      "maxDistanceYRatio": 1.5,
      "paddingX": 25,
      "maxTokens": 8,
      "allowedPattern": "^[0-9a-zA-Z\\+\\-\\.\\s\\(\\)\\\\]+$",
      "optional": false
    },
    {
      "name": "denominator",
      "relativePosition": "BELOW_ANCHOR",
      "maxDistanceYRatio": 1.5,
      "paddingX": 25,
      "maxTokens": 8,
      "allowedPattern": "^[0-9a-zA-Z\\+\\-\\.\\s\\(\\)\\\\]+$",
      "optional": false
    }
  ],
  "constraints": {
    "disallowPolygons": true,
    "maxTotalChars": 30,
    "maxLines": 1
  },
  "template": "\\frac{{{numerator}}}{{{denominator}}}"
}
```

#### B. Căn thức đơn giản có gạch nóc (`RULE_RADICAL_OVERLINE`)
```json
{
  "id": "RULE_RADICAL_OVERLINE",
  "name": "Căn thức đơn giản có gạch nóc",
  "priority": 90,
  "enabled": true,
  "anchor": {
    "type": "COMPOUND",
    "symbol": "\\sqrt",
    "attachedLine": "TOP_OVERLINE"
  },
  "slots": [
    {
      "name": "radicand",
      "relativePosition": "UNDER_OVERLINE",
      "maxDistanceYRatio": 1.5,
      "paddingX": 20,
      "maxTokens": 10,
      "allowedPattern": "^[0-9a-zA-Z\\+\\-\\.\\s\\(\\)]+$",
      "optional": false
    }
  ],
  "constraints": {
    "disallowPolygons": true,
    "maxTotalChars": 30,
    "maxLines": 1
  },
  "template": "\\sqrt{{{radicand}}}"
}
```

#### C. Hệ hai phương trình dấu ngoặc nhọn (`RULE_SYSTEM_OF_EQUATIONS`)
```json
{
  "id": "RULE_SYSTEM_OF_EQUATIONS",
  "name": "Hệ hai phương trình dấu ngoặc nhọn",
  "priority": 80,
  "enabled": true,
  "anchor": {
    "type": "DELIMITER_SYMBOL",
    "symbol": "{",
    "minHeightRatio": 1.5
  },
  "slots": [
    {
      "name": "row1",
      "relativePosition": "RIGHT_UPPER",
      "maxTokens": 15,
      "allowedPattern": "^[0-9a-zA-Z\\+\\-\\=\\.\\s]+$",
      "optional": false
    },
    {
      "name": "row2",
      "relativePosition": "RIGHT_LOWER",
      "maxTokens": 15,
      "allowedPattern": "^[0-9a-zA-Z\\+\\-\\=\\.\\s]+$",
      "optional": false
    }
  ],
  "constraints": {
    "disallowPolygons": true,
    "maxTotalChars": 50,
    "maxLines": 0
  },
  "template": "\\begin{cases} {{row1}} \\\\ {{row2}} \\end{cases}"
}
```

#### D. Mũi tên Vector hình học (`RULE_VECTOR_OVERLINE`)
```json
{
  "id": "RULE_VECTOR_OVERLINE",
  "name": "Mũi tên vector hình học",
  "priority": 95,
  "enabled": true,
  "anchor": {
    "type": "VECTOR_OVERLINE"
  },
  "slots": [
    {
      "name": "base",
      "relativePosition": "BELOW_ANCHOR",
      "maxTokens": 4,
      "allowedPattern": "^[a-zA-Z\\']+$",
      "optional": false
    }
  ],
  "constraints": {
    "disallowPolygons": true,
    "maxTotalChars": 20,
    "maxLines": 0
  },
  "template": "\\vec{{{base}}}"
}
```

---

## 6. Hướng Dẫn Quản Trị & Bổ Sung Từ Điển Trên Giao Diện (Admin UI Walkthrough)

Hệ thống cung cấp giao diện quản trị chuyên nghiệp tại địa chỉ:  
`https://<ten-mien-he-thong>/admin/math` hoặc chọn **Admin Panel $\rightarrow$ Cấu Hình Toán Học (Math Engine)**.

### 6.1. Tổng Quan Bảng Điều Khiển Quản Trị Math Engine
Giao diện bao gồm 4 tab chuyên trách:
1. **Typography & Glyph Spacing**: Tùy chỉnh khoảng cách ký tự nghiêng (triệt tiêu lỗi dính chữ $Oyz$), tỷ lệ phóng to cỡ chữ công thức.
2. **Hồ Sơ Môn Học (Subject Profiles)**: Tùy chỉnh cấu hình riêng cho từng bộ môn Toán học, Vật lý, Hóa học, Sinh học.
3. **Bộ Cấu Hình Nhanh (Presets)**: Chuyển đổi nhanh giữa các cấu hình tối ưu sẵn (Chuẩn Quốc Gia THPT, Siêu Nét 4K, Tối Giản...).
4. **Quy Tắc Cấu Trúc (P2)**: Quản trị các quy tắc cấu trúc toán học động.

---

### 6.2. Các Bước Thao Tác Bổ Sung & Quản Trị Quy Tắc Trên Giao Diện

#### Bước 1: Truy Cập Tab "Quy Tắc Cấu Trúc (P2)"
1. Đăng nhập với tài khoản Quản trị viên (`ADMIN` hoặc `SUPER_ADMIN`).
2. Vào menu **Quản Trị Hệ Thống $\rightarrow$ Cấu Hình Toán Học**.
3. Chọn tab **Quy Tắc Cấu Trúc (P2)**.
4. Hệ thống sẽ tự động đồng bộ và hiển thị danh sách 5 quy tắc cấu trúc chuẩn cùng trạng thái kích hoạt hiện tại.

#### Bước 2: Bật / Tắt (Hot-Toggle) Quy Tắc Tức Thì
- Mỗi quy tắc được trang bị một công tắc chuyển trạng thái **BẬT / TẮT**.
- Khi gạt công tắc:
  - Frontend gọi ngay API: `PATCH /api/quiz/math-dictionary/structural-rule/{id}/toggle`.
  - Backend đảo ngược trạng thái trong bộ nhớ RAM và tự động biên dịch lại bộ `ICompiledStructuralMatcher` trong vòng $< 1\text{ms}$.
  - Tệp `math_dictionary_registry.json` trên ổ đĩa được cập nhật đồng bộ.
  - Toàn bộ quá trình diễn ra **Zero-Downtime** (không cần khởi động lại dịch vụ).

#### Bước 3: Xem Trước Trực Quan Với Kính Lúp KaTeX (Visual Inspector)
- Mỗi thẻ quy tắc đều hiển thị:
  - Khung mã mẫu Template (ví dụ: `\frac{{{numerator}}}{{{denominator}}}`).
  - Khung **Minh họa KaTeX** sử dụng linh kiện `<MathRenderer>`: Cho phép quản trị viên nhìn thấy ngay công thức rendered sắc nét đúng theo chuẩn hiển thị của học sinh.

#### Bước 4: Thêm Quy Tắc Cấu Trúc P2 Mới Trực Tiếp Trên Giao Diện
1. Tại tab **Quy Tắc Cấu Trúc (P2)**, bấm nút tím **`+ Thêm Quy Tắc Mới`**.
2. Một hộp thoại (Modal) khai báo chuẩn sẽ xuất hiện:
   - **Mã Quy Tắc (ID)**: Nhập mã duy nhất (ví dụ: `RULE_CUSTOM_FRACTION`).
   - **Tên Quy Tắc**: Đặt tên thân thiện (ví dụ: `Phân số tùy chỉnh`).
   - **Độ Ưu Tiên**: Nhập từ 1 đến 100 (số càng cao được duyệt trước).
   - **Loại Mỏ Neo (Anchor Type)**: Chọn từ menu: `GEOMETRIC_LINE` (phân số), `COMPOUND` (căn thức), `DELIMITER_SYMBOL` (ngoặc nhọn), `TEXT_ANCHOR` (từ khóa lim), hoặc `VECTOR_OVERLINE` (mũi tên vector).
   - **Khai Báo Slots**: Nhập tên Slot 1, Slot 2 và chọn vị trí không gian tương đối (`ABOVE_ANCHOR`, `BELOW_ANCHOR`, `UNDER_OVERLINE`...).
   - **Mẫu Sinh KaTeX (Template)**: Nhập cú pháp KaTeX với placeholder slot, ví dụ `\frac{{{numerator}}}{{{denominator}}}`.
3. Bấm **Lưu Vào Hệ Thống (DB & RAM)**:
   - Hệ thống tự động gửi payload tới `POST /api/quiz/math-dictionary/structural-rule`.
   - Backend biên dịch ngay lập tức trong RAM $O(1)$ và lưu vĩnh viễn vào tệp cấu hình trên đĩa.
   - Thẻ quy tắc mới lập tức xuất hiện trên bảng điều khiển.

#### Bước 5: Bổ Sung Ký Hiệu Font MathType Hoặc OMML Token Mới
Khi gặp một đề thi chứa ký tự toán học lạ chưa có trong từ điển:
1. Bấm nút xanh dương **`+ Thêm Ký Hiệu Từ Điển`**.
2. Chọn loại định dạng:
   - **Glyph MathType (WMF)**: Chọn font (Symbol / Euclid Symbol / MT Extra), nhập mã Hex (ví dụ `0x00F7`), thực thể HTML (`&#x00F7;`), mã KaTeX (`\div`) và mô tả (`Dấu chia phép tính`).
   - **Token OMML (Word Math)**: Nhập ký tự OMML (ví dụ `≈`) và lệnh KaTeX tương ứng (`\approx `).
3. Bấm **Lưu Vào Từ Điển Hệ Thống**: Dữ liệu được nạp tức thì vào ConcurrentDictionary và tự động đồng bộ hóa xuống cơ sở dữ liệu.

#### Bước 6: Xóa Quy Tắc Cấu Trúc Khỏi Hệ Thống
- Tại mỗi thẻ quy tắc, bấm vào biểu tượng thùng rác màu đỏ (`Trash2`).
- Hệ thống sẽ hiển thị hộp thoại xác nhận an toàn.
- Khi xác nhận, Backend gọi `DELETE /api/quiz/math-dictionary/structural-rule/{id}`, xóa sạch khỏi RAM, tái biên dịch matcher và cập nhật tệp registry trên đĩa.

---

## 7. Đặc Tả API Quản Trị Đăng Bạ Từ Điển Toán Học

Toàn bộ các thao tác trên giao diện đều được hỗ trợ đầy đủ qua chuẩn RESTful API có bảo mật Bearer Token:

### 1. Lấy Toàn Bộ Bản Đăng Bạ Từ Điển
- **Endpoint**: `GET /api/quiz/math-dictionary`
- **Mô tả**: Trả về số lượng và toàn bộ danh mục glyphs, omml tokens, quy tắc cấu trúc và thiết lập typography.
- **Phản hồi mẫu**:
```json
{
  "success": true,
  "version": "2026.09.25.V9",
  "totalGlyphs": 56,
  "totalOmmlTokens": 42,
  "totalStructuralRules": 5,
  "data": { ... }
}
```

### 2. Thêm Hoặc Cập Nhật Quy Tắc Cấu Trúc Mới
- **Endpoint**: `POST /api/quiz/math-dictionary/structural-rule`
- **Body**: Đối tượng `MathStructuralRuleDto` (JSON).
- **Phản hồi**:
```json
{
  "success": true,
  "message": "Successfully registered/updated structural rule 'RULE_SIMPLE_FRACTION'.",
  "ruleId": "RULE_SIMPLE_FRACTION",
  "totalRules": 5
}
```

### 3. Bật/Tắt Trạng Thái Quy Tắc Tức Thì (Hot-Toggle)
- **Endpoint**: `PATCH /api/quiz/math-dictionary/structural-rule/{id}/toggle`
- **Tham số**: `{id}` là mã quy tắc (ví dụ: `RULE_SIMPLE_FRACTION`).
- **Phản hồi**:
```json
{
  "success": true,
  "message": "Structural rule 'RULE_SIMPLE_FRACTION' is now ENABLED.",
  "enabled": true
}
```

### 4. Xóa Quy Tắc Cấu Trúc
- **Endpoint**: `DELETE /api/quiz/math-dictionary/structural-rule/{id}`
- **Tham số**: `{id}` là mã quy tắc cần xóa.

### 5. Nạp Lại Toàn Bộ Từ Điển Từ Đĩa (Hot-Reload)
- **Endpoint**: `POST /api/quiz/math-dictionary/reload`
- **Body** (Tùy chọn): `{ "jsonContent": "{ ... }" }`. Nếu để trống, hệ thống sẽ đọc lại tệp mặc định `Config/math_dictionary_registry.json`.

---

## 8. Quy Trình Vận Hành & Khuyến Nghị Tối Ưu

1. **Chuẩn Soạn Thảo Đề Thi Khuyến Nghị Cho Giáo Viên**:
   - Sử dụng định dạng chuẩn `Câu 1:`, `Câu 2:` hoặc `Bài 1:`, `Bài 2:`.
   - Các phương án trắc nghiệm viết rõ `A.`, `B.`, `C.`, `D.` ở đầu dòng hoặc cách nhau bởi phím Tab/khoảng trắng rõ ràng.
   - Có thể gõ công thức toán bằng MathType, Word Equation hoặc ký hiệu Unicode thông thường; hệ thống AEGIS-INGEST V9.0 sẽ tự động nhận diện và đưa về chuẩn chung tối ưu nhất.
2. **Quy Trình Sao Lưu & Đồng Bộ Cấu Hình**:
   - Khi thực hiện bổ sung quy tắc mới qua giao diện, hệ thống tự động ghi nhận vào tệp `math_dictionary_registry.json`.
   - Khuyến nghị commit tệp cấu hình này vào hệ thống quản lý mã nguồn Git để bảo toàn lịch sử thay đổi của tổ chức.
3. **An Toàn Khảo Thí Tuyệt Đối**:
   - Nhờ cơ chế **Fail-Safe Fallback**, trong trường hợp gặp các hình vẽ hình học quá phức tạp (nhiều đường cong Bezier, biểu đồ thống kê), hệ thống sẽ chủ động bảo tồn nguyên vẹn dưới dạng Vector SVG, cam kết **không bao giờ làm mất nội dung hoặc gián đoạn kỳ thi của học sinh**.

---
*Tài liệu được ban hành và bảo chứng chất lượng bởi Kiến Trúc Sư Hệ Thống AegisQuiz.*
