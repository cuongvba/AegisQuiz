using System;
using System.Linq;
using AegisQuiz.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace AegisQuiz.UnitTests
{
    public class DynamicQuestionIngestionTests
    {
        private readonly ITestOutputHelper _output;

        public DynamicQuestionIngestionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void LoadRules_ShouldLoadConfigRulesSuccessfully()
        {
            var engine = new DynamicQuestionIngestionEngine();
            var rules = engine.GetAllRules();

            _output.WriteLine($"[DynamicIngestion] Tổng số quy tắc nạp được: {rules.Count}");
            foreach (var r in rules)
            {
                _output.WriteLine($" - [{r.RuleCode}] {r.RuleName} (Priority: {r.Priority})");
            }

            Assert.NotEmpty(rules);
            Assert.Contains(rules, r => r.RuleCode == "VN_STANDARD_MULTIPLE_CHOICE");
            Assert.Contains(rules, r => r.RuleCode == "GPLX_NATIONAL_DRIVING_TEST");
        }

        [Fact]
        public void ParseStandardMultipleChoice_ShouldExtractQuestionAndOptions()
        {
            var engine = new DynamicQuestionIngestionEngine();
            var snippet = @"
Câu 1. Thủ đô của nước Cộng hòa Xã hội Chủ nghĩa Việt Nam là thành phố nào?
A. Thành phố Hồ Chí Minh
B. Hà Nội
C. Đà Nẵng
D. Cần Thơ
ĐÁP ÁN: B
Lời giải: Theo Hiến pháp Việt Nam, Hà Nội là thủ đô của cả nước.
";

            var result = engine.ParseSnippet(snippet);

            Assert.True(result.Success);
            Assert.Equal(1, result.ParsedCount);
            var q = result.Questions.First();
            Assert.Contains("Thủ đô của nước", q.Content);
            Assert.Equal(4, q.Options.Count);
            Assert.Equal("B", q.SuggestedAnswer);
            Assert.Contains("Hà Nội là thủ đô", q.AiExplanation);
            _output.WriteLine($"[PASS] Standard Multiple Choice parsed: {q.Content.Substring(0, 30)}... Answer: {q.SuggestedAnswer}");
        }

        [Fact]
        public void ParseGdpt2025TrueFalse_ShouldRecognizeMatrix()
        {
            var engine = new DynamicQuestionIngestionEngine();
            var snippet = @"
Câu 2. Cho hàm số y = f(x) có đạo hàm liên tục trên R. Xét tính đúng sai của các mệnh đề sau:
a) Hàm số đồng biến trên khoảng (0; 2).
b) Đồ thị hàm số có đúng 2 điểm cực trị.
c) Giá trị lớn nhất của hàm số bằng 5.
d) Phương trình f(x) = 0 có đúng 3 nghiệm thực.
ĐÁP ÁN: a:Đ, b:S, c:Đ, d:S
";

            var result = engine.ParseSnippet(snippet, "VN_GDPT_2025_TRUE_FALSE_MATRIX");

            Assert.True(result.Success);
            var q = result.Questions.First();
            Assert.Equal("TRUE_FALSE", q.QuestionType);
            Assert.Equal(4, q.Options.Count);
            _output.WriteLine($"[PASS] True/False Matrix parsed with 4 sub-items, type={q.QuestionType}");
        }

        [Fact]
        public void ParseDrivingLicense_ShouldDetectCriticalQuestion()
        {
            var engine = new DynamicQuestionIngestionEngine();
            var snippet = @"
Câu 18. [ĐIỂM LIỆT] Người điều khiển xe mô tô hai bánh, xe gắn máy có được phép buông cả hai tay khi đang tham gia giao thông không?
1. Được phép nếu đường vắng.
2. Không được phép.
3. Được phép khi có việc khẩn cấp.
ĐÁP ÁN: 2
Giải thích: Hành vi buông cả hai tay là hành vi bị nghiêm cấm theo Luật Giao thông đường bộ.
";

            var result = engine.ParseSnippet(snippet, "GPLX_NATIONAL_DRIVING_TEST");

            Assert.True(result.Success);
            var q = result.Questions.First();
            Assert.True(q.IsCritical, "Câu hỏi điểm liệt phải được gắn cờ IsCritical = true!");
            Assert.Equal(3, q.Options.Count);
            Assert.Equal("2", q.SuggestedAnswer);
            _output.WriteLine($"[PASS] Critical Driving Question flagged IsCritical={q.IsCritical}, Answer={q.SuggestedAnswer}");
        }

        [Fact]
        public void ParseInternationalSat_ShouldRecognizeTaskAndBrackets()
        {
            var engine = new DynamicQuestionIngestionEngine();
            var snippet = @"
Task #42: If 3x + 7 = 22, what is the value of 6x - 5?
[A] 25
[B] 29
[C] 30
[D] 35
[Key: A]
[Rationale] First solve for x: 3x = 15 => x = 5. Then 6(5) - 5 = 25.
";

            var result = engine.ParseSnippet(snippet, "INTERNATIONAL_SAT_LOGIC");

            Assert.True(result.Success);
            var q = result.Questions.First();
            Assert.Contains("If 3x + 7 = 22", q.Content);
            Assert.Equal(4, q.Options.Count);
            Assert.Equal("A", q.SuggestedAnswer);
            Assert.Contains("First solve for x", q.AiExplanation);
            _output.WriteLine($"[PASS] International SAT Logic parsed with 0 lines of code change!");
        }

        [Fact]
        public void HotReload_ShouldApplyNewRuleAtRuntime()
        {
            var engine = new DynamicQuestionIngestionEngine();
            var customRuleJson = @"{
              ""rules"": [
                {
                  ""ruleCode"": ""AI_AGENT_CUSTOM_RULE"",
                  ""ruleName"": ""Đề Thi Đấu Trí Tự Do"",
                  ""priority"": 1,
                  ""isActive"": true,
                  ""anchorSpec"": {
                    ""regexPattern"": ""^CHALLENGE_(\\d+)\\:\\s*"",
                    ""questionNumberGroupIndex"": 1
                  },
                  ""interactionSpec"": {
                    ""defaultQuestionType"": ""SINGLE"",
                    ""optionRegexPattern"": ""^>>([A-D])<<\\s*(.*)$"",
                    ""optionLabelGroupIndex"": 1,
                    ""optionContentGroupIndex"": 2
                  },
                  ""evaluationSpec"": {
                    ""answerRegexPattern"": ""^CORRECT_FLAG\\:\\s*([A-D])"",
                    ""answerGroupIndex"": 1
                  }
                }
              ]
            }";

            // Thực hiện hot-reload tức thời
            bool reloaded = engine.HotReloadRules(customRuleJson);
            Assert.True(reloaded);

            var snippet = @"
CHALLENGE_100: Đây là câu hỏi format dị biệt chưa từng xuất hiện trên đời?
>>A<< Phương án siêu kỳ dị
>>B<< Phương án thông thường
>>C<< Phương án bất khả thi
CORRECT_FLAG: A
";

            var result = engine.ParseSnippet(snippet);
            Assert.True(result.Success);
            Assert.Equal("AI_AGENT_CUSTOM_RULE", result.MatchedRuleCode);
            var q = result.Questions.First();
            Assert.Equal(3, q.Options.Count);
            Assert.Equal("A", q.SuggestedAnswer);
            _output.WriteLine($"[PASS] Hot-reload applied instantly! Matched rule: {result.MatchedRuleCode}");
        }

        [Fact]
        public void AiRuleAutopilot_ShouldSynthesizeValidCigRule_ForUnknownFormat()
        {
            // Kiểm tra bộ giải mã ngữ cảnh Local Heuristic Sniffer
            var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<AegisQuiz.Infrastructure.AI.GeminiRuleAutopilotService>.Instance;
            var autopilot = new AegisQuiz.Infrastructure.AI.GeminiRuleAutopilotService(null!, config, logger);

            var weirdSnippet = @"
# 1. Đâu là kiến trúc tối thượng của Hệ Thống Đấu Trường Trí Tuệ AegisQuiz?
>>A<< Microservices kết hợp CIG Engine động
>>B<< Monolith cũ kỹ thập niên 90
>>C<< Ứng dụng desktop đơn máy
>>D<< Trang HTML tĩnh
FLAG: A
Lời giải: AegisQuiz tích hợp CIG Engine và Arena Gameshow Plugin System độc nhất vô nhị.
";

            var response = autopilot.SynthesizeWithLocalHeuristicSniffer(new AegisQuiz.Application.DTOs.AiRuleAutopilotRequestDto
            {
                SnippetText = weirdSnippet,
                ExamHint = "Đề thi kiến trúc số phong cách Hash"
            });

            Assert.True(response.Success);
            Assert.True(response.ConfidenceScore >= 90.0, $"Confidence: {response.ConfidenceScore}");
            Assert.NotNull(response.GeneratedRule);
            Assert.Equal(1, response.TestedParseResult.TotalQuestionsFound);

            var firstQuestion = response.TestedParseResult.Questions.First();
            Assert.Equal(4, firstQuestion.Options.Count);
            Assert.Equal("A", firstQuestion.SuggestedAnswer);

            _output.WriteLine($"[PASS] AI Autopilot Confidence: {response.ConfidenceScore}%. RuleCode: {response.GeneratedRule.RuleCode}");
            _output.WriteLine($"Reasoning: {response.Reasoning}");
        }

        [Fact]
        public void AddOrUpdateRule_ShouldInstantlyIntegrateIntoEngine()
        {
            var engine = DynamicQuestionIngestionEngine.Instance;
            var customRule = new AegisQuiz.Application.DTOs.QuestionParsingRuleDto
            {
                RuleCode = "INSTANT_JIT_RULE_2026",
                RuleName = "Quy tắc JIT thêm lúc Runtime",
                Priority = 1,
                AnchorSpec = new AegisQuiz.Application.DTOs.AnchorSpecDto
                {
                    RegexPattern = @"^#\s*(\d+)[\.\:\s\-]+",
                    QuestionNumberGroupIndex = 1,
                    DelimiterType = "REGEX_LINE_START"
                },
                InteractionSpec = new AegisQuiz.Application.DTOs.InteractionSpecDto
                {
                    DefaultQuestionType = "SINGLE",
                    OptionRegexPattern = @"^>>\s*([A-D])\s*<<\s*(.*)$",
                    OptionLabelGroupIndex = 1,
                    OptionContentGroupIndex = 2
                },
                EvaluationSpec = new AegisQuiz.Application.DTOs.EvaluationSpecDto
                {
                    AnswerRegexPattern = @"^FLAG[\.\:\s\-]+([A-D])",
                    AnswerGroupIndex = 1
                }
            };

            engine.AddOrUpdateRule(customRule, persistToFile: false);

            var text = @"
# 99. Thử nghiệm JIT Rule tức thời không downtime?
>>A<< Thành công rực rỡ
>>B<< Thất bại
FLAG: A
";
            var parseRes = engine.ParseSnippet(text);
            Assert.True(parseRes.Success);
            Assert.Equal("INSTANT_JIT_RULE_2026", parseRes.MatchedRuleCode);
            Assert.Equal(1, parseRes.TotalQuestionsFound);
            Assert.Equal(2, parseRes.Questions[0].Options.Count);
            Assert.Equal("A", parseRes.Questions[0].SuggestedAnswer);

            _output.WriteLine("[PASS] AddOrUpdateRule integrated instantly and parsed successfully!");
        }

        // ============================================================
        // GIAI ĐOẠN 5: TOÀN CẦU HÓA & ĐA NGỮ HỌC THUẬT QUỐC TẾ
        // ============================================================

        [Fact]
        public void Phase5_GlobalRegistry_ShouldHaveAllInternationalRules()
        {
            var engine = new DynamicQuestionIngestionEngine();
            var rules = engine.GetAllRules();

            _output.WriteLine($"[Phase5] Tổng số quy tắc CIG v2.0: {rules.Count}");
            foreach (var r in rules)
                _output.WriteLine($"  [{r.Language?.ToUpper() ?? "?"}] [{r.RuleCode}] {r.RuleName}");

            // Kiểm tra 5 bộ quy tắc quốc tế mới
            Assert.Contains(rules, r => r.RuleCode == "CHINA_GAOKAO_STANDARD");
            Assert.Contains(rules, r => r.RuleCode == "KOREA_SUNEUNG_STANDARD");
            Assert.Contains(rules, r => r.RuleCode == "JAPAN_JCEE_STANDARD");
            Assert.Contains(rules, r => r.RuleCode == "FRANCE_BACCALAUREAT_STANDARD");
            Assert.Contains(rules, r => r.RuleCode == "INTERNATIONAL_IELTS_TOEFL");

            // Tổng phải có đủ 10 bộ quy tắc (5 gốc + 5 quốc tế)
            Assert.True(rules.Count >= 10, $"Phải có ít nhất 10 quy tắc, thực tế chỉ có {rules.Count}");
            _output.WriteLine($"[PASS] Tất cả 5 bộ quy tắc quốc tế Giai đoạn 5 đã được nạp vào CIG Engine!");
        }

        [Fact]
        public void Phase5_ParseSnippet_Gaokao_ShouldExtractChineseExamFormat()
        {
            var engine = new DynamicQuestionIngestionEngine();
            // Gaokao: Cần dùng SpecificRuleCode vì anchor chứa ký tự CJK.
            // Trong thực tế, Admin sẽ chọn quy tắc Gaokao trước khi phân tích.
            var snippet = @"
第一部分  阅读理解（共两节，满分40分）

第1. 中国的首都是哪个城市？
A. 上海
B. 北京
C. 广州
D. 深圳
答案：B
解析：北京是中国的首都，也是政治、文化中心。
";
            // Gọi với specificRuleCode (đúng cách sử dụng trong môi trường thực tế)
            var result = engine.ParseSnippet(snippet, specificRuleCode: "CHINA_GAOKAO_STANDARD");
            _output.WriteLine($"[Gaokao] Matched Rule: {result.MatchedRuleCode} | Questions: {result.TotalQuestionsFound}");
            if (result.Questions.Count > 0)
            {
                _output.WriteLine($"  SuggestedAnswer: {result.Questions[0].SuggestedAnswer}");
                _output.WriteLine($"  Options Count: {result.Questions[0].Options.Count}");
            }
            Assert.True(result.Success, $"Gaokao format parsing failed. Logs: {string.Join("; ", result.ParsingLogs)}");
            Assert.Equal("CHINA_GAOKAO_STANDARD", result.MatchedRuleCode);
            Assert.Equal("B", result.Questions[0].SuggestedAnswer);
            _output.WriteLine("[PASS] Gaokao (高考) format parsed successfully with specificRuleCode!");
        }

        [Fact]
        public void Phase5_ParseSnippet_Suneung_ShouldExtractKoreanCircledNumberOptions()
        {
            var engine = new DynamicQuestionIngestionEngine();
            var snippet = @"
듣기

1. 한국의 수도는 어디입니까?
① 부산
② 서울
③ 인천
④ 대구
⑤ 광주
정답: ②
해설: 서울은 대한민국의 수도입니다.
";
            var result = engine.ParseSnippet(snippet);
            _output.WriteLine($"[Suneung] Matched Rule: {result.MatchedRuleCode} | Questions: {result.TotalQuestionsFound}");
            if (result.Questions.Count > 0)
            {
                _output.WriteLine($"  SuggestedAnswer: {result.Questions[0].SuggestedAnswer}");
                _output.WriteLine($"  Options Count: {result.Questions[0].Options.Count}");
            }
            Assert.True(result.Success, $"Suneung format parsing failed. Logs: {string.Join("; ", result.ParsingLogs)}");
            Assert.Equal("KOREA_SUNEUNG_STANDARD", result.MatchedRuleCode);
            _output.WriteLine("[PASS] Suneung (수능) Korean circled-number format parsed successfully!");
        }

        [Fact]
        public void Phase5_ParseSnippet_JapanJCEE_ShouldExtractKatakanaAndCircledOptions()
        {
            var engine = new DynamicQuestionIngestionEngine();
            var snippet = @"
第１問

問 1. 日本の首都はどこですか？
① 大阪
② 東京
③ 京都
④ 横浜
正解：②
解説：東京は日本の首都であり、政治・経済の中心地です。
";
            var result = engine.ParseSnippet(snippet);
            _output.WriteLine($"[JCEE] Matched Rule: {result.MatchedRuleCode} | Questions: {result.TotalQuestionsFound}");
            if (result.Questions.Count > 0)
                _output.WriteLine($"  SuggestedAnswer: {result.Questions[0].SuggestedAnswer}");
            Assert.True(result.Success, $"JCEE Japan format parsing failed. Logs: {string.Join("; ", result.ParsingLogs)}");
            Assert.Equal("JAPAN_JCEE_STANDARD", result.MatchedRuleCode);
            _output.WriteLine("[PASS] JCEE Japan (大学入学共通テスト) format parsed successfully!");
        }

        [Fact]
        public void Phase5_ParseSnippet_FranceBaccalaureat_ShouldExtractFrenchExamFormat()
        {
            var engine = new DynamicQuestionIngestionEngine();
            var snippet = @"
EXERCICE 1

1. Quelle est la capitale de la France ?
A. Lyon
B. Marseille
C. Paris
D. Bordeaux
Réponse: C
Explication: Paris est la capitale et la plus grande ville de France.
";
            // Trong thực tế Admin chọn specificRuleCode = Baccalauréat
            var result = engine.ParseSnippet(snippet, specificRuleCode: "FRANCE_BACCALAUREAT_STANDARD");
            _output.WriteLine($"[BAC] Matched Rule: {result.MatchedRuleCode} | Questions: {result.TotalQuestionsFound}");
            if (result.Questions.Count > 0)
            {
                _output.WriteLine($"  SuggestedAnswer: {result.Questions[0].SuggestedAnswer}");
                _output.WriteLine($"  Options Count: {result.Questions[0].Options.Count}");
            }
            Assert.True(result.Success, $"Baccalauréat format parsing failed. Logs: {string.Join("; ", result.ParsingLogs)}");
            Assert.Equal("FRANCE_BACCALAUREAT_STANDARD", result.MatchedRuleCode);
            Assert.Equal("C", result.Questions[0].SuggestedAnswer);
            _output.WriteLine("[PASS] Baccalauréat France format parsed successfully with specificRuleCode!");
        }

        [Fact]
        public void Phase5_ParseSnippet_IELTS_ShouldExtractReadingPassageQuestions()
        {
            var engine = new DynamicQuestionIngestionEngine();
            var snippet = @"
READING PASSAGE 1

Question 1. The author's main purpose in the first paragraph is to
A. describe a historical event
B. introduce a scientific theory
C. argue against a popular belief
D. summarize recent research
Answer: B
Explanation: The passage opens by presenting a new scientific theory about climate patterns.
";
            // IELTS/TOEFL: Dùng specificRuleCode vì anchor "Question N." cũng khớp với quy tắc tiêu chuẩn
            var result = engine.ParseSnippet(snippet, specificRuleCode: "INTERNATIONAL_IELTS_TOEFL");
            _output.WriteLine($"[IELTS] Matched Rule: {result.MatchedRuleCode} | Questions: {result.TotalQuestionsFound}");
            if (result.Questions.Count > 0)
            {
                _output.WriteLine($"  SuggestedAnswer: {result.Questions[0].SuggestedAnswer}");
                _output.WriteLine($"  Options Count: {result.Questions[0].Options.Count}");
            }
            Assert.True(result.Success, $"IELTS/TOEFL format parsing failed. Logs: {string.Join("; ", result.ParsingLogs)}");
            Assert.Equal("INTERNATIONAL_IELTS_TOEFL", result.MatchedRuleCode);
            Assert.Equal("B", result.Questions[0].SuggestedAnswer);
            _output.WriteLine("[PASS] IELTS/TOEFL Academic Reading format parsed successfully with specificRuleCode!");
        }

        [Fact]
        public void Phase5_ParseSnippet_GermanyAbitur_ShouldExtractQuestions()
        {
            var engine = new DynamicQuestionIngestionEngine();
            var snippet = @"
PFLICHTTEIL

Aufgabe 1. Welche Stadt ist die Hauptstadt der Bundesrepublik Deutschland?
A. München
B. Berlin
C. Hamburg
D. Frankfurt
Lösung: B
Erklärung: Berlin ist seit 1990 die Bundeshauptstadt der Bundesrepublik Deutschland.
";
            var result = engine.ParseSnippet(snippet);
            _output.WriteLine($"[Abitur] Matched Rule: {result.MatchedRuleCode} | Questions: {result.TotalQuestionsFound}");
            if (result.Questions.Count > 0)
            {
                _output.WriteLine($"  SuggestedAnswer: {result.Questions[0].SuggestedAnswer}");
                _output.WriteLine($"  Options Count: {result.Questions[0].Options.Count}");
            }
            Assert.True(result.Success, $"Abitur format parsing failed. Logs: {string.Join("; ", result.ParsingLogs)}");
            Assert.Equal("GERMANY_ABITUR_STANDARD", result.MatchedRuleCode);
            Assert.Equal("B", result.Questions[0].SuggestedAnswer);
            _output.WriteLine("[PASS] Germany Abitur format parsed successfully with auto-detected rule!");
        }

        [Fact]
        public void Phase5_ParseSnippet_CambridgeALevels_ShouldExtractQuestions()
        {
            var engine = new DynamicQuestionIngestionEngine();
            var snippet = @"
PAPER 1: MULTIPLE CHOICE

Question 1. Which of the following is an SI base unit?
(a) Newton
(b) Kelvin
(c) Joule
(d) Watt
Mark scheme: (b)
Guidance: Kelvin is the SI base unit for thermodynamic temperature.
";
            var result = engine.ParseSnippet(snippet, specificRuleCode: "CAMBRIDGE_A_LEVELS_STANDARD");
            _output.WriteLine($"[Cambridge] Matched Rule: {result.MatchedRuleCode} | Questions: {result.TotalQuestionsFound}");
            if (result.Questions.Count > 0)
            {
                _output.WriteLine($"  SuggestedAnswer: {result.Questions[0].SuggestedAnswer}");
                _output.WriteLine($"  Options Count: {result.Questions[0].Options.Count}");
            }
            Assert.True(result.Success, $"Cambridge A-Levels format parsing failed. Logs: {string.Join("; ", result.ParsingLogs)}");
            Assert.Equal("CAMBRIDGE_A_LEVELS_STANDARD", result.MatchedRuleCode);
            Assert.Equal("b", result.Questions[0].SuggestedAnswer);
            _output.WriteLine("[PASS] Cambridge International A-Levels format parsed successfully!");
        }
    }
}

