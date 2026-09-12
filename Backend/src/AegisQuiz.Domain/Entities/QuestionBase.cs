using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace AegisQuiz.Domain.Entities
{
    // [Universal Question Model] — học từ quiz-service
    // Dùng JsonDocument Payload để lưu trữ đa dạng loại câu hỏi trong 1 bảng JSONB.
    // Không cần thêm column mỗi khi có loại câu hỏi mới.
    public abstract class QuestionBase : ITenantEntity
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public int Difficulty { get; set; }
        public int DurationSeconds { get; set; }
        public string CategoryCode { get; set; } = string.Empty;

        // Options array and correct option shared for all question types
        public List<string>? Options { get; set; } = new();
        public string? CorrectOption { get; set; } = string.Empty;

        // Content & Option formats: "text", "image", "audio", "video"
        public string ContentType { get; set; } = "text";
        public string OptionType { get; set; } = "text";

        // [Universal Model] Toàn bộ cấu trúc câu hỏi (options, correctAnswer, rubric...)
        // được lưu trong Payload dưới dạng JSONB — linh hoạt với mọi loại câu hỏi.
        public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");

        // [Phase B — Multi-Tenancy] Câu hỏi thuộc về Tenant nào.
        // Guid.Empty = câu hỏi dùng chung (system-wide), chỉ SystemAdmin mới tạo được.
        public Guid TenantId { get; set; } = Guid.Empty;

        // Audit trail
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        // Discriminator property for JSON serialization & EF Core TPH mapping
        [System.Text.Json.Serialization.JsonPropertyName("questionType")]
        public string QuestionType { get; set; } = "SINGLE";

        // [Phân hệ Sát hạch Giao thông & Điểm liệt]
        // IsCritical = true: Sai câu này bị trượt trực tiếp toàn bộ bài thi
        [System.Text.Json.Serialization.JsonPropertyName("isCritical")]
        public bool IsCritical { get; set; } = false;

        [System.Text.Json.Serialization.JsonPropertyName("subCategory")]
        public string? SubCategory { get; set; }

        /// <summary>
        /// [Universal Multi-Industry Taxonomy]
        /// Miền ngành nghề: EDUCATION, BANKING, HEALTHCARE, HSE, GOV_DRIVING, IT_SECURITY, GENERAL
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("domainCode")]
        public string DomainCode { get; set; } = "EDUCATION";

        /// <summary>
        /// [Smart Tags & Hashtags]
        /// Danh sách thẻ gắn cho câu hỏi (#totnghiep2025, #aml, #chuyen-amsterdam, #iso27001...)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        /// <summary>Cấp bậc / Trình độ / Khối lớp (e.g. "Lớp 12", "Chuyên viên RM", "Bác sĩ CKI")</summary>
        [System.Text.Json.Serialization.JsonPropertyName("targetLevel")]
        public string? TargetLevel
        {
            get => TryGetPayloadProperty(Payload, "targetLevel", out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;
        }

        /// <summary>Mục đích sát hạch / Loại kỳ thi (e.g. "Tốt nghiệp THPT", "Tuân thủ định kỳ AML", "Nâng ngạch")</summary>
        [System.Text.Json.Serialization.JsonPropertyName("assessmentPurpose")]
        public string? AssessmentPurpose
        {
            get => TryGetPayloadProperty(Payload, "assessmentPurpose", out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;
        }

        /// <summary>Cơ quan / Tổ chức ban hành (e.g. "Chuyên Amsterdam", "Vietcombank", "Bộ Y tế")</summary>
        [System.Text.Json.Serialization.JsonPropertyName("issuingOrg")]
        public string? IssuingOrg
        {
            get => TryGetPayloadProperty(Payload, "issuingOrg", out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;
        }

        /// <summary>Năm ban hành / Niên khóa (e.g. 2025)</summary>
        [System.Text.Json.Serialization.JsonPropertyName("benchmarkYear")]
        public int? BenchmarkYear
        {
            get
            {
                if (TryGetPayloadProperty(Payload, "benchmarkYear", out var el))
                {
                    if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var y)) return y;
                    if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var sy)) return sy;
                }
                return null;
            }
        }

        /// <summary>Chuẩn mực quy chiếu / Văn bản pháp quy (e.g. "GDPT 2018", "Thông tư 41", "Basel III")</summary>
        [System.Text.Json.Serialization.JsonPropertyName("benchmarkStandard")]
        public string? BenchmarkStandard
        {
            get => TryGetPayloadProperty(Payload, "benchmarkStandard", out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;
        }

        /// <summary>Giải thích chi tiết / Căn cứ pháp lý</summary>
        [System.Text.Json.Serialization.JsonPropertyName("explanation")]
        public string? Explanation
        {
            get => TryGetPayloadProperty(Payload, "explanation", out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;
        }

        /// <summary>
        /// [Universal Shared Context / Stimulus]
        /// Khóa ngoại liên kết tới Bài đọc hiểu / Đoạn ghi âm / Biểu đồ dùng chung
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("contextId")]
        public Guid? ContextId { get; set; }

        public virtual QuestionContext? Context { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("contextTitle")]
        public string? ContextTitle
        {
            get
            {
                if (Context != null) return Context.Title;
                if (TryGetPayloadProperty(Payload, "contextTitle", out var el) && el.ValueKind == JsonValueKind.String) return el.GetString();
                return null;
            }
        }

        [System.Text.Json.Serialization.JsonPropertyName("contextContent")]
        public string? ContextContent
        {
            get
            {
                if (Context != null) return Context.Content;
                if (TryGetPayloadProperty(Payload, "contextContent", out var el) && el.ValueKind == JsonValueKind.String) return el.GetString();
                return null;
            }
        }

        public abstract double EvaluateAnswer(string studentAnswerJson);

        // Helper: đọc JsonDocument an toàn
        protected static bool TryGetPayloadProperty(JsonDocument? payload, string key, out JsonElement value)
        {
            value = default;
            if (payload == null) return false;
            try
            {
                if (payload.RootElement.ValueKind != JsonValueKind.Object) return false;
                return payload.RootElement.TryGetProperty(key, out value)
                       && value.ValueKind != JsonValueKind.Null
                       && value.ValueKind != JsonValueKind.Undefined;
            }
            catch
            {
                return false;
            }
        }
    }

    // ─── Trắc nghiệm 1 đáp án ──────────────────────────────────────────────────
    // Payload schema: { "options": [{"id":"A","text":"..."}], "correctAnswer": "A", "explanation": "..." }
    public class SingleChoiceQuestion : QuestionBase
    {
        public SingleChoiceQuestion() => QuestionType = "SINGLE";

        public override double EvaluateAnswer(string studentAnswerJson)
        {
            // Ưu tiên đọc từ Payload (Universal Model)
            if (TryGetPayloadProperty(Payload, "correctAnswer", out var correctEl))
                return string.Equals(studentAnswerJson?.Trim(), correctEl.ToString()?.Trim(),
                    StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;

            // Fallback legacy: so sánh trực tiếp CorrectOption
            return string.Equals(studentAnswerJson, CorrectOption,
                StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
        }
    }

    // ─── Đúng / Sai ────────────────────────────────────────────────────────────
    // Payload schema: { "correctAnswer": "1" }  ("1"=TRUE, "2"=FALSE)
    public class TrueFalseQuestion : QuestionBase
    {
        public TrueFalseQuestion() => QuestionType = "TRUE_FALSE";

        public override double EvaluateAnswer(string studentAnswerJson)
        {
            if (TryGetPayloadProperty(Payload, "correctAnswer", out var correctEl))
                return string.Equals(studentAnswerJson?.Trim(), correctEl.ToString()?.Trim(),
                    StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
            return 0.0;
        }
    }

    // ─── Nhiều đáp án đúng ────────────────────────────────────────────────────
    // Payload schema: { "options": [...], "correctAnswers": ["A","C"] }
    // studentAnswerJson: JSON array hoặc comma-separated "A,C"
    public class MultiChoiceQuestion : QuestionBase
    {
        public MultiChoiceQuestion() => QuestionType = "MULTI";

        public override double EvaluateAnswer(string studentAnswerJson)
        {
            if (!TryGetPayloadProperty(Payload, "correctAnswers", out var correctEl)
                || correctEl.ValueKind != JsonValueKind.Array)
                return 0.0;

            var correct = correctEl.EnumerateArray()
                .Select(x => x.ToString().Trim())
                .OrderBy(x => x)
                .ToArray();

            string[] submitted;
            try
            {
                using var doc = JsonDocument.Parse(studentAnswerJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    submitted = doc.RootElement.EnumerateArray()
                        .Select(x => x.ToString().Trim()).OrderBy(x => x).ToArray();
                else
                    submitted = studentAnswerJson.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim()).OrderBy(x => x).ToArray();
            }
            catch
            {
                submitted = studentAnswerJson.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()).OrderBy(x => x).ToArray();
            }

            return correct.SequenceEqual(submitted) ? 1.0 : 0.0;
        }
    }

    // ─── Trả lời ngắn / Điền vào chỗ trống ────────────────────────────────────
    // Payload schema: { "correctAnswer": "đáp án chuẩn" }
    public class ShortAnswerQuestion : QuestionBase
    {
        public ShortAnswerQuestion() => QuestionType = "SHORT_ANSWER";

        public override double EvaluateAnswer(string studentAnswerJson)
        {
            if (TryGetPayloadProperty(Payload, "correctAnswer", out var correctEl))
                return string.Equals(studentAnswerJson?.Trim(), correctEl.ToString()?.Trim(),
                    StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
            return 0.0;
        }
    }

    public class FillBlankQuestion : QuestionBase
    {
        public FillBlankQuestion() => QuestionType = "FILL_BLANK";

        public override double EvaluateAnswer(string studentAnswerJson)
        {
            if (TryGetPayloadProperty(Payload, "correctAnswer", out var correctEl))
                return string.Equals(studentAnswerJson?.Trim(), correctEl.ToString()?.Trim(),
                    StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
            return 0.0;
        }
    }

    // ─── Sắp xếp thứ tự ───────────────────────────────────────────────────────
    // Payload schema: { "items": [...], "correctOrder": ["2","3","1"] }
    // studentAnswerJson: JSON array hoặc comma-separated
    public class OrderingQuestion : QuestionBase
    {
        public OrderingQuestion() => QuestionType = "ORDERING";

        public override double EvaluateAnswer(string studentAnswerJson)
        {
            if (!TryGetPayloadProperty(Payload, "correctOrder", out var correctEl)
                || correctEl.ValueKind != JsonValueKind.Array)
                return 0.0;

            var correct = correctEl.EnumerateArray().Select(x => x.ToString().Trim()).ToArray();

            string[] submitted;
            try
            {
                using var doc = JsonDocument.Parse(studentAnswerJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    submitted = doc.RootElement.EnumerateArray()
                        .Select(x => x.ToString().Trim()).ToArray();
                else
                    submitted = studentAnswerJson.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim()).ToArray();
            }
            catch
            {
                submitted = studentAnswerJson.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()).ToArray();
            }

            return correct.SequenceEqual(submitted) ? 1.0 : 0.0;
        }
    }

    // ─── Nối cột (Matching) ───────────────────────────────────────────────────
    // Payload schema: { "leftItems": [...], "rightItems": [...], "correctPairs": [{"left":"0","right":"1"}] }
    // studentAnswerJson: JSON array of "left:right" strings hoặc comma-separated
    public class MatchingQuestion : QuestionBase
    {
        public MatchingQuestion() => QuestionType = "MATCHING";

        public override double EvaluateAnswer(string studentAnswerJson)
        {
            if (!TryGetPayloadProperty(Payload, "correctPairs", out var correctEl)
                || correctEl.ValueKind != JsonValueKind.Array)
                return 0.0;

            var correctSet = correctEl.EnumerateArray()
                .Where(x => x.TryGetProperty("left", out _) && x.TryGetProperty("right", out _))
                .Select(x => $"{x.GetProperty("left").ToString().Trim().ToLowerInvariant()}:{x.GetProperty("right").ToString().Trim().ToLowerInvariant()}")
                .OrderBy(x => x)
                .ToArray();

            string[] submitted;
            try
            {
                using var doc = JsonDocument.Parse(studentAnswerJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    submitted = doc.RootElement.EnumerateArray()
                        .Select(x => x.ToString().Trim().ToLowerInvariant())
                        .OrderBy(x => x).ToArray();
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    submitted = doc.RootElement.EnumerateObject()
                        .Select(prop => $"{prop.Name.Trim().ToLowerInvariant()}:{prop.Value.ToString().Trim().ToLowerInvariant()}")
                        .OrderBy(x => x).ToArray();
                }
                else
                {
                    submitted = studentAnswerJson.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim().ToLowerInvariant()).OrderBy(x => x).ToArray();
                }
            }
            catch
            {
                submitted = studentAnswerJson.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim().ToLowerInvariant()).OrderBy(x => x).ToArray();
            }

            return correctSet.SequenceEqual(submitted) ? 1.0 : 0.0;
        }
    }

    // ─── Tự luận (Essay) — AI chấm điểm ────────────────────────────────────
    // Payload schema: { "rubric": "barem điểm...", "maxScore": 10.0 }
    public class EssayQuestion : QuestionBase
    {
        public EssayQuestion() => QuestionType = "ESSAY";

        public string GradingRubric { get; set; } = string.Empty;

        public override double EvaluateAnswer(string studentAnswerJson)
        {
            // Essay phải được chấm bất đồng bộ bởi Gemini AI Service
            throw new NotImplementedException("Essay questions must be graded asynchronously via Gemini AI Service.");
        }
    }
}
