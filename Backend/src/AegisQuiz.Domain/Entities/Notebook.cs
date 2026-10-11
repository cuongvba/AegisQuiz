using System;
using System.Collections.Generic;

namespace AegisQuiz.Domain.Entities
{
    // [AegisNotebook Sovereign] Giáo trình nhận thức & Ngân hàng đề thích ứng
    public class Notebook : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; } = Guid.Empty; // [Multi-Tenancy Poly-Partitioning]
        public Guid? OrgUnitId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SourcePdfUrl { get; set; } = string.Empty; // Link file PDF gốc
        public string StoragePath { get; set; } = string.Empty; // Đường dẫn lưu trữ nội bộ
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int TotalPages { get; set; }

        public string DomainCode { get; set; } = "GENERAL";
        public Guid? BankTopicId { get; set; } // Liên kết Cây tri thức O(1)

        public NotebookStatus Status { get; set; } = NotebookStatus.Processing;
        public string? ErrorMessage { get; set; }

        // Thống kê phân tích nhận thức (Bloom distribution, IRT theta range, Key terms)
        public string CognitiveStatsJson { get; set; } = "{}";
        // Báo cáo cảm biến xung đột giữa các nguồn tài liệu
        public string ConflictReportJson { get; set; } = "[]";

        // [MỚI] NẠP VAI TRÒ ĐỐI THOẠI (PERSONA ROLE INJECTION)
        public string PersonaName { get; set; } = "Cố Vấn Tri Thức Trưởng";
        public PersonaRoleType PersonaRole { get; set; } = PersonaRoleType.SocraticMentor;
        public string CustomPersonaPrompt { get; set; } = string.Empty;

        public Guid CreatedByAdminId { get; set; } // Người tạo
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // QUAN HỆ 1-N: 1 Notebook quản lý nhiều Nguồn tài liệu (PDF, Word, Excel, Web URL)
        public List<NotebookSource> Sources { get; set; } = new();

        // Cây kiến thức: 1 Notebook có nhiều Chương nhận thức hợp nhất
        public List<NotebookChapter> Chapters { get; set; } = new();

        // Lịch sử Đối thoại Nhận thức Nhập vai (Socratic Chat History)
        public List<NotebookChatMessage> ChatMessages { get; set; } = new();
    }

    // [Multi-Source Ingestion] Thực thể Nguồn Tài Liệu trong Hồ Sơ Tri Thức
    public class NotebookSource
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid NotebookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string SourceUrl { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int TotalPages { get; set; }

        public SourceType Type { get; set; } = SourceType.Pdf;
        public AuthorityLevel Authority { get; set; } = AuthorityLevel.InternalSop; // Cấp thẩm quyền

        public SourceStatus Status { get; set; } = SourceStatus.Ready;
        public string ExtractedText { get; set; } = string.Empty;
        public string MetadataJson { get; set; } = "{}";
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Notebook Notebook { get; set; } = null!;
    }

    public enum AuthorityLevel
    {
        SupremeLegal = 4,   // Luật, Hiến pháp, Nghị định, Chuẩn ISO quốc tế
        Regulatory = 3,     // Thông tư NHNN, Bộ Y Tế, Quy chuẩn Bộ GTVT
        InternalSop = 2,    // Sổ tay quy trình nội bộ, Quy định công ty
        ReferenceCase = 1   // Báo cáo mẫu, Tài liệu tham khảo, Đề luyện thi
    }

    public enum SourceType
    {
        Pdf,
        Docx,
        Excel,
        WebUrl,
        PlainText
    }

    public enum SourceStatus
    {
        Uploaded,
        Processing,
        Ready,
        Failed
    }

    public class NotebookChapter
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid NotebookId { get; set; }

        public int OrderIndex { get; set; } // Thứ tự chương (1, 2, 3...)
        public string Title { get; set; } = string.Empty;
        public int StartPage { get; set; } = 1;
        public int EndPage { get; set; } = 1;

        public string Summary { get; set; } = string.Empty; // Tóm tắt do AI viết
        public string RawContent { get; set; } = string.Empty; // Nội dung gốc trích từ các nguồn

        // Danh sách các Nguồn tài liệu đóng góp vào chương này
        public List<Guid> SourceIds { get; set; } = new();

        // Các ý chính (Key Points) do AI rút trích, lưu dạng JSON
        public string KeyPointsJson { get; set; } = "[]";

        // Tọa độ trích dẫn đa nguồn: [{ "sourceId": "...", "sourceName": "...", "page": 1, "box": [...] }]
        public string PinpointCoordinatesJson { get; set; } = "[]";

        // Thống kê số câu hỏi đã sinh
        public int QuestionCount { get; set; } = 0;

        // Liên kết: 1 Chương sinh ra nhiều Câu hỏi
        public List<Guid> GeneratedQuestionIds { get; set; } = new();

        // Navigation property
        public Notebook Notebook { get; set; } = null!;
    }

    public enum NotebookStatus
    {
        Processing,  // Đang phân tích
        Structuring, // Đang cấu trúc hóa
        Generating,  // Đang sinh câu hỏi
        Ready,       // Hoàn tất
        Failed       // Lỗi
    }

    public enum PersonaRoleType
    {
        SocraticMentor = 1,       // Gia sư Socratic: Luôn hỏi vặn gợi mở, rèn luyện tư duy phản biện
        StrictAuditor = 2,        // Thanh tra Pháp chế / Kiểm toán Trưởng: Soi xét rủi ro, bắt bẻ kẽ hở
        ExecutiveConsultant = 3,  // Cố vấn Điều hành: Tóm tắt chiến lược vĩ mô, khuyến nghị ra quyết định
        ChiefExaminer = 4,        // Trưởng Ban Giám Khảo: Đóng vai phỏng vấn vấn đáp, chấm điểm trực tiếp
        DomainExpert = 5,         // Chuyên gia Kỹ thuật Đầu ngành: Giải thích sâu chi tiết công thức, kỹ thuật
        Custom = 99               // Tùy biến tự do theo System Prompt người dùng nạp vào
    }

    // Thực thể Tin nhắn Đối thoại Nhận thức Nhập vai
    public class NotebookChatMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid NotebookId { get; set; }
        public string Sender { get; set; } = "User"; // "User" | "Assistant"
        public string RoleName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string CitationsJson { get; set; } = "[]"; // [{ sourceId, sourceName, page, textSnippet }]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Notebook Notebook { get; set; } = null!;
    }
}
