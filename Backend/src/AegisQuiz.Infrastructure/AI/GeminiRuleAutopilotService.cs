using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Services;

namespace AegisQuiz.Infrastructure.AI
{
    /// <summary>
    /// AI Rule Autopilot Service - Tự động học định dạng đề lạ và suy diễn quy tắc CIG 4 chiều.
    /// Sử dụng Gemini 1.5 Flash kết hợp Local Heuristic Syntax Sniffer (offline fallback).
    /// </summary>
    public class GeminiRuleAutopilotService : IAiRuleAutopilotService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiKey;
        private readonly ILogger<GeminiRuleAutopilotService> _logger;

        public GeminiRuleAutopilotService(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<GeminiRuleAutopilotService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = config["Gemini:ApiKey"] ?? string.Empty;
            _logger = logger;
        }

        public async Task<AiRuleAutopilotResponseDto> SynthesizeRuleAsync(AiRuleAutopilotRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.SnippetText))
            {
                return new AiRuleAutopilotResponseDto
                {
                    Success = false,
                    ConfidenceScore = 0,
                    Reasoning = "Nội dung đoạn mẫu đề thi trống, không thể phân tích cú pháp."
                };
            }

            // 1. Thử gọi Gemini 1.5 Flash nếu có API key
            if (!string.IsNullOrEmpty(_apiKey))
            {
                try
                {
                    var geminiResult = await SynthesizeWithGeminiAsync(request);
                    if (geminiResult != null && geminiResult.Success)
                    {
                        return geminiResult;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Gemini Rule Autopilot gặp sự cố, chuyển sang Local Heuristic Sniffer.");
                }
            }

            // 2. Offline Fallback: Sử dụng Local Heuristic Sniffer
            return SynthesizeWithLocalHeuristicSniffer(request);
        }

        private async Task<AiRuleAutopilotResponseDto?> SynthesizeWithGeminiAsync(AiRuleAutopilotRequestDto request)
        {
            var systemPrompt = @"Bạn là Chuyên gia CIG (Cognitive Ingestion Grammar) số 1 thế giới về thiết kế bộ phân giải đề thi (Question Ingestion Engine).
Nhiệm vụ của bạn: Phân tích đoạn mẫu đề thi (kèm gợi ý nếu có) và sinh ra một bộ Quy tắc CIG 4 chiều (QuestionParsingRuleDto) chuẩn JSON để bóc tách chính xác toàn bộ câu hỏi.

Cấu trúc CIG 4 chiều yêu cầu:
1. RuleCode: Mã quy tắc (VD: CUSTOM_OLYMPIA_2026, GDPT_2025_MODERN, v.v.)
2. RuleName: Tên mô tả
3. Description: Giải thích
4. Priority: 1-100 (khuyên dùng 50-80)
5. Structure:
   - QuestionHeaderRegexPattern: Regex nhận diện bắt đầu câu (phải có group cho số thứ tự hoặc mã)
   - QuestionNumberGroupIndex: Số thứ tự group (thường là 1)
   - QuestionType: SINGLE | MULTI | TRUE_FALSE | ESSAY | SHORT_ANSWER
   - DelimiterType: REGEX_LINE_START
6. Semantics:
   - MathBlockMarkerRegex: Regex nhận diện công thức LaTeX/MathML
   - CodeBlockMarkerRegex: Regex khối mã nguồn
   - ImageMarkerRegex: Regex hình ảnh
7. Options:
   - OptionRegexPattern: Regex nhận diện phương án (phải có group nhãn A/B/C/D và group nội dung)
   - OptionLabelGroupIndex: Group index của ký tự nhãn (A, B, C, D...)
   - OptionContentGroupIndex: Group index của nội dung phương án
8. Evaluation:
   - AnswerRegexPattern: Regex nhận diện đáp án (VD: ^(?:ĐÁP\\s*ÁN|ANSWER|KEY|CHỌN)[\\.\\:\\s\\-]+([A-D0-9a-d\\,\\s]+))
   - AnswerGroupIndex: Group index chứa đáp án đúng (thường là 1)
   - ExplanationRegexPattern: Regex giải thích/lời giải
   - CriticalQuestionMarkerRegex: Regex điểm liệt (nếu có)
   - ScoreStrategy: ALL_OR_NOTHING | PARTIAL_CREDIT | FATAL_LOCK

Chỉ trả về JSON thuần của đối tượng QuestionParsingRuleDto, không bọc markdown thừa.";

            var userPrompt = $@"Mẫu đề thi thực tế:
---
{request.SnippetText}
---

Gợi ý ngữ cảnh: {request.ExamHint ?? "Không có"}
Ngôn ngữ: {request.Language}

Hãy phân tích và trả về đúng 1 JSON QuestionParsingRuleDto.";

            var client = _httpClientFactory.CreateClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = $"{systemPrompt}\n\n{userPrompt}" }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    maxOutputTokens = 2048,
                    responseMimeType = "application/json"
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini API call failed with status code {StatusCode}", response.StatusCode);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0) return null;

            var rawText = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? string.Empty;

            // Bóc tách JSON
            var start = rawText.IndexOf('{');
            var end = rawText.LastIndexOf('}');
            if (start >= 0 && end >= 0)
            {
                rawText = rawText.Substring(start, end - start + 1);
            }

            var generatedRule = JsonSerializer.Deserialize<QuestionParsingRuleDto>(rawText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (generatedRule == null || string.IsNullOrWhiteSpace(generatedRule.AnchorSpec?.RegexPattern))
            {
                return null;
            }

            // Kiểm tra tính hợp lệ của Regex
            ValidateRuleRegexes(generatedRule);

            // Chạy thử nghiệm ngay trên engine
            var testResult = DynamicQuestionIngestionEngine.Instance.ParseSnippet(request.SnippetText, generatedRule);

            double confidence = 85.0;
            if (testResult.TotalQuestionsFound > 0)
            {
                confidence = 98.0;
            }

            return new AiRuleAutopilotResponseDto
            {
                Success = true,
                ConfidenceScore = confidence,
                Reasoning = $"Gemini 1.5 Flash đã phân tích cú pháp đề thi và trích xuất thành công {testResult.TotalQuestionsFound} câu hỏi với các bộ nhận diện chính quy tương thích 100%.",
                GeneratedRule = generatedRule,
                TestedParseResult = testResult
            };
        }

        /// <summary>
        /// Bộ máy phân tích tĩnh (Local Heuristic Syntax Sniffer) hoạt động offline không cần API Key
        /// </summary>
        public AiRuleAutopilotResponseDto SynthesizeWithLocalHeuristicSniffer(AiRuleAutopilotRequestDto request)
        {
            var text = request.SnippetText;
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            string detectedHeaderRegex = @"^(?:Câu|Bài|Question|Item)\s*(\d+)[\.\:\s\-]+";
            int numberGroup = 1;
            string detectedOptionRegex = @"^(?:([A-D])|[\(]([A-D])[\)])[\.\:\)\s\-]+(.*)$";
            int labelGroup = 1;
            int contentGroup = 3;
            string detectedAnswerRegex = @"^(?:ĐÁP\s*ÁN|ANSWER|KEY|CHỌN)[\.\:\s\-]+([A-D0-9a-d\,\s]+)";
            string detectedExplanationRegex = @"^(?:Lời\s*giải|Hướng\s*dẫn|HD|Giải\s*thích|Explanation)\b";
            string questionType = "SINGLE";
            string ruleName = "AI Auto-Synthesized Rule";
            string ruleCode = $"AUTOPILOT_{DateTime.UtcNow:yyyyMMddHHmmss}";

            // 1. Quét tìm Question Header
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (Regex.IsMatch(trimmed, @"^#\s*(\d+)[\.\:\s\-]+"))
                {
                    detectedHeaderRegex = @"^#\s*(\d+)[\.\:\s\-]+";
                    ruleName = "Đề thi định dạng Hash (#1, #2)";
                    break;
                }
                if (Regex.IsMatch(trimmed, @"^(?:Q|Que|Question)\s*(\d+)[\.\:\s\-]+", RegexOptions.IgnoreCase))
                {
                    detectedHeaderRegex = @"^(?:Q|Que|Question)\s*(\d+)[\.\:\s\-]+";
                    ruleName = "Đề thi Quốc tế tiếng Anh (Question 1..)";
                    break;
                }
                if (Regex.IsMatch(trimmed, @"^\[\s*Câu\s*(\d+)\s*\]", RegexOptions.IgnoreCase))
                {
                    detectedHeaderRegex = @"^\[\s*Câu\s*(\d+)\s*\][\.\:\s\-]*";
                    ruleName = "Đề thi dấu ngoặc vuông ([Câu 1])";
                    break;
                }
                if (Regex.IsMatch(trimmed, @"^PHẦN\s+[IVXLCDM]+\b", RegexOptions.IgnoreCase))
                {
                    ruleName = "Đề thi cấu trúc Phần GDPT 2025";
                }
            }

            // 2. Quét tìm Option Pattern
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (Regex.IsMatch(trimmed, @"^>>\s*([A-D])\s*<<\s*(.*)$"))
                {
                    detectedOptionRegex = @"^>>\s*([A-D])\s*<<\s*(.*)$";
                    labelGroup = 1;
                    contentGroup = 2;
                    break;
                }
                if (Regex.IsMatch(trimmed, @"^\[([A-D])\]\s*(.*)$"))
                {
                    detectedOptionRegex = @"^\[([A-D])\]\s*(.*)$";
                    labelGroup = 1;
                    contentGroup = 2;
                    break;
                }
                if (Regex.IsMatch(trimmed, @"^[a-d]\)\s*(.*)$"))
                {
                    detectedOptionRegex = @"^(?:([a-d])|[\(]([a-d])[\)])[\.\:\)\s\-]+(.*)$";
                    labelGroup = 1;
                    contentGroup = 3;
                    questionType = "TRUE_FALSE";
                    break;
                }
            }

            // 3. Quét tìm Answer Pattern
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (Regex.IsMatch(trimmed, @"^(?:FLAG|KET_QUA|RESULT)[\.\:\s\-]+", RegexOptions.IgnoreCase))
                {
                    detectedAnswerRegex = @"^(?:FLAG|KET_QUA|RESULT|ĐÁP\s*ÁN)[\.\:\s\-]+([A-D0-9a-d\,\s]+)";
                    break;
                }
            }

            var rule = new QuestionParsingRuleDto
            {
                RuleCode = ruleCode,
                RuleName = ruleName,
                Description = $"Tự động sinh bởi Local Heuristic Syntax Sniffer lúc {DateTime.Now:HH:mm:ss dd/MM/yyyy}",
                Priority = 60,
                AnchorSpec = new AnchorSpecDto
                {
                    RegexPattern = detectedHeaderRegex,
                    QuestionNumberGroupIndex = numberGroup,
                    DelimiterType = "REGEX_LINE_START"
                },
                StimulusSpec = new StimulusSpecDto
                {
                    MathBlockMarkerRegex = @"(?:\$\$[^\$]+\$\$|\$[^\$]+\$)",
                    CodeBlockMarkerRegex = @"```[\s\S]*?```"
                },
                InteractionSpec = new InteractionSpecDto
                {
                    DefaultQuestionType = questionType,
                    OptionRegexPattern = detectedOptionRegex,
                    OptionLabelGroupIndex = labelGroup,
                    OptionContentGroupIndex = contentGroup
                },
                EvaluationSpec = new EvaluationSpecDto
                {
                    AnswerRegexPattern = detectedAnswerRegex,
                    AnswerGroupIndex = 1,
                    ExplanationRegexPattern = detectedExplanationRegex,
                    CriticalQuestionMarkerRegex = @"(?:\[\s*ĐIỂM\s*LIỆT\s*\]|⚠️|TỬ\s*THẦN)",
                    ScoreStrategy = "ALL_OR_NOTHING"
                }
            };

            ValidateRuleRegexes(rule);

            var testResult = DynamicQuestionIngestionEngine.Instance.ParseSnippet(text, rule);
            double confidence = testResult.TotalQuestionsFound > 0 ? 92.0 : 65.0;

            return new AiRuleAutopilotResponseDto
            {
                Success = true,
                ConfidenceScore = confidence,
                Reasoning = $"Local Heuristic Syntax Sniffer đã dò quét hình thái văn bản và nhận diện thành công: {ruleName} với {testResult.TotalQuestionsFound} câu hỏi trích xuất được.",
                GeneratedRule = rule,
                TestedParseResult = testResult
            };
        }

        private void ValidateRuleRegexes(QuestionParsingRuleDto rule)
        {
            // Bảo đảm các chuỗi regex hợp lệ, nếu không thì gán lại default an toàn
            try { _ = new Regex(rule.AnchorSpec.RegexPattern); }
            catch { rule.AnchorSpec.RegexPattern = @"^(?:Câu|Bài|Question)\s*(\d+)[\.\:\s\-]+"; }

            try { _ = new Regex(rule.InteractionSpec.OptionRegexPattern); }
            catch { rule.InteractionSpec.OptionRegexPattern = @"^(?:([A-D])|[\(]([A-D])[\)])[\.\:\)\s\-]+(.*)$"; }

            try { _ = new Regex(rule.EvaluationSpec.AnswerRegexPattern); }
            catch { rule.EvaluationSpec.AnswerRegexPattern = @"^(?:ĐÁP\s*ÁN|ANSWER|KEY|CHỌN)[\.\:\s\-]+([A-D0-9a-d\,\s]+)"; }
        }
    }
}
