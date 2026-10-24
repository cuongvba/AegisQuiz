using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AegisQuiz.API.Security;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Domain.Factories;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Infrastructure.Services;
using AegisQuiz.Infrastructure.Excel;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [World-Class Refactor] QuestionsController — Tách từ QuizController.
    /// Chỉ phụ trách: CRUD câu hỏi, Import Excel/Docx, Missing-Answer AI.
    /// Route base: api/quiz/questions
    /// [CRITICAL SECURITY FIX] Trước đây controller này KHÔNG có [Authorize] nào —
    /// bất kỳ ai (kể cả ẩn danh) đều có thể tạo/sửa/xóa/import câu hỏi. Nay yêu cầu
    /// Role-Weight ≥ TeamLeader (ngưỡng thấp nhất trong nhóm "staff" — bảo toàn tính
    /// năng TeamLeader tự quản lý câu hỏi phạm vi TEAM đang hoạt động ở Frontend
    /// admin/questions). Learner/GuestViewer/ẩn danh vẫn bị chặn hoàn toàn.
    /// [KNOWN GAP — theo dõi riêng]: Backend chưa lọc theo creatorId/OrgUnitId khi
    /// Update/Delete, chỉ Frontend (canManageResource) đang tự giới hạn phạm vi TEAM;
    /// cần bổ sung kiểm tra sở hữu ở tầng API trong một đợt hardening kế tiếp.
    /// </summary>
    [ApiController]
    [Route("api/quiz")]
    [Authorize(Policy = RoleWeightPolicies.TeamLeader)]
    public class QuestionsController : ControllerBase
    {
        private readonly AegisQuizDbContext                  _db;
        private readonly IExcelParserService                 _excelParser;
        private readonly IDocxParserService                  _docxParser;
        private readonly IPdfExtractorService                _pdfExtractor;
        private readonly IGeminiSolverService                _geminiSolver;
        private readonly IAiQuestionGeneratorService         _aiGenerator;
        private readonly IVietnameseTextCorrectionService    _textCorrectionService;
        private readonly IContentOwnershipGuard              _ownershipGuard;
        private readonly IExamDeduplicationEngine            _deduplicationEngine;
        private readonly ITenantContext                      _tenantContext;
        private readonly IDynamicDomainContextService        _domainContextService;

        public QuestionsController(
            AegisQuizDbContext                  db,
            IExcelParserService                 excelParser,
            IDocxParserService                  docxParser,
            IPdfExtractorService                pdfExtractor,
            IGeminiSolverService                geminiSolver,
            IAiQuestionGeneratorService         aiGenerator,
            IVietnameseTextCorrectionService    textCorrectionService,
            IContentOwnershipGuard              ownershipGuard,
            IExamDeduplicationEngine            deduplicationEngine,
            ITenantContext                      tenantContext,
            IDynamicDomainContextService        domainContextService)
        {
            _db                   = db;
            _excelParser          = excelParser;
            _docxParser           = docxParser;
            _pdfExtractor         = pdfExtractor;
            _geminiSolver         = geminiSolver;
            _aiGenerator          = aiGenerator;
            _textCorrectionService = textCorrectionService;
            _ownershipGuard       = ownershipGuard;
            _deduplicationEngine  = deduplicationEngine;
            _tenantContext        = tenantContext;
            _domainContextService = domainContextService;
        }

        // ── GET api/quiz/questions (Paged) ─────────────────────────────────────
        [HttpGet("questions")]
        public async Task<IActionResult> GetQuestions([FromQuery] QuestionPagedQuery query)
        {
            try
            {
                var q = _db.Questions.AsNoTracking().Include(x => x.Context).AsQueryable();

                // Filters
                if (!string.IsNullOrWhiteSpace(query.CategoryCode))
                {
                    var targetCode = query.CategoryCode.Trim();
                    var targetTag = targetCode.StartsWith("#") ? targetCode : "#" + targetCode;
                    q = q.Where(x => x.CategoryCode == targetCode 
                                  || x.Tags.Contains(targetCode) 
                                  || x.Tags.Contains(targetTag));
                }

                if (!string.IsNullOrWhiteSpace(query.DomainCode) && query.DomainCode != "ALL")
                    q = q.Where(x => x.DomainCode == query.DomainCode);

                if (!string.IsNullOrWhiteSpace(query.QuestionType))
                {
                    var type = query.QuestionType.ToUpper();
                    q = q.Where(x => EF.Property<string>(x, "QuestionType") == type);
                }

                if (query.Difficulty.HasValue)
                    q = q.Where(x => x.Difficulty == query.Difficulty.Value);

                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var searchTrim = query.Search.Trim();
                    if (searchTrim.StartsWith("#"))
                    {
                        var tagToFind = searchTrim.TrimStart('#').ToLower();
                        q = q.Where(x => x.Tags.Any(t => t.ToLower().Contains(tagToFind)) || x.Content.Contains(searchTrim));
                    }
                    else
                    {
                        q = q.Where(x => x.Content.Contains(searchTrim) || x.Tags.Any(t => t.ToLower().Contains(searchTrim.ToLower())));
                    }
                }

                if (!string.IsNullOrWhiteSpace(query.Tag))
                {
                    var tagLower = query.Tag.Trim().TrimStart('#').ToLower();
                    q = q.Where(x => x.Tags.Any(t => t.ToLower().Contains(tagLower)));
                }

                // [P2 Perf Fix] Đẩy ORDER BY xuống SQL trước khi materialize — toàn bộ các
                // trường sort (Difficulty/Content/CreatedAt/Id) đều là cột thật, Postgres có
                // thể dùng index thay vì phải load hết rồi sort lại trong RAM.
                q = (query.SortBy?.ToLower(), query.Sort) switch
                {
                    ("difficulty", SortDirection.Desc) => q.OrderByDescending(x => x.Difficulty),
                    ("difficulty", _)                  => q.OrderBy(x => x.Difficulty),
                    ("content", SortDirection.Desc)    => q.OrderByDescending(x => x.Content),
                    ("content", _)                      => q.OrderBy(x => x.Content),
                    ("createdat", SortDirection.Asc)   => q.OrderBy(x => x.CreatedAt),
                    _                                   => q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
                };

                // [P2 Perf Fix — Safety Cap] `enabled`/Year/TargetLevel/AssessmentPurpose/
                // IssuingOrg chỉ tồn tại trong Payload JSONB (không phải cột thật), nên KHÔNG
                // thể dịch trực tiếp sang SQL WHERE/Skip/Take mà không thay đổi schema (thêm
                // cột `Enabled` thật + migration + backfill — nằm ngoài phạm vi patch hiện
                // tại). Để tránh tải toàn bộ bảng vào RAM trong trường hợp xấu nhất (ngân
                // hàng câu hỏi hàng trăm nghìn bản ghi), giới hạn số dòng candidate tải vào
                // bộ nhớ ở một ngưỡng an toàn khi không yêu cầu "All" — với PageSize thông
                // thường (10-100), ngưỡng này thừa sức bao trọn nhiều trang mà vẫn chặn được
                // truy vấn không giới hạn gây OOM.
                const int InMemoryScanSafetyCap = 20_000;
                bool willReturnAll = query.All || query.PageSize <= 0;
                if (!willReturnAll)
                    q = q.Take(InMemoryScanSafetyCap);

                // [FIX P0 & High Scale Engine] Load all items matching DB filters với AsNoTracking() hiệu năng cao
                var allItems = await q.ToListAsync();
                var enabledItems = allItems.Where(IsQuestionEnabled).ToList();

                // Lọc thêm theo các trục tọa độ trong Payload nếu có
                if (query.Year.HasValue)
                    enabledItems = enabledItems.Where(x => x.BenchmarkYear == query.Year.Value).ToList();
                if (!string.IsNullOrWhiteSpace(query.TargetLevel))
                    enabledItems = enabledItems.Where(x => string.Equals(x.TargetLevel, query.TargetLevel, StringComparison.OrdinalIgnoreCase)).ToList();
                if (!string.IsNullOrWhiteSpace(query.AssessmentPurpose))
                    enabledItems = enabledItems.Where(x => string.Equals(x.AssessmentPurpose, query.AssessmentPurpose, StringComparison.OrdinalIgnoreCase)).ToList();
                if (!string.IsNullOrWhiteSpace(query.IssuingOrg))
                    enabledItems = enabledItems.Where(x => string.Equals(x.IssuingOrg, query.IssuingOrg, StringComparison.OrdinalIgnoreCase)).ToList();

                // Count AFTER enabled filter — chính xác
                var total = enabledItems.Count;

                // Sort in-memory sau filter (data đã nhỏ)
                enabledItems = (query.SortBy?.ToLower(), query.Sort) switch
                {
                    ("difficulty", SortDirection.Desc) => enabledItems.OrderByDescending(x => x.Difficulty).ToList(),
                    ("difficulty", _)                  => enabledItems.OrderBy(x => x.Difficulty).ToList(),
                    ("content", SortDirection.Desc)    => enabledItems.OrderByDescending(x => x.Content).ToList(),
                    ("content", _)                     => enabledItems.OrderBy(x => x.Content).ToList(),
                    ("createdat", SortDirection.Asc)   => enabledItems.OrderBy(x => x.CreatedAt).ToList(),
                    _                                  => enabledItems.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToList()
                };

                // [Unlimited & Adaptive Pagination] Nếu query.All = true hoặc PageSize <= 0, trả về toàn bộ danh sách không giới hạn
                bool isUnlimited = query.All || query.PageSize <= 0;
                var paged = isUnlimited
                    ? enabledItems
                    : enabledItems
                        .Skip((query.Page - 1) * query.PageSize)
                        .Take(query.PageSize)
                        .ToList();

                return Ok(new PagedResult<QuestionBase>
                {
                    Items    = paged,
                    Total    = total,
                    Page     = isUnlimited ? 1 : query.Page,
                    PageSize = isUnlimited ? total : query.PageSize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lấy câu hỏi: {ex.Message}" });
            }
        }

        // ── GET api/quiz/questions/{id} [NEW P2] ──────────────────────────────
        [HttpGet("questions/{id:guid}")]
        public async Task<IActionResult> GetQuestion(Guid id)
        {
            try
            {
                var question = await _db.Questions.FindAsync(id);
                if (question == null)
                    return NotFound(new { message = "Không tìm thấy câu hỏi" });
                return Ok(question);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lấy câu hỏi: {ex.Message}" });
            }
        }

        // ── POST api/quiz/questions ────────────────────────────────────────────
        [HttpPost("questions")]
        public async Task<IActionResult> CreateQuestion([FromBody] QuestionUpsertDto dto)
        {
            try
            {
                var question = BuildQuestion(dto);
                _db.Questions.Add(question);
                await _db.SaveChangesAsync();
                return Created($"api/quiz/questions/{question.Id}", question);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi tạo câu hỏi: {ex.Message}" });
            }
        }

        // ── PUT api/quiz/questions/{id} ────────────────────────────────────────
        [HttpPut("questions/{id:guid}")]
        public async Task<IActionResult> UpdateQuestion(Guid id, [FromBody] QuestionUpsertDto dto)
        {
            try
            {
                var question = await _db.Questions.FindAsync(id);
                if (question == null)
                    return NotFound(new { message = "Không tìm thấy câu hỏi" });

                if (!await _ownershipGuard.CanManageAsync(User, question.CreatorId, question.OrgUnitId))
                    return Forbid();

                var normType = QuestionFactory.NormalizeType(dto.QuestionType);
                if (question.QuestionType != normType)
                {
                    // Thay đổi Discriminator bằng SQL thuần để tránh lỗi EF Core TPH discriminator immutability
                    await _db.Database.ExecuteSqlRawAsync(
                        "UPDATE \"Questions\" SET \"QuestionType\" = {0} WHERE \"Id\" = {1}",
                        normType, id);
                    _db.Entry(question).State = EntityState.Detached;
                    question = await _db.Questions.FindAsync(id);
                    if (question == null)
                        return StatusCode(500, new { message = "Lỗi nạp lại câu hỏi sau khi đổi loại." });
                }

                ApplyDto(question, dto);
                await _db.SaveChangesAsync();
                return Ok(question);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi cập nhật câu hỏi: {ex.Message}" });
            }
        }

        // ── DELETE api/quiz/questions/{id} ─────────────────────────────────────
        [HttpDelete("questions/{id:guid}")]
        public async Task<IActionResult> DeleteQuestion(Guid id)
        {
            try
            {
                var question = await _db.Questions.FindAsync(id);
                if (question == null)
                    return NotFound(new { message = "Không tìm thấy câu hỏi" });

                if (!await _ownershipGuard.CanManageAsync(User, question.CreatorId, question.OrgUnitId))
                    return Forbid();

                _db.Questions.Remove(question);
                await _db.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi xóa câu hỏi: {ex.Message}" });
            }
        }

        // ── POST api/quiz/questions/bulk-delete ───────────────────────────────
        [HttpPost("questions/bulk-delete")]
        public async Task<IActionResult> BulkDeleteQuestions([FromBody] BulkDeleteQuestionsRequest request)
        {
            try
            {
                if (request?.QuestionIds == null || request.QuestionIds.Count == 0)
                    return BadRequest(new { message = "Danh sách mã câu hỏi cần xóa không được để trống." });

                var tenantId = _tenantContext.CurrentTenantId;
                var isSysAdmin = _tenantContext.IsSystemAdmin;

                var defaultTenantId = new Guid("00000000-0000-0000-0000-000000000001");
                var questions = await _db.Questions
                    .IgnoreQueryFilters()
                    .Where(q => (isSysAdmin || q.TenantId == Guid.Empty || q.TenantId == defaultTenantId || q.TenantId == tenantId) && request.QuestionIds.Contains(q.Id))
                    .ToListAsync();

                if (questions.Count == 0)
                    return Ok(new { success = true, deletedCount = 0, message = "Không tìm thấy câu hỏi nào phù hợp để xóa." });

                // Kiểm tra quyền OwnershipGuard
                var allowedToDelete = new List<QuestionBase>();
                foreach (var q in questions)
                {
                    if (await _ownershipGuard.CanManageAsync(User, q.CreatorId, q.OrgUnitId))
                    {
                        allowedToDelete.Add(q);
                    }
                }

                if (allowedToDelete.Count == 0)
                    return Forbid();

                _db.Questions.RemoveRange(allowedToDelete);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    deletedCount = allowedToDelete.Count,
                    message = $"Đã xóa thành công {allowedToDelete.Count} câu hỏi khỏi hệ thống."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi xóa hàng loạt câu hỏi: {ex.Message}" });
            }
        }

        // ── POST api/quiz/questions/cleanup-orphans ───────────────────────────
        [HttpPost("questions/cleanup-orphans")]
        [HttpDelete("questions/orphans")]
        public async Task<IActionResult> CleanupOrphanQuestions()
        {
            try
            {
                var tenantId = _tenantContext.CurrentTenantId;
                var isSysAdmin = _tenantContext.IsSystemAdmin;
                var defaultTenantId = new Guid("00000000-0000-0000-0000-000000000001");

                // Lấy tất cả topic codes hợp lệ
                var validTopics = await _db.BankTopics
                    .IgnoreQueryFilters()
                    .Where(t => isSysAdmin || t.TenantId == Guid.Empty || t.TenantId == defaultTenantId || t.TenantId == tenantId)
                    .Select(t => t.Code)
                    .ToListAsync();

                var validCodes = new HashSet<string>(validTopics.Where(c => !string.IsNullOrWhiteSpace(c)), StringComparer.OrdinalIgnoreCase);

                // Lấy tất cả câu hỏi trong tenant
                var allQuestions = await _db.Questions
                    .IgnoreQueryFilters()
                    .Where(q => isSysAdmin || q.TenantId == Guid.Empty || q.TenantId == defaultTenantId || q.TenantId == tenantId)
                    .ToListAsync();

                // Lọc câu hỏi mồ côi: CategoryCode không khớp bất kỳ Code nào và không phải GENERAL (trừ khi GENERAL cũng không tồn tại)
                var orphanQuestions = allQuestions.Where(q =>
                {
                    if (string.IsNullOrWhiteSpace(q.CategoryCode)) return true;
                    // Chuẩn hóa dấu gạch nối và gạch dưới để kiểm tra tương đồng
                    var normCat = q.CategoryCode.Replace("-", "").Replace("_", "").ToUpperInvariant();
                    bool hasMatch = validCodes.Any(c => c.Replace("-", "").Replace("_", "").ToUpperInvariant() == normCat);
                    return !hasMatch;
                }).ToList();

                if (orphanQuestions.Count == 0)
                    return Ok(new { success = true, deletedCount = 0, message = "Không phát hiện câu hỏi mồ côi nào. Cơ sở dữ liệu hoàn toàn nhất quán." });

                _db.Questions.RemoveRange(orphanQuestions);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    deletedCount = orphanQuestions.Count,
                    message = $"Đã dọn dẹp thành công {orphanQuestions.Count} câu hỏi mồ côi không thuộc chủ đề nào."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi dọn dẹp câu hỏi mồ côi: {ex.Message}" });
            }
        }

        public class BulkDeleteQuestionsRequest
        {
            public List<Guid> QuestionIds { get; set; } = new();
        }

        // ── POST api/quiz/questions/import (Excel — preview, DOT-2026 Smart Format) ──
        [HttpPost("questions/import")]
        public async Task<IActionResult> ImportQuestionsExcel(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "Vui lòng chọn file Excel để tải lên." });

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext != ".xlsx" && ext != ".xls")
                    return BadRequest(new { message = "Chỉ hỗ trợ file .xlsx hoặc .xls." });

                using var stream = file.OpenReadStream();

                // [DOT-2026 Smart Parser] Sử dụng parser thông minh mới
                var preview = _excelParser.ParseExcelFileToPreview(stream, file.FileName);

                if (preview.TotalQuestions == 0)
                    return BadRequest(new
                    {
                        message = "Không tìm thấy câu hỏi hợp lệ trong file Excel.",
                        warnings = preview.Warnings
                    });

                // [Exam Deduplication & Ontology Detection]
                var allSheetQuestions = preview.Sheets.SelectMany(sh => sh.Questions).ToList();
                var tenantId = _tenantContext.CurrentTenantId;
                var dedupReport = await _deduplicationEngine.AnalyzeAndTagDuplicatesAsync(allSheetQuestions, tenantId);
                preview.DuplicateCount = dedupReport.DuplicateCount;
                preview.UniqueCount = dedupReport.UniqueCount;
                preview.DetectedExamClusters = dedupReport.ClusterMatches;

                // Trả về dữ liệu thanh tra toàn diện cho Universal Cognitive Ingestion Studio (UCIS)
                return Ok(new
                {
                    fileName                  = preview.FileName,
                    totalSheets               = preview.TotalSheets,
                    totalQuestions            = preview.TotalQuestions,
                    duplicateCount            = preview.DuplicateCount,
                    uniqueCount               = preview.UniqueCount,
                    deduplicationRatePercentage = preview.DeduplicationRatePercentage,
                    detectedExamClusters      = preview.DetectedExamClusters,
                    warnings                  = preview.Warnings,
                    detectedDomainCode        = preview.DetectedDomainCode,
                    detectedTargetLevel       = preview.DetectedTargetLevel,
                    detectedAssessmentPurpose = preview.DetectedAssessmentPurpose,
                    detectedIssuingOrg        = preview.DetectedIssuingOrg,
                    detectedBenchmarkYear     = preview.DetectedBenchmarkYear,
                    detectedBenchmarkStandard = preview.DetectedBenchmarkStandard,
                    detectedTags              = preview.DetectedTags,
                    globalMetadataBanner      = preview.GlobalMetadataBanner,
                    sheets                    = preview.Sheets.Select(sh => new
                    {
                        sheetIndex            = sh.SheetIndex,
                        sheetName             = sh.SheetName,
                        sheetRole             = sh.SheetRole,
                        confidenceScore       = sh.ConfidenceScore,
                        isEnabledForImport    = sh.IsEnabledForImport,
                        rowCount              = sh.RowCount,
                        columnCount           = sh.ColumnCount,
                        headerRowIndex        = sh.HeaderRowIndex,
                        detectedTopicCode     = sh.DetectedTopicCode,
                        detectedTopicName     = sh.DetectedTopicName,
                        questionCount         = sh.QuestionCount,
                        questions             = sh.Questions,
                        columnMapping         = sh.ColumnMapping,
                        sampleRows            = sh.SampleRows,
                        healthStats           = sh.HealthStats,
                        detectedDomainCode    = sh.DetectedDomainCode,
                        detectedTargetLevel   = sh.DetectedTargetLevel,
                        detectedAssessmentPurpose = sh.DetectedAssessmentPurpose,
                        detectedIssuingOrg    = sh.DetectedIssuingOrg,
                        detectedBenchmarkYear = sh.DetectedBenchmarkYear,
                        detectedBenchmarkStandard = sh.DetectedBenchmarkStandard,
                        detectedTags          = sh.DetectedTags
                    }),
                    skippedSheets             = preview.SkippedSheets.Select(sk => new
                    {
                        sheetIndex            = sk.SheetIndex,
                        sheetName             = sk.SheetName,
                        reason                = sk.Reason,
                        suggestedRole         = sk.SuggestedRole,
                        rowCount              = sk.RowCount,
                        sampleRows            = sk.SampleRows
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi nhập câu hỏi từ Excel: {ex.Message}" });
            }
        }

        // ── POST api/quiz/questions/confirm-import-excel ───────────────────────
        // Endpoint lưu câu hỏi đã được người dùng xác nhận từ preview Excel
        [HttpPost("questions/confirm-import-excel")]
        public Task<IActionResult> ConfirmImportQuestionsExcel([FromBody] List<DocxImportPreviewDto> items)
            => ConfirmImportQuestionsDocx(items);  // Tái sử dụng hoàn toàn cùng logic với DOCX/PDF

        // ── POST api/quiz/questions/taxonomy/enrich (UCTE v4.0 Autonomous Self-Enrichment) ──
        [HttpPost("questions/taxonomy/enrich")]
        public IActionResult EnrichTaxonomyRule([FromBody] EnrichTaxonomyRuleRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TargetField) || string.IsNullOrWhiteSpace(request.RawHeader))
            {
                return BadRequest(new { message = "TargetField và RawHeader không được để trống." });
            }

            var enriched = UniversalColumnTaxonomyMatrix.Instance.EnrichRule(
                request.TargetField,
                request.RawHeader,
                request.DomainScope ?? "ALL",
                request.LanguageCode ?? "any");

            return Ok(new
            {
                success = true,
                enriched = enriched,
                message = enriched 
                    ? $"Đã làm giàu thành công quy tắc cho trường '{request.TargetField}' từ tiêu đề '{request.RawHeader}'." 
                    : "Quy tắc này đã tồn tại hoặc đã được nhận diện trước đó.",
                totalActiveRules = UniversalColumnTaxonomyMatrix.Instance.Rules.Count
            });
        }

        // ── GET api/quiz/questions/taxonomy/rules (Lấy danh sách quy tắc hiện hành) ──
        [HttpGet("questions/taxonomy/rules")]
        public IActionResult GetTaxonomyRules()
        {
            var rules = UniversalColumnTaxonomyMatrix.Instance.Rules.Select(r => new
            {
                r.Id,
                r.TargetField,
                r.Priority,
                r.ConfidenceScore,
                r.PositivePattern,
                r.NegativePattern,
                r.DomainScope,
                r.LanguageCode,
                r.IsSelfEnriched,
                r.HitCount
            });

            return Ok(new
            {
                totalRules = UniversalColumnTaxonomyMatrix.Instance.Rules.Count,
                rules
            });
        }


        // ── POST api/quiz/questions/import-docx ───────────────────────────────
        [HttpPost("questions/import-docx")]
        public async Task<IActionResult> ImportQuestionsDocx(IFormFile file, [FromQuery] bool useAi = false)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "Vui lòng chọn file DOCX để tải lên." });

                var tenantId = _tenantContext.CurrentTenantId;
                var dynamicContext = await _domainContextService.BuildContextAsync(tenantId);

                using var stream = file.OpenReadStream();
                var rawQuestions = _docxParser.ParseDocxFile(stream, dynamicContext);

                if (rawQuestions == null || rawQuestions.Count == 0)
                    return BadRequest(new { message = "Không tìm thấy câu hỏi hợp lệ trong file DOCX." });

                foreach (var q in rawQuestions)
                {
                    if (string.IsNullOrWhiteSpace(q.SourceFileName)) q.SourceFileName = file.FileName;
                }

                await _deduplicationEngine.AnalyzeAndTagDuplicatesAsync(rawQuestions, tenantId);

                // Chỉ kích hoạt Gemini AI khi người dùng chỉ định useAi=true
                if (useAi)
                {
                    var solvedQuestions = await _geminiSolver.AutoSolveQuestionsAsync(rawQuestions);
                    return Ok(solvedQuestions);
                }

                return Ok(rawQuestions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi nhập câu hỏi từ DOCX: {ex.Message}" });
            }
        }

        // ── POST api/quiz/questions/import-pdf [Kịch bản A] ─────────────────
        [HttpPost("questions/import-pdf")]
        public async Task<IActionResult> ImportQuestionsPdf(IFormFile file, [FromQuery] bool useAi = false)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "Vui lòng chọn file PDF để tải lên." });

                using var stream = file.OpenReadStream();
                var rawQuestions = _pdfExtractor.ParsePdfQuestions(stream);

                if (rawQuestions == null || rawQuestions.Count == 0)
                    return BadRequest(new { message = "Không tìm thấy câu hỏi định dạng sẵn trong file PDF. Nếu đây là tài liệu giáo trình/văn bản nội dung, vui lòng sử dụng tính năng 'Tự động tạo câu hỏi AI từ tài liệu' (Kịch bản B)." });

                foreach (var q in rawQuestions)
                {
                    if (string.IsNullOrWhiteSpace(q.SourceFileName)) q.SourceFileName = file.FileName;
                }

                var tenantId = _tenantContext.CurrentTenantId;
                await _deduplicationEngine.AnalyzeAndTagDuplicatesAsync(rawQuestions, tenantId);

                if (useAi)
                {
                    var solvedQuestions = await _geminiSolver.AutoSolveQuestionsAsync(rawQuestions);
                    return Ok(solvedQuestions);
                }

                return Ok(rawQuestions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi phân tích câu hỏi từ PDF: {ex.Message}" });
            }
        }

        // ── POST api/quiz/questions/generate-from-doc [Kịch bản B] ───────────
        [HttpPost("questions/generate-from-doc")]
        public async Task<IActionResult> GenerateQuestionsFromDoc(
            IFormFile file,
            [FromQuery] int count = 5,
            [FromQuery] string type = "ALL",
            [FromQuery] int difficulty = 3,
            [FromQuery] string topic = "GENERAL",
            [FromQuery] string? focusArea = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "Vui lòng chọn file PDF hoặc DOCX để phân tích." });

                var req = new GenerateQuestionsRequest
                {
                    QuestionCount = count,
                    QuestionType = type,
                    Difficulty = difficulty,
                    TopicCode = topic,
                    FocusArea = focusArea
                };

                using var stream = file.OpenReadStream();
                var generated = await _aiGenerator.GenerateQuestionsFromDocumentAsync(stream, file.FileName, req);
                return Ok(generated);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi AI sinh câu hỏi từ tài liệu: {ex.Message}" });
            }
        }

        // ── POST api/quiz/questions/generate-from-text [Kịch bản B] ──────────
        [HttpPost("questions/generate-from-text")]
        public async Task<IActionResult> GenerateQuestionsFromText([FromBody] GenerateQuestionsRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.DocumentText))
                    return BadRequest(new { message = "Vui lòng nhập đoạn văn bản nội dung để AI sinh câu hỏi." });

                var generated = await _aiGenerator.GenerateQuestionsFromTextAsync(request);
                return Ok(generated);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi AI sinh câu hỏi từ văn bản: {ex.Message}" });
            }
        }

        // ── POST api/quiz/questions/ai-solve-preview ─────────────────────────
        [HttpPost("questions/ai-solve-preview")]
        public async Task<IActionResult> AiSolvePreviewQuestions([FromBody] List<DocxImportPreviewDto> items)
        {
            try
            {
                if (items == null || items.Count == 0)
                    return BadRequest(new { message = "Danh sách câu hỏi trống." });

                var solved = await _geminiSolver.AutoSolveQuestionsAsync(items);
                return Ok(solved);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi gọi AI giải đề: {ex.Message}" });
            }
        }

        // ── POST api/quiz/questions/convert-math-latex ───────────────────────
        [HttpPost("questions/convert-math-latex")]
        public async Task<IActionResult> ConvertMathImagesToLatex([FromBody] List<DocxImportPreviewDto> items)
        {
            try
            {
                if (items == null || items.Count == 0)
                    return BadRequest(new { message = "Danh sách câu hỏi trống." });

                var converted = await _geminiSolver.ConvertMathImagesToLatexAsync(items);
                return Ok(converted);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi chuyển đổi công thức LaTeX: {ex.Message}" });
            }
        }

        // ── POST api/quiz/questions/correct-vietnamese [On-Demand VNCorrect & Maprepl] ──
        [HttpPost("questions/correct-vietnamese")]
        public IActionResult CorrectVietnameseQuestionsBatch([FromBody] List<DocxImportPreviewDto> items)
        {
            try
            {
                if (items == null || items.Count == 0)
                    return BadRequest(new { message = "Danh sách câu hỏi trống." });

                var corrected = _textCorrectionService.CorrectQuestionsBatch(items);
                return Ok(corrected);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi chuẩn hóa văn bản tiếng Việt: {ex.Message}" });
            }
        }

        // ── POST api/quiz/questions/bulk-toggle [NEW P3] ─────────────────────
        [HttpPost("questions/bulk-toggle")]
        public async Task<IActionResult> BulkToggleQuestions([FromBody] BulkToggleRequest request)
        {
            try
            {
                if (request?.Ids == null || request.Ids.Count == 0)
                    return BadRequest(new { message = "Danh sách ID trống." });

                var questions = await _db.Questions
                    .Where(q => request.Ids.Contains(q.Id))
                    .ToListAsync();

                int updated = 0;
                int skipped = 0;
                foreach (var q in questions)
                {
                    // [Ownership Scoping] TeamLeader chỉ được bật/tắt câu hỏi mình sở hữu.
                    if (!await _ownershipGuard.CanManageAsync(User, q.CreatorId, q.OrgUnitId))
                    {
                        skipped++;
                        continue;
                    }

                    var dict = new Dictionary<string, object>();
                    foreach (var prop in q.Payload.RootElement.EnumerateObject())
                        dict[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                            ? (object)prop.Value.GetString()!
                            : prop.Value.GetRawText();

                    dict["enabled"] = request.Enabled;
                    q.Payload = JsonSerializer.SerializeToDocument(dict);
                    updated++;
                }

                await _db.SaveChangesAsync();
                return Ok(new { success = true, updated, skipped });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi batch toggle: {ex.Message}" });
            }
        }

        // ── POST api/quiz/questions/confirm-import-docx ───────────────────────
        [HttpPost("questions/confirm-import-docx")]
        public async Task<IActionResult> ConfirmImportQuestionsDocx([FromBody] List<DocxImportPreviewDto> items)
        {
            if (items == null || items.Count == 0)
                return BadRequest(new { message = "Danh sách câu hỏi trống." });

            // [P1 Data-Integrity Fix] Bọc toàn bộ luồng import (tạo Context dùng chung +
            // tạo/đồng bộ Topic cha-con + tạo hàng loạt Question) trong 1 transaction duy
            // nhất. Trước đây có nhiều SaveChangesAsync() rải rác — nếu lỗi giữa chừng (vd
            // câu hỏi thứ 300/500), DB rơi vào trạng thái nửa vời (một số Topic/Context đã
            // tạo, phần còn lại thì không), rất khó dọn dẹp thủ công.
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
            if (_db.Database.IsRelational())
                transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var toSave = new List<QuestionBase>();

                // [Universal Shared Context] Khử trùng lặp và lưu trữ bài đọc hiểu/ngữ cảnh dùng chung một lần duy nhất
                var contextMap = new Dictionary<string, QuestionContext>(StringComparer.Ordinal);
                foreach (var item in items)
                {
                    if (!string.IsNullOrWhiteSpace(item.ContextContent))
                    {
                        string trimmed = item.ContextContent.Trim();
                        string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(trimmed)));
                        if (!contextMap.ContainsKey(hash))
                        {
                            var existingCtx = await _db.QuestionContexts.FirstOrDefaultAsync(c => c.ContentHash == hash);
                            if (existingCtx == null)
                            {
                                existingCtx = new QuestionContext
                                {
                                    Id = Guid.NewGuid(),
                                    Title = string.IsNullOrWhiteSpace(item.ContextTitle) ? "Bài đọc hiểu dùng chung" : item.ContextTitle.Trim(),
                                    Content = trimmed,
                                    ContentType = "text",
                                    ContentHash = hash,
                                    CreatedAt = DateTime.UtcNow
                                };
                                _db.QuestionContexts.Add(existingCtx);
                            }
                            contextMap[hash] = existingCtx;
                        }
                    }
                }
                if (contextMap.Count > 0)
                {
                    await _db.SaveChangesAsync();
                }

                // [UCIS v4.0 Hierarchical Topic Engine]
                // Tự động khởi tạo & đồng bộ Chủ đề Cha (Folder) và Chủ đề Con (File) có liên kết phân cấp ParentId
                var topicGroups = items
                    .GroupBy(i => string.IsNullOrWhiteSpace(i.TopicCode) ? "GENERAL" : i.TopicCode.Trim())
                    .ToList();

                foreach (var group in topicGroups)
                {
                    var topicCode = group.Key;
                    var firstItem = group.First();
                    var parentCode = !string.IsNullOrWhiteSpace(firstItem.ParentTopicCode) ? firstItem.ParentTopicCode.Trim() : null;
                    var parentName = !string.IsNullOrWhiteSpace(firstItem.ParentTopicName) ? firstItem.ParentTopicName.Trim() : parentCode;
                    var topicName = !string.IsNullOrWhiteSpace(firstItem.TopicName) ? firstItem.TopicName.Trim() : topicCode;
                    var domain = !string.IsNullOrWhiteSpace(firstItem.DomainCode) ? firstItem.DomainCode.Trim() : "GENERAL";
                    var topicDesc = !string.IsNullOrWhiteSpace(firstItem.TopicDescription)
                        ? firstItem.TopicDescription
                        : (!string.IsNullOrWhiteSpace(parentName) 
                            ? $"{topicName} | Đường dẫn: {parentName}" 
                            : $"{topicName} ({domain})");

                    // 1. Xử lý Chủ đề cha (Parent Topic) nếu có
                    Guid? parentId = null;
                    if (!string.IsNullOrWhiteSpace(parentCode))
                    {
                        var parentTopic = await _db.BankTopics.FirstOrDefaultAsync(bt => bt.Code == parentCode);
                        if (parentTopic == null)
                        {
                            parentTopic = new Domain.Entities.BankTopic
                            {
                                Id = Guid.NewGuid(),
                                Code = parentCode,
                                Name = parentName ?? parentCode,
                                CategoryCode = domain,
                                DomainCode = domain,
                                Scope = "COMMUNITY",
                                MaterializedPath = $"/{domain}/{parentCode}/",
                                DepthLevel = 1,
                                Urn = $"urn:aegis:topic:{domain.ToLowerInvariant()}:{parentCode.ToLowerInvariant()}",
                                Description = $"Thư mục chuyên đề: {parentName}",
                                VisibilityScope = Domain.Entities.TopicVisibility.Public,
                                Enabled = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            _db.BankTopics.Add(parentTopic);
                            await _db.SaveChangesAsync();
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(parentTopic.MaterializedPath))
                            {
                                parentTopic.MaterializedPath = $"/{domain}/{parentCode}/";
                                parentTopic.DepthLevel = 1;
                                parentTopic.DomainCode = domain;
                            }
                        }
                        parentId = parentTopic.Id;
                    }

                    // 2. Xử lý Chủ đề con (Child Topic)
                    var childTopic = await _db.BankTopics.FirstOrDefaultAsync(bt => bt.Code == topicCode);
                    var matPath = !string.IsNullOrWhiteSpace(parentCode)
                        ? $"/{domain}/{parentCode}/{topicCode}/"
                        : $"/{domain}/{topicCode}/";
                    int depth = !string.IsNullOrWhiteSpace(parentCode) ? 2 : 1;

                    if (childTopic == null)
                    {
                        childTopic = new Domain.Entities.BankTopic
                        {
                            Id = Guid.NewGuid(),
                            Code = topicCode,
                            Name = topicName,
                            CategoryCode = domain,
                            DomainCode = domain,
                            Scope = "COMMUNITY",
                            MaterializedPath = matPath,
                            DepthLevel = depth,
                            Urn = $"urn:aegis:topic:{domain.ToLowerInvariant()}:{topicCode.ToLowerInvariant()}",
                            Description = topicDesc,
                            ParentId = parentId,
                            QuestionCountCached = group.Count(),
                            VisibilityScope = Domain.Entities.TopicVisibility.Public,
                            Enabled = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.BankTopics.Add(childTopic);
                    }
                    else
                    {
                        // [Auto-Healing]: Chữa lành nếu topic trước đó bị gán mặc định Name = Code
                        if (childTopic.Name == childTopic.Code && !string.IsNullOrWhiteSpace(topicName) && topicName != topicCode)
                        {
                            childTopic.Name = topicName;
                        }
                        if (string.IsNullOrWhiteSpace(childTopic.Description) || childTopic.Description == "Tự động khởi tạo từ dữ liệu nhập file")
                        {
                            childTopic.Description = topicDesc;
                        }
                        if (!childTopic.ParentId.HasValue && parentId.HasValue && childTopic.Id != parentId.Value)
                        {
                            childTopic.ParentId = parentId;
                        }
                        childTopic.DomainCode = domain;
                        childTopic.MaterializedPath = matPath;
                        childTopic.DepthLevel = depth;
                        childTopic.QuestionCountCached = group.Count();
                    }
                }
                await _db.SaveChangesAsync();

                // [Pha khử trùng lặp & Hợp nhất lịch sử xuất hiện]
                var duplicates = items.Where(i => i.IsDuplicate && i.ExistingQuestionId.HasValue).ToList();
                var news = items.Where(i => !i.IsDuplicate || !i.ExistingQuestionId.HasValue).ToList();

                int mergedCount = 0;
                if (duplicates.Count > 0)
                {
                    var dupIds = duplicates.Select(d => d.ExistingQuestionId!.Value).Distinct().ToList();
                    var existingQuestions = await _db.Questions.Where(q => dupIds.Contains(q.Id)).ToListAsync();
                    var existingMap = existingQuestions.ToDictionary(q => q.Id);

                    foreach (var item in duplicates)
                    {
                        if (existingMap.TryGetValue(item.ExistingQuestionId!.Value, out var eq))
                        {
                            var appearance = new ProvenanceAppearance
                            {
                                SourceFile = item.SourceFileName ?? "UnknownSource",
                                ExamTitleInFile = item.SourceExamName,
                                SectionInFile = item.SourceSectionName,
                                OriginalQuestionNo = item.SourceQuestionNo,
                                RowIndex = item.SourceRowIndex,
                                ImportedAt = DateTime.UtcNow
                            };
                            _deduplicationEngine.MergeProvenanceAppearance(eq, appearance);
                            if (!string.IsNullOrWhiteSpace(item.TopicCode))
                            {
                                _deduplicationEngine.AssociateTopicToQuestion(eq, item.TopicCode.Trim(), item.TopicName);
                            }
                            mergedCount++;
                        }
                    }
                }

                foreach (var item in news)
                {
                    var type = QuestionFactory.NormalizeType(item.QuestionType);

                    // Tự động nhận diện MULTI_TRUE_FALSE (GDPT 2025):
                    // 1. Phân loại gốc là TRUE_FALSE và có từ 3 tiểu mục trở lên (a, b, c, d)
                    // 2. Hoặc nếu đáp án gợi ý là ma trận Đúng/Sai đa mệnh đề rõ ràng (ví dụ: "a) Đúng, b) Sai, c) Đúng, d) Sai")
                    if (type == "TRUE_FALSE" && item.Options?.Count >= 3)
                    {
                        type = "MULTI_TRUE_FALSE";
                    }
                    else if (!string.IsNullOrWhiteSpace(item.SuggestedAnswer) &&
                             Regex.IsMatch(item.SuggestedAnswer, @"[a-d][\)\:\.\s]+(đúng|sai|true|false)", RegexOptions.IgnoreCase))
                    {
                        type = "MULTI_TRUE_FALSE";
                    }

                    QuestionBase q = type switch
                    {
                        "MULTI"            => new MultiChoiceQuestion(),
                        "TRUE_FALSE"       => new TrueFalseQuestion(),
                        "MULTI_TRUE_FALSE" => new MultiTrueFalseQuestion(),
                        "SHORT_ANSWER"     => new ShortAnswerQuestion(),
                        "FILL_BLANK"       => new FillBlankQuestion(),
                        "ORDERING"         => new OrderingQuestion(),
                        "MATCHING"         => new MatchingQuestion(),
                        "ESSAY"            => new EssayQuestion(),
                        _                  => new SingleChoiceQuestion()
                    };

                    q.Id            = Guid.NewGuid();
                    item.Options ??= new List<string>();
                    q.Content       = item.Content;
                    q.Difficulty    = item.Difficulty;
                    q.CategoryCode  = string.IsNullOrWhiteSpace(item.TopicCode) ? "GENERAL" : item.TopicCode;
                    q.ContentType   = !string.IsNullOrWhiteSpace(item.MediaUrl) ? "image" : (string.IsNullOrWhiteSpace(item.ContentType) ? "text" : item.ContentType);
                    q.OptionType    = string.IsNullOrWhiteSpace(item.OptionType) ? "text" : item.OptionType;
                    q.DurationSeconds = item.DurationSeconds > 0 ? item.DurationSeconds : 60;
                    var rubric = !string.IsNullOrWhiteSpace(item.GradingRubric) ? item.GradingRubric : (type == "ESSAY" ? item.SuggestedAnswer : null);
                    q.CreatedAt     = DateTime.UtcNow;
                    q.IsCritical    = item.IsCritical;
                    q.SubCategory   = item.SubCategory;

                    // Gắn liên kết Ngữ cảnh dùng chung
                    if (!string.IsNullOrWhiteSpace(item.ContextContent))
                    {
                        string trimmed = item.ContextContent.Trim();
                        string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(trimmed)));
                        if (contextMap.TryGetValue(hash, out var ctxEntity))
                        {
                            q.ContextId = ctxEntity.Id;
                            q.Context = ctxEntity;
                        }
                    }

                    // Gán 5 trục tọa độ đa ngành & Thẻ thông minh
                    q.DomainCode = string.IsNullOrWhiteSpace(item.DomainCode) ? "GENERAL" : item.DomainCode.Trim();
                    q.Tags = item.Tags ?? new List<string>();

                    var canonicalHash = item.CanonicalContentHash ?? _deduplicationEngine.ComputeCanonicalContentHash(item.Content, item.Options);
                    var initialAppearance = new ProvenanceAppearance
                    {
                        SourceFile = item.SourceFileName ?? "OriginalSource",
                        ExamTitleInFile = item.SourceExamName,
                        SectionInFile = item.SourceSectionName,
                        OriginalQuestionNo = item.SourceQuestionNo,
                        RowIndex = item.SourceRowIndex,
                        ImportedAt = DateTime.UtcNow
                    };

                    if ((type == "SINGLE" || type == "MATCHING" || type == "ORDERING" || type == "FILL_BLANK") && (q is SingleChoiceQuestion sq || q is OrderingQuestion || q is MatchingQuestion || q is FillBlankQuestion))
                    {
                        q.Options = item.Options;
                        q.CorrectOption = !string.IsNullOrWhiteSpace(item.SuggestedAnswer) ? item.SuggestedAnswer.Trim() : "1";
                        var optPayload = item.Options.Select((o, idx) => new { id = (idx + 1).ToString(), text = o }).ToArray();
                        q.Payload = JsonSerializer.SerializeToDocument(new
                        {
                            enabled           = true,
                            options           = optPayload,
                            correctAnswer     = item.SuggestedAnswer,
                            explanation       = item.AiExplanation,
                            rubric            = rubric,
                            durationSeconds   = q.DurationSeconds,
                            mediaUrl          = item.MediaUrl,
                            contextId         = q.ContextId,
                            contextTitle      = q.Context?.Title ?? item.ContextTitle,
                            contextContent    = q.Context?.Content ?? item.ContextContent,
                            domainCode        = q.DomainCode,
                            tags              = q.Tags,
                            targetLevel       = item.TargetLevel,
                            assessmentPurpose = item.AssessmentPurpose,
                            issuingOrg        = item.IssuingOrg,
                            benchmarkYear     = item.BenchmarkYear,
                            benchmarkStandard = item.BenchmarkStandard,
                            creatorId         = item.CreatorId,
                            creatorName       = item.CreatorName,
                            orgUnitId         = item.OrgUnitId,
                            orgUnitName       = item.OrgUnitName,
                            scope             = string.IsNullOrWhiteSpace(item.Scope) ? "COMMUNITY" : item.Scope,
                            visibilityScope   = string.IsNullOrWhiteSpace(item.VisibilityScope) ? "PUBLIC" : item.VisibilityScope,
                            contributionStatus = item.ContributionStatus,
                            canonicalContentHash = canonicalHash,
                            sourceFileName    = item.SourceFileName,
                            sourceExamName    = item.SourceExamName,
                            sourceSectionName = item.SourceSectionName,
                            sourceQuestionNo  = item.SourceQuestionNo,
                            sourceRowIndex    = item.SourceRowIndex,
                            occurrenceCount   = 1,
                            _provenance       = new
                            {
                                canonicalContentHash = canonicalHash,
                                occurrences          = 1,
                                firstIngestedAt      = DateTime.UtcNow.ToString("o"),
                                lastSeenAt           = DateTime.UtcNow.ToString("o"),
                                appearances          = new[] { initialAppearance }
                            }
                        });
                    }
                    else if (type == "ESSAY" && q is EssayQuestion eq)
                    {
                        eq.GradingRubric = rubric ?? item.SuggestedAnswer ?? string.Empty;
                        eq.Options       = null;
                        eq.Payload = JsonSerializer.SerializeToDocument(new
                        {
                            enabled           = true,
                            rubric            = eq.GradingRubric,
                            explanation       = item.AiExplanation,
                            durationSeconds   = q.DurationSeconds,
                            mediaUrl          = item.MediaUrl,
                            contextId         = q.ContextId,
                            contextTitle      = q.Context?.Title ?? item.ContextTitle,
                            contextContent    = q.Context?.Content ?? item.ContextContent,
                            domainCode        = q.DomainCode,
                            tags              = q.Tags,
                            targetLevel       = item.TargetLevel,
                            assessmentPurpose = item.AssessmentPurpose,
                            issuingOrg        = item.IssuingOrg,
                            benchmarkYear     = item.BenchmarkYear,
                            benchmarkStandard = item.BenchmarkStandard,
                            creatorId         = item.CreatorId,
                            creatorName       = item.CreatorName,
                            orgUnitId         = item.OrgUnitId,
                            orgUnitName       = item.OrgUnitName,
                            scope             = string.IsNullOrWhiteSpace(item.Scope) ? "COMMUNITY" : item.Scope,
                            visibilityScope   = string.IsNullOrWhiteSpace(item.VisibilityScope) ? "PUBLIC" : item.VisibilityScope,
                            contributionStatus = item.ContributionStatus,
                            canonicalContentHash = canonicalHash,
                            sourceFileName    = item.SourceFileName,
                            sourceExamName    = item.SourceExamName,
                            sourceSectionName = item.SourceSectionName,
                            sourceQuestionNo  = item.SourceQuestionNo,
                            sourceRowIndex    = item.SourceRowIndex,
                            occurrenceCount   = 1,
                            _provenance       = new
                            {
                                canonicalContentHash = canonicalHash,
                                occurrences          = 1,
                                firstIngestedAt      = DateTime.UtcNow.ToString("o"),
                                lastSeenAt           = DateTime.UtcNow.ToString("o"),
                                appearances          = new[] { initialAppearance }
                            }
                        });
                    }
                    else if (type == "MULTI" && q is MultiChoiceQuestion mq)
                    {
                        mq.Options = item.Options;
                        mq.CorrectOption = !string.IsNullOrWhiteSpace(item.SuggestedAnswer) ? item.SuggestedAnswer.Trim() : string.Empty;
                        var optPayload = item.Options.Select((o, idx) => new { id = (idx + 1).ToString(), text = o }).ToArray();
                        var correctAnswersArray = (item.SuggestedAnswer ?? "")
                            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .ToArray();

                        mq.Payload = JsonSerializer.SerializeToDocument(new
                        {
                            enabled           = true,
                            options           = optPayload,
                            correctAnswers    = correctAnswersArray,
                            correctAnswer     = mq.CorrectOption,
                            explanation       = item.AiExplanation,
                            rubric            = rubric,
                            durationSeconds   = q.DurationSeconds,
                            mediaUrl          = item.MediaUrl,
                            contextId         = q.ContextId,
                            contextTitle      = q.Context?.Title ?? item.ContextTitle,
                            contextContent    = q.Context?.Content ?? item.ContextContent,
                            domainCode        = q.DomainCode,
                            tags              = q.Tags,
                            targetLevel       = item.TargetLevel,
                            assessmentPurpose = item.AssessmentPurpose,
                            issuingOrg        = item.IssuingOrg,
                            benchmarkYear     = item.BenchmarkYear,
                            benchmarkStandard = item.BenchmarkStandard,
                            creatorId         = item.CreatorId,
                            creatorName       = item.CreatorName,
                            orgUnitId         = item.OrgUnitId,
                            orgUnitName       = item.OrgUnitName,
                            scope             = string.IsNullOrWhiteSpace(item.Scope) ? "COMMUNITY" : item.Scope,
                            visibilityScope   = string.IsNullOrWhiteSpace(item.VisibilityScope) ? "PUBLIC" : item.VisibilityScope,
                            contributionStatus = item.ContributionStatus,
                            canonicalContentHash = canonicalHash,
                            sourceFileName    = item.SourceFileName,
                            sourceExamName    = item.SourceExamName,
                            sourceSectionName = item.SourceSectionName,
                            sourceQuestionNo  = item.SourceQuestionNo,
                            sourceRowIndex    = item.SourceRowIndex,
                            occurrenceCount   = 1,
                            _provenance       = new
                            {
                                canonicalContentHash = canonicalHash,
                                occurrences          = 1,
                                firstIngestedAt      = DateTime.UtcNow.ToString("o"),
                                lastSeenAt           = DateTime.UtcNow.ToString("o"),
                                appearances          = new[] { initialAppearance }
                            }
                        });
                    }
                    else if (type == "TRUE_FALSE" && q is TrueFalseQuestion tq)
                    {
                        tq.Options = item.Options != null && item.Options.Count >= 2
                            ? item.Options
                            : new List<string> { "Đúng", "Sai" };
                        tq.CorrectOption = item.SuggestedAnswer == "2" ? "Sai" : "Đúng";
                        var optPayload = tq.Options.Select((o, idx) => new { id = (idx + 1).ToString(), text = o }).ToArray();
                        tq.Payload = JsonSerializer.SerializeToDocument(new
                        {
                            enabled           = true,
                            options           = optPayload,
                            correctAnswer     = item.SuggestedAnswer == "2" ? "2" : "1",
                            explanation       = item.AiExplanation,
                            rubric            = rubric,
                            durationSeconds   = q.DurationSeconds,
                            mediaUrl          = item.MediaUrl,
                            contextId         = q.ContextId,
                            contextTitle      = q.Context?.Title ?? item.ContextTitle,
                            contextContent    = q.Context?.Content ?? item.ContextContent,
                            domainCode        = q.DomainCode,
                            tags              = q.Tags,
                            targetLevel       = item.TargetLevel,
                            assessmentPurpose = item.AssessmentPurpose,
                            issuingOrg        = item.IssuingOrg,
                            benchmarkYear     = item.BenchmarkYear,
                            benchmarkStandard = item.BenchmarkStandard,
                            creatorId         = item.CreatorId,
                            creatorName       = item.CreatorName,
                            orgUnitId         = item.OrgUnitId,
                            orgUnitName       = item.OrgUnitName,
                            scope             = string.IsNullOrWhiteSpace(item.Scope) ? "COMMUNITY" : item.Scope,
                            visibilityScope   = string.IsNullOrWhiteSpace(item.VisibilityScope) ? "PUBLIC" : item.VisibilityScope,
                            contributionStatus = item.ContributionStatus,
                            canonicalContentHash = canonicalHash,
                            sourceFileName    = item.SourceFileName,
                            sourceExamName    = item.SourceExamName,
                            sourceSectionName = item.SourceSectionName,
                            sourceQuestionNo  = item.SourceQuestionNo,
                            sourceRowIndex    = item.SourceRowIndex,
                            occurrenceCount   = 1,
                            _provenance       = new
                            {
                                canonicalContentHash = canonicalHash,
                                occurrences          = 1,
                                firstIngestedAt      = DateTime.UtcNow.ToString("o"),
                                lastSeenAt           = DateTime.UtcNow.ToString("o"),
                                appearances          = new[] { initialAppearance }
                            }
                        });
                    }
                    else if (type == "SHORT_ANSWER" && q is ShortAnswerQuestion saq)
                    {
                        saq.CorrectOption = item.SuggestedAnswer ?? "";
                        saq.Payload = JsonSerializer.SerializeToDocument(new
                        {
                            enabled           = true,
                            correctAnswer     = item.SuggestedAnswer,
                            explanation       = item.AiExplanation,
                            rubric            = rubric,
                            durationSeconds   = q.DurationSeconds,
                            mediaUrl          = item.MediaUrl,
                            contextId         = q.ContextId,
                            contextTitle      = q.Context?.Title ?? item.ContextTitle,
                            contextContent    = q.Context?.Content ?? item.ContextContent,
                            domainCode        = q.DomainCode,
                            tags              = q.Tags,
                            targetLevel       = item.TargetLevel,
                            assessmentPurpose = item.AssessmentPurpose,
                            issuingOrg        = item.IssuingOrg,
                            benchmarkYear     = item.BenchmarkYear,
                            benchmarkStandard = item.BenchmarkStandard,
                            creatorId         = item.CreatorId,
                            creatorName       = item.CreatorName,
                            orgUnitId         = item.OrgUnitId,
                            orgUnitName       = item.OrgUnitName,
                            scope             = string.IsNullOrWhiteSpace(item.Scope) ? "COMMUNITY" : item.Scope,
                            visibilityScope   = string.IsNullOrWhiteSpace(item.VisibilityScope) ? "PUBLIC" : item.VisibilityScope,
                            contributionStatus = item.ContributionStatus,
                            canonicalContentHash = canonicalHash,
                            sourceFileName    = item.SourceFileName,
                            sourceExamName    = item.SourceExamName,
                            sourceSectionName = item.SourceSectionName,
                            sourceQuestionNo  = item.SourceQuestionNo,
                            sourceRowIndex    = item.SourceRowIndex,
                            occurrenceCount   = 1,
                            _provenance       = new
                            {
                                canonicalContentHash = canonicalHash,
                                occurrences          = 1,
                                firstIngestedAt      = DateTime.UtcNow.ToString("o"),
                                lastSeenAt           = DateTime.UtcNow.ToString("o"),
                                appearances          = new[] { initialAppearance }
                            }
                        });
                    }
                    else if (type == "MULTI_TRUE_FALSE" && q is MultiTrueFalseQuestion mtq)
                    {
                        mtq.Options = item.Options ?? new List<string>();
                        mtq.CorrectOption = !string.IsNullOrWhiteSpace(item.SuggestedAnswer) ? item.SuggestedAnswer.Trim() : string.Empty;
                        var expectedMap = MultiTrueFalseQuestion.ExtractExpectedMap(null, mtq.CorrectOption, mtq.Options);
                        var subItemsPayload = mtq.Options.Select((o, idx) => {
                            string id = ((char)('a' + idx)).ToString();
                            bool isCorrect = expectedMap.TryGetValue(id, out var val) && val;
                            return new { id, text = o, expected = isCorrect };
                        }).ToArray();
                        var optPayload = mtq.Options.Select((o, idx) => new { id = ((char)('a' + idx)).ToString(), text = o }).ToArray();

                        mtq.Payload = JsonSerializer.SerializeToDocument(new
                        {
                            enabled              = true,
                            options              = optPayload,
                            subItems             = subItemsPayload,
                            correctMap           = expectedMap,
                            correctAnswer        = mtq.CorrectOption,
                            scoringScheme        = "TNPT_2025",
                            explanation          = item.AiExplanation,
                            rubric               = rubric,
                            durationSeconds      = q.DurationSeconds,
                            mediaUrl             = item.MediaUrl,
                            contextId            = q.ContextId,
                            contextTitle         = q.Context?.Title ?? item.ContextTitle,
                            contextContent       = q.Context?.Content ?? item.ContextContent,
                            domainCode           = q.DomainCode,
                            tags                 = q.Tags,
                            targetLevel          = item.TargetLevel,
                            assessmentPurpose    = item.AssessmentPurpose,
                            issuingOrg           = item.IssuingOrg,
                            benchmarkYear        = item.BenchmarkYear,
                            benchmarkStandard    = item.BenchmarkStandard,
                            creatorId            = item.CreatorId,
                            creatorName          = item.CreatorName,
                            orgUnitId            = item.OrgUnitId,
                            orgUnitName          = item.OrgUnitName,
                            scope                = string.IsNullOrWhiteSpace(item.Scope) ? "COMMUNITY" : item.Scope,
                            visibilityScope      = string.IsNullOrWhiteSpace(item.VisibilityScope) ? "PUBLIC" : item.VisibilityScope,
                            contributionStatus   = item.ContributionStatus,
                            canonicalContentHash = canonicalHash,
                            sourceFileName       = item.SourceFileName,
                            sourceExamName       = item.SourceExamName,
                            sourceSectionName    = item.SourceSectionName,
                            sourceQuestionNo     = item.SourceQuestionNo,
                            sourceRowIndex       = item.SourceRowIndex,
                            occurrenceCount      = 1,
                            _provenance          = new
                            {
                                canonicalContentHash = canonicalHash,
                                occurrences          = 1,
                                firstIngestedAt      = DateTime.UtcNow.ToString("o"),
                                lastSeenAt           = DateTime.UtcNow.ToString("o"),
                                appearances          = new[] { initialAppearance }
                            }
                        });
                    }

                    toSave.Add(q);
                }

                if (toSave.Count > 0)
                {
                    if (_db.Database.IsNpgsql())
                    {
                        try
                        {
                            await _db.Database.ExecuteSqlRawAsync(@"ALTER TABLE IF EXISTS ""Questions"" ALTER COLUMN ""QuestionType"" TYPE text;");
                        }
                        catch
                        {
                            // Bỏ qua nếu đã là text hoặc không có quyền DDL
                        }
                    }

                    _db.Questions.AddRange(toSave);
                }
                await _db.SaveChangesAsync();
                if (transaction != null) await transaction.CommitAsync();
                return Ok(new
                {
                    success = true,
                    count = toSave.Count,
                    createdCount = toSave.Count,
                    mergedDuplicateCount = mergedCount,
                    totalProcessed = items.Count
                });
            }
            catch (Exception ex)
            {
                if (transaction != null) await transaction.RollbackAsync();

                var innerMsg = ex.InnerException?.Message;
                var detailedMsg = string.IsNullOrWhiteSpace(innerMsg) ? ex.Message : $"{ex.Message} -> {innerMsg}";
                return StatusCode(500, new { message = $"Lỗi lưu câu hỏi: {detailedMsg}" });
            }
            finally
            {
                if (transaction != null) await transaction.DisposeAsync();
            }
        }

        // ── GET api/quiz/contexts ──────────────────────────────────────────────
        [HttpGet("contexts")]
        public async Task<IActionResult> GetContexts()
        {
            try
            {
                // [P2 Perf Fix] Trước đây dùng correlated subquery COUNT theo từng dòng
                // Context (`_db.Questions.Count(q => q.ContextId == c.Id)` bên trong
                // .Select()). Nay gộp đếm 1 lần bằng GroupBy rồi join bằng Dictionary,
                // giảm số subquery từ O(n) xuống 1 câu SQL tổng hợp duy nhất.
                var questionCountByContext = await _db.Questions
                    .Where(q => q.ContextId != null)
                    .GroupBy(q => q.ContextId!.Value)
                    .Select(g => new { ContextId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ContextId, x => x.Count);

                var contexts = await _db.QuestionContexts
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new
                    {
                        c.Id,
                        c.Title,
                        c.Content,
                        c.ContentType,
                        c.ContentHash,
                        c.CreatedAt
                    })
                    .ToListAsync();

                var result = contexts.Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Content,
                    c.ContentType,
                    c.ContentHash,
                    c.CreatedAt,
                    QuestionCount = questionCountByContext.GetValueOrDefault(c.Id, 0)
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lấy danh sách ngữ cảnh: {ex.Message}" });
            }
        }

        // ── POST api/quiz/questions/batch-assign-context ──────────────────────
        [HttpPost("questions/batch-assign-context")]
        public async Task<IActionResult> BatchAssignContext([FromBody] BatchAssignContextDto dto)
        {
            try
            {
                if (dto == null || dto.QuestionIds == null || dto.QuestionIds.Count == 0)
                    return BadRequest(new { message = "Danh sách câu hỏi cần gán trống." });

                QuestionContext? ctx = null;

                if (dto.ContextId.HasValue && dto.ContextId.Value != Guid.Empty)
                {
                    ctx = await _db.QuestionContexts.FirstOrDefaultAsync(c => c.Id == dto.ContextId.Value);
                }

                if (ctx == null && !string.IsNullOrWhiteSpace(dto.ContextContent))
                {
                    string trimmed = dto.ContextContent.Trim();
                    string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(trimmed)));

                    ctx = await _db.QuestionContexts.FirstOrDefaultAsync(c => c.ContentHash == hash);
                    if (ctx == null)
                    {
                        ctx = new QuestionContext
                        {
                            Id = Guid.NewGuid(),
                            Title = string.IsNullOrWhiteSpace(dto.ContextTitle) ? "Bài đọc hiểu dùng chung" : dto.ContextTitle.Trim(),
                            Content = trimmed,
                            ContentType = "text",
                            ContentHash = hash,
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.QuestionContexts.Add(ctx);
                        await _db.SaveChangesAsync();
                    }
                }

                if (ctx == null)
                    return BadRequest(new { message = "Không xác định được bài đọc hiểu / ngữ cảnh để gán." });

                var candidateQuestions = await _db.Questions
                    .Where(q => dto.QuestionIds.Contains(q.Id))
                    .ToListAsync();

                // [Ownership Scoping] TeamLeader chỉ gán ngữ cảnh cho câu hỏi mình sở hữu.
                var questions = new List<QuestionBase>();
                foreach (var q in candidateQuestions)
                {
                    if (await _ownershipGuard.CanManageAsync(User, q.CreatorId, q.OrgUnitId))
                        questions.Add(q);
                }

                foreach (var q in questions)
                {
                    q.ContextId = ctx.Id;
                    q.Context = ctx;

                    // Đồng bộ payload
                    try
                    {
                        var dict = new Dictionary<string, object>();
                        if (q.Payload != null)
                        {
                            foreach (var prop in q.Payload.RootElement.EnumerateObject())
                            {
                                dict[prop.Name] = prop.Value.Clone();
                            }
                        }
                        dict["contextId"] = ctx.Id;
                        dict["contextTitle"] = ctx.Title;
                        dict["contextContent"] = ctx.Content;

                        q.Payload = JsonSerializer.SerializeToDocument(dict);
                    }
                    catch { /* fallback */ }
                }

                await _db.SaveChangesAsync();
                return Ok(new
                {
                    success = true,
                    count = questions.Count,
                    context = new
                    {
                        ctx.Id,
                        ctx.Title,
                        ctx.Content
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi gán ngữ cảnh hàng loạt: {ex.Message}" });
            }
        }

        // ── POST api/quiz/questions/batch-remove-context ──────────────────────
        [HttpPost("questions/batch-remove-context")]
        public async Task<IActionResult> BatchRemoveContext([FromBody] BatchRemoveContextDto dto)
        {
            try
            {
                if (dto == null || dto.QuestionIds == null || dto.QuestionIds.Count == 0)
                    return BadRequest(new { message = "Danh sách câu hỏi cần gỡ trống." });

                var candidateQuestions = await _db.Questions
                    .Where(q => dto.QuestionIds.Contains(q.Id))
                    .ToListAsync();

                // [Ownership Scoping] TeamLeader chỉ gỡ ngữ cảnh khỏi câu hỏi mình sở hữu.
                var questions = new List<QuestionBase>();
                foreach (var q in candidateQuestions)
                {
                    if (await _ownershipGuard.CanManageAsync(User, q.CreatorId, q.OrgUnitId))
                        questions.Add(q);
                }

                foreach (var q in questions)
                {
                    q.ContextId = null;
                    q.Context = null;

                    // Gỡ khỏi payload
                    try
                    {
                        if (q.Payload != null)
                        {
                            var dict = new Dictionary<string, object>();
                            foreach (var prop in q.Payload.RootElement.EnumerateObject())
                            {
                                if (prop.Name != "contextId" && prop.Name != "contextTitle" && prop.Name != "contextContent")
                                {
                                    dict[prop.Name] = prop.Value.Clone();
                                }
                            }
                            q.Payload = JsonSerializer.SerializeToDocument(dict);
                        }
                    }
                    catch { /* fallback */ }
                }

                await _db.SaveChangesAsync();
                return Ok(new { success = true, count = questions.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi gỡ ngữ cảnh: {ex.Message}" });
            }
        }

        // ── POST api/quiz/questions/batch-tags [Universal Multi-Industry] ────
        [HttpPost("questions/batch-tags")]
        public async Task<IActionResult> BatchAssignTags([FromBody] BatchAssignTagsDto dto)
        {
            try
            {
                if (dto == null || dto.QuestionIds == null || dto.QuestionIds.Count == 0)
                    return BadRequest(new { message = "Danh sách câu hỏi cần gán thẻ trống." });

                var candidateQuestions = await _db.Questions
                    .Where(q => dto.QuestionIds.Contains(q.Id))
                    .ToListAsync();

                // [Ownership Scoping] TeamLeader chỉ gán thẻ cho câu hỏi mình sở hữu.
                var questions = new List<QuestionBase>();
                foreach (var q in candidateQuestions)
                {
                    if (await _ownershipGuard.CanManageAsync(User, q.CreatorId, q.OrgUnitId))
                        questions.Add(q);
                }

                foreach (var q in questions)
                {
                    if (!string.IsNullOrWhiteSpace(dto.DomainCode))
                        q.DomainCode = dto.DomainCode;

                    if (dto.Tags != null && dto.Tags.Count > 0)
                    {
                        q.Tags ??= new List<string>();
                        foreach (var tag in dto.Tags)
                        {
                            var cleanTag = tag.Trim();
                            if (!string.IsNullOrWhiteSpace(cleanTag) && !q.Tags.Contains(cleanTag, StringComparer.OrdinalIgnoreCase))
                            {
                                q.Tags.Add(cleanTag);
                            }
                        }
                    }

                    // Đồng bộ payload
                    try
                    {
                        var dict = new Dictionary<string, object>();
                        if (q.Payload != null)
                        {
                            foreach (var prop in q.Payload.RootElement.EnumerateObject())
                            {
                                dict[prop.Name] = prop.Value.Clone();
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(dto.TargetLevel)) dict["targetLevel"] = dto.TargetLevel;
                        if (!string.IsNullOrWhiteSpace(dto.AssessmentPurpose)) dict["assessmentPurpose"] = dto.AssessmentPurpose;
                        if (!string.IsNullOrWhiteSpace(dto.IssuingOrg)) dict["issuingOrg"] = dto.IssuingOrg;
                        if (dto.BenchmarkYear.HasValue) dict["benchmarkYear"] = dto.BenchmarkYear.Value;
                        if (!string.IsNullOrWhiteSpace(dto.BenchmarkStandard)) dict["benchmarkStandard"] = dto.BenchmarkStandard;

                        q.Payload = JsonSerializer.SerializeToDocument(dict);
                    }
                    catch { /* fallback */ }
                }

                await _db.SaveChangesAsync();
                return Ok(new { success = true, count = questions.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi gán thẻ hàng loạt: {ex.Message}" });
            }
        }

        // ── POST api/quiz/questions/auto-sniff-coordinates ───────────────────
        [HttpPost("questions/auto-sniff-coordinates")]
        public async Task<IActionResult> AutoSniffCoordinates([FromBody] AutoSniffCoordinatesRequest? request)
        {
            try
            {
                var query = _db.Questions.AsQueryable();
                if (request?.QuestionIds != null && request.QuestionIds.Count > 0)
                {
                    query = query.Where(q => request.QuestionIds.Contains(q.Id));
                }

                var questions = await query.ToListAsync();
                if (questions.Count == 0)
                {
                    return Ok(new { success = false, message = "Không tìm thấy câu hỏi nào.", count = 0 });
                }

                // Gom nhóm theo CategoryCode / Topic để phân tích chính xác nhất
                var grouped = questions.GroupBy(q => q.CategoryCode ?? "GENERAL");
                int updatedCount = 0;
                var sampleResults = new List<object>();

                // [P2 Perf Fix] Trước đây mỗi group lại query riêng 1 lần (N+1 round-trip
                // tới DB). Nay load 1 lần toàn bộ Topic khớp các CategoryCode liên quan.
                var topicCodes = grouped.Select(g => g.Key).ToList();
                var topicsByCode = (await _db.BankTopics
                        .AsNoTracking()
                        .Where(t => topicCodes.Contains(t.Code))
                        .ToListAsync())
                    .GroupBy(t => t.Code)
                    .ToDictionary(g => g.Key, g => g.First());

                var tenantId = _tenantContext.CurrentTenantId;
                var dynamicContext = await _domainContextService.BuildContextAsync(tenantId);

                foreach (var group in grouped)
                {
                    var topicCode = group.Key;
                    var topic = topicsByCode.GetValueOrDefault(topicCode);
                    var topicName = topic?.Name ?? topicCode;

                    var sampleContents = group.Select(q => q.Content).Take(20).ToList();
                    var sampleRefs = group.Select(q => q.Explanation).Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e!).Take(20).ToList();

                    var taxonomy = UniversalCoordinateTaxonomyEngine.SniffCoordinates(
                        topicName,
                        topicCode,
                        new[] { topicName, topic?.Description ?? "" },
                        sampleContents,
                        sampleRefs,
                        dynamicContext);

                    sampleResults.Add(new
                    {
                        topicCode,
                        topicName,
                        detected = taxonomy
                    });

                    if (request?.PreviewOnly == true)
                    {
                        continue;
                    }

                    foreach (var q in group)
                    {
                        q.DomainCode = taxonomy.DomainCode;
                        q.Tags ??= new List<string>();
                        foreach (var tag in taxonomy.Tags)
                        {
                            if (!q.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                                q.Tags.Add(tag);
                        }

                        // Đồng bộ payload
                        try
                        {
                            var dict = new Dictionary<string, object>();
                            if (q.Payload != null)
                            {
                                foreach (var prop in q.Payload.RootElement.EnumerateObject())
                                {
                                    dict[prop.Name] = prop.Value.Clone();
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(taxonomy.TargetLevel)) dict["targetLevel"] = taxonomy.TargetLevel;
                            if (!string.IsNullOrWhiteSpace(taxonomy.AssessmentPurpose)) dict["assessmentPurpose"] = taxonomy.AssessmentPurpose;
                            if (!string.IsNullOrWhiteSpace(taxonomy.IssuingOrg)) dict["issuingOrg"] = taxonomy.IssuingOrg;
                            if (taxonomy.BenchmarkYear.HasValue) dict["benchmarkYear"] = taxonomy.BenchmarkYear.Value;
                            if (!string.IsNullOrWhiteSpace(taxonomy.BenchmarkStandard)) dict["benchmarkStandard"] = taxonomy.BenchmarkStandard;
                            dict["domainCode"] = taxonomy.DomainCode;
                            dict["tags"] = q.Tags;

                            q.Payload = JsonSerializer.SerializeToDocument(dict);
                        }
                        catch { }

                        updatedCount++;
                    }
                }

                if (request?.PreviewOnly != true)
                {
                    await _db.SaveChangesAsync();
                }

                return Ok(new
                {
                    success = true,
                    count = updatedCount,
                    previewOnly = request?.PreviewOnly == true,
                    analysis = sampleResults
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi tự động nhận diện tọa độ: {ex.Message}" });
            }
        }

        // ── GET api/quiz/tags/suggestions ────────────────────────────────────
        [HttpGet("tags/suggestions")]
        public async Task<IActionResult> GetTagSuggestions([FromQuery] string? domainCode)
        {
            try
            {
                var query = _db.Questions.AsQueryable();
                if (!string.IsNullOrWhiteSpace(domainCode) && domainCode != "ALL")
                    query = query.Where(q => q.DomainCode == domainCode);

                var allTags = await query.Select(q => q.Tags).ToListAsync();
                var tagFreq = allTags
                    .SelectMany(tags => tags ?? Enumerable.Empty<string>())
                    .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count())
                    .Select(g => new { tag = g.Key, count = g.Count() })
                    .Take(30)
                    .ToList();

                return Ok(tagFreq);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi gợi ý thẻ: {ex.Message}" });
            }
        }

        // ── GET api/quiz/questions/missing-answers ─────────────────────────────
        [HttpGet("questions/missing-answers")]
        public async Task<IActionResult> GetMissingAnswers()
        {
            try
            {
                var questions = await _db.Questions
                    .Where(q =>
                        (!(q is EssayQuestion) && (q.CorrectOption == null || q.CorrectOption == "")) ||
                        ((q is EssayQuestion)  && (((EssayQuestion)q).GradingRubric == null || ((EssayQuestion)q).GradingRubric == "")))
                    .ToListAsync();
                return Ok(questions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi quét câu hỏi thiếu đáp án: {ex.Message}" });
            }
        }

        // ── POST api/quiz/questions/{id}/suggest-answer ────────────────────────
        [HttpPost("questions/{id:guid}/suggest-answer")]
        public async Task<IActionResult> SuggestAnswer(Guid id)
        {
            try
            {
                var question = await _db.Questions.FindAsync(id);
                if (question == null)
                    return NotFound(new { message = "Không tìm thấy câu hỏi" });

                var previewDto = new DocxImportPreviewDto
                {
                    TempId       = question.Id.ToString(),
                    Content      = question.Content,
                    QuestionType = (question is EssayQuestion) ? "ESSAY" : "SINGLE",
                    Options      = question.Options ?? new List<string>()
                };

                var solved = await _geminiSolver.AutoSolveQuestionsAsync(new List<DocxImportPreviewDto> { previewDto });
                var result = solved.FirstOrDefault();

                if (result == null)
                    return BadRequest(new { message = "Gemini AI không thể đưa ra gợi ý." });

                return Ok(new
                {
                    questionId      = id,
                    suggestedAnswer = result.SuggestedAnswer,
                    aiExplanation   = result.AiExplanation
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi gọi AI giải câu hỏi: {ex.Message}" });
            }
        }

        // ── PUT api/quiz/questions/{id}/update-answer ──────────────────────────
        [HttpPut("questions/{id:guid}/update-answer")]
        public async Task<IActionResult> UpdateAnswer(Guid id, [FromBody] UpdateAnswerRequest request)
        {
            try
            {
                var q = await _db.Questions.FindAsync(id);
                if (q == null)
                    return NotFound(new { message = "Không tìm thấy câu hỏi" });

                if (!await _ownershipGuard.CanManageAsync(User, q.CreatorId, q.OrgUnitId))
                    return Forbid();

                // [Data-Loss Fix] Merge vào Payload hiện có thay vì ghi đè toàn bộ — trước đây
                // SerializeToDocument(new {...}) chỉ giữ 2-3 field, xóa mất creatorId/orgUnitId/
                // scope/tags/domainCode/contextId..., khiến TeamLeader mất quyền sở hữu câu hỏi
                // của chính mình ngay sau lần đầu sửa đáp án.
                var payloadDict = new Dictionary<string, object>();
                if (q.Payload != null)
                {
                    foreach (var prop in q.Payload.RootElement.EnumerateObject())
                        payloadDict[prop.Name] = prop.Value.Clone();
                }

                if (q is SingleChoiceQuestion sq)
                {
                    sq.CorrectOption = request.CorrectAnswer;
                    var optPayload = sq.Options?.Select((o, idx) => new { id = (idx + 1).ToString(), text = o }).ToArray()
                                    ?? Array.Empty<object>();
                    string correctVal = request.CorrectAnswer;
                    if (sq.Options != null)
                    {
                        int idx = sq.Options.IndexOf(request.CorrectAnswer);
                        if (idx >= 0) correctVal = (idx + 1).ToString();
                    }
                    payloadDict["options"]       = optPayload;
                    payloadDict["correctAnswer"] = correctVal;
                    payloadDict["explanation"]   = request.AiExplanation ?? "";
                    sq.Payload = JsonSerializer.SerializeToDocument(payloadDict);
                }
                else if (q is EssayQuestion eq)
                {
                    eq.GradingRubric = request.CorrectAnswer;
                    payloadDict["rubric"]      = request.CorrectAnswer;
                    payloadDict["explanation"] = request.AiExplanation ?? "";
                    eq.Payload = JsonSerializer.SerializeToDocument(payloadDict);
                }
                else
                {
                    q.CorrectOption = request.CorrectAnswer;
                    payloadDict["correctAnswer"] = request.CorrectAnswer;
                    payloadDict["explanation"]   = request.AiExplanation ?? "";
                    q.Payload = JsonSerializer.SerializeToDocument(payloadDict);
                }

                await _db.SaveChangesAsync();
                return Ok(new { success = true, questionId = id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi cập nhật đáp án: {ex.Message}" });
            }
        }

        // ── Private Helpers ────────────────────────────────────────────────────
        private static QuestionBase BuildQuestion(QuestionUpsertDto dto)
        {
            var normType = QuestionFactory.NormalizeType(dto.QuestionType);
            var question = normType switch
            {
                "MULTI"            => (QuestionBase)new MultiChoiceQuestion(),
                "TRUE_FALSE"       => new TrueFalseQuestion(),
                "MULTI_TRUE_FALSE" => new MultiTrueFalseQuestion(),
                "SHORT_ANSWER"     => new ShortAnswerQuestion(),
                "FILL_BLANK"       => new FillBlankQuestion(),
                "ORDERING"         => new OrderingQuestion(),
                "MATCHING"         => new MatchingQuestion(),
                "ESSAY"            => new EssayQuestion(),
                _                  => new SingleChoiceQuestion()
            };

            question.Id              = Guid.NewGuid();
            question.Content         = dto.Content;
            question.Difficulty      = dto.Difficulty;
            question.DurationSeconds = dto.DurationSeconds;
            question.CategoryCode    = dto.CategoryCode;
            question.ContentType     = dto.ContentType ?? "text";
            question.OptionType      = dto.OptionType  ?? "text";
            question.Options         = dto.Options ?? new List<string>();
            question.CorrectOption   = dto.CorrectOption ?? string.Empty;
            question.CreatedAt       = DateTime.UtcNow;
            question.IsCritical      = dto.IsCritical;
            question.SubCategory     = dto.SubCategory;
            question.ContextId       = dto.ContextId;
            question.DomainCode      = string.IsNullOrWhiteSpace(dto.DomainCode) ? "EDUCATION" : dto.DomainCode;
            question.Tags            = dto.Tags ?? new List<string>();

            var dict = new Dictionary<string, object>();
            if (dto.Payload != null)
            {
                using var doc = JsonDocument.Parse(dto.Payload.ToString());
                foreach (var prop in doc.RootElement.EnumerateObject())
                    dict[prop.Name] = prop.Value.Clone();
            }
            else
            {
                dict["correctAnswer"] = dto.CorrectOption ?? "";
                dict["options"] = dto.Options ?? new List<string>();
            }
            if (normType == "MULTI")
            {
                var rawAns = dto.CorrectOption ?? string.Empty;
                var tokens = rawAns.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                dict["correctAnswers"] = tokens;
                dict["correctAnswer"] = rawAns;
            }
            if (normType == "MULTI_TRUE_FALSE")
            {
                dict["scoringScheme"] = "TNPT_2025";
                if (!string.IsNullOrWhiteSpace(dto.CorrectOption))
                {
                    dict["correctMap"] = MultiTrueFalseQuestion.ExtractExpectedMap(null, dto.CorrectOption, dto.Options);
                }
            }
            if (!string.IsNullOrWhiteSpace(dto.TargetLevel)) dict["targetLevel"] = dto.TargetLevel;
            if (!string.IsNullOrWhiteSpace(dto.AssessmentPurpose)) dict["assessmentPurpose"] = dto.AssessmentPurpose;
            if (!string.IsNullOrWhiteSpace(dto.IssuingOrg)) dict["issuingOrg"] = dto.IssuingOrg;
            if (dto.BenchmarkYear.HasValue) dict["benchmarkYear"] = dto.BenchmarkYear.Value;
            if (!string.IsNullOrWhiteSpace(dto.BenchmarkStandard)) dict["benchmarkStandard"] = dto.BenchmarkStandard;

            question.Payload = JsonSerializer.SerializeToDocument(dict);
            return question;
        }

        private static void ApplyDto(QuestionBase question, QuestionUpsertDto dto)
        {
            question.Content         = dto.Content;
            question.Difficulty      = dto.Difficulty;
            question.DurationSeconds = dto.DurationSeconds;
            question.CategoryCode    = dto.CategoryCode;
            question.ContentType     = dto.ContentType ?? question.ContentType;
            question.OptionType      = dto.OptionType  ?? question.OptionType;
            question.Options         = dto.Options ?? question.Options;
            question.CorrectOption   = dto.CorrectOption ?? question.CorrectOption;
            question.IsCritical      = dto.IsCritical;
            question.SubCategory     = dto.SubCategory;
            if (!string.IsNullOrWhiteSpace(dto.DomainCode))
                question.DomainCode  = dto.DomainCode;
            if (dto.Tags != null)
                question.Tags        = dto.Tags;
            if (dto.ContextId.HasValue)
                question.ContextId   = dto.ContextId;

            var dict = new Dictionary<string, object>();
            if (question.Payload != null)
            {
                foreach (var prop in question.Payload.RootElement.EnumerateObject())
                    dict[prop.Name] = prop.Value.Clone();
            }
            if (dto.Payload != null)
            {
                using var doc = JsonDocument.Parse(dto.Payload.ToString());
                foreach (var prop in doc.RootElement.EnumerateObject())
                    dict[prop.Name] = prop.Value.Clone();
            }
            else
            {
                dict["correctAnswer"] = dto.CorrectOption ?? question.CorrectOption ?? "";
                dict["options"] = dto.Options ?? question.Options ?? new List<string>();
            }
            if (question is MultiChoiceQuestion)
            {
                var rawAns = question.CorrectOption ?? string.Empty;
                var tokens = rawAns.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                dict["correctAnswers"] = tokens;
                dict["correctAnswer"] = rawAns;
            }
            if (question is MultiTrueFalseQuestion)
            {
                dict["scoringScheme"] = "TNPT_2025";
                dict["correctMap"] = MultiTrueFalseQuestion.ExtractExpectedMap(null, question.CorrectOption, question.Options);
            }
            if (!string.IsNullOrWhiteSpace(dto.TargetLevel)) dict["targetLevel"] = dto.TargetLevel;
            if (!string.IsNullOrWhiteSpace(dto.AssessmentPurpose)) dict["assessmentPurpose"] = dto.AssessmentPurpose;
            if (!string.IsNullOrWhiteSpace(dto.IssuingOrg)) dict["issuingOrg"] = dto.IssuingOrg;
            if (dto.BenchmarkYear.HasValue) dict["benchmarkYear"] = dto.BenchmarkYear.Value;
            if (!string.IsNullOrWhiteSpace(dto.BenchmarkStandard)) dict["benchmarkStandard"] = dto.BenchmarkStandard;

            question.Payload = JsonSerializer.SerializeToDocument(dict);
        }

        // ── POST api/quiz/questions/load-preset-sample ────────────────────────
        [HttpPost("questions/load-preset-sample")]
        public async Task<IActionResult> LoadPresetSample([FromQuery] string preset = "MATH")
        {
            try
            {
                int count = 0;
                if (preset.ToUpper().Contains("F2023") || preset.ToUpper().Contains("GPLX") || preset.ToUpper().Contains("600"))
                {
                    string samplePdf = @"D:\Cuong\DuAn\mybank\AegisQuiz\600caugplx.pdf";
                    if (!System.IO.File.Exists(samplePdf))
                    {
                        samplePdf = @"D:\Cuong\DuAn\mybank\AegisQuiz\GPLXf2023.pdf";
                    }

                    if (!System.IO.File.Exists(samplePdf))
                        return BadRequest(new { message = "Không tìm thấy file GPLX trên máy chủ." });

                    using var fs = System.IO.File.OpenRead(samplePdf);
                    var items = _pdfExtractor.ParsePdfQuestions(fs);

                    // Đồng bộ sa hình chuẩn từ bản cập nhật GPLX 2023 (khắc phục các sa hình 598, 599, 600)
                    string gplx2023Path = @"D:\Cuong\DuAn\mybank\AegisQuiz\GPLXf2023.pdf";
                    if (System.IO.File.Exists(gplx2023Path))
                    {
                        AegisQuiz.Infrastructure.Services.PdfParserService.PatchGplx2023SaHinh(items, gplx2023Path);
                    }

                    string topicCode = preset.ToUpper().Contains("F2023") ? "GPLX_F2023" : "GPLX_600";
                    string topicName = preset.ToUpper().Contains("F2023") ? "Bộ Đề Sát Hạch GPLX 2023 (Biển Báo & Sa Hình Chuẩn)" : "600 Câu Hỏi Sát Hạch Lái Xe (GPLX)";

                    if (!_db.BankTopics.Any(t => t.Code == topicCode))
                    {
                        _db.BankTopics.Add(new Domain.Entities.BankTopic
                        {
                            Id = Guid.NewGuid(),
                            Code = topicCode,
                            Name = topicName,
                            CategoryCode = "GPLX",
                            Description = "Tài liệu GPLX chuẩn hóa với từng sa hình và biển báo gắn chính xác theo từng câu hỏi",
                            VisibilityScope = "PUBLIC"
                        });
                    }

                    // Xóa các câu hỏi cũ của bộ đề nếu đã nạp trước đó để cập nhật mới nhất
                    var oldQs = _db.Questions.Where(q => q.CategoryCode == topicCode).ToList();
                    if (oldQs.Count > 0)
                    {
                        _db.Questions.RemoveRange(oldQs);
                    }

                    foreach (var item in items)
                    {
                        var q = new SingleChoiceQuestion
                        {
                            Id = Guid.NewGuid(),
                            Content = item.Content,
                            Difficulty = item.Difficulty,
                            CategoryCode = topicCode,
                            ContentType = item.ContentType,
                            OptionType = item.OptionType,
                            DurationSeconds = 60,
                            Options = item.Options,
                            CorrectOption = item.SuggestedAnswer,
                            CreatedAt = DateTime.UtcNow,
                            IsCritical = item.IsCritical,
                            SubCategory = item.SubCategory,
                            Payload = JsonSerializer.SerializeToDocument(new
                            {
                                options = item.Options.Select((o, idx) => new { id = (idx + 1).ToString(), text = o }),
                                correctAnswer = item.SuggestedAnswer,
                                isCritical = item.IsCritical,
                                subCategory = item.SubCategory,
                                explanation = item.AiExplanation
                            })
                        };
                        _db.Questions.Add(q);
                        count++;
                    }
                }
                else
                {
                    string sampleDocx = @"D:\Cuong\DuAn\mybank\AegisQuiz\DeToanGiaiChiTiet.docx";
                    if (!System.IO.File.Exists(sampleDocx))
                        return BadRequest(new { message = "Không tìm thấy file DeToanGiaiChiTiet.docx trên máy chủ." });

                    using var fs = System.IO.File.OpenRead(sampleDocx);
                    var items = _docxParser.ParseDocxFile(fs);

                    if (!_db.BankTopics.Any(t => t.Code == "TOAN_GDPT_2025"))
                    {
                        _db.BankTopics.Add(new Domain.Entities.BankTopic
                        {
                            Id = Guid.NewGuid(),
                            Code = "TOAN_GDPT_2025",
                            Name = "Toán Học Kỳ Thi Tốt Nghiệp THPT 2025",
                            CategoryCode = "TOAN_GDPT_2025",
                            Description = "Bộ đề thi thử và chính thức môn Toán cấu trúc chuẩn GDPT 2025 kèm lời giải chi tiết",
                            VisibilityScope = "PUBLIC"
                        });
                    }

                    foreach (var item in items.Take(100))
                    {
                        var q = new SingleChoiceQuestion
                        {
                            Id = Guid.NewGuid(),
                            Content = item.Content,
                            Difficulty = item.Difficulty,
                            CategoryCode = "TOAN_GDPT_2025",
                            ContentType = item.ContentType,
                            OptionType = item.OptionType,
                            DurationSeconds = 60,
                            Options = item.Options,
                            CorrectOption = item.SuggestedAnswer,
                            CreatedAt = DateTime.UtcNow,
                            IsCritical = item.IsCritical,
                            SubCategory = item.SubCategory,
                            Payload = JsonSerializer.SerializeToDocument(new
                            {
                                options = item.Options.Select((o, idx) => new { id = (idx + 1).ToString(), text = o }),
                                correctAnswer = item.SuggestedAnswer,
                                explanation = item.AiExplanation
                            })
                        };
                        _db.Questions.Add(q);
                        count++;
                    }
                }

                await _db.SaveChangesAsync();
                return Ok(new { success = true, count, message = $"Đã nạp thành công {count} câu hỏi vào Ngân hàng câu hỏi!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi khi nạp bộ đề mẫu: {ex.Message}" });
            }
        }

        private static bool IsQuestionEnabled(QuestionBase q)
        {
            if (q.Payload == null) return true;
            try
            {
                if (q.Payload.RootElement.TryGetProperty("enabled", out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.True)  return true;
                    if (prop.ValueKind == JsonValueKind.False) return false;
                    if (prop.ValueKind == JsonValueKind.String)
                        return !string.Equals(prop.GetString(), "false", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { /* fallback */ }
            return true;
        }
    }

    public class EnrichTaxonomyRuleRequest
    {
        public string TargetField { get; set; } = string.Empty;
        public string RawHeader { get; set; } = string.Empty;
        public string? DomainScope { get; set; } = "ALL";
        public string? LanguageCode { get; set; } = "any";
    }
}
