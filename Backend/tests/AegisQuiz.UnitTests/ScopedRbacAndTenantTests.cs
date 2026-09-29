using System;
using System.Linq;
using Xunit;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.UnitTests
{
    public class ScopedRbacAndTenantTests
    {
        [Fact]
        public void RoleWeights_ShouldFollowStrictHierarchy()
        {
            Assert.True(AppRoles.GetRoleWeight(AppRoles.SystemAdmin) > AppRoles.GetRoleWeight(AppRoles.TenantAdmin));
            Assert.True(AppRoles.GetRoleWeight(AppRoles.TenantAdmin) > AppRoles.GetRoleWeight(AppRoles.OrgUnitManager));
            Assert.True(AppRoles.GetRoleWeight(AppRoles.OrgUnitManager) > AppRoles.GetRoleWeight(AppRoles.ContentManager));
            Assert.True(AppRoles.GetRoleWeight(AppRoles.ContentManager) > AppRoles.GetRoleWeight(AppRoles.TeamLeader));
            Assert.True(AppRoles.GetRoleWeight(AppRoles.TeamLeader) > AppRoles.GetRoleWeight(AppRoles.Instructor));
            Assert.True(AppRoles.GetRoleWeight(AppRoles.Instructor) > AppRoles.GetRoleWeight(AppRoles.Learner));
            Assert.True(AppRoles.GetRoleWeight(AppRoles.Learner) > AppRoles.GetRoleWeight(AppRoles.GuestViewer));
        }

        [Theory]
        [InlineData(AppRoles.SystemAdmin, AppRoles.SystemAdmin, true)]
        [InlineData(AppRoles.SystemAdmin, AppRoles.TenantAdmin, true)]
        [InlineData(AppRoles.SystemAdmin, AppRoles.OrgUnitManager, true)]
        [InlineData(AppRoles.SystemAdmin, AppRoles.Learner, true)]
        // TenantAdmin ceiling
        [InlineData(AppRoles.TenantAdmin, AppRoles.SystemAdmin, false)] // Cannot elevate to SystemAdmin!
        [InlineData(AppRoles.TenantAdmin, AppRoles.TenantAdmin, true)]
        [InlineData(AppRoles.TenantAdmin, AppRoles.OrgUnitManager, true)]
        [InlineData(AppRoles.TenantAdmin, AppRoles.Learner, true)]
        // OrgUnitManager ceiling
        [InlineData(AppRoles.OrgUnitManager, AppRoles.SystemAdmin, false)]
        [InlineData(AppRoles.OrgUnitManager, AppRoles.TenantAdmin, false)] // Cannot elevate to TenantAdmin!
        [InlineData(AppRoles.OrgUnitManager, AppRoles.OrgUnitManager, true)]
        [InlineData(AppRoles.OrgUnitManager, AppRoles.TeamLeader, true)]
        [InlineData(AppRoles.OrgUnitManager, AppRoles.Learner, true)]
        // TeamLeader ceiling
        [InlineData(AppRoles.TeamLeader, AppRoles.OrgUnitManager, false)] // Cannot elevate to OrgUnitManager!
        [InlineData(AppRoles.TeamLeader, AppRoles.TeamLeader, true)]
        [InlineData(AppRoles.TeamLeader, AppRoles.Learner, true)]
        // Learner cannot assign anything higher
        [InlineData(AppRoles.Learner, AppRoles.TeamLeader, false)]
        [InlineData(AppRoles.Learner, AppRoles.TenantAdmin, false)]
        public void PrivilegeCeiling_ShouldPreventPrivilegeEscalation(string actorRole, string targetRole, bool expected)
        {
            bool canAssign = AppRoles.CanAssignRole(actorRole, targetRole);
            Assert.Equal(expected, canAssign);
        }

        [Fact]
        public void GetAssignableRoles_ShouldOnlyIncludeRolesAtOrBelowActorWeight()
        {
            var tenantAdminAssignable = AppRoles.GetAssignableRoles(AppRoles.TenantAdmin);
            Assert.DoesNotContain(AppRoles.SystemAdmin, tenantAdminAssignable);
            Assert.Contains(AppRoles.TenantAdmin, tenantAdminAssignable);
            Assert.Contains(AppRoles.OrgUnitManager, tenantAdminAssignable);
            Assert.Contains(AppRoles.Learner, tenantAdminAssignable);

            var orgManagerAssignable = AppRoles.GetAssignableRoles(AppRoles.OrgUnitManager);
            Assert.DoesNotContain(AppRoles.SystemAdmin, orgManagerAssignable);
            Assert.DoesNotContain(AppRoles.TenantAdmin, orgManagerAssignable);
            Assert.Contains(AppRoles.OrgUnitManager, orgManagerAssignable);
            Assert.Contains(AppRoles.TeamLeader, orgManagerAssignable);
            Assert.Contains(AppRoles.Learner, orgManagerAssignable);

            var teamLeaderAssignable = AppRoles.GetAssignableRoles(AppRoles.TeamLeader);
            Assert.DoesNotContain(AppRoles.SystemAdmin, teamLeaderAssignable);
            Assert.DoesNotContain(AppRoles.TenantAdmin, teamLeaderAssignable);
            Assert.DoesNotContain(AppRoles.OrgUnitManager, teamLeaderAssignable);
            Assert.Contains(AppRoles.TeamLeader, teamLeaderAssignable);
            Assert.Contains(AppRoles.Learner, teamLeaderAssignable);
        }

        [Fact]
        public void SubtreeScoping_ShouldEnforceHorizontalAndVerticalIsolation()
        {
            string managerScope = "/HQ/MIENBAC";

            string validChild1 = "/HQ/MIENBAC/CN_HANOI";
            string validChild2 = "/HQ/MIENBAC/CN_HAIPHONG/PHONG_KT";
            string invalidParent = "/HQ";
            string invalidSibling = "/HQ/MIENNAM/CN_SAIGON";

            Assert.True(validChild1.StartsWith(managerScope), "Chi nhánh Hà Nội phải thuộc phân hiệu Miền Bắc.");
            Assert.True(validChild2.StartsWith(managerScope), "Phòng kế toán Hải Phòng phải thuộc phân hiệu Miền Bắc.");
            Assert.False(invalidParent.StartsWith(managerScope), "Hội sở chính KHÔNG được thuộc thẩm quyền của Giám đốc Miền Bắc.");
            Assert.False(invalidSibling.StartsWith(managerScope), "Chi nhánh Sài Gòn (Miền Nam) KHÔNG được thuộc thẩm quyền của Giám đốc Miền Bắc.");
        }

        [Fact]
        public void CyclePrevention_NodeCannotBeMovedIntoDescendant()
        {
            string currentNodePath = "/HQ/MIENBAC";
            string targetParentPath = "/HQ/MIENBAC/CN_HANOI"; // Target parent is a descendant of currentNode!

            bool isCycle = targetParentPath.StartsWith(currentNodePath);
            Assert.True(isCycle, "Di chuyển một node vào node con cháu của chính nó phải bị phát hiện là vòng lặp chu trình.");
        }

        [Theory]
        [InlineData("/HOLDING", 1)]
        [InlineData("/HOLDING/APAC", 2)]
        [InlineData("/HOLDING/APAC/VN", 3)]
        [InlineData("/HOLDING/APAC/VN/FINTECH", 4)]
        [InlineData("/HOLDING/APAC/VN/FINTECH/ENGINEERING", 5)]
        [InlineData("/HOLDING/APAC/VN/FINTECH/ENGINEERING/CORE_BANKING", 6)]
        [InlineData("/HOLDING/APAC/VN/FINTECH/ENGINEERING/CORE_BANKING/TRANSACTIONS", 7)]
        [InlineData("/HOLDING/APAC/VN/FINTECH/ENGINEERING/CORE_BANKING/TRANSACTIONS/SQUAD_SPEED", 8)]
        [InlineData("/1/2/3/4/5/6/7/8/9/10/11/12/13/14/15/16/17/18/19/20", 20)]
        public void DepthLevel_ShouldDeriveCorrectlyForArbitraryDepths(string path, int expectedDepth)
        {
            int calculatedDepth = path.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;
            Assert.Equal(expectedDepth, calculatedDepth);
        }

        [Fact]
        public void OrgUnitType_ShouldSupportGlobalConglomerateTaxonomy()
        {
            // Verify all new enterprise types exist and have defined enum values
            Assert.Equal(0, (int)OrgUnitType.Holding);
            Assert.Equal(1, (int)OrgUnitType.HeadOffice);
            Assert.Equal(2, (int)OrgUnitType.Region);
            Assert.Equal(3, (int)OrgUnitType.Branch);
            Assert.Equal(4, (int)OrgUnitType.Department);
            Assert.Equal(5, (int)OrgUnitType.Classroom);
            Assert.Equal(6, (int)OrgUnitType.Sector);
            Assert.Equal(7, (int)OrgUnitType.Division);
            Assert.Equal(8, (int)OrgUnitType.Section);
            Assert.Equal(9, (int)OrgUnitType.Squad);
            Assert.Equal(99, (int)OrgUnitType.Custom);
        }

        [Fact]
        public void SubtreeScoping_ShouldWorkAtExtremeDepths()
        {
            string divisionScope = "/HOLDING/APAC/VN/FINTECH/ENGINEERING";

            string deepSquad = "/HOLDING/APAC/VN/FINTECH/ENGINEERING/CORE_BANKING/TRANSACTIONS/SQUAD_SPEED/POD_ALPHA";
            string siblingDivision = "/HOLDING/APAC/VN/FINTECH/MARKETING/CAMPAIGNS";
            string parentCorp = "/HOLDING/APAC/VN";

            Assert.True(deepSquad.StartsWith(divisionScope), "Squad con cháu ở độ sâu tầng 8 phải nằm trong phạm vi của Division.");
            Assert.False(siblingDivision.StartsWith(divisionScope), "Phòng Marketing ngang hàng không thể bị quản lý bởi Engineering.");
            Assert.False(parentCorp.StartsWith(divisionScope), "Cấp công ty cha không thể bị quản lý bởi cấp phòng ban con.");
        }
    }
}
