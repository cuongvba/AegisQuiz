using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Application.Interfaces
{
    public interface IUniversalAiRouter
    {
        /// <summary>
        /// Thực thi yêu cầu AI qua bộ định tuyến thông minh (Smart Router):
        /// - Kiểm tra Semantic Cache
        /// - Chọn Provider tối ưu theo nhiệm vụ và FinOps (DeepSeek cho toán/ngân hàng, Gemini cho bóc tách, Claude cho văn tự luận)
        /// - Tự động Failover nếu gặp 429 hoặc lỗi kết nối
        /// </summary>
        Task<AiExecutionResponse> ExecuteAsync(AiExecutionRequest request);

        /// <summary>
        /// Sinh bản đồ tư duy sư phạm 5 bước (Cognitive Walkthrough) cho câu hỏi:
        /// 1. Bóc tách dữ kiện -> 2. Tọa độ pháp lý -> 3. Phân tích bẫy -> 4. Mẹo nhớ nhanh -> 5. Tình huống mở rộng
        /// </summary>
        Task<PedagogicalWalkthroughDto> GenerateWalkthroughAsync(
            string questionContent,
            List<string> options,
            string correctAnswer,
            string domainCode,
            AgentPersonaType persona = AgentPersonaType.SupremeArbiter,
            string? tenantId = null);

        /// <summary>
        /// Lấy toàn bộ cấu hình AI, danh sách Providers, Key Pool và dữ liệu FinOps Telemetry
        /// </summary>
        AiSystemConfigDto GetSystemConfig();

        /// <summary>
        /// Cập nhật cấu hình hệ thống AI (API keys, Task routing, Cache)
        /// </summary>
        void UpdateSystemConfig(AiSystemConfigDto config);

        /// <summary>
        /// Đăng ký cấu hình BYOK riêng cho Tenant / Chi nhánh ngân hàng
        /// </summary>
        void RegisterTenantByok(TenantByokConfigDto byokConfig);

        /// <summary>
        /// Thử nghiệm kết nối và đo độ trễ (latency test) của từng Provider
        /// </summary>
        Task<Dictionary<string, (bool Success, long LatencyMs, string Message)>> TestProvidersAsync();
    }
}
