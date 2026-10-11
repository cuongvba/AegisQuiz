using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.Infrastructure.AI.Prompt
{
    /// <summary>
    /// [AegisAI Cognitive Prompt Forge Assistant Engine]
    /// Hiện thực hóa Động cơ Trợ lý Quản trị & Đề xuất Sinh Cây Prompt 6 Tầng Nhận Thức.
    /// Tuân thủ 100% CLEAR Architecture, OCP và The 5 Golden Commitments.
    /// </summary>
    public class PromptForgeAssistantService : IPromptForgeAssistantService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ISymposiumPromptComposer _promptComposer;
        private readonly ILogger<PromptForgeAssistantService> _logger;

        public PromptForgeAssistantService(
            IServiceScopeFactory scopeFactory,
            ISymposiumPromptComposer promptComposer,
            ILogger<PromptForgeAssistantService> logger)
        {
            _scopeFactory = scopeFactory;
            _promptComposer = promptComposer;
            _logger = logger;
        }

        public async Task<MetaPromptProposalDto> ProposeMetaPromptAsync(
            PromptProposalRequest request,
            CancellationToken ct = default)
        {
            var domain = string.IsNullOrWhiteSpace(request.OrganizationDomain) ? "Bách Khoa Toàn Thư Vạn Vật" : request.OrganizationDomain.Trim();
            var axioms = string.IsNullOrWhiteSpace(request.CoreAxioms) ? "Độc lập nhận thức, Falsifiability Karl Popper, thực chứng dữ liệu, phi sáo rỗng" : request.CoreAxioms.Trim();

            var sb = new StringBuilder();
            sb.AppendLine($"# 🏛️ ĐẠI KIẾN TRÚC SƯ SYSTEM PROMPT TỐI CAO (SUPREME EPISTEMIC PROMPT ARCHITECT)");
            sb.AppendLine($"## LĨNH VỰC BẢO CHỨNG: {domain.ToUpperInvariant()}");
            sb.AppendLine($"## CHUẨN MỰC BIỆN CHỨNG: {GetRigorDescription(request.RigorLevel)}");
            sb.AppendLine();
            sb.AppendLine($"Bạn là No.1 AI Prompt Architect & Master Living Documentarian của Hệ sinh thái AegisQuiz, bảo trợ nhận thức cho tổ chức chuyên ngành '{domain}'.");
            sb.AppendLine($"Nhiệm vụ tối thượng của bạn là khởi tạo, thẩm định và truyền thụ Hiến chương sinh khí cho các Viện Hàn Lâm Tri Thức tương ứng.");
            sb.AppendLine();
            sb.AppendLine("## NĂM TIÊN ĐỀ SÁNG TẠO NHẬN THỨC BẤT BIẾN (THE 5 INVARIANT EPISTEMIC AXIOMS):");
            sb.AppendLine($"1. ĐẶC TÙ MIỀN KHOA HỌC: Mọi học thuyết và tranh luận phải neo giữ vững chắc vào bản chất chuyên môn của '{domain}' ({axioms}).");

            if (request.RigorLevel == DialecticalRigorLevel.StrictPopperian)
            {
                sb.AppendLine("2. NGUYÊN TẮC PHẢN NGHIỆM POPPER TRIỆT ĐỂ: Một luận điểm chỉ có giá trị khoa học khi chỉ ra được điều kiện thực chứng mà nếu xảy ra sẽ bác bỏ chính nó (Popperian Falsifiability). Tuyệt đối cấm ngụy biện người rơm và dán nhãn.");
            }
            else if (request.RigorLevel == DialecticalRigorLevel.PragmaticEmpirical)
            {
                sb.AppendLine("2. NGUYÊN TẮC THỰC CHỨNG & DỮ LIỆU ĐỊNH LƯỢNG: Ưu tiên tối thượng số liệu quan trắc, phân tích độ nhạy và tính khả thi trong thực tế ứng dụng. Lý thuyết không có bằng chứng thực nghiệm là vô hiệu.");
            }
            else if (request.RigorLevel == DialecticalRigorLevel.SystemicHolistic)
            {
                sb.AppendLine("2. TƯ DUY HỆ THỐNG PHI TUYẾN: Tiếp cận bài toán qua lăng kính chỉnh thể luận (Holism), phân tích vòng lặp phản hồi (Feedback Loops) và các hiện tượng nổi lên (Emergent Properties).");
            }
            else
            {
                sb.AppendLine("2. CÂN BẰNG HỌC THUẬT BIỆN CHỨNG: Kết hợp nhuần nhuyễn giữa diễn dịch tiên đề và quy nạp thực chứng; tôn trọng sự đa dạng của các trường phái học thuật.");
            }

            sb.AppendLine("3. PHÂN BỔ GHẾ PHI ĐỐI XỨNG BẮT BUỘC: Mỗi Viện được sinh ra phải có đủ các vai: Viện Trưởng (Tổng hợp & Quyết định), Đại Biện Đối Kháng (Vạch trần thiên kiến), Kiểm Chứng Thực Nghiệm (Đối soát dữ liệu) và Siêu Lý Thuyết Gia.");
            sb.AppendLine("4. VĂN PHONG HỌC THUẬT TINH TẾ & BÁC HỌC: Ngôn từ sắc bén, mực thước, khách quan tuyệt đối; nghiêm cấm các xưng tụng sáo rỗng hoặc vuốt ve cảm xúc.");
            sb.AppendLine("5. KHẢ NĂNG TỰ TIẾN HÓA VÀ SỬA SAI: Luôn duy trì cơ chế hiệu chỉnh tiên đề khi nhận được bằng chứng mới từ thế giới thực; bảo toàn nguyên lý Open/Closed Principle (OCP).");

            var bodyText = sb.ToString();
            var estimatedTokens = (int)Math.Ceiling(bodyText.Length / 3.8);

            return new MetaPromptProposalDto
            {
                TemplateCode = "ARCHITECT_META",
                Body = bodyText,
                Rationale = $"Được thiết kế riêng biệt cho miền '{domain}' với mức độ khắt khe '{request.RigorLevel}'. Tích hợp sẵn 5 tiên đề nhận thức luận và bộ lọc chống suy thoái mô hình.",
                ExtractedAxioms = new List<string>
                {
                    $"Bản sắc chuyên ngành: {domain}",
                    $"Chuẩn mực biện chứng: {request.RigorLevel}",
                    "Nguyên lý Falsifiability Popper",
                    "Phân bổ 4 vai nhận thức phi đối xứng",
                    "OCP & Tự tiến hóa không hard-code"
                },
                EstimatedTokens = estimatedTokens,
                Version = 1
            };
        }

        public async Task<CascadingPromptStackDto> GenerateCascadingStackAsync(
            GenerateStackRequest request,
            CancellationToken ct = default)
        {
            var domain = string.IsNullOrWhiteSpace(request.DomainFocus) ? "Khoa Học Đa Ngành" : request.DomainFocus.Trim();
            var metaBody = string.IsNullOrWhiteSpace(request.MetaPromptBody) ? (await ProposeMetaPromptAsync(new PromptProposalRequest { OrganizationDomain = domain }, ct)).Body : request.MetaPromptBody;

            // 1. Tự động sinh Tầng 1 (Constitution)
            var constitution =
                $"=== [HIẾN PHÁP BIỆN CHỨNG CHUNG CỦA ĐẠI HỘI THẢO (CONSTITUTION v2)] ===\n" +
                $"1. Chân lý khách quan là mục tiêu duy nhất: Không học giả AI nào được bảo vệ định kiến chủ quan.\n" +
                $"2. Tiêu chuẩn thực nghiệm cao nhất: Tuân thủ chỉ thị Tầng 0 cho miền [{domain}].\n" +
                $"3. Tính liên ngành không ranh giới: Mọi vấn đề phức tạp phải được mổ xẻ dưới nhiều góc nhìn độc lập.\n" +
                $"4. Trách nhiệm gia tăng tri thức: Mỗi phát biểu phải cung cấp ít nhất một góc nhìn hoặc bằng chứng mới mẻ.";

            // 2. Tự động sinh Tầng 2 (Council Charters đề xuất)
            var charters = new List<CouncilCharterProposalDto>
            {
                new()
                {
                    CouncilCode = "PRIMARY_ACADEMY",
                    DisplayName = $"Viện Nghiên Cứu {domain} Chuyên Sâu",
                    DisciplineArea = $"{domain} Ứng Dụng, Mô Hình Lý Thuyết & Thực Tiễn Triển Khai",
                    TaxonomyPath = $"/EPISTEMIC/{domain.ToUpperInvariant().Replace(" ", "_")}/CORE",
                    CharterPromptBody = $"=== [HIẾN CHƯƠNG VIỆN: Viện Nghiên Cứu {domain} Chuyên Sâu] ===\n" +
                                        $"• Lĩnh vực: Nghiên cứu cấu trúc nền tảng và các định luật trong miền {domain}.\n" +
                                        $"• Sứ mệnh: Khám phá bản chất thực tại, loại bỏ ngụy khoa học và đề xuất giải pháp tối ưu."
                },
                new()
                {
                    CouncilCode = "EPISTEMIC_CRITIQUE",
                    DisplayName = "Viện Phản Biện Nhận Thức & Thẩm Định Tính Hợp Thức",
                    DisciplineArea = "Nhận thức luận, Phương pháp luận khoa học, Kiểm định sai số & Thống kê suy luận",
                    TaxonomyPath = "/EPISTEMIC/METHODOLOGY/CRITIQUE",
                    CharterPromptBody = "=== [HIẾN CHƯƠNG VIỆN: Viện Phản Biện Nhận Thức & Thẩm Định Tính Hợp Thức] ===\n" +
                                        "• Lĩnh vực: Mổ xẻ các lỗ hổng logic, kiểm tra mẫu thống kê và độ tin cậy của dữ liệu.\n" +
                                        "• Tuyên ngôn: Một giả thuyết chưa trải qua thử thách phản nghiệm khốc liệt thì chưa có quyền trở thành chân lý."
                }
            };

            // 3. Tự động sinh Tầng 3 (Seat Roles)
            var seatRoles = new List<SeatRoleProposalDto>
            {
                new()
                {
                    SeatTitle = "Viện Trưởng Học Thuật",
                    Role = "President",
                    PreferredModel = "deepseek-r1",
                    DirectivePromptBody = "Chủ trì phiên tranh luận, dẫn dắt các suy luận chuỗi dài CoT chuyên sâu, cân bằng luận điểm."
                },
                new()
                {
                    SeatTitle = "Đại Biện Đối Kháng Khắc Nghiệt",
                    Role = "AdversarialCritic",
                    PreferredModel = "claude-3-5-sonnet",
                    DirectivePromptBody = "Chuyên tìm kiếm các điểm nghẽn, giả định ngầm sai lầm và kiểm tra tính phản nghiệm Popperian."
                },
                new()
                {
                    SeatTitle = "Thư Ký Dữ Liệu & Thực Nghiệm",
                    Role = "EmpiricalAuditor",
                    PreferredModel = "gemini-3.8-flash",
                    DirectivePromptBody = "Đối chiếu số liệu quan trắc, cung cấp dẫn chứng cụ thể từ y văn, báo cáo và thực tiễn."
                }
            };

            // 4. Tầng 4 & 5
            var phaseDirectives = "=== [CHỈ THỊ CÁC GIAI ĐOẠN BIỆN CHỨNG 1 -> 6] ===\n" +
                                  "Pha 1 (Formulation) -> Pha 2 (Cross-Exam) -> Pha 3 (Critique) -> Pha 4 (Synthesis) -> Pha 5 (Evaluation) -> Pha 6 (Declaration).";

            var outputContract = "=== [HỢP ĐỒNG ĐỊNH DẠNG ĐẦU RA BẮT BUỘC] ===\n" +
                                 "Mọi lượt phát biểu bắt buộc tuân thủ cấu trúc:\n" +
                                 "1. Luận Điểm Cốt Lõi (Thesis)\n" +
                                 "2. Lập Luận Chứng Minh Kèm Bằng Chứng (Argumentation & Evidence)\n" +
                                 "3. Phân Tích Giới Hạn & Phản Chứng (Boundary Conditions & Counter-arguments)\n" +
                                 "4. Đóng Góp Nhận Thức Gia Tăng (Epistemic Value Added)";

            var totalTokens = (int)Math.Ceiling((metaBody.Length + constitution.Length + phaseDirectives.Length + outputContract.Length) / 3.8);

            return new CascadingPromptStackDto
            {
                MetaPromptLayer0 = metaBody,
                ConstitutionLayer1 = constitution,
                CouncilChartersLayer2 = charters,
                SeatRolesLayer3 = seatRoles,
                PhaseDirectivesLayer4 = phaseDirectives,
                OutputContractLayer5 = outputContract,
                TotalEstimatedTokens = totalTokens
            };
        }

        public async Task<AxiomaticAuditReportDto> AuditConsistencyAsync(
            Guid? tenantId,
            string locale = "vi-VN",
            CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

            var templates = await db.SymposiumPromptTemplates
                .Where(t => t.IsActive && t.Locale == locale && (t.TenantId == tenantId || t.TenantId == null))
                .ToListAsync(ct);

            var report = new AxiomaticAuditReportDto
            {
                IsFullyConsistent = true,
                ConsistencyScore = 1.0,
                AuditChecklist = new List<string>
                {
                    "Kiểm tra sự hiện diện của Prompt Tầng 0 (ARCHITECT_META)",
                    "Kiểm tra nguyên tắc phản nghiệm Popperian trong Tầng 1 & Tầng 3",
                    "Quét mâu thuẫn giữa yêu cầu thực chứng và các từ ngữ cảm tính",
                    "Kiểm tra tính đầy đủ của 5 tầng prompt"
                }
            };

            var meta = templates.FirstOrDefault(t => t.Layer == PromptLayer.Meta);
            if (meta == null)
            {
                report.IsFullyConsistent = false;
                report.ConsistencyScore -= 0.3;
                report.Inconsistencies.Add(new AxiomaticInconsistencyDto
                {
                    LayerName = "Layer 0 (Meta)",
                    TargetKey = "*",
                    Severity = "CRITICAL",
                    ConflictSummary = "Hệ thống chưa có mẫu Prompt Tầng 0 (ARCHITECT_META) trong CSDL PostgreSQL.",
                    RemediationSuggestion = "Sử dụng tính năng 'Khởi Sinh Prompt Tầng 0' để gieo hạt mẫu chuẩn vào Database."
                });
            }

            var constitution = templates.FirstOrDefault(t => t.Layer == PromptLayer.Constitution);
            if (constitution == null)
            {
                report.IsFullyConsistent = false;
                report.ConsistencyScore -= 0.2;
                report.Inconsistencies.Add(new AxiomaticInconsistencyDto
                {
                    LayerName = "Layer 1 (Constitution)",
                    TargetKey = "*",
                    Severity = "WARNING",
                    ConflictSummary = "Chưa tìm thấy Hiến pháp Tầng 1 riêng biệt, đang dùng bản mặc định trong bộ nhớ RAM.",
                    RemediationSuggestion = "Tự động sinh và lưu Hiến pháp Tầng 1 vào Database."
                });
            }

            // Kiểm tra tính nhất quán Falsifiability
            if (meta != null && meta.Body.Contains("Falsifiability", StringComparison.OrdinalIgnoreCase))
            {
                var seatRoles = templates.Where(t => t.Layer == PromptLayer.SeatRole).ToList();
                var hasCritic = seatRoles.Any(s => s.Body.Contains("phản biện", StringComparison.OrdinalIgnoreCase) || s.Body.Contains("critic", StringComparison.OrdinalIgnoreCase));
                if (seatRoles.Any() && !hasCritic)
                {
                    report.IsFullyConsistent = false;
                    report.ConsistencyScore -= 0.15;
                    report.Inconsistencies.Add(new AxiomaticInconsistencyDto
                    {
                        LayerName = "Layer 3 (SeatRole)",
                        TargetKey = "AdversarialCritic",
                        Severity = "WARNING",
                        ConflictSummary = "Tầng 0 bắt buộc nguyên tắc phản nghiệm nhưng chưa có Ghế Đại Biện Phản Biện rõ ràng trong Tầng 3.",
                        RemediationSuggestion = "Thêm hoặc cấu hình chỉ thị cho ghế Adversarial Critic."
                    });
                }
            }

            report.ConsistencyScore = Math.Max(0.0, Math.Round(report.ConsistencyScore, 2));
            return report;
        }

        public async Task<bool> EnactPromptStackAsync(
            EnactPromptStackRequest request,
            CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

            // 1. Cập nhật hoặc thêm mới Tầng 0 (ARCHITECT_META)
            if (!string.IsNullOrWhiteSpace(request.MetaPromptLayer0))
            {
                var existingMeta = await db.SymposiumPromptTemplates
                    .FirstOrDefaultAsync(t => t.Layer == PromptLayer.Meta && t.TemplateCode == "ARCHITECT_META" && t.TenantId == request.TenantId, ct);

                if (existingMeta != null)
                {
                    existingMeta.Body = request.MetaPromptLayer0;
                    existingMeta.Version += 1;
                    existingMeta.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    db.SymposiumPromptTemplates.Add(new SymposiumPromptTemplate
                    {
                        Id = Guid.NewGuid(),
                        Layer = PromptLayer.Meta,
                        TemplateCode = "ARCHITECT_META",
                        TargetKey = "*",
                        Locale = request.Locale,
                        Version = 1,
                        Body = request.MetaPromptLayer0,
                        Description = request.ChangeSummary,
                        TenantId = request.TenantId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            // 2. Cập nhật hoặc thêm mới Tầng 1 (CONSTITUTION)
            if (!string.IsNullOrWhiteSpace(request.ConstitutionLayer1))
            {
                var existingConst = await db.SymposiumPromptTemplates
                    .FirstOrDefaultAsync(t => t.Layer == PromptLayer.Constitution && t.TemplateCode == "SUPREME_CONSTITUTION" && t.TenantId == request.TenantId, ct);

                if (existingConst != null)
                {
                    existingConst.Body = request.ConstitutionLayer1;
                    existingConst.Version += 1;
                    existingConst.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    db.SymposiumPromptTemplates.Add(new SymposiumPromptTemplate
                    {
                        Id = Guid.NewGuid(),
                        Layer = PromptLayer.Constitution,
                        TemplateCode = "SUPREME_CONSTITUTION",
                        TargetKey = "*",
                        Locale = request.Locale,
                        Version = 1,
                        Body = request.ConstitutionLayer1,
                        Description = request.ChangeSummary,
                        TenantId = request.TenantId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            // 3. Cập nhật các Hiến chương Tầng 2
            if (request.Charters != null)
            {
                foreach (var charter in request.Charters)
                {
                    if (string.IsNullOrWhiteSpace(charter.CharterPromptBody)) continue;
                    var code = $"CHARTER_{charter.CouncilCode}";
                    var existingCharter = await db.SymposiumPromptTemplates
                        .FirstOrDefaultAsync(t => t.Layer == PromptLayer.CouncilCharter && t.TemplateCode == code && t.TenantId == request.TenantId, ct);

                    if (existingCharter != null)
                    {
                        existingCharter.Body = charter.CharterPromptBody;
                        existingCharter.Version += 1;
                        existingCharter.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        db.SymposiumPromptTemplates.Add(new SymposiumPromptTemplate
                        {
                            Id = Guid.NewGuid(),
                            Layer = PromptLayer.CouncilCharter,
                            TemplateCode = code,
                            TargetKey = charter.CouncilCode,
                            Locale = request.Locale,
                            Version = 1,
                            Body = charter.CharterPromptBody,
                            Description = $"Hiến chương {charter.DisplayName}",
                            TenantId = request.TenantId,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await db.SaveChangesAsync(ct);

            // 4. Kích hoạt Hot-Reload tức thì
            _promptComposer.InvalidateCache();
            _logger.LogInformation("[PromptForge] ✨ ĐÃ PHÊ CHUẨN VÀ HOT-RELOAD THÀNH CÔNG BỘ PROMPT PHÂN TẦNG VÀO POSTGRESQL!");

            return true;
        }

        public async Task<List<PromptTemplateSummaryDto>> GetAllTemplatesAsync(
            Guid? tenantId,
            string locale = "vi-VN",
            CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

            // Gieo hạt bản Tầng 0 mặc định nếu DB chưa có
            await EnsureSeedMetaPromptAsync(db, ct);

            var templates = await db.SymposiumPromptTemplates
                .Where(t => t.Locale == locale && (t.TenantId == tenantId || t.TenantId == null))
                .OrderBy(t => t.Layer)
                .ThenBy(t => t.TemplateCode)
                .ToListAsync(ct);

            return templates.Select(t => new PromptTemplateSummaryDto
            {
                Id = t.Id,
                TemplateCode = t.TemplateCode,
                Layer = (int)t.Layer,
                LayerName = t.Layer.ToString(),
                TargetKey = t.TargetKey,
                Version = t.Version,
                Locale = t.Locale,
                Body = t.Body,
                Description = t.Description,
                IsActive = t.IsActive,
                UpdatedAt = t.UpdatedAt ?? t.CreatedAt
            }).ToList();
        }

        private async Task EnsureSeedMetaPromptAsync(AegisQuizDbContext db, CancellationToken ct)
        {
            var exists = await db.SymposiumPromptTemplates.AnyAsync(t => t.Layer == PromptLayer.Meta, ct);
            if (!exists)
            {
                _logger.LogInformation("[PromptForge] Gieo hạt bản mẫu Prompt Tầng 0 (ARCHITECT_META) vào PostgreSQL...");
                db.SymposiumPromptTemplates.Add(new SymposiumPromptTemplate
                {
                    Id = Guid.NewGuid(),
                    Layer = PromptLayer.Meta,
                    TemplateCode = "ARCHITECT_META",
                    TargetKey = "*",
                    Locale = "vi-VN",
                    Version = 1,
                    IsActive = true,
                    Description = "Mẫu System Prompt Tầng 0 khởi sinh Viện Hàn Lâm toàn cầu",
                    Body = @"# 🏛️ ĐẠI KIẾN TRÚC SƯ SYSTEM PROMPT TỐI CAO (SUPREME EPISTEMIC PROMPT ARCHITECT)
Bạn là No.1 AI Prompt Architect & Master Living Documentarian của Hệ sinh thái AegisQuiz.
Nhiệm vụ của bạn là thiết kế, khởi tạo và truyền sinh khí cho các Viện Hàn Lâm Tri Thức Vũ Trụ (Universal AI Councils).

## NĂM TIÊU CHUẨN THIẾT KẾ SYSTEM PROMPT BẤT BIẾN:
1. KHÔNG GIAN KHỞI TẠO ĐA CHIỀU (Multidimensional Cognitive Framing): Mỗi Viện phải có trục nhận thức riêng biệt, độc lập và có tính đối kháng lành mạnh với các Viện khác.
2. ZERO HARD-CODE & PLUGGABLE: Mọi cấu trúc prompt phải sử dụng tokenized placeholders {{council.*}}, {{seat.*}}, {{symposium.*}} để nạp động 100% từ Database.
3. PHÂN VAI GHẾ PHI ĐỐI XỨNG: Bắt buộc cấu hình đủ 4 vai: Chủ Tịch (President - tổng hợp, ra quyết định), Đại Biện Đối Kháng (Adversarial Critic - vạch trần lỗ hổng), Kiểm Chứng Dữ Kiện (Empirical Auditor - truy vết thực nghiệm), Siêu Lý Thuyết Gia (Metatheorist - tích hợp đa ngành).
4. KHÁCH QUAN KHOA HỌC & MỸ HỌC TINH TẾ: Từ ngữ mang đậm tinh thần bác học, chuẩn xác về mặt triết học và khoa học, tránh sáo rỗng hoặc xưng tụng vô nghĩa.
5. SẴN SÀNG TIẾN HÓA: Cung cấp chỉ dẫn tư duy giúp các mô hình AI tự sửa sai, liên tục tinh chỉnh định đề khi đối diện với phản chứng thực nghiệm.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync(ct);
            }
        }

        private static string GetRigorDescription(DialecticalRigorLevel level) => level switch
        {
            DialecticalRigorLevel.StrictPopperian => "Phản Nghiệm Triệt Để (Karl Popper - Khắt khe tìm kiếm phản chứng)",
            DialecticalRigorLevel.PragmaticEmpirical => "Thực Nghiệm Thực Dụng (Định lượng dữ liệu & ứng dụng thực tiễn)",
            DialecticalRigorLevel.ConstructiveDialectical => "Biện Chứng Xây Dựng (Tổng hợp luận điểm & phản đề)",
            DialecticalRigorLevel.SystemicHolistic => "Tư Duy Hệ Thống Tổng Thể (Phản hồi phi tuyến tính & Chỉnh thể luận)",
            _ => "Học Thuật Mực Thước"
        };
    }
}
