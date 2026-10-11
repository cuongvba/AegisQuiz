using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.Infrastructure.AI.Selection
{
    /// <summary>
    /// [AegisAI Spatio-Temporal Role Allocation Engine]
    /// Phân vai học giả biện chứng theo Không gian (Văn hóa, Tri thức) & Thời gian (ELO Drift, Năng lực mới nhất).
    /// Đảm bảo tính đa dạng tuyệt đối (Multi-Model Diversity): Triệt tiêu hoàn toàn sự độc canh 1 model duy nhất cho mọi Viện.
    /// Quản lý Bầy Sói (WolfpackSwarm) dưới dạng Chuỗi Mô Hình & Bể Khóa (Keys Pool) ưu tiên User BYOK -> System.
    /// </summary>
    public class SpatioTemporalRoleAllocator : ISpatioTemporalRoleAllocator
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<SpatioTemporalRoleAllocator> _logger;

        public SpatioTemporalRoleAllocator(
            IServiceScopeFactory scopeFactory,
            IConfiguration config,
            ILogger<SpatioTemporalRoleAllocator> logger)
        {
            _scopeFactory = scopeFactory;
            _config = config;
            _logger = logger;
        }

        public async Task EvaluateCouncilsReadinessAsync(List<UniversalAiCouncil> councils, Guid tenantId, CancellationToken ct = default)
        {
            if (councils == null || !councils.Any()) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

            var activeModels = await db.AiModelRegistries.AsNoTracking()
                .Where(m => m.IsEnabled && (m.TenantId == Guid.Empty || m.TenantId == tenantId))
                .ToListAsync(ct);

            var activeModelMap = activeModels.ToDictionary(m => m.ModelCode, m => m, StringComparer.OrdinalIgnoreCase);

            // Bể khóa người dùng đã đăng ký (User BYOK / Community)
            var userKeys = await db.UserAiKeys.AsNoTracking()
                .Where(k => k.IsActive && (k.TenantId == Guid.Empty || k.TenantId == tenantId))
                .ToListAsync(ct);

            // Bể khóa dự trữ của hệ thống từ Config
            var systemProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(_config["DeepSeek:ApiKey"])) systemProviders.Add("DeepSeek");
            if (!string.IsNullOrWhiteSpace(_config["Gemini:ApiKey"])) systemProviders.Add("Google");
            if (!string.IsNullOrWhiteSpace(_config["Anthropic:ApiKey"])) systemProviders.Add("Anthropic");
            if (!string.IsNullOrWhiteSpace(_config["OpenAI:ApiKey"])) systemProviders.Add("OpenAI");
            if (!string.IsNullOrWhiteSpace(_config["Groq:ApiKey"])) systemProviders.Add("Groq");

            foreach (var council in councils)
            {
                council.TotalSeatsCount = council.Seats?.Count ?? 0;
                int readyCount = 0;

                if (council.Seats != null && council.Seats.Any())
                {
                    foreach (var seat in council.Seats)
                    {
                        var candidateModelCodes = new List<string>();
                        var alpha = !string.IsNullOrWhiteSpace(seat.AlphaModelCode)
                            ? seat.AlphaModelCode
                            : (!string.IsNullOrWhiteSpace(seat.AssignedModelCode) ? seat.AssignedModelCode : seat.PreferredModelCode);

                        if (!string.IsNullOrWhiteSpace(alpha)) candidateModelCodes.Add(alpha);

                        if (!string.IsNullOrWhiteSpace(seat.SwarmModelCodesJson))
                        {
                            try
                            {
                                var parsed = JsonSerializer.Deserialize<List<string>>(seat.SwarmModelCodesJson);
                                if (parsed != null)
                                {
                                    foreach (var m in parsed.Where(m => !string.IsNullOrWhiteSpace(m) && !candidateModelCodes.Contains(m)))
                                    {
                                        candidateModelCodes.Add(m);
                                    }
                                }
                            }
                            catch { /* json parse ignore */ }
                        }

                        // Xây dựng Hồ sơ năng lực & Keys Pool (SwarmWolfProfiles) cho từng con Sói trong Bầy
                        var wolfProfiles = new List<WolfModelKeyPoolSummary>();
                        foreach (var mCode in candidateModelCodes)
                        {
                            activeModelMap.TryGetValue(mCode, out var regModel);
                            var provider = regModel?.ProviderCompany ?? InferProviderCompany(mCode);
                            var userKeyCount = userKeys.Count(k => k.ProviderCompany.Equals(provider, StringComparison.OrdinalIgnoreCase));
                            var hasSysKey = systemProviders.Contains(provider);
                            var hasComKey = userKeys.Any(k => k.Scope == KeyUsageScope.CommunityContributed && k.ProviderCompany.Equals(provider, StringComparison.OrdinalIgnoreCase));

                            wolfProfiles.Add(new WolfModelKeyPoolSummary
                            {
                                ModelCode = mCode,
                                DisplayName = regModel?.DisplayName ?? mCode,
                                ProviderCompany = provider,
                                CognitiveArchetype = regModel?.CognitiveArchetype ?? seat.IdealArchetype,
                                ContemporaryStrength = GetContemporaryStrength(mCode, regModel),
                                UserKeysCount = userKeyCount,
                                HasSystemKey = hasSysKey,
                                HasCommunityKey = hasComKey,
                                KeyPriorityFlow = userKeyCount > 0
                                    ? "User BYOK (Ưu tiên 1 - Đã có Khóa) ➔ Community ➔ System"
                                    : (hasSysKey ? "System Default (Dự trữ công quyền) ➔ Cần bổ sung BYOK để tối ưu FinOps" : "Chưa có Khóa trong Bể!")
                            });
                        }

                        seat.SwarmWolfProfiles = wolfProfiles;

                        // Ghế sẵn sàng nếu có ít nhất 1 Sói có Model và có Khóa trong Bể
                        var hasReadyWolf = wolfProfiles.Any(w => w.IsKeyPoolReady);
                        seat.IsReady = hasReadyWolf || activeModelMap.ContainsKey(seat.PreferredModelCode);
                        if (seat.IsReady) readyCount++;
                    }
                }

                council.ReadySeatsCount = readyCount;

                if (council.TotalSeatsCount == 0 || readyCount == council.TotalSeatsCount)
                {
                    council.ReadinessStatus = "READY";
                    council.ReadinessMessage = $"Toàn diện {council.ReadySeatsCount}/{council.TotalSeatsCount} ghế chuyên gia và bầy sói đã sẵn sàng với bể khóa tối ưu.";
                }
                else if (readyCount > 0)
                {
                    council.ReadinessStatus = "DEGRADED";
                    council.ReadinessMessage = $"{council.ReadySeatsCount}/{council.TotalSeatsCount} ghế sẵn sàng (Một số ghế đang dùng bầy sói dự phòng).";
                }
                else
                {
                    council.ReadinessStatus = "OFFLINE";
                    council.ReadinessMessage = "Chưa kích hoạt mô hình hoặc chưa có API Key trong bể khóa của các ghế thuộc viện này.";
                }

                council.SpatioTemporalAffinity = DetermineCouncilNaturalAffinity(council);
            }
        }

        private static string InferProviderCompany(string modelCode)
        {
            var lower = modelCode.ToLower();
            if (lower.Contains("deepseek")) return "DeepSeek";
            if (lower.Contains("gemini")) return "Google";
            if (lower.Contains("claude")) return "Anthropic";
            if (lower.Contains("gpt") || lower.Contains("o1") || lower.Contains("o3")) return "OpenAI";
            if (lower.Contains("qwen")) return "Alibaba";
            if (lower.Contains("llama")) return "Meta";
            return "Custom";
        }

        private static string GetContemporaryStrength(string modelCode, AiModelRegistry? model)
        {
            var code = modelCode.ToLower();
            if (code.Contains("deepseek-r1") || code.Contains("reasoner") || code.Contains("o1") || code.Contains("o3"))
                return "Tư duy Suy Luận Chuỗi Dài (CoT), giải toán logic đa bước và bóc tách nan đề nhận thức với FinOps tối ưu.";
            if (code.Contains("gemini") || code.Contains("flash") || code.Contains("pro"))
                return "Ngữ Cảnh Cực Đại (1M - 2M Tokens), nuốt trọn văn bản đồ sộ và hồ sơ thực chứng đa nguồn xuyên ngành.";
            if (code.Contains("claude") || code.Contains("sonnet") || code.Contains("opus"))
                return "Chuẩn Mực Sư Phạm & Biện Hộ Đối Kháng (Devil's Advocate), chống bẫy nhận thức và chuẩn hóa văn phong.";
            if (code.Contains("gpt-4o") || code.Contains("gpt-4"))
                return "Cân Bằng Đa Nhiệm & Kiến Tạo Siêu Hình, trực giác nhanh và phản xạ tổng hợp liên ngành.";
            if (code.Contains("qwen"))
                return "Phân Tích Đa Văn Hóa & Triết Học Đông Tây, tri thức Á Đông và tính toán toán học cao cấp.";

            if (model != null)
            {
                return model.CognitiveArchetype switch
                {
                    CognitiveArchetype.DeepReasoner => "Suy luận logic sâu tầng, chuỗi suy tưởng CoT",
                    CognitiveArchetype.MassiveContext => "Khả năng xử lý ngữ cảnh siêu dài hàng triệu tokens",
                    CognitiveArchetype.LinguisticArbiter => "Thẩm định ngôn ngữ sư phạm, barem chấm và phản biện đối kháng",
                    CognitiveArchetype.UltraFastScanner => "Tốc độ xử lý siêu tốc real-time < 250ms",
                    CognitiveArchetype.SovereignLocal => "Bảo mật tuyệt đối On-Premise nội bộ",
                    _ => "Mô hình nhận thức hỗ trợ tổng hợp"
                };
            }

            return "Phân tích nhận thức và suy luận học thuật tổng hợp";
        }

        public async Task AllocateCouncilRolesAsync(
            string topicTitle,
            string problemStatement,
            List<UniversalAiCouncil> participatingCouncils,
            Guid tenantId,
            CancellationToken ct = default)
        {
            if (participatingCouncils == null || !participatingCouncils.Any()) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

            var activeModels = await db.AiModelRegistries.AsNoTracking()
                .Where(m => m.IsEnabled && (m.TenantId == Guid.Empty || m.TenantId == tenantId))
                .OrderByDescending(m => m.BenchmarkElo)
                .ToListAsync(ct);

            var topicSpace = DetectEpistemicSpace(topicTitle, problemStatement);
            _logger.LogInformation("[RoleAllocator] Đề tài: '{Title}' -> Không gian tri thức phân tích: {Space}", topicTitle, topicSpace);

            // BẢNG ĐIỀU PHỐI ĐA MÔ HÌNH HETEROGENEOUS SWARM (ĐẢM BẢO MỖI VIỆN MANG MỘT MÔ HÌNH CHỦ ĐẠO KHÁC NHAU)
            var assignedPresidentModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < participatingCouncils.Count; i++)
            {
                var council = participatingCouncils[i];
                if (council.Seats == null || !council.Seats.Any()) continue;

                // 1. Phân bổ Model cho ghế Chủ Tịch (President) - Đảm bảo tính độc nhất đa dạng giữa các Viện
                var presSeat = council.Seats.FirstOrDefault(s => s.Role == CouncilSeatRole.President) ?? council.Seats.First();
                var presAllocation = DispatchDiversePresidentModel(council, topicSpace, assignedPresidentModels, activeModels);
                presSeat.AssignedModelCode = presAllocation.ModelCode;
                presSeat.AssignmentRationale = presAllocation.Rationale;
                assignedPresidentModels.Add(presAllocation.ModelCode);

                // 2. Phân bổ Model cho ghế Phản Biện Đối Kháng (Adversarial Critic) - Đảm bảo đối trọng với ghế Chủ tịch
                var criticSeat = council.Seats.FirstOrDefault(s => s.Role == CouncilSeatRole.AdversarialCritic);
                if (criticSeat != null)
                {
                    var criticAllocation = DispatchAntitheticalCriticModel(council, presAllocation.ModelCode, topicSpace);
                    criticSeat.AssignedModelCode = criticAllocation.ModelCode;
                    criticSeat.AssignmentRationale = criticAllocation.Rationale;
                }

                // 3. Phân bổ các ghế còn lại (Kiểm chứng, Tổng hợp)
                foreach (var seat in council.Seats.Where(s => s.Role != CouncilSeatRole.President && s.Role != CouncilSeatRole.AdversarialCritic))
                {
                    if (seat.Role == CouncilSeatRole.EmpiricalAuditor)
                    {
                        seat.AssignedModelCode = "gemini-2.0-flash";
                        seat.AssignmentRationale = "Phân vai [Thẩm Tra Thực Địa]: Năng lực kiểm chứng dữ kiện với độ trễ thấp và cửa sổ ngữ cảnh cực đại bám sát Hồ Sơ Deep Research.";
                    }
                    else
                    {
                        seat.AssignedModelCode = "gpt-4o";
                        seat.AssignmentRationale = "Phân vai [Kiến Tạo Metatheory]: Dung nạp và hòa giải các góc nhìn đối kháng để kiến tạo khung chân lý chung.";
                    }
                }
            }
        }

        private (string ModelCode, string Rationale) DispatchDiversePresidentModel(
            UniversalAiCouncil council,
            EpistemicSpace topicSpace,
            HashSet<string> alreadyAssignedModels,
            List<AiModelRegistry> activeModels)
        {
            // Danh sách model ưu tiên theo thế mạnh đặc thù của từng Viện
            var candidates = new List<(string ModelCode, string Rationale)>();

            switch (council.CouncilCode)
            {
                case "PHILOSOPHY":
                    if (topicSpace == EpistemicSpace.EasternHermeneutics)
                    {
                        candidates.Add(("deepseek-chat", "Phân vai [Đông Phương Bản Thể Luận]: Am tường kho ngữ liệu văn tự Nho - Phật - Đạo thâm sâu, tối ưu cho giải mã minh triết Á Đông."));
                        candidates.Add(("qwen-2.5-72b", "Phân vai [Ngữ Dụng Học Á Đông]: Nền tảng tri thức Hán - Nôm và ngôn ngữ Á Đông vượt trội."));
                    }
                    else
                    {
                        candidates.Add(("claude-3-5-sonnet", "Phân vai [Tây Phương Luận Lý Học]: Cấu trúc lập luận quy nạp và diễn dịch bản thể luận chặt chẽ chuẩn mực Kant - Hegel."));
                        candidates.Add(("gpt-4o", "Phân vai [Triết Học Phân Tích & Ngữ Nghĩa]: Khả năng bóc tách khái niệm hiện sinh và phân tâm học sắc sảo."));
                    }
                    break;

                case "LIFE_SCIENCES":
                case "AGRO_ECOLOGY":
                    candidates.Add(("gemini-2.0-flash", "Phân vai [Thực Nghiệm Sinh Quyển & Tiến Hóa]: Năng lực phân tích đa thể thức, bám sát các chu trình nhiệt động lực sinh thái và dữ liệu gen hành tinh."));
                    candidates.Add(("gpt-4o", "Phân vai [Mô Hình Hóa Hệ Sinh Thái]: Khả năng phân tích động lực học dân số và cân bằng sinh quyển phức hợp."));
                    break;

                case "COSMOLOGY":
                case "MATHEMATICS":
                    candidates.Add(("deepseek-reasoner", "Phân vai [Logic Cực Hạn CoT & Lượng Tử]: Đạt đỉnh điểm ELO về suy diễn chuỗi dài lý tính trên các hệ tiên đề bất toàn Gödel và cấu trúc không - thời gian."));
                    candidates.Add(("gemini-2.0-flash-thinking", "Phân vai [Toán Học Hình Thức & Đa Chiều]: Khả năng mô phỏng hình học phi Euclid và ma trận xác suất lượng tử."));
                    break;

                case "ECONOMICS":
                case "JURISPRUDENCE":
                    candidates.Add(("gpt-4o", "Phân vai [Kinh Tế Lượng & Lý Thuyết Trò Chơi]: Nắm bắt sâu sắc cân bằng Nash, ngoại ứng tiêu cực và cơ chế phân bổ tài nguyên tối ưu Pareto."));
                    candidates.Add(("claude-3-5-sonnet", "Phân vai [Triết Lý Pháp Quyền & Khế Ước]: Bóc tách các xung đột lợi ích và thiết chế văn minh bền vững."));
                    break;

                case "AGI_CYBERNETICS":
                case "NEUROSCIENCE":
                    candidates.Add(("claude-3-5-sonnet", "Phân vai [Căn Chỉnh Nhân Loại & Ý Thức Số]: Mô hình tiêu biểu về an toàn AI, điều khiển học nhận thức và đo lường tính chủ quan Qualia."));
                    candidates.Add(("deepseek-reasoner", "Phân vai [Thuật Toán Tự Lặp & Mạng Nơ-ron]: Đào sâu cấu trúc toán học của các mạng thần kinh học sâu."));
                    break;

                default:
                    candidates.Add(("claude-3-5-sonnet", "Phân vai [Học Thuật Đa Miền]: Năng lực luận lý và phản biện đa ngành cấp cao."));
                    candidates.Add(("gemini-2.0-flash", "Phân vai [Tổng Hợp Thực Chứng]: Tốc độ xử lý và khả năng đối chiếu dữ liệu diện rộng."));
                    candidates.Add(("deepseek-reasoner", "Phân vai [Suy Lý Độc Lập]: Chuỗi CoT bóc tách các mâu thuẫn nhận thức."));
                    candidates.Add(("gpt-4o", "Phân vai [Hệ Thống Hóa Tri Thức]: Năng lực liên kết các trường phái tư tưởng."));
                    break;
            }

            // Chọn candidate đầu tiên chưa bị Viện khác sử dụng
            foreach (var cand in candidates)
            {
                if (!alreadyAssignedModels.Contains(cand.ModelCode))
                {
                    return cand;
                }
            }

            // Fallback danh sách mô hình đa dạng toàn cục nếu tất cả candidate trên đã bị dùng
            var globalFallbacks = new[]
            {
                ("claude-3-5-sonnet", "Phân vai [Biện Chứng Đa Chiều]: Đảm bảo góc nhìn học thuật độc lập."),
                ("deepseek-reasoner", "Phân vai [Lý Tính Phản Tỉnh]: Tự suy luận chuỗi dài độc lập."),
                ("gemini-2.0-flash", "Phân vai [Kiểm Chứng Thực Địa]: Khảo sát đa chiều từ kho tri thức."),
                ("gpt-4o", "Phân vai [Mô Hình Hóa Tổng Thể]: Phân tích đa tác nhân toàn cầu."),
                ("qwen-2.5-72b", "Phân vai [Minh Triết Bản Địa]: Góc nhìn văn hóa và bản thể đặc thù.")
            };

            foreach (var fb in globalFallbacks)
            {
                if (!alreadyAssignedModels.Contains(fb.Item1))
                {
                    return (fb.Item1, fb.Item2);
                }
            }

            return candidates.First();
        }

        private (string ModelCode, string Rationale) DispatchAntitheticalCriticModel(
            UniversalAiCouncil council,
            string presidentModel,
            EpistemicSpace topicSpace)
        {
            // Ghế phản biện đối kháng (Devil's Advocate) PHẢI KHÁC model của Chủ tịch Viện
            if (presidentModel.Contains("deepseek", StringComparison.OrdinalIgnoreCase))
            {
                return ("claude-3-5-sonnet",
                    "Phân vai [Phản Biện Sắc Lạnh - Tây Phương]: Dùng lăng kính phản nghiệm Popper và logic hình thức để vạch trần các giả định chủ quan.");
            }
            else if (presidentModel.Contains("claude", StringComparison.OrdinalIgnoreCase))
            {
                return ("deepseek-reasoner",
                    "Phân vai [Phản Biện Cực Đoan - CoT Khắc Nghiệt]: Dùng chuỗi suy diễn phản chứng toán học để bóc trần ảo giác lập luận và ngụy biện ngoại suy.");
            }
            else if (presidentModel.Contains("gemini", StringComparison.OrdinalIgnoreCase))
            {
                return ("claude-3-5-sonnet",
                    "Phân vai [Phản Biện Bản Thể]: Đặt câu hỏi về nền tảng đạo đức và chiều sâu triết học đằng sau các con số thống kê thực nghiệm.");
            }
            else
            {
                return ("deepseek-reasoner",
                    "Phân vai [Truy Vấn Tiên Đề]: Vạch trần tính tự mâu thuẫn trong mô hình lý thuyết đề xuất.");
            }
        }

        private EpistemicSpace DetectEpistemicSpace(string title, string problem)
        {
            var text = $"{title} {problem}".ToLowerInvariant();

            if (Regex.IsMatch(text, @"\b(phương đông|đông phương|nho gia|đạo giáo|phật|thiền|vô vi|lão tử|trang tử|kinh dịch|âm dương|ngũ hành|tự tánh|bản tánh|bát nhã|khí|á đông)\b", RegexOptions.IgnoreCase))
            {
                return EpistemicSpace.EasternHermeneutics;
            }

            if (Regex.IsMatch(text, @"\b(phương tây|tây phương|kant|hegel|aristotle|plato|hiện sinh|khai sáng|nietzsche|descartes|hiện tượng luận|duy lý|cấu trúc luận|ngữ nghĩa học)\b", RegexOptions.IgnoreCase))
            {
                return EpistemicSpace.WesternAnalytic;
            }

            if (Regex.IsMatch(text, @"\b(lượng tử|fermi|gödel|godel|toán học|entropy|nhiệt động|hàm sóng|bất toàn|tính toán|vũ trụ|ngân hà|rừng rậm đen)\b", RegexOptions.IgnoreCase))
            {
                return EpistemicSpace.QuantumFormalLogic;
            }

            if (Regex.IsMatch(text, @"\b(sự sống|sinh học|sinh quyển|di truyền|mã gen|adn|nông nghiệp|sinh-số|sinh thái|hệ sinh thái|trái đất|tiến hóa|môi trường)\b", RegexOptions.IgnoreCase))
            {
                return EpistemicSpace.EmpiricalBioscience;
            }

            if (Regex.IsMatch(text, @"\b(agi|ai|trí tuệ nhân tạo|siêu trí tuệ|qualia|ý thức|mạng thần kinh|cybernetics|tự động hóa)\b", RegexOptions.IgnoreCase))
            {
                return EpistemicSpace.CyberneticAgi;
            }

            return EpistemicSpace.UniversalGeneral;
        }

        private string DetermineCouncilNaturalAffinity(UniversalAiCouncil council)
        {
            return council.CouncilCode switch
            {
                "PHILOSOPHY" => "Bản Thể Học So Sánh (Đông - Tây Hòa Hợp)",
                "COSMOLOGY" => "Vật Lý Lượng Tử & Cấu Trúc Không - Thời Gian",
                "MATHEMATICS" => "Logic Hình Thức & Giới Hạn Tính Toán Gödel",
                "AGI_CYBERNETICS" => "Siêu Trí Tuệ Nhân Tạo & Điều Khiển Học Ý Thức",
                "ECONOMICS" => "Kinh Tế Lượng & Động Lực Học Hậu Khan Hiếm",
                "LIFE_SCIENCES" => "Nhiệt Động Lực Sinh Quyển & Tiến Hóa Sinh-Số",
                "AGRO_ECOLOGY" => "Nông Nghiệp Tái Tạo & Sinh Thái Bền Vững",
                "ARTS_AESTHETICS" => "Mỹ Học & Biểu Tượng Nhận Thức Đương Đại",
                "JURISPRUDENCE" => "Triết Lý Pháp Quyền & Khế Ước Văn Minh",
                "NEUROSCIENCE" => "Khoa Học Thần Kinh & Cấu Trúc Tâm Thức",
                _ => "Toàn Cầu Thực Chứng & Siêu Hình Học"
            };
        }
    }

    public enum EpistemicSpace
    {
        UniversalGeneral = 0,
        EasternHermeneutics = 1,
        WesternAnalytic = 2,
        QuantumFormalLogic = 3,
        EmpiricalBioscience = 4,
        CyberneticAgi = 5
    }
}
