using System;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// [Enterprise Fortress — Epoch 3] Nhật ký kiểm toán bất biến chống giả mạo (Tamper-Evident Audit Trail).
    /// Áp dụng cơ chế liên kết chuỗi băm mật mã học (Cryptographic Hash Chaining / SHA-256)
    /// tương tự công nghệ phân tán để bảo đảm tính minh bạch tuyệt đối của kỳ thi doanh nghiệp/ngân hàng.
    /// </summary>
    public class ExamAuditLog : ITenantEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Khách thuê (Tenant) sở hữu kỳ thi</summary>
        public Guid TenantId { get; set; }

        /// <summary>Định danh kỳ thi hoặc phiên thi</summary>
        public Guid ExamId { get; set; }

        /// <summary>Thí sinh hoặc giám thị thực hiện hành động</summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Hành vi / Sự kiện được ghi nhận:
        /// EXAM_PREWARMED, CANDIDATE_JOINED, QUESTION_ANSWERED, TAB_SWITCH, WINDOW_BLUR,
        /// MULTI_MONITOR_DETECTED, CLIPBOARD_COPY, CLIPBOARD_PASTE, RAPID_ANSWER_ANOMALY,
        /// EXAM_SUBMITTED, VOIDED_BY_PROCTOR, AUDIT_INTEGRITY_VERIFIED
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>Mức độ nghiêm trọng: INFO, WARNING, CRITICAL, DISQUALIFIED</summary>
        public string Severity { get; set; } = "INFO";

        /// <summary>Chi tiết bổ sung dưới dạng JSON (tọa độ chuột, nội dung clipboard, user agent...)</summary>
        public string Details { get; set; } = "{}";

        /// <summary>Địa chỉ IP của client</summary>
        public string ClientIp { get; set; } = "127.0.0.1";

        /// <summary>Trình duyệt / Thiết bị của client</summary>
        public string UserAgent { get; set; } = string.Empty;

        /// <summary>Thứ tự tuần tự bất biến của bản ghi trong chuỗi kiểm toán của ExamId</summary>
        public long SequenceNumber { get; set; }

        /// <summary>Mã băm SHA-256 của bản ghi liền trước (Genesis Block = "GENESIS_0000000000000000")</summary>
        public string PreviousLogHash { get; set; } = "GENESIS_0000000000000000";

        /// <summary>
        /// Mã băm SHA-256 của bản ghi hiện tại:
        /// SHA256(TenantId + ExamId + UserId + Action + TimestampUtc + ClientIp + SequenceNumber + PreviousLogHash + Details)
        /// </summary>
        public string CurrentHash { get; set; } = string.Empty;

        /// <summary>Mốc thời gian UTC được đóng dấu bất biến</summary>
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}
