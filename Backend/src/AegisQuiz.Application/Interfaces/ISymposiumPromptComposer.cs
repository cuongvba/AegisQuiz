using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Prompt Architecture] Bộ ghép System Prompt phân tầng cho từng ghế trong Đại Hội Thảo.
    /// Mọi nội dung prompt đến từ DB (bảng SymposiumPromptTemplates); lớp này chỉ chọn bản phù hợp và điền placeholder.
    /// </summary>
    public interface ISymposiumPromptComposer
    {
        Task<ComposedSystemPrompt> ComposeSeatPromptAsync(PromptCompositionContext context, CancellationToken ct = default);

        /// <summary>Prompt của chuyên gia Kiến trúc sư System Prompt (tầng Meta), dùng khi soạn hiến chương cho Viện mới.</summary>
        Task<ComposedSystemPrompt> ComposeArchitectPromptAsync(string locale, Guid? tenantId, CancellationToken ct = default);

        /// <summary>Xóa cache sau khi quản trị viên sửa mẫu prompt (Hot-Reload).</summary>
        void InvalidateCache();
    }

    public class PromptCompositionContext
    {
        public UniversalAiCouncil Council { get; set; } = null!;
        public UniversalCouncilSeat Seat { get; set; } = null!;

        /// <summary>Mã pha, ví dụ: FORMULATION, CROSS_EXAMINATION, CRITIQUE, SYNTHESIS, EVALUATION, DECLARATION.</summary>
        public string PhaseCode { get; set; } = string.Empty;

        public string TopicTitle { get; set; } = string.Empty;
        public string CoreProblemStatement { get; set; } = string.Empty;

        /// <summary>Danh sách tên các Viện cùng tham dự, để ghế biết mình đối thoại với ai.</summary>
        public List<string> ParticipatingCouncils { get; set; } = new();

        public string Locale { get; set; } = "vi-VN";
        public Guid? TenantId { get; set; }

        /// <summary>Biến bổ sung tùy ý (mở rộng không cần sửa interface).</summary>
        public Dictionary<string, string> ExtraVariables { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class ComposedSystemPrompt
    {
        public string Text { get; set; } = string.Empty;

        /// <summary>Danh sách mẫu đã áp dụng dạng "TemplateCode@vVersion[tenant]", phục vụ truy vết và kiểm toán.</summary>
        public List<string> AppliedTemplates { get; set; } = new();

        /// <summary>Các tầng không tìm thấy mẫu nào phù hợp.</summary>
        public List<string> MissingLayers { get; set; } = new();

        /// <summary>Placeholder không có giá trị — cảnh báo cấu hình sai, không bị nuốt im lặng.</summary>
        public List<string> UnresolvedPlaceholders { get; set; } = new();

        public int EstimatedTokens { get; set; }
    }
}
