# 🔌 Đặc Tả Giao Diện Lập Trình REST API (REST API Reference)

> **Giao thức**: HTTPS RESTful JSON  
> **Base URL**: `http://localhost:5000/api` (Local) hoặc `https://api.aegisquiz.internal/api` (Production)  
> **Xác thực**: Bearer Token trong Header `Authorization: Bearer <JWT>`

---

## 1. Xác Thực & Người Dùng (`AuthController`, `PkiAuthController`)

### `POST /api/auth/login`
- Đăng nhập hệ thống bằng email và mật khẩu.
- **Request Body**:
  ```json
  { "email": "student@dehoc.vn", "password": "SecretPassword123" }
  ```
- **Response (200 OK)**:
  ```json
  { "token": "eyJhbGciOi...", "expiresIn": 86400, "user": { "id": "u1", "name": "Nguyễn Văn A", "role": "Student" } }
  ```

### `POST /api/auth/register`
- Đăng ký tài khoản người học mới.

### `POST /api/auth/pki/verify`
- Xác thực chữ ký số bằng khóa công khai (PKI).

---

## 2. Quản Lý Ngân Hàng Câu Hỏi & Đề Thi (`QuestionsController`, `TopicsController`)

### `GET /api/questions`
- Lấy danh sách câu hỏi phân trang theo chủ đề và độ khó.
- **Query Params**: `page=1&pageSize=20&topicId=uuid&difficulty=1.5`.

### `POST /api/questions/upload-docx`
- Tải lên file Word (.docx) để hệ thống tự động bóc tách đề thi và công thức toán học.
- **Content-Type**: `multipart/form-data`.
- **Form Data**: `file: [binary .docx]`.
- **Response**: Danh sách các câu hỏi đã bóc tách thành công kèm ảnh công thức và phương án.

### `GET /api/topics`
- Lấy cây danh mục lộ trình kiến thức và chương mục.

---

## 3. Quản Lý Bài Thi & Lượt Thi (`AttemptController`, `AdaptiveController`)

### `POST /api/attempt/start`
- Bắt đầu một lượt thi mới (Standard hoặc Mock Test).
- **Request Body**: `{ "examId": "exam-123" }`.
- **Response**: Trả về `attemptId`, thời gian giới hạn và danh sách câu hỏi.

---

## 6. Quản Lý Cây Phân Cấp Tổ Chức Đa Tầng (`OrganizationUnitsController`)

> **Base Route**: `/api/quiz/org-units`  
> **Áp dụng**: Kỳ quan 16 — Quản trị phân tầng 5 cấp (Hội sở -> Vùng -> Chi nhánh -> Phòng ban -> Lớp học).

### `GET /api/quiz/org-units/tree`
- Lấy toàn bộ cây tổ chức phân cấp lồng nhau (Hierarchical Tree) của Tenant hiện tại.
- **Headers**: `Authorization: Bearer <token>`, `X-Tenant-ID: <uuid>`
- **Response (200 OK)**:
  ```json
  [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "code": "HO",
      "name": "Hội sở chính Agribank",
      "unitType": "HeadOffice",
      "hierarchyPath": "/HO",
      "children": [
        {
          "id": "e4d29a1b-...",
          "parentId": "3fa85f64-...",
          "code": "MB_HN",
          "name": "Khu vực Miền Bắc - Hà Nội",
          "unitType": "Region",
          "hierarchyPath": "/HO/MB_HN",
          "children": [
            {
              "id": "f8a12c3d-...",
              "parentId": "e4d29a1b-...",
              "code": "CN_HOANKIEM",
              "name": "Chi nhánh Hoàn Kiếm",
              "unitType": "Branch",
              "hierarchyPath": "/HO/MB_HN/CN_HOANKIEM",
              "children": []
            }
          ]
        }
      ]
    }
  ]
  ```

### `GET /api/quiz/org-units`
- Lấy danh sách phẳng tất cả đơn vị trực thuộc Tenant (phục vụ bộ lọc, dropdown).
- **Query Params**: `unitType=3` (tùy chọn lọc theo loại đơn vị: 1: HeadOffice, 2: Region, 3: Branch, 4: Department, 5: Classroom).

### `POST /api/quiz/org-units`
- Tạo mới một đơn vị trong cây phân cấp. Hệ thống tự động tính toán `hierarchyPath`.
- **Request Body**:
  ```json
  {
    "parentId": "3fa85f64-...",
    "code": "CN_DONGDA",
    "name": "Chi nhánh Đống Đa",
    "unitType": 3,
    "email": "dongda@agribank.com.vn",
    "phoneNumber": "024.3852.xxxx"
  }
  ```

### `PUT /api/quiz/org-units/{id}`
- Cập nhật thông tin đơn vị tổ chức (tên, liên hệ, thứ tự hiển thị).

### `DELETE /api/quiz/org-units/{id}`
- Vô hiệu hóa đơn vị tổ chức (Chặn xóa nếu còn đơn vị con trực thuộc để bảo vệ tính toàn vẹn cây).

---

## 7. Quản Lý Ngành Động & Tọa Độ Tri Thức (`DomainsController`)

> **Base Route**: `/api/quiz/domains`  
> **Áp dụng**: Kỳ quan 16 — Động cơ Tri thức Đa ngành Động & Hệ Tọa độ Tri thức Đặc thù.

### `GET /api/quiz/domains`
- Lấy danh sách các ngành nghề ĐANG BẬT cho Tenant hiện tại.
- Tự động fallback về 7 ngành chuẩn toàn cầu nếu Tenant chưa cấu hình riêng.
- **Response (200 OK)**:
  ```json
  [
    {
      "code": "BANKING",
      "name": "Tài chính - Ngân hàng",
      "description": "Nghiệp vụ tín dụng, thanh toán, ngân quỹ, quản trị rủi ro",
      "icon": "Landmark",
      "colorBadge": "#059669",
      "isSystemStandard": true,
      "displayOrder": 1
    },
    {
      "code": "AGRI_SPECIAL",
      "name": "Tín dụng Nông nghiệp Nông thôn",
      "description": "Nghiệp vụ đặc thù Tam nông theo Nghị định 55",
      "icon": "Wheat",
      "colorBadge": "#16a34a",
      "isSystemStandard": false,
      "displayOrder": 2
    }
  ]
  ```

### `GET /api/quiz/domains/catalog`
- Lấy toàn bộ danh mục ngành (gồm cả ngành đã bật và tắt) để Quản trị viên cấu hình.
- **Response**: Trả về danh sách kèm thuộc tính `isEnabled: true/false` và `customDisplayName`.

### `PUT /api/quiz/domains/configs`
- Cập nhật trạng thái bật/tắt ngành của Tenant.
- **Request Body**:
  ```json
  [
    { "domainCode": "BANKING", "isEnabled": true, "customDisplayName": "Nghiệp vụ Agribank", "displayOrder": 1 },
    { "domainCode": "IT_SECURITY", "isEnabled": true, "displayOrder": 2 },
    { "domainCode": "HEALTHCARE", "isEnabled": false, "displayOrder": 99 }
  ]
  ```

### `POST /api/quiz/domains`
- Tạo mới một ngành hoặc tiểu ngành đặc thù cho Tenant.
- **Request Body**:
  ```json
  {
    "code": "AGRI_CREDIT",
    "name": "Tín dụng Nông nghiệp",
    "description": "Chuyên môn thẩm định dự án nông lâm ngư nghiệp",
    "icon": "Sprout",
    "colorBadge": "#15803d",
    "parentDomainCode": "BANKING"
  }
  ```

### `GET /api/quiz/domains/presets`
- Lấy danh sách bộ tọa độ tri thức đặc thù của Tenant.
- **Query Params**: `domainCode=BANKING&coordinateType=TARGET_LEVEL`.

### `POST /api/quiz/domains/presets`
- Thêm mới tọa độ tri thức kèm từ đồng nghĩa cho AI tự động học:
- **Request Body**:
  ```json
  {
    "domainCode": "BANKING",
    "coordinateType": "TARGET_LEVEL",
    "presetCode": "KHDN",
    "presetLabel": "Tín dụng Khách hàng Doanh nghiệp",
    "synonymsJson": "[\"KHDN\", \"Tín dụng doanh nghiệp\", \"Corporate Credit\"]"
  }
  ```

### `DELETE /api/quiz/domains/presets/{id}`
- Xóa bộ tọa độ tri thức tùy biến.

### `POST /api/attempt/finalize`
- Nộp toàn bộ bài thi và tính toán điểm tổng kết.

### `POST /api/adaptive/next-question`
- Gọi thuật toán IRT để lấy câu hỏi thích ứng tiếp theo dựa trên năng lực $\theta$.

---

## 4. Phân Hệ Đấu Trường Gameshow (`ArenaController`)

### `GET /api/arena/games`
- Lấy danh sách tất cả các thể thức Gameshow được hỗ trợ cùng metadata (Olympia, Rung Chuông Vàng, Chiếc Nón Kỳ Diệu...).

### `GET /api/arena/rooms`
- Lấy danh sách các phòng thi đấu đang mở.

### `GET /api/arena/rooms/{roomId}`
- Lấy chi tiết trạng thái phòng thi đấu, danh sách người chơi và vòng thi hiện tại.

### `POST /api/arena/rooms`
- Tạo phòng thi đấu mới.
- **Request Body**:
  ```json
  {
    "gameCode": "OLYMPIA",
    "roomName": "Tranh Tài Khối 12",
    "hostPlayerName": "Thầy Tuấn"
  }
  ```
- **Response**: Trạng thái phòng thi đấu `ArenaRoomState` ban đầu.
