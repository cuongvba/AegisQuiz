using System.Collections.Generic;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Symposium Architecture] Bộ cung cấp danh mục Viện Hàn Lâm tiền cấu hình (Preconfigured Councils Provider).
    /// Tách rời dữ liệu khởi tạo mẫu khỏi lõi điều phối để bảo đảm OCP và nguyên lý lớp nhỏ gọn.
    /// </summary>
    public interface IPreconfiguredCouncilProvider
    {
        /// <summary>
        /// Lấy danh sách các Viện Hàn Lâm mặc định của hệ thống.
        /// </summary>
        List<UniversalAiCouncil> GetPreconfiguredCouncils();
    }
}
