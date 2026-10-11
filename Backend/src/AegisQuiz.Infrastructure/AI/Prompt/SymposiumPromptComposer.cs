using System;
using System.Collections.Concurrent;
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
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.Infrastructure.AI.Prompt
{
    /// <summary>
    /// [AegisAI Sovereign Prompt Composer]
    /// Hiện thực bộ ghép System Prompt phân tầng 6 lớp cho Đại Hội Thảo Bách Khoa Vạn Vật Tri Thức.
    /// Tuân thủ OCP & Zero Hard-code: Mọi mẫu prompt nạp từ DB (SymposiumPromptTemplates),
    /// tự động phân giải độ ưu tiên (Tenant > Global, Specific Key > "*", Version cao nhất),
    /// nội suy tham số an toàn và hỗ trợ Hot-Reload qua InvalidateCache().
    /// </summary>
    public class SymposiumPromptComposer : ISymposiumPromptComposer
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEnumerable<ICognitiveLayerPlugin> _plugins;
        private readonly ILogger<SymposiumPromptComposer> _logger;
        private static readonly ConcurrentDictionary<string, List<SymposiumPromptTemplate>> _templateCache = new();
        private static readonly Regex _placeholderRegex = new(@"\{\{([\w\.]+)\}\}", RegexOptions.Compiled);

        public SymposiumPromptComposer(
            IServiceScopeFactory scopeFactory,
            IEnumerable<ICognitiveLayerPlugin> plugins,
            ILogger<SymposiumPromptComposer> logger)
        {
            _scopeFactory = scopeFactory;
            _plugins = plugins ?? Enumerable.Empty<ICognitiveLayerPlugin>();
            _logger = logger;
        }

        public void InvalidateCache()
        {
            _templateCache.Clear();
            _logger.LogInformation("[PromptComposer] Đã xóa cache mẫu prompt. Kích hoạt Hot-Reload từ DB.");
        }

        public async Task<ComposedSystemPrompt> ComposeSeatPromptAsync(
            PromptCompositionContext context,
            CancellationToken ct = default)
        {
            var templates = await GetAllActiveTemplatesAsync(context.Locale, ct);
            var appliedTemplates = new List<string>();
            var missingLayers = new List<string>();
            var unresolvedPlaceholders = new List<string>();
            var sb = new StringBuilder();

            // Biến nội suy sẵn sàng
            var variables = BuildVariableMap(context);

            // 5 Lớp cấu thành prompt ghế ngồi cơ bản:
            // 1. Constitution -> 2. CouncilCharter -> 3. SeatRole -> 4. PhaseDirective -> 5. OutputContract
            var targetLayers = new[]
            {
                PromptLayer.Constitution,
                PromptLayer.CouncilCharter,
                PromptLayer.SeatRole,
                PromptLayer.PhaseDirective,
                PromptLayer.OutputContract
            };

            foreach (var layer in targetLayers)
            {
                var matched = ResolveBestTemplate(templates, layer, context);
                if (matched != null)
                {
                    appliedTemplates.Add($"{matched.TemplateCode}@v{matched.Version}[{(matched.TenantId.HasValue ? "Tenant" : "Global")}]");
                    var interpolated = InterpolateVariables(matched.Body, variables, unresolvedPlaceholders);
                    sb.AppendLine(interpolated);
                    sb.AppendLine();
                }
                else
                {
                    missingLayers.Add(layer.ToString());
                    // Fallback khẩn cấp cho từng lớp nếu DB trống hạt giống
                    var fallback = GetFallbackLayerText(layer, context, variables);
                    if (!string.IsNullOrEmpty(fallback))
                    {
                        sb.AppendLine(fallback);
                        sb.AppendLine();
                    }
                }
            }

            // 6. PHỔ NHẬN THỨC MỞ RỘNG VÔ HẠN (L6..Ln VIA COGNITIVE LAYER PLUGINS - OCP)
            if (_plugins != null)
            {
                foreach (var plugin in _plugins.OrderBy(p => p.LayerOrder))
                {
                    try
                    {
                        if (plugin.IsApplicable(context))
                        {
                            var contribution = await plugin.ProcessLayerAsync(context, ct);
                            if (contribution != null && !string.IsNullOrWhiteSpace(contribution.DirectiveText))
                            {
                                appliedTemplates.Add($"{plugin.LayerKey}@Plugin");
                                if (!string.IsNullOrWhiteSpace(contribution.HeaderText))
                                {
                                    sb.AppendLine(contribution.HeaderText);
                                }
                                sb.AppendLine(contribution.DirectiveText);
                                sb.AppendLine();
                            }
                        }
                    }
                    catch (Exception exPlugin)
                    {
                        _logger.LogWarning(exPlugin, "[PromptComposer] Lỗi khi thực thi plugin tầng nhận thức {Key}: {Message}", plugin.LayerKey, exPlugin.Message);
                    }
                }
            }

            var promptText = sb.ToString().Trim();
            return new ComposedSystemPrompt
            {
                Text = promptText,
                AppliedTemplates = appliedTemplates,
                MissingLayers = missingLayers,
                UnresolvedPlaceholders = unresolvedPlaceholders.Distinct().ToList(),
                EstimatedTokens = (int)Math.Ceiling(promptText.Length / 3.8) // Hệ số xấp xỉ tiếng Việt / token
            };
        }

        public async Task<ComposedSystemPrompt> ComposeArchitectPromptAsync(
            string locale,
            Guid? tenantId,
            CancellationToken ct = default)
        {
            var templates = await GetAllActiveTemplatesAsync(locale, ct);
            var matched = templates
                .Where(t => t.Layer == PromptLayer.Meta && t.IsActive)
                .OrderByDescending(t => t.TenantId == tenantId)
                .ThenByDescending(t => t.Version)
                .FirstOrDefault();

            var applied = new List<string>();
            string text;

            if (matched != null)
            {
                applied.Add($"{matched.TemplateCode}@v{matched.Version}");
                text = matched.Body;
            }
            else
            {
                _logger.LogWarning("[PromptComposer] CẢNH BÁO KIẾN TRÚC: CSDL thiếu mẫu prompt Layer Meta (ARCHITECT_META) cho locale '{Locale}'. Kích hoạt Emergency In-Memory Fallback!", locale);
                text = GetEmergencyFallbackMetaPrompt(locale);
                applied.Add("ARCHITECT_META_EMBEDDED_FALLBACK@v1");
            }

            return new ComposedSystemPrompt
            {
                Text = text,
                AppliedTemplates = applied,
                MissingLayers = new List<string>(),
                UnresolvedPlaceholders = new List<string>(),
                EstimatedTokens = (int)Math.Ceiling(text.Length / 3.8)
            };
        }

        private async Task<List<SymposiumPromptTemplate>> GetAllActiveTemplatesAsync(string locale, CancellationToken ct)
        {
            var cacheKey = string.IsNullOrWhiteSpace(locale) ? "vi-VN" : locale;
            if (_templateCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();
                var items = await db.SymposiumPromptTemplates
                    .AsNoTracking()
                    .Where(t => t.IsActive && (t.Locale == cacheKey || t.Locale == "*"))
                    .ToListAsync(ct);

                _templateCache[cacheKey] = items;
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PromptComposer] Không thể nạp bảng SymposiumPromptTemplates từ DB, kích hoạt Fallback Registry.");
                return new List<SymposiumPromptTemplate>();
            }
        }

        private SymposiumPromptTemplate? ResolveBestTemplate(
            List<SymposiumPromptTemplate> templates,
            PromptLayer layer,
            PromptCompositionContext context)
        {
            string targetKey = layer switch
            {
                PromptLayer.CouncilCharter => context.Council.CouncilCode,
                PromptLayer.SeatRole => context.Seat.Role.ToString(),
                PromptLayer.PhaseDirective => context.PhaseCode,
                _ => "*"
            };

            return templates
                .Where(t => t.Layer == layer && t.IsActive)
                .OrderByDescending(t => t.TenantId == context.TenantId && context.TenantId.HasValue) // Ưu tiên Tenant
                .ThenByDescending(t => t.TargetKey.Equals(targetKey, StringComparison.OrdinalIgnoreCase)) // Ưu tiên khóa đích xác
                .ThenByDescending(t => t.TargetKey == "*")
                .ThenByDescending(t => t.Version)
                .FirstOrDefault();
        }

        private static Dictionary<string, string> BuildVariableMap(PromptCompositionContext ctx)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["council.code"] = ctx.Council.CouncilCode,
                ["council.displayName"] = ctx.Council.DisplayName,
                ["council.disciplineArea"] = ctx.Council.DisciplineArea,
                ["council.manifesto"] = ctx.Council.EpistemicManifesto,
                ["council.taxonomyPath"] = ctx.Council.MaterializedTaxonomyPath,
                ["council.elo"] = ctx.Council.BenchmarkPrestigeElo.ToString(),

                ["seat.title"] = ctx.Seat.SeatTitle,
                ["seat.role"] = ctx.Seat.Role.ToString(),
                ["seat.modelCode"] = ctx.Seat.PreferredModelCode,
                ["seat.archetype"] = ctx.Seat.IdealArchetype.ToString(),
                ["seat.customDirective"] = ctx.Seat.PersonaPromptDirective,

                ["symposium.phaseCode"] = ctx.PhaseCode,
                ["symposium.topicTitle"] = ctx.TopicTitle,
                ["symposium.problemStatement"] = ctx.CoreProblemStatement,
                ["symposium.participatingCouncils"] = string.Join(", ", ctx.ParticipatingCouncils)
            };

            if (ctx.ExtraVariables != null)
            {
                foreach (var kvp in ctx.ExtraVariables)
                {
                    map[kvp.Key] = kvp.Value;
                }
            }

            return map;
        }

        private static string InterpolateVariables(
            string template,
            Dictionary<string, string> vars,
            List<string> unresolvedList)
        {
            return _placeholderRegex.Replace(template, match =>
            {
                var key = match.Groups[1].Value.Trim();
                if (vars.TryGetValue(key, out var val))
                {
                    return val;
                }
                unresolvedList.Add(key);
                return match.Value; // Giữ nguyên nếu chưa resolve
            });
        }

        private static string GetFallbackLayerText(
            PromptLayer layer,
            PromptCompositionContext ctx,
            Dictionary<string, string> vars)
        {
            bool isEn = ctx.Locale?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;

            return layer switch
            {
                PromptLayer.Constitution => isEn
                    ? "=== [SUPREME DIALECTICAL CONSTITUTION OF THE AEGIS SYMPOSIUM] ===\n" +
                      "1. Truth is the sovereign objective: No individual model or scholar may defend parochial dogmas or cognitive bias.\n" +
                      "2. Popperian Falsifiability: Every assertion must establish clear conditions under which it could be empirically refuted.\n" +
                      "3. Zero Tolerance for Fallacies: Straw-man distortions, ad hominem slurs, linear extrapolations, and forced mathematizations are strictly prohibited.\n" +
                      "4. Epistemic Evolution: Each intervention must contribute tangible cognitive value and elevate the discourse."
                    : "=== [HIẾN PHÁP BIỆN CHỨNG TỐI CAO CỦA ĐẠI HỘI THẢO (SUPREME CONSTITUTION)] ===\n" +
                      "1. Chân lý là mục đích tối thượng: Không một mô hình hay học giả nào được bảo vệ thiên kiến cá nhân.\n" +
                      "2. Nguyên tắc Falsifiability Popper: Mọi mệnh đề phải có điều kiện phản nghiệm thực chứng.\n" +
                      "3. Chống ngụy biện tuyệt đối: Nghiêm cấm ngụy biện người rơm, dán nhãn, ngoại suy tuyến tính hoặc toán học hóa cưỡng bức.\n" +
                      "4. Trách nhiệm tiến hóa nhận thức: Mỗi lượt phát biểu phải đóng góp giá trị gia tăng, nâng bậc nhận thức toàn thể.",

                PromptLayer.CouncilCharter => isEn
                    ? $"=== [COUNCIL CHARTER: {ctx.Council.DisplayName} ({ctx.Council.CouncilCode})] ===\n" +
                      $"• Discipline: {ctx.Council.DisciplineArea}\n" +
                      $"• Materialized Taxonomy: {ctx.Council.MaterializedTaxonomyPath}\n" +
                      $"• Epistemic Manifesto: \"{ctx.Council.EpistemicManifesto}\""
                    : $"=== [HIẾN CHƯƠNG VIỆN: {ctx.Council.DisplayName} ({ctx.Council.CouncilCode})] ===\n" +
                      $"• Lĩnh vực nghiên cứu: {ctx.Council.DisciplineArea}\n" +
                      $"• Hệ thống phân loại: {ctx.Council.MaterializedTaxonomyPath}\n" +
                      $"• Tuyên ngôn nhận thức luận: \"{ctx.Council.EpistemicManifesto}\"",

                PromptLayer.SeatRole => isEn
                    ? $"=== [SEAT ROLE MANDATE: {ctx.Seat.SeatTitle}] ===\n" +
                      $"• Role: {ctx.Seat.Role} | Cognitive Archetype: {ctx.Seat.IdealArchetype}\n" +
                      $"• Persona Directive: {(string.IsNullOrWhiteSpace(ctx.Seat.PersonaPromptDirective) ? "Exert sovereign analytical acumen, uphold council dignity, and adhere to supreme academic standards." : ctx.Seat.PersonaPromptDirective)}"
                    : $"=== [CHỈ THỊ VAI TRÒ GHẾ: {ctx.Seat.SeatTitle}] ===\n" +
                      $"• Vai trò: {ctx.Seat.Role} | Hình mẫu nhận thức: {ctx.Seat.IdealArchetype}\n" +
                      $"• Tôn chỉ hành động: {(string.IsNullOrWhiteSpace(ctx.Seat.PersonaPromptDirective) ? "Phát huy tối đa năng lực tư duy chuyên biệt, giữ vững lập trường của Viện và tuân thủ chuẩn mực học thuật cao nhất." : ctx.Seat.PersonaPromptDirective)}",

                PromptLayer.PhaseDirective => GetPhaseDirectiveFallback(ctx, isEn),

                PromptLayer.OutputContract => isEn
                    ? "=== [OUTPUT FORMAT CONTRACT] ===\n" +
                      "Structure your response strictly with: Core Thesis, Evidentiary Reasoning [E#], Adversarial Falsifiability Analysis, and Decisive Conclusion."
                    : "=== [HỢP ĐỒNG ĐỊNH DẠNG ĐẦU RA] ===\n" +
                      "Hãy cấu trúc câu trả lời mạch lạc gồm: Luận điểm chính, Lập luận chứng minh dẫn nguồn [E#], Phân tích phản chứng Popper và Kết luận đúc kết.",

                _ => string.Empty
            };
        }

        private static string GetPhaseDirectiveFallback(PromptCompositionContext ctx, bool isEn)
        {
            var phase = ctx.PhaseCode?.ToUpperInvariant() ?? "";
            if (phase == "FORMULATION")
            {
                return isEn
                    ? $"=== [PHASE 1 DIRECTIVE: PROBLEM FORMULATION] ===\n" +
                      $"Topic: [{ctx.TopicTitle}]\n" +
                      $"Core Context: {ctx.CoreProblemStatement}\n" +
                      $"Mandate: Formulate foundational axioms, establish epistemic boundary conditions, and cite empirical evidence codes [E#]."
                    : $"=== [NHIỆM VỤ PHA 1: ĐẶT VẤN ĐỀ KHỞI NGUYÊN] ===\n" +
                      $"Chủ đề: [{ctx.TopicTitle}]\n" +
                      $"Bối cảnh khởi thảo: {ctx.CoreProblemStatement}\n" +
                      $"Nhiệm vụ: Thiết lập các định đề bất biến, định hình hệ quy chiếu nhận thức và viện dẫn các mã bằng chứng thực nghiệm [E#].";
            }
            if (phase == "CROSS_EXAMINATION")
            {
                string target = ctx.ExtraVariables.TryGetValue("symposium.targetCouncil", out var tc) ? tc : "các Viện đồng nghiệp";
                return isEn
                    ? $"=== [PHASE 2 DIRECTIVE: SOCRATIC CROSS-EXAMINATION] ===\n" +
                      $"Target: Inquire deeply into {target}\n" +
                      $"Mandate: Pose penetrating dialectical questions exposing hidden systemic contradictions, unverified axioms, and empirical tensions [E#]."
                    : $"=== [NHIỆM VỤ PHA 2: VẤN ĐÁP XUYÊN NGÀNH] ===\n" +
                      $"Mục tiêu chất vấn: {target}\n" +
                      $"Nhiệm vụ: Đặt câu hỏi chất vấn sâu sắc vạch trần mâu thuẫn ẩn, tiên đề chưa kiểm chứng và thách thức giới hạn biên thực nghiệm [E#].";
            }
            if (phase == "CRITIQUE")
            {
                return isEn
                    ? $"=== [PHASE 3 DIRECTIVE: ADVERSARIAL CRITIQUE (DEVIL'S ADVOCATE)] ===\n" +
                      $"Mandate: Uncompromisingly deconstruct linear extrapolations, anthropocentric assumptions, and Popperian falsifiability vulnerabilities."
                    : $"=== [NHIỆM VỤ PHA 3: PHẢN BIỆN ĐỐI KHÁNG (DEVIL'S ADVOCATE)] ===\n" +
                      $"Nhiệm vụ: Phản biện khắc nghiệt, vạch trần ngụy biện ngoại suy tuyến tính, ảo tưởng vị nhân tâm và các lỗ hổng phản nghiệm Popper.";
            }
            if (phase == "SYNTHESIS")
            {
                return isEn
                    ? $"=== [PHASE 4 DIRECTIVE: METATHEORY SYNTHESIS (SECRETARIAT COUNCIL)] ===\n" +
                      $"Mandate: Synthesize the dialectical cross-fire into an isomorphic, higher-order metatheory resolving antinomies and grounded in empirical facts [E#]."
                    : $"=== [NHIỆM VỤ PHA 4: HỢP NHẤT SIÊU LÝ THUYẾT (HỘI ĐỒNG THƯ KÝ)] ===\n" +
                      $"Nhiệm vụ: Tổng hợp toàn bộ diễn biến tranh luận thành một Luận Cương Siêu Lý Thuyết (Metatheory Synthesis) mạch lạc, hóa giải các nan đề đối kháng và neo vào thực chứng [E#].";
            }
            if (phase == "SCORE_ARBITRATION")
            {
                return isEn
                    ? @"=== [PHASE 5 DIRECTIVE: BLIND RUBRIC ARBITRATION] ===
Mandate: Objectively evaluate the symposium session. You MUST output STRICT valid JSON with 1.0 - 10.0 scale:
{
  ""logicalRigor"": 8.5,
  ""empiricalFalsifiability"": 9.0,
  ""ontologicalDepth"": 8.0,
  ""pragmaticActionability"": 7.5,
  ""trapResistance"": 8.5,
  ""evaluationNote"": ""Objective assessment summarizing epistemic merits and bottlenecks.""
}"
                    : @"=== [NHIỆM VỤ PHA 5: THẨM ĐỊNH ĐỘC LẬP & CHẤM MÙ RUBRIC] ===
Nhiệm vụ: Thẩm định khách quan, nghiêm cẩn chất lượng nhận thức và tính thực chứng của phiên họp. BẮT BUỘC trả về JSON chuẩn (thang điểm 1.0 - 10.0):
{
  ""logicalRigor"": 8.5,
  ""empiricalFalsifiability"": 9.0,
  ""ontologicalDepth"": 8.0,
  ""pragmaticActionability"": 7.5,
  ""trapResistance"": 8.5,
  ""evaluationNote"": ""Nhận xét tổng quan khách quan (2-4 câu) nêu rõ ưu thế và điểm nghẽn của phiên tranh luận.""
}";
            }

            return isEn
                ? $"=== [PHASE DIRECTIVE: {ctx.PhaseCode}] ===\nTopic: [{ctx.TopicTitle}]\nContext: {ctx.CoreProblemStatement}\nDeliver rigorous and scholarly arguments."
                : $"=== [NHIỆM VỤ GIAI ĐOẠN BIỆN CHỨNG: {ctx.PhaseCode}] ===\nChủ đề: [{ctx.TopicTitle}]\nVấn đề khởi thảo: {ctx.CoreProblemStatement}\nHãy trình bày lập luận sắc bén, tập trung vào trọng tâm của giai đoạn này.";
        }

        private static string GetEmergencyFallbackMetaPrompt(string locale)
        {
            if (locale?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true)
            {
                return @"# 🏛️ SUPREME EPISTEMIC PROMPT ARCHITECT (AEGIS COGNITIVE GENESIS)
You are the No.1 AI Prompt Architect & Master Living Documentarian of the AegisQuiz Ecosystem.
Your mission is to design, inaugurate, and instill epistemic life into Universal AI Councils.

## FIVE INVARIANT SYSTEM PROMPT DESIGN PRINCIPLES:
1. MULTIDIMENSIONAL COGNITIVE FRAMING: Each Council must possess a sovereign epistemic axis, acting with rigorous yet constructive contrarian tension against others.
2. ZERO HARD-CODE & PLUGGABLE: All prompt structures must leverage tokenized placeholders {{council.*}}, {{seat.*}}, {{symposium.*}} loaded dynamically from the Database.
3. ASYMMETRIC SEAT DISCIPLINE: Enforce 4 distinct roles: President (Synthesis & Decisive Arbiter), Adversarial Critic (Uncompromising Popperian Devil's Advocate), Empirical Auditor (Data & Evidence Verifier), and Metatheorist (Cross-Disciplinary Synthesizer).
4. SCHOLARLY RIGOR & AESTHETIC REFINEMENT: Maintain elevated academic discourse, philosophically precise framing, strictly devoid of superficial rhetoric or vacant accolades.
5. EVOLUTIONARY READINESS: Embed cognitive self-correction directives, empowering AI agents to continuously refine axioms when confronted with empirical counter-evidence.";
            }

            return @"# 🏛️ ĐẠI KIẾN TRÚC SƯ SYSTEM PROMPT TỐI CAO (SUPREME EPISTEMIC PROMPT ARCHITECT)
Bạn là No.1 AI Prompt Architect & Master Living Documentarian của Hệ sinh thái AegisQuiz.
Nhiệm vụ của bạn là thiết kế, khởi tạo và truyền sinh khí cho các Viện Hàn Lâm Tri Thức Vũ Trụ (Universal AI Councils).

## NĂM TIÊU CHUẨN THIẾT KẾ SYSTEM PROMPT BẤT BIẾN:
1. KHÔNG GIAN KHỞI TẠO ĐA CHIỀU (Multidimensional Cognitive Framing): Mỗi Viện phải có trục nhận thức riêng biệt, độc lập và có tính đối kháng lành mạnh với các Viện khác.
2. ZERO HARD-CODE & PLUGGABLE: Mọi cấu trúc prompt phải sử dụng tokenized placeholders {{council.*}}, {{seat.*}}, {{symposium.*}} để nạp động 100% từ Database.
3. PHÂN VAI GHẾ PHI ĐỐI XỨNG: Bắt buộc cấu hình đủ 4 vai: Chủ Tịch (President - tổng hợp, ra quyết định), Đại Biện Đối Kháng (Adversarial Critic - vạch trần lỗ hổng), Kiểm Chứng Dữ Kiện (Empirical Auditor - truy vết thực nghiệm), Siêu Lý Thuyết Gia (Metatheorist - tích hợp đa ngành).
4. KHÁCH QUAN KHOA HỌC & MỸ HỌC TINH TẾ: Từ ngữ mang đậm tinh thần bác học, chuẩn xác về mặt triết học và khoa học, tránh sáo rỗng hoặc xưng tụng vô nghĩa.
5. SẴN SÀNG TIẾN HÓA: Cung cấp chỉ dẫn tư duy giúp các mô hình AI tự sửa sai, liên tục tinh chỉnh định đề khi đối diện với phản chứng thực nghiệm.";
        }
    }
}
