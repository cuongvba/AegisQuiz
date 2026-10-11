using System;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// [AegisAI Prompt Architecture] Mẫu System Prompt phân tầng cho Đại Hội Thảo.
    /// Mỗi lượt phát biểu của một ghế được ghép từ nhiều tầng (Constitution → Council → Role → Phase → Output),
    /// mỗi tầng chọn bản cụ thể nhất theo thứ tự: Tenant riêng > Toàn cục, TargetKey chính xác > "*", Version cao nhất.
    /// Thêm/sửa prompt = INSERT/UPDATE một dòng DB, không sửa mã nguồn (OCP).
    /// </summary>
    public class SymposiumPromptTemplate
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Mã định danh logic, ví dụ: CONSTITUTION, ROLE_PRESIDENT, PHASE_CRITIQUE.</summary>
        public string TemplateCode { get; set; } = string.Empty;

        public PromptLayer Layer { get; set; } = PromptLayer.Constitution;

        /// <summary>
        /// Khóa áp dụng trong tầng: CouncilCode (tầng Council), tên CouncilSeatRole (tầng Role),
        /// mã pha (tầng Phase) hoặc "*" cho mọi đối tượng.
        /// </summary>
        public string TargetKey { get; set; } = "*";

        public string Locale { get; set; } = "vi-VN";
        public int Version { get; set; } = 1;

        /// <summary>Nội dung prompt, hỗ trợ placeholder dạng {{council.displayName}}.</summary>
        public string Body { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        /// <summary>Null = áp dụng toàn hệ thống; có giá trị = bản ghi đè riêng của Tenant (White-label).</summary>
        public Guid? TenantId { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>Thứ tự tầng cũng là thứ tự ghép prompt (số nhỏ đứng trước).</summary>
    public enum PromptLayer
    {
        /// <summary>Prompt của chính chuyên gia Kiến trúc sư System Prompt — dùng để soạn hiến chương cho Viện mới.</summary>
        Meta = 0,
        /// <summary>Hiến pháp nhận thức chung cho mọi ghế, mọi Viện.</summary>
        Constitution = 1,
        /// <summary>Hiến chương danh tính của một Viện.</summary>
        CouncilCharter = 2,
        /// <summary>Chỉ thị vai trò ghế (Chủ tịch, Phản biện, Kiểm chứng, Tổng hợp).</summary>
        SeatRole = 3,
        /// <summary>Nhiệm vụ của pha tranh biện hiện tại.</summary>
        PhaseDirective = 4,
        /// <summary>Hợp đồng định dạng đầu ra (JSON schema).</summary>
        OutputContract = 5
    }
}
