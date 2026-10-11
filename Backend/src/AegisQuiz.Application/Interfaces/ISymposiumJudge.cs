using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Grounded Symposium - Independent Arbiter & Blind Rubric Evaluator]
    /// Trọng tài Độc lập chấm mù Rubric nhận thức cho Đại Hội Thảo:
    /// - Không tham gia tranh luận trực tiếp nhằm đảm bảo tính khách quan tuyệt đối.
    /// - Đọc toàn văn biên bản tranh luận và Sổ cái Bằng chứng [E1..En].
    /// - Thẩm định 5 trục: Chặt chẽ logic (R1), Kiểm chứng thực nghiệm (R2), Chiều sâu bản thể (R3), Khả thi thực tiễn (R4), Kháng bẫy ngụy biện (R5).
    /// - Đo đạc tỷ lệ Grounding (tỷ lệ trích dẫn bằng chứng thực địa).
    /// </summary>
    public interface ISymposiumJudge
    {
        Task<SymposiumRubricScore> EvaluateSessionAsync(
            string topicTitle,
            string coreProblemStatement,
            EmpiricalEvidenceDossierDto? empiricalDossier,
            List<SymposiumDialogueTurn> dialogueLog,
            List<UniversalAiCouncil> participatingCouncils,
            Guid? userId = null,
            Guid? tenantId = null,
            string locale = "vi-VN",
            CancellationToken ct = default);
    }
}
