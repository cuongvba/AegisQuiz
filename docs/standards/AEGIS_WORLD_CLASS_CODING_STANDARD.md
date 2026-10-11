# AEGISQUIZ — WORLD-CLASS CODING STANDARD v1.0
Nguyen Tac Lap Trinh Chuan Tac Hang Dau The Gioi

**Soan thao boi:** No.1 — IT Architect, AI Specialist & Nha Kien Truc Phan Mem
**Phien ban:** 1.0 · **Ngay:** 2026-09-17
**Pham vi ap dung:** Toan bo codebase AegisQuiz (Backend C#/.NET 8 + Frontend React/TypeScript)

---

## MUC LUC

1. Vai Tro & Cam Ket Kien Truc Su
2. Nguyen Ly Nen Tang: CLEAR Architecture
3. Nguyen Tac Dynamic — Khong Bao Gio Hard-code
4. Nguyen Tac Config-Driven Extension
5. Da Ngon Ngu (i18n) — Tieu Chuan Quoc Te
6. Dynamic UI Loading — Giao Dien Nap Dong
7. Mau Thiet Ke Thong Minh (Smart Patterns)
8. Quy Tac Dat Ten & Cau Truc File
9. Kiem Soat Chat Luong & Review Checklist
10. Anti-Patterns — Dieu Tuyet Doi Khong Lam

---

## 1. VAI TRO & CAM KET KIEN TRUC SU

### Dinh Nghia Vai Tro "No.1"

No.1 la:
> **Lead System Analyst, Master Living Documentarian, IT Architect, AI, Design, Education & Economic Specialist**  
> *(Tong Cong Trinh Su & Chu Nhiem Kien Truc He Sinh Thai AegisQuiz)*

Vai tro nay khong don thuan la "viet code" — ma la thiet ke he sinh thai phan mem song dong,
co kha nang tu mo rong, tu thich nghi va ton tai ben vung qua nhieu the he ky thuat.

Cam ket cot loi:
- Correctness : Code phai dung logic nghiep vu 100%, khong nhap nhang
- Clarity     : Bat ky ky su moi nao cung doc hieu duoc trong < 5 phut
- Scalability : Them tinh nang moi khong can sua code cu (OCP — Open/Closed Principle)
- Resilience  : He thong tu phuc hoi, log ro rang, khong crash tham lang
- Elegance    : Code dep la code don gian nhat giai quyet dung bai toan

### 5 Tru Cot Thich Nghi Vo Han (Infinite Adaptive Pillars):
1. **Dang cau hoi moi**: Mo rong qua TPH + JSONB Payload + Dynamic Evaluator Strategy (Khong sua doi Core Entity).
2. **The loai de thi moi**: Mo rong qua Ngu phap Nhan thuc CIG luu trong DB (0ms Hot-Reload, khong sua C# Parser).
3. **Da ngon ngu toan cau**: i18n 3 tang (Type-Safe UI + Tu khoa UCTE tu DB + RTL automatic).
4. **Loai hinh to chuc**: Pho thich ung 3 muc (Express -> Pro -> Enterprise) + Materialized Path O(1) bat bien.
5. **Tuy bien ca nhan/doanh nghiep**: 100% Database-Driven White-Labeling (CSS Variables & Feature Flags tu DB, 0ms redeploy).

---

## 2. NGUYEN LY NEN TANG: CLEAR ARCHITECTURE

### 2.1 So Do Kien Truc Tong The

```
+--------------------------------------------------------------+
|                    PRESENTATION LAYER                        |
|  React/TypeScript . Dynamic UI . i18n . Lazy Loading        |
+--------------------------------------------------------------+
|                    APPLICATION LAYER                         |
|  Use Cases . DTOs . Interfaces . Orchestration Logic        |
+--------------------------------------------------------------+
|                     DOMAIN LAYER          (CORE)            |
|  Entities . Value Objects . Domain Events . Business Rules  |
+--------------------------------------------------------------+
|                  INFRASTRUCTURE LAYER                        |
|  DB (EF Core) . AI Services . File Parsers . External APIs  |
+--------------------------------------------------------------+
|                   CROSS-CUTTING CONCERNS                     |
|  Logging . Auth/RBAC . i18n . Config . Caching . Metrics   |
+--------------------------------------------------------------+
```

### 2.2 Quy Tac Phu Thuoc (Dependency Rule)

```
Presentation  ->  Application  ->  Domain  <- Infrastructure
Tat ca phu thuoc huong vao DOMAIN
Domain khong biet gi ve ben ngoai
```

Quy tac vang:
- Domain Layer = Zero dependencies. Chi plain C# / TypeScript.
- Application Layer = Chi tham chieu Interfaces, khong import Infrastructure.
- Infrastructure implement Application.Interfaces (DIP — Dependency Inversion).

### 2.3 Backend: Phan Tang C# Chuan Tac

```csharp
// DUNG — Application layer chi thay Interface
public class ImportQuestionsUseCase
{
    private readonly IDocxParserService _parser;   // Interface
    private readonly IQuestionRepository _repo;    // Interface

    public ImportQuestionsUseCase(IDocxParserService parser, IQuestionRepository repo)
    {
        _parser = parser;
        _repo = repo;
    }
}

// SAI — Application layer biet ve Infrastructure (vi pham DIP)
public class ImportQuestionsUseCase
{
    private readonly DocxParserService _parser; // Concrete class
}
```

### 2.4 Frontend: Phan Tang React Chuan Tac

```
src/
+-- pages/          # Route components (thin, only orchestrate)
+-- features/       # Feature modules (self-contained)
|   +-- questions/
|       +-- api.ts       # API calls chi cho feature nay
|       +-- hooks.ts     # Business logic hooks
|       +-- components/  # UI components
|       +-- types.ts     # Types/Interfaces
+-- shared/
|   +-- api/        # Base API client
|   +-- hooks/      # Shared hooks
|   +-- components/ # Shared UI
|   +-- lib/        # Pure utilities (no React)
+-- services/       # Cross-cutting services (auth, config)
```

---

## 3. NGUYEN TAC DYNAMIC — KHONG BAO GIO HARD-CODE

### 3.1 Kim Chi Nam Tuyet Doi

"Bat cu thu gi co the thay doi theo khach hang, theo thoi gian, theo nganh nghe —
PHAI den tu DB hoac Config, khong bao gio cung trong code."

### 3.2 Phan Loai: Cai Gi Nen Dong?

| Loai du lieu       | Nguon      | Vi du AegisQuiz                          |
|-------------------|------------|------------------------------------------|
| Business Constants | DB Table   | Danh sach domain, loai cau hoi, kho      |
| Feature Flags      | DB/Config  | Bat/tat AI Solver, bat Timer cau hoi     |
| UI Labels/Messages | i18n JSON  | Moi text hien thi cho nguoi dung         |
| Workflow Logic     | DB Rules   | Quy tac import DOCX, parsing rules       |
| Permissions        | DB RBAC    | Quyen theo vai tro, theo org unit        |
| External Endpoints | Config/Env | API keys, URLs, timeouts                 |

### 3.3 Backend: Pattern Config-Driven Service

```csharp
// ANTI-PATTERN — Cung trong code
public class QuestionTypeValidator
{
    private readonly string[] VALID_TYPES = { "SINGLE", "MULTIPLE", "ORDERING" }; // Hard-coded!
}

// WORLD-CLASS — Load tu DB/Config
public class QuestionTypeValidator
{
    private readonly IDynamicConfigService _config;
    public QuestionTypeValidator(IDynamicConfigService config) => _config = config;

    public async Task<bool> IsValidAsync(string type)
    {
        // Them loai moi: chi insert DB, khong can deploy code
        var validTypes = await _config.GetListAsync("question.valid_types");
        return validTypes.Contains(type);
    }
}
```

### 3.4 Frontend: Pattern Dynamic Constants tu API

```typescript
// ANTI-PATTERN — Hard-code danh sach domain
const DOMAIN_OPTIONS = [
  { code: 'BANKING', label: 'Ngan Hang' },
  { code: 'EDUCATION', label: 'Giao Duc' },
  // Moi tenant khac nhau phai deploy lai!
];

// WORLD-CLASS — Load tu API (tenant-aware)
const { domains } = useAdaptiveTenant();
// - Moi tenant thay domain list rieng cua ho
// - Them domain moi: chi can insert DB, khong deploy code
```

---

## 4. NGUYEN TAC CONFIG-DRIVEN EXTENSION

### 4.1 Open/Closed Principle O Cap He Thong

"Mo rong he thong bang cach them du lieu vao DB/Config —
khong phai bang cach sua source code."

### 4.2 Backend: Plugin Registry Pattern

```csharp
// Interface chuan cho moi plugin
public interface IArenaGamePlugin
{
    string PluginCode { get; }     // Tu DB: "OLYMPIA", "GOLDEN_BELL"
    string DisplayName { get; }
    Task<GameResult> ExecuteAsync(GameContext ctx);
}

// DI Registration — them plugin moi = them 1 dong nay, khong sua code cu
builder.Services.AddSingleton<IArenaGamePlugin, OlympiaPlugin>();
builder.Services.AddSingleton<IArenaGamePlugin, GoldenBellPlugin>();
// Them plugin moi: chi them dong moi + class moi
```

### 4.3 Frontend: Dynamic Route/Feature Loading

```typescript
const FEATURE_MAP: Record<string, React.LazyExoticComponent<any>> = {
  'arena':    React.lazy(() => import('./features/arena/ArenaPage')),
  'adaptive': React.lazy(() => import('./features/adaptive/AdaptivePage')),
  'notebook': React.lazy(() => import('./features/notebook/NotebookPage')),
};

function DynamicRouter() {
  const { enabledFeatures } = useAdaptiveTenant(); // Tu DB
  return (
    <Routes>
      {enabledFeatures.map(feature => {
        const Component = FEATURE_MAP[feature.code];
        return Component ? (
          <Route key={feature.code} path={feature.path} element={
            <Suspense fallback={<SkeletonPage />}><Component /></Suspense>
          } />
        ) : null;
      })}
    </Routes>
  );
}
```

### 4.4 DB Schema Cho Config-Driven System

```sql
CREATE TABLE system_configs (
    id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id    UUID REFERENCES tenants(id),  -- NULL = global
    config_key   VARCHAR(200) NOT NULL,
    config_value JSONB NOT NULL,
    is_active    BOOLEAN DEFAULT TRUE,
    updated_at   TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, config_key)
);

INSERT INTO system_configs (tenant_id, config_key, config_value) VALUES
  (NULL,  'question.types',       '["SINGLE","MULTIPLE","ORDERING","MATCHING"]'),
  (NULL,  'practice.enableTimer', '{"perQuestion": false, "totalExam": true}'),
  (uuid1, 'ui.primaryColor',      '"#1a56db"'),  -- Tenant BIDV
  (uuid2, 'ui.primaryColor',      '"#d61f1f"');  -- Tenant VCB
```

---

## 5. DA NGON NGU (i18n) — TIEU CHUAN QUOC TE

### 5.1 Kien Truc i18n Chuan Tac

```
Frontend/src/shared/lib/i18n/
+-- index.ts          # i18n engine (react-i18next)
+-- locales/
|   +-- vi/           # Tieng Viet (ngon ngu goc)
|   |   +-- quiz.json
|   |   +-- admin.json
|   |   +-- errors.json
|   +-- en/           # Tieng Anh
+-- types.ts          # Type-safe translation keys
```

### 5.2 Quy Tac Tuyet Doi Ve Text Trong Code

```typescript
// TUYET DOI KHONG — Hardcode text trong JSX
<button>Luu cau hoi</button>

// CHUAN TAC WORLD-CLASS — Dung translation key
const { t } = useTranslation('quiz');
<button>{t('action.saveQuestion')}</button>
```

### 5.3 Type-Safe Translation Keys

```typescript
type QuizTranslationKey =
  | 'action.saveQuestion'
  | 'action.importDocx'
  | 'status.processingPdf'
  | 'error.noQuestionsFound';

function useQuizTranslation() {
  const { t } = useTranslation('quiz');
  return (key: QuizTranslationKey, vars?: Record<string, string>) => t(key, vars);
}
// TypeScript bao loi compile neu dung key sai
```

### 5.4 Backend: Localized Error Messages

```csharp
// ANTI-PATTERN
throw new InvalidOperationException("Khong the giai ma tep PDF");

// WORLD-CLASS — Dung IStringLocalizer
throw new DomainException(
    _localizer["Error.PdfParseFailed", ex.Message],
    errorCode: "PDF_PARSE_ERROR");
```

---

## 6. DYNAMIC UI LOADING — GIAO DIEN NAP DONG

### 6.1 Ba Tang Loading Dong

```
Tang 1: Route-level (Code Splitting)
  -> React.lazy() + Suspense
  -> Moi page la 1 chunk JS rieng, chi load khi can

Tang 2: Feature-level (Feature Flags tu DB)
  -> useAdaptiveTenant() -> enabledFeatures
  -> Feature chi load neu tenant bat trong DB

Tang 3: Component-level (Data-driven)
  -> Component tu render dua tren data tu API
```

### 6.2 Skeleton Loading — 4 State Ro Rang

```typescript
function QuestionList() {
  const { data, isLoading, isError } = useQuestions();

  if (isLoading)    return <QuestionListSkeleton />;  // 1. Loading
  if (isError)      return <ErrorBoundary />;          // 2. Error
  if (!data.length) return <EmptyState />;             // 3. Empty
  return <QuestionListContent items={data} />;         // 4. Success
}

// Skeleton — khong dung spinner vi gay lo lang
function QuestionListSkeleton() {
  return (
    <div className="space-y-3">
      {Array.from({ length: 5 }).map((_, i) => (
        <div key={i} className="h-16 bg-slate-800/50 rounded-xl animate-pulse" />
      ))}
    </div>
  );
}
```

### 6.3 Optimistic UI Pattern

```typescript
const { mutate: saveQuestion } = useMutation({
  mutationFn: (q: Question) => api.post('/questions', q),
  onMutate: async (newQuestion) => {
    await queryClient.cancelQueries(['questions']);
    const prev = queryClient.getQueryData(['questions']);
    queryClient.setQueryData(['questions'],
      (old: Question[]) => [...old, { ...newQuestion, id: 'temp-' + Date.now() }]);
    return { prev };
  },
  onError: (err, _, context) => {
    queryClient.setQueryData(['questions'], context?.prev);
    toast.error('Luu that bai. Da khoi phuc.');
  },
  onSettled: () => queryClient.invalidateQueries(['questions']),
});
```

### 6.4 Kỷ Luật Zero-Monolith Component (Ngưỡng $\le$ 250 LOC)

Tuyệt đối cấm viết các component "quái vật" (Monolithic God-Components) chứa hàng ngàn dòng code. Mọi UI Component phải tuân thủ nghiêm ngặt:
* **Ngưỡng cứng**: Tối đa **250 dòng code / file**. File vượt quá 300 dòng là vi phạm luật kiến trúc và bắt buộc phải refactor.
* **Mô hình phân rã 3 tầng**:
  1. **Smart Container**: Quản lý state, mutation, query data qua Custom Hooks chuyên biệt (`useDocxImport`, `useQuestionFilter`).
  2. **Presentational Dumb Views**: Nhận props thuần túy, render UI không side-effects (`DocxQuestionList`, `DocxInlineEditor`).
  3. **Sub-Modals / Sub-Drawers**: Tách thành các file độc lập trong thư mục `components/` của tính năng.

### 6.5 Bắt Buộc Code Splitting & Bundle Size Optimization ($\le$ 150KB gzip chunk)

* **Route-based Lazy Loading**: 100% các trang trong `pages/` phải được import qua `React.lazy()` và bọc bởi `Suspense`.
* **Heavy Library Isolation**: Các thư viện nặng (KaTeX, Three.js Arena 3D, Chart.js, XLSX, Heavy Parser Engines) KHÔNG ĐƯỢC đóng gói vào Main Vendor Bundle. Bắt buộc tách thành dynamic chunks tải theo nhu cầu.
* **Kích thước chunk tải ban đầu**: Main Initial Bundle Chunk tối đa **$\le$ 150KB gzip** để đạt FCP < 0.8s trên mọi mạng di động 4G/5G.

### 6.6 Dynamic Question Micro-Widget Registry (Triệt Tiêu Switch-Case UI)

Theo nguyên lý Open/Closed (OCP), tuyệt đối không dùng khối `switch(question.type)` hoặc `if-else` khổng lồ để render dạng câu hỏi trong [QuestionCard.tsx](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Frontend/src/components/quiz/QuestionCard.tsx). Toàn bộ 9 dạng câu hỏi phải được cấu hình qua Dynamic Registry:

```typescript
// widgets/quiz/QuestionWidgetRegistry.ts
export const QuestionWidgetRegistry: Record<string, React.LazyExoticComponent<React.ComponentType<any>>> = {
  SINGLE: React.lazy(() => import('./widgets/SingleChoiceWidget')),
  MULTI_TRUE_FALSE: React.lazy(() => import('./widgets/MultiTrueFalseWidget')),
  SHORT_ANSWER: React.lazy(() => import('./widgets/ShortAnswerWidget')),
  MATCHING: React.lazy(() => import('./widgets/MatchingWidget')),
  ORDERING: React.lazy(() => import('./widgets/OrderingWidget')),
  FILL_BLANK: React.lazy(() => import('./widgets/FillBlankWidget')),
  ESSAY: React.lazy(() => import('./widgets/EssayWidget')),
};
```

---

## 7. MAU THIET KE THONG MINH (SMART PATTERNS)

### 7.1 Backend: Result<T> Pattern

```csharp
public record Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static Result<T> Ok(T value)
        => new() { IsSuccess = true, Value = value };
    public static Result<T> Fail(string code, string message)
        => new() { IsSuccess = false, ErrorCode = code, ErrorMessage = message };
}

// Controller gon gang
var result = await _importUseCase.ExecuteAsync(stream);
if (!result.IsSuccess)
    return BadRequest(new { code = result.ErrorCode, message = result.ErrorMessage });
return Ok(result.Value);
```

### 7.2 Frontend: State Machine Cho Complex Flows

```typescript
// ANTI-PATTERN — Boolean flags gay mau thuan
const [isLoading, setIsLoading]       = useState(false);
const [isProcessing, setIsProcessing] = useState(false);
const [isDone, setIsDone]             = useState(false);
// 2^3 = 8 to hop, nhieu cai vo nghia!

// WORLD-CLASS — State Machine ro rang
type ImportState =
  | { status: 'idle' }
  | { status: 'reading';    fileName: string; fileType: 'PDF' | 'DOCX' }
  | { status: 'ai_solving'; progress: number }
  | { status: 'done';       questions: Question[] }
  | { status: 'error';      code: string; message: string };

const [importState, setImportState] = useState<ImportState>({ status: 'idle' });

function ImportOverlay() {
  switch (importState.status) {
    case 'idle':       return null;
    case 'reading':    return <ReadingOverlay file={importState.fileName} type={importState.fileType} />;
    case 'ai_solving': return <AiSolvingOverlay progress={importState.progress} />;
    case 'error':      return <ErrorToast code={importState.code} message={importState.message} />;
    default:           return null;
  }
}
```

### 7.3 Backend: Mediator Pattern

```csharp
// Moi Use Case la 1 Handler doc lap < 200 dong
public record ImportPdfQuestionsCommand(Stream PdfStream, Guid TenantId)
    : IRequest<Result<List<QuestionPreview>>>;

public class ImportPdfQuestionsHandler
    : IRequestHandler<ImportPdfQuestionsCommand, Result<List<QuestionPreview>>>
{
    private readonly IPdfExtractorService _pdfExtractor;

    public async Task<Result<List<QuestionPreview>>> Handle(
        ImportPdfQuestionsCommand cmd, CancellationToken ct)
    {
        var questions = _pdfExtractor.ParsePdfQuestions(cmd.PdfStream);
        if (!questions.Any())
            return Result<List<QuestionPreview>>.Fail("PDF_EMPTY", "Khong tim thay cau hoi");
        return Result<List<QuestionPreview>>.Ok(questions);
    }
}

// Controller chi dieu phoi, khong co business logic
[HttpPost("questions/import-pdf")]
public async Task<IActionResult> ImportPdf(IFormFile file)
{
    using var stream = file.OpenReadStream();
    var result = await _mediator.Send(new ImportPdfQuestionsCommand(stream, GetTenantId()));
    return result.IsSuccess ? Ok(result.Value) : BadRequest(result);
}
```

### 7.4 Caching Da Tang

```csharp
// Tang 1: In-Memory  (< 1ms)
// Tang 2: Redis      (< 5ms)
// Tang 3: CDN/Static (< 50ms global)

public async Task<TenantConfig> GetAsync(Guid tenantId)
{
    var key = $"tenant:config:{tenantId}";
    if (_memoryCache.TryGetValue(key, out TenantConfig cached)) return cached;

    var redisData = await _redis.GetStringAsync(key);
    if (redisData != null)
    {
        var cfg = JsonSerializer.Deserialize<TenantConfig>(redisData)!;
        _memoryCache.Set(key, cfg, TimeSpan.FromMinutes(5));
        return cfg;
    }

    var dbConfig = await _db.TenantConfigs.FindAsync(tenantId);
    await _redis.SetStringAsync(key, JsonSerializer.Serialize(dbConfig),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });
    _memoryCache.Set(key, dbConfig, TimeSpan.FromMinutes(5));
    return dbConfig!;
}
```

---

## 8. QUY TAC DAT TEN & CAU TRUC FILE

### 8.1 Quy Uoc Dat Ten

```
C# (Backend):
  Class/Interface : PascalCase  -> QuestionImportService, IDocxParser
  Method          : PascalCase  -> ParsePdfQuestions(), GetTenantId()
  Variable/Param  : camelCase   -> pdfStream, tenantId, rawQuestions
  Constant        : UPPER_SNAKE -> MAX_FILE_SIZE_MB, DEFAULT_TIMEOUT_SEC
  Async method    : suffix Async -> ParsePdfAsync(), ImportDocxAsync()

TypeScript/React (Frontend):
  Component  : PascalCase   -> DocxPreviewModal, QuestionCard
  Hook       : use prefix   -> useAdaptiveTenant(), useImportState()
  Type/Iface : PascalCase   -> ImportState, TenantConfig
  Constant   : UPPER_SNAKE  -> MAX_QUESTIONS_PER_IMPORT
  Handler    : handle prefix -> handleFileImport, handleSubmit
  Bool state : is/has/can   -> isLoading, hasError, canSubmit
```

### 8.2 Comment Chuan Tac — Giai Thich TAI SAO, Khong Giai Thich CAI GI

```csharp
// DUNG — Comment giai thich TAI SAO
// PDF scan khong co text layer -> phai detect va switch sang image mode
// Threshold 50 chars/page la kinh nghiem thuc te tu 600+ file GPLX
if (totalTextLength < SCAN_PDF_TEXT_THRESHOLD)
    return ExtractScannedPdfQuestions(document, pageImagesMap);

// SAI — Comment vo nghia (noi lai code)
// Kiem tra neu stream null thi return empty list
if (pdfStream == null) return new List<DocxImportPreviewDto>();
```

---

## 9. KIEM SOAT CHAT LUONG & REVIEW CHECKLIST

### Truoc Moi Commit — Tu Review 6 Chieu

```
ARCHITECTURE
  [ ] Khong vi pham Dependency Rule (outer -> inner only)
  [ ] Business logic trong Domain/Application, khong trong Controller

DYNAMIC & CONFIG-DRIVEN
  [ ] Khong co magic string/number cung trong code logic
  [ ] Gia tri tenant-specific deu den tu Config/DB

i18n
  [ ] Khong co hardcode text trong JSX
  [ ] Error messages dung error code + localized message

RESILIENCE
  [ ] Moi external call co try/catch co y nghia
  [ ] Log du context (tenantId, userId, file, error type)
  [ ] Khong nuot exception tham lang
  [ ] Timeout set cho moi HTTP call

PERFORMANCE
  [ ] Query DB khong co N+1 (batch load truoc)
  [ ] Expensive operation duoc cache

SECURITY
  [ ] Input validation ca frontend va backend
  [ ] File upload validate MIME type, khong chi extension
  [ ] Tenant isolation dam bao (khong lo data cheo tenant)
```

### Logging Chuan Tac

```csharp
// Format: [System] Action | Context -> Result
_logger.LogInformation(
    "[PdfParserService] ParsePdfQuestions | TenantId={TenantId} | File={FileName} -> Found {Count} in {Ms}ms",
    tenantId, fileName, questions.Count, sw.ElapsedMilliseconds);

_logger.LogError(ex,
    "[PdfParserService] FAILED | TenantId={TenantId} | File={FileName} | Error={ErrorCode}",
    tenantId, fileName, "PDF_CORRUPT");
```

---

## 10. ANTI-PATTERNS — DIEU TUYET DOI KHONG LAM

### A1: God Class / God Component
2000-dong class lam du moi thu. Tach thanh Use Cases doc lap < 200 dong moi cai.

### A2: String-based Role Check
```typescript
if (user.role === 'admin') { }  // De typo, capital A gay bug
// Dung: if (user.permissions.includes(Permission.MANAGE_QUESTIONS)) { }
```

### A3: Database Calls Trong Loop (N+1)
```csharp
// 1000 cau hoi = 1000 DB calls -> CHET SERVER
foreach (var q in questions)
    var topic = await _db.Topics.FindAsync(q.TopicCode); // TRONG LOOP!

// Batch load truoc:
var topicsMap = await _db.Topics
    .Where(t => topicCodes.Contains(t.Code)).ToDictionaryAsync(t => t.Code);
```

### A4: Shared Mutable State
```typescript
let currentUser = null; // Module-level mutable -> race condition
// Dung: const { state } = useContext(AuthContext); // Immutable
```

### A5: Bo Qua Tenant Isolation
```csharp
// LO DU LIEU CHEO TENANT!
var questions = await _db.Questions.ToListAsync();

// LUON filter tenantId
var questions = await _db.Questions
    .Where(q => q.TenantId == GetEffectiveTenantId()).ToListAsync();
```

---

## TONG KET: KIM CHI NAM 10 DIEM

```
+------------------------------------------------------------------+
|              KIM CHI NAM KIEN TRUC SU WORLD-CLASS                |
+------------------------------------------------------------------+
|  1. CLEAR   - Moi nguoi doc hieu duoc ngay                       |
|  2. SINGLE  - Moi class/function lam dung 1 viec (SRP)          |
|  3. DYNAMIC - Config/DB, khong bao gio hard-code thay doi duoc   |
|  4. OPEN    - Mo rong bang them moi, khong sua cu (OCP)          |
|  5. TYPED   - Type-safe tu dau den cuoi (generics, TS strict)    |
|  6. LOCAL   - Moi text di qua i18n, khong cung trong code        |
|  7. LAZY    - Load cai gi khi can cai do, tranh bloat            |
|  8. TENANT  - Moi query deu scoped theo tenant                   |
|  9. LOG     - Log du context, khong nuot loi tham lang           |
| 10. TEST    - Viet code de test, inject dependencies             |
+------------------------------------------------------------------+
```

*Tai lieu nay la nen tang kien truc bat bien cua AegisQuiz.*
*Moi thanh vien team can doc va cam ket tuan thu truoc khi dong gop code vao production.*

Phien ban tiep theo (v2.0): Event-Driven Architecture, CQRS day du, GraphQL Federation cho da tenant.
