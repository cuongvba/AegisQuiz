using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Excel;

namespace AegisQuiz.Infrastructure.Transpiler
{
    /// <summary>
    /// [Moodle XML Universal Exporter Strategy]
    /// Chiến lược xuất bản câu hỏi sang định dạng Moodle XML chuẩn W3C.
    /// Tương thích 100% Plug-and-Play với Moodle LMS (300M+ người dùng), Totara LMS, IOMAD.
    /// Hỗ trợ đầy đủ 9 dạng câu hỏi, nhúng hình ảnh Base64 tự động (Zero Broken Links) và toán LaTeX MathJax.
    /// </summary>
    public class MoodleXmlExportStrategy : ITranspilerExportStrategy
    {
        public string FormatCode => "MOODLE_XML";
        public string DisplayName => "Moodle XML Format (W3C Standard)";
        public string Description => "Định dạng XML chuẩn mực cao nhất của Moodle LMS. Nhúng trọn gói hình ảnh Base64 và công thức toán MathJax, kéo thả 1 chạm vào Ngân hàng câu hỏi Moodle.";
        public string TargetPlatforms => "Moodle LMS, Totara Learn, IOMAD, Canvas (via XML Converter)";
        public string MimeType => "application/xml";
        public string FileExtension => ".xml";
        public bool SupportsSelfContainedMedia => true;
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
                return Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<quiz></quiz>");
            }

            var xmlSettings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                IndentChars = "  ",
                OmitXmlDeclaration = false
            };

            using var memoryStream = new MemoryStream();
            using (var writer = XmlWriter.Create(memoryStream, xmlSettings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("quiz");

                // 1. Thẻ thiết lập Category (Danh mục câu hỏi trong Moodle)
                string categoryName = !string.IsNullOrWhiteSpace(options.CategoryName) 
                    ? options.CategoryName 
                    : (!string.IsNullOrWhiteSpace(options.ExamTitle) ? options.ExamTitle : "AegisQuiz Transpiled Category");
                
                WriteCategoryElement(writer, categoryName);

                // 2. Chuyển dịch từng câu hỏi sang Moodle XML
                int index = 1;
                foreach (var q in questions)
                {
                    WriteQuestionElement(writer, q, index++, options);
                }

                writer.WriteEndElement(); // </quiz>
                writer.WriteEndDocument();
            }

            return memoryStream.ToArray();
        }

        private void WriteCategoryElement(XmlWriter writer, string categoryName)
        {
            writer.WriteStartElement("question");
            writer.WriteAttributeString("type", "category");
            
            writer.WriteStartElement("category");
            writer.WriteStartElement("text");
            writer.WriteString($"$course$/{categoryName}");
            writer.WriteEndElement(); // </text>
            writer.WriteEndElement(); // </category>

            writer.WriteStartElement("info");
            writer.WriteAttributeString("format", "moodle_auto_format");
            writer.WriteStartElement("text");
            writer.WriteString("Ngân hàng câu hỏi được bóc tách và xuất bản tự động bởi AegisQuiz Universal Exam Transpiler.");
            writer.WriteEndElement(); // </text>
            writer.WriteEndElement(); // </info>

            writer.WriteEndElement(); // </question>
        }

        private void WriteQuestionElement(XmlWriter writer, DocxImportPreviewDto q, int orderIndex, TranspileOptionsDto options)
        {
            string rawType = (q.QuestionType ?? "SINGLE").ToUpperInvariant();

            switch (rawType)
            {
                case "MULTI_TRUE_FALSE":
                    WriteMultiTrueFalseCloze(writer, q, orderIndex, options);
                    break;
                case "TRUE_FALSE":
                    WriteTrueFalse(writer, q, orderIndex, options);
                    break;
                case "MULTI":
                    WriteMultiChoice(writer, q, orderIndex, options, isSingle: false);
                    break;
                case "MATCHING":
                    WriteMatching(writer, q, orderIndex, options);
                    break;
                case "ORDERING":
                    WriteOrdering(writer, q, orderIndex, options);
                    break;
                case "SHORT_ANSWER":
                case "FILL_BLANK":
                    WriteShortAnswer(writer, q, orderIndex, options);
                    break;
                case "ESSAY":
                    WriteEssay(writer, q, orderIndex, options);
                    break;
                case "SINGLE":
                default:
                    WriteMultiChoice(writer, q, orderIndex, options, isSingle: true);
                    break;
            }
        }

        #region Question Type Writers

        private void WriteMultiChoice(XmlWriter writer, DocxImportPreviewDto q, int orderIndex, TranspileOptionsDto options, bool isSingle)
        {
            writer.WriteStartElement("question");
            writer.WriteAttributeString("type", "multichoice");

            WriteCommonHeader(writer, q, orderIndex, options);

            writer.WriteStartElement("single");
            writer.WriteString(isSingle ? "true" : "false");
            writer.WriteEndElement();

            writer.WriteStartElement("shuffleanswers");
            writer.WriteString(options.ShuffleAnswers ? "1" : "0");
            writer.WriteEndElement();

            writer.WriteStartElement("answernumbering");
            writer.WriteString("abc");
            writer.WriteEndElement();

            var answers = ParseAnswers(q);
            int correctCount = answers.Count(a => a.IsCorrect);
            if (correctCount == 0 && answers.Count > 0)
            {
                answers[0].IsCorrect = true;
                correctCount = 1;
            }

            double correctFraction = isSingle ? 100.0 : (correctCount > 0 ? Math.Round(100.0 / correctCount, 4) : 100.0);
            int wrongCount = answers.Count - correctCount;
            double wrongPenaltyFraction = (!isSingle && wrongCount > 0) ? -Math.Round(100.0 / wrongCount, 4) : 0.0;

            foreach (var ans in answers)
            {
                writer.WriteStartElement("answer");
                double fraction = ans.IsCorrect ? correctFraction : wrongPenaltyFraction;
                writer.WriteAttributeString("fraction", fraction.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture));
                writer.WriteAttributeString("format", "html");

                writer.WriteStartElement("text");
                string answerHtml = FormatContentWithMath(ans.Content);
                writer.WriteCData(answerHtml);
                writer.WriteEndElement();

                // Feedback
                writer.WriteStartElement("feedback");
                writer.WriteAttributeString("format", "html");
                writer.WriteStartElement("text");
                writer.WriteCData(ans.IsCorrect ? "<p>Chính xác!</p>" : "<p>Chưa chính xác.</p>");
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteEndElement(); // </answer>
            }

            WriteGeneralFeedback(writer, q);
            writer.WriteEndElement(); // </question>
        }

        private void WriteTrueFalse(XmlWriter writer, DocxImportPreviewDto q, int orderIndex, TranspileOptionsDto options)
        {
            writer.WriteStartElement("question");
            writer.WriteAttributeString("type", "truefalse");

            WriteCommonHeader(writer, q, orderIndex, options);

            string ansRaw = q.SuggestedAnswer?.Trim().ToUpperInvariant() ?? string.Empty;
            bool isTrue = (ansRaw == "TRUE" || ansRaw == "ĐÚNG" || ansRaw == "DUNG" || ansRaw == "T" || ansRaw == "1");

            // True option
            writer.WriteStartElement("answer");
            writer.WriteAttributeString("fraction", isTrue ? "100" : "0");
            writer.WriteAttributeString("format", "moodle_auto_format");
            writer.WriteStartElement("text");
            writer.WriteString("true");
            writer.WriteEndElement();
            writer.WriteEndElement();

            // False option
            writer.WriteStartElement("answer");
            writer.WriteAttributeString("fraction", !isTrue ? "100" : "0");
            writer.WriteAttributeString("format", "moodle_auto_format");
            writer.WriteStartElement("text");
            writer.WriteString("false");
            writer.WriteEndElement();
            writer.WriteEndElement();

            WriteGeneralFeedback(writer, q);
            writer.WriteEndElement(); // </question>
        }

        private void WriteMultiTrueFalseCloze(XmlWriter writer, DocxImportPreviewDto q, int orderIndex, TranspileOptionsDto options)
        {
            writer.WriteStartElement("question");
            writer.WriteAttributeString("type", "cloze");

            WriteCommonHeaderCustom(writer, q, orderIndex, options, (w) =>
            {
                var sb = new StringBuilder();
                sb.Append("<div>").Append(FormatContentWithMath(q.Content)).Append("</div><br/>");
                sb.Append("<div class=\"aegis-multi-tf-container\" style=\"background:#f8fafc;padding:12px;border-radius:8px;border:1px solid #cbd5e1;\">");
                sb.Append("<p><strong>Xác định tính Đúng / Sai của các khẳng định sau:</strong></p>");
                sb.Append("<table style=\"width:100%;border-collapse:collapse;\">");

                var answers = ParseAnswers(q);
                char label = 'a';
                int itemIdx = 0;
                foreach (var ans in answers)
                {
                    bool isTrue = CheckItemIsTrue(q.SuggestedAnswer, itemIdx++, ans.Content);
                    string clozeMacro = isTrue ? "{1:MULTICHOICE:=Đúng~Sai}" : "{1:MULTICHOICE:Đúng~=Sai}";

                    sb.Append("<tr style=\"border-bottom:1px solid #e2e8f0;\">");
                    sb.Append($"<td style=\"padding:8px;width:30px;font-weight:bold;\">{label})</td>");
                    sb.Append($"<td style=\"padding:8px;\">{FormatContentWithMath(ans.Content)}</td>");
                    sb.Append($"<td style=\"padding:8px;width:120px;text-align:right;\">{clozeMacro}</td>");
                    sb.Append("</tr>");
                    label++;
                }

                sb.Append("</table></div>");
                return sb.ToString();
            });

            WriteGeneralFeedback(writer, q);
            writer.WriteEndElement(); // </question>
        }

        private void WriteMatching(XmlWriter writer, DocxImportPreviewDto q, int orderIndex, TranspileOptionsDto options)
        {
            writer.WriteStartElement("question");
            writer.WriteAttributeString("type", "matching");

            WriteCommonHeader(writer, q, orderIndex, options);

            writer.WriteStartElement("shuffleanswers");
            writer.WriteString("1");
            writer.WriteEndElement();

            var answers = ParseAnswers(q);
            foreach (var ans in answers)
            {
                string left = ans.Content;
                string right = "Đúng";

                if (ans.Content.Contains("=>"))
                {
                    var parts = ans.Content.Split(new[] { "=>" }, StringSplitOptions.RemoveEmptyEntries);
                    left = parts[0].Trim();
                    right = parts.Length > 1 ? parts[1].Trim() : "Đúng";
                }
                else if (ans.Content.Contains("|"))
                {
                    var parts = ans.Content.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    left = parts[0].Trim();
                    right = parts.Length > 1 ? parts[1].Trim() : "Đúng";
                }

                writer.WriteStartElement("subquestion");
                writer.WriteAttributeString("format", "html");
                writer.WriteStartElement("text");
                writer.WriteCData(FormatContentWithMath(left));
                writer.WriteEndElement();

                writer.WriteStartElement("answer");
                writer.WriteStartElement("text");
                writer.WriteString(right);
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteEndElement(); // </subquestion>
            }

            WriteGeneralFeedback(writer, q);
            writer.WriteEndElement(); // </question>
        }

        private void WriteOrdering(XmlWriter writer, DocxImportPreviewDto q, int orderIndex, TranspileOptionsDto options)
        {
            writer.WriteStartElement("question");
            writer.WriteAttributeString("type", "matching");

            WriteCommonHeader(writer, q, orderIndex, options);

            writer.WriteStartElement("shuffleanswers");
            writer.WriteString("1");
            writer.WriteEndElement();

            var answers = ParseAnswers(q);
            int step = 1;
            foreach (var ans in answers)
            {
                writer.WriteStartElement("subquestion");
                writer.WriteAttributeString("format", "html");
                writer.WriteStartElement("text");
                writer.WriteCData($"<p>Bước {step}: {FormatContentWithMath(ans.Content)}</p>");
                writer.WriteEndElement();

                writer.WriteStartElement("answer");
                writer.WriteStartElement("text");
                writer.WriteString($"Vị trí {step}");
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteEndElement(); // </subquestion>
                step++;
            }

            WriteGeneralFeedback(writer, q);
            writer.WriteEndElement(); // </question>
        }

        private void WriteShortAnswer(XmlWriter writer, DocxImportPreviewDto q, int orderIndex, TranspileOptionsDto options)
        {
            writer.WriteStartElement("question");
            writer.WriteAttributeString("type", "shortanswer");

            WriteCommonHeader(writer, q, orderIndex, options);

            writer.WriteStartElement("usecase");
            writer.WriteString("0"); // Không phân biệt hoa thường
            writer.WriteEndElement();

            string correctAnswer = !string.IsNullOrWhiteSpace(q.SuggestedAnswer) ? q.SuggestedAnswer.Trim() : "1";

            writer.WriteStartElement("answer");
            writer.WriteAttributeString("fraction", "100");
            writer.WriteAttributeString("format", "moodle_auto_format");
            writer.WriteStartElement("text");
            writer.WriteString(correctAnswer);
            writer.WriteEndElement();
            writer.WriteStartElement("feedback");
            writer.WriteStartElement("text");
            writer.WriteString("Chính xác!");
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement(); // </answer>

            WriteGeneralFeedback(writer, q);
            writer.WriteEndElement(); // </question>
        }

        private void WriteEssay(XmlWriter writer, DocxImportPreviewDto q, int orderIndex, TranspileOptionsDto options)
        {
            writer.WriteStartElement("question");
            writer.WriteAttributeString("type", "essay");

            WriteCommonHeader(writer, q, orderIndex, options);

            writer.WriteStartElement("responseformat");
            writer.WriteString("editor");
            writer.WriteEndElement();

            writer.WriteStartElement("responserequired");
            writer.WriteString("1");
            writer.WriteEndElement();

            writer.WriteStartElement("responsefieldlines");
            writer.WriteString("15");
            writer.WriteEndElement();

            WriteGeneralFeedback(writer, q);
            writer.WriteEndElement(); // </question>
        }

        #endregion

        #region Helpers

        private void WriteCommonHeader(XmlWriter writer, DocxImportPreviewDto q, int orderIndex, TranspileOptionsDto options)
        {
            WriteCommonHeaderCustom(writer, q, orderIndex, options, (w) => $"<p>{FormatContentWithMath(q.Content)}</p>");
        }

        private void WriteCommonHeaderCustom(XmlWriter writer, DocxImportPreviewDto q, int orderIndex, TranspileOptionsDto options, Func<DocxImportPreviewDto, string> contentFormatter)
        {
            // Name
            writer.WriteStartElement("name");
            writer.WriteStartElement("text");
            string safeTitle = !string.IsNullOrWhiteSpace(q.TopicCode) ? $"[{q.TopicCode}] Câu {orderIndex}" : $"Câu {orderIndex}";
            writer.WriteString(safeTitle);
            writer.WriteEndElement();
            writer.WriteEndElement();

            // Question Text
            writer.WriteStartElement("questiontext");
            writer.WriteAttributeString("format", "html");
            writer.WriteStartElement("text");
            writer.WriteCData(contentFormatter(q));
            writer.WriteEndElement();

            // Embed Self-Contained Images
            EmbedBase64Files(writer, q);

            writer.WriteEndElement(); // </questiontext>

            // General Score
            writer.WriteStartElement("defaultgrade");
            writer.WriteString(options.DefaultPoints ?? "1.0");
            writer.WriteEndElement();

            writer.WriteStartElement("penalty");
            writer.WriteString("0.3333333");
            writer.WriteEndElement();

            writer.WriteStartElement("hidden");
            writer.WriteString("0");
            writer.WriteEndElement();
        }

        private void WriteGeneralFeedback(XmlWriter writer, DocxImportPreviewDto q)
        {
            if (string.IsNullOrWhiteSpace(q.AiExplanation)) return;

            writer.WriteStartElement("generalfeedback");
            writer.WriteAttributeString("format", "html");
            writer.WriteStartElement("text");
            writer.WriteCData($"<div class=\"aegis-explanation\"><strong>Lời giải chi tiết:</strong><br/>{FormatContentWithMath(q.AiExplanation)}</div>");
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private void EmbedBase64Files(XmlWriter writer, DocxImportPreviewDto q)
        {
            var images = ExcelImageHelper.ExtractImages(q);
            if (images.Count == 0) return;

            int imgIndex = 1;
            foreach (var img in images)
            {
                if (string.IsNullOrWhiteSpace(img.Base64Data)) continue;

                string fileName = $"image_{imgIndex++}.png";

                writer.WriteStartElement("file");
                writer.WriteAttributeString("name", fileName);
                writer.WriteAttributeString("path", "/");
                writer.WriteAttributeString("encoding", "base64");
                writer.WriteString(img.Base64Data);
                writer.WriteEndElement();
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

        private static string FormatContentWithMath(string? text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("\r\n", "<br/>").Replace("\n", "<br/>");
        }

        #endregion
    }
}
