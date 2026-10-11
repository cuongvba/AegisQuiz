using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [AegisQuiz Universal Document Transformation API — DTaaS]
    /// Cung cấp dịch vụ chuyển đổi đề thi từ Word (.docx) / PDF (.pdf) sang bảng tính Excel (.xlsx).
    /// Hỗ trợ chuẩn B2B RESTful API cho các hệ thống LMS, trường học và đối tác phần mềm giáo dục.
    /// </summary>
    [ApiController]
    [Route("api/v1/convert")]
    public class ConvertController : ControllerBase
    {
        private readonly IDocxParserService _docxParser;
        private readonly IPdfExtractorService _pdfExtractor;
        private readonly IUniversalExcelExportService _excelExportService;
        private readonly IUniversalExamTranspilerService _transpilerService;
        private readonly IDynamicDomainContextService _domainContextService;
        private readonly ITenantContext _tenantContext;
        private readonly Microsoft.Extensions.Logging.ILogger<ConvertController> _logger;

        public ConvertController(
            IDocxParserService docxParser,
            IPdfExtractorService pdfExtractor,
            IUniversalExcelExportService excelExportService,
            IUniversalExamTranspilerService transpilerService,
            IDynamicDomainContextService domainContextService,
            ITenantContext tenantContext,
            Microsoft.Extensions.Logging.ILogger<ConvertController> logger)
        {
            _docxParser = docxParser;
            _pdfExtractor = pdfExtractor;
            _excelExportService = excelExportService;
            _transpilerService = transpilerService;
            _domainContextService = domainContextService;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách các mẫu xuất bản bảng tính Excel sẵn có trong hệ thống (Native Isomorphic, GDPT 2025, Azota...).
        /// </summary>
        [HttpGet("templates")]
        public IActionResult GetAvailableTemplates()
        {
            var templates = _excelExportService.GetAvailableTemplates();
            return Ok(new
            {
                success = true,
                totalTemplates = templates.Count,
                data = templates
            });
        }

        /// <summary>
        /// Chuyển đổi trực tiếp tài liệu đề thi Word (.docx) hoặc PDF (.pdf) sang file Excel (.xlsx).
        /// </summary>
        [HttpPost("file-to-excel")]
        [RequestSizeLimit(35 * 1024 * 1024)] // 35 MB
        public async Task<IActionResult> ConvertFileToExcel(
            IFormFile file,
            [FromForm] string templateCode = "NATIVE_AEGIS",
            [FromForm] IFormFile? customTemplateFile = null,
            [FromForm] string? examTitle = null,
            [FromForm] string? issuingOrg = null,
            [FromForm] string? targetLevel = null,
            [FromForm] string? domainCode = null,
            [FromForm] bool includeExplanation = true,
            [FromForm] bool includeImages = true)
        {
            var sw = Stopwatch.StartNew();

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "Vui lòng đính kèm tệp .docx hoặc .pdf để chuyển đổi." });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".docx" && extension != ".pdf")
            {
                return BadRequest(new { success = false, message = "Định dạng không được hỗ trợ. Chỉ chấp nhận tệp .docx hoặc .pdf." });
            }

            try
            {
                var tenantId = _tenantContext.CurrentTenantId;
                var dynamicContext = await _domainContextService.BuildContextAsync(tenantId);

                List<DocxImportPreviewDto> questions;
                using (var stream = file.OpenReadStream())
                {
                    if (extension == ".docx")
                    {
                        questions = _docxParser.ParseDocxFile(stream, dynamicContext);
                    }
                    else
                    {
                        questions = _pdfExtractor.ParsePdfQuestions(stream);
                    }
                }

                if (questions == null || questions.Count == 0)
                {
                    return UnprocessableEntity(new
                    {
                        success = false,
                        message = "Không tìm thấy cấu trúc câu hỏi hợp lệ trong tài liệu đầu vào."
                    });
                }

                var options = new ExcelExportOptionsDto
                {
                    ExamTitle = !string.IsNullOrWhiteSpace(examTitle) ? examTitle : Path.GetFileNameWithoutExtension(file.FileName),
                    IssuingOrg = issuingOrg,
                    TargetLevel = targetLevel,
                    DomainCode = domainCode,
                    IncludeExplanation = includeExplanation,
                    IncludeImages = includeImages
                };

                if (customTemplateFile != null && customTemplateFile.Length > 0)
                {
                    using var tStream = new MemoryStream();
                    await customTemplateFile.CopyToAsync(tStream);
                    options.CustomTemplateBytes = tStream.ToArray();
                    templateCode = "CUSTOM_BYOT";
                }

                byte[] excelBytes = _excelExportService.Export(questions, templateCode, options);

                sw.Stop();

                string safeBaseName = Path.GetFileNameWithoutExtension(file.FileName);
                string downloadFileName = $"{safeBaseName}_{templateCode.ToLowerInvariant()}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                Response.Headers.Append("X-Total-Questions", questions.Count.ToString());
                Response.Headers.Append("X-Export-Template", templateCode);
                Response.Headers.Append("X-Processing-Time-Ms", sw.ElapsedMilliseconds.ToString());

                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    downloadFileName);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "[ConvertController] Lỗi xử lý chuyển đổi tài liệu file-to-excel: {Msg}", ex.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Lỗi xử lý chuyển đổi tài liệu: {ex.Message}",
                    durationMs = sw.ElapsedMilliseconds
                });
            }
        }

        /// <summary>
        /// Xuất bản trực tiếp danh sách câu hỏi xem trước (Preview AST) sang file Excel (.xlsx).
        /// Tiện ích phục vụ người dùng bấm xuất Excel ngay trên giao diện xem trước.
        /// </summary>
        [HttpPost("preview-to-excel")]
        public IActionResult ConvertPreviewToExcel(
            [FromBody] List<DocxImportPreviewDto> questions,
            [FromQuery] string templateCode = "NATIVE_AEGIS",
            [FromQuery] string? examTitle = null,
            [FromQuery] bool includeExplanation = true,
            [FromQuery] bool includeImages = true)
        {
            var sw = Stopwatch.StartNew();

            if (questions == null || questions.Count == 0)
            {
                return BadRequest(new { success = false, message = "Danh sách câu hỏi không được rỗng." });
            }

            try
            {
                var options = new ExcelExportOptionsDto
                {
                    ExamTitle = !string.IsNullOrWhiteSpace(examTitle) ? examTitle : "Exported_Quiz_Exam",
                    IncludeExplanation = includeExplanation,
                    IncludeImages = includeImages
                };

                byte[] excelBytes = _excelExportService.Export(questions, templateCode, options);

                sw.Stop();
                string downloadFileName = BuildSafeDownloadFileName(options.ExamTitle, templateCode);

                Response.Headers.Append("X-Total-Questions", questions.Count.ToString());
                Response.Headers.Append("X-Export-Template", templateCode);
                Response.Headers.Append("X-Processing-Time-Ms", sw.ElapsedMilliseconds.ToString());
                Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{downloadFileName}\"; filename*=UTF-8''{Uri.EscapeDataString(downloadFileName)}");

                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    downloadFileName);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "[ConvertController] Lỗi xuất bản Excel từ preview: {Msg}", ex.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Lỗi xuất bản Excel từ preview: {ex.Message}",
                    durationMs = sw.ElapsedMilliseconds
                });
            }
        }

        /// <summary>
        /// [BYOT] Xuất bản danh sách câu hỏi xem trước theo Template Mẫu (.xlsx) do người dùng tải lên.
        /// Tự động đọc header row, map cột tương ứng và điền nội dung bảo toàn formatting mẫu.
        /// </summary>
        [HttpPost("preview-to-custom-template")]
        [RequestSizeLimit(35 * 1024 * 1024)]
        public async Task<IActionResult> ConvertPreviewToCustomTemplateExcel(
            IFormFile templateFile,
            [FromForm] string questionsJson,
            [FromForm] string? examTitle = null,
            [FromForm] bool includeExplanation = true,
            [FromForm] bool includeImages = true)
        {
            var sw = Stopwatch.StartNew();

            if (templateFile == null || templateFile.Length == 0)
            {
                return BadRequest(new { success = false, message = "Vui lòng đính kèm tệp mẫu Excel (.xlsx) để áp dụng." });
            }

            if (string.IsNullOrWhiteSpace(questionsJson))
            {
                return BadRequest(new { success = false, message = "Dữ liệu câu hỏi questionsJson không được rỗng." });
            }

            try
            {
                var questions = System.Text.Json.JsonSerializer.Deserialize<List<DocxImportPreviewDto>>(
                    questionsJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (questions == null || questions.Count == 0)
                {
                    return BadRequest(new { success = false, message = "Danh sách câu hỏi không có dữ liệu hợp lệ." });
                }

                using var tStream = new MemoryStream();
                await templateFile.CopyToAsync(tStream);

                string baseName = Path.GetFileNameWithoutExtension(templateFile.FileName);
                string effectiveTitle = !string.IsNullOrWhiteSpace(examTitle) ? examTitle : baseName;

                var options = new ExcelExportOptionsDto
                {
                    ExamTitle = effectiveTitle,
                    IncludeExplanation = includeExplanation,
                    IncludeImages = includeImages,
                    CustomTemplateBytes = tStream.ToArray()
                };

                byte[] excelBytes = _excelExportService.Export(questions, "CUSTOM_BYOT", options);

                sw.Stop();
                string downloadFileName = BuildSafeDownloadFileName(effectiveTitle, "CUSTOM", baseName);

                Response.Headers.Append("X-Total-Questions", questions.Count.ToString());
                Response.Headers.Append("X-Export-Template", "CUSTOM_BYOT");
                Response.Headers.Append("X-Processing-Time-Ms", sw.ElapsedMilliseconds.ToString());
                Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{downloadFileName}\"; filename*=UTF-8''{Uri.EscapeDataString(downloadFileName)}");

                return File(
                    excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    downloadFileName);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "[ConvertController] Lỗi xuất bản theo template tùy biến: {Msg}", ex.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Lỗi xuất bản Excel theo template tùy biến: {ex.Message}",
                    durationMs = sw.ElapsedMilliseconds
                });
            }
        }

        #region Universal Exam Transpiler Endpoints (Moodle, Canvas QTI, GIFT)

        /// <summary>
        /// Lấy danh sách các chuẩn xuất bản quốc tế (Moodle XML, Canvas QTI Package, Moodle GIFT...) đang sẵn sàng.
        /// </summary>
        [HttpGet("transpiler-formats")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public IActionResult GetAvailableTranspilerFormats()
        {
            var formats = _transpilerService.GetAvailableFormats();
            return Ok(new
            {
                success = true,
                totalFormats = formats.Count,
                data = formats
            });
        }

        /// <summary>
        /// [Universal Exam Transpiler] Chuyển dịch trực tiếp danh sách câu hỏi xem trước (Preview AST) sang chuẩn quốc tế.
        /// Hỗ trợ: MOODLE_XML, QTI_PACKAGE, MOODLE_GIFT.
        /// </summary>
        [HttpPost("preview-transpile")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public IActionResult ConvertPreviewToStandard(
            [FromBody] List<DocxImportPreviewDto> questions,
            [FromQuery] string formatCode = "MOODLE_XML",
            [FromQuery] string? examTitle = null,
            [FromQuery] string? categoryName = null,
            [FromQuery] bool includeExplanation = true,
            [FromQuery] bool includeImages = true)
        {
            var sw = Stopwatch.StartNew();

            if (questions == null || questions.Count == 0)
            {
                return BadRequest(new { success = false, message = "Danh sách câu hỏi không được rỗng." });
            }

            try
            {
                var options = new TranspileOptionsDto
                {
                    ExamTitle = !string.IsNullOrWhiteSpace(examTitle) ? examTitle : "AegisQuiz_Transpiled_Exam",
                    CategoryName = categoryName,
                    IncludeExplanation = includeExplanation,
                    IncludeImages = includeImages
                };

                byte[] resultBytes = _transpilerService.Transpile(questions, formatCode, options);

                var availableFormats = _transpilerService.GetAvailableFormats();
                var formatInfo = availableFormats.FirstOrDefault(f => string.Equals(f.FormatCode, formatCode, StringComparison.OrdinalIgnoreCase))
                                 ?? availableFormats.FirstOrDefault(f => f.FormatCode == "MOODLE_XML");

                string extension = formatInfo?.FileExtension ?? ".xml";
                string mimeType = formatInfo?.MimeType ?? "application/xml";

                sw.Stop();
                string safeTitle = Regex.Replace(!string.IsNullOrWhiteSpace(examTitle) ? examTitle : "AegisQuiz_Exam", @"[\/\\:*?""<>|\x00-\x1F]+", "_");
                string downloadFileName = $"{safeTitle}_{formatCode.ToLowerInvariant()}_{DateTime.Now:yyyyMMddHHmmss}{extension}";

                Response.Headers.Append("X-Total-Questions", questions.Count.ToString());
                Response.Headers.Append("X-Transpiler-Format", formatCode);
                Response.Headers.Append("X-Processing-Time-Ms", sw.ElapsedMilliseconds.ToString());
                Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{downloadFileName}\"; filename*=UTF-8''{Uri.EscapeDataString(downloadFileName)}");

                return File(resultBytes, mimeType, downloadFileName);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "[ConvertController] Lỗi chuyển dịch sang chuẩn '{Format}': {Msg}", formatCode, ex.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Lỗi chuyển dịch chuẩn quốc tế ({formatCode}): {ex.Message}",
                    durationMs = sw.ElapsedMilliseconds
                });
            }
        }

        /// <summary>
        /// [Universal Exam Transpiler] Chuyển đổi trực tiếp tài liệu Word/PDF sang chuẩn quốc tế (Moodle XML, QTI Zip, GIFT) trong 1 bước.
        /// </summary>
        [HttpPost("file-transpile")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [RequestSizeLimit(35 * 1024 * 1024)] // 35 MB
        public async Task<IActionResult> ConvertFileToStandard(
            IFormFile file,
            [FromForm] string formatCode = "MOODLE_XML",
            [FromForm] string? examTitle = null,
            [FromForm] string? categoryName = null,
            [FromForm] bool includeExplanation = true,
            [FromForm] bool includeImages = true)
        {
            var sw = Stopwatch.StartNew();

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "Vui lòng đính kèm tệp .docx hoặc .pdf." });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".docx" && extension != ".pdf")
            {
                return BadRequest(new { success = false, message = "Định dạng không được hỗ trợ. Chỉ chấp nhận tệp .docx hoặc .pdf." });
            }

            try
            {
                var tenantId = _tenantContext.CurrentTenantId;
                var dynamicContext = await _domainContextService.BuildContextAsync(tenantId);

                List<DocxImportPreviewDto> questions;
                using (var stream = file.OpenReadStream())
                {
                    if (extension == ".docx")
                    {
                        questions = _docxParser.ParseDocxFile(stream, dynamicContext);
                    }
                    else
                    {
                        questions = _pdfExtractor.ParsePdfQuestions(stream);
                    }
                }

                if (questions == null || questions.Count == 0)
                {
                    return UnprocessableEntity(new
                    {
                        success = false,
                        message = "Không tìm thấy cấu trúc câu hỏi hợp lệ trong tài liệu đầu vào."
                    });
                }

                var options = new TranspileOptionsDto
                {
                    ExamTitle = !string.IsNullOrWhiteSpace(examTitle) ? examTitle : Path.GetFileNameWithoutExtension(file.FileName),
                    CategoryName = categoryName,
                    IncludeExplanation = includeExplanation,
                    IncludeImages = includeImages
                };

                byte[] resultBytes = _transpilerService.Transpile(questions, formatCode, options);

                var availableFormats = _transpilerService.GetAvailableFormats();
                var formatInfo = availableFormats.FirstOrDefault(f => string.Equals(f.FormatCode, formatCode, StringComparison.OrdinalIgnoreCase))
                                 ?? availableFormats.FirstOrDefault(f => f.FormatCode == "MOODLE_XML");

                string targetExt = formatInfo?.FileExtension ?? ".xml";
                string mimeType = formatInfo?.MimeType ?? "application/xml";

                sw.Stop();
                string safeBaseName = Path.GetFileNameWithoutExtension(file.FileName);
                string downloadFileName = $"{safeBaseName}_{formatCode.ToLowerInvariant()}_{DateTime.Now:yyyyMMddHHmmss}{targetExt}";

                Response.Headers.Append("X-Total-Questions", questions.Count.ToString());
                Response.Headers.Append("X-Transpiler-Format", formatCode);
                Response.Headers.Append("X-Processing-Time-Ms", sw.ElapsedMilliseconds.ToString());
                Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{downloadFileName}\"; filename*=UTF-8''{Uri.EscapeDataString(downloadFileName)}");

                return File(resultBytes, mimeType, downloadFileName);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "[ConvertController] Lỗi chuyển đổi file-transpile ({Format}): {Msg}", formatCode, ex.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Lỗi chuyển đổi file-transpile ({formatCode}): {ex.Message}",
                    durationMs = sw.ElapsedMilliseconds
                });
            }
        }

        #endregion

        private static string BuildSafeDownloadFileName(string? title, string templateCode, string? suffix = null)
        {
            string raw = !string.IsNullOrWhiteSpace(title) ? title.Trim() : "De_Thi_AegisQuiz";
            // Loại bỏ ký tự cấm của hệ điều hành
            string clean = Regex.Replace(raw, @"[\/\\:*?""<>|\x00-\x1F]+", "_");
            // Rút gọn khoảng trắng hoặc gạch dưới liên tiếp
            clean = Regex.Replace(clean, @"[\s_]+", "_").Trim('_');
            if (string.IsNullOrWhiteSpace(clean)) clean = "De_Thi_AegisQuiz";
            if (clean.Length > 50) clean = clean.Substring(0, 50).TrimEnd('_');

            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return string.IsNullOrWhiteSpace(suffix)
                ? $"{clean}_{templateCode.ToUpperInvariant()}_{timeStamp}.xlsx"
                : $"{clean}_{suffix}_{timeStamp}.xlsx";
        }
    }
}
