using System;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [Universal Dynamic Domain Context Service]
    /// Nạp và xây dựng ngữ cảnh đa ngành động (DynamicDomainContext) theo từng Tenant từ Database.
    /// Bao gồm: Danh mục ngành (DynamicDomains), Cấu hình Tenant (TenantDomainConfigs), 
    /// Bộ tọa độ tri thức đặc thù (TenantCoordinatePresets), và Danh mục chủ đề (BankTopics).
    /// </summary>
    public interface IDynamicDomainContextService
    {
        Task<DynamicDomainContext> BuildContextAsync(Guid? tenantId = null, CancellationToken ct = default);
    }
}
