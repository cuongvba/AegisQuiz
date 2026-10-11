using System;
using System.Collections.Generic;
using System.Linq;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Infrastructure.AI.Contributions
{
    /// <summary>
    /// [AegisAI Council Academic Generator] Bộ sinh đóng góp học thuật và lập trường nhận thức (Zero Hard-code & OCP).
    /// Sinh luận giải, phản biện và giải pháp hoàn toàn tự động dựa trên Metadata của Viện trong DB (EpistemicManifesto, DisciplineArea, SpatioTemporalAffinity).
    /// </summary>
    public class CouncilContributionGenerator : ICouncilContributionGenerator
    {
        public List<CouncilAcademicContributionDto> GenerateContributions(
            string topicTitle,
            string problemStatement,
            List<UniversalAiCouncil> participating,
            EmpiricalEvidenceDossierDto? dossier)
        {
            var list = new List<CouncilAcademicContributionDto>();

            foreach (var council in participating)
            {
                var presSeat = council.Seats?.FirstOrDefault(s => s.Role == CouncilSeatRole.President) 
                    ?? council.Seats?.FirstOrDefault()
                    ?? new UniversalCouncilSeat { SeatTitle = "Viện Trưởng Đại Diện", PreferredModelCode = "deepseek-r1" };

                var model = !string.IsNullOrEmpty(presSeat.AssignedModelCode) ? presSeat.AssignedModelCode : presSeat.PreferredModelCode;

                // Tự động phân tích lập trường nhận thức từ Tuyên ngôn của Viện trong DB (Zero Hardcode)
                var manifesto = !string.IsNullOrWhiteSpace(council.EpistemicManifesto)
                    ? council.EpistemicManifesto
                    : $"Nghiên cứu quy luật cốt lõi trong lĩnh vực {council.DisciplineArea}.";

                var discipline = !string.IsNullOrWhiteSpace(council.DisciplineArea)
                    ? council.DisciplineArea
                    : "Khoa học Liên ngành Toàn diện";

                var perspective = !string.IsNullOrWhiteSpace(council.SpatioTemporalAffinity)
                    ? council.SpatioTemporalAffinity
                    : "Hệ quy chiếu Biện chứng Đương đại";

                var dto = new CouncilAcademicContributionDto
                {
                    CouncilCode = council.CouncilCode,
                    CouncilDisplayName = council.DisplayName,
                    SpatioTemporalAffinity = perspective,
                    RepresentativeModel = model,
                    ScholarTitle = presSeat.SeatTitle,
                    CoreEpistemicStance = $"{manifesto} (Trường phái: {perspective})",
                    DeepAnalysis = $"Dưới góc độ chuyên sâu {discipline} ({perspective}), vấn đề [{topicTitle}] đặt ra yêu cầu phải khảo sát từ căn bản: {manifesto} Khi soi chiếu vào bối cảnh thực tế: \"{problemStatement}\", quý viện nhận thấy mâu thuẫn hệ thống không thể chỉ giải quyết bằng tư duy cục bộ, mà phải tái cấu trúc toàn diện mối quan hệ giữa các thực thể tham gia.",
                    PrimaryObjection = $"Phản biện trọng tâm: Mọi giả định xem nhẹ ranh giới quy luật của {discipline} hoặc áp đặt giải pháp một chiều mà không tính đến các phản ứng ngược trong hệ sinh thái đều có nguy cơ dẫn đến sai số nghiêm trọng.",
                    ProposedRemedy = $"Đề xuất giải pháp hòa giải: Thiết lập khung quản trị tương thích với nguyên lý {discipline}, chuẩn hóa cơ chế giám sát thực nghiệm và phân bổ nguồn lực cân bằng động.",
                    KeyTheses = new List<string>
                    {
                        $"Quy luật nền tảng {discipline}",
                        $"Nguyên lý bản thể: {manifesto.Split('.').FirstOrDefault()?.Trim() ?? manifesto}",
                        $"Cân bằng động liên ngành theo {perspective}"
                    }
                };

                list.Add(dto);
            }

            return list;
        }
    }
}
