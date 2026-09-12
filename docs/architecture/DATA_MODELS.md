# 📊 Đặc Tả Lược Đồ Dữ Liệu & Mô Hình Thực Thể (Data Models & Schema)

Tài liệu này mô tả chi tiết kiến trúc dữ liệu của **AegisQuiz**, bao gồm cả **Cơ sở dữ liệu Quan hệ Lâu dài (PostgreSQL via EF Core)** và **Cấu trúc Trạng thái Bộ nhớ Tạm thời (In-Memory RAM State)**.

---

## 1. Sơ Đồ Thực Thể Quan Hệ Tổng Thể (ERD)

```mermaid
erDiagram
    USERS ||--o{ QUIZ_ATTEMPTS : takes
    USERS ||--o{ USER_ACHIEVEMENTS : earns
    USERS ||--o{ NOTEBOOKS : owns
    EXAMS ||--o{ QUESTIONS : contains
    EXAMS ||--o{ QUIZ_ATTEMPTS : instantiated_by
    QUESTIONS ||--o{ ATTEMPT_ANSWERS : evaluated_in
    QUIZ_ATTEMPTS ||--o{ ATTEMPT_ANSWERS : records
    TOPICS ||--o{ QUESTIONS : categorizes

    USERS {
        uuid id PK
        string username
        string email
        string password_hash
        string role
        string ou
        boolean is_premium
        timestamp created_at
    }

    QUESTIONS {
        uuid id PK
        string content
        string question_type
        jsonb options
        string correct_answer
        text explanation
        float irt_b_difficulty
        float irt_a_discrimination
        float irt_c_guessing
        uuid topic_id FK
        timestamp created_at
    }

    EXAMS {
        uuid id PK
        string title
        string description
        int duration_minutes
        int total_questions
        float pass_percentage
        boolean is_secure_mode
        timestamp created_at
    }

    QUIZ_ATTEMPTS {
        uuid id PK
        uuid user_id FK
        uuid exam_id FK
        timestamp start_time
        timestamp end_time
        float final_score
        float estimated_theta
        string status
    }

    ATTEMPT_ANSWERS {
        uuid id PK
        uuid attempt_id FK
        uuid question_id FK
        string user_answer
        boolean is_correct
        int elapsed_ms
        timestamp submitted_at
    }
```

---

## 2. Các Thực Thể Chính Trong PostgreSQL

### 2.1. Bảng `Questions` (Ngân Hàng Câu Hỏi)
- **`Id`** (`UUID`, PK): Khóa chính.
- **`Content`** (`TEXT`): Nội dung đề bài (hỗ trợ định dạng Markdown và công thức toán LaTeX).
- **`QuestionType`** (`VARCHAR(50)`): `MULTIPLE_CHOICE`, `FILL_IN_BLANK`, `TRUE_FALSE`, `MATCHING`.
- **`Options`** (`JSONB`): Danh sách các phương án lựa chọn `["A. ...", "B. ...", "C. ...", "D. ..."]`.
- **`CorrectAnswer`** (`TEXT`): Đáp án chính xác.
- **`Explanation`** (`TEXT`): Lời giải chi tiết từ giáo viên hoặc sinh tự động bởi Gemini AI.
- **Tham số IRT (Item Response Theory)**:
  - `IrtDifficulty` ($b \in [-3.0, +3.0]$): Độ khó của câu hỏi.
  - `IrtDiscrimination` ($a \in [0.5, 2.5]$): Độ phân biệt năng lực người giỏi và người yếu.
  - `IrtGuessing` ($c \in [0.0, 0.35]$): Xác suất đoán mò ngẫu nhiên (mặc định 0.25 cho trắc nghiệm 4 lựa chọn).

### 2.2. Bảng `QuizAttempts` (Lịch Sử Bài Thi)
- Lưu trữ từng lượt thi của thí sinh:
  - `UserId`, `ExamId`, `StartTime`, `EndTime`.
  - `FinalScore`: Điểm thang 10 hoặc thang 100.
  - `EstimatedTheta`: Điểm năng lực chuẩn hóa $\theta \in [-4.0, +4.0]$.
  - `ViolationCount`: Số lần vi phạm an ninh phòng thi (rời màn hình, mở tab mới, cắm màn hình phụ).

---

## 3. Cấu Trúc Dữ Liệu Bộ Nhớ Đấu Trường (Arena In-Memory Models)

Để phục vụ tốc độ phản xạ mili-giây cho các Gameshow (Olympia, Rung Chuông Vàng, Chiếc Nón Kỳ Diệu...), dữ liệu phòng được biểu diễn bằng các lớp C# lưu trong RAM:

### 3.1. `ArenaRoomState`
```csharp
public class ArenaRoomState
{
    public string RoomId { get; set; }           // Mã phòng (VD: "UKJMJI")
    public string GameCode { get; set; }         // "OLYMPIA", "GOLDEN_BELL"...
    public string RoomName { get; set; }         // Tên phòng do Host đặt
    public string HostUserId { get; set; }       // ID của Host/Trọng tài
    public string CurrentStage { get; set; }     // Tên vòng thi (VD: "KHOI_DONG", "VUOT_CNV")
    public int CurrentQuestionIndex { get; set; }// Vị trí câu hỏi hiện tại
    public List<ArenaQuestionItem> Questions { get; set; } // Danh sách câu hỏi trong trận
    public ConcurrentDictionary<string, ArenaPlayer> Players { get; set; } // Danh sách người chơi
    public string? BuzzerWinnerPlayerId { get; set; } // Thí sinh bấm chuông đầu tiên
    public long BuzzerTimestampMs { get; set; }       // Xung nhịp phần khớp lệnh
    public DateTime StageStartTimeUtc { get; set; }   // Thời điểm bắt đầu vòng
    public int StageDurationSeconds { get; set; }     // Thời lượng của vòng
    public Dictionary<string, object> CustomData { get; set; } // Dữ liệu linh hoạt riêng
}
```

### 3.2. `ArenaPlayer`
```csharp
public class ArenaPlayer
{
    public string Id { get; set; }               // ID hoặc ConnectionId của thí sinh
    public string Name { get; set; }             // Tên hiển thị
    public string Avatar { get; set; }           // Emoji hoặc URL ảnh đại diện
    public int Score { get; set; }               // Điểm số hiện tại trong trận đấu
    public bool IsConnected { get; set; }        // Trạng thái kết nối WebSocket
    public int SeatNumber { get; set; }          // Ghế số (1-100 cho Rung Chuông Vàng)
    public string Status { get; set; }           // "ACTIVE", "ELIMINATED", "BUZZED"
    public string? TeamName { get; set; }        // "Team A", "Team B" (University Challenge)
    public int Streak { get; set; }              // Chuỗi trả lời đúng liên tiếp
    public int StepPosition { get; set; }        // Bậc thang hiện tại (Nhanh Như Chớp)
    public bool StarOfHopeUsed { get; set; }     // Đã dùng Ngôi sao hy vọng hay chưa
}
```

### 3.3. `CustomData` — Dữ Liệu Biến Thiên Theo Từng Gameshow:
- **Olympia**: Lưu trữ trạng thái 4 mảnh ghép hàng ngang `cnvOpenedRows` và ẩn số trung tâm `cnvKeyword`.
- **Chiếc Nón Kỳ Diệu**: Lưu trữ `targetWord` (từ khóa bí mật) và `revealedLetters` (các chữ cái đã được lật mở).
- **Rung Chuông Vàng**: Lưu số lượt cứu trợ còn lại `rescueAttemptsLeft` và danh sách thí sinh được hồi sinh.
- **University Challenge**: Lưu trữ trạng thái mở micro thảo luận nhóm `isTeamMicOpen`.
- **Jeopardy!**: Danh sách các ô ma trận đã được giải `solvedCells`.
- **Nhanh Như Chớp**: Vị trí dốc nghiêng cao nhất đạt được trong 2 phút `maxStepReached`.

---

## 4. Mô Hình Quản Trị Đa Khách Thuê & Cây Tổ Chức Đa Cấp (Kỳ Quan 16)

Hệ thống bổ sung phân hệ thực thể phục vụ **Triết lý Bộc Lộ Độ Phức Tạp Tiệm Tiến** (từ Cá nhân đến Tập đoàn toàn cầu):

```mermaid
erDiagram
    TENANTS ||--o{ ORGANIZATION_UNITS : contains
    TENANTS ||--o{ TENANT_DOMAIN_CONFIGS : configures
    TENANTS ||--o{ TENANT_COORDINATE_PRESETS : defines
    DYNAMIC_DOMAINS ||--o{ DYNAMIC_DOMAINS : sub_domains
    DYNAMIC_DOMAINS ||--o{ TENANT_DOMAIN_CONFIGS : activates

    TENANTS {
        uuid id PK
        string code UK
        string name
        int scale_type "1:Individual, 2:Team, 3:Classroom, 4:Organization, 5:Enterprise, 6:Global"
        int plan
        string custom_domain
        jsonb feature_flags_json
        string keycloak_realm_id
        timestamp created_at
    }

    ORGANIZATION_UNITS {
        uuid id PK
        uuid tenant_id FK
        uuid parent_id FK
        string code
        string name
        int unit_type "1:HeadOffice, 2:Region, 3:Branch, 4:Department, 5:Classroom"
        string hierarchy_path "e.g. /HO/MB_HN/CN_HOANKIEM"
        string email
        string phone_number
        boolean is_active
        int display_order
    }

    DYNAMIC_DOMAINS {
        string code PK "e.g. BANKING, EDUCATION, AGRI_CREDIT"
        string name
        text description
        string icon
        string color_badge
        boolean is_system_standard
        uuid tenant_id FK
        string parent_domain_code FK
        boolean is_active
        int display_order
    }

    TENANT_DOMAIN_CONFIGS {
        uuid id PK
        uuid tenant_id FK
        string domain_code FK
        boolean is_enabled
        string custom_display_name
        int display_order
    }

    TENANT_COORDINATE_PRESETS {
        uuid id PK
        uuid tenant_id FK
        string domain_code
        string coordinate_type "TARGET_LEVEL, ISSUING_ORG, STANDARD, PURPOSE"
        string preset_code
        string preset_label
        jsonb synonyms_json "Từ điển đồng nghĩa phục vụ AI"
        boolean is_default
        int display_order
    }
```

### Chiến Lược Đánh Chỉ Mục (Indexing Strategy):
1. **`idx_org_units_tenant_parent`**: `(tenant_id, parent_id)` tối ưu truy vấn nạp cây lồng nhau.
2. **`idx_org_units_hierarchy_path`**: `(hierarchy_path)` tối ưu truy vấn đệ quy tìm tất cả con cháu (`HierarchyPath.StartsWith(...)`).
3. **`idx_tenant_domain_configs_unique`**: `(tenant_id, domain_code) UNIQUE` đảm bảo mỗi tenant chỉ có 1 bản ghi trạng thái cho mỗi ngành.
4. **`idx_tenant_coordinate_presets`**: `(tenant_id, domain_code, coordinate_type)` nạp nhanh bộ từ điển cho AI Cognitive Banner Sniffer.
5. **Global Query Filter**: Tự động chèn `WHERE (tenant_id = @current_tenant_id OR tenant_id = '00000000-0000-0000-0000-000000000000')` vào mọi truy vấn EF Core, cách ly dữ liệu tuyệt đối giữa các ngân hàng/trường học.
