using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Application.Services.Taxonomy
{
    public class K12MathTaxonomySnifferStrategy : ITaxonomySnifferStrategy
    {
        public int Priority => 10;

        private static readonly Regex MathPattern = new Regex(
            @"(hàm số|tích phân|đạo hàm|tiệm cận|cực trị|đồ thị|nguyên hàm|không gian\s+oxyz|mặt cầu|mặt phẳng|tọa độ|vectơ|vector|xác suất|biến cố|hình chóp|lăng trụ|nhị thức|lim|f\(x\)|f'\(x\)|\\int|\\frac|\\sqrt)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public bool CanHandle(string contentPreview) =>
            MathPattern.IsMatch(contentPreview ?? string.Empty);

        public TaxonomyAlignmentResult Sniff(string contentPreview)
        {
            var text = (contentPreview ?? string.Empty).ToLowerInvariant();
            var category = "TOAN_GIAI_TICH";
            var categoryName = "Toán Giải Tích 12";
            var cognitive = "THONG_HIEU";

            if (text.Contains("tích phân") || text.Contains("nguyên hàm") || text.Contains("\\int"))
            {
                category = "NGUYEN_HAM_TICH_PHAN";
                categoryName = "Nguyên Hàm & Tích Phân";
                cognitive = "VAN_DUNG";
            }
            else if (text.Contains("oxyz") || text.Contains("mặt phẳng") || text.Contains("mặt cầu"))
            {
                category = "HINH_HOC_OXYZ";
                categoryName = "Hình Học Tọa Độ Oxyz";
                cognitive = "VAN_DUNG";
            }
            else if (text.Contains("xác suất") || text.Contains("biến cố"))
            {
                category = "XAC_SUAT_THONG_KE";
                categoryName = "Xác Suất & Thống Kê";
            }

            if (text.Contains("giá trị lớn nhất") || text.Contains("tham số m") || text.Contains("đồng biến trên"))
            {
                cognitive = "VAN_DUNG_CAO";
            }

            return new TaxonomyAlignmentResult(
                DomainCode: "EDUCATION",
                DomainName: "Giáo dục & Học thuật",
                TargetLevel: "GRADE_12",
                CategoryCode: category,
                CategoryName: categoryName,
                BenchmarkStandard: "MOET_GDPT_2025",
                CognitiveLevel: cognitive,
                ConfidenceScore: 0.95
            );
        }
    }

    public class DrivingLicenseTaxonomySnifferStrategy : ITaxonomySnifferStrategy
    {
        public int Priority => 20;

        private static readonly Regex DrivingPattern = new Regex(
            @"(biển báo|sa hình|giao thông|đường bộ|ngược chiều|xe ưu tiên|nhường đường|tốc độ tối đa|nồng độ cồn|vạch kẻ đường|báo hiệu|hạng b1|hạng b2|hạng c|gplx)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public bool CanHandle(string contentPreview) =>
            DrivingPattern.IsMatch(contentPreview ?? string.Empty);

        public TaxonomyAlignmentResult Sniff(string contentPreview)
        {
            var text = (contentPreview ?? string.Empty).ToLowerInvariant();
            var category = "LUAT_GIAO_THONG_DUONG_BO";
            var categoryName = "Quy tắc Giao thông Đường bộ";
            var cognitive = "THONG_HIEU";

            if (text.Contains("biển báo") || text.Contains("p.102") || text.Contains("qcvn 41"))
            {
                category = "HE_THONG_BIEN_BAO";
                categoryName = "Hệ thống Biển báo Hiệu Đường bộ";
                cognitive = "NHAN_BIET";
            }
            else if (text.Contains("sa hình") || text.Contains("thứ tự các xe"))
            {
                category = "GIAI_THE_SA_HINH";
                categoryName = "Kỹ năng Giải Sa hình";
                cognitive = "VAN_DUNG";
            }

            return new TaxonomyAlignmentResult(
                DomainCode: "GOV_DRIVING",
                DomainName: "Sát hạch Lái xe Quốc gia",
                TargetLevel: "GPLX_B2",
                CategoryCode: category,
                CategoryName: categoryName,
                BenchmarkStandard: "QCVN_41_2019",
                CognitiveLevel: cognitive,
                ConfidenceScore: 0.98
            );
        }
    }

    public class BankingComplianceTaxonomySnifferStrategy : ITaxonomySnifferStrategy
    {
        public int Priority => 30;

        private static readonly Regex BankingPattern = new Regex(
            @"(tín dụng|nợ xấu|bảo đảm|thế chấp|rửa tiền|aml|thanh toán quốc tế|l/c|ngân hàng|khách hàng doanh nghiệp|khdn|tiền gửi|cho vay|hạn mức)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public bool CanHandle(string contentPreview) =>
            BankingPattern.IsMatch(contentPreview ?? string.Empty);

        public TaxonomyAlignmentResult Sniff(string contentPreview)
        {
            var text = (contentPreview ?? string.Empty).ToLowerInvariant();
            var category = "TIN_DUNG_KHDN";
            var categoryName = "Tín dụng Khách hàng Doanh nghiệp";
            var cognitive = "VAN_DUNG";

            if (text.Contains("rửa tiền") || text.Contains("aml") || text.Contains("tuân thủ"))
            {
                category = "PHONG_CHONG_RUA_TIEN";
                categoryName = "Phòng chống Rửa tiền (AML)";
                cognitive = "THONG_HIEU";
            }
            else if (text.Contains("thanh toán quốc tế") || text.Contains("l/c"))
            {
                category = "THANH_TOAN_QUOC_TE";
                categoryName = "Thanh toán Quốc tế & Tài trợ Thương mại";
            }

            return new TaxonomyAlignmentResult(
                DomainCode: "BANKING",
                DomainName: "Tài chính - Ngân hàng",
                TargetLevel: "EXECUTIVE",
                CategoryCode: category,
                CategoryName: categoryName,
                BenchmarkStandard: "CIRCULAR_02_NHNN",
                CognitiveLevel: cognitive,
                ConfidenceScore: 0.92
            );
        }
    }

    public class GeneralFallbackTaxonomySnifferStrategy : ITaxonomySnifferStrategy
    {
        public int Priority => 999;

        public bool CanHandle(string contentPreview) => true;

        public TaxonomyAlignmentResult Sniff(string contentPreview)
        {
            return new TaxonomyAlignmentResult(
                DomainCode: "GENERAL",
                DomainName: "Tổng hợp / Đại cương",
                TargetLevel: "STANDARD",
                CategoryCode: "KIEN_THUC_CHUNG",
                CategoryName: "Kiến thức Đại cương",
                BenchmarkStandard: "GENERAL_STANDARD",
                CognitiveLevel: "THONG_HIEU",
                ConfidenceScore: 0.70
            );
        }
    }

    public class TaxonomySnifferDispatcher : ITaxonomySnifferDispatcher
    {
        private readonly IEnumerable<ITaxonomySnifferStrategy> _strategies;

        public TaxonomySnifferDispatcher(IEnumerable<ITaxonomySnifferStrategy> strategies)
        {
            _strategies = strategies.OrderBy(s => s.Priority);
        }

        public TaxonomyAlignmentResult SniffTaxonomy(string contentPreview)
        {
            var strategy = _strategies.FirstOrDefault(s => s.CanHandle(contentPreview));
            return strategy != null
                ? strategy.Sniff(contentPreview)
                : new TaxonomyAlignmentResult(
                    DomainCode: "GENERAL",
                    DomainName: "Tổng hợp / Đại cương",
                    TargetLevel: "STANDARD",
                    CategoryCode: "KIEN_THUC_CHUNG",
                    CategoryName: "Kiến thức Đại cương",
                    BenchmarkStandard: "GENERAL_STANDARD",
                    CognitiveLevel: "THONG_HIEU",
                    ConfidenceScore: 0.50
                );
        }
    }
}
