using System.Text.Json;
using AegisQuiz.Domain.Entities;
using Xunit;

namespace AegisQuiz.UnitTests
{
    public class EnterpriseEntitlementMatrixTests
    {
        [Fact]
        public void Starter_Plan_Should_Only_Have_Base_Features_And_Zero_Arena_Seats()
        {
            var features = PlanFeatureCatalog.GetFeaturesForPlan(TenantPlan.Starter);
            var quotas = PlanFeatureCatalog.GetQuotasForPlan(TenantPlan.Starter);

            Assert.Contains(FeatureKeys.TranspilerMoodle, features);
            Assert.Contains(FeatureKeys.AdvancedMathWmf, features);
            Assert.DoesNotContain(FeatureKeys.TranspilerQti, features);
            Assert.DoesNotContain(FeatureKeys.AiSymposium, features);
            Assert.DoesNotContain(FeatureKeys.IrtCat3Pl, features);

            Assert.Equal(0, quotas.MaxConcurrentArenaSeats);
            Assert.Equal(10, quotas.MaxUsers);
        }

        [Fact]
        public void Professional_Plan_Should_Unlock_Qti_Gift_And_Arena()
        {
            var features = PlanFeatureCatalog.GetFeaturesForPlan(TenantPlan.Professional);
            var quotas = PlanFeatureCatalog.GetQuotasForPlan(TenantPlan.Professional);

            Assert.Contains(FeatureKeys.TranspilerMoodle, features);
            Assert.Contains(FeatureKeys.TranspilerGift, features);
            Assert.Contains(FeatureKeys.TranspilerQti, features);
            Assert.Contains(FeatureKeys.ArenaGameshow, features);
            Assert.Contains(FeatureKeys.HierarchyTree, features);
            Assert.Contains(FeatureKeys.SplitScreenStudio, features);
            Assert.Contains(FeatureKeys.AiAutoRubricSolve, features);

            // Vẫn chưa có AI Symposium và IRT CAT 3-PL
            Assert.DoesNotContain(FeatureKeys.AiSymposium, features);
            Assert.DoesNotContain(FeatureKeys.IrtCat3Pl, features);
            Assert.DoesNotContain(FeatureKeys.WhiteLabelTheming, features);

            Assert.Equal(50, quotas.MaxConcurrentArenaSeats);
            Assert.Equal(100, quotas.MaxUsers);
        }

        [Fact]
        public void Enterprise_Plan_Should_Unlock_AiSymposium_IrtCat3Pl_And_WhiteLabel()
        {
            var features = PlanFeatureCatalog.GetFeaturesForPlan(TenantPlan.Enterprise);
            var quotas = PlanFeatureCatalog.GetQuotasForPlan(TenantPlan.Enterprise);

            Assert.Contains(FeatureKeys.TranspilerMoodle, features);
            Assert.Contains(FeatureKeys.TranspilerQti, features);
            Assert.Contains(FeatureKeys.TranspilerCanvas, features);
            Assert.Contains(FeatureKeys.SplitScreenStudio, features);
            Assert.Contains(FeatureKeys.AiAutoRubricSolve, features);
            Assert.Contains(FeatureKeys.AiSymposium, features);
            Assert.Contains(FeatureKeys.IrtCat3Pl, features);
            Assert.Contains(FeatureKeys.WhiteLabel, features);
            Assert.Contains(FeatureKeys.WhiteLabelTheming, features);
            Assert.Contains(FeatureKeys.AuditLog, features);

            Assert.Equal(1000, quotas.MaxConcurrentArenaSeats);
            Assert.Equal(1000, quotas.MaxUsers);
        }

        [Fact]
        public void Custom_Feature_Overrides_Should_Correctly_Add_Or_Remove_Features()
        {
            // Cho phép gói Starter mua thêm Add-on AI Symposium
            var overrides = JsonSerializer.Serialize(new Dictionary<string, bool>
            {
                [FeatureKeys.AiSymposium] = true,
                [FeatureKeys.TranspilerMoodle] = false // Cấm Moodle
            });

            var features = PlanFeatureCatalog.GetFeaturesForPlan(TenantPlan.Starter, overrides);

            Assert.Contains(FeatureKeys.AiSymposium, features);
            Assert.DoesNotContain(FeatureKeys.TranspilerMoodle, features);
        }
    }
}
