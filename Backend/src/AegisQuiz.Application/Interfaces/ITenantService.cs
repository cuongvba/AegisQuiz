using System;
using System.Threading.Tasks;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Interfaces
{
    // ── Tenant Context — Inject vào mọi service/controller ───────────────────
    // Cung cấp TenantId hiện tại cho mọi query trong hệ thống Multi-Tenant.
    public interface ITenantContext
    {
        Guid   CurrentTenantId { get; }
        string TenantCode      { get; }
        bool   IsSystemAdmin   { get; }   // Cho phép cross-tenant query
        string CurrentRole     { get; }   // [RBAC] AppRoles.* constant
        void   SetTenant(Guid tenantId, string code, bool isSystemAdmin = false);
        void   SetRole(string role);      // [RBAC] Gọi bởi TenantMiddleware
    }

    // ── Tenant Service — Admin operations ─────────────────────────────────────
    public interface ITenantService
    {
        Task<Tenant?> GetByIdAsync(Guid tenantId);
        Task<Tenant?> GetByCodeAsync(string code);
        Task<Tenant>  CreateAsync(string code, string name, TenantPlan plan);
        Task<bool>    IsActiveAsync(Guid tenantId);
    }
}
