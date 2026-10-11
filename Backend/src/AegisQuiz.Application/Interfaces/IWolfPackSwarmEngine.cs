using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Dynamic Adaptive Wolf Pack Swarm Engine]
    /// Nhạc trưởng điều phối bầy sói nhận thức đa mô hình, phân giải đồng thuận và quản trị BYOK Sovereign.
    /// </summary>
    public interface IWolfPackSwarmEngine
    {
        /// <summary>
        /// Kích hoạt bầy sói đa mô hình phản biện song song và tổng hợp đồng thuận
        /// </summary>
        Task<WolfPackSwarmAuditResult> AuditQuestionWithSwarmAsync(WolfPackAuditRequest request, CancellationToken ct = default);

        /// <summary>
        /// Thử nghiệm trực tiếp độ trễ và tính hợp lệ của một khóa/endpoint API
        /// </summary>
        Task<TestAiKeyResult> PingModelKeyAsync(TestAiKeyRequest request, CancellationToken ct = default);

        /// <summary>
        /// Lấy danh sách các mô hình nhận thức đang hoạt động trong hệ thống
        /// </summary>
        Task<List<AiModelRegistryDto>> GetActiveModelsAsync(Guid? tenantId = null, CancellationToken ct = default);

        /// <summary>
        /// Thêm hoặc cập nhật một mô hình AI động 100% qua Database
        /// </summary>
        Task<AiModelRegistryDto> UpsertModelAsync(UpsertAiModelRequest request, Guid tenantId, CancellationToken ct = default);

        /// <summary>
        /// Xóa mô hình tùy biến
        /// </summary>
        Task<bool> DeleteModelAsync(Guid id, Guid tenantId, CancellationToken ct = default);

        /// <summary>
        /// Lấy danh sách khóa cá nhân BYOK của người dùng
        /// </summary>
        Task<List<UserAiKeyDto>> GetUserKeysAsync(Guid userId, Guid tenantId, CancellationToken ct = default);

        /// <summary>
        /// Lưu hoặc cập nhật khóa cá nhân BYOK
        /// </summary>
        Task<UserAiKeyDto> SaveUserKeyAsync(SaveUserAiKeyRequest request, Guid userId, Guid tenantId, CancellationToken ct = default);

        /// <summary>
        /// Xóa khóa cá nhân BYOK
        /// </summary>
        Task<bool> DeleteUserKeyAsync(Guid keyId, Guid userId, Guid tenantId, CancellationToken ct = default);

        /// <summary>
        /// Lấy Bảng Vàng vinh danh các cá nhân đóng góp khóa API vào cộng đồng
        /// </summary>
        Task<List<CommunityContributorHonorDto>> GetCommunityHallOfFameAsync(int limit = 10, CancellationToken ct = default);

        /// <summary>
        /// Khám phá danh mục Model Presets của Gateway (OpenRouter, GitHub Models) kèm ma trận thế mạnh & vai trò Counter
        /// </summary>
        Task<List<GatewayModelPresetDto>> GetGatewayPresetsAsync(string gateway, CancellationToken ct = default);

        /// <summary>
        /// Nạp nhanh hàng loạt model từ Gateway vào Database và tự động tối ưu hóa gán vào các Viện/Ghế phản biện (Counter)
        /// </summary>
        Task<ImportGatewayModelsResult> ImportGatewayModelsAsync(ImportGatewayModelsRequest request, Guid tenantId, CancellationToken ct = default);
    }
}
