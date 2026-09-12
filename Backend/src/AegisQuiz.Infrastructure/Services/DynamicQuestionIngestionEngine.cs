using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [Universal Dynamic Question Ingestion Engine]
    /// Động cơ thông dịch Ngữ pháp Nhận thức Toàn cầu (CIG Engine):
    /// - Quản lý các quy tắc nhận diện câu hỏi từ file JSON và CSDL.
    /// - JIT Regex Compiled Cache với độ trễ <0.1ms mỗi lần khớp.
    /// - Hỗ trợ Hot-reload tức thì 0ms downtime từ giao diện Admin.
    /// - Cung cấp Live Document Sandbox để thử nghiệm trực tiếp trên Web.
    /// </summary>
    public class DynamicQuestionIngestionEngine
    {
        private static readonly Lazy<DynamicQuestionIngestionEngine> _instance = 
            new Lazy<DynamicQuestionIngestionEngine>(() => new DynamicQuestionIngestionEngine());
        public static DynamicQuestionIngestionEngine Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, Regex> _regexCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<QuestionParsingRuleDto> _rules = new();
        private readonly object _syncLock = new();

        public DynamicQuestionIngestionEngine()
        {
            LoadDefaultRules();
        }

        /// <summary>
        /// Nạp các quy tắc mặc định từ file cấu hình parsing_rules_registry.json
        /// </summary>
        public void LoadDefaultRules()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var possiblePaths = new[]
                {
                    Path.Combine(baseDir, "Config", "parsing_rules_registry.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Config", "parsing_rules_registry.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Backend", "src", "AegisQuiz.Infrastructure", "Config", "parsing_rules_registry.json"),
                    @"D:\Cuong\DuAn\mybank\AegisQuiz\Backend\src\AegisQuiz.Infrastructure\Config\parsing_rules_registry.json"
                };

                string? foundPath = possiblePaths.FirstOrDefault(File.Exists);
                if (foundPath != null)
                {
                    string json = File.ReadAllText(foundPath);
                    if (!HotReloadRules(json) || _rules.Count == 0)
                    {
                        LoadHardcodedFallbackRules();
                    }
                }
                else
                {
                    // Fallback bộ quy tắc cốt lõi nếu không tìm thấy file
                    LoadHardcodedFallbackRules();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DynamicIngestionEngine] Cảnh báo khi nạp file config: {ex.Message}. Sử dụng Fallback.");
                LoadHardcodedFallbackRules();
            }
        }

        /// <summary>
        /// Hot-reload toàn bộ quy tắc từ chuỗi JSON (0ms downtime)
        /// </summary>
        public bool HotReloadRules(string jsonConfig)
        {
            lock (_syncLock)
            {
                try
                {
                    using var doc = JsonDocument.Parse(jsonConfig);
                    if (doc.RootElement.TryGetProperty("rules", out var rulesArray))
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var parsedRules = JsonSerializer.Deserialize<List<QuestionParsingRuleDto>>(rulesArray.GetRawText(), options);
                        if (parsedRules != null && parsedRules.Count > 0)
                        {
                            _rules.Clear();
                            _rules.AddRange(parsedRules.Where(r => r.IsActive).OrderBy(r => r.Priority));
                            _regexCache.Clear();

                            // Pre-compile các regex chính
                            foreach (var rule in _rules)
                            {
                                CompileAndCache(rule.AnchorSpec.RegexPattern);
                                CompileAndCache(rule.InteractionSpec.OptionRegexPattern);
                                CompileAndCache(rule.EvaluationSpec.AnswerRegexPattern);
                            }

                            Console.WriteLine($"[DynamicIngestionEngine] Đã nạp thành công {_rules.Count} quy tắc động vào bộ nhớ.");
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DynamicIngestionEngine] Lỗi hot-reload quy tắc: {ex.Message}");
                }
                return false;
            }
        }

        public IReadOnlyList<QuestionParsingRuleDto> GetAllRules()
        {
            lock (_syncLock)
            {
                return _rules.ToList();
            }
        }

        /// <summary>
        /// Thêm mới hoặc cập nhật một quy tắc động vào bộ nhớ và JIT Regex cache (0ms downtime)
        /// </summary>
        public bool AddOrUpdateRule(QuestionParsingRuleDto newRule, bool persistToFile = false)
        {
            if (newRule == null || string.IsNullOrWhiteSpace(newRule.RuleCode)) return false;

            lock (_syncLock)
            {
                // Xóa rule cũ nếu đã tồn tại
                _rules.RemoveAll(r => string.Equals(r.RuleCode, newRule.RuleCode, StringComparison.OrdinalIgnoreCase));
                _rules.Add(newRule);
                _rules.Sort((a, b) => a.Priority.CompareTo(b.Priority));

                // Pre-compile các regex của rule mới
                CompileAndCache(newRule.AnchorSpec.RegexPattern);
                CompileAndCache(newRule.InteractionSpec.OptionRegexPattern);
                CompileAndCache(newRule.EvaluationSpec.AnswerRegexPattern);

                Console.WriteLine($"[DynamicIngestionEngine] Đã nạp thành công quy tắc [{newRule.RuleCode}] {newRule.RuleName} vào hệ thống.");

                if (persistToFile)
                {
                    try
                    {
                        var possiblePaths = new[]
                        {
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "parsing_rules_registry.json"),
                            Path.Combine(Directory.GetCurrentDirectory(), "Config", "parsing_rules_registry.json"),
                            Path.Combine(Directory.GetCurrentDirectory(), "Backend", "src", "AegisQuiz.Infrastructure", "Config", "parsing_rules_registry.json"),
                            @"D:\Cuong\DuAn\mybank\AegisQuiz\Backend\src\AegisQuiz.Infrastructure\Config\parsing_rules_registry.json"
                        };
                        string? foundPath = possiblePaths.FirstOrDefault(File.Exists);
                        if (foundPath != null)
                        {
                            var registryObj = new
                            {
                                version = "1.0.0",
                                lastUpdated = DateTime.UtcNow.ToString("o"),
                                rules = _rules
                            };
                            var json = JsonSerializer.Serialize(registryObj, new JsonSerializerOptions { WriteIndented = true });
                            File.WriteAllText(foundPath, json);
                            Console.WriteLine($"[DynamicIngestionEngine] Đã lưu vĩnh viễn quy tắc vào tệp: {foundPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DynamicIngestionEngine] Cảnh báo khi lưu vĩnh viễn tệp JSON: {ex.Message}");
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Khớp điểm neo nhận diện câu hỏi từ một dòng văn bản.
        /// Rules được sắp xếp theo Priority tăng dần (số nhỏ = đặc thù nhất = kiểm tra trước).
        /// Trả về rule khớp đầu tiên theo thứ tự ưu tiên đã sắp xếp.
        /// </summary>
        public bool TryMatchQuestionAnchor(string line, out int questionNumber, out QuestionParsingRuleDto? matchedRule)
        {
            questionNumber = 0;
            matchedRule = null;

            lock (_syncLock)
            {
                foreach (var rule in _rules)
                {
                    var regex = GetOrAddRegex(rule.AnchorSpec.RegexPattern);
                    var match = regex.Match(line);
                    if (match.Success)
                    {
                        matchedRule = rule;
                        if (rule.AnchorSpec.QuestionNumberGroupIndex <= match.Groups.Count - 1)
                        {
                            var numStr = match.Groups[rule.AnchorSpec.QuestionNumberGroupIndex].Value;
                            int.TryParse(numStr, out questionNumber);
                        }
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Khớp một phương án trả lời (Option)
        /// </summary>
        public bool TryMatchOption(string line, QuestionParsingRuleDto rule, out string label, out string content)
        {
            label = string.Empty;
            content = string.Empty;

            var regex = GetOrAddRegex(rule.InteractionSpec.OptionRegexPattern);
            var match = regex.Match(line);
            if (match.Success)
            {
                int labelIdx = rule.InteractionSpec.OptionLabelGroupIndex;
                if (labelIdx <= match.Groups.Count - 1 && !string.IsNullOrEmpty(match.Groups[labelIdx].Value))
                {
                    label = match.Groups[labelIdx].Value.Trim();
                }
                else
                {
                    for (int i = 1; i <= Math.Min(3, match.Groups.Count - 1); i++)
                    {
                        if (!string.IsNullOrEmpty(match.Groups[i].Value))
                        {
                            label = match.Groups[i].Value.Trim();
                            break;
                        }
                    }
                }

                int contentIdx = rule.InteractionSpec.OptionContentGroupIndex;
                if (contentIdx <= match.Groups.Count - 1 && !string.IsNullOrEmpty(match.Groups[contentIdx].Value))
                {
                    content = match.Groups[contentIdx].Value.Trim();
                }
                else
                {
                    content = line.Substring(match.Length).Trim();
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Khớp dòng đáp án đúng (Answer Key)
        /// </summary>
        public bool TryMatchAnswer(string line, QuestionParsingRuleDto rule, out string answer)
        {
            answer = string.Empty;
            var regex = GetOrAddRegex(rule.EvaluationSpec.AnswerRegexPattern);
            var match = regex.Match(line);
            if (match.Success)
            {
                int groupIdx = rule.EvaluationSpec.AnswerGroupIndex;
                if (groupIdx <= match.Groups.Count - 1 && !string.IsNullOrEmpty(match.Groups[groupIdx].Value))
                {
                    answer = match.Groups[groupIdx].Value.Trim();
                }
                else
                {
                    answer = match.Value.Trim();
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Nhận diện dòng bắt đầu phần Lời giải thích / Hướng dẫn giải
        /// </summary>
        public bool TryMatchExplanation(string line, QuestionParsingRuleDto rule)
        {
            if (string.IsNullOrEmpty(rule.EvaluationSpec.ExplanationRegexPattern)) return false;
            var regex = GetOrAddRegex(rule.EvaluationSpec.ExplanationRegexPattern);
            return regex.IsMatch(line);
        }

        /// <summary>
        /// Nhận diện xem câu hỏi có phải là Câu Điểm Liệt tử thần hay không
        /// </summary>
        public bool IsCriticalQuestion(string text, QuestionParsingRuleDto? rule = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            if (rule != null && !string.IsNullOrEmpty(rule.EvaluationSpec.CriticalQuestionMarkerRegex))
            {
                var regex = GetOrAddRegex(rule.EvaluationSpec.CriticalQuestionMarkerRegex);
                if (regex.IsMatch(text)) return true;
            }

            lock (_syncLock)
            {
                foreach (var r in _rules)
                {
                    if (!string.IsNullOrEmpty(r.EvaluationSpec.CriticalQuestionMarkerRegex))
                    {
                        var regex = GetOrAddRegex(r.EvaluationSpec.CriticalQuestionMarkerRegex);
                        if (regex.IsMatch(text)) return true;
                    }
                }
            }

            return Regex.IsMatch(text, @"(?:\bĐIỂM\s*LIỆT\b|⚠️|TỬ\s*THẦN|\bBẮT\s*BUỘC\s*ĐÚNG\b|^\s*\*\s*(?:Câu|Question|\d+))", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// [Admin Sandbox Engine] Phân tích một đoạn snippet văn bản thô theo quy tắc
        /// </summary>
        public ParseSnippetResponseDto ParseSnippet(string snippetText, string? specificRuleCode = null)
        {
            return ParseSnippetInternal(snippetText, null, specificRuleCode);
        }

        /// <summary>
        /// Phân tích một đoạn snippet bằng một bộ quy tắc tùy biến truyền vào trực tiếp (dùng cho AI Autopilot Sandbox)
        /// </summary>
        public ParseSnippetResponseDto ParseSnippet(string snippetText, QuestionParsingRuleDto customRule)
        {
            return ParseSnippetInternal(snippetText, customRule, null);
        }

        private ParseSnippetResponseDto ParseSnippetInternal(string snippetText, QuestionParsingRuleDto? customRule, string? specificRuleCode)
        {
            var response = new ParseSnippetResponseDto();
            if (string.IsNullOrWhiteSpace(snippetText))
            {
                response.Success = false;
                response.ParsingLogs.Add("Văn bản thử nghiệm rỗng.");
                return response;
            }

            var lines = snippetText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            DocxImportPreviewDto? currentQ = null;
            QuestionParsingRuleDto? activeRule = customRule;
            var promptBuffer = new List<string>();

            if (activeRule == null && !string.IsNullOrEmpty(specificRuleCode))
            {
                activeRule = _rules.FirstOrDefault(r => string.Equals(r.RuleCode, specificRuleCode, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // 1. Kiểm tra dòng bắt đầu câu hỏi (Anchor)
                bool isAnchor = false;
                int qNum = 0;
                QuestionParsingRuleDto? matchedRule = null;

                if (activeRule != null)
                {
                    var regex = GetOrAddRegex(activeRule.AnchorSpec.RegexPattern);
                    var m = regex.Match(line);
                    if (m.Success)
                    {
                        isAnchor = true;
                        matchedRule = activeRule;
                        int.TryParse(m.Groups[activeRule.AnchorSpec.QuestionNumberGroupIndex].Value, out qNum);
                    }
                }
                else
                {
                    isAnchor = TryMatchQuestionAnchor(line, out qNum, out matchedRule);
                }

                if (isAnchor && matchedRule != null)
                {
                    if (currentQ != null)
                    {
                        response.Questions.Add(currentQ);
                    }

                    activeRule ??= matchedRule;
                    response.MatchedRuleCode = matchedRule.RuleCode;

                    currentQ = new DocxImportPreviewDto
                    {
                        Content = line,
                        QuestionType = matchedRule.InteractionSpec.DefaultQuestionType,
                        TopicCode = matchedRule.TargetExamType,
                        IsCritical = IsCriticalQuestion(line, matchedRule)
                    };

                    promptBuffer.Clear();
                    response.ParsingLogs.Add($"[Anchor Match] Nhận diện Câu #{qNum} bằng quy tắc {matchedRule.RuleCode}");
                    continue;
                }

                var ruleToUse = activeRule ?? _rules.FirstOrDefault() ?? GetDefaultFallbackRules().First();

                // 2. Nếu chưa có câu hỏi được khởi tạo (không có prefix "Câu 1:")
                if (currentQ == null)
                {
                    // Kiểm tra xem dòng hiện tại có chứa các phương án inline trên cùng 1 dòng hay không
                    var inlineOpts = ExtractInlineOptions(line);
                    if (inlineOpts.Count >= 2)
                    {
                        var firstOpt = inlineOpts[0];
                        var promptPart = line.Substring(0, firstOpt.StartIndex).Trim();
                        var fullPrompt = promptBuffer.Count > 0 ? string.Join("\n", promptBuffer) + "\n" + promptPart : promptPart;
                        currentQ = new DocxImportPreviewDto
                        {
                            Content = string.IsNullOrWhiteSpace(fullPrompt) ? "Câu hỏi" : fullPrompt,
                            QuestionType = ruleToUse.InteractionSpec.DefaultQuestionType,
                            TopicCode = ruleToUse.TargetExamType,
                            IsCritical = IsCriticalQuestion(fullPrompt, ruleToUse)
                        };
                        foreach (var io in inlineOpts)
                        {
                            currentQ.Options.Add($"{io.Letter}. {io.Content}");
                            if (io.IsMarkedCorrect) currentQ.SuggestedAnswer = (io.Letter - 'A' + 1).ToString();
                        }
                        promptBuffer.Clear();
                        response.ParsingLogs.Add($"[Inline Options] Nhận diện {inlineOpts.Count} phương án inline");
                        continue;
                    }

                    // Kiểm tra xem dòng hiện tại có phải là dòng bắt đầu phương án (A/B/C/D...) hay không
                    if (TryMatchOption(line, ruleToUse, out string implicitOptLabel, out string implicitOptContent))
                    {
                        var implicitPrompt = promptBuffer.Count > 0 ? string.Join("\n", promptBuffer) : "Câu hỏi";
                        currentQ = new DocxImportPreviewDto
                        {
                            Content = implicitPrompt,
                            QuestionType = ruleToUse.InteractionSpec.DefaultQuestionType,
                            TopicCode = ruleToUse.TargetExamType,
                            IsCritical = IsCriticalQuestion(implicitPrompt, ruleToUse)
                        };
                        currentQ.Options.Add($"{implicitOptLabel}. {implicitOptContent}");
                        promptBuffer.Clear();
                        response.ParsingLogs.Add($"[Implicit Anchor] Khởi tạo câu hỏi tự động từ phương án {implicitOptLabel}");
                        continue;
                    }

                    promptBuffer.Add(line);
                    continue;
                }

                // 3. Kiểm tra dòng đáp án đúng
                if (TryMatchAnswer(line, ruleToUse, out string answerKey))
                {
                    currentQ.SuggestedAnswer = answerKey;
                    response.ParsingLogs.Add($"[Answer Match] Câu có đáp án: {answerKey}");
                    continue;
                }

                // 4. Kiểm tra dòng giải thích
                if (TryMatchExplanation(line, ruleToUse))
                {
                    currentQ.AiExplanation = line;
                    response.ParsingLogs.Add($"[Explanation Match] Nhận diện lời giải: {line.Substring(0, Math.Min(30, line.Length))}...");
                    continue;
                }

                // 5. Kiểm tra dòng phương án (Option)
                if (TryMatchOption(line, ruleToUse, out string optLabel, out string optContent))
                {
                    currentQ.Options.Add($"{optLabel}. {optContent}");
                    response.ParsingLogs.Add($"[Option Match] Thêm phương án {optLabel}");
                    continue;
                }

                // 6. Nội dung bổ sung đề bài hoặc lời giải
                if (!string.IsNullOrEmpty(currentQ.AiExplanation))
                {
                    currentQ.AiExplanation += "\n" + line;
                }
                else if (currentQ.Options.Count == 0)
                {
                    currentQ.Content += "\n" + line;
                    if (IsCriticalQuestion(line, ruleToUse))
                    {
                        currentQ.IsCritical = true;
                    }
                }
            }

            if (currentQ != null)
            {
                response.Questions.Add(currentQ);
            }

            response.ParsedCount = response.Questions.Count;
            response.Success = response.ParsedCount > 0;
            return response;
        }

        /// <summary>
        /// Bóc tách các phương án nằm cùng một dòng (inline options dạng A. ... B. ... hoặc 1. ... 2. ...)
        /// </summary>
        public static List<InlineOptionMatch> ExtractInlineOptions(string text)
        {
            var options = new List<InlineOptionMatch>();
            if (string.IsNullOrWhiteSpace(text)) return options;

            var regex = new Regex(@"(?:^|[\s\t\.\,\;\–\—]+|(?<=[a-z0-9\)]))(\*?)([A-G]|[1-4])(\*?)[\.\:\)]+\s*", RegexOptions.IgnoreCase);
            var matches = regex.Matches(text);
            if (matches.Count < 2) return options;

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                char rawChar = match.Groups[2].Value[0];
                char letter = char.IsDigit(rawChar) ? (char)('A' + (rawChar - '1')) : char.ToUpperInvariant(rawChar);
                bool isCorrect = match.Groups[1].Value == "*" || match.Groups[3].Value == "*";

                int contentStart = match.Index + match.Length;
                int contentEnd = (i + 1 < matches.Count) ? matches[i + 1].Index : text.Length;

                string content = text.Substring(contentStart, contentEnd - contentStart).Trim();
                content = Regex.Replace(content, @"[\.\s\t]+$", "").Trim();

                options.Add(new InlineOptionMatch
                {
                    Letter = letter,
                    Content = content,
                    IsMarkedCorrect = isCorrect,
                    StartIndex = match.Index
                });
            }

            return options;
        }

        private Regex GetOrAddRegex(string pattern)
        {
            return _regexCache.GetOrAdd(pattern, p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled));
        }

        private void CompileAndCache(string pattern)
        {
            if (!string.IsNullOrWhiteSpace(pattern))
            {
                _ = GetOrAddRegex(pattern);
            }
        }

        private void LoadHardcodedFallbackRules()
        {
            _rules.Clear();
            _rules.AddRange(GetDefaultFallbackRules());
            _regexCache.Clear();

            foreach (var rule in _rules)
            {
                CompileAndCache(rule.AnchorSpec.RegexPattern);
                CompileAndCache(rule.InteractionSpec.OptionRegexPattern);
                CompileAndCache(rule.EvaluationSpec.AnswerRegexPattern);
            }
        }

        private List<QuestionParsingRuleDto> GetDefaultFallbackRules()
        {
            return new List<QuestionParsingRuleDto>
            {
                new QuestionParsingRuleDto
                {
                    RuleCode = "VN_STANDARD_MULTIPLE_CHOICE",
                    RuleName = "Trắc Nghiệm Đơn Tiêu Chuẩn (A/B/C/D)",
                    TargetExamType = "GENERAL",
                    Language = "vi",
                    Priority = 10,
                    IsActive = true,
                    AnchorSpec = new AnchorSpecDto
                    {
                        RegexPattern = @"^(?:Câu|Question|Item|Bài|Q)\s*(\d+)",
                        QuestionNumberGroupIndex = 1
                    },
                    InteractionSpec = new InteractionSpecDto
                    {
                        DefaultQuestionType = "SINGLE",
                        OptionRegexPattern = @"^(?:([A-D])|[(]([A-D])[)])[\.\:\)\s\-]+(.*)$",
                        OptionLabelGroupIndex = 1,
                        OptionContentGroupIndex = 3
                    },
                    EvaluationSpec = new EvaluationSpecDto
                    {
                        AnswerRegexPattern = @"^(?:ĐÁP\s*ÁN|ANSWER|KEY|CHỌN)[\.\:\s\-]+([A-D0-9a-d\,\s]+)",
                        AnswerGroupIndex = 1,
                        ExplanationRegexPattern = @"^(?:Lời\s*giải|Hướng\s*dẫn|HD|Giải\s*thích|Explanation|Căn\s*cứ)\b",
                        CriticalQuestionMarkerRegex = @"(?:\bĐIỂM\s*LIỆT\b|⚠️|TỬ\s*THẦN)"
                    }
                },
                new QuestionParsingRuleDto
                {
                    RuleCode = "VN_GDPT_2025_TRUE_FALSE_MATRIX",
                    RuleName = "Trắc Nghiệm Đúng / Sai Đa Mệnh Đề (GDPT 2025)",
                    TargetExamType = "GDPT_2025",
                    Language = "vi",
                    Priority = 20,
                    IsActive = true,
                    AnchorSpec = new AnchorSpecDto
                    {
                        RegexPattern = @"^(?:Câu|Question)\s*(\d+)",
                        QuestionNumberGroupIndex = 1
                    },
                    InteractionSpec = new InteractionSpecDto
                    {
                        DefaultQuestionType = "TRUE_FALSE",
                        OptionRegexPattern = @"^(?:([a-d])|[(]([a-d])[)])[\.\:\)\s\-]+(.*)$",
                        OptionLabelGroupIndex = 1,
                        OptionContentGroupIndex = 3
                    },
                    EvaluationSpec = new EvaluationSpecDto
                    {
                        AnswerRegexPattern = @"^(?:ĐÁP\s*ÁN|ANSWER|KEY)[\.\:\s\-]+([a-d\:\sĐSđsTFtf\,\;]+)",
                        AnswerGroupIndex = 1,
                        ExplanationRegexPattern = @"^(?:Lời\s*giải|Hướng\s*dẫn|HD|Giải\s*thích)\b"
                    }
                },
                new QuestionParsingRuleDto
                {
                    RuleCode = "VN_GDPT_2025_SHORT_ANSWER",
                    RuleName = "Trắc Nghiệm Trả Lời Ngắn / Điền Số (GDPT 2025)",
                    TargetExamType = "GDPT_2025",
                    Language = "vi",
                    Priority = 30,
                    IsActive = true,
                    AnchorSpec = new AnchorSpecDto
                    {
                        RegexPattern = @"^(?:Câu|Question)\s*(\d+)",
                        QuestionNumberGroupIndex = 1
                    },
                    InteractionSpec = new InteractionSpecDto
                    {
                        DefaultQuestionType = "SHORT_ANSWER",
                        OptionRegexPattern = @"^(?:([A-D])|[(]([A-D])[)])[\.\:\s\-]+(.*)$"
                    },
                    EvaluationSpec = new EvaluationSpecDto
                    {
                        AnswerRegexPattern = @"^(?:ĐÁP\s*ÁN|ANSWER|KEY|KẾT\s*QUẢ)[\.\:\s\-]+([\\-\\+]?\\d+[\\,\\.]?\\d*)",
                        AnswerGroupIndex = 1
                    }
                },
                new QuestionParsingRuleDto
                {
                    RuleCode = "GPLX_NATIONAL_DRIVING_TEST",
                    RuleName = "Sát Hạch Lái Xe Quốc Gia (GPLX 600 Câu & Điểm Liệt)",
                    TargetExamType = "GPLX",
                    Language = "vi",
                    Priority = 15,
                    IsActive = true,
                    AnchorSpec = new AnchorSpecDto
                    {
                        RegexPattern = @"^(?:Câu|Question)\s*(\d+)",
                        QuestionNumberGroupIndex = 1
                    },
                    InteractionSpec = new InteractionSpecDto
                    {
                        DefaultQuestionType = "SINGLE",
                        OptionRegexPattern = @"^(?:([1-4])|([A-D]))[\.\:\s\-]+(.*)$",
                        OptionLabelGroupIndex = 1,
                        OptionContentGroupIndex = 3
                    },
                    EvaluationSpec = new EvaluationSpecDto
                    {
                        AnswerRegexPattern = @"^(?:ĐÁP\s*ÁN|CHỌN)[\.\:\s\-]+([1-4A-D])",
                        AnswerGroupIndex = 1,
                        CriticalQuestionMarkerRegex = @"(?:\bĐIỂM\s*LIỆT\b|⚠️|TỬ\s*THẦN)"
                    }
                },
                new QuestionParsingRuleDto
                {
                    RuleCode = "INTERNATIONAL_SAT_LOGIC",
                    RuleName = "Khảo Thí Quốc Tế SAT / GRE Logic (Format Không Dấu Chấm)",
                    TargetExamType = "SAT",
                    Language = "en",
                    Priority = 5,
                    IsActive = true,
                    AnchorSpec = new AnchorSpecDto
                    {
                        RegexPattern = @"^(?:Task|Problem)\s*#?(\d+)",
                        QuestionNumberGroupIndex = 1
                    },
                    InteractionSpec = new InteractionSpecDto
                    {
                        DefaultQuestionType = "SINGLE",
                        OptionRegexPattern = @"^\[([A-D])\]\s*(.*)$",
                        OptionLabelGroupIndex = 1,
                        OptionContentGroupIndex = 2
                    },
                    EvaluationSpec = new EvaluationSpecDto
                    {
                        AnswerRegexPattern = @"^\[(?:Key|Truth|Answer)\s*\:\s*([A-D])\]",
                        AnswerGroupIndex = 1,
                        ExplanationRegexPattern = @"^\[(?:Rationale|Explanation)\]"
                    }
                }
            };
        }
    }

    public class InlineOptionMatch
    {
        public char Letter { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsMarkedCorrect { get; set; }
        public int StartIndex { get; set; }
    }
}
