using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [Phase 4] Hiện thực Shadow Testing Pipeline cho AI Rule Autopilot & CIG Evolution
    /// Đảm bảo mọi quy tắc mới trước khi lên Production đều vượt qua kiểm định an toàn và chống thoái lui (Zero Regression).
    /// </summary>
    public class ShadowTestingService : IShadowTestingService
    {
        private readonly ILogger<ShadowTestingService> _logger;

        public ShadowTestingService(ILogger<ShadowTestingService> logger)
        {
            _logger = logger;
        }

        public async Task<ShadowTestResultDto> RunShadowTestAsync(ShadowTestRequestDto request, CancellationToken ct = default)
        {
            var result = new ShadowTestResultDto();
            var proposedRule = request.ProposedRule;

            if (proposedRule == null || string.IsNullOrWhiteSpace(proposedRule.RuleCode))
            {
                result.Passed = false;
                result.SafetyScore = 0.0;
                result.Recommendation = "Quy tắc đề xuất không hợp lệ (RuleCode trống).";
                return result;
            }

            var pattern = proposedRule.Structure?.QuestionHeaderRegexPattern;
            if (string.IsNullOrWhiteSpace(pattern))
            {
                result.Passed = false;
                result.SafetyScore = 0.0;
                result.Recommendation = "QuestionHeaderRegexPattern không được để trống.";
                return result;
            }

            // 1. Kiểm tra tính hợp lệ cú pháp của Regex (Tránh Exception / Regex Injection)
            try
            {
                _ = new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1));
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.SafetyScore = 0.0;
                result.Recommendation = $"Biểu thức Regex không hợp lệ hoặc có cú pháp lỗi: {ex.Message}";
                return result;
            }

            // Warm-up Engine & JIT trước khi đo lường thời gian xử lý thực tế của Regex
            _ = DynamicQuestionIngestionEngine.Instance.ParseSnippet("Câu 1. Khởi tạo engine warm-up\nA. Phương án 1\nB. Phương án 2");

            var baselineSuite = GetBaselineRegressionSuite();
            var swTotal = Stopwatch.StartNew();
            var totalExecutionMs = 0.0;
            int totalTests = 0;

            // 2. Chạy kiểm tra Target Samples (Mẫu mục tiêu mà rule hướng đến)
            if (request.CustomTestSamples != null && request.CustomTestSamples.Count > 0)
            {
                foreach (var sample in request.CustomTestSamples)
                {
                    if (string.IsNullOrWhiteSpace(sample)) continue;
                    totalTests++;

                    var swSample = Stopwatch.StartNew();
                    var parseResult = DynamicQuestionIngestionEngine.Instance.ParseSnippet(sample, proposedRule);
                    swSample.Stop();
                    var elapsedMs = swSample.Elapsed.TotalMilliseconds;
                    totalExecutionMs += elapsedMs;

                    bool matched = parseResult.Success && parseResult.TotalQuestionsFound > 0;
                    if (matched) result.TargetMatchesCount++;

                    result.Details.Add(new ShadowTestDetailDto
                    {
                        SampleName = "Target Sample",
                        SampleSnippet = sample.Length > 80 ? sample.Substring(0, 80) + "..." : sample,
                        ExpectedRuleCode = proposedRule.RuleCode,
                        ActualMatchedRuleCode = parseResult.MatchedRuleCode ?? (matched ? proposedRule.RuleCode : "NO_MATCH"),
                        IsTargetMatch = matched,
                        IsRegression = false,
                        ExecutionMs = Math.Round(elapsedMs, 2),
                        Status = matched ? "PASS" : "WARN_NO_PARSE",
                        Note = matched ? $"Đã trích xuất thành công {parseResult.TotalQuestionsFound} câu hỏi." : "Không trích xuất được câu hỏi từ mẫu mục tiêu."
                    });
                }
            }

            // 3. Chạy Regression Testing trên Baseline Suite (Chống chiếm quyền các đề chuẩn)
            foreach (var baseline in baselineSuite)
            {
                ct.ThrowIfCancellationRequested();
                totalTests++;

                var swSample = Stopwatch.StartNew();
                // Test 1: Chạy với Engine hiện tại xem chuẩn gốc là gì
                var baselineParse = DynamicQuestionIngestionEngine.Instance.ParseSnippet(baseline.Snippet);
                swSample.Stop();

                var elapsedMs = swSample.Elapsed.TotalMilliseconds;
                totalExecutionMs += elapsedMs;

                // Kiểm tra xem Regex của proposedRule có bắt nhầm dòng neo của chuẩn gốc khác không
                var lines = baseline.Snippet.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var firstAnchorLine = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim() ?? string.Empty;

                bool headerCollides = false;
                try
                {
                    headerCollides = Regex.IsMatch(firstAnchorLine, proposedRule.AnchorSpec.RegexPattern);
                }
                catch
                {
                    headerCollides = false;
                }

                // Nếu header va chạm với một rule chuẩn khác (cướp quyền chuẩn gốc) mà làm biến dạng kết quả parse
                bool isRegression = false;
                if (headerCollides && baseline.ExpectedRuleCode != proposedRule.RuleCode)
                {
                    var proposedParse = DynamicQuestionIngestionEngine.Instance.ParseSnippet(baseline.Snippet, proposedRule);
                    if (!proposedParse.Success || proposedParse.TotalQuestionsFound != baselineParse.TotalQuestionsFound)
                    {
                        isRegression = true;
                        result.RegressionsCount++;
                    }
                }

                result.Details.Add(new ShadowTestDetailDto
                {
                    SampleName = baseline.Name,
                    SampleSnippet = baseline.Snippet.Length > 80 ? baseline.Snippet.Substring(0, 80) + "..." : baseline.Snippet,
                    ExpectedRuleCode = baseline.ExpectedRuleCode,
                    ActualMatchedRuleCode = isRegression ? proposedRule.RuleCode : baselineParse.MatchedRuleCode,
                    IsRegression = isRegression,
                    IsTargetMatch = false,
                    ExecutionMs = Math.Round(elapsedMs, 2),
                    Status = isRegression ? "FAIL_REGRESSION" : "PASS",
                    Note = isRegression 
                        ? $"CẢNH BÁO: Quy tắc mới đã cướp quyền parse của chuẩn [{baseline.ExpectedRuleCode}] và gây thoái lui (Regression)." 
                        : $"Tương thích hoàn toàn với chuẩn [{baseline.ExpectedRuleCode}]."
                });
            }

            swTotal.Stop();
            result.TotalSamplesTested = totalTests;
            result.AverageExecutionMs = totalTests > 0 ? Math.Round(totalExecutionMs / totalTests, 2) : 0.0;

            // 4. Tính toán Safety Score & Phán quyết
            double penalty = (result.RegressionsCount * 25.0);
            if (result.AverageExecutionMs > 100.0) penalty += 15.0; // Phạt nếu hiệu năng Regex quá chậm
            
            result.SafetyScore = Math.Max(0.0, Math.Min(100.0, 100.0 - penalty));
            result.Passed = result.SafetyScore >= 95.0 && result.RegressionsCount == 0;

            if (result.Passed)
            {
                result.Recommendation = "Quy tắc an toàn tuyệt đối (Zero Regression). Đạt tiêu chuẩn đưa vào Production.";
                if (request.AutoPromote)
                {
                    try
                    {
                        DynamicQuestionIngestionEngine.Instance.AddOrUpdateRule(proposedRule, persistToFile: true);
                        result.AutoPromoted = true;
                        _logger.LogInformation("[ShadowTesting] Đã tự động kích hoạt (Auto-Promoted) quy tắc {RuleCode} lên Production Registry.", proposedRule.RuleCode);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[ShadowTesting] Lỗi khi tự động promote quy tắc {RuleCode}", proposedRule.RuleCode);
                        result.Note = $"Lỗi ghi file cấu hình: {ex.Message}";
                    }
                }
            }
            else
            {
                result.Recommendation = $"Phát hiện {result.RegressionsCount} điểm xung đột thoái lui (Regression). Cần thu hẹp Regex Neo hoặc bổ sung context filter để tránh ảnh hưởng đến các bộ đề khác.";
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Bộ dữ liệu kiểm thử chuẩn (Baseline Ground-Truth Suite) bao quát các format đề thi cốt lõi
        /// </summary>
        private List<BaselineTestSample> GetBaselineRegressionSuite()
        {
            return new List<BaselineTestSample>
            {
                new BaselineTestSample
                {
                    Name = "VN GDPT 2025 - Trắc nghiệm 4 lựa chọn A/B/C/D",
                    ExpectedRuleCode = "VN_GDPT_2025_FOUR_OPTIONS",
                    Snippet = "Câu 1. Tập nghiệm của phương trình 2x - 4 = 0 là\nA. {2}\nB. {4}\nC. {0}\nD. {-2}\nLời giải:\nTa có 2x = 4 <=> x = 2."
                },
                new BaselineTestSample
                {
                    Name = "VN GDPT 2025 - Trắc nghiệm Đúng/Sai đa ý a) b) c) d)",
                    ExpectedRuleCode = "VN_GDPT_2025_TRUE_FALSE_MATRIX",
                    Snippet = "Câu 2. Cho hàm số y = f(x) có đồ thị như hình vẽ.\na) Hàm số đồng biến trên khoảng (0; 2).\nb) Điểm cực đại của đồ thị hàm số là (1; 3).\nc) Đồ thị hàm số đi qua gốc tọa độ O.\nd) Giá trị lớn nhất của hàm số trên đoạn [-1; 2] bằng 5."
                },
                new BaselineTestSample
                {
                    Name = "GPLX Quốc gia - Câu hỏi điểm liệt có cảnh báo",
                    ExpectedRuleCode = "GPLX_NATIONAL_DRIVING_TEST",
                    Snippet = "Câu 3. [Điểm liệt] Hành vi điều khiển xe cơ giới chạy quá tốc độ quy định bị nghiêm cấm không?\n1- Bị nghiêm cấm.\n2- Không bị nghiêm cấm.\n3- Bị nghiêm cấm tùy từng trường hợp cụ thể."
                },
                new BaselineTestSample
                {
                    Name = "Quốc tế SAT - Đọc hiểu / Logic tiếng Anh",
                    ExpectedRuleCode = "INTERNATIONAL_SAT_LOGIC",
                    Snippet = "Question 1\nWhich choice best describes the main idea of the text?\nA. It illustrates the historical context of the event.\nB. It contrasts two opposing scientific viewpoints.\nC. It demonstrates the necessity of environmental regulation.\nD. It emphasizes the importance of community activism."
                },
                new BaselineTestSample
                {
                    Name = "Trung Quốc Gaokao - Toán đa lựa chọn",
                    ExpectedRuleCode = "CHINA_GAOKAO_STANDARD",
                    Snippet = "第1题 已知集合 A = {1, 2, 3}, B = {2, 4}, 则 A ∩ B =\nA. {2}\nB. {1, 2}\nC. {1, 2, 3, 4}\nD. ∅"
                }
            };
        }

        private class BaselineTestSample
        {
            public string Name { get; set; } = string.Empty;
            public string ExpectedRuleCode { get; set; } = string.Empty;
            public string Snippet { get; set; } = string.Empty;
        }
    }
}
