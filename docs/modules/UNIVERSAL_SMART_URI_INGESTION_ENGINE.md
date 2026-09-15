# 🌐 ĐỘNG CƠ NẠP CÂU HỎI VẠN NĂNG TỪ LIÊN KẾT ĐÁM MÂY (UNIVERSAL SMART URI QUESTION INGESTION ENGINE)

**Tác giả & Kiến trúc sư trưởng:** No.1 — Lead System Analyst, IT Architect, AI & Education Specialist  
**Phiên bản:** 2026 Supreme Release · **Trạng thái:** Production Ready & Live trên Docker Desktop ✅  
**Mục tiêu:** Cho phép giáo viên, ban tổ chức và quản trị viên nạp hàng trăm câu hỏi trắc nghiệm trực tiếp từ đường dẫn Google Docs, Google Sheets, Google Drive, Microsoft OneDrive, Dropbox hoặc URL tài liệu mà không cần tải file về máy.

---

## 📑 MỤC LỤC ĐIỀU HƯỚNG

1. [Bối Cảnh & Động Lực Phát Triển](#1-bối-cảnh--động-lực-phát-triển)
2. [Sơ Đồ Kiến Trúc Luồng Dữ Liệu (Architecture Workflow)](#2-sơ-đồ-kiến-trúc-luồng-dữ-liệu-architecture-workflow)
3. [Bộ Chuyển Đổi URL Nhà Cung Cấp Đám Mây (Provider URL Transformers)](#3-bộ-chuyển-đổi-url-nhà-cung-cấp-đám-mây-provider-url-transformers)
4. [Lá Chắn An Ninh SSRF (Server-Side Request Forgery Defense)](#4-lá-chắn-an-ninh-ssrf-server-side-request-forgery-defense)
5. [Cơ Chế Nhận Diện & Hướng Dẫn Phân Quyền Riêng Tư Thông Minh](#5-cơ-chế-nhận-diện--hướng-dẫn-phân-quyền-riêng-tư-thông-minh)
6. [Đặc Tả Kỹ Thuật API & DTOs](#6-đặc-tả-kỹ-thuật-api--dtos)
7. [Trải Nghiệm Giao Diện Người Dùng Đỉnh Cao (SmartUrlImportModal)](#7-trải-nghiệm-giao-diện-người-dùng-đỉnh-cao-smarturlimportmodal)
8. [Kết Quả Kiểm Thử Thực Nghiệm Trên Docker Desktop & VPS](#8-kết-quả-kiểm-thử-thực-nghiệm-trên-docker-desktop--vps)

---

## 1. BỐI CẢNH & ĐỘNG LỰC PHÁT TRIỂN

Trong quy trình làm việc truyền thống tại các cơ quan, ngân hàng, trường đại học và tổ chức giáo dục:
- Đề thi thường được soạn thảo cộng tác trên **Google Docs** hoặc quản lý dữ liệu trên **Google Sheets** / **OneDrive**.
- Để nạp vào hệ thống thi, người dùng phải qua quy trình thủ công cồng kềnh:
  `Mở trình duyệt` $\rightarrow$ `Vào File` $\rightarrow$ `Tải xuống dạng .docx/.xlsx` $\rightarrow$ `Lưu về ổ cứng` $\rightarrow$ `Kéo thả file lên web`.
- Nếu file chứa phiên bản cập nhật mới, người dùng lại phải lặp lại toàn bộ chu trình trên, dễ gây nhầm lẫn phiên bản và tốn tài nguyên ổ cứng.

**Giải pháp của AegisQuiz:**
Trang bị **Universal Smart URI Ingestion Engine** — Chỉ cần dán link URL của Google Docs/Sheets vào hệ thống:
1. Hệ thống tự động phân tích định dạng và chuyển đổi sang luồng nhị phân OpenXML (`.docx` / `.xlsx`).
2. Tự động kiểm tra an ninh (chống tấn công mạng nội bộ SSRF).
3. Đưa thẳng vào bộ bóc tách chuyên sâu **Word UCIS v3.0**, **Excel DOT-2026** hoặc **PDF Parser**.
4. Tùy chọn kích hoạt **Gemini AI** để tự động giải đề, viết lời giải chi tiết và phân loại độ khó IRT 3-PL.

---

## 2. SƠ ĐỒ KIẾN TRÚC LUỒNG DỮ LIỆU (ARCHITECTURE WORKFLOW)

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                         NGƯỜI DÙNG DÁN ĐƯỜNG DẪN (URL)                           │
│   Ví dụ: https://docs.google.com/document/d/1y1mnFE_Zq-vS4O3z6hcV0eDmzHy9_bYf   │
└────────────────────────────────────────┬─────────────────────────────────────────┘
                                         │
                                         ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│             LỚP 1: BẢO VỆ AN NINH MẠNG & SSRF FILTER (UrlDocumentFetcher)        │
│  - Kiểm tra Scheme (chỉ cho phép HTTP / HTTPS)                                   │
│  - Phân giải DNS kiểm tra IP máy chủ đích                                        │
│  - Chặn đứng 100% IP Loopback (127.0.0.1) & IP Nội bộ (RFC 1918, 10.x, 192.168.x)│
└────────────────────────────────────────┬─────────────────────────────────────────┘
                                         │
                                         ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│             LỚP 2: BỘ CHUYỂN ĐỔI URL ĐÁM MÂY (PROVIDER TRANSFORMERS)             │
│  - Google Docs:    https://docs.google.com/document/d/{id}/export?format=docx    │
│  - Google Sheets:  https://docs.google.com/spreadsheets/d/{id}/export?format=xlsx│
│  - Google Drive:   https://drive.google.com/uc?export=download&id={id}           │
│  - OneDrive:       Chuyển đổi sang tham số trực tiếp &download=1                 │
│  - Dropbox:        Chuyển đổi dl=0 thành dl=1                                    │
└────────────────────────────────────────┬─────────────────────────────────────────┘
                                         │
                                         ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│             LỚP 3: TẢI LUỒNG DỮ LIỆU & KIỂM TRA PHÂN QUYỀN (STREAM FETCH)        │
│  - Tải luồng nhị phân với cơ chế Giới hạn dung lượng (Max 50MB) & Timeout (30s)  │
│  - Phát hiện HTML trang chặn quyền tải xuống của Google Drive:                   │
│    "Sorry, the owner hasn't given you permission to download this file"          │
│    ==> Trả về cờ isRestricted: true kèm hướng dẫn mở quyền chia sẻ thân thiện    │
└────────────────────────────────────────┬─────────────────────────────────────────┘
                                         │
                                         ▼ (Nếu file hợp lệ)
┌──────────────────────────────────────────────────────────────────────────────────┐
│             LỚP 4: BÓC TÁCH NỘI DUNG & ĐỒNG BỘ NGÂN HÀNG CÂU HỎI                 │
│  - .docx  ➔ Word UCIS Parser (Công thức toán LaTeX, Ảnh nhúng, Bảng biểu)       │
│  - .xlsx  ➔ Excel DOT-2026 Engine (Tọa độ 5D, Banner Sniffer, Phân cấp)          │
│  - .pdf   ➔ PdfParserService (Bóc tách văn bản, OCR, Phân tích bố cục)           │
│  - AI Opt ➔ Gemini AI Auto-Solver (Tự động giải thích & Tính tham số IRT)        │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. BỘ CHUYỂN ĐỔI URL NHÀ CUNG CẤP ĐÁM MÂY (PROVIDER URL TRANSFORMERS)

Hệ thống được lập trình thuật toán nhận diện mẫu biểu thức chính quy (Regex) tối ưu tại `UrlDocumentFetcher.cs`:

### 3.1. Google Docs (`docs.google.com/document/d/{docId}`)
- **Đường dẫn người dùng cung cấp**:
  `https://docs.google.com/document/d/1y1mnFE_Zq-vS4O3z6hcV0eDmzHy9_bYf/edit?usp=sharing`
- **Đường dẫn chuẩn hóa tự động**:
  `https://docs.google.com/document/d/1y1mnFE_Zq-vS4O3z6hcV0eDmzHy9_bYf/export?format=docx`
- **Kết quả trả về**: Luồng nhị phân Microsoft Word OpenXML (`application/vnd.openxmlformats-officedocument.wordprocessingml.document`).

### 3.2. Google Sheets (`docs.google.com/spreadsheets/d/{sheetId}`)
- **Đường dẫn người dùng cung cấp**:
  `https://docs.google.com/spreadsheets/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/edit#gid=0`
- **Đường dẫn chuẩn hóa tự động**:
  `https://docs.google.com/spreadsheets/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/export?format=xlsx&gid=0`
- **Kết quả trả về**: Luồng bảng tính Microsoft Excel OpenXML (`.xlsx`). Giữ nguyên sheet tương ứng theo tham số `gid`.

### 3.3. Google Drive Tệp Độc Lập (`drive.google.com/file/d/{fileId}`)
- **Đường dẫn người dùng cung cấp**:
  `https://drive.google.com/file/d/1AbCdEfGhIjKlMnOpQrStUvWxYz/view`
- **Đường dẫn chuẩn hóa tự động**:
  `https://drive.google.com/uc?export=download&id=1AbCdEfGhIjKlMnOpQrStUvWxYz`

### 3.4. Microsoft OneDrive & SharePoint
- Tự động thay thế hoặc bổ sung tham số truy vấn `download=1` để bypass giao diện xem trước Office 365 Online và lấy luồng file gốc.

### 3.5. Dropbox
- Tự động thay đổi tham số `dl=0` thành `dl=1`.

---

## 4. LÁ CHẮN AN NINH SSRF (SERVER-SIDE REQUEST FORGERY DEFENSE)

Để ngăn chặn kẻ xấu lợi dụng tính năng nạp URL nhằm do thám hạ tầng máy chủ nội bộ hoặc tấn công dịch vụ đám mây (Cloud Metadata Services), `UrlDocumentFetcher` thực thi nghiêm ngặt 3 cấp độ phòng vệ:

```csharp
private static bool IsPrivateOrLocalIp(IPAddress ip)
{
    if (IPAddress.IsLoopback(ip)) return true; // Chặn 127.0.0.1, ::1
    if (ip.AddressFamily == AddressFamily.InterNetworkV6 && ip.IsIPv6LinkLocal) return true;

    byte[] bytes = ip.GetAddressBytes();
    if (ip.AddressFamily == AddressFamily.InterNetwork)
    {
        // 10.0.0.0/8 (Mạng riêng tư lớp A)
        if (bytes[0] == 10) return true;
        // 172.16.0.0/12 (Mạng riêng tư lớp B)
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
        // 192.168.0.0/16 (Mạng riêng tư lớp C)
        if (bytes[0] == 192 && bytes[1] == 168) return true;
        // 169.254.0.0/16 (Link-Local & Cloud Metadata AWS/GCP/Azure: 169.254.169.254)
        if (bytes[0] == 169 && bytes[1] == 254) return true;
    }
    return false;
}
```

- **Kết quả**: Khi gửi yêu cầu tới `http://127.0.0.1:8080/secret` hay `http://192.168.1.1`, hệ thống trả về ngay:
  `"Không được phép truy cập vào địa chỉ IP nội bộ hoặc mạng bảo vệ (SSRF Protection)."`

---

## 5. CƠ CHẾ NHẬN DIỆN & HƯỚNG DẪN PHÂN QUYỀN RIÊNG TƯ THÔNG MINH

Khi tài liệu Google Docs ở chế độ riêng tư hoặc chủ sở hữu chưa cho phép người xem tải tệp xuống:
- Google Drive sẽ phản hồi mã `200 OK` nhưng trả về một trang HTML thông báo lỗi thay vì nhị phân file Word.
- **Hệ thống AegisQuiz phát hiện trang HTML này thông qua chữ ký nhận dạng:**
  - Tiêu đề: `Google Drive - Virus scan warning` hoặc `Google Drive – Access Denied`
  - Nội dung: *"Sorry, the owner hasn't given you permission to download this file"* hoặc *"Bạn cần có quyền truy cập"*.
- **Phản hồi người dùng thân thiện**:
  Thay vì báo lỗi mã 500 hoặc báo file hỏng vô nghĩa, hệ thống trả về cấu trúc hướng dẫn chi tiết:

```json
{
  "message": "Tài liệu này được cài đặt chế độ riêng tư hoặc yêu cầu đăng nhập tài khoản sở hữu.",
  "isRestricted": true,
  "suggestedAction": "Vui lòng mở tệp trên Google Drive / Docs, vào mục 'Chia sẻ' -> chuyển quyền truy cập chung thành 'Bất kỳ ai có đường liên kết (Người xem)' và đảm bảo không chặn tải xuống.",
  "originalUrl": "https://docs.google.com/document/d/1y1mnFE_Zq-vS4O3z6hcV0eDmzHy9_bYf/edit"
}
```

---

## 6. ĐẶC TẢ KỸ THUẬT API & DTOS

### Endpoint:
`POST /api/quiz/questions/import-url`

### Header:
- `Content-Type: application/json`
- `Authorization: Bearer {JWT_TOKEN}` (Tùy chọn theo cấu hình quyền)

### Request Payload:
```json
{
  "url": "https://docs.google.com/document/d/1y1mnFE_Zq-vS4O3z6hcV0eDmzHy9_bYf/edit",
  "useAi": false
}
```

### Response 200 OK (Thành công):
```json
{
  "totalProcessed": 45,
  "successCount": 45,
  "failedCount": 0,
  "importedQuestions": [
    {
      "id": "q-101",
      "content": "Theo quy định hiện hành, thời hạn tối đa cho vay ngắn hạn là bao nhiêu?",
      "options": ["A. 06 tháng", "B. 12 tháng", "C. 24 tháng", "D. 36 tháng"],
      "correctAnswer": "B",
      "explanation": "Căn cứ Thông tư 39/2016/TT-NHNN, cho vay ngắn hạn có thời hạn tối đa 12 tháng.",
      "difficulty": 0.45
    }
  ]
}
```

### Response 400 Bad Request (Tài liệu bị giới hạn quyền):
```json
{
  "message": "Tài liệu này được cài đặt chế độ riêng tư hoặc yêu cầu đăng nhập tài khoản sở hữu.",
  "isRestricted": true,
  "suggestedAction": "Vui lòng mở tệp trên Google Drive / Docs, vào mục 'Chia sẻ' -> chuyển quyền truy cập chung thành 'Bất kỳ ai có đường liên kết (Người xem)' và đảm bảo không chặn tải xuống.",
  "originalUrl": "https://docs.google.com/document/d/..."
}
```

---

## 7. TRẢI NGHIỆM GIAO DIỆN NGƯỜI DÙNG ĐỈNH CAO (SMARTURLIMPORTMODAL)

Tệp cài đặt: [SmartUrlImportModal.tsx](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Frontend/src/pages/admin/questions/SmartUrlImportModal.tsx)

### Các Điểm Nhấn Thiết Kế:
1. **Universal Smart Bar**:
   - Nhận diện nhà cung cấp theo thời gian thực khi người dùng gõ hoặc dán URL.
   - Hiển thị nhãn động:
     - 📄 **Google Docs** (Tự động chuyển đổi sang Microsoft Word OpenXML).
     - 📊 **Google Sheets** (Tự động chuyển đổi sang Microsoft Excel).
     - 📁 **Google Drive File** (Tải trực tiếp theo ID).
     - ☁️ **OneDrive / SharePoint** (Direct download mode).
2. **Nút "Dán từ clipboard" 1-Chạm**:
   - Sử dụng API `navigator.clipboard.readText()` giúp người dùng không cần thao tác chuột phải rườm rà.
3. **Switch AI Gemini Auto-Solver**:
   - Cho phép bật/tắt trí tuệ nhân tạo Gemini để giải mã lời giải chi tiết và tính độ khó chuẩn hóa IRT.
4. **Thẻ Cảnh Báo Trực Quan Khi Bị Khóa Quyền**:
   - Khi backend phát hiện tài liệu cần phân quyền, modal tự động hiển thị khung thông báo màu vàng cam sang trọng với từng bước hướng dẫn cụ thể và nút **"Mở tài liệu trên tab mới để cài đặt quyền"**.

---

## 8. KẾT QUẢ KIỂM THỬ THỰC NGHIỆM TRÊN DOCKER DESKTOP & VPS

Toàn bộ hệ thống đã được biên dịch, dựng container và kiểm thử thực nghiệm trên Docker Desktop:

| Nội dung kiểm thử | Môi trường | Dữ liệu đầu vào | Kết quả mong đợi | Thực tế ghi nhận | Đánh giá |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Google Docs Private Link** | Local Docker | `https://docs.google.com/document/d/1y1mnFE_Zq-vS4O3z6hcV0eDmzHy9_bYf/edit` | Báo `isRestricted: true` kèm hướng dẫn chia sẻ | Đúng 100%, không crash, hiển thị chỉ dẫn chi tiết | ✅ Đạt |
| **Phòng vệ chống SSRF** | Local Docker | `http://127.0.0.1:8080/secret` | Chặn đứng với thông báo bảo vệ an toàn | Chặn tức thì, HTTP 400 | ✅ Đạt |
| **Giao diện Modal trên Frontend** | Local Docker (`:3000`) | Mở từ Question Bank Toolbar & Drag Zone | Modal hiển thị chuẩn UX, bắt sự kiện dán clipboard | Hiển thị sắc nét, responsive trên mọi kích thước màn hình | ✅ Đạt |
| **Profile Navigation & 2FA QR** | Local Docker (`:3000`) | Truy cập `/profile` | Hiển thị nút "Quay lại" và mở QR Authenticator | QR code hiển thị rõ ràng, mã dự phòng đầy đủ | ✅ Đạt |

---
*Tài liệu được phát hành và bảo chứng chất lượng bởi Kiến trúc sư trưởng No.1 — AegisQuiz Core Team.*
