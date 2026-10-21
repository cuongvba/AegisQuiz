using System.Linq;
using System.Text.RegularExpressions;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Services.Docx
{
    /// <summary>
    /// Cầu nối bộ phân loại ↔ từ điển cấu trúc (OCP + Multi-language): mọi quyết định theo dạng câu hỏi
    /// và mọi mẫu ngôn ngữ đều được tra cứu từ <see cref="IDynamicQuestionIngestionEngine"/>, không nhúng trong mã.
    /// </summary>
    public static partial class DocxStateMachineClassifier
    {
        /// <summary>Dạng/phân đoạn có năng lực <paramref name="capability"/> theo hồ sơ cấu hình hay không.</summary>
        private static bool Has(IDynamicQuestionIngestionEngine cig, string? questionType, string capability)
            => cig.HasTypeCapability(questionType, capability);

        /// <summary>Khớp mẫu ngôn ngữ học theo khóa; trả về <see cref="Match.Empty"/> khi không khớp (an toàn dùng .Success / .Groups).</summary>
        private static Match LinguisticMatch(IDynamicQuestionIngestionEngine cig, string patternKey, string line)
            => cig.TryMatchLinguisticPattern(patternKey, line, out var match) ? match : Match.Empty;

        /// <summary>Gỡ các dòng HẾT / chỉ thị coi thi còn sót ở đuôi nội dung (dữ liệu đến từ từ điển, đa ngôn ngữ).</summary>
        private static string StripTrailingExamNoise(IDynamicQuestionIngestionEngine cig, string content)
        {
            if (string.IsNullOrEmpty(content)) return content;
            var lines = content.Replace("\r\n", "\n").Split('\n').ToList();
            while (lines.Count > 0)
            {
                var last = lines[^1];
                if (string.IsNullOrWhiteSpace(last) || cig.TryMatchEndOfExam(last) || cig.IsProctorDisclaimerLine(last))
                    lines.RemoveAt(lines.Count - 1);
                else break;
            }
            return string.Join("\n", lines).Trim();
        }
    }
}
