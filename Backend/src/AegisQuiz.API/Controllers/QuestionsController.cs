using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Domain.Factories;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Infrastructure.Services;
using AegisQuiz.Infrastructure.Excel;
using AegisQuiz.API.Services;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [World-Class Refactor] QuestionsController — Tách từ QuizController.
    /// Chỉ phụ trách: CRUD câu hỏi, Import Excel/Docx, Missing-Answer AI.
    /// Route base: api/quiz/questions
    /// </summary>
    [ApiController]
    [Route("api/quiz")]
    public class QuestionsController : ControllerBase
    {
        private readonly AegisQuizDbContext                  _db;
        private readonly IExcelParserService                 _excelParser;
        private readonly IDocxParserService                  _docxParser;
        private readonly IPdfExtractorService                _pdfExtractor;
        private readonly IGeminiSolverService                _geminiSolver;
        private readonly IAiQuestionGeneratorService         _aiGenerator;
        private readonly IVietnameseTextCorrectionService    _textCorrectionService;
        private readonly IUrlDocumentFetcher                 _urlFetcher;

        public QuestionsController(
            AegisQuizDbContext                  db,
            IExcelParserService                 excelParser,
            IDocxParserService                  docxParser,
            IPdfExtractorService                pdfExtractor,
            IGeminiSolverService                geminiSolver,
            IAiQuestionGeneratorService         aiGenerator,
            IVietnameseTextCorrectionService    textCorrectionService,
            IUrlDocumentFetcher                 urlFetcher)
        {
            _db                   = db;
            _excelParser          = excelParser;
            _docxParser           = docxParser;
            _pdfExtractor         = pdfExtractor;
            _geminiSolver         = geminiSolver;
            _aiGenerator          = aiGenerator;
            _textCorrectionService = textCorrectionService;
            _urlFetcher           = urlFetcher;
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
                    q = q.Where(x => x.CategoryCode == query.CategoryCode);

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

                _db.Questions.Remove(question);
                await _db.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi xóa câu hỏi: {ex.Message}" });
            }
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

                // Trả về dữ liệu thanh tra toàn diện cho Universal Cognitive Ingestion Studio (UCIS)
                return Ok(new
                {
                    fileName                  = preview.FileName,
                    totalSheets               = preview.TotalSheets,
                    totalQuestions            = preview.TotalQuestions,
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

                using var stream = file.OpenReadStream();
                var rawQuestions = _docxParser.ParseDocxFile(stream);

                if (rawQuestions == null || rawQuestions.Count == 0)
                    return BadRequest(new { message = "Không tìm thấy câu hỏi hợp lệ trong file DOCX." });

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

        // ── POST api/quiz/questions/import-url [Universal Smart URI Ingestion] ─
        [HttpPost("questions/import-url")]
        public async Task<IActionResult> ImportQuestionsFromUrl([FromBody] ImportUrlRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url))
                return BadRequest(new { message = "Vui lòng nhập đường dẫn URL tài liệu (Google Docs, Google Sheets, OneDrive hoặc direct link)." });

            try
            {
                var fetchResult = await _urlFetcher.FetchDocumentAsync(request.Url);
                if (!fetchResult.Success)
                {
                    return BadRequest(new
                    {
                        message = fetchResult.ErrorMessage ?? "Không thể tải tài liệu từ liên kết đã cung cấp.",
                        isRestricted = fetchResult.IsRestricted,
                        suggestedAction = fetchResult.SuggestedAction,
                        originalUrl = fetchResult.OriginalUrl
                    });
                }

                if (fetchResult.Stream == null || fetchResult.FileSizeBytes == 0)
                {
                    return BadRequest(new { message = "Tài liệu tải về có dung lượng rỗng." });
                }

                // 1. Nếu là định dạng EXCEL (bao gồm Google Sheets)
                if (fetchResult.DocumentType == "EXCEL")
                {
                    var preview = _excelParser.ParseExcelFileToPreview(fetchResult.Stream, fetchResult.FileName);
                    if (preview.TotalQuestions == 0)
                    {
                        return BadRequest(new
                        {
                            message = "Không tìm thấy câu hỏi hợp lệ trong bảng tính Excel / Google Sheets.",
                            warnings = preview.Warnings
                        });
                    }

                    return Ok(new
                    {
                        sourceType                = "EXCEL",
                        fileName                  = preview.FileName,
                        totalSheets               = preview.TotalSheets,
                        totalQuestions            = preview.TotalQuestions,
                        warnings                  = preview.Warnings,
                        detectedDomainCode        = preview.DetectedDomainCode,
                        detectedTargetLevel       = preview.DetectedTargetLevel,
                        detectedAssessmentPurpose = preview.DetectedAssessmentPurpose,
                        detectedIssuingOrg        = preview.DetectedIssuingOrg,
                        detectedBenchmarkYear     = preview.DetectedBenchmarkYear,
                        detectedBenchmarkStandard = preview.DetectedBenchmarkStandard,
                        detectedTags              = preview.DetectedTags,
                        globalMetadataBanner      = preview.GlobalMetadataBanner,
                        sheets                    = preview.Sheets
                    });
                }

                // 2. Nếu là định dạng PDF
                if (fetchResult.DocumentType == "PDF")
                {
                    var rawQuestions = _pdfExtractor.ParsePdfQuestions(fetchResult.Stream);
                    if (rawQuestions == null || rawQuestions.Count == 0)
                    {
                        return BadRequest(new { message = "Không tìm thấy câu hỏi định dạng sẵn trong file PDF." });
                    }

                    if (request.UseAi)
                    {
                        var solvedQuestions = await _geminiSolver.AutoSolveQuestionsAsync(rawQuestions);
                        return Ok(new
                        {
                            sourceType = "PDF",
                            fileName = fetchResult.FileName,
                            totalQuestions = solvedQuestions.Count,
                            questions = solvedQuestions
                        });
                    }

                    return Ok(new
                    {
                        sourceType = "PDF",
                        fileName = fetchResult.FileName,
                        totalQuestions = rawQuestions.Count,
                        questions = rawQuestions
                    });
                }

                // 3. Mặc định là định dạng Word DOCX (bao gồm Google Docs)
                {
                    var rawQuestions = _docxParser.ParseDocxFile(fetchResult.Stream);
                    if (rawQuestions == null || rawQuestions.Count == 0)
                    {
                        return BadRequest(new { message = "Không tìm thấy câu hỏi hợp lệ trong tài liệu Word / Google Docs." });
                    }

                    if (request.UseAi)
                    {
                        var solvedQuestions = await _geminiSolver.AutoSolveQuestionsAsync(rawQuestions);
                        return Ok(new
                        {
                            sourceType = "DOCX",
                            fileName = fetchResult.FileName,
                            totalQuestions = solvedQuestions.Count,
                            questions = solvedQuestions
                        });
                    }

                    return Ok(new
                    {
                        sourceType = "DOCX",
                        fileName = fetchResult.FileName,
                        totalQuestions = rawQuestions.Count,
                        questions = rawQuestions
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi xử lý nạp câu hỏi từ liên kết: {ex.Message}" });
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
                foreach (var q in questions)
                {
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
                return Ok(new { success = true, updated });
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
            try
            {
                if (items == null || items.Count == 0)
                    return BadRequest(new { message = "Danh sách câu hỏi trống." });

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
                        : $"{topicName} | Số lượng: {group.Count()} câu hỏi";

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

                foreach (var item in items)
                {
                    var type = QuestionFactory.NormalizeType(item.QuestionType);
                    QuestionBase q = type switch
                    {
                        "MULTI"        => new MultiChoiceQuestion(),
                        "TRUE_FALSE"   => new TrueFalseQuestion(),
                        "ESSAY"        => new EssayQuestion(),
                        "SHORT_ANSWER" => new ShortAnswerQuestion(),
                        _              => new SingleChoiceQuestion()
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

                    if ((type == "SINGLE" || type == "MATCHING" || type == "ORDERING") && q is SingleChoiceQuestion sq)
                    {
                        sq.Options = item.Options;
                        // Bảo toàn chỉ mục số chuẩn 1, 2, 3, 4 đồng bộ tuyệt đối với Payload.correctAnswer
                        sq.CorrectOption = !string.IsNullOrWhiteSpace(item.SuggestedAnswer) ? item.SuggestedAnswer.Trim() : "1";
                        var optPayload = item.Options.Select((o, idx) => new { id = (idx + 1).ToString(), text = o }).ToArray();
                        sq.Payload = JsonSerializer.SerializeToDocument(new
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
                            contributionStatus = item.ContributionStatus
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
                            contributionStatus = item.ContributionStatus
                        });
                    }
                    else if (type == "MULTI" && q is MultiChoiceQuestion mq)
                    {
                        mq.Options = item.Options;
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
                            contributionStatus = item.ContributionStatus
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
                            contributionStatus = item.ContributionStatus
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
                            contributionStatus = item.ContributionStatus
                        });
                    }

                    toSave.Add(q);
                }

                _db.Questions.AddRange(toSave);
                await _db.SaveChangesAsync();
                return Ok(new { success = true, count = toSave.Count });
            }
            catch (Exception ex)
            {
                // [Kỳ tích Tự chữa lành Runtime] Nếu gặp lỗi thiếu cột DepthLevel (42703), tự động bổ sung cột vào PostgreSQL và thử lại ngay lập tức!
                if (ex.Message.Contains("42703") || ex.Message.Contains("DepthLevel") || ex.InnerException?.Message.Contains("42703") == true || ex.InnerException?.Message.Contains("DepthLevel") == true)
                {
                    try
                    {
                        await _db.Database.ExecuteSqlRawAsync(@"
                            ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""DepthLevel"" integer NOT NULL DEFAULT 0;
                            ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""MaterializedPath"" text NULL;
                            ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""Scope"" text NOT NULL DEFAULT 'COMMUNITY';
                            ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""DomainCode"" text NOT NULL DEFAULT 'GENERAL';
                            ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""DisplayOrder"" integer NOT NULL DEFAULT 0;
                            ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""QuestionCountCached"" integer NOT NULL DEFAULT 0;
                            ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""Urn"" text NULL;
                            ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""ParentId"" uuid NULL;
                            ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""AllowedTenantIds"" text[] NOT NULL DEFAULT '{}';
                            ALTER TABLE ""BankTopics"" ADD COLUMN IF NOT EXISTS ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
                            
                            ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""ContextId"" uuid NULL;
                            ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""DomainCode"" text NOT NULL DEFAULT 'EDUCATION';
                            ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""IsCritical"" boolean NOT NULL DEFAULT false;
                            ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""SubCategory"" text NULL;
                            ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""Tags"" text[] NOT NULL DEFAULT '{}';
                            ALTER TABLE ""Questions"" ADD COLUMN IF NOT EXISTS ""TenantId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
                        ");

                        // Xóa các entity bị lỗi khỏi ChangeTracker trước khi thử lại
                        _db.ChangeTracker.Clear();

                        // Thử lại lần 2 sau khi DB đã được tự động chữa lành cột
                        return await ConfirmImportQuestionsDocx(items);
                    }
                    catch (Exception retryEx)
                    {
                        return StatusCode(500, new { message = $"Lỗi lưu câu hỏi (sau khi tự chữa lành): {retryEx.Message}" });
                    }
                }

                return StatusCode(500, new { message = $"Lỗi lưu câu hỏi: {ex.Message}" });
            }
        }

        // ── GET api/quiz/contexts ──────────────────────────────────────────────
        [HttpGet("contexts")]
        public async Task<IActionResult> GetContexts()
        {
            try
            {
                var contexts = await _db.QuestionContexts
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new
                    {
                        c.Id,
                        c.Title,
                        c.Content,
                        c.ContentType,
                        c.ContentHash,
                        c.CreatedAt,
                        QuestionCount = _db.Questions.Count(q => q.ContextId == c.Id)
                    })
                    .ToListAsync();

                return Ok(contexts);
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

                var questions = await _db.Questions
                    .Where(q => dto.QuestionIds.Contains(q.Id))
                    .ToListAsync();

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

                var questions = await _db.Questions
                    .Where(q => dto.QuestionIds.Contains(q.Id))
                    .ToListAsync();

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

                var questions = await _db.Questions
                    .Where(q => dto.QuestionIds.Contains(q.Id))
                    .ToListAsync();

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

                foreach (var group in grouped)
                {
                    var topicCode = group.Key;
                    var topic = await _db.BankTopics.FirstOrDefaultAsync(t => t.Code == topicCode);
                    var topicName = topic?.Name ?? topicCode;

                    var sampleContents = group.Select(q => q.Content).Take(20).ToList();
                    var sampleRefs = group.Select(q => q.Explanation).Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e!).Take(20).ToList();

                    var taxonomy = UniversalCoordinateTaxonomyEngine.SniffCoordinates(
                        topicName,
                        topicCode,
                        new[] { topicName, topic?.Description ?? "" },
                        sampleContents,
                        sampleRefs);

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
                    sq.Payload = JsonSerializer.SerializeToDocument(new
                    {
                        options       = optPayload,
                        correctAnswer = correctVal,
                        explanation   = request.AiExplanation ?? ""
                    });
                }
                else if (q is EssayQuestion eq)
                {
                    eq.GradingRubric = request.CorrectAnswer;
                    eq.Payload = JsonSerializer.SerializeToDocument(new
                    {
                        rubric      = request.CorrectAnswer,
                        explanation = request.AiExplanation ?? ""
                    });
                }
                else
                {
                    q.CorrectOption = request.CorrectAnswer;
                    q.Payload = JsonSerializer.SerializeToDocument(new
                    {
                        correctAnswer = request.CorrectAnswer,
                        explanation   = request.AiExplanation ?? ""
                    });
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
            var question = dto.QuestionType.ToUpper() switch
            {
                "SINGLE"       => (QuestionBase)new SingleChoiceQuestion(),
                "MULTI"        => new MultiChoiceQuestion(),
                "TRUE_FALSE"   => new TrueFalseQuestion(),
                "SHORT_ANSWER" => new ShortAnswerQuestion(),
                "FILL_BLANK"   => new FillBlankQuestion(),
                "ORDERING"     => new OrderingQuestion(),
                "MATCHING"     => new MatchingQuestion(),
                "ESSAY"        => new EssayQuestion(),
                _              => new SingleChoiceQuestion()
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

    public class ImportUrlRequest
    {
        public string Url { get; set; } = string.Empty;
        public bool UseAi { get; set; } = false;
    }
}
