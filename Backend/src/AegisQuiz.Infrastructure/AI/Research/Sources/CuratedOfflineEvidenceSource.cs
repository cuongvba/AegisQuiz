using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Infrastructure.AI.Research.Sources
{
    /// <summary>
    /// [Evidence Plugin] Thư viện Bằng chứng Ngoại tuyến đã được Thẩm định Khoa học (Curated Peer-Reviewed Library).
    /// Cung cấp các dữ liệu định lượng, chỉ số đo lường và DOI có thẩm quyền cao nhất cho các chủ đề trọng điểm.
    /// </summary>
    public class CuratedOfflineEvidenceSource : IEvidenceSource
    {
        public string SourceName => "CuratedOfflineLibrary";
        public int Priority => 10;

        public bool CanHandle(string topicTitle, string coreProblemStatement)
        {
            var text = $"{topicTitle} {coreProblemStatement}";
            return MatchesAnthropocene(text) || MatchesBanking(text) || MatchesIrtEdtech(text) || MatchesPhysics(text);
        }

        public Task<List<EvidenceLedgerItemDto>> GatherEvidenceAsync(
            string topicTitle,
            string coreProblemStatement,
            CancellationToken ct = default)
        {
            var text = $"{topicTitle} {coreProblemStatement}";
            var items = new List<EvidenceLedgerItemDto>();

            if (MatchesAnthropocene(text))
            {
                items.Add(new EvidenceLedgerItemDto
                {
                    ClaimOrFact = "Quần thể động vật hoang dã có xương sống toàn cầu sụt giảm trung bình 69% trong giai đoạn 1970-2018 do suy giảm sinh cảnh và khai thác quá mức.",
                    QuantitativeValue = "Giảm 69% (LPI Index, 1970-2018)",
                    SourceTitle = "WWF / ZSL Living Planet Report 2022 - Building a nature-positive society",
                    DoiOrUrl = "https://doi.org/10.1016/j.biocon.2022.109720",
                    Provenance = "CuratedOfflineLibrary",
                    IsOnlineVerified = false,
                    ConfidenceWeight = 0.98
                });

                items.Add(new EvidenceLedgerItemDto
                {
                    ClaimOrFact = "Khoảng 1 triệu loài động thực vật hiện đang đối mặt với nguy cơ tuyệt chủng trong vài thập kỷ tới nếu không có hành động đảo ngược xu thế.",
                    QuantitativeValue = "1,000,000 loài có nguy cơ tuyệt chủng",
                    SourceTitle = "IPBES Global Assessment Report on Biodiversity and Ecosystem Services (Díaz et al., 2019)",
                    DoiOrUrl = "https://doi.org/10.5281/zenodo.3831673",
                    Provenance = "CuratedOfflineLibrary",
                    IsOnlineVerified = false,
                    ConfidenceWeight = 0.99
                });

                items.Add(new EvidenceLedgerItemDto
                {
                    ClaimOrFact = "Sinh khối của loài người (36%) và vật nuôi thuần hóa (60%) chiếm tới 96% tổng sinh khối động vật có vú trên cạn, động vật hoang dã chỉ còn chiếm 4%.",
                    QuantitativeValue = "Sinh khối thú hoang dã còn lại: 4%",
                    SourceTitle = "Bar-On, Phillips, & Milo. The biomass distribution on Earth. PNAS 115(25):6506-6511 (2018)",
                    DoiOrUrl = "https://doi.org/10.1073/pnas.1711842115",
                    Provenance = "CuratedOfflineLibrary",
                    IsOnlineVerified = false,
                    ConfidenceWeight = 0.97
                });

                items.Add(new EvidenceLedgerItemDto
                {
                    ClaimOrFact = "Tốc độ tuyệt chủng hiện tại ở Kỷ Nhân sinh (Anthropocene) ước tính cao gấp 100 đến 1,000 lần so với tốc độ nền địa chất tự nhiên.",
                    QuantitativeValue = "Cao gấp 100x - 1,000x tốc độ nền",
                    SourceTitle = "Ceballos et al. Accelerated modern human-induced species losses: Entering the sixth mass extinction. Science Advances 1(5) (2015)",
                    DoiOrUrl = "https://doi.org/10.1126/sciadv.1400253",
                    Provenance = "CuratedOfflineLibrary",
                    IsOnlineVerified = false,
                    ConfidenceWeight = 0.98
                });

                items.Add(new EvidenceLedgerItemDto
                {
                    ClaimOrFact = "Khái niệm Kỷ Nhân sinh (Anthropocene) ghi nhận tác động của con người đã vượt các lực lượng tự nhiên trong việc định hình địa chất và khí hậu Trái Đất.",
                    QuantitativeValue = "Tập trung biến đổi địa tầng từ giữa thế kỷ 20",
                    SourceTitle = "Crutzen, P. J. Geology of mankind. Nature 415, 23 (2002)",
                    DoiOrUrl = "https://doi.org/10.1038/415023a",
                    Provenance = "CuratedOfflineLibrary",
                    IsOnlineVerified = false,
                    ConfidenceWeight = 0.96
                });
            }
            else if (MatchesBanking(text))
            {
                items.Add(new EvidenceLedgerItemDto
                {
                    ClaimOrFact = "Khung chuẩn Basel III yêu cầu tỷ lệ an toàn vốn tối thiểu (CAR) 8% kèm bộ đệm vốn bảo toàn (CCB) 2.5% để chống chịu sốc thanh khoản toàn hệ thống.",
                    QuantitativeValue = "CAR tối thiểu 8.0%, CCB 2.5%",
                    SourceTitle = "Basel Committee on Banking Supervision (BCBS) - Basel III: A global regulatory framework (2011)",
                    DoiOrUrl = "https://www.bis.org/bcbs/basel3.htm",
                    Provenance = "CuratedOfflineLibrary",
                    IsOnlineVerified = false,
                    ConfidenceWeight = 0.99
                });

                items.Add(new EvidenceLedgerItemDto
                {
                    ClaimOrFact = "Tỷ lệ Đảm bảo Khả năng Thanh khoản (LCR) yêu cầu ngân hàng nắm giữ đủ tài sản có tính thanh khoản cao (HQLA) để sống sót qua 30 ngày khủng hoảng nghiêm trọng.",
                    QuantitativeValue = "LCR >= 100% trong kịch bản stress test 30 ngày",
                    SourceTitle = "BCBS - Basel III: The Liquidity Coverage Ratio and liquidity risk monitoring tools (2013)",
                    DoiOrUrl = "https://www.bis.org/publ/bcbs238.htm",
                    Provenance = "CuratedOfflineLibrary",
                    IsOnlineVerified = false,
                    ConfidenceWeight = 0.98
                });
            }
            else if (MatchesIrtEdtech(text))
            {
                items.Add(new EvidenceLedgerItemDto
                {
                    ClaimOrFact = "Mô hình IRT 3 tham số (3-PL) mô hình hóa xác suất trả lời đúng dựa trên: Độ khó (b), Độ phân biệt (a) và Tham số đoán mò ngẫu nhiên (c).",
                    QuantitativeValue = "Hàm Logistic P(theta) = c + (1-c)/(1 + exp(-1.7a(theta - b)))",
                    SourceTitle = "Lord, F. M., & Novick, M. R. Statistical Theories of Mental Test Scores. Addison-Wesley (1968)",
                    DoiOrUrl = "https://doi.org/10.1111/j.1745-3984.1969.tb00676.x",
                    Provenance = "CuratedOfflineLibrary",
                    IsOnlineVerified = false,
                    ConfidenceWeight = 0.99
                });
            }
            else if (MatchesPhysics(text))
            {
                items.Add(new EvidenceLedgerItemDto
                {
                    ClaimOrFact = "Định lý Bất toàn của Kurt Gödel chứng minh rằng mọi hệ tiên đề hình thức đủ mạnh đều chứa những mệnh đề chân lý không thể chứng minh hoặc bác bỏ bên trong hệ đó.",
                    QuantitativeValue = "Không thể chứng minh tính phi mâu thuẫn từ nội tại",
                    SourceTitle = "Gödel, K. Über formal unentscheidbare Sätze der Principia Mathematica und verwandter Systeme I (1931)",
                    DoiOrUrl = "https://doi.org/10.1007/BF01700692",
                    Provenance = "CuratedOfflineLibrary",
                    IsOnlineVerified = false,
                    ConfidenceWeight = 0.99
                });
            }

            return Task.FromResult(items);
        }

        private static bool MatchesAnthropocene(string text)
        {
            var p = @"(tàn phá|tuyệt chủng|kỷ nhân sinh|anthropocene|đa dạng sinh học|sinh quyển|biomass|hệ sinh thái|môi trường|hoang dã|biến đổi khí hậu|phá hủy|con người.*trái đất)";
            return Regex.IsMatch(text, p, RegexOptions.IgnoreCase);
        }

        private static bool MatchesBanking(string text)
        {
            var p = @"(ngân hàng|basel|tín dụng|rủi ro|nợ xấu|lãi suất|thanh khoản|car|camels|vốn điều lệ)";
            return Regex.IsMatch(text, p, RegexOptions.IgnoreCase);
        }

        private static bool MatchesIrtEdtech(string text)
        {
            var p = @"(irt|item response|khảo thí|đo lường|trắc nghiệm|độ khó|độ phân biệt|rasch|fsrs|dkt)";
            return Regex.IsMatch(text, p, RegexOptions.IgnoreCase);
        }

        private static bool MatchesPhysics(string text)
        {
            var p = @"(lượng tử|quantum|gödel|tiên đề|vật lý|tương đối|wheeler|hỗn độn|entropi)";
            return Regex.IsMatch(text, p, RegexOptions.IgnoreCase);
        }
    }
}
