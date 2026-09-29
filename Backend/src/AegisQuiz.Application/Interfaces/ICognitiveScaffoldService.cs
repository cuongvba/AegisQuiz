using System;
using System.Threading;
using System.Threading.Tasks;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// Bậc thang gợi ý theo thuyết Vùng phát triển gần (Zone of Proximal Development - ZPD).
    /// </summary>
    public enum ScaffoldLevel
    {
        /// <summary>Bậc 1: Mỏ neo Khái niệm & Công thức nền tảng</summary>
        ConceptAnchor = 1,
        /// <summary>Bậc 2: Định hướng Chiến lược & Phương pháp tiếp cận</summary>
        MethodStrategy = 2,
        /// <summary>Bậc 3: Hướng dẫn Từng bước giải quyết (Step-by-step)</summary>
        CalculationStep = 3,
        /// <summary>Bậc 4: Mổ xẻ Chi tiết Socratic & Bẫy tư duy thường gặp</summary>
        FullWalkthrough = 4
    }

    public class ScaffoldHintDto
    {
        public Guid QuestionId { get; set; }
        public ScaffoldLevel Level { get; set; }
        public string LevelTitle { get; set; } = string.Empty;
        public string HintContent { get; set; } = string.Empty; // Chứa KaTeX LaTeX chuẩn $...$
        public bool HasNextLevel { get; set; }
        public string? SocraticQuestion { get; set; } // Câu hỏi kích thích tư duy tự suy luận
    }

    /// <summary>
    /// [Epoch 2: Intelligent Feedback Scaffold]
    /// Dịch vụ gợi ý nhận thức đa bậc Socratic, không làm mất đi "nỗ lực nhận thức tích cực" của học viên.
    /// </summary>
    public interface ICognitiveScaffoldService
    {
        Task<ScaffoldHintDto> GetScaffoldHintAsync(
            Guid questionId,
            ScaffoldLevel level,
            string? studentAnswer = null,
            CancellationToken ct = default);
    }
}
