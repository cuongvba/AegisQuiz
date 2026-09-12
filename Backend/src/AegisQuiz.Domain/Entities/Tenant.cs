using System;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// [Phase B — Multi-Tenancy] Tenant entity — Tổ chức/Đơn vị sử dụng hệ thống.
    /// Mỗi ngân hàng, trường học là 1 Tenant riêng biệt.
    /// </summary>
    public class Tenant
    {
        public Guid   Id          { get; set; } = Guid.NewGuid();
        public string Code        { get; set; } = string.Empty; // "agribank", "vietcombank"
        public string Name        { get; set; } = string.Empty;
        public string? LogoUrl    { get; set; }
        public string? PrimaryColor { get; set; } = "#1a73e8";
        public string? Domain     { get; set; }  // "agribank.aegisquiz.com"
        public bool   IsActive    { get; set; } = true;
        public TenantPlan Plan    { get; set; } = TenantPlan.Professional;
        public TenantScaleType ScaleType { get; set; } = TenantScaleType.Enterprise;
        public int    MaxUsers    { get; set; } = 1000;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }

        // Keycloak realm cho tenant này
        public string? KeycloakRealmId { get; set; }
        
        // Cấu hình đặc thù (JSON)
        public string? ConfigJson { get; set; }

        // Cấu hình tính năng tiệm tiến (Feature Flags JSON: enableIrt, enableOrgUnits, enableProctoring, etc.)
        public string? FeatureFlagsJson { get; set; }

        // Tùy biến domain riêng (Custom CNAME / Domain)
        public string? CustomDomain { get; set; }
    }

    public enum TenantPlan
    {
        Starter      = 1,
        Professional = 2,
        Enterprise   = 3,
        Global       = 4
    }

    /// <summary>
    /// Quy mô tổ chức của Tenant phục vụ triết lý Progressive Complexity Disclosure
    /// </summary>
    public enum TenantScaleType
    {
        Individual   = 1, // Cá nhân / Gia sư (Express Mode - 1-Click)
        Team         = 2, // Nhóm làm việc / Squad / Tổ bộ môn
        Classroom    = 3, // Lớp học / Khóa đào tạo ngắn hạn
        Organization = 4, // Trường học / Doanh nghiệp SME (2-tier)
        Enterprise   = 5, // Tập đoàn đa chi nhánh / Ngân hàng toàn quốc (5-tier)
        Global       = 6  // Tập đoàn toàn cầu / Liên minh đa quốc gia
    }

    /// <summary>
    /// Interface cơ sở cho tất cả entity cần tenant isolation.
    /// </summary>
    public interface ITenantEntity
    {
        Guid TenantId { get; set; }
    }
}
