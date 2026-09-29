using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [Enterprise Clean Architecture] MathDictionaryController
    /// Quản trị Từ điển Toán học Động (Universal Dynamic Math Dictionary):
    /// - GET /api/quiz/math-dictionary: Xem toàn bộ từ điển và thống kê các token đang được nạp.
    /// - POST /api/quiz/math-dictionary/reload: Nạp lại từ điển từ file hoặc payload JSON (Hot-Reload 0ms Downtime).
    /// - POST /api/quiz/math-dictionary/symbol: Đăng ký / cập nhật ký tự Glyph tức thì.
    /// - POST /api/quiz/math-dictionary/omml-token: Đăng ký / cập nhật ánh xạ OMML token tức thì.
    /// </summary>
    [ApiController]
    [Route("api/quiz/math-dictionary")]
    public class MathDictionaryController : ControllerBase
    {
        private readonly IDynamicMathDictionaryService _mathDictionary;

        public MathDictionaryController(IDynamicMathDictionaryService mathDictionary)
        {
            _mathDictionary = mathDictionary;
        }

        /// <summary>
        /// Lấy toàn bộ đăng bạ từ điển toán học động hiện tại trong bộ nhớ
        /// </summary>
        [HttpGet]
        public IActionResult GetDictionary()
        {
            var registry = _mathDictionary.GetCurrentRegistry();
            return Ok(new
            {
                success = true,
                version = registry.Version,
                lastUpdated = registry.LastUpdated,
                totalGlyphs = registry.SymbolGlyphs.Count,
                totalGreek = registry.GreekLetters.Count,
                totalOmmlTokens = registry.OmmlTokens.Count,
                totalAccents = registry.AccentMappings.Count,
                totalInlineRules = registry.InlineNormalizations.Count,
                totalStructuralRules = registry.StructuralSimplificationRules.Count,
                data = registry
            });
        }

        /// <summary>
        /// Nạp lại từ điển từ file hệ thống hoặc từ chuỗi JSON gửi lên (Zero Downtime)
        /// </summary>
        [HttpPost("reload")]
        public async Task<IActionResult> ReloadDictionary([FromBody] ReloadDictionaryRequest? request)
        {
            try
            {
                string json;
                if (!string.IsNullOrWhiteSpace(request?.JsonContent))
                {
                    json = request.JsonContent;
                }
                else
                {
                    // Đọc từ file Config mặc định
                    string configPath = Path.Combine(AppContext.BaseDirectory, "Config", "math_dictionary_registry.json");
                    if (!System.IO.File.Exists(configPath))
                    {
                        configPath = Path.Combine(Directory.GetCurrentDirectory(), "Config", "math_dictionary_registry.json");
                    }
                    if (!System.IO.File.Exists(configPath))
                    {
                        return NotFound(new { success = false, message = $"Registry file not found at {configPath}" });
                    }
                    json = await System.IO.File.ReadAllTextAsync(configPath);
                }

                bool success = _mathDictionary.HotReload(json);
                if (!success)
                {
                    return BadRequest(new { success = false, message = "Failed to parse or apply dictionary JSON." });
                }

                var registry = _mathDictionary.GetCurrentRegistry();
                return Ok(new
                {
                    success = true,
                    message = "Dynamic math dictionary hot-reloaded successfully.",
                    version = registry.Version,
                    lastUpdated = registry.LastUpdated,
                    totalGlyphs = registry.SymbolGlyphs.Count,
                    totalOmmlTokens = registry.OmmlTokens.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Thêm hoặc cập nhật một Glyph ký hiệu font MathType trực tiếp
        /// </summary>
        [HttpPost("symbol")]
        public IActionResult UpsertSymbol([FromBody] SymbolGlyphEntryDto entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.CharHex) || string.IsNullOrWhiteSpace(entry.Replacement))
            {
                return BadRequest(new { success = false, message = "CharHex and Replacement are required." });
            }

            _mathDictionary.UpsertSymbolGlyph(entry);
            return Ok(new
            {
                success = true,
                message = $"Successfully registered/updated symbol '{entry.CharHex}' with replacement '{entry.Replacement}'."
            });
        }

        /// <summary>
        /// Thêm hoặc cập nhật một ánh xạ OMML token trực tiếp
        /// </summary>
        [HttpPost("omml-token")]
        public IActionResult UpsertOmmlToken([FromBody] UpsertOmmlTokenRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Latex))
            {
                return BadRequest(new { success = false, message = "Token and Latex are required." });
            }

            _mathDictionary.UpsertOmmlToken(request.Token, request.Latex);
            return Ok(new
            {
                success = true,
                message = $"Successfully registered/updated OMML token '{request.Token}' -> '{request.Latex}'."
            });
        }

        /// <summary>
        /// [P2 Declarative Structural Rule]: Thêm hoặc cập nhật một quy tắc cấu trúc toán học động
        /// </summary>
        [HttpPost("structural-rule")]
        public IActionResult UpsertStructuralRule([FromBody] MathStructuralRuleDto rule)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.Id))
            {
                return BadRequest(new { success = false, message = "Rule Id and specification are required." });
            }

            _mathDictionary.UpsertStructuralRule(rule);
            return Ok(new
            {
                success = true,
                message = $"Successfully registered/updated structural rule '{rule.Id}' ({rule.Name}).",
                ruleId = rule.Id,
                totalRules = _mathDictionary.GetCurrentRegistry().StructuralSimplificationRules.Count
            });
        }

        /// <summary>
        /// [P2 Declarative Structural Rule]: Xóa quy tắc cấu trúc toán học theo mã ID
        /// </summary>
        [HttpDelete("structural-rule/{id}")]
        public IActionResult DeleteStructuralRule(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { success = false, message = "Rule Id is required." });
            }

            bool deleted = _mathDictionary.DeleteStructuralRule(id);
            if (!deleted)
            {
                return NotFound(new { success = false, message = $"Structural rule '{id}' not found." });
            }

            return Ok(new
            {
                success = true,
                message = $"Successfully deleted structural rule '{id}'.",
                totalRules = _mathDictionary.GetCurrentRegistry().StructuralSimplificationRules.Count
            });
        }

        /// <summary>
        /// [P2 Declarative Structural Rule]: Bật/Tắt quy tắc cấu trúc toán học tức thì (Hot-Toggle 0ms)
        /// </summary>
        [HttpPatch("structural-rule/{id}/toggle")]
        public IActionResult ToggleStructuralRule(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { success = false, message = "Rule Id is required." });
            }

            var registry = _mathDictionary.GetCurrentRegistry();
            var existing = registry.StructuralSimplificationRules.Find(r => string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                return NotFound(new { success = false, message = $"Structural rule '{id}' not found." });
            }

            existing.Enabled = !existing.Enabled;
            _mathDictionary.UpsertStructuralRule(existing);

            return Ok(new
            {
                success = true,
                message = $"Structural rule '{id}' is now {(existing.Enabled ? "ENABLED" : "DISABLED")}.",
                enabled = existing.Enabled
            });
        }

        /// <summary>
        /// Lấy toàn bộ cấu hình typography và tham số động của Math Engine
        /// </summary>
        [HttpGet("settings")]
        public IActionResult GetSettings()
        {
            var settings = _mathDictionary.GetEngineSettings();
            return Ok(new
            {
                success = true,
                data = settings
            });
        }

        /// <summary>
        /// Cập nhật cấu hình động cho Math Engine
        /// </summary>
        [HttpPut("settings")]
        public IActionResult UpdateSettings([FromBody] MathEngineTypographyDto settings)
        {
            if (settings == null)
            {
                return BadRequest(new { success = false, message = "Settings payload is required." });
            }

            _mathDictionary.UpdateEngineSettings(settings);
            return Ok(new
            {
                success = true,
                message = "Math engine typography settings updated successfully.",
                data = _mathDictionary.GetEngineSettings()
            });
        }

        /// <summary>
        /// Lấy cấu hình profile toán học theo môn học chuyên biệt (Toán, Lý, Hóa, Sinh)
        /// </summary>
        [HttpGet("settings/subject/{subjectCode}")]
        public IActionResult GetSubjectProfile(string subjectCode)
        {
            var profile = _mathDictionary.GetSubjectProfile(subjectCode);
            return Ok(new
            {
                success = true,
                data = profile
            });
        }

        /// <summary>
        /// [Chẩn đoán & Chuyển đổi Trực tiếp] Kiểm chứng công thức toán học WMF/SVG
        /// Giúp quản trị viên kiểm tra xem lỗi công thức là do khâu tạo mã (WMF/SVG engine) hay hiển thị
        /// </summary>
        [HttpPost("inspect-formula")]
        public IActionResult InspectFormula([FromBody] InspectFormulaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.RawInput))
            {
                return BadRequest(new { success = false, message = "RawInput is required." });
            }

            string raw = request.RawInput.Trim();
            string inputType = "unknown";
            string rawSvg = string.Empty;
            string cleanedSvg = string.Empty;
            string cleanedDataUri = string.Empty;
            int polygonCount = 0;
            int lineCount = 0;
            int textCount = 0;
            int pathCount = 0;
            string extractedTexts = string.Empty;
            string viewBox = string.Empty;
            bool hasBlackPillDefect = false;
            string diagnosis = string.Empty;

            try
            {
                if (raw.StartsWith("data:image/x-wmf;base64,", StringComparison.OrdinalIgnoreCase) ||
                    raw.StartsWith("data:image/wmf;base64,", StringComparison.OrdinalIgnoreCase))
                {
                    inputType = "wmf_base64";
                    int comma = raw.IndexOf(',');
                    byte[] wmfBytes = Convert.FromBase64String(raw.Substring(comma + 1));
                    cleanedDataUri = AegisQuiz.Infrastructure.Services.MathTypeWmfEngine.ConvertMediaToDataUri("diagnostic.wmf", wmfBytes);
                    if (cleanedDataUri.StartsWith("data:image/svg+xml;base64,"))
                    {
                        int c2 = cleanedDataUri.IndexOf(',');
                        byte[] svgB = Convert.FromBase64String(cleanedDataUri.Substring(c2 + 1));
                        cleanedSvg = System.Text.Encoding.UTF8.GetString(svgB);
                        rawSvg = cleanedSvg;
                    }
                    diagnosis = "Đầu vào là nhị phân WMF MathType. Đã dịch trực tiếp qua MathTypeWmfEngine sang Vector SVG.";
                }
                else if (raw.StartsWith("data:image/svg+xml;base64,", StringComparison.OrdinalIgnoreCase) ||
                         raw.StartsWith("image/svg+xml;base64,", StringComparison.OrdinalIgnoreCase))
                {
                    inputType = "svg_base64";
                    int comma = raw.IndexOf(',');
                    byte[] svgBytes = Convert.FromBase64String(raw.Substring(comma + 1));
                    rawSvg = System.Text.Encoding.UTF8.GetString(svgBytes);
                    cleanedSvg = AegisQuiz.Infrastructure.Services.MathTypeWmfEngine.SanitizeMathTypeSvg(rawSvg);
                    cleanedDataUri = "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(cleanedSvg));
                }
                else if (raw.StartsWith("<svg", StringComparison.OrdinalIgnoreCase))
                {
                    inputType = "raw_svg_xml";
                    rawSvg = raw;
                    cleanedSvg = AegisQuiz.Infrastructure.Services.MathTypeWmfEngine.SanitizeMathTypeSvg(rawSvg);
                    cleanedDataUri = "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(cleanedSvg));
                }
                else
                {
                    inputType = "plain_math_text";
                    diagnosis = "Đầu vào là văn bản / ký tự LaTeX / OMML thông thường.";
                }

                if (!string.IsNullOrEmpty(rawSvg))
                {
                    polygonCount = System.Text.RegularExpressions.Regex.Matches(rawSvg, @"<polygon\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
                    lineCount = System.Text.RegularExpressions.Regex.Matches(rawSvg, @"<line\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
                    textCount = System.Text.RegularExpressions.Regex.Matches(rawSvg, @"<text\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
                    pathCount = System.Text.RegularExpressions.Regex.Matches(rawSvg, @"<path\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;

                    var vbMatch = System.Text.RegularExpressions.Regex.Match(rawSvg, @"viewBox=""([^""]+)""", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (vbMatch.Success) viewBox = vbMatch.Groups[1].Value;

                    var textMatches = System.Text.RegularExpressions.Regex.Matches(rawSvg, @"<text[^>]*>([\s\S]*?)</text>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    var sbT = new System.Text.StringBuilder();
                    foreach (System.Text.RegularExpressions.Match tm in textMatches)
                    {
                        sbT.Append(tm.Groups[1].Value).Append(" ");
                    }
                    extractedTexts = sbT.ToString().Trim();

                    if (polygonCount > 0)
                    {
                        hasBlackPillDefect = true;
                        diagnosis = $"Phát hiện {polygonCount} thẻ <polygon> màu đen đặc. LỖI THUỘC QUÁ TRÌNH TẠO MÃ (Engine dịch nhầm record WMF 0x0538/0x0626 thành đa giác tọa độ rác che phủ viewBox). Đã tự động lọc sạch triệt để.";
                    }
                    else
                    {
                        diagnosis = "Mã SVG chuẩn W3C, không chứa polygon rác gây ô đen. Công thức hiển thị sắc nét.";
                    }
                }

                return Ok(new
                {
                    success = true,
                    inputType,
                    hasBlackPillDefect,
                    polygonCount,
                    lineCount,
                    textCount,
                    pathCount,
                    viewBox,
                    extractedTexts,
                    rawSvg,
                    cleanedSvg,
                    cleanedDataUri,
                    diagnosis
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }

    public class InspectFormulaRequest
    {
        public string RawInput { get; set; } = string.Empty;
    }

    public class ReloadDictionaryRequest
    {
        public string? JsonContent { get; set; }
    }

    public class UpsertOmmlTokenRequest
    {
        public string Token { get; set; } = string.Empty;
        public string Latex { get; set; } = string.Empty;
    }
}
