using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Transpiler
{
    /// <summary>
    /// [Moodle GIFT Universal Exporter Strategy]
    /// Chiến lược xuất bản câu hỏi sang định dạng GIFT (General Import Format Technology).
    /// Đây là chuẩn định dạng văn bản trực quan, nhẹ nhất và phổ biến hàng đầu thế giới cho Moodle, Canvas và TalentLMS.
    /// </summary>
    public class MoodleGiftExportStrategy : ITranspilerExportStrategy
    {
        public string FormatCode => "MOODLE_GIFT";
        public string DisplayName => "Moodle GIFT Plain Text Format";
        public string Description => "Định dạng text trực quan của Moodle. Dễ đọc, dễ sao chép, tương thích nhanh với Moodle, Canvas và TalentLMS.";
        public string TargetPlatforms => "Moodle LMS, Canvas LMS (via GIFT import), TalentLMS, Quizlet";
        public string MimeType => "text/plain";
        public string FileExtension => ".gift.txt";
        public bool SupportsSelfContainedMedia => false;
        public bool IsPackageZip => false;

        private class InternalAnswerItem
        {
            public string Key { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public bool IsCorrect { get; set; }
        }

        public byte[] Transpile(IReadOnlyList<DocxImportPreviewDto> questions, TranspileOptionsDto options)
        {
            if (questions == null || questions.Count == 0)
            {
                return Encoding.UTF8.GetBytes("// AegisQuiz GIFT Export: Không có câu hỏi nào.\n");
            }

            var sb = new StringBuilder();
            sb.AppendLine("// ================================================================");
            sb.AppendLine($"// AegisQuiz Universal Exam Transpiler — GIFT Standard");
            sb.AppendLine($"// Exam: {options.ExamTitle ?? "Quiz Exam"}");
            sb.AppendLine($"// Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("// ================================================================\n");

            string categoryName = !string.IsNullOrWhiteSpace(options.CategoryName) 
                ? options.CategoryName 
                : (!string.IsNullOrWhiteSpace(options.ExamTitle) ? options.ExamTitle : "AegisQuiz Transpiled");
            sb.AppendLine($"$CATEGORY: $course$/{EscapeGift(categoryName)}\n");

            int index = 1;
            foreach (var q in questions)
            {
                WriteGiftQuestion(sb, q, index++, options);
                sb.AppendLine();
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private void WriteGiftQuestion(StringBuilder sb, DocxImportPreviewDto q, int index, TranspileOptionsDto options)
        {
            string safeTitle = !string.IsNullOrWhiteSpace(q.TopicCode) ? $"[{q.TopicCode}] Câu {index}" : $"Câu {index}";
            sb.AppendLine($"::{EscapeGift(safeTitle)}::");

            string rawType = (q.QuestionType ?? "SINGLE").ToUpperInvariant();
            string content = EscapeGift(q.Content ?? string.Empty);

            switch (rawType)
            {
                case "TRUE_FALSE":
                    string ansRaw = q.SuggestedAnswer?.Trim().ToUpperInvariant() ?? string.Empty;
                    bool isTrue = (ansRaw == "TRUE" || ansRaw == "ĐÚNG" || ansRaw == "T" || ansRaw == "1");
                    sb.Append(content).Append(" {");
                    sb.Append(isTrue ? "TRUE" : "FALSE");
                    if (!string.IsNullOrWhiteSpace(q.AiExplanation))
                    {
                        sb.Append("#").Append(EscapeGift(q.AiExplanation));
                    }
                    sb.AppendLine("}");
                    break;

                case "MULTI":
                    sb.AppendLine(content + " {");
                    var mAnswers = ParseAnswers(q);
                    int correctCount = mAnswers.Count(a => a.IsCorrect);
                    double correctPct = correctCount > 0 ? Math.Round(100.0 / correctCount, 2) : 100.0;
                    int wrongCount = mAnswers.Count - correctCount;
                    double wrongPct = wrongCount > 0 ? -Math.Round(100.0 / wrongCount, 2) : 0.0;

                    foreach (var ans in mAnswers)
                    {
                        double pct = ans.IsCorrect ? correctPct : wrongPct;
                        sb.AppendLine($"  ~%{pct}%{EscapeGift(ans.Content)}");
                    }
                    sb.AppendLine("}");
                    break;

                case "MATCHING":
                    sb.AppendLine(content + " {");
                    var matchAnswers = ParseAnswers(q);
                    foreach (var ans in matchAnswers)
                    {
                        string left = ans.Content;
                        string right = "Đúng";
                        if (ans.Content.Contains("=>"))
                        {
                            var parts = ans.Content.Split(new[] { "=>" }, StringSplitOptions.RemoveEmptyEntries);
                            left = parts[0].Trim();
                            right = parts.Length > 1 ? parts[1].Trim() : "Đúng";
                        }
                        sb.AppendLine($"  ={EscapeGift(left)} -> {EscapeGift(right)}");
                    }
                    sb.AppendLine("}");
                    break;

                case "SHORT_ANSWER":
                case "FILL_BLANK":
                    string sAnswer = !string.IsNullOrWhiteSpace(q.SuggestedAnswer) ? q.SuggestedAnswer.Trim() : "Đáp án";
                    sb.Append(content).Append(" {=");
                    sb.Append(EscapeGift(sAnswer));
                    if (!string.IsNullOrWhiteSpace(q.AiExplanation))
                    {
                        sb.Append("#").Append(EscapeGift(q.AiExplanation));
                    }
                    sb.AppendLine("}");
                    break;

                case "ESSAY":
                    sb.Append(content).AppendLine(" {}");
                    break;

                case "MULTI_TRUE_FALSE":
                    sb.AppendLine(content);
                    sb.AppendLine("{");
                    var mtfAnswers = ParseAnswers(q);
                    char label = 'a';
                    int idx = 0;
                    foreach (var ans in mtfAnswers)
                    {
                        bool itemTrue = CheckItemIsTrue(q.SuggestedAnswer, idx++, ans.Content);
                        string state = itemTrue ? "Đúng" : "Sai";
                        sb.AppendLine($"  ={label}) {EscapeGift(ans.Content)} -> {state}");
                        label++;
                    }
                    sb.AppendLine("}");
                    break;

                case "SINGLE":
                default:
                    sb.AppendLine(content + " {");
                    var sAnswers = ParseAnswers(q);
                    foreach (var ans in sAnswers)
                    {
                        if (ans.IsCorrect)
                        {
                            sb.Append("  =").Append(EscapeGift(ans.Content));
                            if (!string.IsNullOrWhiteSpace(q.AiExplanation))
                            {
                                sb.Append("#").Append(EscapeGift(q.AiExplanation));
                            }
                            sb.AppendLine();
                        }
                        else
                        {
                            sb.AppendLine($"  ~{EscapeGift(ans.Content)}");
                        }
                    }
                    sb.AppendLine("}");
                    break;
            }
        }

        private static List<InternalAnswerItem> ParseAnswers(DocxImportPreviewDto q)
        {
            var list = new List<InternalAnswerItem>();
            if (q.Options == null || q.Options.Count == 0) return list;

            string suggested = q.SuggestedAnswer?.Trim().ToUpperInvariant() ?? string.Empty;
            char keyChar = 'A';
            int index = 1;

            foreach (var opt in q.Options)
            {
                string currentKey = keyChar.ToString();
                string cleanContent = Regex.Replace(opt, @"^(?:\[?[A-Ha-h0-9]\]?[\.\)\:]|\([A-Ha-h0-9]\))\s*", "").Trim();
                bool isCorrect = (suggested == currentKey || suggested == index.ToString() || (suggested.Length > 1 && suggested.Contains(currentKey)));

                list.Add(new InternalAnswerItem
                {
                    Key = currentKey,
                    Content = cleanContent,
                    IsCorrect = isCorrect
                });

                keyChar++;
                index++;
            }

            if (!list.Any(a => a.IsCorrect) && list.Count > 0)
            {
                list[0].IsCorrect = true;
            }

            return list;
        }

        private static bool CheckItemIsTrue(string? suggested, int index, string optionContent)
        {
            if (string.IsNullOrWhiteSpace(suggested)) return true;
            var parts = suggested.Split(new[] { ',', ';', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (index < parts.Length)
            {
                string p = parts[index].Trim().ToUpperInvariant();
                return p == "Đ" || p == "ĐÚNG" || p == "DUNG" || p == "T" || p == "TRUE" || p == "1";
            }
            return optionContent.ToUpperInvariant().Contains("ĐÚNG") || optionContent.ToUpperInvariant().Contains("TRUE");
        }

        private static string EscapeGift(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("\\", "\\\\")
                       .Replace("~", "\\~")
                       .Replace("=", "\\=")
                       .Replace("#", "\\#")
                       .Replace("{", "\\{")
                       .Replace("}", "\\}")
                       .Replace(":", "\\:");
        }
    }
}
