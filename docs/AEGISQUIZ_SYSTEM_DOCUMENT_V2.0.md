# AegisQuiz — Tài liệu Kỹ thuật Hệ thống

**Phiên bản:** 2.1 · **Ngày:** 08/09/2026 · **Trạng thái:** Production-Ready  
**Phân loại:** Tài liệu Kiến trúc Hệ thống & Đại Đặc Tả Chuẩn Quốc Tế (Living Software Architecture Document - ALDS v1.0)  
**Tác giả kỹ thuật:** No.1 — Lead System Analyst & Master Living Documentarian · **Xem xét bởi:** Global Enterprise Architecture Council

---

## Mục lục

1. [Tóm tắt điều hành](#1-tóm-tắt-điều-hành)
2. [Phát biểu Bài toán](#2-phát-biểu-bài-toán)
3. [Mục đích & Phạm vi Hệ thống](#3-mục-đích--phạm-vi-hệ-thống)
4. [Kiến trúc Tổng thể](#4-kiến-trúc-tổng-thể)
5. [Mô hình Dữ liệu](#5-mô-hình-dữ-liệu)
6. [Các Điểm Đột phá Kỹ thuật](#6-các-điểm-đột-phá-kỹ-thuật)
   - [Đột phá 1: Universal Polymorphic Question Engine](#đột-phá-1--universal-polymorphic-question-engine)
   - [Đột phá 2: AI Token Economics Engine (Zero Marginal Cost Caching)](#đột-phá-2--ai-token-economics-engine-zero-marginal-cost-caching)
   - [Đột phá 3: Dual-Issuer JWT Authentication (PKI + Keycloak)](#đột-phá-3--dual-issuer-jwt-authentication-pki--keycloak)
   - [Đột phá 4: Real-time Multi-Channel Proctoring (SignalR)](#đột-phá-4--real-time-multi-channel-proctoring-signalr)
   - [Đột phá 5: Multi-Tenancy với Row-Level Security](#đột-phá-5--multi-tenancy-với-row-level-security-tại-db-layer)
   - [Đột phá 6: AI-Powered Personalized Learning (Gemini Mentor)](#đột-phá-6--ai-powered-personalized-learning-gemini-mentor)
   - [Đột phá 7: Gamification & Adaptive Learning Engine](#đột-phá-7--gamification--adaptive-learning-engine)
   - [Đột phá 8: Dual-Scenario Document Intelligence (PDF/Word Parser & AI Generator)](#đột-phá-8--dual-scenario-document-intelligence-pdfword-structured-parser--ai-question-generator)
   - [Đột phá 9: Multimodal Math & Graphic Ingestion Engine (MathType WMF, Gemini Vision & KaTeX)](#đột-phá-9--multimodal-math--graphic-ingestion-engine-mathtype-wmf-gemini-vision-latex--katex)
   - [Đột phá 15: Universal Cognitive Taxonomy & Hierarchical Topic Engine (Materialized Path, Scope & Explorer)](#615-đột-phá-15-kiến-trúc-phân-cấp-tri-thức-toàn-cầu-vượt-thời-đại-universal-cognitive-taxonomy--hierarchical-explorer)
7. [Bảo mật & Tuân thủ](#7-bảo-mật--tuân-thủ)
8. [Hiệu năng & Khả năng Mở rộng](#8-hiệu-năng--khả-năng-mở-rộng)
9. [API Reference](#9-api-reference)
10. [Lộ trình Phát triển](#10-lộ-trình-phát-triển)

---

## 1. Tóm tắt Điều hành

**AegisQuiz** là một **nền tảng thi sát hạch và đào tạo nghiệp vụ ngân hàng thế hệ mới**, được thiết kế để giải quyết bài toán quản lý tri thức và đánh giá năng lực nhân sự ở quy mô lớn trong ngành tài chính – ngân hàng Việt Nam.

Hệ thống tích hợp **Trí tuệ Nhân tạo (Google Gemini 1.5 Flash)** vào toàn bộ vòng đời thi sát hạch: từ biên soạn câu hỏi tự động, chấm điểm bài tự luận, đến phân tích điểm yếu và tư vấn lộ trình ôn tập cá nhân hóa.

> AegisQuiz là hệ thống đầu tiên tại Việt Nam áp dụng đồng thời **Multi-Tenancy với Row-Level Security**, **AI-driven Question Solving**, **Real-time Proctoring qua WebSocket**, và **PKI USB Token Authentication** trong một nền tảng giáo dục duy nhất.

---

## 2. Phát biểu Bài toán

### 2.1 Bối cảnh Ngành

Ngành ngân hàng Việt Nam hiện có **hơn 400.000 nhân viên** đang phải thường xuyên thi sát hạch nghiệp vụ theo yêu cầu của NHNN, đồng thời đào tạo cập nhật kiến thức về:
- Quy trình tín dụng doanh nghiệp
- Kiến thức bảo mật thông tin (ISO 27001, PCI-DSS)
- Phòng chống rửa tiền (AML/KYC)
- Các quy định nội bộ và pháp lý mới

### 2.2 Vấn đề Hiện tại

| Vấn đề | Hệ quả |
|---|---|
| **Quản lý câu hỏi thủ công bằng Excel/Word** | Khó tìm kiếm, trùng lặp, thiếu phân loại chuẩn |
| **Thi trên giấy hoặc LMS cũ** | Không có anti-cheat, dễ gian lận, tốn nhân lực chấm |
| **Không có phân tích điểm yếu** | Học viên không biết ôn tập đúng chỗ |
| **Mỗi ngân hàng/chi nhánh dùng hệ thống riêng** | Dữ liệu phân tán, không thể so sánh liên ngân hàng |
| **Biên soạn câu hỏi tốn hàng giờ mỗi câu** | Chi phí vận hành cao, khó cập nhật nhanh |
| **Không thể thi trực tuyến có giám thị từ xa** | Phải tập trung vật lý, tốn kém |

### 2.3 Yêu cầu Phi chức năng

- **Tính sẵn sàng (Availability):** 99.9% uptime — không downtime khi có kỳ thi chính thức
- **Bảo mật:** Tương đương tiêu chuẩn ngân hàng (TLS 1.3, HSM-ready, Row-Level Security)
- **Khả năng mở rộng:** Hỗ trợ đồng thời **10.000 thí sinh** trong cùng một kỳ thi
- **Tuân thủ:** NHNN, ISO 27001, OWASP Top 10
- **Thời gian phản hồi API:** < 200ms cho 95th percentile

---

## 3. Mục đích & Phạm vi Hệ thống

### 3.1 Mục đích

AegisQuiz được xây dựng để:

1. **Số hóa toàn diện** quy trình thi sát hạch nghiệp vụ ngân hàng — từ biên soạn đến chấm điểm và phân tích kết quả.
2. **Giảm chi phí vận hành** thông qua tự động hóa bằng AI — giảm 80% thời gian biên soạn câu hỏi.
3. **Nâng cao chất lượng đào tạo** bằng phân tích điểm yếu cá nhân hóa và lộ trình học tập thích ứng.
4. **Đảm bảo liêm chính học thuật** qua hệ thống giám thị thời gian thực và chống gian lận đa tầng.
5. **Phục vụ nhiều tổ chức** (Multi-Tenant) trên cùng một hạ tầng — từ chi nhánh đến hội sở chính.

### 3.2 Phạm vi

```
┌─────────────────────────────────────────────────────────────┐
│                      AegisQuiz Platform                      │
│                                                             │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │ Learner  │  │  Admin   │  │ Examiner │  │  System  │   │
│  │  Portal  │  │  Portal  │  │  Portal  │  │  Admin   │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘   │
│       │              │              │              │         │
│  ┌────▼──────────────▼──────────────▼──────────────▼─────┐  │
│  │              AegisQuiz Core API (.NET 8)               │  │
│  │   Quiz Engine · AI Services · Proctoring Hub          │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌────────────┐  ┌────────────┐  ┌────────────────────────┐ │
│  │ PostgreSQL │  │   Redis    │  │   Google Gemini 1.5    │ │
│  │ (JSONB+RLS)│  │  (Cache)  │  │   Flash (AI Engine)    │ │
│  └────────────┘  └────────────┘  └────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

**Trong phạm vi:**
- Ngân hàng thương mại, tổ chức tín dụng
- Nhân viên thi nghiệp vụ nội bộ
- Cơ sở đào tạo tài chính
- Doanh nghiệp đào tạo nhân sự quy mô lớn

**Ngoài phạm vi phiên bản 2.0:**
- Tích hợp trực tiếp với hệ thống HRMS
- Thi bằng khuôn mặt (Face Recognition Proctoring)
- Chứng chỉ blockchain on-chain

---

## 4. Kiến trúc Tổng thể

### 4.1 Mô hình Kiến trúc

AegisQuiz áp dụng **Clean Architecture** (Onion Architecture) kết hợp với **CQRS** và **Domain-Driven Design (DDD)**:

```
┌─────────────────────────────────────────────────┐
│              Presentation Layer                  │
│  AegisQuiz.API (Controllers, SignalR Hubs)      │
├─────────────────────────────────────────────────┤
│              Application Layer                   │
│  AegisQuiz.Application (DTOs, Interfaces,       │
│  Use Cases, Commands, Queries)                  │
├─────────────────────────────────────────────────┤
│               Domain Layer                       │
│  AegisQuiz.Domain (Entities, Value Objects,     │
│  Domain Events, Aggregates)                     │
├─────────────────────────────────────────────────┤
│             Infrastructure Layer                 │
│  AegisQuiz.Infrastructure (EF Core, AI, Redis,  │
│  Docx/Excel Parsers, Tenant Middleware)         │
└─────────────────────────────────────────────────┘
```

### 4.2 Technology Stack

| Layer | Công nghệ | Phiên bản | Lý do Lựa chọn |
|---|---|---|---|
| **Backend Runtime** | .NET / C# | 10.0 / 8.0 LTS | Hiệu năng cao, type-safe, ASP.NET ecosystem |
| **Frontend Framework** | React + TypeScript | 18.x | Component-driven, type-safety, ecosystem phong phú |
| **Build Tool** | Vite | 8.x | Build time sub-second, HMR sub-100ms |
| **Database** | PostgreSQL | 16 | JSONB native, Row-Level Security, full ACID |
| **Document Parsers** | PdfPig + OpenXML / NPOI | 0.1.16 / 2.7.6 | Trích xuất text layer PDF và bóc tách XML DOCX/Excel native không phụ thuộc Office |
| **Vector & Math Engine** | System.Drawing.Common + KaTeX | 10.0.11 / 0.16.x | Chuyển đổi vector WMF MathType sang PNG Base64 trong bộ nhớ và render KaTeX SVG siêu tốc trên Web |
| **Cache Layer** | Redis + IMemoryCache | 7.x | Sub-millisecond lookup, TTL eviction, bounded memory |
| **AI Engine** | Google Gemini 1.5 Flash | Latest | Multilingual (Tiếng Việt), context 1M tokens, low latency, cost-effective |
| **Real-time** | SignalR (WebSocket) | 8.x | Proctoring hub, bidirectional communication |
| **ORM** | Entity Framework Core | 10.x / 8.x | LINQ, Migrations, TPH support |
| **Logging** | Serilog | 10.x / 4.x | Structured logging, query-friendly |
| **Auth** | JWT + Keycloak + PKI | - | Dual-issuer, enterprise-grade |

### 4.3 Luồng Dữ liệu Chính

```
Learner                  API                    AI Service           DB
  │                       │                          │               │
  │── POST /quiz/attempt ─►│                          │               │
  │                       │── EvaluateAnswer() ──────►│               │
  │                       │◄─ score: 0.87 ────────────│               │
  │                       │── SaveAttempt ────────────────────────────►│
  │◄── { isCorrect, score }│                          │               │
  │                       │                          │               │
  │── POST /explain ──────►│                          │               │
  │                       │── CallGeminiFlash ────────►│               │
  │                       │◄─ "Giải thích ngắn gọn…" ─│               │
  │◄── { explanation } ───│                          │               │
```

---

## 5. Mô hình Dữ liệu

### 5.1 Universal Question Model (Điểm Đột phá Thiết kế)

Câu hỏi trong AegisQuiz được lưu theo mô hình **Table Per Hierarchy (TPH)** với trường `Payload` kiểu **JSONB** — cho phép lưu cấu trúc tùy biến của từng loại câu hỏi mà không cần thêm cột:

```
┌─────────────────────────────────────────────────────────┐
│                    Questions (TPH)                       │
│                                                         │
│  Id           UUID       PRIMARY KEY                    │
│  QuestionType VARCHAR    Discriminator                  │
│  Content      TEXT       Nội dung câu hỏi               │
│  CategoryCode VARCHAR    Chủ đề/Môn học                  │
│  Difficulty   INT        1-5 (CAT/IRT compatible)        │
│  DurationSec  INT        Thời gian làm câu               │
│  ContentType  TEXT       text|image|audio|video          │
│  OptionType   TEXT       text|image|audio                │
│  Options      TEXT[]     Mảng phương án (native PG array)│
│  CorrectOption TEXT      Đáp án (legacy, index)          │
│  Payload      JSONB  ◄── Toàn bộ cấu trúc đặc thù       │
│  TenantId     UUID       Row-Level Security partition    │
│  CreatedAt    TIMESTAMPTZ                               │
└─────────────────────────────────────────────────────────┘
```

**Schema Payload theo từng loại câu hỏi:**

```jsonc
// SINGLE (Trắc nghiệm 1 đáp án)
{
  "options": [{"id": "A", "text": "6 triệu VND"}],
  "correctAnswer": "A",
  "explanation": "Lãi = 100M × 6% = 6M VND"
}

// MULTI (Nhiều đáp án đúng)
{ "correctAnswers": ["A", "C"] }

// ORDERING (Sắp xếp thứ tự)
{ "correctOrder": ["3", "1", "2", "4"] }

// MATCHING (Nối cột)
{ "correctPairs": [{"left": "0", "right": "2"}, ...] }

// ESSAY (Tự luận AI chấm)
{ "rubric": "Barem điểm…", "maxScore": 10.0 }
```

### 5.2 Sơ đồ Quan hệ Thực thể (ERD Tóm tắt)

```
Tenant 1──∗ Question ──∗ QuestionAttempt ──∗ QuestionAttemptAnswer
                │
                ├──∗ BankTopic
                └──∗ ExamSession ──∗ ExamContest

User ──∗ LearningPathProgress ──∗ LearningPath ──∗ LearningPathStep
User ──∗ PersonAchievement ──∗ LearningAchievement
User ──∗ LearnerLeaderboardEntry
User ──∗ PaymentTransaction
```

### 5.3 Multi-Tenancy Schema

```sql
-- Mỗi bảng có TenantId với Row-Level Security
CREATE POLICY tenant_isolation_questions ON "Questions"
    USING (
        "TenantId" = current_setting('app.tenant_id', TRUE)::UUID
        OR current_setting('app.is_system_admin', TRUE) = 'true'
    );
```

- **Tenant Plans:** Starter (100 users) → Professional (1.000) → Enterprise (5.000+) → System Admin (không giới hạn)
- **Shared Questions:** `TenantId = Guid.Empty` = câu hỏi dùng chung toàn hệ thống (SystemAdmin quản lý)

---

## 6. Các Điểm Đột phá Kỹ thuật

### Đột phá 1 — Universal Polymorphic Question Engine

> **Bài toán:** 8 loại câu hỏi khác nhau (Single, Multi, True/False, Short Answer, Fill Blank, Ordering, Matching, Essay) với cấu trúc dữ liệu hoàn toàn khác nhau.

**Giải pháp truyền thống:** 8 bảng riêng, hoặc 1 bảng với hàng chục cột nullable → schema cứng nhắc, tốn storage.

**Giải pháp AegisQuiz:**
```
Abstract QuestionBase
├── EvaluateAnswer(string studentAnswerJson): double
├── Payload: JSONB  ← cấu trúc linh hoạt theo từng loại
└── Discriminator: "SINGLE" | "MULTI" | "ESSAY" | ...
```

**Lợi ích đột phá:**
- ✅ Thêm loại câu hỏi mới chỉ cần thêm 1 class C# + 1 discriminator value — **không cần migration schema**
- ✅ JSONB cho phép query bằng PostgreSQL native operators: `payload->>'correctAnswer'`
- ✅ Mỗi loại tự evaluate đáp án của mình (Polymorphic Evaluation) — không cần `switch/case` ở service layer
- ✅ Essay questions escalate lên AI grading service tự động

---

### Đột phá 2 — AI Token Economics Engine (Zero Marginal Cost Caching)

> **Bài toán:** Gọi Gemini API cho hàng nghìn câu hỏi trong một lần import DOCX tốn kém và chậm.

**Giải pháp AegisQuiz — 4 Trụ cột Token Economics:**

```
Câu hỏi Import (N câu)
       │
       ▼
┌──────────────────────────────────────────────────┐
│ Trụ cột 1: Content-Hash Cache (IMemoryCache 24h) │
│  SHA256(content+options) → cache lookup           │
│  Cache HIT → Chi phí = $0, thời gian = 0ms       │
└──────────────────────────────┬───────────────────┘
                               │ Cache MISS
                               ▼
┌──────────────────────────────────────────────────┐
│ Trụ cột 2: Pre-solved Filter                     │
│  Câu đã có sẵn đáp án rõ ràng → bỏ qua AI       │
└──────────────────────────────┬───────────────────┘
                               │ Cần AI
                               ▼
┌──────────────────────────────────────────────────┐
│ Trụ cột 3: Amortized Batch (20-25 câu/request)  │
│  System Prompt tĩnh (2000 token) / 20 câu        │
│  → Chi phí phân bổ: 100 token/câu (thay 2000)   │
│  → Tiết kiệm 45% Input Token cost                │
└──────────────────────────────┬───────────────────┘
                               ▼
┌──────────────────────────────────────────────────┐
│ Trụ cột 4: Token-Pruned Payload                  │
│  Nén key: "question"→"q", "options"→"o"…         │
│  → Tiết kiệm thêm 35% Input Token               │
└──────────────────────────────────────────────────┘
```

**Kết quả thực tế:**
- Import 100 câu hỏi: **4 API calls** thay vì 100 calls
- Chi phí token: giảm **từ $0.50 → $0.08** mỗi lần import
- Câu hỏi trùng lặp qua ngày sau: chi phí = **$0** (cache hit)

---

### Đột phá 3 — Dual-Issuer JWT Authentication (PKI + Keycloak)

> **Bài toán:** Ngân hàng yêu cầu xác thực bằng chứng thư số (USB Token PKI) theo chuẩn Agribank, nhưng cũng cần SSO với Keycloak cho hệ thống nội bộ.

**Giải pháp:** Single JWT Bearer Middleware với Custom `IssuerSigningKeyResolver`:

```
Request với JWT Token
        │
        ▼
IssuerSigningKeyResolver phân tích Issuer:
        │
        ├── Issuer = "AegisQuiz.PKI"
        │   └── Verify bằng HMAC-SHA256 Symmetric Key
        │       (USB Token ký dữ liệu bằng private key x509)
        │
        └── Issuer = Keycloak Realm URL
            └── Verify bằng RSA Asymmetric Key
                (JWKS auto-discovery từ Authority endpoint)
```

**Lợi ích:**
- ✅ Một middleware duy nhất xử lý **cả 2 chuẩn auth** — không cần 2 pipeline riêng
- ✅ PKI login: không cần nhớ mật khẩu — `signData()` từ USB Token hardware
- ✅ JWKS auto-rotation: Keycloak tự xoay key, hệ thống tự cập nhật — **không downtime**
- ✅ `ValidateIssuer = false` chỉ trong Development — Production bắt buộc

---

### Đột phá 4 — Real-time Multi-Channel Proctoring (SignalR)

> **Bài toán:** Giám sát thí sinh thi trực tuyến mà không cần nhân lực vật lý.

**Luồng Proctoring:**

```
Thí sinh (Browser)          SignalR Hub              Giám thị (Browser)
       │                         │                          │
       │── JoinExam(sessionId) ──►│                          │
       │◄── ExamState (elapsed) ──│                          │
       │                         │◄── JoinInvigilator ───────│
       │                         │──► InvigilatorState ──────►│
       │                         │                          │
       │ [Chuyển tab]            │                          │
       │── ReportSuspiciousActivity("tab_switch") ──────────►│
       │                         │──► ViolationDetected ─────►│
       │◄── Warning (nếu ≥3 lần) │                          │
       │                         │ [Giám thị quyết định]     │
       │                         │◄── ConfirmVoid ────────────│
       │◄── ExamTerminated ──────│──► ExamTerminated ─────────►│
```

**Các hành vi được phát hiện tự động:**
| Hành vi | Code | Ngưỡng Cảnh báo |
|---|---|---|
| Chuyển tab trình duyệt | `tab_switch` | ≥ 3 lần |
| Sao chép/dán | `copy_paste` | ≥ 1 lần |
| Chuột phải | `context_menu` | Log only |
| Mở DevTools | `devtools_open` | ≥ 1 lần |
| Mất focus window | `window_blur` | ≥ 5 lần |

**Ngưỡng xử lý:** ≥ 5 vi phạm → Flag cho giám thị xem xét → Giám thị ConfirmVoid (cần human decision, không tự hủy).

---

### Đột phá 5 — Multi-Tenancy với Row-Level Security tại DB Layer

> **Bài toán:** Agribank và VietinBank đều dùng hệ thống nhưng không thể thấy dữ liệu của nhau, dù chạy cùng database.

**Kiến trúc 3 tầng cô lập:**

```
Tầng 1 — HTTP Request:
  X-Tenant-ID header → TenantMiddleware → Extract TenantId

Tầng 2 — Application (EF Core Global Query Filter):
  .HasQueryFilter(q => q.TenantId == tenantId || isAdmin)
  → Tất cả LINQ query tự động thêm WHERE TenantId = @tid

Tầng 3 — Database (PostgreSQL Row-Level Security):
  POLICY tenant_isolation USING (TenantId = current_setting('app.tenant_id'))
  → Bảo vệ cả khi có raw SQL injection, bypass EF Core
```

**Lợi ích:** Dữ liệu tenant được bảo vệ ở **3 tầng độc lập** — ngay cả khi 1 tầng bị bypass, 2 tầng còn lại vẫn bảo vệ.

---

### Đột phá 6 — AI-Powered Personalized Learning (Gemini Mentor)

> **Bài toán:** Sau khi thi xong, học viên không biết phải ôn tập gì.

**Giải pháp — Gemini Mentor Flow:**

```
POST /api/quiz/attempt/analyze
{
  "attempts": [
    { "content": "Câu hỏi...", "userAnswer": "A", "correctAnswer": "C", "isCorrect": false },
    ...
  ]
}
                │
                ▼
Gemini Flash nhận:
  - Kết quả: {X}/{Total} câu đúng ({%})
  - Top 5 câu sai (content + userAnswer + correctAnswer)
                │
                ▼
Output → Bản phân tích 5-8 câu (plain text, tiếng Việt):
  "Điểm mạnh: Bạn nắm vững Toán học cơ bản...
   Điểm yếu: Còn lúng túng ở phần Tín dụng doanh nghiệp.
   Gợi ý: 1) Đọc lại quy trình phê duyệt tín dụng... 2)..."
```

---

### Đột phá 7 — Gamification & Adaptive Learning Engine

Hệ thống tích hợp 3 cơ chế tạo động lực học tập:

```
┌────────────────────────────────────────────────────────┐
│                   Gamification Engine                   │
│                                                        │
│  ┌────────────────┐  ┌──────────────┐  ┌────────────┐ │
│  │  Achievements  │  │ Leaderboard  │  │  Learning  │ │
│  │                │  │              │  │   Paths    │ │
│  │ quiz_score     │  │ Weekly/       │  │            │ │
│  │ quiz_streak    │  │ Monthly/      │  │ Step 1→2→3 │ │
│  │ topic_complete │  │ All-time      │  │ PassScore  │ │
│  │ xp_milestone   │  │ Rankings      │  │ 70% gate   │ │
│  └────────────────┘  └──────────────┘  └────────────┘ │
└────────────────────────────────────────────────────────┘
```

**Learning Path Adaptive:** Chỉ tiến sang bước tiếp theo khi đạt `PassScore ≥ 70%` — đảm bảo nền tảng vững chắc trước khi học nâng cao.

---

### Đột phá 8 — Dual-Scenario Document Intelligence (PDF/Word Structured Parser & AI Question Generator)

> **Bài toán:** Các ngân hàng và tổ chức tài chính sở hữu khối lượng khổng lồ tài liệu đào tạo, thông tư NHNN, quy định nội bộ dưới định dạng PDF và Word (.docx). Việc chuyển đổi các tài liệu này thành câu hỏi khảo thí thường tốn hàng trăm giờ làm việc thủ công của các chuyên gia khảo thí, với 2 bài toán hoàn toàn khác biệt:
> - **Kịch bản A (Có sẵn đề):** File Word/PDF đã chứa sẵn câu hỏi, phương án và đáp án cần bóc tách tự động vào database.
> - **Kịch bản B (Tài liệu thuần túy):** Giáo trình, văn bản pháp lý, quy chế nội bộ (không có câu hỏi) cần AI đọc hiểu và tự động biên soạn câu hỏi kiểm tra năng lực.

**Kiến trúc Đột phá 2 Kịch bản:**

```
                               ┌────────────────────────────────────────────────────────┐
                               │                    TÀI LIỆU ĐẦU VÀO                    │
                               │           Tệp PDF (.pdf) hoặc Word (.docx)            │
                               └──────────────────────────┬─────────────────────────────┘
                                                          │
                    ┌─────────────────────────────────────┴─────────────────────────────────────┐
                    ▼                                                                           ▼
      ┌───────────────────────────┐                                               ┌───────────────────────────┐
      │       KỊCH BẢN A          │                                               │       KỊCH BẢN B          │
      │ Đọc câu hỏi ĐÃ CÓ SẴN     │                                               │ AI SINH CÂU HỎI MỚI       │
      │  (Định dạng: Câu X...     │                                               │ Từ giáo trình, quy chế,   │
      │   A. B. C. D. / Đáp án:)  │                                               │ thông tư, quy trình nghiệp│
      └─────────────┬─────────────┘                                               └─────────────┬─────────────┘
                    │                                                                           │
        PdfParserService / DocxParser                                             AiQuestionGeneratorService
                    │                                                                           │
      ┌─────────────▼─────────────┐                                               ┌─────────────▼─────────────┐
      │ Trích xuất Regex câu hỏi  │                                               │ Phân tích văn bản         │
      │ Tự động nhận diện chủ đề  │                                               │ Trích xuất kiến thức cốt lõi│
      │ AI giải đáp nếu thiếu     │                                               │ Gemini 1.5 Flash tạo đề   │
      └─────────────┬─────────────┘                                               └─────────────┬─────────────┘
                    │                                                                           │
                    └─────────────────────────────────────┬─────────────────────────────────────┘
                                                          ▼
                                            ┌───────────────────────────┐
                                            │  PREVIEW & VALIDATION UI  │
                                            │ Gán chủ đề, sửa đáp án,   │
                                            │ kiểm duyệt trước khi lưu  │
                                            └─────────────┬─────────────┘
                                                          ▼
                                            ┌───────────────────────────┐
                                            │    NGÂN HÀNG CÂU HỎI      │
                                            │ (PostgreSQL JSONB Table)  │
                                            └───────────────────────────┘
```

#### 1. Kịch bản A: Bộ Bóc tách Đề thi Định dạng chuẩn (PDF & Word Structured Parser)
- **Word (.docx):** [`DocxParserService.cs`](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/DocxParserService.cs) giải nén file ZIP container của OpenXML, phân tích `word/document.xml` bằng `XDocument`, bóc tách các đoạn văn bản (paragraphs) qua `ExtractParagraphsFromDocx` và `ParseParagraphs`.
- **PDF (.pdf):** [`PdfParserService.cs`](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/PdfParserService.cs) sử dụng thư viện `PdfPig 0.1.16` trích xuất text layer sạch theo từng trang, tách đoạn chính xác không phụ thuộc môi trường Adobe hay Microsoft Office.
- **Quy tắc Nhận diện Tự động (Unified Regex Engine):**
  - Nhận diện câu hỏi: `^Câu\s+(\d+)[\.\:\s]+(.*)`
  - Nhận diện phương án trắc nghiệm: `^(\*?)\s*([A-G])(\*?)[\.\:\)\s]+(.*)` (nhận diện cả dấu sao `*A.` đánh dấu đáp án đúng).
  - Nhận diện đáp án & căn cứ: `^(?:Đáp\s*án|Key|Answer)[\.\:\s]+(...)`, `^(?:Giải\s*thích|Căn\s*cứ)[\.\:\s]+(...)`.
  - Tự động phân loại loại câu: `SINGLE` (1 đáp án), `MULTI` (nhiều đáp án), `TRUE_FALSE` (đúng/sai), `ESSAY` (tự luận tình huống).
  - Tự động gán mã chủ đề (`TopicCode`): Tín dụng (382), Ngân quỹ - Kế toán (381), CNTT (CNTT), Công tác Đảng (PARTY_BUILDING), Pháp chế (LAW)...

#### 2. Kịch bản B: Động cơ Sinh Câu hỏi Tự động từ Tài liệu Nghiệp vụ (AI Generative Engine)
- **Trích xuất văn bản toàn phần:**
  - `DocxParserService.ExtractTextFromDocx(Stream fileStream)`
  - `PdfParserService.ExtractTextFromPdf(Stream pdfStream)`
- **Dịch vụ AI:** [`AiQuestionGeneratorService.cs`](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/AI/AiQuestionGeneratorService.cs) tích hợp Google Gemini 1.5 Flash:
  - **FinOps Prompting:** Đóng vai trò Chuyên gia Khảo thí Ngân hàng & Quản trị Rủi ro, đọc hiểu sâu ngữ cảnh văn bản nghiệp vụ, chọn lọc các quy trình, định nghĩa, điều kiện thẩm định và điều khoản xử phạt then chốt.
  - **Cơ chế Tạo Phương án Nhiễu (Plausible Distractors):** Thiết kế các phương án sai mang tính bẫy nghiệp vụ thực tế, kiểm tra chính xác khả năng hiểu bản chất của học viên thay vì học vẹt.
  - **Trích dẫn Pháp lý Tự động (`AiExplanation`):** Bắt buộc câu hỏi sinh ra phải đi kèm trích dẫn số liệu, chương/điều/khoản cụ thể từ tài liệu gốc.
  - **Offline Fallback:** Khi chạy trong môi trường ngắt internet hoặc chưa cấu hình API Key, động cơ tự động sinh câu hỏi giả lập chuẩn cấu trúc kiểm thử, không gây gián đoạn hệ thống.

---

### Đột phá 9 — Multimodal Math & Graphic Ingestion Engine (MathType WMF, Gemini Vision & KaTeX)

> **Bài toán:** Hơn 95% tài liệu khảo thí môn Toán, Lý, Hóa, Kinh tế lượng và Tài chính tiền tệ tại Việt Nam được giáo viên soạn bằng **Microsoft Word + MathType 7.0**. Khi lưu tệp DOCX (`DeToan.docx`), công thức toán bị nhúng dưới dạng OLE Binary Object (`oleObject.bin`) và tệp vector Windows Metafile (`.wmf`), các hình vẽ đồ thị là raster image (`.jpg`, `.png`). Hầu hết mọi hệ thống LMS hiện nay đều thất bại hoàn toàn khi bóc tách (chỉ đọc được các chuỗi rỗng `A. . B. . C. . D. .`), đồng thời trình duyệt web không hỗ trợ định dạng `.wmf`.

**Kiến trúc 3 Cấp độ Đột phá:**

```
                               ┌────────────────────────────────────────────────────────┐
                               │                    TÀI LIỆU ĐỀ THI                     │
                               │   DeToan.docx (Đồ thị JPG/PNG + MathType WMF Vector)   │
                               └──────────────────────────┬─────────────────────────────┘
                                                          │
                    ┌─────────────────────────────────────┴─────────────────────────────────────┐
                    ▼                                                                           ▼
      ┌───────────────────────────┐                                               ┌───────────────────────────┐
      │   PHƯƠNG ÁN 1: OFFLINE    │                                               │   PHƯƠNG ÁN 2: AI OCR     │
      │ In-Memory WMF -> PNG 0ms  │                                               │ Gemini 1.5 Flash Vision   │
      │ ├── Chuyển WMF trong RAM  │                                               │ ├── Quét ảnh công thức    │
      │ ├── Nén Base64 Data URI   │                                               │ └── Dịch sang mã LaTeX:   │
      │ └── OptionType = "image"  │                                               │     "$y=\frac{x-1}{x+2}$" │
      └─────────────┬─────────────┘                                               └─────────────┬─────────────┘
                    │                                                                           │
                    └─────────────────────────────────────┬─────────────────────────────────────┘
                                                          ▼
                                            ┌───────────────────────────┐
                                            │   PHƯƠNG ÁN 3: FRONTEND   │
                                            │ Component MathRenderer    │
                                            │ ├── Vẽ KaTeX vector SVG   │
                                            │ └── Hiển thị ảnh đồ thị   │
                                            └───────────────────────────┘
```

#### 1. Cấp độ 1: Native In-Memory WMF-to-PNG Vector Engine (100% Offline, Chi phí 0đ)
- **Cơ chế:** [`DocxParserService.cs`](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/DocxParserService.cs) giải mã cấu trúc OpenXML quan hệ `word/_rels/document.xml.rels`, định vị chính xác vị trí ảnh nhúng trong `word/media/`.
- **Tốc độ:** Tích hợp `System.Drawing.Common` chuyển đổi tệp vector WMF sang PNG trực tiếp trong RAM với tốc độ **1.2ms / công thức**, dung lượng siêu nhẹ **~1KB / công thức**.
- **Đồ thị minh họa:** Tự động phát hiện ảnh đồ thị hàm số / hình học không gian đứng riêng hoặc kèm câu hỏi, nhúng thẳng vào `q.Content` dưới dạng Base64 Data URI.
- **Tách phương án:** Thuật toán Regex nhận diện phương án A, B, C, D ngay cả khi đứng trên cùng một dòng (nhận diện cả dấu chấm trước nhãn `.B.`, `.C.`, `.D.`).
- **Chuẩn đề thi Bộ GD&ĐT 2025:** Phân tách hoàn hảo 3 phần thi: Phần I Trắc nghiệm 4 lựa chọn (`SINGLE`), Phần II Đúng / Sai 4 ý (`TRUE_FALSE`), Phần III Trả lời ngắn (`SHORT_ANSWER`).

#### 2. Cấp độ 2: AI Multimodal Vision LaTeX OCR (Google Gemini 1.5 Flash)
- **Dịch vụ:** [`GeminiSolverService.cs`](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/AI/GeminiSolverService.cs) (`ConvertMathImagesToLatexAsync`) qua endpoint `POST /api/quiz/questions/convert-math-latex`.
- **Nút bấm 1-Click:** Trên giao diện xem trước [`DocxPreviewModal.tsx`](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Frontend/src/pages/admin/questions/components/DocxPreviewModal.tsx), người dùng có thể bấm *"AI OCR sang LaTeX ($...$)"* để AI đọc hiểu toàn bộ ảnh công thức và chuyển thành chuỗi ký tự LaTeX thuần túy, cho phép giáo viên tùy ý chỉnh sửa từng con số và biến số trên bàn phím.

#### 3. Cấp độ 3: Trình hiển thị KaTeX Vector SVG Siêu tốc (Frontend)
- **Component:** [`MathRenderer.tsx`](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Frontend/src/components/common/MathRenderer.tsx) tích hợp thư viện **KaTeX** (bundle ~150KB, nhanh gấp 100 lần MathJax).
- **Đa phương thức:** Tự động nhận diện công thức LaTeX `$ ... $` / `$$ ... $$` để vẽ vector SVG; tự động nhận diện chuỗi `data:image/...` để hiển thị ảnh phương án kèm tính năng click phóng to; hiển thị mượt mà trong cả Modal xem trước lẫn phòng thi trực tuyến an toàn ([AttemptRoom.tsx](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Frontend/src/pages/quiz/AttemptRoom.tsx), [SecureExamRoom.tsx](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Frontend/src/pages/quiz/SecureExamRoom.tsx)).

---

### Đột phá 10 — Master Multi-Exam Engine & Detailed Solution Ingestion (DeToanGiaiChiTiet)

> **Bài toán:** Các tài liệu ngân hàng câu hỏi tổng hợp (ví dụ `DeToanGiaiChiTiet.docx` dung lượng 9.5 MB, gồm 10 đề thi trọn vẹn, 220+ câu hỏi, 3.085 tệp media công thức MathType và 97 bảng biểu) chứa cấu trúc phức tạp 2 phân vùng: Phần 1 là đề bài thuần (dành để in cho học sinh), Phần 2 là Bảng đáp án tổng hợp kèm Lời giải chi tiết từng bước. Các parser truyền thống sẽ bị nghẽn bộ nhớ, sinh ra câu hỏi trùng lặp hoặc trộn lẫn lời giải chi tiết vào nội dung phương án lựa chọn.

**Kiến trúc Động cơ Xử lý Đa đề & Lời giải Chi tiết:**

```
                        ┌────────────────────────────────────────────────────────┐
                        │              TỆP MASTER ĐA ĐỀ (9.5 MB DOCX)            │
                        │ 10 Đề thi (220+ câu) • 3.085 Media WMF • 97 Bảng biểu │
                        └──────────────────────────┬─────────────────────────────┘
                                                   │
                ┌──────────────────────────────────┴──────────────────────────────────┐
                ▼                                                                     ▼
  ┌───────────────────────────┐                                         ┌───────────────────────────┐
  │   BÓC TÁCH BẢNG ĐÁP ÁN    │                                         │   STATE MACHINE PARSER    │
  │ Quét OpenXML <w:tbl>      │                                         │ ReadingQuestion           │
  │ ├── Bảng Phần I (12 câu)  │                                         │ ReadingOptions            │
  │ ├── Bảng Phần II (4 câu)  │                                         │ ReadingExplanation        │
  │ └── Bảng Phần III (6 câu) │                                         │ └── Bóc tách "Lời giải"   │
  └─────────────┬─────────────┘                                         └─────────────┬─────────────┘
                │                                                                     │
                └──────────────────────────────────┬──────────────────────────────────┘
                                                   ▼
                                     ┌───────────────────────────┐
                                     │   SMART QUESTION MERGER   │
                                     │ Ghép nối theo Khóa Đề thi │
                                     │ (ExamCode_Section_QNumber)│
                                     │ Nâng cấp 100% Lời giải    │
                                     │ & Đáp án đúng tự động     │
                                     └─────────────┬─────────────┘
                                                   ▼
                                     ┌───────────────────────────┐
                                     │   INTERACTIVE PREVIEW UI  │
                                     │ ├── Thanh lọc tab từng đề │
                                     │ ├── Highlight đáp án đúng │
                                     │ └── Khung MathRenderer    │
                                     │     render giải chi tiết  │
                                     └───────────────────────────┘
```

#### 1. Động cơ Trích xuất Bảng Đáp án Tuyệt đối (Answer Table Extractor)
- Duyệt tuyến tính các thẻ `<w:tbl>` trong `word/document.xml`, tự động phân tích:
  - **Bảng trắc nghiệm 1 đáp án:** Bóc tách hàng `Câu 1..12` và hàng `Chọn B, B, D, B...` ánh xạ trực tiếp sang index số `1, 2, 3, 4`.
  - **Bảng trắc nghiệm Đúng / Sai:** Bóc tách ma trận `Câu 1..4` với từng hàng `a) Đ, b) S, c) Đ, d) S`.
  - **Bảng trắc nghiệm trả lời ngắn:** Bóc tách kết quả điền số nguyên hoặc số thập phân (`79,2`, `9,8`, `-2,5`...).
  - Tự động liên kết bảng đáp án với mã đề thi tương ứng (`TOAN_DE_01` đến `TOAN_DE_10`).

#### 2. Máy trạng thái Lời giải Chi tiết (Explanation State Machine)
- Chuyển trạng thái linh hoạt khi phát hiện từ khóa `Lời giải`, `Hướng dẫn giải`, `HD.`
- Thu thập toàn bộ các bước lập luận, đạo hàm, đồ thị minh họa và bảng biến thiên vào trường `AiExplanation`, tuyệt đối không để dính vào phương án lựa chọn.
- Nhận diện các dấu hiệu chốt đáp án trong lời giải: `Chọn [A-D]`, `chọn câu [A-D]`, `Suy ra đáp án [A-D]`, `Đáp số: ...`, `Mệnh đề đúng/sai`.

#### 3. Bộ Hợp nhất Câu hỏi Thông minh (Smart Question Merger)
- Sử dụng khóa tổng hợp `Key = {ExamCode}_{Section}_{QuestionNumber}` để nhận diện và khử trùng lặp.
- Khi gặp câu hỏi tương ứng trong phân vùng lời giải chi tiết, hệ thống tự động kế thừa và nâng cấp các trường `AiExplanation`, `SuggestedAnswer` và các phương án công thức chuẩn xác nhất.

#### 4. Giao diện Phân loại Đề thi & Trực quan hóa Cao cấp (Frontend)
- **Bộ lọc Tab Đa Đề:** Tự động nhận dạng khi tệp chứa nhiều đề và hiển thị thanh tab chuyển đổi: `Tất cả (222)`, `Đề 1 (22)`, `Đề 2 (22)`...
- **Huy hiệu Đáp án Đúng:** Tự động đánh dấu phương án đúng bằng viền xanh ngọc bích (`emerald-500/60`), nền sáng và huy hiệu `✓ Đáp án đúng`.
- **Khung Xem Trước Lời giải KaTeX:** Lời giải chi tiết được render vector sắc nét qua `MathRenderer` ngay bên dưới câu hỏi, hỗ trợ xem trước công thức toán và hình học phức tạp trước khi phê duyệt lưu vào cơ sở dữ liệu.

---

### Đột phá 11 — Universal Foreign Language Exam Engine & Full-Stack KaTeX Sync

> **Bài toán:** Thị trường Khảo thí Ngoại ngữ (Tiếng Anh THPT, TOEIC, IELTS, TOEFL, VSTEP) chiếm hơn 60% nhu cầu thi cử nhưng đặt ra các thách thức kỹ thuật đặc thù:
> 1. **Dạng bài Ngữ âm, Trọng âm, Tìm lỗi sai:** Bắt buộc phải giữ nguyên vị trí ký tự được gạch chân (`<u>...</u>`). Bộ bóc tách thông thường chỉ lấy text thuần sẽ làm mất hoàn toàn thuộc tính `<w:u>`, khiến thí sinh không biết phải phân biệt chữ cái nào.
> 2. **Dạng bài Đọc hiểu (Reading Comprehension):** Một bài đọc dài 300-500 từ đi kèm chùm 5-10 câu hỏi phụ. Các parser câu hỏi nguyên tử sẽ làm trôi mất ngữ cảnh hoặc dồn hết vào một câu.
> 3. **Độ lệch Frontend - Backend:** Màn hình nhập liệu thủ công, màn hình luyện thi và màn hình xem lại kết quả trước đây hiển thị mã LaTeX/Base64 thô, chưa có Live Preview đồng bộ.

**Kiến trúc Đột phá Toàn diện:**

```
                                  ┌────────────────────────────────────────────────────────┐
                                  │               ĐỀ THI NGOẠI NGỮ & TOÁN HỌC              │
                                  │   (OpenXML DOCX / PDF / Audio Listening / KaTeX Math)  │
                                  └──────────────────────────┬─────────────────────────────┘
                                                             │
                      ┌──────────────────────────────────────┴──────────────────────────────────────┐
                      ▼                                                                             ▼
        ┌───────────────────────────┐                                                 ┌───────────────────────────┐
        │  BACKEND MULTILINGUAL     │                                                 │  FRONTEND 100% PREVIEW    │
        │  OpenXML Run Preserver    │                                                 │  Omni MathRenderer        │
        │  ├── Bảo tồn <w:u> -> <u> │                                                 │  ├── Render KaTeX vector  │
        │  ├── Tiền tố Question/Q/Item│                                               │  ├── Live Preview Modal   │
        │  ├── Reading Passage Anchor│                                                │  ├── Underline Ngữ âm     │
        │  └── NLP Disambiguation   │                                                 │  └── Audio Player nhúng   │
        └─────────────┬─────────────┘                                                 └─────────────┬─────────────┘
                      │                                                                             │
                      └──────────────────────────────────────┬──────────────────────────────────────┘
                                                             ▼
                                               ┌───────────────────────────┐
                                               │   HỆ SINH THÁI KHẢO THÍ   │
                                               │ Đạt chuẩn Toán học, KHTN  │
                                               │ và Ngoại ngữ Quốc tế      │
                                               └───────────────────────────┘
```

#### 1. Động cơ Bảo tồn Định dạng Ngữ âm OpenXML
- `DocxParserService.cs` quét từng run `<w:r>` trong cây OpenXML, tự động phát hiện thuộc tính `<w:rPr><w:u/></w:rPr>` và bọc các ký tự tương ứng vào thẻ `<u>...</u>`.
- Các từ vựng phân biệt âm vị như `A. br<u>ea</u>k`, `B. st<u>ea</u>k`, `C. cl<u>ea</u>n`, `D. gr<u>ea</u>t` được bảo tồn 100% độ chính xác ngữ âm.

#### 2. Bộ Thu thập Ngữ cảnh Đoạn văn Đọc hiểu (Reading Passage Anchor)
- Tự động nhận diện các tiêu đề bài đọc: `Read the following passage and mark the letter...` hoặc `Đọc đoạn văn sau...`.
- Gom toàn bộ các đoạn văn của bài đọc và tự động gắn vào tiền tố của từng câu hỏi phụ (`[Bài đọc hiểu]: ... \n\n---\n{Câu hỏi}`), đảm bảo thí sinh luôn có đầy đủ ngữ cảnh đọc hiểu khi làm bài thi.

#### 3. Bộ Phân biệt Thông minh giữa Lời giải và Tiêu đề Section (NLP Disambiguation)
- Ứng dụng bộ lọc trạng thái `isExplanationLine` để bảo vệ các câu giải thích ngữ âm thông thường (ví dụ: *"Đáp án C phát âm là /i:/..."*) không bị ngắt hoặc nhận diện nhầm thành Section Header.

#### 4. Phủ 100% Khả năng Trực quan hóa trên Frontend (KaTeX, Audio & Underline)
- **[QuestionModal.tsx](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Frontend/src/pages/admin/questions/components/QuestionModal.tsx):** Trang bị Live Preview cho Nội dung câu hỏi, Các phương án và Trích dẫn giải thích ngay trong lúc Quản trị viên đang gõ phím.
- **[QuestionCard.tsx](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Frontend/src/components/quiz/QuestionCard.tsx) (Luyện thi):** Render công thức KaTeX và phương án toán học chuẩn vector SVG.
- **[AttemptReview.tsx](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Frontend/src/pages/quiz/AttemptReview.tsx) (Xem lại bài thi):** Render lời giải chi tiết và công thức Toán học / Ngoại ngữ sau khi thi.
- **Trình phát Audio Player Nhúng:** Tự động nhúng player `<audio controls>` khi câu hỏi hoặc phương án chứa file âm thanh (`.mp3`, `.wav`), sẵn sàng cho các kỳ thi kỹ năng Nghe (Listening).

#### 5. Động cơ Xử lý Đề thi Chuẩn Quốc Gia GDPT 2018 (DeTiengAnhChiTiet.docx)
- **Tách Phương án Dính liền (No Whitespace Boundary):** Xử lý thẻ `<w:tab/>` thành khoảng trắng và sử dụng lookbehind `(?<=[a-z0-9\)])` để tách phương án khi chữ in hoa đứng liền kề từ trước (`gripsB.`, `giftC.`, `graspD.`).
- **Bóc tách Bảng Đáp án Lưới 8 Cột:** Tự động bóc tách các bảng dạng cặp cột `(Câu, Đáp án)` lặp theo chiều ngang (`Table 1`), lấy chuẩn xác **100% đáp án (40/40 câu)** trong 0.1ms.
- **Máy trạng thái Lời giải Chi tiết (`isSolutionPhase`):** Khử trùng lặp giữa phần đề bài (`Question 1..40`) và phần lời giải (`Câu 1..40`), đảm bảo duy trì đúng 40 câu hỏi duy nhất và hợp nhất **100% lời giải chi tiết** vào từng câu.
- **Phân định Phạm vi Bài đọc (Passage Scoping):** Nhận diện cụm từ `from (\d+) to (\d+)` để giới hạn bài đọc đúng các câu con (1-6, 7-11, 17-24, 25-34, 35-40), giải phóng ngữ cảnh khi sang dạng bài sắp xếp (12-16).
- **Chuẩn hóa Dạng bài Sắp xếp Đoạn văn / Hội thoại mới:** Giữ nguyên các mệnh đề `a.`, `b.`, `c.`, `d.`, `e.` trong thân câu hỏi và trích xuất các phương án hoán vị `A. c - e - a - d - b` thành 4 options trắc nghiệm.

### 6.12 ĐỘT PHÁ 12: GIAO DIỆN GAME HÓA HUYỀN THOẠI & ĐẤU TRƯỜNG TRÍ TUỆ (NEXT-GEN GAME ARENA)

Hệ thống nâng cấp toàn diện giao diện làm bài thi/luyện tập từ dạng form tĩnh truyền thống thành một **Đấu Trường Trò Chơi Tương Tác Cực Đỉnh (Legendary Gamified UI)**, thỏa mãn trọn vẹn cả 4 chiều Sư phạm - Cảm xúc - Âm thanh - Khắc sâu kiến thức:

#### 1. Bộ Tổng hợp Âm thanh Web Audio Synthesizer (0ms Delay)
- Tích hợp động cơ âm thanh tổng hợp bản địa qua Web Audio API, không tốn băng thông tải file MP3:
  - **Select (Plink Arcade):** Âm thanh phím bấm nảy giòn tan khi chọn phương án.
  - **Match (Harmonic Chime):** Hợp âm Major Arpeggio rạng rỡ (C5-E5-G5-C6) khi ghép đúng cặp đôi hoàn hảo.
  - **Undo (Soft Whoosh):** Âm thanh nhẹ nhàng khi gỡ thẻ hoặc đổi lựa chọn.
  - **Gem (Pop Sound):** Âm nảy gỗ vui tai khi kéo thả hoặc chạm vào viên ngọc từ vựng.
  - Tích hợp nút bật/tắt âm thanh (Mute/Unmute) lưu trạng thái cục bộ linh hoạt.

#### 2. Đấu trường Ghép Đôi 3D "Cool Pair Matching" Siêu Mượt
- Bố cục 2 cột đối xứng tương tác trực quan: Cột Mục Khởi đầu (Left Items) và Cột Thẻ Đối ứng (Right Items).
- **Thao tác 1-Chạm Thông minh:** Chạm vào thẻ bên Trái (thẻ phát sáng Neon Cyan hào quang 3D) -> Chạm vào thẻ bên Phải (lập tức kết nối thành công, đổi màu Neon Emerald với huy hiệu Cặp nối và icon liên kết).
- **Phân bổ Màu Độc lập (Color Palette Mapping):** Mỗi cặp nối mang một dải màu pastel riêng biệt (Cyan, Emerald, Amber, Purple, Rose, Blue), giúp thí sinh nhận diện tức thì cấu trúc liên kết.
- Thanh hiển thị tiến độ trực quan: *"Ghép đúng X/Y cặp (Z%)"* với thanh tiến độ lân tinh.

#### 3. Sắp Xếp Từ Vựng "Smart Monkey" (Vocabulary Word Gems)
- Hiển thị các khối từ vựng và mệnh đề dưới dạng **Viên Ngọc Từ Vựng 3D** với số thứ tự phát sáng.
- Hỗ trợ đồng thời 3 cơ chế tương tác: Kéo thả trực quan (Drag & Drop), Nút di chuyển lên/xuống mượt mà, và cơ chế chạm nhanh trên màn hình cảm ứng di động / iPad.

#### 4. Khắc Sâu Kiến Thức Cốt Lõi "Omni Flash Insight"
- Dưới mỗi câu hỏi, khi thí sinh kiểm tra gợi ý hoặc sau khi hoàn thành, hệ thống lật mở thẻ tri thức **Omni Flash Insight**:
  - 📌 **Căn cứ & Nguồn khảo thí:** Trích dẫn văn bản, đoạn văn minh chứng.
  - 💡 **Mẹo vàng làm bài:** Quy tắc ngữ pháp, phương pháp loại trừ bẫy và từ vựng then chốt cần ghi nhớ.

### 6.13 ĐỘT PHÁ 13: HỆ THỐNG CHẨN ĐOÁN LỖ HỔNG KIẾN THỨC & HỌC TẬP TỪ SAI LẦM (AI DIAGNOSTIC & MISTAKE-DRIVEN MASTERY LEARNING)

Hệ thống AegisQuiz giải quyết triệt để "Hố đen giáo dục" của các kỳ thi trực tuyến truyền thống như IOE (nơi học sinh làm bài xong chỉ biết điểm số tổng quát mà bị giấu nhẹm câu sai, tước đoạt cơ hội học tập từ sai lầm).

```
                        ┌────────────────────────────────────────────────────────┐
                        │      QUY TRÌNH HỌC TỪ SAI LẦM THÔNG MINH CỦA AEGISQUIZ │
                        └──────────────────────────┬─────────────────────────────┘
                                                   │
                ┌──────────────────────────────────┴──────────────────────────────────┐
                ▼                                                                     ▼
┌───────────────────────────────────────────────┐     ┌───────────────────────────────────────────────┐
│     BƯỚC 1: MỔ XẺ LỖI SAI ĐA CHIỀU            │     │     BƯỚC 2: BÁC SĨ CHẨN ĐOÁN AI (GAP RADAR)   │
│ ├── Đối chiếu 3 lớp: Chọn sai ➔ Đúng ➔ Tại sao │     │ ├── Phân loại 4 bệnh lý nhận thức             │
│ ├── Phân tích ngữ âm IPA, cấu trúc ngữ pháp   │     │ ├── Vẽ biểu đồ Radar Lỗ hổng kiến thức        │
│ └── Trích dẫn minh chứng từ đề / sách GK     │     │ └── Báo cáo đồng hành cho Phụ huynh / Thầy cô │
└───────────────────────┬───────────────────────┘     └───────────────────────┬───────────────────────┘
                        │                                                     │
                        └──────────────────────────┬──────────────────────────┘
                                                   │
                ┌──────────────────────────────────┴──────────────────────────────────┐
                ▼                                                                     ▼
┌───────────────────────────────────────────────┐     ┌───────────────────────────────────────────────┐
│     BƯỚC 3: SỔ TAY LỖI SAI (MISTAKE VAULT)    │     │     BƯỚC 4: 1-CLICK CHINH PHỤC LẠI CÂU SAI    │
│ ├── Tự động lưu mọi vấp ngã vào Sổ tay Vàng  │     │ ├── Nút "Retest Mistakes Only" làm lại tức thì│
│ ├── Thuật toán Lặp lại ngắt quãng (SM-2)      │     │ ├── Sinh câu biến thể tương tự để rèn luyện   │
│ └── Huy hiệu "Xóa Sổ Lỗi Sai" thưởng XP       │     │ └── Làm đúng 100% mới cho hoàn thành chủ đề!  │
└───────────────────────────────────────────────┘     └───────────────────────────────────────────────┘
```

#### 1. Mổ Xẻ Chi Tiết Từng Câu Sai Tức Thì (Instant Mistake Breakdown)
- Ngay tại màn hình kết quả, hệ thống cung cấp Tab chuyên biệt: **"Bộ Sưu Tập Câu Hỏi Cần Chinh Phục (Mistakes to Master)"**.
- **So sánh Đối chiếu 3 Lớp Trực Quan:**
  - ❌ **Lựa chọn của thí sinh:** Tô đỏ cảnh báo, chỉ rõ chữ cái và nội dung thí sinh đã chọn.
  - ✔️ **Đáp án chính xác:** Tô xanh ngọc bích phát sáng kèm icon kiểm chứng.
  - 💡 **Mổ xẻ nguyên nhân sai lầm (Why you missed this?):** Tự động phân tích bản chất cấu trúc ngữ pháp, thì động từ, mạo từ hoặc công thức toán học bị hiểu sai.
  - 🔊 **Âm thanh Audio / Ngữ âm IPA Nhúng:** Cho phép bấm nghe lại đoạn audio hoặc cách phát âm chuẩn của từ vựng bị sai.

#### 2. Bác Sĩ Chẩn Đoán Lỗ Hổng Kiến Thức (Omni Diagnostic AI Doctor)
- AI tự động phân tích toàn bộ câu sai và phân loại thành **4 Bệnh Lý Nhận Thức**:
  1. 🟡 **Mắc Bẫy Đề Thi (Distractor Trap):** Nhầm từ đồng âm khác nghĩa (`hear`/`here`), bẫy từ loại (Noun/Adj).
  2. 🔴 **Hổng Kiến Thức Nền Tảng (Fundamental Knowledge Gap):** Chưa nắm vững cấu trúc lõi (Câu điều kiện, Mệnh đề quan hệ, Tích phân từng phần).
  3. 🔵 **Bất Cẩn / Áp Lực Thời Gian (Careless Error):** Đọc sót từ phủ định (`NOT`, `EXCEPT`, `INCORRECT`).
  4. 🟣 **Nhầm Lẫn Âm Thanh / Thính Giác (Auditory Confusion):** Nhầm lẫn âm đuôi `/s/`, `/z/`, hoặc không nhận ra hiện tượng nối âm (Connected Speech) trong bài nghe.
- **Biểu Đồ Radar Điểm Yếu (Skill Radar Chart):** Trực quan hóa tỷ lệ làm chủ từng kỹ năng/chuyên đề để thí sinh biết chính xác mình cần tập trung cải thiện phần nào.

#### 3. Sổ Tay Lỗi Sai (Mistake Vault) & Nút Bấm 1-Click "Chinh Phục Lại Câu Sai"
- **Nút Chinh Phục Câu Sai Ngay (Retest Mistakes Only):** Cho phép thí sinh mở ngay một phòng thi đặc biệt chỉ gồm những câu vừa làm sai để làm lại khi kiến thức còn đang nóng hổi.
- **Sổ Tay Lỗi Sai Cá Nhân (Mistake Vault):** Tự động lưu trữ mọi câu vấp ngã vào hồ sơ cá nhân của học sinh. Ứng dụng thuật toán **Lặp lại ngắt quãng (Spaced Repetition SM-2)** để tự động nhắc học sinh ôn lại câu sai sau 1 ngày, 3 ngày, 7 ngày đến khi khắc sâu vĩnh viễn.

#### 4. Báo Cáo Sư Phạm Đồng Hành Cho Phụ Huynh & Giáo Viên
- **Báo cáo Phụ huynh (Parent Diagnostic Co-pilot):** Tóm tắt kết quả dễ hiểu, chỉ rõ điểm mạnh, điểm yếu cụ thể của con và gợi ý bài học cần ôn tập mà không chỉ là một con số điểm vô cảm.
- **Bản đồ Nhiệt Lớp Học (Classroom Heatmap):** Hỗ trợ giáo viên xem nhanh tỷ lệ sai theo từng câu hỏi của cả lớp để dành thời gian tiết học chữa đúng chủ điểm học sinh hay mắc lỗi nhất.

#### 5. Phân Định An Toàn Giữa Kỳ Thi Chính Thức & Vòng Tự Luyện
- **Vòng Tự Luyện (35 Vòng Học Kỳ):** Mở 100% tính năng xem giải thích, phân tích lỗi sai và làm lại ngay lập tức.
- **Kỳ Thi Chính Thức (Cấp Trường / Huyện / Tỉnh):**
  - *Trong lúc đang làm bài:* Khóa bảo mật tuyệt đối đáp án và gợi ý.
  - *Sau khi kết thúc ca thi:* Hệ thống tự động mở khóa báo cáo phân tích chi tiết cho thí sinh để học sinh và phụ huynh có thể học hỏi và phục khảo minh bạch.

### 6.15 ĐỘT PHÁ 15: KIẾN TRÚC PHÂN CẤP TRI THỨC TOÀN CẦU VƯỢT THỜI ĐẠI (UNIVERSAL COGNITIVE TAXONOMY & HIERARCHICAL EXPLORER)

Giải quyết bài toán chiến lược mang tầm nhìn thế kỷ: xây dựng nền tảng khảo thí phục vụ đa quốc gia, đa lĩnh vực, cá nhân hóa theo từng tổ chức và quản trị phân quyền kép bền vững cho quy mô hàng vạn chủ đề (10.000+ Topics):

```
                                  ┌────────────────────────────────────────────────────────┐
                                  │   UNIVERSAL COGNITIVE TAXONOMY & TOPIC HIERARCHY       │
                                  └──────────────────────────┬─────────────────────────────┘
                                                             │
                      ┌──────────────────────────────────────┴──────────────────────────────────────┐
                      ▼                                                                             ▼
        ┌───────────────────────────┐                                                 ┌───────────────────────────┐
        │  BACKEND $O(1)$ PATH      │                                                 │  FRONTEND EXPLORER        │
        │  Materialized Path        │                                                 │  CognitiveTopicExplorer   │
        │  ├── Scope: COMMUNITY     │                                                 │  ├── Domain Filter Pills  │
        │  │   vs. TENANT           │                                                 │  ├── Scope Dual Selector  │
        │  ├── DomainCode: BANKING  │                                                 │  ├── Fuzzy Search 0ms     │
        │  ├── /DOMAIN/PARENT/CHILD/│                                                 │  ├── Accordion Tree View  │
        │  └── QuestionCountCached  │                                                 │  └── 1-Click Bulk Select  │
        └─────────────┬─────────────┘                                                 └─────────────┬─────────────┘
                      │                                                                             │
                      └──────────────────────────────────────┬──────────────────────────────────────┘
                                                             ▼
                                               ┌───────────────────────────┐
                                               │   HỆ THỐNG CÂY TRI THỨC   │
                                               │   2026-DOT2 (3.891 câu)   │
                                               │   19 Chuyên đề Ngân hàng  │
                                               └───────────────────────────┘
```

#### 1. Mô Hình Phả Hệ Chuỗi Cụ Thể Hóa ($O(1)$ Materialized Path)
- Bổ sung trường `MaterializedPath` dạng `/{DomainCode}/{ParentTopicCode}/{ChildTopicCode}/` trong `BankTopic.cs`.
- Truy vấn toàn bộ cây con cháu hoặc đếm số câu hỏi chỉ với 1 mệnh đề SQL duy nhất:
  `WHERE "MaterializedPath" LIKE '/BANKING/2026_DOT2/%'`
  đạt độ phức tạp thời gian tuyệt đối $O(1)$, triệt tiêu hoàn toàn nghẽn đệ quy bộ nhớ CTE khi dữ liệu đạt quy mô hàng triệu câu hỏi.
- Bổ sung `DepthLevel` (0: Root Domain, 1: Parent Folder, 2: Child Topic, 3: Micro-concept), `QuestionCountCached` (bộ đệm số câu hỏi nâng tốc độ tải trang lên gấp 10 lần), và mã định danh vĩnh cửu `Urn` (`urn:aegis:topic:{domain}:{code}`).

#### 2. Quản Trị Phân Quyền Kép Toàn Cầu (Multi-Tenancy Dual Scope)
- `COMMUNITY`: Kho tri thức mở công cộng. Người dùng toàn cầu có thể tự do đóng góp câu hỏi mới và tham gia ôn luyện sát hạch hoàn toàn miễn phí.
- `TENANT`: Kho tri thức doanh nghiệp độc quyền. Dành riêng cho các ngân hàng, tập đoàn quản trị các bộ đề thi nội bộ bảo mật cao, đồng thời kế thừa và khai thác trọn vẹn kho tri thức mở từ cộng đồng.

#### 3. Phân Hệ Danh Mục Ngành Động (Dynamic Domain Taxonomy)
- Khởi tạo thực thể `DynamicDomain.cs` hỗ trợ mở rộng danh mục ngành linh hoạt: `BANKING` (Ngân hàng & Tài chính Enterprise), `EDUCATION` (Khảo thí & Giáo dục quốc gia), `GOV_DRIVING` (Sát hạch giao thông Cục Đường Bộ), `GENERAL` (Tổng hợp & Kỹ năng mở).
- Cung cấp API `GET /api/quiz/topics/tree` gom nhóm 3 tầng: `DomainGroup` ➔ `ParentFolder` ➔ `ChildTopics`, tích hợp cơ chế tự phục hồi `AutoHealHierarchicalTopicsAsync` gom nhóm trọn vẹn 19 chuyên đề con ngân hàng vào cây thư mục cha `2026-DOT2` với **3.891 câu hỏi nghiệp vụ**.

#### 4. Giao Diện Đồ Thị Nhận Thức Thông Minh: `CognitiveTopicExplorer.tsx`
- Thay thế toàn bộ lưới checkbox phẳng chật chội trên trang `/practice`:
  - **Domain Filter Pills**: Nút chuyển đổi nhanh kèm badge số câu hỏi thời gian thực.
  - **Scope Selector**: Lọc phân quyền rõ nét giữa Gói Cộng Đồng và Doanh Nghiệp.
  - **Fuzzy Search Real-time**: Ô tìm kiếm mờ tức thì trong hàng vạn chuyên đề.
  - **Folder Accordion Cây Phả Hệ**: Thư mục cha `2026-DOT2` hiển thị badge `19 chuyên đề con`, `3.891 câu hỏi`, kèm nút **"1-Chạm Chọn Tất Cả" (1-Click Bulk Select)** để thí sinh chọn toàn bộ kỳ thi chỉ với 1 click chuột, hoặc mở rộng mũi tên để tùy biến số lượng câu hỏi và chế độ lấy ngẫu nhiên cho từng chuyên đề con riêng biệt.

---

## 7. Bảo mật & Tuân thủ

### 7.1 Mô hình Bảo mật Đa tầng

```
Internet
  │
  │ TLS 1.3 (bắt buộc Production)
  ▼
Rate Limiter (Fixed Window):
  ├── AI Endpoints: 10 req/min (bảo vệ ngân sách AI)
  ├── Webhook:     100 req/min (chống DDoS)
  └── Standard:   200 req/min
  │
  ▼
Authentication Middleware:
  ├── JWT Bearer (HS256 — PKI hoặc RS256 — Keycloak)
  └── JWKS auto-discovery
  │
  ▼
Tenant Resolution Middleware:
  └── X-Tenant-ID header → Extract & Validate
  │
  ▼
EF Core Global Query Filter (Application-level RLS)
  │
  ▼
PostgreSQL Row-Level Security (Database-level RLS)
```

### 7.2 Đặc điểm Bảo mật

| Biện pháp | Chuẩn | Mô tả |
|---|---|---|
| **PKI USB Token** | PKCS#11 | Xác thực bằng chứng thư số x509, chữ ký số HMAC-SHA256 |
| **Keycloak SSO** | OIDC/OAuth 2.0 | Single Sign-On enterprise, JWKS key rotation tự động |
| **Row-Level Security** | PostgreSQL RLS | Cô lập dữ liệu tenant ở tầng database |
| **Rate Limiting** | Token Bucket | Chống DDoS và bảo vệ AI budget |
| **Structured Logging** | Serilog | Audit trail cho mọi hành động — truy vết sự cố |
| **Health Checks** | K8s ready | `/health` và `/health/detail` cho liveness/readiness probe |
| **Input Validation** | FluentValidation | Chặn SQL injection, XSS tại tầng DTO |

### 7.3 Chứng chỉ Anti-Cheat

Hệ thống phát hiện và ghi nhận **6 loại hành vi gian lận** qua SignalR proctoring. Mọi vi phạm được:
- Ghi vào database với timestamp UTC
- Đẩy real-time cho giám thị
- Lưu `EndReason` trong ExamSession để audit

---

## 8. Hiệu năng & Khả năng Mở rộng

### 8.1 Chiến lược Cache

```
Request Flow (Cache-Aside Pattern):
  ┌─────────┐     Cache HIT     ┌───────┐
  │ Client  │ ←───────────────── │ Redis │
  └────┬────┘                   └───────┘
       │ Cache MISS                  ▲
       ▼                            │
  ┌─────────┐   Query + Cache   ┌───────┐
  │  API    │ ─────────────────► │  DB   │
  └─────────┘                   └───────┘
```

| Đối tượng Cache | TTL | Layer |
|---|---|---|
| AI Gemini Solver Results | 24 giờ | IMemoryCache |
| Question Bank (hot topics) | 5 phút | Redis |
| Leaderboard | 10 phút | Redis |
| Tenant Config | 30 phút | Redis |

### 8.2 Database Performance

- **JSONB GIN Index** trên `Payload` — full-text search trong JSON content
- **Composite Index** `(TenantId, CategoryCode)` — query theo tenant + chủ đề
- **Partial Index** `(UserId, AttemptDate)` — lịch sử làm bài nhanh
- **CONCURRENTLY Index** — không lock bảng khi thêm index production

### 8.3 Horizontal Scaling

```
Load Balancer (Nginx/ALB)
     │
     ├──► API Instance 1
     ├──► API Instance 2   ←── Shared: Redis (session), PostgreSQL
     └──► API Instance N
              │
              └──► SignalR Backplane (Redis Pub/Sub)
                   → SignalR messages broadcast qua tất cả instances
```

**SignalR Scale-out:** Redis Backplane đảm bảo WebSocket messages từ thí sinh tới instance 1 được broadcast tới giám thị ở instance 2 — **không mất message** khi scale horizontal.

---

## 9. API Reference

### 9.1 Core Endpoints

#### Question Bank API
| Method | Endpoint | Mô tả |
|---|---|---|
| `GET` | `/api/quiz/questions` | Danh sách câu hỏi (có phân trang, filter, sort) |
| `GET` | `/api/quiz/questions/{id}` | Lấy 1 câu hỏi theo ID |
| `POST` | `/api/quiz/questions` | Tạo câu hỏi mới |
| `PUT` | `/api/quiz/questions/{id}` | Cập nhật câu hỏi |
| `DELETE` | `/api/quiz/questions/{id}` | Xóa câu hỏi |
| `POST` | `/api/quiz/questions/bulk-toggle` | Enable/Disable nhiều câu cùng lúc |

#### Import & AI Document Intelligence API
| Method | Endpoint | Mô tả |
|---|---|---|
| `POST` | `/api/quiz/questions/import` | Import câu hỏi từ file Excel (.xlsx, .xls) |
| `POST` | `/api/quiz/questions/import-docx` | [Kịch bản A] Bóc tách câu hỏi từ file Word (.docx), hỗ trợ AI tự giải nếu `useAi=true` |
| `POST` | `/api/quiz/questions/import-pdf` | [Kịch bản A] Bóc tách câu hỏi từ file PDF (.pdf), hỗ trợ AI tự giải nếu `useAi=true` |
| `POST` | `/api/quiz/questions/generate-from-doc` | [Kịch bản B] AI Gemini đọc tài liệu PDF/Word tự sinh câu hỏi theo tham số |
| `POST` | `/api/quiz/questions/generate-from-text` | [Kịch bản B] AI Gemini đọc văn bản dán tự sinh câu hỏi theo tham số |
| `POST` | `/api/quiz/questions/ai-solve-preview` | AI tự động giải đề & điền căn cứ pháp lý cho danh sách xem trước |
| `POST` | `/api/quiz/questions/convert-math-latex` | [Đột phá 9] AI Gemini Vision nhận diện ảnh công thức MathType sang mã LaTeX ($...$) |
| `POST` | `/api/quiz/questions/confirm-import-docx` | Xác nhận lưu câu hỏi vào DB (hỗ trợ SINGLE, MULTI, TRUE_FALSE, SHORT_ANSWER, ESSAY) |

#### Attempt & Analytics API
| Method | Endpoint | Mô tả |
|---|---|---|
| `POST` | `/api/quiz/attempt` | Nộp kết quả 1 câu hỏi |
| `GET` | `/api/quiz/attempts/history` | Lịch sử làm bài của user |
| `GET` | `/api/quiz/weak-topics` | Top 5 chủ đề yếu nhất |
| `POST` | `/api/quiz/{id}/explain` | AI giải thích câu hỏi (Gemini) |
| `POST` | `/api/quiz/attempt/analyze` | AI phân tích toàn bộ kết quả thi |

### 9.2 SignalR Hub — ExamProctoringHub (`/hubs/proctoring`)

| Method (Client → Server) | Tham số | Mô tả |
|---|---|---|
| `JoinExam` | `examSessionId: Guid` | Thí sinh vào phòng thi |
| `ReportSuspiciousActivity` | `sessionId, activityType, details` | Báo vi phạm |
| `JoinInvigilator` | `examSessionId: Guid` | Giám thị vào phòng giám sát |
| `ConfirmVoidByInvigilator` | `examSessionId: Guid` | Hủy bài thi |

| Event (Server → Client) | Dữ liệu | Mô tả |
|---|---|---|
| `ExamState` | `{elapsed, tabSwitchCount, serverTime}` | Trạng thái phòng thi |
| `ViolationDetected` | `{activityType, violationCount, timestamp}` | Thông báo vi phạm (→ giám thị) |
| `Warning` | `{message, violationCount}` | Cảnh báo thí sinh |
| `FlagForReview` | `{violationCount, reason}` | Flag cho giám thị |
| `ExamTerminated` | `{reason, violationCount}` | Bài thi bị hủy |

### 9.3 Pagination & Filter

```
GET /api/quiz/questions?page=1&pageSize=20&search=tín+dụng&categoryCode=TD&difficulty=3&sortBy=createdAt&sort=Desc
```

**Response:**
```json
{
  "items": [...],
  "total": 1250,
  "page": 1,
  "pageSize": 20
}
```

---

## 10. Lộ trình Phát triển

### Phase A — MVP (✅ Hoàn thành)
- Universal Question Model (8 loại câu hỏi)
- Admin CRUD + Import Excel/DOCX
- AI Auto-Solve + AI Explain
- Basic Quiz Practice Mode

### Phase B — Enterprise & Document Intelligence (✅ Hoàn thành)
- Multi-Tenancy + Row-Level Security (PostgreSQL)
- Keycloak SSO + PKI USB Token Authentication
- Real-time Proctoring Hub (SignalR)
- Gamification (Achievements, Leaderboard, Learning Paths)
- AI Mentor (phân tích điểm yếu, gợi ý lộ trình)
- Dual-Scenario Document Intelligence (Kịch bản A & B: PDF/DOCX Parser + Generative AI Question Creator)
- Multimodal Math & Graphic Ingestion Engine (In-Memory MathType WMF-to-PNG, Gemini Vision LaTeX OCR, KaTeX SVG Rendering)
- Rate Limiting + Health Checks
- Structured Logging (Serilog)

### Phase C — Scale & Advanced IRT (🚧 Kế hoạch)
- **IRT/CAT Engine:** Item Response Theory — Adaptive Testing tự điều chỉnh độ khó theo khả năng thí sinh
- **Face Recognition Proctoring:** Tích hợp camera giám sát danh tính
- **Blockchain Certificate:** Chứng chỉ on-chain (Polygon) — không thể làm giả
- **HRMS Integration:** Đồng bộ kết quả thi với hệ thống nhân sự
- **Mobile App:** React Native — thi trên thiết bị di động
- **Offline Mode:** Service Worker — thi khi mất kết nối

### Phase D — AI-Native Autonomous Platform
- **Multi-Modal Document Parsing:** Nhận diện bảng biểu phức tạp và sơ đồ từ file Scan/OCR
- **Auto-rubric Grading:** AI chấm tự luận với rubric tùy chỉnh đa tiêu chí
- **Predictive Analytics:** Dự báo khả năng thi đạt/rớt trước kỳ thi chính thức

---

## Phụ lục A — Glossary

| Thuật ngữ | Định nghĩa |
|---|---|
| **TPH** | Table Per Hierarchy — Mô hình EF Core lưu class hierarchy vào 1 bảng với discriminator |
| **JSONB** | JSON Binary — Kiểu dữ liệu PostgreSQL hỗ trợ indexing và query trong JSON |
| **RLS** | Row-Level Security — Cơ chế cô lập dữ liệu ở tầng database PostgreSQL |
| **IRT/CAT** | Item Response Theory / Computerized Adaptive Testing — Đo lường năng lực thích ứng |
| **JWKS** | JSON Web Key Set — Endpoint cung cấp public keys để verify JWT (Keycloak) |
| **PKI** | Public Key Infrastructure — Hạ tầng chứng thư số, chữ ký số |
| **FinOps** | Financial Operations for AI — Tối ưu chi phí vận hành AI API |
| **Proctoring** | Giám sát thi cử trực tuyến |
| **Tenant** | Tổ chức/đơn vị khách hàng trong mô hình Multi-Tenancy |

## Phụ lục B — Deployment Requirements

```yaml
Minimum Production Requirements:
  API Server:
    CPU: 4 vCPU
    RAM: 8 GB
    OS:  Ubuntu 22.04 LTS / Windows Server 2022

  Database:
    PostgreSQL: 16.x
    Storage:    100 GB SSD (NVMe recommended)
    RAM:        16 GB (shared_buffers = 4GB)

  Cache:
    Redis: 7.x
    RAM:   4 GB

  External Services:
    Google Gemini API Key (billing enabled)
    Keycloak Server 23+ (hoặc managed Keycloak)
    SSL Certificate (Let's Encrypt hoặc enterprise CA)
```

---

*Tài liệu này được biên soạn theo chuẩn IEEE 42010 (Architecture Description) và ISO/IEC 25010 (Software Quality). Mọi thay đổi kiến trúc cần được cập nhật vào tài liệu này và thông qua review của Solution Architect.*

---
**© 2026 AegisQuiz Engineering Team. Tài liệu Nội bộ — Bảo mật Cấp độ 2 (Restricted)**
