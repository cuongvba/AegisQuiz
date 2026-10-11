using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Infrastructure.AI.Selection
{
    /// <summary>
    /// [AegisAI Fast Epistemic Intent Router - AIER v2.0]
    /// Bộ định tuyến nhận thức tối thượng:
    /// 1. Sử dụng Fast LLM phân tích bản thể học (Ontology Extraction) & khử đa nghĩa (Polysemy Elimination).
    /// 2. Đo khoảng trống tri thức (KnowledgeGapScore) để kích hoạt Tự Sinh Viện Mới (Genesis Engine).
    /// 3. Tuyển chọn Viện Đối Trọng Biện Chứng Động (Dialectical Contrarian) kèm luận giải khoa học.
    /// 4. Cơ chế Resilience & Fallback In-Memory: Tự động chạy thuật toán ngữ nghĩa nội tại nếu mạng/API quota gặp sự cố.
    /// </summary>
    public class FastEpistemicIntentRouter : IEpistemicIntentRouter
    {
        private readonly ISeatModelInvoker? _seatInvoker;
        private readonly ILogger<FastEpistemicIntentRouter> _logger;

        public FastEpistemicIntentRouter(
            ILogger<FastEpistemicIntentRouter> logger,
            ISeatModelInvoker? seatInvoker = null)
        {
            _logger = logger;
            _seatInvoker = seatInvoker;
        }

        public async Task<EpistemicRoutingIntent> RouteEpistemicIntentAsync(
            string topicTitle,
            string coreProblem,
            List<UniversalAiCouncil> existingCouncils,
            Guid? tenantId = null,
            CancellationToken ct = default)
        {
            var combinedText = $"{topicTitle} {coreProblem}".Trim();
            if (string.IsNullOrWhiteSpace(combinedText))
            {
                return new EpistemicRoutingIntent
                {
                    PrimaryDomain = "GENERAL",
                    RecommendedLeadCouncilCode = existingCouncils.FirstOrDefault()?.CouncilCode
                };
            }

            // 1. Thử phân tích bằng LLM siêu tốc (Deep Epistemic Cognitive Arbiter)
            if (_seatInvoker != null)
            {
                try
                {
                    var llmIntent = await AnalyzeWithFastLlmAsync(topicTitle, coreProblem, existingCouncils, tenantId, ct);
                    if (llmIntent != null)
                    {
                        _logger.LogInformation("[AIER-v2] 🧠 Phân giải ý định nhận thức thành công qua Fast LLM: PrimaryDomain={Domain}, Lead={Lead}, Contrarian={Contrarian}, GapScore={Gap:F2}, Novel={IsNovel}",
                            llmIntent.PrimaryDomain, llmIntent.RecommendedLeadCouncilCode, llmIntent.RecommendedContrarianCouncilCode, llmIntent.KnowledgeGapScore, llmIntent.IsNovelDomain);
                        return llmIntent;
                    }
                }
                catch (Exception exLlm)
                {
                    _logger.LogWarning(exLlm, "[AIER-v2] Fast LLM phân giải ý định gặp sự cố hoặc timeout, tự động kích hoạt Bộ Phân Luồng Ngữ Nghĩa Nội Tại Tối Ưu (Zero-Fallout Resilience).");
                }
            }

            // 2. Fallback: Bộ Phân Luồng Bản Thể Học Ngữ Nghĩa Nội Tại (Semantic Ontology Heuristics)
            return AnalyzeWithSemanticOntologyHeuristics(topicTitle, coreProblem, existingCouncils);
        }

        private async Task<EpistemicRoutingIntent?> AnalyzeWithFastLlmAsync(
            string topicTitle,
            string coreProblem,
            List<UniversalAiCouncil> existingCouncils,
            Guid? tenantId,
            CancellationToken ct)
        {
            var councilCatalog = string.Join("\n", existingCouncils.Select(c => 
                $"- Code: {c.CouncilCode} | Name: {c.DisplayName} | Discipline: {c.DisciplineArea} | Taxonomy: {c.MaterializedTaxonomyPath}"));

            var systemPrompt = 
@"Bạn là No.1 — Giám Đốc Phân Luồng Nhận Thức Tối Cao (Epistemic Intent Arbiter) của AegisQuiz.
Nhiệm vụ của bạn là giải mã bản thể học sâu sắc của đề tài hội thảo khoa học, khử bỏ hoàn toàn các bẫy đa nghĩa (ví dụ: 'vốn gen' là Sinh học chứ không phải Kinh tế tài chính; 'rối lượng tử' là Vật lý chứ không phải Tâm lý học).

Yêu cầu xuất ra JSON duy nhất theo schema sau:
{
  ""primaryDomain"": ""Mã định danh miền bản thể học cốt lõi (VD: QUANTUM_PHYSICS, MOLECULAR_BIOLOGY, ARCHAEOLOGY, AGRO_ECOLOGY, FINANCIAL_ECONOMICS, PHILOSOPHY)"",
  ""coreEpistemicConcepts"": [""khái niệm 1"", ""khái niệm 2"", ""khái niệm 3""],
  ""recommendedLeadCouncilCode"": ""Mã CouncilCode phù hợp nhất trong danh mục để làm Viện Chủ Trì (hoặc null nếu là miền tri thức hoàn toàn mới)"",
  ""recommendedContrarianCouncilCode"": ""Mã CouncilCode phù hợp nhất trong danh mục để làm Viện Đối Trọng Biện Chứng phản biện đa chiều"",
  ""dialecticalReason"": ""Luận giải ngắn gọn vì sao Viện Đối Trọng này tạo ra góc nhìn phản biện biện chứng sâu sắc cho đề tài"",
  ""knowledgeGapScore"": 0.15, // Số thực từ 0.0 (rất khớp với Viện hiện có) đến 1.0 (hoàn toàn mới lạ, chưa có Viện nào nghiên cứu)
  ""isNovelDomain"": false, // true nếu knowledgeGapScore >= 0.70 và cần Tự Sinh Viện Mới
  ""suggestedGenesis"": { // Chỉ điền khi isNovelDomain = true, ngược lại để null
    ""proposedCouncilCode"": ""MÃ_VIỆN_MỚI"",
    ""proposedDisplayName"": ""Tên đầy đủ của Viện mới"",
    ""disciplineArea"": ""Mô tả chuyên môn sâu"",
    ""materializedTaxonomyPath"": ""/PHÂN_LOẠI/TIỂU_NGÀNH"",
    ""epistemicRationale"": ""Tuyên ngôn sứ mệnh nhận thức luận""
  }
}";

            var userPrompt = 
$@"ĐỀ TÀI HỘI THẢO:
Tiêu đề: {topicTitle}
Vấn đề cốt lõi: {coreProblem}

DANH MỤC VIỆN HÀN LÂM HIỆN CÓ:
{councilCatalog}

Hãy phân giải ý định nhận thức và xuất JSON thuần túy, không kèm markdown fence.";

            var request = new SeatInvocationRequest
            {
                ModelCode = "gemini-2.0-flash", // Hoặc model mặc định siêu tốc
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                Temperature = 0.1,
                MaxTokens = 1000,
                TenantId = tenantId
            };

            var response = await _seatInvoker!.InvokeSeatAsync(request, ct);
            if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
            {
                return null;
            }

            var cleanJson = CleanJsonOutput(response.Content);
            var parsed = JsonSerializer.Deserialize<EpistemicRoutingIntent>(cleanJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return parsed;
        }

        private EpistemicRoutingIntent AnalyzeWithSemanticOntologyHeuristics(
            string topicTitle,
            string coreProblem,
            List<UniversalAiCouncil> existingCouncils)
        {
            var text = $"{topicTitle} {coreProblem}".ToLowerInvariant();
            var intent = new EpistemicRoutingIntent();

            // 1. Khử bẫy đa nghĩa và nhận diện Miền Bản Thể Học Cốt Lõi
            if (ContainsAny(text, "gen", "adn", "rna", "di truyền", "sinh học", "sinh vật", "hệ sinh thái", "loài", "tuyệt chủng", "rừng ngập mặn", "crispr"))
            {
                intent.PrimaryDomain = "LIFE_SCIENCES";
                intent.CoreEpistemicConcepts = new List<string> { "vốn gen", "đa dạng sinh thái", "bảo tồn di truyền" };
                intent.RecommendedLeadCouncilCode = existingCouncils.FirstOrDefault(c => c.CouncilCode == "LIFE_SCIENCES")?.CouncilCode;
                intent.RecommendedContrarianCouncilCode = existingCouncils.FirstOrDefault(c => c.CouncilCode == "ECONOMICS")?.CouncilCode
                    ?? existingCouncils.FirstOrDefault(c => c.CouncilCode == "PHILOSOPHY")?.CouncilCode;
                intent.DialecticalReason = "Xem xét bài toán bảo tồn sinh học dưới lăng kính chi phí kinh tế, ngoại ứng sinh thái và triết học đạo đức môi trường.";
                intent.KnowledgeGapScore = 0.1;
                intent.IsNovelDomain = false;
            }
            else if (ContainsAny(text, "lượng tử", "quantum", "epr", "hạt", "hạt cơ bản", "vật lý", "tương đối", "vũ trụ", "hố đen", "sóng hấp dẫn", "higgs"))
            {
                intent.PrimaryDomain = "QUANTUM_PHYSICS";
                intent.CoreEpistemicConcepts = new List<string> { "rối lượng tử", "nghịch lý epr", "hàm sóng", "vật lý cơ bản" };
                intent.RecommendedLeadCouncilCode = existingCouncils.FirstOrDefault(c => c.CouncilCode == "COSMOLOGY" || c.CouncilCode == "MATHEMATICS")?.CouncilCode;
                intent.RecommendedContrarianCouncilCode = existingCouncils.FirstOrDefault(c => c.CouncilCode == "PHILOSOPHY")?.CouncilCode;
                intent.DialecticalReason = "Khảo sát tính hiện thực (Realism) và tính phi cục bộ (Non-locality) từ góc độ Bản thể luận và Nhận thức luận triết học.";
                intent.KnowledgeGapScore = 0.15;
                intent.IsNovelDomain = false;
            }
            else if (ContainsAny(text, "khảo cổ", "khai quật", "cổ vật", "nền văn minh", "hóa thạch", "tiền sử", "lăng mộ", "di chỉ"))
            {
                intent.PrimaryDomain = "ARCHAEOLOGY";
                intent.CoreEpistemicConcepts = new List<string> { "khảo cổ học", "di chỉ lịch sử", "niên đại học" };
                intent.RecommendedLeadCouncilCode = existingCouncils.FirstOrDefault(c => c.CouncilCode == "ARCHAEOLOGY")?.CouncilCode;
                intent.RecommendedContrarianCouncilCode = existingCouncils.FirstOrDefault(c => c.CouncilCode == "COSMOLOGY" || c.CouncilCode == "LIFE_SCIENCES")?.CouncilCode;
                intent.DialecticalReason = "Đối chiếu bằng chứng di chỉ lịch sử với phương pháp định tuổi phóng xạ và địa tầng học tự nhiên.";
                
                var archCouncil = existingCouncils.FirstOrDefault(c => c.CouncilCode == "ARCHAEOLOGY");
                if (archCouncil == null)
                {
                    intent.KnowledgeGapScore = 0.85;
                    intent.IsNovelDomain = true;
                    intent.SuggestedGenesis = new SuggestedGenesisMetadata
                    {
                        ProposedCouncilCode = "ARCHAEOLOGY",
                        ProposedDisplayName = "Viện Khảo Cổ Học & Lịch Sử Các Nền Văn Minh",
                        DisciplineArea = "Khảo cổ học thực nghiệm, Niên đại học phóng xạ, Lịch sử cổ đại & Di sản nhân loại",
                        MaterializedTaxonomyPath = "/HUMANITY/ARCHAEOLOGY/HISTORY",
                        EpistemicRationale = "Khảo cổ học là chiếc chìa khóa khai mở nguồn gốc văn minh nhân loại qua các tầng địa chất và di vật vật chất."
                    };
                }
                else
                {
                    intent.KnowledgeGapScore = 0.1;
                    intent.IsNovelDomain = false;
                }
            }
            else if (ContainsAny(text, "kinh tế", "lạm phát", "tiền tệ", "tài chính", "ngân hàng", "gdp", "thị trường", "chứng khoán", "lãi suất"))
            {
                intent.PrimaryDomain = "ECONOMICS";
                intent.CoreEpistemicConcepts = new List<string> { "kinh tế vĩ mô", "chính sách tiền tệ", "thị trường tài chính" };
                intent.RecommendedLeadCouncilCode = existingCouncils.FirstOrDefault(c => c.CouncilCode == "ECONOMICS")?.CouncilCode;
                intent.RecommendedContrarianCouncilCode = existingCouncils.FirstOrDefault(c => c.CouncilCode == "MATHEMATICS")?.CouncilCode
                    ?? existingCouncils.FirstOrDefault(c => c.CouncilCode == "PHILOSOPHY")?.CouncilCode;
                intent.DialecticalReason = "Kiểm chứng các mô hình kinh tế vi mô và vĩ mô bằng phương pháp xác suất thống kê và lý thuyết trò chơi toán học.";
                intent.KnowledgeGapScore = 0.1;
                intent.IsNovelDomain = false;
            }
            else if (ContainsAny(text, "trí tuệ nhân tạo", "ai", "mạng nơ-ron", "machine learning", "deep learning", "agi", "llm", "transformer"))
            {
                intent.PrimaryDomain = "ARTIFICIAL_INTELLIGENCE";
                intent.CoreEpistemicConcepts = new List<string> { "học sâu", "trí tuệ nhân tạo", "siêu trí tuệ" };
                intent.RecommendedLeadCouncilCode = existingCouncils.FirstOrDefault(c => c.CouncilCode == "AGI_CYBERNETICS")?.CouncilCode;
                intent.RecommendedContrarianCouncilCode = existingCouncils.FirstOrDefault(c => c.CouncilCode == "PHILOSOPHY")?.CouncilCode
                    ?? existingCouncils.FirstOrDefault(c => c.CouncilCode == "NEUROSCIENCE")?.CouncilCode;
                intent.DialecticalReason = "Đánh giá rủi ro an toàn AGI, ý thức nhân tạo và vấn đề căn chỉnh giá trị con người (AI Alignment).";
                intent.KnowledgeGapScore = 0.1;
                intent.IsNovelDomain = false;
            }
            else
            {
                // Miền tổng quát
                intent.PrimaryDomain = "INTERDISCIPLINARY";
                intent.RecommendedLeadCouncilCode = existingCouncils.FirstOrDefault()?.CouncilCode;
                intent.RecommendedContrarianCouncilCode = existingCouncils.Skip(1).FirstOrDefault()?.CouncilCode;
                intent.DialecticalReason = "Bổ trợ liên ngành giữa lý thuyết trừu tượng và ứng dụng thực tiễn.";
                intent.KnowledgeGapScore = 0.3;
                intent.IsNovelDomain = false;
            }

            return intent;
        }

        private static bool ContainsAny(string text, params string[] needles)
        {
            return needles.Any(n => Regex.IsMatch(text, $@"\b{Regex.Escape(n)}\b", RegexOptions.IgnoreCase));
        }

        private static string CleanJsonOutput(string raw)
        {
            var clean = raw.Trim();
            if (clean.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(7);
            }
            else if (clean.StartsWith("```"))
            {
                clean = clean.Substring(3);
            }

            if (clean.EndsWith("```"))
            {
                clean = clean.Substring(0, clean.Length - 3);
            }

            return clean.Trim();
        }
    }
}
