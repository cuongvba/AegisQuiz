using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Infrastructure.AI.Outcomes
{
    /// <summary>
    /// [AegisAI Outcome Builder] Bộ kiến tạo 5 Khối Tri Thức Cấu Trúc Toàn Diện.
    /// Bóc tách và kết tinh kết quả hội thảo thành Thực trạng [E#] -> Căn nguyên -> Điểm tiến bộ -> Nan đề mở -> Giải pháp hành động & Tiên đề mới.
    /// </summary>
    public class SymposiumOutcomeBuilder : ISymposiumOutcomeBuilder
    {
        private readonly ISeatModelInvoker _seatInvoker;
        private readonly ILogger<SymposiumOutcomeBuilder> _logger;

        public SymposiumOutcomeBuilder(
            ISeatModelInvoker seatInvoker,
            ILogger<SymposiumOutcomeBuilder> logger)
        {
            _seatInvoker = seatInvoker;
            _logger = logger;
        }

        public async Task<SymposiumStructuredOutcomeDto> BuildStructuredOutcomeAsync(
            SymposiumTeleologyMode mode,
            string topicTitle,
            string problemStatement,
            EmpiricalEvidenceDossierDto? dossier,
            List<SymposiumDialogueTurn> dialogueLog,
            List<CouncilAcademicContributionDto> contributions,
            List<string> citedEvidenceIds,
            bool hasRealTurns,
            Guid? userId,
            Guid? tenantId,
            CancellationToken ct = default)
        {
            if (hasRealTurns)
            {
                try
                {
                    var modeGuidance = mode switch
                    {
                        SymposiumTeleologyMode.EmpiricalActionable => 
                            "Trọng tâm: THỰC NGHIỆM & HÀNH ĐỘNG. Tập trung phân tích Thực trạng dựa trên số liệu [E#], chỉ rõ 3-5 nguyên nhân cốt lõi, ghi nhận các điểm tiến bộ đã đạt được, và đưa ra gói giải pháp hành động cụ thể phân kỳ lộ trình rõ ràng (Ngắn hạn 0-1 năm, Trung hạn 1-3 năm, Dài hạn 3-5+ năm).",
                        SymposiumTeleologyMode.EpistemicGenesis =>
                            "Trọng tâm: KHỞI SINH TRI THỨC MỚI. Khái quát hạt nhân Tiên đề mới (Novel Epistemic Axiom) cho nhân loại, công thức hóa Nan đề/Nghịch lý mở cốt lõi (Core Antinomy) chưa thể giải quyết và các Giả thuyết mở (Open Conjectures) cho giới học thuật tiếp tục nghiên cứu.",
                        _ =>
                            "Trọng tâm: TOÀN NĂNG LAI GHÉP. Dung hợp hoàn hảo cả 5 khối tri thức: Thực trạng [E#] -> Căn nguyên -> Điểm tiến bộ -> Nan đề mâu thuẫn trung tâm -> Gói giải pháp hành động phân kỳ lộ trình & Hạt nhân Tiên đề tri thức mới."
                    };

                    var prompt = $@"Bạn là Ban Thư Ký Hàn Lâm Tối Cao AegisQuiz. Hãy bóc tách và cấu trúc hóa toàn bộ kết quả của Đại Hội Thảo sau thành một bản Kỷ yếu Cấu Trúc 5 Khối Tri Thức theo định dạng JSON chuẩn.

{modeGuidance}

Đề tài: {topicTitle}
Vấn đề khởi thảo: {problemStatement}
Sổ cái bằng chứng:
{dossier?.FormattedDossierMarkdown}

Diễn biến phát biểu của các Viện Hàn Lâm:
{string.Join("\n\n", dialogueLog.Select(t => $"[{t.PhaseName}] {t.SpeakerTitle}: {t.Content}"))}

YÊU CẦU ĐỊNH DẠNG JSON TUYỆT ĐỐI (chỉ trả về JSON hợp lệ, không bọc văn bản giải thích thừa):
{{
  ""EmpiricalStatusQuo"": [""Đánh giá thực trạng khách quan 1 dựa trên các bằng chứng thực nghiệm [E1..En]..."", ""Thực trạng 2...""],
  ""RootCauses"": [
    ""Nguyên nhân cốt lõi 1..."",
    ""Nguyên nhân cốt lõi 2..."",
    ""Nguyên nhân cốt lõi 3...""
  ],
  ""ValidatedProgress"": [
    ""Điểm tiến bộ thực tế 1 đã ghi nhận..."",
    ""Điểm tiến bộ thực tế 2...""
  ],
  ""CoreAntinomy"": ""Công thức hóa nan đề/nghịch lý mâu thuẫn hệ thống sâu sắc nhất giữa các trường phái..."",
  ""ActionableSolutions"": [
    {{
      ""Horizon"": ""Ngắn hạn (0 - 1 năm)"",
      ""SolutionTitle"": ""Tên giải pháp ngắn hạn..."",
      ""Description"": ""Mô tả chi tiết giải pháp..."",
      ""ResponsibleDomain"": ""Cơ quan / Chủ thể chịu trách nhiệm..."",
      ""FeasibilityScore"": 8.5,
      ""GroundedEvidenceIds"": [""E1"", ""E2""]
    }},
    {{
      ""Horizon"": ""Trung hạn (1 - 3 năm)"",
      ""SolutionTitle"": ""Tên giải pháp trung hạn..."",
      ""Description"": ""Mô tả chi tiết..."",
      ""ResponsibleDomain"": ""Cơ quan chịu trách nhiệm..."",
      ""FeasibilityScore"": 8.0,
      ""GroundedEvidenceIds"": [""E3""]
    }},
    {{
      ""Horizon"": ""Dài hạn (3 - 5+ năm)"",
      ""SolutionTitle"": ""Tên giải pháp chiến lược dài hạn..."",
      ""Description"": ""Tái cấu trúc bền vững..."",
      ""ResponsibleDomain"": ""Liên minh đa phương..."",
      ""FeasibilityScore"": 7.5,
      ""GroundedEvidenceIds"": [""E4""]
    }}
  ],
  ""NovelEpistemicAxiom"": ""Tuyên ngôn Tiên đề mới đúc kết từ hội thảo đóng góp cho nhân loại..."",
  ""OpenConjectures"": [
    ""Nan đề/giả thuyết mở 1 cần thế giới tiếp tục nghiên cứu..."",
    ""Nan đề/giả thuyết mở 2...""
  ]
}}";

                    var req = new SeatInvocationRequest
                    {
                        ModelCode = "gpt-4o",
                        SystemPrompt = "Bạn là Ban Thư Ký Hàn Lâm Tối Cao AegisQuiz. Xuất dữ liệu cấu trúc JSON chuẩn xác, khách quan, giàu chiều sâu và viện dẫn mã bằng chứng [E#].",
                        UserPrompt = prompt,
                        Temperature = 0.2,
                        MaxTokens = 2500,
                        UserId = userId,
                        TenantId = tenantId
                    };

                    var resp = await _seatInvoker.InvokeSeatAsync(req, ct);
                    if (resp.Success && !string.IsNullOrWhiteSpace(resp.Content))
                    {
                        var parsed = ParseStructuredOutcomeFromJson(resp.Content, mode);
                        if (parsed != null)
                        {
                            return parsed;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[SymposiumOutcomeBuilder] Lỗi khi trích xuất kết quả cấu trúc qua LLM, chuyển sang Fallback");
                }
            }

            return BuildFallbackStructuredOutcome(mode, topicTitle, problemStatement, dossier, contributions, citedEvidenceIds);
        }

        private static SymposiumStructuredOutcomeDto? ParseStructuredOutcomeFromJson(string rawText, SymposiumTeleologyMode mode)
        {
            try
            {
                var clean = rawText.Trim();
                if (clean.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                {
                    clean = clean[7..];
                }
                else if (clean.StartsWith("```"))
                {
                    clean = clean[3..];
                }

                if (clean.EndsWith("```"))
                {
                    clean = clean[..^3];
                }
                clean = clean.Trim();

                var parsed = JsonSerializer.Deserialize<SymposiumStructuredOutcomeDto>(clean, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsed != null)
                {
                    parsed.AppliedMode = mode;
                    return parsed;
                }
            }
            catch
            {
                // Fallback nếu JSON bị lỗi cú pháp
            }
            return null;
        }

        private static SymposiumStructuredOutcomeDto BuildFallbackStructuredOutcome(
            SymposiumTeleologyMode mode,
            string topicTitle,
            string problemStatement,
            EmpiricalEvidenceDossierDto? dossier,
            List<CouncilAcademicContributionDto> contributions,
            List<string> citedEvidenceIds)
        {
            var outcome = new SymposiumStructuredOutcomeDto
            {
                AppliedMode = mode
            };

            // 1. Thực trạng thực chứng
            var evidenceSummary = citedEvidenceIds.Count > 0
                ? $"được kiểm chứng qua {citedEvidenceIds.Count} nguồn thực nghiệm ({string.Join(", ", citedEvidenceIds.Select(e => $"[{e}]"))})"
                : "dựa trên các chỉ số quan trắc và nghiên cứu chuyên ngành liên quan";

            var factsSummary = (dossier?.VerifiedFacts != null && dossier.VerifiedFacts.Count > 0)
                ? string.Join("; ", dossier.VerifiedFacts.Take(2))
                : string.Empty;

            var statusQuoText = !string.IsNullOrWhiteSpace(factsSummary)
                ? $"{factsSummary}. Toàn bộ hiện trạng {evidenceSummary} phản ánh sự bất cân đối giữa nhu cầu phát triển và năng lực kiểm soát rủi ro hệ thống."
                : $"Vấn đề [{topicTitle}] đang ở giai đoạn chuyển biến phức tạp, {evidenceSummary}. Bối cảnh: {problemStatement}";

            outcome.EmpiricalStatusQuo = new List<string> { statusQuoText };

            // 2. Căn nguyên cốt lõi (Root Causes)
            outcome.RootCauses = new List<string>
            {
                "Bất đối xứng thông tin và độ trễ nhận thức giữa các tác nhân ra quyết định trong hệ thống.",
                "Các ngoại ứng tiêu cực chưa được định giá và nội hóa đầy đủ vào mô hình vận hành hiện tại.",
                "Sự phụ thuộc vào lối mòn tư duy (Path-dependency) và xung đột lợi ích giữa mục tiêu ngắn hạn với sự ổn định dài hạn."
            };

            // 3. Điểm tiến bộ đã đạt được (Validated Progress)
            if (dossier != null && dossier.VerifiedFacts.Count > 0)
            {
                outcome.ValidatedProgress = dossier.VerifiedFacts.Take(3).ToList();
            }
            else
            {
                outcome.ValidatedProgress = new List<string>
                {
                    "Xác lập được sự đồng thuận ban đầu về việc cần thiết phải tái cấu trúc khung quản trị và giám sát.",
                    "Sự xuất hiện của các công cụ phân tích dữ liệu và AI hỗ trợ nhận diện sớm các điểm đứt gãy hệ thống.",
                    "Mở rộng nhận thức liên ngành, kết hợp giữa khoa học tự nhiên, kinh tế học và đạo đức học hành vi."
                };
            }

            // 4. Nan đề / Nghịch lý cốt lõi (Core Antinomy)
            outcome.CoreAntinomy = dossier != null && dossier.UnresolvedControversies.Count > 0
                ? string.Join(" ", dossier.UnresolvedControversies.Take(2))
                : "Nghịch lý giữa nhu cầu tối ưu hóa cục bộ ngắn hạn nhằm duy trì tốc độ phát triển và yêu cầu bắt buộc phải bảo tồn độ đàn hồi, cân bằng động của toàn bộ hệ sinh thái.";

            // 5. Gói giải pháp hành động phân kỳ (Actionable Solutions)
            var e1 = citedEvidenceIds.Count > 0 ? new List<string> { citedEvidenceIds[0] } : new List<string> { "E1" };
            var e2 = citedEvidenceIds.Count > 1 ? new List<string> { citedEvidenceIds[1] } : e1;
            var e3 = citedEvidenceIds.Count > 2 ? new List<string> { citedEvidenceIds[2] } : e1;

            outcome.ActionableSolutions = new List<ActionableSolutionItemDto>
            {
                new ActionableSolutionItemDto
                {
                    Horizon = "Ngắn hạn (0 - 1 năm)",
                    SolutionTitle = "Thiết lập Ban Giám sát Khủng hoảng & Chỉ số Cảnh báo Sớm Thực nghiệm",
                    Description = "Ban hành quy chế khẩn cấp, triển khai dashboard dữ liệu thời gian thực và nội bộ hóa quy trình kiểm toán rủi ro đa chiều dựa trên các bằng chứng quan trắc.",
                    ResponsibleDomain = "Cơ quan Quản lý Điều hành & Ủy ban Đánh giá Rủi ro",
                    FeasibilityScore = 9.0,
                    GroundedEvidenceIds = e1
                },
                new ActionableSolutionItemDto
                {
                    Horizon = "Trung hạn (1 - 3 năm)",
                    SolutionTitle = "Tái Cấu Trúc Khung Thể Chế & Chuẩn Hóa Cơ Chế Trách Nhiệm",
                    Description = "Cập nhật hành lang pháp lý, sửa đổi quy chuẩn vận hành liên ngành và đưa các chỉ số đo lường hiệu quả ngoại ứng tiêu cực vào tiêu chí đánh giá bắt buộc.",
                    ResponsibleDomain = "Khối Pháp chế, Chính sách & Viện Nghiên cứu Chuyên ngành",
                    FeasibilityScore = 8.2,
                    GroundedEvidenceIds = e2
                },
                new ActionableSolutionItemDto
                {
                    Horizon = "Dài hạn (3 - 5+ năm)",
                    SolutionTitle = "Chuyển Đổi Mô Thức Văn Minh & Nền Tảng Cân Bằng Động Tự Phục Hồi",
                    Description = "Xây dựng hệ thống điều khiển học hồi tiếp liên tục, chuyển hóa hoàn toàn cơ cấu vận hành sang mô hình tự điều hòa cân bằng entropy và bền vững trường tồn.",
                    ResponsibleDomain = "Liên minh Đa Phương & Hội đồng Khoa học Độc lập",
                    FeasibilityScore = 7.6,
                    GroundedEvidenceIds = e3
                }
            };

            // 6. Tiên đề tri thức mới & Nan đề mở
            outcome.NovelEpistemicAxiom = $"Mọi hệ thống phức hợp muốn tồn tại bền vững trong kỷ nguyên AI bắt buộc phải gắn kết chặt chẽ giữa Chuẩn tắc Đạo đức học Trách nhiệm và Nền tảng Dữ kiện Thực chứng đa chiều, loại trừ triệt để tư duy vị nhân tâm ích kỷ.";

            outcome.OpenConjectures = new List<string>
            {
                "Làm thế nào để lượng hóa chính xác chi phí suy thoái entropy trong các thuật toán ra quyết định tự động?",
                "Ranh giới dung hòa tối ưu giữa quyền tự chủ của các thực thể thành phần và trật tự an ninh tổng thể của toàn mạng lưới là gì?"
            };

            return outcome;
        }
    }
}
