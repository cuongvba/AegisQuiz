using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// [Enterprise Entitlements & Monetization] Hằng số mã tính năng thương mại (Feature Keys).
    /// Đây là SSOT cho toàn bộ hệ thống (Backend policy, API gates và Frontend FeatureGate).
    /// </summary>
    public static class FeatureKeys
    {
        // ── Nhóm Xuất Bản Đề Thi (Universal Transpiler) ───────────────────────
        public const string TranspilerMoodle = "transpiler:moodle";
        public const string TranspilerGift   = "transpiler:gift";
        public const string TranspilerQti    = "transpiler:qti";
        public const string TranspilerCanvas = "transpiler:canvas";

        // ── Nhóm Trí Tuệ Nhân Tạo & Khảo Thí Nâng Cao ────────────────────────
        public const string AiSymposium      = "ai:symposium";
        public const string IrtCat3Pl        = "irt:cat3pl";
        public const string AdvancedMathWmf  = "math:vector_wmf";

        // ── Nhóm Đấu Trường Tương Tác & Gamification ─────────────────────────
        public const string ArenaGameshow    = "arena:gameshow";

        // ── Nhóm Quản Trị Tổ Chức & Doanh Nghiệp (Enterprise Governance) ─────
        public const string HierarchyTree    = "org:hierarchy_tree";
        public const string WhiteLabel       = "branding:white_label";
        public const string AuditLog         = "security:audit_log";
        public const string DynamicDomains   = "tenant:dynamic_domains";
    }

    /// <summary>
    /// Bản đồ đóng gói tính năng theo Gói Dịch Vụ (Tenant Subscription Plan).
    /// </summary>
    public static class PlanFeatureCatalog
    {
        private static readonly HashSet<string> StarterFeatures = new()
        {
            FeatureKeys.TranspilerMoodle,
            FeatureKeys.AdvancedMathWmf
        };

        private static readonly HashSet<string> ProfessionalFeatures = new()
        {
            FeatureKeys.TranspilerMoodle,
            FeatureKeys.TranspilerGift,
            FeatureKeys.TranspilerQti,
            FeatureKeys.AdvancedMathWmf,
            FeatureKeys.ArenaGameshow,
            FeatureKeys.HierarchyTree,
            FeatureKeys.DynamicDomains
        };

        private static readonly HashSet<string> EnterpriseFeatures = new()
        {
            FeatureKeys.TranspilerMoodle,
            FeatureKeys.TranspilerGift,
            FeatureKeys.TranspilerQti,
            FeatureKeys.TranspilerCanvas,
            FeatureKeys.AdvancedMathWmf,
            FeatureKeys.ArenaGameshow,
            FeatureKeys.HierarchyTree,
            FeatureKeys.DynamicDomains,
            FeatureKeys.AiSymposium,
            FeatureKeys.IrtCat3Pl,
            FeatureKeys.WhiteLabel,
            FeatureKeys.AuditLog
        };

        private static readonly HashSet<string> GlobalFeatures = new(EnterpriseFeatures);

        public static HashSet<string> GetFeaturesForPlan(TenantPlan plan, string? customOverridesJson = null)
        {
            var baseFeatures = plan switch
            {
                TenantPlan.Starter      => new HashSet<string>(StarterFeatures),
                TenantPlan.Professional => new HashSet<string>(ProfessionalFeatures),
                TenantPlan.Enterprise   => new HashSet<string>(EnterpriseFeatures),
                TenantPlan.Global       => new HashSet<string>(GlobalFeatures),
                _                       => new HashSet<string>(StarterFeatures)
            };

            // Nạp cấu hình ghi đè linh hoạt (Feature Flags JSONB) từ Tenant
            if (!string.IsNullOrWhiteSpace(customOverridesJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(customOverridesJson);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.True)
                            {
                                baseFeatures.Add(prop.Name);
                            }
                            else if (prop.Value.ValueKind == JsonValueKind.False)
                            {
                                baseFeatures.Remove(prop.Name);
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback an toàn nếu json không hợp lệ
                }
            }

            return baseFeatures;
        }

        public static TenantQuotaLimits GetQuotasForPlan(TenantPlan plan) => plan switch
        {
            TenantPlan.Starter => new TenantQuotaLimits
            {
                MaxUsers = 10,
                MaxQuestionsPerMonth = 200,
                MaxConcurrentArenaSeats = 0,
                AiTokensMonthly = 0,
                StorageLimitMb = 500
            },
            TenantPlan.Professional => new TenantQuotaLimits
            {
                MaxUsers = 100,
                MaxQuestionsPerMonth = 5000,
                MaxConcurrentArenaSeats = 50,
                AiTokensMonthly = 200000,
                StorageLimitMb = 5000
            },
            TenantPlan.Enterprise => new TenantQuotaLimits
            {
                MaxUsers = 1000,
                MaxQuestionsPerMonth = 50000,
                MaxConcurrentArenaSeats = 1000,
                AiTokensMonthly = 2000000,
                StorageLimitMb = 50000
            },
            TenantPlan.Global => new TenantQuotaLimits
            {
                MaxUsers = -1, // Unlimited
                MaxQuestionsPerMonth = -1,
                MaxConcurrentArenaSeats = 10000,
                AiTokensMonthly = -1,
                StorageLimitMb = -1
            },
            _ => new TenantQuotaLimits()
        };
    }

    public class TenantQuotaLimits
    {
        public int MaxUsers { get; set; } = 10;
        public int MaxQuestionsPerMonth { get; set; } = 200;
        public int MaxConcurrentArenaSeats { get; set; } = 0;
        public long AiTokensMonthly { get; set; } = 0;
        public int StorageLimitMb { get; set; } = 500;
    }
}
