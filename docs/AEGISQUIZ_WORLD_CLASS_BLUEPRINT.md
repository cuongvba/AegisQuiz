# 🌐 AEGISQUIZ — WORLD-CLASS PLATFORM BLUEPRINT
## Tài liệu Chiến lược Nâng cấp Hệ thống Hàng đầu Thế giới
> **Phiên bản**: 1.0 · **Ngày**: 2026-08-27  
> **Tác giả**: Aegis Prime (God-Tier Architect)  
> **Mục tiêu**: Top 5 Global Banking & Academic Training Platform  
> **Phạm vi**: Ngân hàng toàn cầu · Đại học · Tổ chức Tài chính · FinTech

---

## MỤC LỤC

1. [Tổng quan Tầm nhìn Chiến lược](#1-tổng-quan-tầm-nhìn-chiến-lược)
2. [Kiểm tra Thực trạng Kiến trúc Hiện tại](#2-kiểm-tra-thực-trạng-kiến-trúc-hiện-tại)
3. [Ma trận GAP Analysis](#3-ma-trận-gap-analysis)
4. [Phase 0 — Stabilize & Launch](#4-phase-0--stabilize--launch-hiện-tại--2-tháng)
5. [Phase 1 — Multi-Tenant Foundation](#5-phase-1--multi-tenant-foundation-tháng-3-6)
6. [Phase 2 — AI-Native Intelligence Platform](#6-phase-2--ai-native-intelligence-platform-tháng-7-12)
7. [Phase 3 — Global Market Domination](#7-phase-3--global-market-domination-năm-2-3)
8. [Kiến trúc Kỹ thuật Mục tiêu](#8-kiến-trúc-kỹ-thuật-mục-tiêu-world-class)
9. [Security & Compliance Framework](#9-security--compliance-framework)
10. [Chiến lược Kinh doanh & Monetization](#10-chiến-lược-kinh-doanh--monetization)
11. [KPIs & Metrics](#11-kpis--metrics-đo-lường-thành-công)
12. [Rủi ro & Mitigation Plan](#12-rủi-ro--mitigation-plan)

---

## 1. TỔNG QUAN TẦM NHÌN CHIẾN LƯỢC

### 1.1 Tuyên ngôn Sứ mệnh

> *"AegisQuiz là nền tảng đào tạo trí tuệ nhân tạo đầu tiên trên thế giới tích hợp xác thực PKI cấp ngân hàng, chấm điểm tự luận bằng AI, và học tập thích ứng cá nhân hóa — giúp mọi tổ chức tài chính và giáo dục trên toàn cầu xây dựng đội ngũ xuất sắc với chi phí tối thiểu và hiệu quả tối đa."*

### 1.2 Định vị Thị trường

```
                    AI Sophistication
                         ▲
                         │
              AegisQuiz  │  ← Target Position (2027)
              Target ●   │
                         │   ● Coursera
              ● Kahoot!  │            ● Docebo
                         │  ● Moodle
─────────────────────────┼──────────────────────── Banking Specialization
         Generic         │                Specialized
                         │
             AegisQuiz   │
             Current ●   │
                         │
```

### 1.3 Thị trường Mục tiêu (TAM/SAM/SOM)

| Phân khúc | TAM (Toàn cầu) | SAM (Châu Á) | SOM (Năm 3) |
|---|---|---|---|
| **Banking Training** | $18.2B/năm | $4.1B/năm | $45M/năm |
| **Academic EdTech** | $404B/năm | $87B/năm | $12M/năm |
| **Compliance Training** | $32B/năm | $7.8B/năm | $8M/năm |
| **FinTech Certification** | $5.6B/năm | $2.1B/năm | $6M/năm |
| **Tổng** | **$459.8B** | **$101B** | **$71M** |

---

## 2. KIỂM TRA THỰC TRẠNG KIẾN TRÚC HIỆN TẠI

### 2.1 Inventory Toàn bộ Hệ thống

#### Backend (.NET 10 — Clean Architecture)

```
AegisQuiz.Domain/
├── Entities/
│   ├── QuestionBase.cs        ← Universal JSONB Model (8 loại câu hỏi)
│   ├── ExamSession.cs         ← Anti-cheat: TabSwitch, Proctoring
│   ├── LearningPath.cs        ← Path/Step/Progress tracking
│   ├── LearningAchievement.cs ← XP, Badge, Trigger system
│   ├── LearnerLeaderboard.cs  ← Pre-computed & Real-time rankings
│   ├── PaymentTransaction.cs  ← Monetization layer
│   ├── Notebook.cs            ← AI-assisted note-taking
│   └── QuestionAttempt.cs     ← Per-question tracking
```

**Đánh giá Domain**: ⭐⭐⭐⭐½ — Cực kỳ robust, design linh hoạt. 
**Điểm cần cải thiện**: Thiếu `TenantId`, thiếu `UserId` audit trail, thiếu `SoftDelete`.

#### Backend — Infrastructure Layer

```
AegisQuiz.Infrastructure/
├── AI/
│   ├── GeminiGradingService.cs   ← Gemini 1.5 Flash, JSON parsing
│   ├── GeminiMentorService.cs    ← Study plan generation
│   ├── GeminiNotebookService.cs  ← AI note assistance
│   └── GeminiSolverService.cs    ← Missing answer detection
├── Data/
│   └── AegisQuizDbContext.cs     ← TPH Discriminator, JSONB, Indexes
```

**Phát hiện quan trọng**: `GeminiGradingService` dùng `HttpClient` trực tiếp thay vì `IHttpClientFactory` → **Memory leak** trong production.

#### API Layer

```
AegisQuiz.API/
├── Controllers/
│   ├── QuizController.cs         ← 1075 lines, GOD CLASS anti-pattern!
│   ├── AuthController.cs         ← Keycloak OAuth2
│   ├── PkiAuthController.cs      ← Agribank USB Token PKI (unique!)
│   ├── MentorController.cs       ← AI Mentor endpoints
│   └── WebhookController.cs      ← Payment webhook
├── Hubs/
│   └── ExamProctoringHub.cs      ← SignalR WebSocket
├── Services/
│   └── ExamTimerService.cs       ← Server-authoritative timer
```

**Phát hiện quan trọng**:
1. `QuizController.cs` — 1075 dòng là **GOD CLASS anti-pattern**, cần chia thành 8+ controllers
2. `ExplainQuestion()` dùng hardcoded template thay vì gọi Gemini thật → **Misleading code**
3. `AnalyzeAttempt()` dùng `Task.Delay(1200)` giả lập AI → **Technical debt nghiêm trọng**
4. `GetQuestions()` gọi `ToListAsync()` không có phân trang → **N+1 / OOM risk** khi DB lớn

#### Frontend (React 19 + Vite + TypeScript)

```
src/
├── pages/
│   ├── admin/
│   │   └── AdminQuestionsPage.tsx  ← 107KB! GOD FILE
│   ├── quiz/ (AttemptRoom, SecureExamRoom, AttemptReview)
│   ├── gamification/ (Leaderboard, Achievements)
│   ├── paths/ (PathExplorer)
│   └── paywall/ (PaywallPage)
├── services/
│   └── learner-quiz.service.ts   ← 398 lines, tốt (normalizeQuestion robust)
├── hooks/
│   └── useAgribankPKI.ts         ← PKI WebSocket hook (unique!)
```

**AdminQuestionsPage.tsx — 107KB** là file frontend lớn nhất từng gặp trong production. Cần refactor ngay.

#### Infrastructure-as-Code

```
infra/main.tf        ← GCP: Cloud Run + Cloud SQL + Memorystore
docker-compose.yml   ← Postgres 17 + Redis 7 + Keycloak 25
```

**Đánh giá IaC**: ⭐⭐⭐⭐ — GCP Cloud Run là lựa chọn đúng. Thiếu: Secret Manager, CDN, monitoring stack.

### 2.2 Điểm Mạnh Độc quyền (Moat Analysis)

| # | Unique Differentiator | Mô tả Kỹ thuật | Có thể Copy? |
|---|---|---|---|
| 1 | **PKI USB Token Auth** | `useAgribankPKI.ts` + `PkiAuthController.cs` — WebSocket tới local PKI plugin, Dual-Issuer JWT | 🔴 Rất khó — cần partnership ngân hàng |
| 2 | **Universal JSONB Question** | TPH + JSONB Payload — 8 loại câu hỏi không cần migration mới | 🟡 Khó — cần rethink architecture |
| 3 | **AI-Native from Day 1** | 4 Gemini services embedded vào core workflow, không phải addon | 🟡 Khó — cần Gemini API access |
| 4 | **Banking Domain Expertise** | Câu hỏi chuyên ngành tài chính, CIIA, banking regulation | 🔴 Rất khó — cần domain expert |
| 5 | **Server-Authoritative Exam** | `ExamTimerService` + SignalR — chống gian lận client-side timer | 🟢 Có thể copy nhưng đã đi trước |

---

## 3. MA TRẬN GAP ANALYSIS

### 3.1 Gap Priority Matrix

```
                    Impact (High → Low)
                    ▲
         CRITICAL   │   MUST-DO
                    │
    Multi-Tenancy ●─┤─● SCORM/xAPI
         i18n ●─────┤─● Essay Grading (fix)
   Pagination ●─────┤─● QuizController refactor
                    │
        HIGH        │   SHOULD-DO  
    CAT/IRT ●───────┤─● Advanced Proctoring
  Analytics ●───────┤─● Content Authoring
   Mobile App●──────┤─● BFF Pattern
                    │
        MEDIUM      │   NICE-TO-HAVE
     SRS ●──────────┤─● Blockchain Cert
   Marketplace ●────┤─● Voice Assessment
                    │
─────────────────────────────────────────── Effort
            Easy         Medium         Hard
```

### 3.2 Chi tiết từng GAP

#### 🔴 GAP-01: Thiếu Multi-Tenancy (CRITICAL — Blocker)

**Hiện trạng**: 
- `AegisQuizDbContext` không có `TenantId` trong bất kỳ entity nào
- Mọi query đều shared data giữa tất cả users/organizations

**Tác động kinh doanh**: 
- Không thể bán cho 2 ngân hàng khác nhau mà data isolated
- GDPR/Data Sovereignty violation khi deploy ở EU

**Giải pháp Kỹ thuật**:
```csharp
// Pattern 1: Row-Level Security (Khuyến nghị cho shared DB)
public abstract class TenantEntity
{
    public Guid TenantId { get; set; }
}

// Trong DbContext — tự động filter theo TenantId
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<QuestionBase>()
        .HasQueryFilter(q => q.TenantId == _tenantContext.CurrentTenantId);
}

// Pattern 2: Schema-per-tenant (Cho banking với isolation cao)
// Mỗi ngân hàng = 1 PostgreSQL schema riêng: "agribank", "vietcombank"
```

**Effort**: 2 tuần Backend + 3 ngày Frontend

---

#### 🔴 GAP-02: Thiếu i18n & L10n (CRITICAL — Global Blocker)

**Hiện trạng**:
- UI hardcoded tiếng Việt
- Error messages trong `QuizController` bằng tiếng Việt
- Seed data chỉ có câu hỏi tiếng Việt

**Tác động kinh doanh**: 
- Không deploy được cho ING Bank (Netherlands), Maybank (Malaysia), BDO (Philippines)

**Giải pháp**:
```
Frontend: react-i18next + locale files
  └── locales/
      ├── vi/translation.json
      ├── en/translation.json
      ├── ja/translation.json   ← JLPT/Japanese banking market
      ├── ar/translation.json   ← RTL support
      └── ko/translation.json   ← Korean banking (KB, Shinhan)

Backend: Resource files + Accept-Language header parsing
  └── Resources/
      ├── ErrorMessages.vi.resx
      └── ErrorMessages.en.resx
```

**Effort**: 1 tuần setup + ongoing content translation

---

#### 🔴 GAP-03: QuizController GOD CLASS (CRITICAL — Maintainability)

**Hiện trạng**: 
- `QuizController.cs` — **1075 dòng**, 25+ endpoints trong 1 file
- Vi phạm Single Responsibility Principle nghiêm trọng
- `ExplainQuestion()` fake AI (hardcoded template)
- `AnalyzeAttempt()` fake AI (`Task.Delay(1200)`)

**Giải pháp**:
```
Tách thành:
├── QuestionsController.cs       ← CRUD, Import, Export (200 lines)
├── AttemptController.cs         ← Submit, Review, Analyze (150 lines)  
├── AdaptiveController.cs        ← CAT engine, Practice (150 lines)
├── TopicsController.cs          ← CRUD chủ đề (100 lines)
├── LeaderboardController.cs     ← Rankings, Stats (100 lines)
├── AchievementsController.cs    ← Badges, XP (100 lines)
└── LearningPathController.cs    ← Paths, Progress (150 lines)
```

---

#### 🔴 GAP-04: Thiếu Pagination (CRITICAL — OOM Risk)

**Hiện trạng**:
```csharp
// GetQuestions() — Load toàn bộ database vào RAM!
var questions = await _dbContext.Questions.ToListAsync();
```

**Tác động**: 10,000 câu hỏi → OOM crash trong production

**Giải pháp**:
```csharp
// Cursor-based pagination (tốt hơn offset cho large datasets)
[HttpGet("questions")]
public async Task<IActionResult> GetQuestions(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? cursor = null)
{
    var query = _dbContext.Questions.AsQueryable();
    var total = await query.CountAsync();
    var items = await query
        .OrderBy(q => q.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    return Ok(new PagedResult<QuestionBase> { Items = items, Total = total, Page = page });
}
```

---

#### 🔴 GAP-05: Essay Grading chưa implement (CRITICAL — Feature Gap)

**Hiện trạng**:
```csharp
// EssayQuestion.EvaluateAnswer() — Throws exception!
public override double EvaluateAnswer(string studentAnswerJson)
{
    throw new NotImplementedException("Essay questions must be graded asynchronously via Gemini AI Service.");
}
```

**Giải pháp**: Async grading pipeline

```csharp
// Pattern: Command + Background Queue
public class EssayGradingJob
{
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public string StudentAnswer { get; set; }
    public string Rubric { get; set; }
}

// Sử dụng Hangfire hoặc .NET BackgroundService + Channel<T>
public class EssayGradingWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var job in _channel.ReadAllAsync(ct))
        {
            var result = await _geminiGrading.GradeEssayAsync(...);
            await _db.SaveGradingResultAsync(job.AttemptId, result);
            await _hub.Clients.User(userId).SendAsync("EssayGraded", result);
        }
    }
}
```

---

#### 🟡 GAP-06: Thiếu IRT/CAT Engine (HIGH — Competitive Advantage)

**Hiện trạng**:
```csharp
// GenerateAdaptiveQuiz() — Chỉ lọc theo weakestCategory (naive approach)
var weakestCategory = history
    .GroupBy(h => h.Category)
    .OrderBy(s => s.Accuracy)
    .FirstOrDefault()?.Category ?? "Toán";
```

**Vấn đề**: Không tính đến difficulty calibration, không dự đoán probability of success.

**Giải pháp — Item Response Theory (IRT) 3-Parameter Model**:

```python
# Python FastAPI microservice (hiệu năng cao hơn C# cho ML)
# IRT 3PL: P(θ) = c + (1-c) / (1 + exp(-a*(θ-b)))
# θ = learner ability, a = discrimination, b = difficulty, c = guessing

class IRTEngine:
    def estimate_ability(self, responses: List[Response]) -> float:
        """Maximum Likelihood Estimation của θ (người học)"""
        ...
    
    def select_next_question(self, theta: float, bank: List[Question]) -> Question:
        """CAT: Chọn câu có Maximum Information tại θ hiện tại"""
        info = [self.information(q.b, theta) for q in bank]
        return bank[np.argmax(info)]
    
    def should_stop(self, se: float, threshold: float = 0.3) -> bool:
        """Dừng khi Standard Error of Measurement < threshold"""
        return se < threshold
```

**Kết quả**: Giảm 40-60% số câu hỏi cần thiết, tăng accuracy đánh giá năng lực.

---

#### 🟡 GAP-07: Thiếu SCORM/xAPI Compliance (HIGH — B2B Sales)

**Hiện trạng**: Proprietary protocol, không tương thích LMS standards

**Tác động kinh doanh**: 
- 90% Doanh nghiệp/Đại học đang dùng Moodle, Blackboard, SAP SuccessFactors, Canvas
- Không có SCORM = không thể bán cho họ

**Giải pháp**:
```csharp
// SCORM 2004 Package Generator
public class ScormPackageService
{
    public async Task<byte[]> ExportAsScorm(Guid courseId)
    {
        // 1. Sinh imsmanifest.xml
        // 2. Đóng gói câu hỏi + media vào ZIP
        // 3. Implement SCORM API (LMSInitialize, LMSSetValue, LMSCommit...)
    }
}

// xAPI (Tin Can) Statement Builder
public class XApiService
{
    public Statement BuildAttemptStatement(QuestionAttempt attempt)
    {
        return new Statement
        {
            Actor = new Agent { Name = attempt.UserId.ToString() },
            Verb = new Verb { Id = "http://adlnet.gov/expapi/verbs/answered" },
            Object = new Activity { Id = $"https://aegisquiz.com/questions/{attempt.QuestionId}" },
            Result = new Result { Score = new Score { Raw = attempt.IsCorrect ? 1 : 0 } }
        };
    }
}
```

---

#### 🟡 GAP-08: Thiếu Advanced Proctoring (HIGH — Banking Requirement)

**Hiện trạng**: Chỉ có `NumberOfTabSwitches` counter

**Target cho Banking**:
```
Level 1: Tab-switch detection ✅ (đã có)
Level 2: Copy-paste prevention ⬜
Level 3: Full-screen enforcement ⬜ (Fullscreen API)
Level 4: AI face detection (MediaPipe) ⬜
Level 5: Gaze tracking ⬜ 
Level 6: Environment scanning ⬜ (phát hiện người khác trong phòng)
```

**Giải pháp Level 4-5**:
```typescript
// Frontend: MediaPipe Face Detection
import { FaceMesh } from '@mediapipe/face_mesh';

const faceMesh = new FaceMesh({ locateFile: ... });
faceMesh.onResults((results) => {
    const faceCount = results.multiFaceLandmarks.length;
    if (faceCount === 0) proctoring.flagEvent('FACE_NOT_DETECTED');
    if (faceCount > 1) proctoring.flagEvent('MULTIPLE_FACES');
    
    // Gaze estimation từ eye landmarks
    const gazeDirection = estimateGaze(results.multiFaceLandmarks[0]);
    if (gazeDirection === 'LOOKING_AWAY') proctoring.flagEvent('GAZE_DEVIATION');
});
```

---

#### 🟡 GAP-09: Thiếu Analytics & BI Engine (HIGH — Decision Support)

**Hiện trạng**: Không có analytics dashboard cho Tổ chức/Admin

**Target — World-Class Analytics**:
```
Learning Analytics Dashboard:
├── Cohort Analysis (nhóm học theo lứa, bộ phận)
├── Knowledge Retention Curve (Ebbinghaus forgetting curve)
├── Learning Velocity (tốc độ tiến bộ theo tuần)
├── Topic Heatmap (chủ đề nào yếu nhất toàn tổ chức)
├── Exam Performance Trends
├── ROI Report (chi phí đào tạo vs năng suất)
└── Compliance Tracker (% nhân viên hoàn thành bắt buộc)
```

**Giải pháp**:
```
OLTP: PostgreSQL (operational data)
    ↓ CDC (Debezium / pg_logical)
Event Stream: Apache Kafka / GCP Pub/Sub
    ↓ Stream Processing
OLAP: ClickHouse (analytics queries)
    ↓ BI Layer
Dashboard: Metabase (embedded, open-source) hoặc Apache Superset
```

---

#### 🟡 GAP-10: Thiếu Content Authoring Tool (HIGH — Productivity)

**Hiện trạng**: Chỉ import từ Excel/Docx, không tạo trong app

**Target**:
```
Rich Question Editor:
├── TipTap/ProseMirror editor (rich text)
├── LaTeX math support (MathJax/KaTeX)
├── Code block với syntax highlight (banking IT training)
├── Media upload (hình ảnh, audio, video câu hỏi)
├── Question preview real-time
├── Version history & change tracking
└── Collaborative editing (nhiều người cùng soạn)
```

---

## 4. PHASE 0 — STABILIZE & LAUNCH (Hiện tại → 2 tháng)

### Mục tiêu: Production-ready cho Agribank, 500 CCU stable

### 4.1 Tuần 1-2: Critical Bug Fixes

#### Task P0-01: Refactor QuizController → 8 Controllers
```
Priority: CRITICAL | Effort: 3 days | Risk: LOW

Tách QuizController.cs (1075 lines) thành:
- QuestionsController (CRUD + Import + Export)
- AttemptController (Submit + Review + AI Analyze)
- AdaptiveController (CAT + Practice generation)
- TopicsController (CRUD + Hierarchy)
- LeaderboardController (Rankings)
- AchievementsController (Badges + XP + Check)
- LearningPathController (Paths + Progress)
- ExamSessionController (Proctored exam management)
```

#### Task P0-02: Fix Pagination (OOM Prevention)
```
Priority: CRITICAL | Effort: 1 day | Risk: LOW

GET /api/quiz/questions?page=1&pageSize=20&sortBy=difficulty
Response: { items: [...], total: 1234, page: 1, pageSize: 20 }

Tất cả List endpoints phải có pagination
```

#### Task P0-03: Fix Essay Grading (NotImplementedException)
```
Priority: CRITICAL | Effort: 2 days | Risk: MEDIUM

Bước 1: EssayQuestion.EvaluateAnswer() → return -1 (flag cho async)
Bước 2: Implement EssayGradingWorker (BackgroundService)
Bước 3: SignalR notification khi Gemini trả về kết quả
```

#### Task P0-04: Fix GeminiGradingService (Memory Leak)
```
Priority: HIGH | Effort: 0.5 day | Risk: LOW

Thay:
  new HttpClient()  // Tạo mới mỗi request → socket exhaustion!

Bằng:
  IHttpClientFactory (Singleton HttpClient với connection pooling)
  
// Program.cs
builder.Services.AddHttpClient("gemini", client => {
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

#### Task P0-05: Fix ExplainQuestion + AnalyzeAttempt (Fake AI)
```
Priority: HIGH | Effort: 1 day | Risk: LOW

Thay Task.Delay() + hardcoded text bằng gọi GeminiSolverService thật:
- ExplainQuestion → _geminiSolverService.ExplainAsync(question, selectedAnswer)
- AnalyzeAttempt → _geminiSolverService.AnalyzeSessionAsync(attempts)
```

### 4.2 Tuần 3-4: Quality & Security

#### Task P0-06: Refactor AdminQuestionsPage.tsx (107KB → Modules)
```
Priority: HIGH | Effort: 3 days | Risk: MEDIUM

Tách thành:
├── AdminQuestionsPage.tsx        (main page, ~200 lines)
├── components/
│   ├── QuestionTable.tsx          (bảng hiển thị)
│   ├── QuestionFormModal.tsx      (modal tạo/sửa)
│   ├── ImportExcelModal.tsx       (import Excel)
│   ├── ImportDocxModal.tsx        (import Docx + AI preview)
│   ├── MissingAnswerPanel.tsx     (AI Solver panel)
│   └── QuestionTypeEditor/        (editors cho từng loại câu hỏi)
│       ├── SingleChoiceEditor.tsx
│       ├── MatchingEditor.tsx
│       └── EssayEditor.tsx
```

#### Task P0-07: Unit Tests cho Business Logic
```
Priority: HIGH | Effort: 2 days | Risk: LOW

Test Coverage Target: 80% cho Domain layer
- QuestionBase.EvaluateAnswer() — 8 loại × 5 cases = 40 tests
- GeminiGradingService — mock Gemini API
- ExamTimerService — timer expiry, auto-submit
- PkiAuthController — token validation

Tool: xUnit + Moq + FluentAssertions
```

#### Task P0-08: Security Hardening
```
Priority: HIGH | Effort: 2 days | Risk: LOW

1. Thêm Authorization attributes vào các endpoints (hiện nhiều endpoint public)
2. Implement BFF pattern: Frontend không lưu token trong localStorage
3. Add OWASP security headers (CSP, HSTS, X-Frame-Options)
4. Enable HTTPS redirect trong Production
5. Scan với dotnet list package --vulnerable
```

### 4.3 Tuần 5-6: Performance & DevOps

#### Task P0-09: CI/CD Pipeline
```
Priority: HIGH | Effort: 2 days

GitHub Actions workflow:
├── on: push (main branch)
├── jobs:
│   ├── test: dotnet test + npm test
│   ├── security: dotnet audit + npm audit
│   ├── build: docker build
│   └── deploy:
│       ├── Push to Google Container Registry
│       └── Deploy to Cloud Run (blue/green)
```

#### Task P0-10: Load Testing
```
Priority: HIGH | Effort: 1 day

Tool: k6 (JavaScript-based, realistic user flows)
Scenarios:
- Scenario 1: 500 concurrent quiz-takers (primary)
- Scenario 2: 50 admin users importing questions
- Scenario 3: 1000 CCU leaderboard refresh

Acceptance Criteria:
- P99 latency < 500ms cho /api/quiz/attempt
- P99 latency < 2000ms cho AI endpoints
- Zero errors under 500 CCU
```

### 4.4 Phase 0 — Definition of Done

```
☐ Tất cả endpoints có pagination
☐ QuizController tách thành 8 controllers
☐ Essay grading pipeline hoạt động end-to-end
☐ GeminiGradingService dùng IHttpClientFactory
☐ AdminQuestionsPage refactored thành modules
☐ Unit test coverage ≥ 80% (Domain layer)
☐ CI/CD pipeline deploy thành công lên GCP
☐ Load test 500 CCU pass (P99 < 500ms)
☐ OWASP Top 10 scan sạch
☐ Health checks passing (Postgres + Redis)
```

---

## 5. PHASE 1 — MULTI-TENANT FOUNDATION (Tháng 3-6)

### Mục tiêu: Phục vụ 5-10 tổ chức, 5,000 CCU, doanh thu đầu tiên

### 5.1 Multi-Tenancy Implementation

#### Database Strategy: Row-Level Security (PostgreSQL)
```sql
-- Bước 1: Thêm TenantId vào tất cả bảng
ALTER TABLE "Questions" ADD COLUMN "TenantId" UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
ALTER TABLE "QuestionAttempts" ADD COLUMN "TenantId" UUID NOT NULL DEFAULT '...';
-- ... tất cả các bảng

-- Bước 2: Enable RLS
ALTER TABLE "Questions" ENABLE ROW LEVEL SECURITY;

-- Bước 3: Policy
CREATE POLICY tenant_isolation ON "Questions"
    USING (TenantId = current_setting('app.tenant_id')::UUID);

-- Bước 4: Set context trước mỗi query
SET LOCAL "app.tenant_id" = '{{tenantId}}';
```

#### Application Tenant Context
```csharp
// ITenantContext — inject vào mọi service
public interface ITenantContext
{
    Guid CurrentTenantId { get; }
    string TenantCode { get; }
    TenantConfig Config { get; }
}

// Middleware tự động extract TenantId từ JWT claims
public class TenantMiddleware
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var tenantClaim = context.User.FindFirst("tenant_id");
        if (tenantClaim != null)
            tenantContext.SetCurrentTenant(Guid.Parse(tenantClaim.Value));
        await _next(context);
    }
}
```

#### Tenant Onboarding Flow
```
1. Admin tạo Tenant (tên tổ chức, domain, logo)
2. Hệ thống tự sinh:
   - Keycloak Realm mới
   - PostgreSQL Schema (hoặc RLS filter)
   - Tenant Admin account
   - Default LearningPaths cho banking domain
3. Admin invite users (bulk import từ Active Directory/Excel)
4. Configure: branding, allowed question types, max users
```

### 5.2 Internationalization (i18n)

#### Frontend Setup
```typescript
// i18next configuration
import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import Backend from 'i18next-http-backend';

i18n
  .use(Backend)
  .use(initReactI18next)
  .init({
    fallbackLng: 'en',
    supportedLngs: ['vi', 'en', 'ja', 'ko', 'ar', 'th', 'id'],
    backend: {
      loadPath: '/locales/{{lng}}/{{ns}}.json',
    },
    interpolation: { escapeValue: false },
  });

// RTL support cho Arabic
document.dir = i18n.language === 'ar' ? 'rtl' : 'ltr';
```

#### Locale Files Structure
```
public/locales/
├── en/
│   ├── common.json      ← Navigation, buttons, labels
│   ├── quiz.json        ← Quiz-specific UI
│   ├── admin.json       ← Admin panel
│   └── errors.json      ← Error messages
├── vi/
│   └── ...              ← Vietnamese (hiện có)
├── ja/
│   └── ...              ← Japanese (JLPT market)
└── ar/
    └── ...              ← Arabic (Middle East banking)
```

#### Backend Localization
```csharp
// Accept-Language header → IStringLocalizer
builder.Services.AddLocalization(opt => opt.ResourcesPath = "Resources");

// Error messages từ Resource files (không hardcode)
return BadRequest(new { message = _localizer["Error.InvalidAnswer"] });
```

### 5.3 SCORM 1.2 Exporter

```csharp
// Cho phép export course thành SCORM package
public class ScormExportService
{
    public async Task<byte[]> ExportLearningPath(Guid pathId, Guid tenantId)
    {
        var path = await _db.LearningPaths
            .Include(p => p.Steps)
            .FirstAsync(p => p.Id == pathId && p.TenantId == tenantId);
        
        // 1. Sinh imsmanifest.xml
        var manifest = GenerateManifest(path);
        
        // 2. Đóng gói questions + assets vào ZIP
        using var zip = new ZipArchive(...);
        zip.AddEntry("imsmanifest.xml", manifest);
        zip.AddEntry("scorm_api.js", ScormApiAdapter);
        
        // 3. Sinh HTML players cho từng loại câu hỏi
        foreach (var step in path.Steps)
        {
            var questions = GetQuestionsForStep(step);
            zip.AddEntry($"step_{step.StepOrder}/index.html", 
                RenderQuizPlayer(questions));
        }
        
        return zip.ToByteArray();
    }
}
```

### 5.4 Content Authoring Tool

#### Rich Question Editor (TipTap)
```typescript
// Tích hợp TipTap Editor với extensions
const editor = useEditor({
  extensions: [
    StarterKit,
    Mathematics, // LaTeX support
    Image.configure({ uploadFn: uploadToGCS }),
    AudioExtension,
    VideoExtension,
    CodeBlock.configure({ lowlight }),
    // Custom: QuestionOptionBlock, HintBlock, ExplanationBlock
  ],
  onUpdate: ({ editor }) => {
    setQuestionContent(editor.getJSON());
  },
});
```

#### Media Storage (GCS)
```csharp
// Media upload cho câu hỏi hình ảnh/audio/video
[HttpPost("questions/upload-media")]
public async Task<IActionResult> UploadMedia(IFormFile file)
{
    var gcsSetting = new GcsMediaUploader(_options);
    var url = await gcsSetting.UploadAsync(file, tenantId);
    return Ok(new { url });
}
```

### 5.5 Tenant Admin Portal

```
Tenant Admin Dashboard:
├── User Management (bulk import, roles, departments)
├── Question Bank Management (per-tenant library)
├── Learning Path Builder (drag-and-drop steps)
├── Exam Scheduler (set date, time, quota)
├── Branding (logo, colors, domain)
├── Analytics (cohort, completion rate, pass rate)
├── Integration (SSO config, SCORM export, API keys)
└── Billing (usage, invoices, upgrade plan)
```

### 5.6 Phase 1 — Definition of Done

```
☐ Multi-tenancy với RLS hoạt động cho 3+ tenants
☐ i18n: tiếng Anh + tiếng Việt + 1 ngôn ngữ Châu Á
☐ SCORM 1.2 export hoạt động với Moodle test
☐ Content Authoring: tạo câu hỏi trong app
☐ Tenant Onboarding portal hoàn chỉnh
☐ 5,000 CCU load test pass
☐ SOC 2 Type I audit bắt đầu
☐ Tích hợp với 1 HR system (SAP/Workday API)
☐ Revenue: Pilot với 1-2 ngân hàng, ký HĐ
```

---

## 6. PHASE 2 — AI-NATIVE INTELLIGENCE PLATFORM (Tháng 7-12)

### Mục tiêu: 50+ institutions, 50,000 CCU, SEA + Europe market entry

### 6.1 IRT/CAT Adaptive Engine (Python Microservice)

#### Item Response Theory Implementation
```python
# FastAPI microservice — aegisquiz-irt-service
import numpy as np
from scipy.optimize import brentq
from scipy.stats import norm

class IRT3PLEngine:
    """
    3-Parameter Logistic Model:
    P(θ) = c + (1-c) / (1 + exp(-a*(θ-b)))
    
    a = discrimination parameter (độ phân biệt)
    b = difficulty parameter (độ khó, tương đương θ của người ngang tài)
    c = guessing parameter (xác suất đoán mò đúng)
    θ = person ability (năng lực người học, ~N(0,1))
    """
    
    def probability_correct(self, theta: float, a: float, b: float, c: float) -> float:
        return c + (1 - c) / (1 + np.exp(-a * (theta - b)))
    
    def information(self, theta: float, a: float, b: float, c: float) -> float:
        """Fisher Information — càng cao = câu hỏi càng hữu ích để đánh giá θ"""
        p = self.probability_correct(theta, a, b, c)
        q = 1 - p
        return (a**2 * (p - c)**2 * q) / ((1 - c)**2 * p)
    
    def estimate_ability_mle(self, responses: List[Tuple[float, float, float, int]]) -> Tuple[float, float]:
        """Maximum Likelihood Estimation của θ và Standard Error"""
        def log_likelihood(theta):
            ll = 0
            for a, b, c, response in responses:
                p = self.probability_correct(theta, a, b, c)
                ll += response * np.log(p + 1e-10) + (1-response) * np.log(1-p + 1e-10)
            return -ll  # Minimize negative log-likelihood
        
        result = minimize(log_likelihood, x0=0.0, bounds=[(-4, 4)])
        theta_hat = result.x[0]
        se = 1 / np.sqrt(sum(self.information(theta_hat, a, b, c) for a, b, c, _ in responses))
        return theta_hat, se
    
    def select_next_question(self, theta: float, available: List[Question]) -> Question:
        """CAT: Chọn câu hỏi tối đa hóa thông tin tại θ hiện tại"""
        infos = [(q, self.information(theta, q.irt_a, q.irt_b, q.irt_c)) for q in available]
        return max(infos, key=lambda x: x[1])[0]
    
    def should_terminate(self, se: float, n_items: int, config: CATConfig) -> bool:
        return (se < config.se_threshold or      # Standard Error nhỏ đủ
                n_items >= config.max_items or   # Đã hỏi đủ số câu
                n_items >= config.min_items and se < config.relaxed_threshold)

# IRT Parameter Calibration (batch job, chạy hàng tuần)
class IRTCalibrator:
    def calibrate_question(self, question_id: Guid, response_data: pd.DataFrame):
        """Marginal Maximum Likelihood Estimation để ước lượng a, b, c"""
        # Sử dụng pyirt hoặc py3irt library
        ...
```

#### Integration với .NET Backend
```csharp
// CAT endpoint gọi Python IRT service
[HttpPost("adaptive/cat")]
public async Task<IActionResult> StartCATSession([FromBody] CATRequest request)
{
    // 1. Lấy ability estimate hiện tại của user
    var theta = await _irtClient.GetUserAbilityAsync(request.UserId, request.TopicCode);
    
    // 2. IRT service chọn câu hỏi đầu tiên
    var firstQuestion = await _irtClient.SelectNextQuestionAsync(theta, request.TopicCode);
    
    // 3. Tạo CAT session
    var session = new CATSession { UserId = request.UserId, CurrentTheta = theta };
    return Ok(new { sessionId = session.Id, question = firstQuestion });
}

[HttpPost("adaptive/cat/{sessionId}/respond")]
public async Task<IActionResult> SubmitCATResponse(Guid sessionId, [FromBody] CATResponse response)
{
    // 1. Update theta với response mới
    var updated = await _irtClient.UpdateAbilityAsync(sessionId, response);
    
    // 2. Check termination
    if (updated.ShouldTerminate)
        return Ok(new { finished = true, finalAbility = updated.Theta, report = updated.Report });
    
    // 3. Chọn câu tiếp theo
    var nextQuestion = await _irtClient.SelectNextQuestionAsync(updated.Theta, sessionId);
    return Ok(new { finished = false, question = nextQuestion, ability = updated.Theta });
}
```

### 6.2 AI Question Generator (RAG Pipeline)

```
Workflow: PDF/Word → AegisQuiz Question Bank

1. Upload tài liệu (Regulatory guide, Product manual, Policy)
   ↓
2. Document Chunking (LangChain text splitter)
   ↓
3. Embedding (text-embedding-004) → Lưu vào pgvector
   ↓
4. Generation Pipeline (Gemini 1.5 Pro, 128K context window):
   Prompt: "Từ đoạn tài liệu này, sinh 5 câu hỏi trắc nghiệm cấp độ [difficulty]
           theo format JSON: [{type, content, options, correctAnswer, explanation}]"
   ↓
5. AI Quality Scoring (Gemini judge):
   - Độ chính xác nội dung
   - Độ rõ ràng của câu hỏi
   - Tính phân biệt năng lực
   ↓
6. Human Review Queue (Admin xem và approve)
   ↓
7. Publish vào Question Bank + IRT Calibration
```

```csharp
// PDF Q&A Generation endpoint
[HttpPost("questions/generate-from-document")]
[RequestSizeLimit(50_000_000)] // 50MB max
public async Task<IActionResult> GenerateFromDocument(
    IFormFile document, 
    [FromForm] GenerationConfig config)
{
    // 1. Parse document
    var text = await _documentParser.ExtractTextAsync(document);
    
    // 2. Chunk text
    var chunks = _textChunker.Split(text, chunkSize: 2000, overlap: 200);
    
    // 3. Generate questions per chunk
    var generated = new List<GeneratedQuestion>();
    foreach (var chunk in chunks)
    {
        var questions = await _geminiGenerator.GenerateQuestionsAsync(
            chunk, config.Difficulty, config.QuestionCount, config.QuestionTypes);
        generated.AddRange(questions);
    }
    
    // 4. Return for human review
    return Ok(new { generated, reviewRequired = true });
}
```

### 6.3 Advanced Proctoring System

```typescript
// ProctoringEngine.ts — Hệ thống giám thị AI
export class ProctoringEngine {
  private faceMesh: FaceMesh;
  private violations: ProctoringEvent[] = [];
  
  async initialize(videoElement: HTMLVideoElement) {
    // MediaPipe Face Mesh
    this.faceMesh = new FaceMesh({ locateFile: MEDIAPIPE_CDN });
    this.faceMesh.onResults(this.onFaceResults.bind(this));
    
    // Start camera
    const camera = new Camera(videoElement, {
      onFrame: async () => await this.faceMesh.send({ image: videoElement }),
      width: 320, height: 240
    });
    await camera.start();
  }
  
  private onFaceResults(results: Results) {
    // Rule 1: No face detected
    if (results.multiFaceLandmarks.length === 0) {
      this.flagViolation('FACE_NOT_VISIBLE', 'HIGH');
    }
    
    // Rule 2: Multiple faces (cheating with helper)
    if (results.multiFaceLandmarks.length > 1) {
      this.flagViolation('MULTIPLE_FACES', 'CRITICAL');
    }
    
    // Rule 3: Gaze deviation (looking at notes/phone)
    if (results.multiFaceLandmarks.length === 1) {
      const gaze = this.estimateGaze(results.multiFaceLandmarks[0]);
      if (Math.abs(gaze.x) > 0.3 || Math.abs(gaze.y) > 0.3) {
        this.flagViolation('GAZE_DEVIATION', 'MEDIUM');
      }
    }
  }
  
  private estimateGaze(landmarks: NormalizedLandmarkList): {x: number, y: number} {
    // Eye center từ landmarks 468-477 (iris)
    const leftIris = landmarks[468];
    const rightIris = landmarks[473];
    return {
      x: (leftIris.x + rightIris.x) / 2 - 0.5,
      y: (leftIris.y + rightIris.y) / 2 - 0.5
    };
  }
  
  private flagViolation(type: string, severity: 'LOW'|'MEDIUM'|'HIGH'|'CRITICAL') {
    const event = { type, severity, timestamp: Date.now() };
    this.violations.push(event);
    
    // Gửi real-time tới Proctor dashboard qua SignalR
    this.proctoringHub.send('ViolationDetected', event);
    
    // Auto-submit nếu CRITICAL violation tích lũy
    if (this.violations.filter(v => v.severity === 'CRITICAL').length >= 3) {
      this.triggerAutoSubmit('CRITICAL_VIOLATION_THRESHOLD');
    }
  }
}
```

### 6.4 Learning Analytics (ClickHouse + Metabase)

```sql
-- ClickHouse schema cho analytics (read-heavy, columnar)
CREATE TABLE learning_events (
    event_id UUID,
    tenant_id UUID,
    user_id UUID,
    event_type String,  -- 'ATTEMPT', 'LOGIN', 'PATH_PROGRESS', 'ACHIEVEMENT'
    question_id Nullable(UUID),
    topic_code String,
    difficulty UInt8,
    is_correct Nullable(Bool),
    time_spent_seconds UInt32,
    score Float32,
    event_at DateTime64(3),
    properties JSON
) ENGINE = MergeTree()
PARTITION BY toYYYYMM(event_at)
ORDER BY (tenant_id, user_id, event_at);

-- Cohort retention query
SELECT
    cohort_month,
    countIf(month_diff = 0) AS m0,
    countIf(month_diff = 1) AS m1,
    countIf(month_diff = 3) AS m3,
    round(countIf(month_diff = 1) / countIf(month_diff = 0) * 100, 1) AS m1_retention
FROM (
    SELECT 
        user_id,
        toStartOfMonth(min(event_at)) AS cohort_month,
        dateDiff('month', toStartOfMonth(min(event_at)), toStartOfMonth(event_at)) AS month_diff
    FROM learning_events
    WHERE event_type = 'ATTEMPT' AND tenant_id = '{{tenantId}}'
    GROUP BY user_id, toStartOfMonth(event_at)
)
GROUP BY cohort_month
ORDER BY cohort_month;
```

### 6.5 Flutter Mobile App

```
AegisQuiz Mobile (Flutter):
├── Offline-first Architecture
│   ├── Hive DB cho câu hỏi cached
│   ├── Drift (SQLite) cho progress tracking
│   └── Background sync khi online
├── Features
│   ├── Daily Practice (Push notification)
│   ├── Spaced Repetition (SRS algorithm)
│   ├── Offline Exam Mode
│   └── Certificate viewer
├── Platform Support
│   ├── iOS (banking app store guidelines)
│   └── Android
└── Security
    ├── Certificate Pinning
    ├── Biometric auth (Face ID / Fingerprint)
    └── Jailbreak/Root detection
```

### 6.6 Phase 2 — Definition of Done

```
☐ CAT/IRT engine hoạt động, A/B test vs legacy adaptive
☐ AI Question Generator: 100 câu/tài liệu trong < 2 phút
☐ Proctoring Level 4 (face detection) hoạt động
☐ ClickHouse analytics pipeline streaming
☐ Mobile app trên App Store + Google Play
☐ 50,000 CCU load test pass
☐ SOC 2 Type II certified
☐ GDPR compliance verified
☐ 50+ institutions onboarded
☐ MRR ≥ $500K
```

---

## 7. PHASE 3 — GLOBAL MARKET DOMINATION (Năm 2-3)

### Mục tiêu: 500+ institutions, 500K CCU, Global Top 5

### 7.1 Blockchain Certification (Polygon)

```solidity
// Smart Contract: AegisCertificate.sol
// Deploy trên Polygon PoS (phí gas thấp ~0.001 MATIC/cert)
pragma solidity ^0.8.20;
import "@openzeppelin/contracts/token/ERC721/ERC721.sol";

contract AegisCertificate is ERC721 {
    struct Certificate {
        address recipient;
        string recipientName;
        string courseName;
        uint256 score;        // 0-10000 (basis points)
        uint256 issuedAt;
        string tenantName;
        bytes32 contentHash;  // Hash của toàn bộ dữ liệu chứng chỉ
    }
    
    mapping(uint256 => Certificate) public certificates;
    uint256 private _tokenIdCounter;
    
    event CertificateIssued(
        uint256 indexed tokenId, 
        address indexed recipient, 
        string courseName,
        uint256 score
    );
    
    function issueCertificate(
        address recipient,
        string memory recipientName,
        string memory courseName,
        uint256 score
    ) external onlyIssuer returns (uint256) {
        uint256 tokenId = _tokenIdCounter++;
        certificates[tokenId] = Certificate({
            recipient: recipient,
            recipientName: recipientName,
            courseName: courseName,
            score: score,
            issuedAt: block.timestamp,
            tenantName: _tenantName,
            contentHash: keccak256(abi.encode(recipient, courseName, score, block.timestamp))
        });
        
        _mint(recipient, tokenId);
        emit CertificateIssued(tokenId, recipient, courseName, score);
        return tokenId;
    }
    
    // Verification function — anyone can verify
    function verifyCertificate(uint256 tokenId) external view returns (bool, Certificate memory) {
        Certificate memory cert = certificates[tokenId];
        bool isValid = ownerOf(tokenId) == cert.recipient;
        return (isValid, cert);
    }
}
```

### 7.2 Spaced Repetition System (FSRS Algorithm)

```python
# FSRS-4.5 (Free Spaced Repetition Scheduler) — better than Anki's SM-2
# Research paper: https://arxiv.org/abs/2402.11544

class FSRSScheduler:
    """
    FSRS optimizes next review date based on:
    - Difficulty (D): 1-10, intrinsic property of item
    - Stability (S): number of days until 90% retention
    - Retrievability (R): current recall probability
    """
    
    def schedule_next_review(self, 
                              card: FlashCard, 
                              rating: Rating,  # AGAIN=1, HARD=2, GOOD=3, EASY=4
                              review_time: datetime) -> ScheduleResult:
        
        if card.state == 'new':
            return self._schedule_new_card(card, rating)
        elif card.state == 'learning':
            return self._schedule_learning_card(card, rating)
        else:
            return self._schedule_review_card(card, rating, review_time)
    
    def _schedule_review_card(self, card, rating, t):
        # Update stability based on retrievability at review time
        R = self.retrievability(card.last_review, t, card.stability)
        
        if rating == Rating.GOOD:
            new_stability = card.stability * (
                self.w[8] * exp(self.w[9] * (1 - R)) + 1  # Memory strengthening
            )
        elif rating == Rating.AGAIN:
            new_stability = self.w[11] * pow(card.difficulty, -self.w[12]) * \
                            pow(card.stability + 1, self.w[13]) * \
                            exp(self.w[14] * (1 - R)) - 1
        
        next_interval = self.next_interval(new_stability, target_R=0.9)
        return ScheduleResult(next_review=t + timedelta(days=next_interval), 
                             new_stability=new_stability)
```

### 7.3 Multi-Region Global Deployment

```hcl
# Terraform: Multi-Region GCP Deploy
# Regions: asia-southeast1 (SG), europe-west1 (BE), us-central1 (IA)

module "aegisquiz_asia" {
  source = "./modules/regional-deployment"
  region = "asia-southeast1"
  min_instances = 2
  max_instances = 100
  db_tier = "db-custom-4-16384"  # 4 vCPU, 16GB RAM
}

module "aegisquiz_europe" {
  source = "./modules/regional-deployment"
  region = "europe-west1"
  min_instances = 2
  max_instances = 50
  # GDPR: data residency enforcement
  data_residency_eu = true
}

# Global Load Balancer với GeoDNS
resource "google_compute_global_forwarding_rule" "aegisquiz_global" {
  name       = "aegisquiz-global-lb"
  target     = google_compute_target_https_proxy.aegisquiz.self_link
  port_range = "443"
}

# Cloud Armor: DDoS + WAF
resource "google_compute_security_policy" "aegisquiz_armor" {
  rule {
    action   = "rate_based_ban"
    priority = 1000
    match {
      expr { expression = "request.path.matches('/api/quiz/.*')" }
    }
    rate_limit_options {
      rate_limit_threshold { count = 1000; interval_sec = 60 }
      ban_duration_sec = 300
    }
  }
}
```

### 7.4 Regulatory Compliance Matrix

| Regulation | Market | Requirement | Implementation |
|---|---|---|---|
| **GDPR** | EU | Data sovereignty, Right to erasure | PostgreSQL RLS per region, Soft delete, Data export API |
| **PDPA** | Thailand | Data protection | Consent tracking, Encryption at rest |
| **PDPB** | India | Localization | India Cloud Run region, Data mirroring |
| **MAS TRM** | Singapore | Banking technology risk | Penetration testing, Audit logs 5 years |
| **BSP Circular** | Philippines | Banking IT | Disaster recovery, BCP documentation |
| **OJK** | Indonesia | Financial services | Local hosting option, Indonesian language |
| **RBI IT Framework** | India | Banking IT | Data classification, Encryption standards |
| **DPDP Act** | India | Privacy | DPO appointment, Breach notification 72h |

---

## 8. KIẾN TRÚC KỸ THUẬT MỤC TIÊU (WORLD-CLASS)

### 8.1 Target Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    GLOBAL EDGE (Cloudflare / Cloud Armor)               │
│              DDoS Protection · WAF · GeoDNS · CDN Assets               │
└──────────────────────────────┬──────────────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────────────┐
│                         API GATEWAY (Kong)                               │
│     Rate Limiting · Auth · Routing · Request Tracing (OpenTelemetry)    │
└────┬─────────────┬───────────────┬───────────────────┬──────────────────┘
     │             │               │                   │
┌────▼───┐  ┌──────▼─────┐  ┌─────▼──────┐  ┌────────▼───────┐
│Quiz API│  │CAT/IRT Svc │  │AI Services │  │Proctoring Hub  │
│.NET 10 │  │Python+FastA│  │Gemini API  │  │SignalR+MediaP. │
│8 Ctrl  │  │IRT 3PL     │  │Grading     │  │Face Detection  │
└────┬───┘  └──────┬─────┘  │Mentor      │  └────────┬───────┘
     │             │         │Generator   │           │
     └──────┬──────┘         └─────┬──────┘           │
            │                      │                   │
┌───────────▼──────────────────────▼───────────────────▼──────────────────┐
│                           DATA LAYER                                      │
│                                                                           │
│  PostgreSQL 17 (RLS)    Redis Cluster    ClickHouse      pgvector        │
│  ├─ Questions (JSONB)   ├─ Sessions      ├─ Events       ├─ Embeddings   │
│  ├─ Attempts            ├─ Leaderboard   ├─ Analytics    └─ RAG Index    │
│  ├─ ExamSessions        └─ AI Cache      └─ Cohorts                      │
│  └─ LearningPaths                                                         │
│                                                                           │
│  GCS (Media)            Elasticsearch   InfluxDB                         │
│  ├─ Question images     └─ Full-text     └─ Time-series metrics          │
│  ├─ Audio files             search                                        │
│  └─ Certificates                                                          │
└──────────────────────────────────────────────────────────────────────────┘
            │
┌───────────▼──────────────────────────────────────────────────────────────┐
│                       INTEGRATION LAYER                                   │
│                                                                           │
│  Keycloak (OIDC/SAML)    Kafka (Events)    Polygon (Certs)              │
│  ├─ SSO with AD/LDAP     ├─ LearningEvent  ├─ ERC-721 NFT              │
│  ├─ SAML2 (Enterprise)   ├─ PaymentEvent   └─ USDC Payments            │
│  └─ PKI USB Token        └─ AuditLog                                    │
└──────────────────────────────────────────────────────────────────────────┘
```

### 8.2 Service Dependency Map

```
Frontend (React PWA)
├── calls → API Gateway (Kong)
│   ├── routes → Quiz API (.NET 10)
│   │   ├── reads/writes → PostgreSQL
│   │   ├── caches → Redis
│   │   ├── delegates AI → AI Services
│   │   └── delegates CAT → IRT Service
│   ├── routes → Proctoring Hub (SignalR)
│   │   └── stores events → PostgreSQL
│   └── routes → Notification Service
│       └── sends → FCM / SMTP
├── connects → Keycloak (OAuth2/OIDC)
└── streams → ClickHouse (via CDC from PostgreSQL)
```

### 8.3 Technology Stack Final Decision

| Layer | Current | Target | Rationale |
|---|---|---|---|
| **Backend API** | .NET 10 | .NET 10 ✅ | Giữ nguyên — excellent performance |
| **Frontend** | React 19 + Vite | React 19 + Next.js | SSR cho SEO, ISR cho performance |
| **Mobile** | ❌ | Flutter | Cross-platform, single codebase |
| **CAT Engine** | C# (naive) | Python FastAPI | ML ecosystem, scipy/numpy |
| **Primary DB** | PostgreSQL 17 | PostgreSQL 17 ✅ | JSONB + RLS + pgvector |
| **Analytics DB** | ❌ | ClickHouse | 100x faster than PostgreSQL for analytics |
| **Search** | ❌ | Elasticsearch | Full-text question search |
| **Cache** | Redis 7 | Redis Cluster | HA, sharding |
| **Message Queue** | ❌ | Cloud Pub/Sub | Event-driven, GCP native |
| **AI** | Gemini 1.5 Flash | Gemini 2.0 Pro | Upgrade khi stable |
| **Embedding** | ❌ | text-embedding-004 | RAG pipeline |
| **Vector DB** | ❌ | pgvector | PostgreSQL extension, đơn giản hóa stack |
| **Identity** | Keycloak 25 | Keycloak 26 + Firebase | Firebase cho B2C, Keycloak cho B2B |
| **API Gateway** | ❌ | Kong OSS → Apigee | Kong trước, Apigee khi scale |
| **Blockchain** | ❌ | Polygon PoS | Low gas fees cho certificates |
| **IaC** | Terraform | Terraform + Helm | K8s khi cần K8s |
| **CI/CD** | ❌ | GitHub Actions + Cloud Deploy | GCP native |
| **Monitoring** | Serilog | Serilog + OpenTelemetry + Cloud Monitoring | Full observability |
| **CDN** | ❌ | Cloud CDN + Firebase Hosting | GCP native |

---

## 9. SECURITY & COMPLIANCE FRAMEWORK

### 9.1 Security Architecture (Defense in Depth)

```
Layer 1 — Perimeter: Cloud Armor WAF, DDoS Protection, Rate Limiting
Layer 2 — Network: VPC, Private Service Connect, No public DB
Layer 3 — Identity: Keycloak OIDC, PKI tokens, MFA mandatory
Layer 4 — Application: OWASP Top 10, Input validation, CSRF
Layer 5 — Data: Encryption at rest (AES-256), TLS 1.3 in transit
Layer 6 — Audit: Immutable audit logs (Cloud Logging), 5-year retention
Layer 7 — Response: SIEM alerts, Incident Response Plan
```

### 9.2 Data Classification

| Class | Examples | Controls |
|---|---|---|
| **TOP SECRET** | PKI keys, JWT secrets, DB passwords | GCP Secret Manager, rotation 90 days |
| **CONFIDENTIAL** | User PII, exam answers, scores | AES-256 encryption, RLS, audit log |
| **INTERNAL** | Questions, learning paths, analytics | Role-based access, tenant isolation |
| **PUBLIC** | Marketing content, sample questions | CDN, no auth required |

### 9.3 Compliance Checklist

```
GDPR (EU):
☐ Data Processing Agreement template
☐ Privacy Policy (multi-language)
☐ Consent management (opt-in)
☐ Right to erasure API (/api/users/{id}/gdpr-erasure)
☐ Data portability export (/api/users/{id}/export)
☐ DPA (Data Protection Authority) registration
☐ Data residency: EU data stays in eu-west1

Security Standards:
☐ SOC 2 Type II (Trust Services Criteria)
☐ ISO 27001 certification (năm 2)
☐ Penetration testing (annual, external firm)
☐ Bug bounty program (HackerOne)
☐ OWASP ASVS Level 2 compliance

Banking Specific:
☐ PCI DSS (nếu xử lý card payments)
☐ MAS TRM Guidelines (Singapore)
☐ Basel III training content compliance
☐ AML/CFT training module certification
```

---

## 10. CHIẾN LƯỢC KINH DOANH & MONETIZATION

### 10.1 Pricing Model (SaaS B2B)

```
Tier STARTER (Dành cho trường nhỏ/startup):
├── Đến 100 learners
├── 5 admins
├── 1,000 câu hỏi
├── Basic analytics
├── Không có AI features
└── Giá: $299/tháng

Tier PROFESSIONAL (SME Banking):
├── Đến 1,000 learners  
├── Unlimited admins
├── Unlimited câu hỏi
├── CAT/Adaptive learning
├── AI Essay Grading (500 attempts/tháng)
├── Basic Proctoring (tab-switch)
├── SCORM export
└── Giá: $999/tháng

Tier ENTERPRISE (Major Banks/Universities):
├── Unlimited learners
├── Multi-tenant (multiple departments)
├── Full AI suite (unlimited grading)
├── Advanced Proctoring (face detection)
├── Custom branding + domain
├── SSO/SAML integration
├── Analytics BI dashboard
├── API access
├── SLA 99.9%
├── Dedicated support
└── Giá: $3,999/tháng (~$48K/năm)

Tier GLOBAL (Top-tier Banks/Consortiums):
├── Tất cả Enterprise features
├── Multi-region deployment
├── Custom AI model fine-tuning
├── Blockchain certificates
├── Custom compliance (GDPR, MAS, RBI)
├── Professional services
├── SLA 99.99%
└── Giá: Custom ($100K+/năm)

Add-ons:
├── AI Question Generator: $0.10/câu hỏi được tạo
├── Proctoring: $0.50/exam session
├── Blockchain Certificate: $0.05/certificate
└── Translation: $0.02/word
```

### 10.2 Go-to-Market Strategy

```
Phase 0-1 (Việt Nam First):
├── Agribank (pilot, free → commercial)
├── Vietcombank, BIDV, VPBank
├── Đại học Ngân hàng TP.HCM
└── FPT Software, Techcombank partnerships

Phase 2 (SEA Expansion):
├── Singapore: DBS, OCBC, UOB
├── Malaysia: Maybank, CIMB, Public Bank
├── Thailand: Bangkok Bank, Kasikorn
├── Indonesia: BCA, Mandiri, BRI
└── Philippines: BDO, BPI, Metrobank

Phase 3 (Global):
├── Japan: MUFG, Mizuho, SMBC (JLPT requirement)
├── Korea: KB, Shinhan, Hana
├── Middle East: FAB, Emirates NBD (Arabic RTL)
└── Europe: ING, BNP Paribas, HSBC (GDPR compliant)
```

### 10.3 Revenue Model

```
Year 1 Target:
├── 5 Enterprise clients: $3,999 × 5 × 12 = $240K
├── 15 Professional clients: $999 × 15 × 12 = $180K
├── Add-ons (AI Generator, Proctoring): $30K
└── Total ARR: ~$450K

Year 2 Target:
├── 20 Enterprise: $960K
├── 50 Professional: $600K
├── 5 Global: $750K
├── Add-ons: $150K
└── Total ARR: ~$2.5M

Year 3 Target (Series A ready):
├── 100 Enterprise: $4.8M
├── 200 Professional: $2.4M
├── 20 Global: $3M
├── Add-ons: $800K
└── Total ARR: ~$11M
```

---

## 11. KPIs & METRICS ĐO LƯỜNG THÀNH CÔNG

### 11.1 Technical KPIs

| Metric | Phase 0 Target | Phase 1 Target | Phase 2 Target | Phase 3 Target |
|---|---|---|---|---|
| API P99 Latency (quiz/attempt) | < 500ms | < 300ms | < 200ms | < 150ms |
| AI Grading Latency (P90) | < 10s | < 5s | < 3s | < 2s |
| Concurrent Users | 500 CCU | 5,000 CCU | 50,000 CCU | 500,000 CCU |
| System Uptime | 99.5% | 99.9% | 99.9% | 99.99% |
| Test Coverage (Domain) | 80% | 85% | 90% | 95% |
| Deployment Frequency | Weekly | Daily | Multiple/day | On-demand |
| Mean Time to Recovery | < 4h | < 2h | < 1h | < 15min |
| Security Vulnerabilities (Critical) | 0 | 0 | 0 | 0 |

### 11.2 Product KPIs (Learning Effectiveness)

| Metric | Description | Target |
|---|---|---|
| **Learning Velocity** | Ability gain per session (IRT θ improvement) | +0.3 σ/week |
| **Knowledge Retention** | 30-day retention rate vs. non-SRS | > 75% |
| **CAT Efficiency** | % reduction in questions vs. fixed-length | 40-60% |
| **Essay Grading Accuracy** | Correlation with human grader | r > 0.85 |
| **Exam Completion Rate** | % learners who finish started exams | > 90% |
| **Cheating Detection Rate** | % proctoring violations flagged correctly | > 95% |

### 11.3 Business KPIs

| Metric | Phase 0 | Phase 1 | Phase 2 | Phase 3 |
|---|---|---|---|---|
| **ARR** | $0 → $50K | $450K | $2.5M | $11M |
| **Active Institutions** | 1 | 5-10 | 50+ | 500+ |
| **Monthly Active Learners** | 500 | 5,000 | 100,000 | 2,000,000 |
| **NPS (Net Promoter Score)** | - | > 40 | > 50 | > 60 |
| **Churn Rate** | - | < 5%/yr | < 3%/yr | < 2%/yr |
| **CAC (Customer Acquisition)** | High (manual) | < $5,000 | < $3,000 | < $2,000 |
| **LTV:CAC Ratio** | - | > 3x | > 5x | > 8x |

---

## 12. RỦI RO & MITIGATION PLAN

| # | Rủi ro | Khả năng | Tác động | Mitigation |
|---|---|---|---|---|
| R01 | Gemini API cost escalation | MEDIUM | HIGH | Cache kết quả AI, dùng Flash vs Pro thông minh, set budget alert |
| R02 | Data breach tại tenant | LOW | CRITICAL | SOC 2, Pen test, RLS, encryption, cyber insurance |
| R03 | Đối thủ lớn copy PKI auth | LOW | HIGH | Patent registration, deep banking partnerships |
| R04 | Keycloak version breaking change | MEDIUM | MEDIUM | Pin version, test upgrade pipeline, have auth team |
| R05 | Postgres JSONB performance degradation | LOW | HIGH | Index optimization, partition by TenantId, read replicas |
| R06 | Mobile app rejected (banking policies) | MEDIUM | MEDIUM | Review Apple/Google banking policies trước khi build |
| R07 | GDPR fine | LOW | CRITICAL | DPO hire, privacy-by-design, regular audit |
| R08 | Key developer departure | MEDIUM | HIGH | Pair programming, documentation, bus factor > 2 |
| R09 | Gemini API unavailability | LOW | HIGH | Fallback to GPT-4o, circuit breaker |
| R10 | IRT calibration data insufficient | HIGH | MEDIUM | Bootstrap với Rasch model (1PL), upgrade to 3PL khi đủ data |

---

## APPENDIX A: IMPLEMENTATION ORDER (Sprint-by-Sprint)

```
Sprint 1 (Tuần 1-2):
├── P0-04: Pagination cho tất cả List endpoints
├── P0-01: Begin QuizController split (3/8 controllers)
└── P0-08: Security headers + Authorization attributes

Sprint 2 (Tuần 3-4):
├── P0-01: Complete QuizController split
├── P0-03: Essay grading async pipeline
└── P0-04: Fix GeminiGradingService (IHttpClientFactory)

Sprint 3 (Tuần 5-6):
├── P0-06: AdminQuestionsPage refactor
├── P0-05: Fix ExplainQuestion + AnalyzeAttempt (real Gemini)
└── P0-07: Unit tests 80% coverage

Sprint 4 (Tuần 7-8):
├── P0-09: CI/CD GitHub Actions pipeline
├── P0-10: Load test k6
└── Security audit OWASP scan

Sprint 5-8 (Tháng 3-4):
├── Multi-tenancy: TenantId migration
├── i18n frontend + backend
└── Tenant onboarding portal

Sprint 9-12 (Tháng 5-6):
├── SCORM 1.2 exporter
├── Content Authoring (TipTap)
└── Analytics MVP (Metabase)

Sprint 13-20 (Tháng 7-10):
├── CAT/IRT Python service
├── AI Question Generator
└── Advanced Proctoring (MediaPipe)

Sprint 21-26 (Tháng 11-12):
├── Mobile app (Flutter)
├── ClickHouse analytics
└── Multi-region deployment prep
```

---

## APPENDIX B: TEAM STRUCTURE ĐỀ XUẤT

```
Engineering (Tối thiểu để scale):
├── Backend Lead (.NET 10) — Architect
├── Backend Sr. Dev × 2
├── Frontend Lead (React/Next.js)
├── Frontend Sr. Dev × 1
├── AI/ML Engineer (Python, Gemini, IRT)
├── DevOps/Platform Engineer (GCP, Terraform, K8s)
├── Mobile Developer (Flutter)
└── QA Engineer (Automation, k6, Selenium)

Product & Domain:
├── Product Manager (Banking EdTech domain)
└── Banking Domain Expert (Trainer/SME)

Business:
├── Sales (Enterprise B2B)
└── Customer Success
```

---

*Tài liệu này là CĂN CỨ PHÁP LÝ cho toàn bộ quá trình nâng cấp AegisQuiz.*  
*Mọi quyết định kiến trúc phải tham chiếu tài liệu này.*  
*Review và cập nhật mỗi tháng bởi Tech Lead.*

---
**AEGIS PRIME · God-Tier Architect · AegisQuiz World-Class Blueprint v1.0**  
**Classification: INTERNAL — STRATEGIC**
