using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.Infrastructure.AI.Genesis
{
    /// <summary>
    /// [AegisAI Epistemic Genesis Engine - Supreme Autonomous Inaugurator]
    /// Hiện thực hóa Hội đồng Đại Kiến Trúc Sư Tối Cao Tầng 0:
    /// Khi phát hiện đề tài thuộc vùng tri thức mới chưa có Viện chuyên trách tương xứng,
    /// động cơ tự động phân tích ngữ nghĩa, khởi sinh Viện Hàn Lâm độc lập, bố trí ghế chuyên gia,
    /// soạn thảo Tuyên ngôn nhận thức luận và khắc ghi vĩnh cửu vào CSDL PostgreSQL.
    /// </summary>
    public class EpistemicGenesisEngine : IEpistemicGenesisEngine
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ISymposiumPromptComposer _promptComposer;
        private readonly ILogger<EpistemicGenesisEngine> _logger;

        public EpistemicGenesisEngine(
            IServiceScopeFactory scopeFactory,
            ISymposiumPromptComposer promptComposer,
            ILogger<EpistemicGenesisEngine> logger)
        {
            _scopeFactory = scopeFactory;
            _promptComposer = promptComposer;
            _logger = logger;
        }

        public async Task<UniversalAiCouncil?> GenesisCouncilForTopicAsync(
            string topicTitle,
            string coreProblemStatement,
            AegisQuiz.Application.Models.AI.SuggestedGenesisMetadata? suggestedGenesis = null,
            Guid? tenantId = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(topicTitle)) return null;

            _logger.LogInformation("[EpistemicGenesis] 🌟 ĐẠI KIẾN TRÚC SƯ TỐI CAO TẦNG 0 KHỞI ĐỘNG HỘI THẢO KHỞI SINH VIỆN MỚI CHO ĐỀ TÀI: {Topic}", topicTitle);

            // 1. Phân tích trích xuất bản thể học và từ khóa cốt lõi từ đề tài hoặc từ gợi ý AIER
            CouncilSpecification councilSpec;
            if (suggestedGenesis != null && !string.IsNullOrWhiteSpace(suggestedGenesis.ProposedCouncilCode))
            {
                councilSpec = new CouncilSpecification
                {
                    CouncilCode = suggestedGenesis.ProposedCouncilCode.ToUpperInvariant().Trim(),
                    DisplayName = suggestedGenesis.ProposedDisplayName,
                    ShortDiscipline = suggestedGenesis.DisciplineArea.Split(new[] { ',', '&', ';' })[0].Trim(),
                    DisciplineArea = suggestedGenesis.DisciplineArea,
                    MaterializedTaxonomyPath = suggestedGenesis.MaterializedTaxonomyPath,
                    EpistemicManifesto = suggestedGenesis.EpistemicRationale,
                    TriggerKeywords = new List<string> { topicTitle.ToLowerInvariant() }
                };
            }
            else
            {
                var fullText = $"{topicTitle} {coreProblemStatement}".Trim();
                councilSpec = SynthesizeCouncilSpecification(fullText, topicTitle);
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

            // 2. Kiểm tra xem Viện với Code này đã tồn tại chưa để tránh trùng lặp
            var existing = await db.UniversalAiCouncils.Include(c => c.Seats)
                .FirstOrDefaultAsync(c => c.CouncilCode == councilSpec.CouncilCode, ct);

            if (existing != null)
            {
                _logger.LogInformation("[EpistemicGenesis] Viện {Code} đã tồn tại trong CSDL, tái sử dụng.", existing.CouncilCode);
                return existing;
            }

            // 3. Khởi tạo Thực Thể Viện Hàn Lâm Mới
            var councilId = Guid.NewGuid();
            var newCouncil = new UniversalAiCouncil
            {
                Id = councilId,
                CouncilCode = councilSpec.CouncilCode,
                DisplayName = councilSpec.DisplayName,
                DisciplineArea = councilSpec.DisciplineArea,
                MaterializedTaxonomyPath = councilSpec.MaterializedTaxonomyPath,
                EpistemicManifesto = councilSpec.EpistemicManifesto,
                BenchmarkPrestigeElo = 2900,
                IsActive = true,
                TriggerKeywordsJson = JsonSerializer.Serialize(councilSpec.TriggerKeywords),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Seats = new List<UniversalCouncilSeat>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        CouncilId = councilId,
                        SeatTitle = $"Viện Trưởng {councilSpec.ShortDiscipline}",
                        Role = CouncilSeatRole.President,
                        PreferredModelCode = "deepseek-r1",
                        IdealArchetype = CognitiveArchetype.DeepReasoner,
                        PersonaPromptDirective = $"Chủ trì đối thoại học thuật, dẫn dắt các lập luận tiên đề về {councilSpec.ShortDiscipline} với tư duy suy luận chuỗi dài CoT sâu sắc.",
                        SortOrder = 1
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        CouncilId = councilId,
                        SeatTitle = $"Đại Biện Phản Biện Đối Kháng",
                        Role = CouncilSeatRole.AdversarialCritic,
                        PreferredModelCode = "claude-3-5-sonnet",
                        IdealArchetype = CognitiveArchetype.LinguisticArbiter,
                        PersonaPromptDirective = $"Mổ xẻ các ngụy biện, kiểm tra tính phản nghiệm thực chứng (Popperian Falsifiability) trong các giả thuyết về {councilSpec.ShortDiscipline}.",
                        SortOrder = 2
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        CouncilId = councilId,
                        SeatTitle = $"Thư Ký Dữ Liệu & Thực Nghiệm",
                        Role = CouncilSeatRole.EmpiricalAuditor,
                        PreferredModelCode = "gemini-3.8-flash",
                        IdealArchetype = CognitiveArchetype.MassiveContext,
                        PersonaPromptDirective = $"Cung cấp dữ liệu quan sát, đối soát bằng chứng lịch sử và thực tiễn trong miền {councilSpec.ShortDiscipline}.",
                        SortOrder = 3
                    }
                }
            };

            db.UniversalAiCouncils.Add(newCouncil);

            // 4. Khởi tạo Hiến chương riêng của Viện mới vào bảng SymposiumPromptTemplates (Layer 2)
            var charterTemplate = new SymposiumPromptTemplate
            {
                Id = Guid.NewGuid(),
                Layer = PromptLayer.CouncilCharter,
                TemplateCode = $"CHARTER_{newCouncil.CouncilCode}",
                TargetKey = newCouncil.CouncilCode,
                Version = 1,
                Locale = "vi-VN",
                TenantId = tenantId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Body = $"=== [HIẾN CHƯƠNG VIỆN: {newCouncil.DisplayName} ({newCouncil.CouncilCode})] ===\n" +
                       $"• Lĩnh vực nghiên cứu: {newCouncil.DisciplineArea}\n" +
                       $"• Trục phân loại tri thức: {newCouncil.MaterializedTaxonomyPath}\n" +
                       $"• Tuyên ngôn nhận thức luận: \"{newCouncil.EpistemicManifesto}\"\n" +
                       $"• Phương pháp luận: Thẩm định khách quan, kết hợp mô hình lý thuyết với dữ liệu thực nghiệm quan trắc."
            };

            db.SymposiumPromptTemplates.Add(charterTemplate);
            await db.SaveChangesAsync(ct);

            // 5. Kích hoạt 0ms Hot-Reload trên toàn bộ hệ thống
            _promptComposer.InvalidateCache();

            _logger.LogInformation("[EpistemicGenesis] ✨ KHỞI SINH THÀNH CÔNG VIỆN MỚI: [{Code}] {Name} với {SeatCount} ghế chuyên gia!",
                newCouncil.CouncilCode, newCouncil.DisplayName, newCouncil.Seats.Count);

            return newCouncil;
        }

        private static CouncilSpecification SynthesizeCouncilSpecification(string fullText, string topicTitle)
        {
            var textLower = fullText.ToLowerInvariant();

            // 1. Phân loại miền nhận thức dựa trên các cụm từ hạt giống
            if (textLower.Contains("nông nghiệp") || textLower.Contains("lúa") || textLower.Contains("canh tác") || textLower.Contains("cây trồng") || textLower.Contains("đất") || textLower.Contains("thổ nhưỡng"))
            {
                return new CouncilSpecification
                {
                    CouncilCode = "AGRO_ECOLOGY",
                    DisplayName = "Viện Nông Nghiệp Sinh Thái & An Ninh Lương Thực",
                    ShortDiscipline = "Nông Nghiệp Sinh Thái",
                    DisciplineArea = "Nông học chính xác, Sinh thái học đất, Khoa học cây trồng & Thích ứng biến đổi khí hậu",
                    MaterializedTaxonomyPath = "/NATURE/AGRICULTURE/ECOLOGY",
                    EpistemicManifesto = "Nông nghiệp bền vững là sự cộng sinh hài hòa giữa trí tuệ công nghệ và chu trình tuần hoàn của đất, nước và khí quyển.",
                    TriggerKeywords = new List<string> { "nông nghiệp", "nông học", "canh tác", "lúa nước", "cây trồng", "thổ nhưỡng", "phân bón", "thủy lợi", "an ninh lương thực", "sinh thái nông nghiệp" }
                };
            }

            if (textLower.Contains("biển") || textLower.Contains("đại dương") || textLower.Contains("hải dương") || textLower.Contains("thủy sản") || textLower.Contains("rạn san hô"))
            {
                return new CouncilSpecification
                {
                    CouncilCode = "OCEANOGRAPHY",
                    DisplayName = "Viện Hải Dương Học & Địa Sinh Quyển Biển",
                    ShortDiscipline = "Hải Dương Học",
                    DisciplineArea = "Hải dương học vật lý, Sinh học biển sâu, Động lực học hải lưu & Khí hậu toàn cầu",
                    MaterializedTaxonomyPath = "/NATURE/EARTH/OCEANOGRAPHY",
                    EpistemicManifesto = "Đại dương là trái tim điều hòa nhiệt năng và cái nôi khởi nguyên duy trì sự sống của hành tinh.",
                    TriggerKeywords = new List<string> { "hải dương", "đại dương", "biển", "hải lưu", "thủy sản", "san hô", "sinh vật biển", "sóng thần", "độ mặn" }
                };
            }

            if (textLower.Contains("khảo cổ") || textLower.Contains("cổ đại") || textLower.Contains("di tích") || textLower.Contains("văn minh cổ") || textLower.Contains("hóa thạch"))
            {
                return new CouncilSpecification
                {
                    CouncilCode = "ARCHAEOLOGY",
                    DisplayName = "Viện Khảo Cổ Học & Lịch Sử Các Nền Văn Minh",
                    ShortDiscipline = "Khảo Cổ Học",
                    DisciplineArea = "Khảo cổ học niên đại, Nhân chủng học văn hóa, Văn minh cổ đại & Cổ sinh vật học",
                    MaterializedTaxonomyPath = "/HUMANITY/HISTORY/ARCHAEOLOGY",
                    EpistemicManifesto = "Dấu tích quá khứ là tấm gương phản chiếu quy luật hưng suy và bản sắc vĩnh hằng của nền văn minh nhân loại.",
                    TriggerKeywords = new List<string> { "khảo cổ", "cổ đại", "di tích", "văn minh cổ", "hóa thạch", "tiền sử", "cổ vật", "khai quật" }
                };
            }

            if (textLower.Contains("y học") || textLower.Contains("bệnh") || textLower.Contains("dược") || textLower.Contains("tế bào gốc") || textLower.Contains("miễn dịch") || textLower.Contains("điều trị"))
            {
                return new CouncilSpecification
                {
                    CouncilCode = "PRECISION_MEDICINE",
                    DisplayName = "Viện Y Học Chính Xác & Tái Tạo Sinh Học",
                    ShortDiscipline = "Y Học Chính Xác",
                    DisciplineArea = "Y học hệ thống, Liệu pháp gen, Dược lý học phân tử & Tế bào gốc tái tạo",
                    MaterializedTaxonomyPath = "/LIFE/MEDICINE/PRECISION",
                    EpistemicManifesto = "Y học vị nhân sinh hướng tới việc giải mã tận gốc căn nguyên bệnh tật ở cấp độ phân tử và phục hồi toàn diện sức khỏe con người.",
                    TriggerKeywords = new List<string> { "y học", "bệnh học", "dược học", "điều trị", "tế bào gốc", "miễn dịch", "ung thư", "gen trị liệu", "vắc xin" }
                };
            }

            // Mẫu tổng hợp tự thích ứng cho đề tài độc bản chưa từng có
            var cleanTitle = Regex.Replace(topicTitle, @"[^\p{L}\p{N}\s]", "").Trim();
            var words = cleanTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var mainKeywords = words.Where(w => w.Length > 2).Take(5).ToList();
            var shortCode = string.Join("_", mainKeywords.Take(2)).ToUpperInvariant();
            if (shortCode.Length < 3) shortCode = "CUSTOM_EPISTEMIC_" + Math.Abs(topicTitle.GetHashCode() % 1000);

            return new CouncilSpecification
            {
                CouncilCode = $"SPECIAL_{shortCode}",
                DisplayName = $"Viện Nghiên Cứu Chuyên Đề: {topicTitle}",
                ShortDiscipline = topicTitle,
                DisciplineArea = $"Chuyên khảo liên ngành sâu về [{topicTitle}] và các vấn đề phụ thuộc",
                MaterializedTaxonomyPath = $"/SPECIALIZED/{shortCode}",
                EpistemicManifesto = $"Mọi hiện tượng tri thức đặc thù đều ẩn chứa những quy luật bản thể đòi hỏi phương pháp tiếp cận biện chứng liên ngành.",
                TriggerKeywords = mainKeywords
            };
        }

        private class CouncilSpecification
        {
            public string CouncilCode { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string ShortDiscipline { get; set; } = string.Empty;
            public string DisciplineArea { get; set; } = string.Empty;
            public string MaterializedTaxonomyPath { get; set; } = string.Empty;
            public string EpistemicManifesto { get; set; } = string.Empty;
            public List<string> TriggerKeywords { get; set; } = new();
        }
    }
}
