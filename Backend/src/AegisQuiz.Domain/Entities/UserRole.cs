using System;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// [RBAC] Mapping User ↔ Role ↔ Tenant với scope OrgUnit tùy chọn.
    ///
    /// Đặc biệt cho TeamLeader: OrgUnitId xác định nhóm cụ thể mà leader quản lý.
    /// Một user có thể có nhiều UserRole (multi-role, multi-scope).
    ///
    /// Ví dụ:
    ///   { UserId, Role="TeamLeader", TenantId, OrgUnitId="team-hanoi" }
    ///   { UserId, Role="Learner",    TenantId, OrgUnitId=null }
    /// </summary>
    public class UserRole : ITenantEntity
    {
        public Guid   Id         { get; set; } = Guid.NewGuid();

        // ── User reference (string để tương thích với Keycloak sub / PKI employeeId)
        public string UserId     { get; set; } = string.Empty;

        // ── Tenant isolation ──────────────────────────────────────────────────
        public Guid   TenantId   { get; set; }

        // ── Role (dùng AppRoles.* constants) ─────────────────────────────────
        public string Role       { get; set; } = AppRoles.Learner;

        // ── OrgUnit scope (nullable — chỉ bắt buộc với TeamLeader) ───────────
        /// <summary>
        /// Với TeamLeader: phạm vi quản lý chỉ trong OrgUnit này.
        /// Với Learner: đơn vị học viên thuộc về.
        /// null = không giới hạn scope (TenantAdmin, ContentManager, ...).
        /// </summary>
        public Guid?  OrgUnitId  { get; set; }

        // ── Metadata ──────────────────────────────────────────────────────────
        public bool      IsActive   { get; set; } = true;
        public DateTime  AssignedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UserId của người thực hiện gán role (audit trail).</summary>
        public string?   AssignedBy { get; set; }

        /// <summary>Thời điểm hết hạn role (null = vĩnh viễn).</summary>
        public DateTime? ExpiresAt  { get; set; }

        // ── Navigation ────────────────────────────────────────────────────────
        public virtual OrganizationUnit? OrgUnit { get; set; }
    }
}
