using System;
using System.Collections.Generic;

namespace AegisQuiz.Domain.Entities
{
    // [AegisNotebook] Giáo trình được sinh tự động từ PDF
    public class Notebook
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string SourcePdfUrl { get; set; } = string.Empty; // Link file PDF gốc trên Cloud Storage
        public Guid CreatedByAdminId { get; set; } // Chỉ Admin mới được tạo
        
        public NotebookStatus Status { get; set; } = NotebookStatus.Processing;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Cây kiến thức: 1 Notebook có nhiều Chương
        public List<NotebookChapter> Chapters { get; set; } = new();
    }

    public class NotebookChapter
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid NotebookId { get; set; }
        
        public int OrderIndex { get; set; } // Thứ tự chương (1, 2, 3...)
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty; // Tóm tắt do AI viết
        public string RawContent { get; set; } = string.Empty; // Nội dung gốc trích từ PDF
        
        // Các ý chính (Key Points) do AI rút trích, lưu dạng JSON
        public string KeyPointsJson { get; set; } = "[]";

        // Liên kết: 1 Chương sinh ra nhiều Câu hỏi
        public List<Guid> GeneratedQuestionIds { get; set; } = new();
        
        // Navigation property
        public Notebook Notebook { get; set; } = null!;
    }

    public enum NotebookStatus
    {
        Processing,  // Đang phân tích PDF
        Structuring, // Đang cấu trúc hóa (Gemini đang đọc)
        Generating,  // Đang sinh câu hỏi
        Ready,       // Hoàn tất, sẵn sàng cho học sinh
        Failed       // Lỗi
    }
}
