using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// [IMS QTI 3.0 & Universal Stimulus Architecture]
    /// Lưu trữ Ngữ cảnh / Dữ liệu dùng chung cho một nhóm câu hỏi:
    /// - Đoạn văn đọc hiểu (Reading Comprehension Passage cho đề tiếng Anh / Ngoại ngữ / Ngữ văn)
    /// - Đoạn ghi âm nghe hiểu (Listening Audio Clip)
    /// - Đồ thị / Biểu đồ / Sơ đồ phân tích (Chart / Diagram / Infographic)
    /// - Tình huống phân tích nghiệp vụ (Business / Clinical / Legal Case Study)
    /// Giúp tiết kiệm đến 80% dung lượng CSDL và mang lại trải nghiệm Split-screen chuẩn quốc tế cho thí sinh.
    /// </summary>
    public class QuestionContext : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Tiêu đề ngắn của ngữ cảnh (ví dụ: "[Bài đọc hiểu] (Câu 7 - 11)")
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Nội dung chi tiết của đoạn văn / tình huống dùng chung (hỗ trợ Markdown / HTML / Math)
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Định dạng ngữ cảnh: "text" | "markdown" | "audio" | "image" | "mixed"
        /// </summary>
        public string ContentType { get; set; } = "text";

        /// <summary>
        /// Đường dẫn tệp đính kèm (URL file Audio MP3 nghe hiểu, ảnh sơ đồ lớn...)
        /// </summary>
        public string? MediaUrl { get; set; }

        /// <summary>
        /// Mã băm SHA-256 của nội dung để tự động khử trùng lặp (Deduplication) khi nhập tệp
        /// </summary>
        public string? ContentHash { get; set; }

        /// <summary>
        /// Mã định danh Tenant (Đa khách thuê)
        /// </summary>
        public Guid TenantId { get; set; } = Guid.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Danh sách các câu hỏi con sử dụng chung ngữ cảnh này
        /// </summary>
        [JsonIgnore]
        public virtual ICollection<QuestionBase> Questions { get; set; } = new List<QuestionBase>();
    }
}
