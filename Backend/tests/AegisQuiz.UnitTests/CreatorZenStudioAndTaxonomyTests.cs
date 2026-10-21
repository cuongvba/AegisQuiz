using System.Collections.Generic;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Services.Taxonomy;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AegisQuiz.UnitTests
{
    public class CreatorZenStudioAndTaxonomyTests
    {
        private readonly ITaxonomySnifferDispatcher _dispatcher;
        private readonly ExamDeduplicationEngine _dedupEngine;

        public CreatorZenStudioAndTaxonomyTests()
        {
            var strategies = new List<ITaxonomySnifferStrategy>
            {
                new K12MathTaxonomySnifferStrategy(),
                new DrivingLicenseTaxonomySnifferStrategy(),
                new BankingComplianceTaxonomySnifferStrategy(),
                new GeneralFallbackTaxonomySnifferStrategy()
            };
            _dispatcher = new TaxonomySnifferDispatcher(strategies);

            var options = new DbContextOptionsBuilder<AegisQuizDbContext>()
                .UseInMemoryDatabase(databaseName: "ZenStudioTestDb")
                .Options;
            var db = new AegisQuizDbContext(options);
            _dedupEngine = new ExamDeduplicationEngine(db, NullLogger<ExamDeduplicationEngine>.Instance);
        }

        [Fact]
        public void MathStrategy_ShouldAccuratelyDetect_K12MathDocx()
        {
            var sampleMath = "Câu 1. Cho hàm số y = f(x) liên tục trên R và có đạo hàm f'(x) = x^2 - 4. Tìm điểm cực trị và tích phân từ 0 đến 1.";
            var result = _dispatcher.SniffTaxonomy(sampleMath);

            Assert.Equal("EDUCATION", result.DomainCode);
            Assert.Equal("MOET_GDPT_2025", result.BenchmarkStandard);
            Assert.True(result.ConfidenceScore >= 0.85);
        }

        [Fact]
        public void DrivingStrategy_ShouldDetect_GplxQuestions()
        {
            var sampleDriving = "Câu 5. Người lái xe không được lùi xe ở những khu vực nào dưới đây? Đây là câu sa hình biển báo bắt buộc.";
            var result = _dispatcher.SniffTaxonomy(sampleDriving);

            Assert.Equal("GOV_DRIVING", result.DomainCode);
            Assert.Equal("QCVN_41_2019", result.BenchmarkStandard);
            Assert.True(result.ConfidenceScore >= 0.90);
        }

        [Fact]
        public void BankingStrategy_ShouldDetect_BankingCompliance()
        {
            var sampleBanking = "Câu 10. Hạn mức cấp tín dụng đối với một khách hàng không được vượt quá tỷ lệ nào theo quy định của Ngân hàng Nhà nước?";
            var result = _dispatcher.SniffTaxonomy(sampleBanking);

            Assert.Equal("BANKING", result.DomainCode);
            Assert.Equal("CIRCULAR_02_NHNN", result.BenchmarkStandard);
        }

        [Fact]
        public void DeduplicationEngine_ShouldGenerate_IdenticalFingerprints_RegardlessOfWhitespace()
        {
            var content1 = "Câu 1.   Cho tam giác   ABC vuông tại A có AB = 3, AC = 4.";
            var content2 = "Bài 1: Cho tam giác ABC vuông tại A có AB = 3, AC = 4.";
            var opts1 = new List<string> { "BC = 5", "BC = 6", "BC = 7", "BC = 8" };
            var opts2 = new List<string> { "BC = 5  ", "  BC = 6", "BC = 7", "BC = 8" };

            var hash1 = _dedupEngine.ComputeCanonicalContentHash(content1, opts1);
            var hash2 = _dedupEngine.ComputeCanonicalContentHash(content2, opts2);

            Assert.Equal(hash1, hash2);
            Assert.False(string.IsNullOrEmpty(hash1));
        }
    }
}
