using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Infrastructure.Services;
using Xunit;

namespace AegisQuiz.UnitTests
{
    public class ExamOntologyAndDeduplicationTests
    {
        private readonly ExamDeduplicationEngine _engine;

        public ExamOntologyAndDeduplicationTests()
        {
            var options = new DbContextOptionsBuilder<AegisQuizDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var dbContext = new AegisQuizDbContext(options);
            _engine = new ExamDeduplicationEngine(dbContext, NullLogger<ExamDeduplicationEngine>.Instance);
        }

        [Fact]
        public void CanonicalFingerprint_PrefixInvariance_MatchesIdenticalHash()
        {
            // Cùng một câu hỏi nhưng ở 2 đề khác nhau có tiền tố đánh số khác nhau ("Câu 1:" vs "Bài 45.")
            var q1 = "Câu 1: Cho hàm số $f(x) = x^2 + 1$. Khẳng định nào sau đây đúng?";
            var q2 = "Bài 45. Cho hàm số $f(x) = x^2 + 1$. Khẳng định nào sau đây đúng?";

            var opts1 = new List<string> { "A. Đồng biến trên R", "B. Nghịch biến trên R", "C. Có cực trị tại x=0", "D. Không có cực trị" };
            var opts2 = new List<string> { "1. Đồng biến trên R", "2. Nghịch biến trên R", "3. Có cực trị tại x=0", "4. Không có cực trị" };

            var hash1 = _engine.ComputeCanonicalContentHash(q1, opts1);
            var hash2 = _engine.ComputeCanonicalContentHash(q2, opts2);

            Assert.False(string.IsNullOrWhiteSpace(hash1));
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void CanonicalFingerprint_ShuffledOptionsInvariance_MatchesIdenticalHash()
        {
            // Cùng một câu hỏi nhưng các phương án A, B, C, D bị hoán vị / đảo mã đề
            var prompt = "Thủ đô của nước Cộng hòa Xã hội Chủ nghĩa Việt Nam là:";
            var optsA = new List<string> { "A. Hà Nội", "B. Huế", "C. Đà Nẵng", "D. TP. Hồ Chí Minh" };
            var optsB = new List<string> { "A. TP. Hồ Chí Minh", "B. Huế", "C. Hà Nội", "D. Đà Nẵng" };

            var hashA = _engine.ComputeCanonicalContentHash(prompt, optsA);
            var hashB = _engine.ComputeCanonicalContentHash(prompt, optsB);

            Assert.Equal(hashA, hashB);
        }

        [Fact]
        public void CanonicalFingerprint_MathNormalization_MatchesIdenticalHash()
        {
            // Chuẩn hóa ký hiệu toán học tương đương và khoảng trắng trong công thức LaTeX
            var q1 = "Tìm tập nghiệm của bất phương trình $2 \\times x \\le 10$.";
            var q2 = "Tìm tập nghiệm của bất phương trình $2 * x <= 10$.";

            var hash1 = _engine.ComputeCanonicalContentHash(q1, null);
            var hash2 = _engine.ComputeCanonicalContentHash(q2, null);

            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void JaccardSimilarity_OverlapRatio_CalculatedAccurately()
        {
            // Kiểm tra giải thuật phân cụm đề thi (Exam Clustering)
            var setA = new List<string> { "H1", "H2", "H3", "H4", "H5", "H6", "H7", "H8" };
            var setB = new List<string> { "H1", "H2", "H3", "H4", "H5", "H6", "H9", "H10" };

            // Giao = 6 (H1..H6), Hợp = 10 (H1..H10) -> Jaccard = 6/10 = 0.6
            var similarity = _engine.ComputeJaccardSimilarity(setA, setB);

            Assert.Equal(0.6, Math.Round(similarity, 2));
        }

        [Fact]
        public void ProvenanceAppearanceMerge_CorrectlyAppendsAndIncrementsCount()
        {
            // Khởi tạo câu hỏi có sẵn với 1 lần xuất hiện ban đầu
            var initialAppearance = new ProvenanceAppearance
            {
                SourceFile = "DeThiThu2025_TruongChuyen.docx",
                ExamTitleInFile = "Đề thi thử THPT Quốc gia 2025",
                SectionInFile = "Phần I",
                OriginalQuestionNo = "1",
                RowIndex = 12,
                ImportedAt = DateTime.UtcNow.AddDays(-10)
            };

            var question = new SingleChoiceQuestion
            {
                Id = Guid.NewGuid(),
                Content = "Phương trình nào dưới đây là phương trình bậc nhất hai ẩn?",
                Payload = JsonSerializer.SerializeToDocument(new
                {
                    canonicalContentHash = "hash123456",
                    sourceFileName = "DeThiThu2025_TruongChuyen.docx",
                    sourceExamName = "Đề thi thử THPT Quốc gia 2025",
                    sourceSectionName = "Phần I",
                    sourceQuestionNo = "1",
                    sourceRowIndex = 12,
                    occurrenceCount = 1,
                    _provenance = new
                    {
                        canonicalContentHash = "hash123456",
                        occurrences = 1,
                        firstIngestedAt = DateTime.UtcNow.AddDays(-10).ToString("o"),
                        appearances = new[] { initialAppearance }
                    }
                })
            };

            // Câu hỏi này lại được tìm thấy trong tệp "TuyenTap40DeToan2026.docx" tại Đề 15, Câu 3
            var newAppearance = new ProvenanceAppearance
            {
                SourceFile = "TuyenTap40DeToan2026.docx",
                ExamTitleInFile = "Đề số 15 - Trường THPT Chuyên KHTN",
                SectionInFile = "Phần I - Trắc nghiệm nhiều lựa chọn",
                OriginalQuestionNo = "3",
                RowIndex = 450,
                ImportedAt = DateTime.UtcNow
            };

            _engine.MergeProvenanceAppearance(question, newAppearance);

            // Kiểm tra xem số lần xuất hiện có được tăng lên 2 và lưu đầy đủ phả hệ truy vết hay không
            Assert.Equal(2, question.OccurrenceCount);
            Assert.NotNull(question.Appearances);
            Assert.Equal(2, question.Appearances!.Count);

            var first = question.Appearances[0];
            Assert.Equal("DeThiThu2025_TruongChuyen.docx", first.SourceFile);

            var second = question.Appearances[1];
            Assert.Equal("TuyenTap40DeToan2026.docx", second.SourceFile);
            Assert.Equal("Đề số 15 - Trường THPT Chuyên KHTN", second.ExamTitleInFile);
            Assert.Equal("3", second.OriginalQuestionNo);
            Assert.Equal(450, second.RowIndex);
        }

        [Fact]
        public void ProvenanceAppearanceMerge_Idempotent_DoesNotDuplicateIdenticalAppearance()
        {
            var initialAppearance = new ProvenanceAppearance
            {
                SourceFile = "DeThi.docx",
                ExamTitleInFile = "Đề 1",
                RowIndex = 10
            };

            var question = new SingleChoiceQuestion
            {
                Id = Guid.NewGuid(),
                Content = "Test Prompt",
                Payload = JsonSerializer.SerializeToDocument(new
                {
                    canonicalContentHash = "hash_test",
                    occurrenceCount = 1,
                    _provenance = new
                    {
                        canonicalContentHash = "hash_test",
                        occurrences = 1,
                        appearances = new[] { initialAppearance }
                    }
                })
            };

            // Nạp lại đúng cùng file, cùng đề, cùng dòng
            var duplicateAppearance = new ProvenanceAppearance
            {
                SourceFile = "DeThi.docx",
                ExamTitleInFile = "Đề 1",
                RowIndex = 10
            };

            _engine.MergeProvenanceAppearance(question, duplicateAppearance);

            // Số lần xuất hiện và danh sách không bị tăng trùng lặp
            Assert.Equal(1, question.OccurrenceCount);
            Assert.Single(question.Appearances!);
        }

        [Fact]
        public void AssociateTopicToQuestion_MultipleTopics_BothLinkedSuccessfully()
        {
            // Bắt đầu với câu hỏi thuộc chủ đề môn học gốc TOAN_12
            var question = new SingleChoiceQuestion
            {
                Id = Guid.NewGuid(),
                Content = "Nguyên hàm của hàm số $f(x) = 2x$ là:",
                CategoryCode = "TOAN_12",
                Tags = new List<string> { "toan-12", "nguyen-ham" }
            };

            // Khi đề 102 được nạp (hoặc gán thêm), câu hỏi được liên kết thêm với DE_102
            _engine.AssociateTopicToQuestion(question, "DE_102", "Đề 102 - Kỳ thi TNTHPT 2026");

            Assert.Contains("TOAN_12", question.LinkedTopicCodes);
            Assert.Contains("DE_102", question.LinkedTopicCodes);
            Assert.Contains("#topic:DE_102", question.Tags);

            // Gán tiếp vào đề tổng hợp ôn thi đại học ON_THI_DH
            _engine.AssociateTopicToQuestion(question, "ON_THI_DH", "Tuyển tập ôn thi đại học");

            Assert.Contains("ON_THI_DH", question.LinkedTopicCodes);
            Assert.Contains("#topic:ON_THI_DH", question.Tags);
            Assert.Equal(3, question.LinkedTopicCodes.Count);

            // Kiểm tra tính Idempotent: Gán lại DE_102 lần 2 không làm phình duplicate tags
            _engine.AssociateTopicToQuestion(question, "DE_102", "Đề 102 - Kỳ thi TNTHPT 2026");
            Assert.Single(question.Tags, t => t == "#topic:DE_102");
            Assert.Equal(3, question.LinkedTopicCodes.Count);
        }

        [Fact]
        public void DisassociateTopicFromQuestion_SharedQuestion_PreservesEntityAndPromotesNextTopic()
        {
            var question = new SingleChoiceQuestion
            {
                Id = Guid.NewGuid(),
                Content = "Giá trị cực đại của hàm số $y = x^3 - 3x$ là:",
                CategoryCode = "DE_102",
                Tags = new List<string> { "#topic:DE_102", "#topic:TOAN_12", "#topic:ON_THI_DH" }
            };

            // Đăng ký linked topics vào JSONB
            _engine.AssociateTopicToQuestion(question, "DE_102", "Đề 102");
            _engine.AssociateTopicToQuestion(question, "TOAN_12", "Toán 12");
            _engine.AssociateTopicToQuestion(question, "ON_THI_DH", "Ôn thi ĐH");

            // Khi người dùng xóa hoặc hủy liên kết khỏi DE_102:
            _engine.DisassociateTopicFromQuestion(question, "DE_102", null);

            // DE_102 không còn trong danh sách liên kết
            Assert.DoesNotContain("DE_102", question.LinkedTopicCodes);
            Assert.DoesNotContain("#topic:DE_102", question.Tags);

            // Chủ đề còn lại được tự động thăng hạng (Promotion) thành CategoryCode mới
            Assert.Equal("TOAN_12", question.CategoryCode);
            Assert.Contains("TOAN_12", question.LinkedTopicCodes);
            Assert.Contains("ON_THI_DH", question.LinkedTopicCodes);
        }

        [Fact]
        public void DisassociateTopicFromQuestion_SoleTopic_FallsBackToGeneral()
        {
            var question = new SingleChoiceQuestion
            {
                Id = Guid.NewGuid(),
                Content = "Phương trình $e^x = 1$ có nghiệm là:",
                CategoryCode = "DE_CHUYEN_HA_NOI"
            };

            // Hủy chủ đề duy nhất mà không có chủ đề thay thế
            _engine.DisassociateTopicFromQuestion(question, "DE_CHUYEN_HA_NOI", null);

            Assert.Equal("GENERAL", question.CategoryCode);
            Assert.DoesNotContain("DE_CHUYEN_HA_NOI", question.LinkedTopicCodes);
        }
    }
}
