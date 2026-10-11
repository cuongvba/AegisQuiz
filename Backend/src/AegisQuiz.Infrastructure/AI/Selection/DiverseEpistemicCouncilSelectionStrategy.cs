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
    /// [AegisAI Diverse Epistemic Council Selection Strategy - AIER v2.0 Integrated]
    /// Thuật toán tuyển chọn Viện Hàn Lâm thông minh tối ưu vượt trội:
    /// 1. Tích hợp Tầng Định Tuyến Ý Định Nhận Thức (IEpistemicIntentRouter) giải mã bản thể học chủ đề.
    /// 2. Khử hoàn toàn các bẫy từ vựng đa nghĩa (Polysemy), đồng nghĩa (Synonym Gap) và stopwords.
    /// 3. Tuyển chọn Viện Đối Trọng Biện Chứng Động (Dialectical Contrarian) dựa trên mâu thuẫn nhận thức thực thụ kèm luận giải khoa học.
    /// 4. Phát hiện Miền Tri Thức Mới dựa trên KnowledgeGapScore và kích hoạt Động Cơ Khởi Sinh Viện Mới (Genesis Engine) tự động.
    /// </summary>
    public class DiverseEpistemicCouncilSelectionStrategy : ICouncilSelectionStrategy
    {
        private readonly IEpistemicGenesisEngine? _genesisEngine;
        private readonly IEpistemicIntentRouter? _intentRouter;
        private readonly ILogger<DiverseEpistemicCouncilSelectionStrategy> _logger;

        public DiverseEpistemicCouncilSelectionStrategy(
            ILogger<DiverseEpistemicCouncilSelectionStrategy> logger,
            IEpistemicGenesisEngine? genesisEngine = null,
            IEpistemicIntentRouter? intentRouter = null)
        {
            _logger = logger;
            _genesisEngine = genesisEngine;
            _intentRouter = intentRouter;
        }

        public async Task<List<UniversalAiCouncil>> SelectCouncilsAsync(
            CouncilSelectionContext context,
            List<UniversalAiCouncil> availableCouncils,
            CancellationToken ct = default)
        {
            if (availableCouncils == null || !availableCouncils.Any())
            {
                return new List<UniversalAiCouncil>();
            }

            // 1. Trường hợp người dùng yêu cầu chỉ đích danh các Viện
            if (context.RequestedCouncilCodes != null && context.RequestedCouncilCodes.Any())
            {
                var requested = availableCouncils
                    .Where(c => context.RequestedCouncilCodes.Contains(c.CouncilCode, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (requested.Any())
                {
                    var finalRequested = new List<UniversalAiCouncil>();
                    foreach (var c in requested)
                    {
                        if (c.ReadinessStatus == "OFFLINE")
                        {
                            _logger.LogWarning("[CouncilSelector] ⚠️ Viện chỉ định '{Code}' đang ở trạng thái OFFLINE (chưa sẵn sàng API Key). Tìm viện dự phòng thay thế...", c.CouncilCode);
                            var substitute = availableCouncils.FirstOrDefault(a => 
                                a.CouncilCode != c.CouncilCode && 
                                !context.RequestedCouncilCodes.Contains(a.CouncilCode, StringComparer.OrdinalIgnoreCase) &&
                                (a.ReadinessStatus == "READY" || a.ReadinessStatus == "DEGRADED") &&
                                (a.MaterializedTaxonomyPath == c.MaterializedTaxonomyPath || a.DisciplineArea == c.DisciplineArea));

                            if (substitute != null)
                            {
                                _logger.LogInformation("[CouncilSelector] 🔄 Tự động thay thế Viện OFFLINE '{OldCode}' bằng Viện Sẵn Sàng: '{NewCode}' ({NewName})",
                                    c.CouncilCode, substitute.CouncilCode, substitute.DisplayName);
                                finalRequested.Add(substitute);
                            }
                            else
                            {
                                finalRequested.Add(c);
                            }
                        }
                        else
                        {
                            finalRequested.Add(c);
                        }
                    }

                    _logger.LogInformation("[CouncilSelector] Đã phê duyệt {Count} Viện tham gia phiên họp: {Codes}",
                        finalRequested.Count, string.Join(", ", finalRequested.Select(r => $"{r.CouncilCode}({r.ReadinessStatus})")));
                    return finalRequested;
                }
            }

            // 2. 🌟 GIẢI MÃ Ý ĐỊNH BẢN THỂ HỌC (EPISTEMIC INTENT ROUTING - AIER v2.0)
            EpistemicRoutingIntent? routingIntent = null;
            if (_intentRouter != null)
            {
                try
                {
                    routingIntent = await _intentRouter.RouteEpistemicIntentAsync(
                        context.TopicTitle, context.CoreProblemStatement, availableCouncils, context.TenantId, ct);
                    
                    _logger.LogInformation("[CouncilSelector] 🎯 Kết quả phân luồng nhận thức: PrimaryDomain={Domain}, Lead={Lead}, Contrarian={Contrarian}, GapScore={Gap:F2}, Novel={Novel}",
                        routingIntent.PrimaryDomain, routingIntent.RecommendedLeadCouncilCode, routingIntent.RecommendedContrarianCouncilCode, routingIntent.KnowledgeGapScore, routingIntent.IsNovelDomain);
                }
                catch (Exception exRoute)
                {
                    _logger.LogWarning(exRoute, "[CouncilSelector] Không thể hoàn thành định tuyến AIER, tiếp tục với phân tích từ vựng nâng cao.");
                }
            }

            // 3. Phân tích văn bản đề tài thành các tokens
            var fullText = $"{context.TopicTitle} {context.CoreProblemStatement}".ToLowerInvariant();
            var tokens = ExtractWordTokens(fullText);

            var scoredCouncils = new List<(UniversalAiCouncil Council, double TotalScore, double DomainScore, List<string> MatchedKeywords)>();

            foreach (var council in availableCouncils)
            {
                double domainScore = 0;
                var matchedKeywords = new List<string>();

                // A. Thưởng điểm đặc biệt nếu Router AIER xác định đây là Viện Chủ Trì bản thể học
                if (routingIntent != null && !string.IsNullOrWhiteSpace(routingIntent.RecommendedLeadCouncilCode) &&
                    string.Equals(council.CouncilCode, routingIntent.RecommendedLeadCouncilCode, StringComparison.OrdinalIgnoreCase))
                {
                    domainScore += 65.0; // Trọng số quyết định tuyệt đối cho Viện Chủ Trì
                    matchedKeywords.Add($"[AIER-LeadDomain: {routingIntent.PrimaryDomain}]");
                }

                // B. So khớp từ TriggerKeywordsJson (Cấu hình động 100% DB)
                var keywords = ParseKeywords(council.TriggerKeywordsJson);
                foreach (var kw in keywords)
                {
                    var kwLower = kw.Trim().ToLowerInvariant();
                    if (string.IsNullOrWhiteSpace(kwLower)) continue;

                    // KHỬ BẪY ĐA NGHĨA (Polysemy Filter):
                    // Nếu đề tài thuộc Sinh học (LIFE_SCIENCES) thì từ "vốn" KHÔNG được gán điểm cho Viện Kinh Tế (ECONOMICS)
                    if (routingIntent != null && routingIntent.PrimaryDomain == "LIFE_SCIENCES" && council.CouncilCode == "ECONOMICS" && kwLower == "vốn")
                    {
                        continue; // Bỏ qua bẫy "vốn gen" -> "vốn tài chính"
                    }

                    // Nếu là từ đơn -> kiểm tra token set; Nếu là cụm từ -> kiểm tra phrase match có biên
                    if (kwLower.Contains(' '))
                    {
                        if (Regex.IsMatch(fullText, $@"\b{Regex.Escape(kwLower)}\b", RegexOptions.IgnoreCase))
                        {
                            domainScore += 15.0; // Trọng số cao cho cụm từ chuyên môn
                            matchedKeywords.Add(kw);
                        }
                    }
                    else
                    {
                        if (tokens.Contains(kwLower))
                        {
                            domainScore += 8.0;
                            matchedKeywords.Add(kw);
                        }
                    }
                }

                // C. So khớp tên hiển thị & Miền ngành
                var nameTokens = ExtractWordTokens(council.DisplayName.ToLowerInvariant());
                foreach (var nt in nameTokens)
                {
                    if (nt.Length > 2 && tokens.Contains(nt))
                    {
                        domainScore += 5.0;
                        matchedKeywords.Add(nt);
                    }
                }

                // D. So khớp Taxonomy Path (VD: /HUMANITY/ARTS, /UNIVERSE/PHYSICS)
                var taxSegments = council.MaterializedTaxonomyPath
                    .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.ToLowerInvariant());

                foreach (var seg in taxSegments)
                {
                    if (seg.Length > 2 && tokens.Contains(seg))
                    {
                        domainScore += 4.0;
                    }
                }

                // E. Hệ số uy tín ELO nhẹ để ưu tiên chất lượng học thuật khi điểm tương đương
                double totalScore = domainScore + (council.BenchmarkPrestigeElo / 1000.0);

                // F. Trọng số Readiness (Tình trạng sẵn sàng kết nối mô hình thực)
                if (council.ReadinessStatus == "READY")
                {
                    totalScore += 25.0;
                }
                else if (council.ReadinessStatus == "DEGRADED")
                {
                    totalScore += 10.0;
                }
                else if (council.ReadinessStatus == "OFFLINE")
                {
                    totalScore -= 40.0;
                }

                scoredCouncils.Add((council, totalScore, domainScore, matchedKeywords));
            }

            // 4. Sắp xếp theo tổng điểm
            var ordered = scoredCouncils
                .OrderByDescending(x => x.TotalScore)
                .ToList();

            _logger.LogInformation("[CouncilSelector] Đề tài '{Topic}' -> Top viện: {TopList}",
                context.TopicTitle, string.Join("; ", ordered.Take(3).Select(x => $"{x.Council.CouncilCode}=Total:{x.TotalScore:F1}/Domain:{x.DomainScore:F1} [{string.Join(',', x.MatchedKeywords)}]")));

            var selected = new List<UniversalAiCouncil>();

            // Lấy các viện có điểm match thực tế chuyên ngành (domainScore >= 6.0)
            var relevant = ordered.Where(x => x.DomainScore >= 6.0).ToList();

            // 5. 🌟 CƠ CHẾ KHỞI SINH VIỆN MỚI TỰ ĐỘNG (JUST-IN-TIME COUNCIL GENESIS)
            // Kích hoạt khi:
            // 1. Router AIER xác định IsNovelDomain == true hoặc KnowledgeGapScore >= 0.70
            // 2. Hoặc điểm DomainScore cao nhất hiện có < 20.0
            var academicRelevant = scoredCouncils.Where(x => !x.Council.CouncilCode.StartsWith("SYS_")).ToList();
            double topDomainScore = academicRelevant.Any() ? academicRelevant.Max(x => x.DomainScore) : 0.0;

            bool shouldTriggerGenesis = context.AllowDynamicGenesis && 
                ((routingIntent != null && (routingIntent.IsNovelDomain || routingIntent.KnowledgeGapScore >= 0.70)) || topDomainScore < 20.0);

            if (shouldTriggerGenesis && _genesisEngine != null)
            {
                _logger.LogInformation("[CouncilSelector] 🚨 Phát hiện Miền Tri Thức Mới (GapScore: {GapScore:F2}, TopDomainScore: {TopScore:F1}). Kích hoạt Đại Kiến Trúc Sư Tầng 0 khởi sinh Viện mới!",
                    routingIntent?.KnowledgeGapScore ?? 1.0, topDomainScore);
                try
                {
                    var genesisCouncil = await _genesisEngine.GenesisCouncilForTopicAsync(
                        context.TopicTitle, context.CoreProblemStatement, routingIntent?.SuggestedGenesis, context.TenantId, ct);

                    if (genesisCouncil != null && !selected.Any(s => s.CouncilCode == genesisCouncil.CouncilCode))
                    {
                        selected.Insert(0, genesisCouncil);
                        _logger.LogInformation("[CouncilSelector] 🌟 TẦNG 0 ĐÃ KHỞI SINH VIỆN MỚI DẪN ĐẦU ĐẠI HỘI THẢO: [{Code}] {Name}",
                            genesisCouncil.CouncilCode, genesisCouncil.DisplayName);
                    }
                }
                catch (Exception exGen)
                {
                    _logger.LogWarning(exGen, "[CouncilSelector] Lỗi khi Tầng 0 tự sinh viện mới, tiếp tục luồng fallback.");
                }
            }

            if (relevant.Any())
            {
                int primaryLimit = context.IncludeContrarianPerspective
                    ? Math.Max(1, context.MaxCouncils - selected.Count - 1)
                    : Math.Max(1, context.MaxCouncils - selected.Count);

                foreach (var rel in relevant)
                {
                    if (selected.Count >= context.MaxCouncils) break;
                    if (!selected.Any(s => s.CouncilCode == rel.Council.CouncilCode))
                    {
                        selected.Add(rel.Council);
                    }
                }

                // 6. 🌟 TUYỂN CHỌN VIỆN ĐỐI TRỌNG BIỆN CHỨNG ĐỘNG (DIALECTICAL CONTRARIAN)
                if (context.IncludeContrarianPerspective && selected.Count < context.MaxCouncils)
                {
                    UniversalAiCouncil? contrarian = null;

                    // A. Ưu tiên tuyệt đối chỉ định đối trọng từ AIER Router kèm lý giải biện chứng
                    if (routingIntent != null && !string.IsNullOrWhiteSpace(routingIntent.RecommendedContrarianCouncilCode))
                    {
                        contrarian = availableCouncils.FirstOrDefault(c => 
                            string.Equals(c.CouncilCode, routingIntent.RecommendedContrarianCouncilCode, StringComparison.OrdinalIgnoreCase) &&
                            !selected.Any(s => s.CouncilCode == c.CouncilCode));

                        if (contrarian != null)
                        {
                            _logger.LogInformation("[CouncilSelector] ⚖️ Đã bổ sung Viện Đối Trọng Biện Chứng Động: [{Code}] {Name}. Luận giải: {Reason}",
                                contrarian.CouncilCode, contrarian.DisplayName, routingIntent.DialecticalReason);
                        }
                    }

                    // B. Fallback qua thuật toán heuristic đối lập nếu chưa chọn được
                    if (contrarian == null)
                    {
                        contrarian = SelectContrarianCouncil(selected, availableCouncils);
                        if (contrarian != null)
                        {
                            _logger.LogInformation("[CouncilSelector] Đã bổ sung Viện Đối Trọng (Heuristics Fallback): {Code}", contrarian.CouncilCode);
                        }
                    }

                    if (contrarian != null && !selected.Any(s => s.CouncilCode == contrarian.CouncilCode))
                    {
                        selected.Add(contrarian);
                    }
                }
            }

            // 7. Nếu vẫn chưa đủ MinCouncils, bổ sung từ các viện có sẵn
            if (selected.Count < context.MinCouncils)
            {
                var remaining = availableCouncils
                    .Where(c => !selected.Any(s => s.CouncilCode == c.CouncilCode))
                    .OrderByDescending(c => c.BenchmarkPrestigeElo)
                    .ToList();

                foreach (var rem in remaining)
                {
                    selected.Add(rem);
                    if (selected.Count >= context.MinCouncils) break;
                }
            }

            _logger.LogInformation("[CouncilSelector] Tuyển chọn hoàn tất {Count} Viện cho đề tài '{Topic}': {List}",
                selected.Count, context.TopicTitle, string.Join(", ", selected.Select(s => s.DisplayName)));

            return selected;
        }

        private static HashSet<string> ExtractWordTokens(string text)
        {
            var matches = Regex.Matches(text, @"[\p{L}\p{N}]+");
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in matches)
            {
                if (m.Value.Length >= 2)
                {
                    set.Add(m.Value.ToLowerInvariant());
                }
            }
            return set;
        }

        private static List<string> ParseKeywords(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]")
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static UniversalAiCouncil? SelectContrarianCouncil(
            List<UniversalAiCouncil> currentSelected,
            List<UniversalAiCouncil> allCouncils)
        {
            var selectedTaxonomies = currentSelected.Select(s => s.MaterializedTaxonomyPath).ToList();

            if (selectedTaxonomies.All(t => t.Contains("HUMANITY") || t.Contains("ARTS") || t.Contains("PHILOSOPHY")))
            {
                return allCouncils.FirstOrDefault(c => c.CouncilCode == "NEUROSCIENCE")
                    ?? allCouncils.FirstOrDefault(c => c.CouncilCode == "AGI_CYBERNETICS")
                    ?? allCouncils.FirstOrDefault(c => c.CouncilCode == "COSMOLOGY");
            }

            if (selectedTaxonomies.All(t => t.Contains("PHYSICS") || t.Contains("MATHEMATICS") || t.Contains("QUANTUM")))
            {
                return allCouncils.FirstOrDefault(c => c.CouncilCode == "PHILOSOPHY")
                    ?? allCouncils.FirstOrDefault(c => c.CouncilCode == "LIFE_SCIENCES");
            }

            if (selectedTaxonomies.All(t => t.Contains("SOCIETY") || t.Contains("ECONOMICS")))
            {
                return allCouncils.FirstOrDefault(c => c.CouncilCode == "PHILOSOPHY")
                    ?? allCouncils.FirstOrDefault(c => c.CouncilCode == "MATHEMATICS");
            }

            return allCouncils.FirstOrDefault(c => c.CouncilCode == "PHILOSOPHY" && !currentSelected.Any(s => s.CouncilCode == "PHILOSOPHY"))
                ?? allCouncils.FirstOrDefault(c => !currentSelected.Any(s => s.Id == c.Id));
        }
    }
}
