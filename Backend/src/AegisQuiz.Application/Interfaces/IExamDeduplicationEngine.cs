using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// Báo cáo thanh tra trùng lặp cho phiên nhập đề
    /// </summary>
    public class ExamDeduplicationReport
    {
        public int TotalIncomingQuestions { get; set; }
        public int DuplicateCount { get; set; }
        public int UniqueCount { get; set; }
        public double DeduplicationRatePercentage => TotalIncomingQuestions > 0 
            ? Math.Round((double)DuplicateCount / TotalIncomingQuestions * 100, 1) 
            : 0;

        public List<ExamClusterMatch> ClusterMatches { get; set; } = new();
    }

    /// <summary>
    /// [Động cơ Nhận diện Bản thể học Đề thi & Khử trùng lặp Đa tầng v2026]
    /// </summary>
    public interface IExamDeduplicationEngine
    {
        /// <summary>
        /// Tính toán chuỗi mã băm vân tay chuẩn tắc (Canonical Content Hash) từ nội dung và các phương án.
        /// Miễn nhiễm với cách đánh số thứ tự (Câu 1/Bài 1), khoảng trắng, công thức toán và thứ tự xáo trộn phương án.
        /// </summary>
        string ComputeCanonicalContentHash(string content, IEnumerable<string>? options);

        /// <summary>
        /// Tính toán độ tương đồng Jaccard giữa hai tập hợp câu hỏi (theo vân tay chuẩn tắc).
        /// J(A, B) = |A ∩ B| / |A ∪ B|
        /// </summary>
        double ComputeJaccardSimilarity(IEnumerable<string> setA, IEnumerable<string> setB);

        /// <summary>
        /// Kiểm tra và đánh dấu trùng lặp đối với danh sách câu hỏi xem trước (Preview Questions).
        /// </summary>
        Task<ExamDeduplicationReport> AnalyzeAndTagDuplicatesAsync(
            List<DocxImportPreviewDto> incomingQuestions, 
            Guid tenantId,
            CancellationToken ct = default);

        /// <summary>
        /// Hợp nhất vết tích nguồn gốc (Provenance Appearance) vào câu hỏi đã có sẵn trong cơ sở dữ liệu.
        /// Tăng chỉ số tần suất xuất hiện và bảo toàn toàn vẹn lịch sử đối chứng mà không tạo bản ghi mới.
        /// </summary>
        void MergeProvenanceAppearance(QuestionBase existingQuestion, ProvenanceAppearance appearance);

        /// <summary>
        /// Gắn liên kết một chủ đề / đề thi mới vào câu hỏi đã có (Quan hệ N - M)
        /// Cập nhật Tags và linkedTopicCodes trong JSONB Payload mà không nhân bản câu hỏi vật lý.
        /// </summary>
        void AssociateTopicToQuestion(QuestionBase question, string topicCode, string? topicName = null);

        /// <summary>
        /// Gỡ bỏ liên kết một chủ đề khỏi câu hỏi.
        /// Nếu câu hỏi đang có CategoryCode == topicCode, tự động thăng cấp một chủ đề liên kết khác
        /// hoặc chuyển về fallbackCode.
        /// </summary>
        void DisassociateTopicFromQuestion(QuestionBase question, string topicCode, string? fallbackCode = "GENERAL");
    }
}
