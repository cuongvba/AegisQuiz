using System;
using AegisQuiz.Infrastructure.Excel;
using Xunit;

namespace AegisQuiz.UnitTests
{
    public class SmartTopicAbbreviationTests
    {
        [Theory]
        [InlineData("07. Kế toán giao dịch nội bộ.xlsx", "KT_GDNB")]
        [InlineData("Nghiệp vụ tín dụng KHDN 2026.xlsx", "TD_KHDN_26")]
        [InlineData("Thẩm định tài sản bảo đảm", "TDTS_TSBD")]
        [InlineData("Giao dịch viên - Đợt 2", "GDV_D2")]
        [InlineData("Kiểm ngân viên và thủ quỹ", "KNV_TQ")]
        [InlineData("Thanh toán quốc tế", "TTQT")]
        [InlineData("Phòng chống rửa tiền AML", "PCRT_AML")]
        [InlineData("Quản trị rủi ro tín dụng", "QTRR_TD")]
        [InlineData("Xử lý nợ xấu", "XLN")]
        [InlineData("Thẻ tín dụng quốc tế", "THE_TDQT")]
        [InlineData("Giấy phép lái xe hạng B2", "GPLX_B2")]
        [InlineData("Toán học lớp 12", "TOAN_12")]
        [InlineData("Kỹ năng xử lý khiếu nại khách hàng", "XLKN_KH")]
        public void Abbreviate_ShouldGenerateConciseMeaningfulCode(string input, string expectedSubstring)
        {
            var code = SmartTopicAbbreviationEngine.Abbreviate(input);

            Assert.False(string.IsNullOrWhiteSpace(code));
            Assert.True(code.Length <= 18, $"Code '{code}' is longer than 18 characters.");
            Assert.Contains(expectedSubstring, code);
        }

        [Fact]
        public void BuildHierarchicalTopicInfo_BankingExcelFile_ShouldCreateCompactCodesAndPreserveAccents()
        {
            var folder = "2026-DOT2";
            var file = "07. Kế toán giao dịch nội bộ.xlsx";
            var sheet = "Thẻ tín dụng quốc tế 240 câu";

            var (parentCode, parentName, childCode, childName, desc) =
                SmartTopicAbbreviationEngine.BuildHierarchicalTopicInfo(folder, file, sheet, 240);

            // Parent Topic
            Assert.Equal("2026-DOT2", parentName);
            Assert.Equal("2026_DOT2", parentCode);

            // Child Topic: Giữ nguyên tiếng Việt có dấu cho Tên
            Assert.Equal("07. Kế toán giao dịch nội bộ — Thẻ tín dụng quốc tế", childName);
            // Mã con rút gọn thông minh có tiền tố đợt thi
            Assert.StartsWith("2026_DOT2", childCode);
            Assert.Contains("KT_GDNB", childCode);

            // Description
            Assert.Contains("240 câu hỏi", desc);
            Assert.Contains("Kế toán giao dịch nội bộ", desc);
        }

        [Fact]
        public void BuildHierarchicalTopicInfo_GenericFolder_ShouldFallbackToChungWithoutPrefixBloat()
        {
            var folder = "CHUNG";
            var file = "Nghiệp vụ Tín dụng SME.xlsx";
            var sheet = "Sheet1";

            var (parentCode, parentName, childCode, childName, _) =
                SmartTopicAbbreviationEngine.BuildHierarchicalTopicInfo(folder, file, sheet, 50);

            Assert.Equal("CHUNG", parentCode);
            Assert.Equal("Chủ đề chung", parentName);
            Assert.Equal("Nghiệp vụ Tín dụng SME", childName);
            Assert.Contains("TD_SME", childCode);
        }
    }
}
