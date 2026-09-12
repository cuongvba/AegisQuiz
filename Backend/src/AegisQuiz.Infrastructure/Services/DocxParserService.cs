using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [World-Class Enterprise Docx Parser]
    /// Trình bóc tách tệp Word hỗ trợ toàn diện:
    /// 1. Bóc tách câu hỏi & phương án dạng Text thông thường.
    /// 2. Bóc tách công thức toán học MathType (WMF vector) và tự động chuyển đổi sang PNG Base64 trong bộ nhớ (0ms, 100% offline).
    /// 3. Bóc tách hình vẽ minh họa, đồ thị, hình học không gian (JPG, PNG) nhúng trực tiếp vào câu hỏi.
    /// 4. Hỗ trợ chuẩn đề thi Bộ GD&ĐT 2025: Phần I (Trắc nghiệm), Phần II (Đúng/Sai), Phần III (Trả lời ngắn).
    /// </summary>
    public class DocxParserService : IDocxParserService
    {
        private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        public List<DocxImportPreviewDto> ParseDocxFile(Stream fileStream)
        {
            return ParseRichDocxStream(fileStream);
        }

        public string ExtractTextFromDocx(Stream fileStream)
        {
            var rawParagraphs = ExtractRawParagraphsFromDocx(fileStream);
            return string.Join("\n", rawParagraphs.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        public static List<DocxImportPreviewDto> ParseParagraphs(IEnumerable<string> paragraphs)
        {
            return ParseParagraphList(paragraphs.ToList());
        }

        /// <summary>
        /// Phân tích tệp DOCX toàn diện kèm trích xuất đồ thị, hình học và công thức toán học MathType
        /// </summary>
        public static List<DocxImportPreviewDto> ParseRichDocxStream(Stream fileStream)
        {
            var results = new List<DocxImportPreviewDto>();

            try
            {
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read, true);

                // 1. Tải bản đồ quan hệ (rId -> Media path)
                var relsMap = LoadRelationships(archive);

                // 2. Tải toàn bộ media nhúng trong word/media/
                var mediaDict = LoadMediaFiles(archive);

                // 3. Tải document.xml và duyệt các đoạn văn
                var documentEntry = archive.GetEntry("word/document.xml");
                if (documentEntry == null) return results;

                using var entryStream = documentEntry.Open();
                var doc = XDocument.Load(entryStream);

                // 4. Bóc tách bảng đáp án tổng hợp (Bảng Đáp án Phần I, II, III) nếu có
                var answerKeyMap = ExtractAnswerTables(doc);

                // 5. Bóc tách các đoạn văn kèm hình ảnh và công thức MathType chuyển thành PNG Base64
                var paragraphs = ExtractParagraphTokens(doc, relsMap, mediaDict);
                return ParseParagraphList(paragraphs, answerKeyMap);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DocxParserService] Lỗi bóc tách DOCX: {ex.Message}");
            }

            return results;
        }

        private static List<DocxImportPreviewDto> ParseParagraphList(
            List<string> paragraphs,
            Dictionary<string, Dictionary<string, Dictionary<int, string>>>? answerKeyMap = null)
        {
            var results = new List<DocxImportPreviewDto>();
            var questionMap = new Dictionary<string, DocxImportPreviewDto>(StringComparer.OrdinalIgnoreCase);

            DocxImportPreviewDto? currentQuestion = null;
            int currentQuestionNumber = 0;
            string currentSection = "SINGLE"; // SINGLE, TRUE_FALSE, SHORT_ANSWER, ESSAY
            string currentExamCode = "DEFAULT";
            string currentTopicCode = "GENERAL";
            bool inExplanation = false;
            string? currentReadingPassage = null;
            int passageStartQ = 0;
            int passageEndQ = int.MaxValue;
            bool isSolutionPhase = false;
            var examQuestionsByNum = new Dictionary<int, DocxImportPreviewDto>();

            foreach (var rawLine in paragraphs)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // 0. Nhận dạng Tiêu đề Đề thi: "ĐỀ 1", "ĐỀ 2", "ĐỀ SỐ 01", "ĐỀ ON THI..."
                var matchExamHeading = Regex.Match(line, @"^(?:ĐỀ|D[E|Ê])\s*(?:SỐ\s*|THI\s*|ÔN\s*THI\s*)?(\d+)\b", RegexOptions.IgnoreCase);
                if (matchExamHeading.Success)
                {
                    if (currentQuestion != null && !isSolutionPhase)
                    {
                        FinalizeAndMergeQuestion(results, questionMap, currentQuestion, currentExamCode, currentQuestionNumber);
                        examQuestionsByNum[currentQuestionNumber] = currentQuestion;
                        currentQuestion = null;
                    }

                    int examNum = int.Parse(matchExamHeading.Groups[1].Value);
                    currentExamCode = $"TOAN_DE_{examNum:D2}";
                    currentTopicCode = currentExamCode;
                    currentSection = "SINGLE";
                    inExplanation = false;
                    currentReadingPassage = null;
                    passageStartQ = 0;
                    passageEndQ = int.MaxValue;
                    if (!isSolutionPhase)
                    {
                        examQuestionsByNum.Clear();
                    }
                    currentQuestion = null;
                    continue;
                }

                // 0b. Nhận dạng Tiêu đề Phần Bảng Đáp án hoặc Lời giải tham khảo
                var matchSolPhase = Regex.Match(line, @"^(?:LỜI\s*GIẢI|L[\.\?\s]*I\s*GI[\.\?\s]*I|HƯỚNG\s*DẪN\s*GIẢI|HU[\.\?\s]*NG\s*D[\.\?\s]*N\s*GI[\.\?\s]*I)\s*(?:THAM\s*KHẢO|CHI\s*TIẾT)?[\.\:\s\-]*$", RegexOptions.IgnoreCase);
                var matchAnsTableHeading = Regex.Match(line, @"^(?:ĐÁP\s*ÁN|D[\.\?\s]*P\s*[\.\?\s]*N)\s*(?:THAM\s*KHẢO|CHI\s*TIẾT)?[\.\:\s\-]*$", RegexOptions.IgnoreCase);
                if (matchSolPhase.Success || matchAnsTableHeading.Success)
                {
                    if (currentQuestion != null && !isSolutionPhase)
                    {
                        FinalizeAndMergeQuestion(results, questionMap, currentQuestion, currentExamCode, currentQuestionNumber);
                        examQuestionsByNum[currentQuestionNumber] = currentQuestion;
                        currentQuestion = null;
                    }
                    if (matchSolPhase.Success)
                    {
                        isSolutionPhase = true;
                    }
                    inExplanation = false;
                    currentReadingPassage = null;
                    passageStartQ = 0;
                    passageEndQ = 0;
                    continue;
                }

                // 1. Nhận dạng thay đổi Chủ đề: "Chủ đề: Toán học", "Môn: Toán", "Môn: Tiếng Anh"
                var matchTopicHeading = Regex.Match(line, @"^(?:Chủ\s*đề|Chương|Lĩnh\s*vực|Môn\s*thi|Môn|Học\s*phần|Mã\s*CĐ)[\.\:\s\-]+([^\n\r]+)", RegexOptions.IgnoreCase);
                if (matchTopicHeading.Success)
                {
                    var rawTopic = matchTopicHeading.Groups[1].Value.Trim();
                    currentTopicCode = ResolveTopicCode(rawTopic);
                    continue;
                }

                var upper = line.ToUpperInvariant();
                bool isQuestionLine = Regex.IsMatch(line, @"^(?:Câu|Question|Item|Task|Exercise|Q)\s*\d+", RegexOptions.IgnoreCase);
                bool isExplanationLine = Regex.IsMatch(line, @"^(?:Lời\s*giải|L[\.\?\s]*i\s*gi[\.\?\s]*i|Hướng\s*dẫn|HD|Giải\s*thích|Explanation|Căn\s*cứ)\b", RegexOptions.IgnoreCase);

                // 2. Nhận dạng Tiêu đề Đoạn văn Đọc hiểu (Reading Comprehension Passage / Advertisement / Leaflet)
                var matchPassageHeader = Regex.Match(line, @"^(?:Read\s+(?:the\s+)?(?:following\s+)?(?:passage|advertisement|leaflet|text|article|announcement)|Đọc\s+(?:đoạn\s+văn|bài\s+đọc|thông\s+báo|tờ\s+rơi)\s+sau)[\s\S]*", RegexOptions.IgnoreCase);
                if (matchPassageHeader.Success)
                {
                    if (currentQuestion != null && !isSolutionPhase)
                    {
                        FinalizeAndMergeQuestion(results, questionMap, currentQuestion, currentExamCode, currentQuestionNumber);
                        examQuestionsByNum[currentQuestionNumber] = currentQuestion;
                        currentQuestion = null;
                    }
                    currentReadingPassage = line;
                    inExplanation = false;
                    var matchRange = Regex.Match(line, @"\b(?:from|từ)\s*(\d+)\s*(?:to|đến)\s*(\d+)\b", RegexOptions.IgnoreCase);
                    if (matchRange.Success)
                    {
                        passageStartQ = int.Parse(matchRange.Groups[1].Value);
                        passageEndQ = int.Parse(matchRange.Groups[2].Value);
                    }
                    else
                    {
                        passageStartQ = 0;
                        passageEndQ = int.MaxValue;
                    }
                    if (currentTopicCode == "GENERAL") currentTopicCode = "TIENG_ANH";
                    continue;
                }

                // 2b. Nhận dạng Section Headers môn Ngoại ngữ (Tiếng Anh, TOEIC, IELTS, THPT):
                if (!isQuestionLine && !isExplanationLine && (upper.Contains("PRONUNCIATION") || upper.Contains("PHONETICS") || upper.Contains("PHÁT ÂM") || upper.Contains("NGỮ ÂM")
                    || upper.Contains("STRESS") || upper.Contains("TRỌNG ÂM")
                    || upper.Contains("CLOZE TEST") || upper.Contains("ERROR IDENTIFICATION") || upper.Contains("TÌM LỖI SAI")
                    || upper.Contains("SENTENCE TRANSFORMATION") || upper.Contains("VIẾT LẠI CÂU")
                    || upper.Contains("MARK THE LETTER A, B, C, OR D")
                    || upper.Contains("ARRANGEMENT OF UTTERANCES") || upper.Contains("SẮP XẾP CÂU")))
                {
                    if (currentQuestion != null && !isSolutionPhase)
                    {
                        FinalizeAndMergeQuestion(results, questionMap, currentQuestion, currentExamCode, currentQuestionNumber);
                        examQuestionsByNum[currentQuestionNumber] = currentQuestion;
                        currentQuestion = null;
                    }
                    currentSection = "SINGLE";
                    inExplanation = false;
                    currentReadingPassage = null;
                    passageStartQ = 0;
                    passageEndQ = 0;
                    if (currentTopicCode == "GENERAL") currentTopicCode = "TIENG_ANH";
                    continue;
                }

                // 2c. Nhận dạng Section Headers theo chuẩn Bộ GD&ĐT (Kiểm tra III, II trước I để tránh prefix La Mã)
                if (Regex.IsMatch(upper, @"\b(?:PHẦN|PHAN)\s+III\b") || upper.Contains("TRẢ LỜI NGẮN") || upper.Contains("TRA LOI NGAN"))
                {
                    if (currentQuestion != null && !isSolutionPhase)
                    {
                        FinalizeAndMergeQuestion(results, questionMap, currentQuestion, currentExamCode, currentQuestionNumber);
                        examQuestionsByNum[currentQuestionNumber] = currentQuestion;
                        currentQuestion = null;
                    }
                    currentSection = "SHORT_ANSWER";
                    inExplanation = false;
                    currentReadingPassage = null;
                    passageStartQ = 0;
                    passageEndQ = 0;
                    continue;
                }
                if (Regex.IsMatch(upper, @"\b(?:PHẦN|PHAN)\s+II\b") || upper.Contains("ĐÚNG SAI") || upper.Contains("DUNG SAI"))
                {
                    if (currentQuestion != null && !isSolutionPhase)
                    {
                        FinalizeAndMergeQuestion(results, questionMap, currentQuestion, currentExamCode, currentQuestionNumber);
                        examQuestionsByNum[currentQuestionNumber] = currentQuestion;
                        currentQuestion = null;
                    }
                    currentSection = "TRUE_FALSE";
                    inExplanation = false;
                    currentReadingPassage = null;
                    passageStartQ = 0;
                    passageEndQ = 0;
                    continue;
                }
                if (Regex.IsMatch(upper, @"\b(?:PHẦN|PHAN)\s+I\b") || (upper.Contains("TRẮC NGHIỆM") && !upper.Contains("ĐÚNG SAI") && !upper.Contains("DUNG SAI") && !upper.Contains("TRẢ LỜI NGẮN") && !upper.Contains("TRA LOI NGAN")))
                {
                    if (currentQuestion != null && !isSolutionPhase)
                    {
                        FinalizeAndMergeQuestion(results, questionMap, currentQuestion, currentExamCode, currentQuestionNumber);
                        examQuestionsByNum[currentQuestionNumber] = currentQuestion;
                        currentQuestion = null;
                    }
                    currentSection = "SINGLE";
                    inExplanation = false;
                    currentReadingPassage = null;
                    passageStartQ = 0;
                    passageEndQ = 0;
                    continue;
                }
                if (upper.Contains("TỰ LUẬN") || upper.Contains("TU LUAN") || upper.Contains("XỬ LÝ TÌNH HUỐNG") || upper.Contains("TINH HUONG"))
                {
                    if (currentQuestion != null && !isSolutionPhase)
                    {
                        FinalizeAndMergeQuestion(results, questionMap, currentQuestion, currentExamCode, currentQuestionNumber);
                        examQuestionsByNum[currentQuestionNumber] = currentQuestion;
                        currentQuestion = null;
                    }
                    currentSection = "ESSAY";
                    inExplanation = false;
                    currentReadingPassage = null;
                    passageStartQ = 0;
                    passageEndQ = 0;
                    continue;
                }

                // 2d. Đang thu thập các đoạn văn tiếp theo của bài đọc hiểu khi chưa vào câu hỏi con
                if (!string.IsNullOrEmpty(currentReadingPassage) && currentQuestion == null
                    && !Regex.IsMatch(line, @"^(?:Câu|Question|Item|Task|Exercise|Q)\s*\d+", RegexOptions.IgnoreCase))
                {
                    currentReadingPassage += "\n\n" + line;
                    continue;
                }

                // 3. Nhận dạng câu hỏi mới: Hỗ trợ cả tiếng Việt, tiếng Anh và Dynamic CIG Rules
                var cleanLine = Regex.Replace(line, @"^<[a-z0-9]+>", "", RegexOptions.IgnoreCase).Trim();
                bool isDynamicAnchor = DynamicQuestionIngestionEngine.Instance.TryMatchQuestionAnchor(cleanLine, out int dynQNum, out var matchedRule);
                var matchQuestion = Regex.Match(cleanLine, @"^(?:Câu|Question|Item|Task|Exercise|Q)\s*(\d+)[\.\:\s\-]+(.*)", RegexOptions.IgnoreCase);
                if (matchQuestion.Success || isDynamicAnchor)
                {
                    int qNum = matchQuestion.Success ? int.Parse(matchQuestion.Groups[1].Value) : dynQNum;
                    string remainderText = "";
                    if (matchQuestion.Success)
                    {
                        remainderText = matchQuestion.Groups[2].Value.Trim();
                    }
                    else if (matchedRule != null)
                    {
                        var dynRegex = new Regex(matchedRule.AnchorSpec.RegexPattern, RegexOptions.IgnoreCase);
                        var mDyn = dynRegex.Match(cleanLine);
                        remainderText = mDyn.Success ? cleanLine.Substring(mDyn.Length).Trim() : cleanLine;
                    }

                    // Nếu đang trong giai đoạn Lời giải tham khảo/chi tiết: gắn trực tiếp vào câu hỏi đã bóc tách
                    if (isSolutionPhase)
                    {
                        string keyWithSection = $"{currentExamCode}_{currentSection}_{qNum}";
                        string keySingle = $"{currentExamCode}_SINGLE_{qNum}";
                        DocxImportPreviewDto? existingQ = null;
                        if (questionMap.TryGetValue(keyWithSection, out existingQ) ||
                            questionMap.TryGetValue(keySingle, out existingQ) ||
                            examQuestionsByNum.TryGetValue(qNum, out existingQ))
                        {
                            currentQuestion = existingQ;
                            inExplanation = true;
                            var remainder = remainderText;
                            if (!string.IsNullOrEmpty(remainder))
                            {
                                AppendExplanationLine(currentQuestion, remainder);
                                TryExtractAnswer(currentQuestion, remainder);
                            }
                        }
                        else
                        {
                            currentQuestion = null;
                        }
                        continue;
                    }

                    if (currentQuestion != null)
                    {
                        FinalizeAndMergeQuestion(results, questionMap, currentQuestion, currentExamCode, currentQuestionNumber);
                        examQuestionsByNum[currentQuestionNumber] = currentQuestion;
                    }

                    currentQuestionNumber = qNum;
                    inExplanation = false;

                    var contentText = remainderText;
                    var sameLineOptions = ExtractOptionsFromLine(contentText);
                    string questionBody = contentText;

                    if (sameLineOptions.Count > 0)
                    {
                        var firstOptRegex = new Regex(@"(?:^|[\s\t\.\,\;\–\—]+|(?<=[a-z0-9\)]))(\*?)" + sameLineOptions[0].Letter + @"(\*?)[\.\:\)]+\s*");
                        var mFirst = firstOptRegex.Match(contentText);
                        if (mFirst.Success)
                        {
                            questionBody = contentText.Substring(0, mFirst.Index).Trim();
                        }
                        else
                        {
                            questionBody = "";
                        }
                    }

                    if (string.IsNullOrWhiteSpace(questionBody))
                    {
                        questionBody = $"Chọn phương án đúng nhất điền vào vị trí ({currentQuestionNumber}):";
                    }

                    string? attachedContextId = null;
                    string? attachedContextTitle = null;
                    string? attachedContextContent = null;

                    if (!string.IsNullOrEmpty(currentReadingPassage) && currentQuestionNumber >= passageStartQ && currentQuestionNumber <= passageEndQ)
                    {
                        // [Universal Shared Context] Lưu riêng biệt ngữ cảnh dùng chung, không nhân bản vào thân câu hỏi
                        attachedContextId = $"CTX_{currentExamCode}_{passageStartQ}_{passageEndQ}";
                        attachedContextTitle = $"Bài đọc hiểu (Câu {passageStartQ} - {passageEndQ})";
                        attachedContextContent = currentReadingPassage.Trim();
                    }

                    var inferredTopic = currentTopicCode != "GENERAL" ? currentTopicCode : InferTopicFromContent(questionBody);

                    // Tra cứu bảng đáp án trước nếu có
                    string initialAnswer = "";
                    if (answerKeyMap != null &&
                        answerKeyMap.TryGetValue(currentExamCode, out var secMap) &&
                        secMap.TryGetValue(currentSection, out var qAnsMap) &&
                        qAnsMap.TryGetValue(currentQuestionNumber, out var tableAns))
                    {
                        initialAnswer = tableAns;
                    }

                    bool isFatal = Regex.IsMatch(questionBody, @"(?:\bDIEM[_\s]*LIET\b|\bĐIỂM[_\s]*LIỆT\b|mất\s*an\s*toàn\s*giao\s*thông\s*nghiêm\s*trọng|bị\s*nghiêm\s*cấm|\bnghiêm\s*cấm\b|nồng\s*độ\s*cồn|sử\s*dụng\s*ma\s*túy|đua\s*xe\s*trái\s*phép)", RegexOptions.IgnoreCase);
                    if (!isFatal && matchedRule != null)
                    {
                        isFatal = DynamicQuestionIngestionEngine.Instance.IsCriticalQuestion(questionBody, matchedRule);
                    }
                    string? subCat = null;
                    if (Regex.IsMatch(questionBody, @"(?:sa\s*hình|thứ\s*tự\s*các\s*xe|xe\s*nào\s*được\s*quyền\s*đi|xe\s*nào\s*phải\s*nhường)", RegexOptions.IgnoreCase))
                    {
                        subCat = "SA_HINH";
                    }
                    else if (Regex.IsMatch(questionBody, @"(?:biển\s*nào|biển\s*báo|biển\s*số)", RegexOptions.IgnoreCase))
                    {
                        subCat = "BIEN_BAO";
                    }

                    currentQuestion = new DocxImportPreviewDto
                    {
                        Content = questionBody,
                        QuestionType = currentSection,
                        TopicCode = inferredTopic,
                        Difficulty = isFatal ? 5 : (currentSection == "SINGLE" ? 2 : (currentSection == "TRUE_FALSE" ? 3 : 4)),
                        ContentType = questionBody.Contains("data:image/") ? "image" : "text",
                        OptionType = "text",
                        SuggestedAnswer = initialAnswer,
                        IsCritical = isFatal,
                        SubCategory = subCat,
                        ContextId = attachedContextId,
                        ContextTitle = attachedContextTitle,
                        ContextContent = attachedContextContent
                    };

                    if (sameLineOptions.Count > 0)
                    {
                        foreach (var opt in sameLineOptions)
                        {
                            currentQuestion.Options.Add(opt.Content);
                            if (opt.IsMarkedCorrect)
                            {
                                currentQuestion.SuggestedAnswer = (opt.Letter - 'A' + 1).ToString();
                            }
                        }
                    }

                    examQuestionsByNum[currentQuestionNumber] = currentQuestion;
                    continue;
                }

                // 4. Nếu đang trong ngữ cảnh của câu hỏi
                if (currentQuestion != null)
                {
                    // Nếu đang ở giai đoạn Lời giải chi tiết: gom toàn bộ vào lời giải
                    if (isSolutionPhase)
                    {
                        AppendExplanationLine(currentQuestion, line);
                        TryExtractAnswer(currentQuestion, line);
                        continue;
                    }
                    // 4.1. Nhận dạng bắt đầu Lời giải / Hướng dẫn giải
                    var matchExplStart = Regex.Match(line, @"^(?:Lời\s*giải|L[\.\?\s]*i\s*gi[\.\?\s]*i|Hướng\s*dẫn\s*giải|Hu?ng\s*d?n\s*gi?i|HƯỚNG\s*DẪN|LỜI\s*GIẢI|HD)[\.\:\s\-]+(.*)", RegexOptions.IgnoreCase);
                    bool isExplHeaderOnly = Regex.IsMatch(line, @"^(?:Lời\s*giải|L[\.\?\s]*i\s*gi[\.\?\s]*i|Hướng\s*dẫn\s*giải|Hu?ng\s*d?n\s*gi?i|HƯỚNG\s*DẪN|LỜI\s*GIẢI|HD)[\.\:\s\-]*$", RegexOptions.IgnoreCase);

                    if (matchExplStart.Success || isExplHeaderOnly)
                    {
                        inExplanation = true;
                        var remainder = matchExplStart.Success ? matchExplStart.Groups[1].Value.Trim() : "";
                        if (!string.IsNullOrEmpty(remainder))
                        {
                            AppendExplanationLine(currentQuestion, remainder);
                            TryExtractAnswer(currentQuestion, remainder);
                        }
                        continue;
                    }

                    // 4.2. Nếu đang ở trạng thái đọc Lời giải chi tiết
                    if (inExplanation)
                    {
                        AppendExplanationLine(currentQuestion, line);
                        TryExtractAnswer(currentQuestion, line);
                        continue;
                    }

                    // 4.3. Nhận dạng dòng Đáp án tường minh trước lời giải: "Đáp án: A", "Đáp số: 79,2", "Key: C"
                    var matchAnswerLine = Regex.Match(line, @"^(?:Đáp\s*án|Đáp\s*số|Đa['\s]*p\s*a['\s]*n|Đ\/a|Key|Answer|Kết\s*quả)[\.\:\s]+(.*)", RegexOptions.IgnoreCase);
                    if (matchAnswerLine.Success)
                    {
                        var ansToken = matchAnswerLine.Groups[1].Value.Trim();
                        if (currentQuestion.QuestionType == "SINGLE" && ansToken.Length == 1 && char.IsLetter(ansToken[0]))
                        {
                            int optIndex = char.ToUpperInvariant(ansToken[0]) - 'A' + 1;
                            currentQuestion.SuggestedAnswer = optIndex.ToString();
                        }
                        else
                        {
                            currentQuestion.SuggestedAnswer = ansToken;
                        }
                        continue;
                    }

                    // 4.4. Nhận dạng dòng Giải thích / Căn cứ
                    var matchExplanationLine = Regex.Match(line, @"^(?:Giải\s*thích|Căn\s*cứ|Ghi\s*chú)[\.\:\s]+(.*)", RegexOptions.IgnoreCase);
                    if (matchExplanationLine.Success)
                    {
                        currentQuestion.AiExplanation = matchExplanationLine.Groups[1].Value.Trim();
                        continue;
                    }

                    // 4.5. Kiểm tra xem dòng này có chứa các phương án A. B. C. D. hay không
                    if (currentQuestion.QuestionType == "SINGLE" || currentQuestion.QuestionType == "MULTI")
                    {
                        var parsedOptions = ExtractOptionsFromLine(line);
                        if (parsedOptions.Count > 0)
                        {
                            foreach (var opt in parsedOptions)
                            {
                                currentQuestion.Options.Add(opt.Content);
                                if (opt.IsMarkedCorrect && string.IsNullOrEmpty(currentQuestion.SuggestedAnswer))
                                {
                                    currentQuestion.SuggestedAnswer = currentQuestion.Options.Count.ToString();
                                }
                                if (opt.Content.Contains("data:image/"))
                                {
                                    currentQuestion.OptionType = "image";
                                }
                            }
                            continue;
                        }
                    }

                    // 4.6. Kiểm tra xem dòng này có phải các ý a), b), c), d) của phần Đúng/Sai không
                    if (currentQuestion.QuestionType == "TRUE_FALSE")
                    {
                        var matchSubItem = Regex.Match(line, @"^([a-d])[\)\.]\s*(.*)", RegexOptions.IgnoreCase);
                        if (matchSubItem.Success)
                        {
                            string itemLetter = matchSubItem.Groups[1].Value.ToLowerInvariant();
                            string itemContent = matchSubItem.Groups[2].Value.Trim();
                            currentQuestion.Options.Add($"{itemLetter}) {itemContent}");
                            if (itemContent.Contains("data:image/"))
                            {
                                currentQuestion.OptionType = "image";
                            }
                            continue;
                        }
                    }

                    // 4.7. Nếu là ảnh minh họa cho câu hỏi (hình vẽ đồ thị, khối đa diện đứng riêng lẻ)
                    if (line.StartsWith("data:image/") || line.StartsWith("![Hình vẽ]") || line.Contains("data:image/"))
                    {
                        if (!currentQuestion.Content.Contains("data:image/"))
                        {
                            currentQuestion.Content += "\n\n" + (line.StartsWith("![") ? line : $"![Hình minh họa]({line})");
                            currentQuestion.ContentType = "image";
                        }
                        else
                        {
                            currentQuestion.Content += "\n\n" + line;
                        }
                        continue;
                    }

                    // 4.8. Nếu là văn bản tiếp nối của câu hỏi
                    if (currentQuestion.Options.Count == 0)
                    {
                        currentQuestion.Content += "\n" + line;
                        if (!currentQuestion.IsCritical && Regex.IsMatch(line, @"(?:\bDIEM[_\s]*LIET\b|\bĐIỂM[_\s]*LIỆT\b|mất\s*an\s*toàn\s*giao\s*thông\s*nghiêm\s*trọng|bị\s*nghiêm\s*cấm|\bnghiêm\s*cấm\b|nồng\s*độ\s*cồn|sử\s*dụng\s*ma\s*túy|đua\s*xe\s*trái\s*phép)", RegexOptions.IgnoreCase))
                        {
                            currentQuestion.IsCritical = true;
                            currentQuestion.Difficulty = 5;
                        }
                    }
                    else
                    {
                        // Tiếp nối phương án trước đó
                        currentQuestion.Options[currentQuestion.Options.Count - 1] += " " + line;
                    }
                }
            }

            if (currentQuestion != null && !isSolutionPhase)
            {
                FinalizeAndMergeQuestion(results, questionMap, currentQuestion, currentExamCode, currentQuestionNumber);
            }

            return results;
        }

        private static void AppendExplanationLine(DocxImportPreviewDto q, string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            // Bỏ qua tiêu đề "Lời giải" trùng lặp
            if (Regex.IsMatch(line, @"^(?:Lời\s*giải|L?i\s*gi?i|Hướng\s*dẫn\s*giải|Hu?ng\s*d?n\s*gi?i|HƯỚNG\s*DẪN|LỜI\s*GIẢI|HD)[\.\:\s\-]*$", RegexOptions.IgnoreCase))
                return;

            if (string.IsNullOrWhiteSpace(q.AiExplanation))
            {
                q.AiExplanation = line;
            }
            else
            {
                q.AiExplanation += "\n" + line;
            }
        }

        private static void TryExtractAnswer(DocxImportPreviewDto q, string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            if (q.QuestionType == "SINGLE")
            {
                // 1. "Chọn A", "Choose A", "Option A", "Chọn câu B"
                var matchChon = Regex.Match(line, @"(?:Chọn|Ch[oọ\?]n|Choose|Option)\s*(?:câu\s*|đáp\s*án\s*)?([A-D])\b", RegexOptions.IgnoreCase);
                if (matchChon.Success)
                {
                    char letter = char.ToUpperInvariant(matchChon.Groups[1].Value[0]);
                    q.SuggestedAnswer = (letter - 'A' + 1).ToString();
                    return;
                }

                // 2. "Suy ra đáp án B", "đáp án đúng là C", "Đáp án C", "Answer C", "Key C"
                var matchDapAn = Regex.Match(line, @"(?:suy\s*ra\s*)?(?:đáp\s*án|dap\s*an|d[\?\.]*p\s*[\?\.]*n|answer|key)(?:\s*(?:đúng\s*)?là)?[\:\s]*([A-D])\b", RegexOptions.IgnoreCase);
                if (matchDapAn.Success)
                {
                    char letter = char.ToUpperInvariant(matchDapAn.Groups[1].Value[0]);
                    q.SuggestedAnswer = (letter - 'A' + 1).ToString();
                    return;
                }

                // 3. "[A-D] là đáp án đúng", "Do đó [A-D] đúng"
                var matchLaDapAn = Regex.Match(line, @"\b([A-D])\s*(?:là\s*đáp\s*án|đúng)\b", RegexOptions.IgnoreCase);
                if (matchLaDapAn.Success)
                {
                    char letter = char.ToUpperInvariant(matchLaDapAn.Groups[1].Value[0]);
                    q.SuggestedAnswer = (letter - 'A' + 1).ToString();
                    return;
                }
            }
            else if (q.QuestionType == "SHORT_ANSWER")
            {
                // "Đáp số: 79,2", "Đáp án: 9,8", "Da'p a'n : 9,8", "kết quả bằng 16"
                var matchShort = Regex.Match(line, @"^(?:Đáp\s*số|Đáp\s*án|Dap\s*so|Dap\s*an|Da['\s]*p\s*a['\s]*n|Kết\s*quả)(?:\s*bằng)?[\.\:\s]+([0-9\,\.\-]+)", RegexOptions.IgnoreCase);
                if (matchShort.Success)
                {
                    q.SuggestedAnswer = matchShort.Groups[1].Value.Trim();
                    return;
                }
            }
            else if (q.QuestionType == "TRUE_FALSE")
            {
                // "a) Đúng", "b) Sai", "a) ... Mệnh đề đúng", "b) ... Suy ra mệnh đề sai"
                var matchTf = Regex.Match(line, @"(?:^|\b)([a-d])[\)\.]?.*?\b(?:mệnh\s*đề\s+|khẳng\s*định\s+)?(đúng|sai|d|s)\b", RegexOptions.IgnoreCase);
                if (matchTf.Success)
                {
                    string itemLetter = matchTf.Groups[1].Value.ToLowerInvariant();
                    string valToken = matchTf.Groups[2].Value.ToLowerInvariant();
                    string val = (valToken.StartsWith("đ") || valToken.StartsWith("d")) ? "Đúng" : "Sai";

                    var parts = (q.SuggestedAnswer ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => !p.StartsWith(itemLetter + ":", StringComparison.OrdinalIgnoreCase) && !p.StartsWith(itemLetter + ")", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    parts.Add($"{itemLetter}) {val}");
                    parts.Sort();
                    q.SuggestedAnswer = string.Join(", ", parts);
                }
            }
        }

        private static void FinalizeAndMergeQuestion(
            List<DocxImportPreviewDto> results,
            Dictionary<string, DocxImportPreviewDto> questionMap,
            DocxImportPreviewDto q,
            string examCode,
            int qNum)
        {
            FinalizeQuestion(q);

            string key = $"{examCode}_{q.QuestionType}_{qNum}";

            if (questionMap.TryGetValue(key, out var existing))
            {
                // Nâng cấp Lời giải chi tiết
                if (string.IsNullOrWhiteSpace(existing.AiExplanation) && !string.IsNullOrWhiteSpace(q.AiExplanation))
                {
                    existing.AiExplanation = q.AiExplanation;
                }
                else if (!string.IsNullOrWhiteSpace(q.AiExplanation) && q.AiExplanation.Length > existing.AiExplanation.Length)
                {
                    existing.AiExplanation = q.AiExplanation;
                }

                // Nâng cấp Đáp án đúng
                if ((string.IsNullOrWhiteSpace(existing.SuggestedAnswer) || existing.SuggestedAnswer.Contains("Điền đáp số"))
                    && !string.IsNullOrWhiteSpace(q.SuggestedAnswer))
                {
                    existing.SuggestedAnswer = q.SuggestedAnswer;
                }

                // Nâng cấp Phương án nếu bản trước bị thiếu
                if (existing.Options.Count == 0 && q.Options.Count > 0)
                {
                    existing.Options = q.Options;
                    existing.OptionType = q.OptionType;
                }

                // Nâng cấp Nội dung & Ảnh minh họa nếu bản sau đầy đủ hơn
                if (q.Content.Length > existing.Content.Length && q.Content.Contains("data:image/"))
                {
                    existing.Content = q.Content;
                    existing.ContentType = q.ContentType;
                }
            }
            else
            {
                questionMap[key] = q;
                results.Add(q);
            }
        }

        private static Dictionary<string, Dictionary<string, Dictionary<int, string>>> ExtractAnswerTables(XDocument doc)
        {
            var map = new Dictionary<string, Dictionary<string, Dictionary<int, string>>>(StringComparer.OrdinalIgnoreCase);
            var body = doc.Descendants(W + "body").FirstOrDefault();
            if (body == null) return map;

            string currentExam = "DEFAULT";

            foreach (var el in body.Elements())
            {
                if (el.Name == W + "p")
                {
                    var pText = string.Concat(el.Descendants(W + "t").Select(t => t.Value)).Trim();
                    var matchExam = Regex.Match(pText, @"^(?:ĐỀ|D[E|Ê])\s*(?:SỐ\s*|THI\s*|ÔN\s*THI\s*)?(\d+)\b", RegexOptions.IgnoreCase);
                    if (matchExam.Success)
                    {
                        int examNum = int.Parse(matchExam.Groups[1].Value);
                        currentExam = $"TOAN_DE_{examNum:D2}";
                    }
                }
                else if (el.Name == W + "tbl")
                {
                    var rows = el.Descendants(W + "tr").ToList();
                    if (rows.Count < 2) continue;

                    var row0 = rows[0].Descendants(W + "tc").Select(tc => string.Concat(tc.Descendants(W + "t").Select(t => t.Value)).Trim()).ToList();
                    var row1 = rows[1].Descendants(W + "tc").Select(tc => string.Concat(tc.Descendants(W + "t").Select(t => t.Value)).Trim()).ToList();

                    // Dạng 1: Bảng trắc nghiệm 1 đáp án hoặc trả lời ngắn
                    if (row0.Count >= 2 && Regex.IsMatch(row0[0], @"^(?:Câu|C.u)$", RegexOptions.IgnoreCase)
                        && Regex.IsMatch(row1[0], @"^(?:Chọn|Ch.n)$", RegexOptions.IgnoreCase))
                    {
                        if (!map.ContainsKey(currentExam)) map[currentExam] = new();
                        for (int c = 1; c < Math.Min(row0.Count, row1.Count); c++)
                        {
                            if (int.TryParse(row0[c], out int qNum))
                            {
                                string ans = row1[c].Trim();
                                string sec = Regex.IsMatch(ans, @"^[A-D]$", RegexOptions.IgnoreCase) ? "SINGLE" : "SHORT_ANSWER";
                                if (!map[currentExam].ContainsKey(sec)) map[currentExam][sec] = new();

                                if (sec == "SINGLE")
                                {
                                    char ansChar = char.ToUpperInvariant(ans[0]);
                                    map[currentExam][sec][qNum] = (ansChar - 'A' + 1).ToString();
                                }
                                else
                                {
                                    map[currentExam][sec][qNum] = ans;
                                }
                            }
                        }
                    }
                    // Dạng 2: Bảng trắc nghiệm Đúng / Sai (Row 0: Câu 1 | Câu 2 | Câu 3 | Câu 4)
                    else if (row0.Count >= 4 && Regex.IsMatch(row0[0], @"^(?:Câu|C.u)\s*1", RegexOptions.IgnoreCase))
                    {
                        if (!map.ContainsKey(currentExam)) map[currentExam] = new();
                        if (!map[currentExam].ContainsKey("TRUE_FALSE")) map[currentExam]["TRUE_FALSE"] = new();

                        for (int col = 0; col < row0.Count; col++)
                        {
                            int qNum = col + 1;
                            var tfParts = new List<string>();
                            for (int r = 1; r < rows.Count; r++)
                            {
                                var cells = rows[r].Descendants(W + "tc").Select(tc => string.Concat(tc.Descendants(W + "t").Select(t => t.Value)).Trim()).ToList();
                                if (col < cells.Count && !string.IsNullOrEmpty(cells[col]))
                                {
                                    tfParts.Add(cells[col]);
                                }
                            }
                            if (tfParts.Count > 0)
                            {
                                map[currentExam]["TRUE_FALSE"][qNum] = string.Join(", ", tfParts);
                            }
                        }
                    }
                    // Dạng 3: Bảng lưới đa cột dọc (Câu | Đáp án | Câu | Đáp án ...)
                    else if (row0.Count >= 2 && Regex.IsMatch(row0[0], @"^(?:Câu|C[\.\?\s]*u)$", RegexOptions.IgnoreCase)
                             && Regex.IsMatch(row0[1], @"^(?:Đáp\s*án|D[\.\?\s]*p\s*[\.\?\s]*n|Answer|Key)$", RegexOptions.IgnoreCase))
                    {
                        if (!map.ContainsKey(currentExam)) map[currentExam] = new();
                        if (!map[currentExam].ContainsKey("SINGLE")) map[currentExam]["SINGLE"] = new();

                        for (int r = 1; r < rows.Count; r++)
                        {
                            var cells = rows[r].Descendants(W + "tc").Select(tc => string.Concat(tc.Descendants(W + "t").Select(t => t.Value)).Trim()).ToList();
                            for (int c = 0; c < cells.Count - 1; c += 2)
                            {
                                if (int.TryParse(cells[c], out int qNum))
                                {
                                    string ans = cells[c + 1].Trim().ToUpperInvariant();
                                    if (Regex.IsMatch(ans, @"^[A-D]$"))
                                    {
                                        char ansChar = ans[0];
                                        map[currentExam]["SINGLE"][qNum] = (ansChar - 'A' + 1).ToString();
                                    }
                                    else if (!string.IsNullOrWhiteSpace(ans))
                                    {
                                        map[currentExam]["SINGLE"][qNum] = ans;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return map;
        }

        private class ParsedOption
        {
            public char Letter { get; set; }
            public string Content { get; set; } = string.Empty;
            public bool IsMarkedCorrect { get; set; }
        }

        /// <summary>
        /// Bóc tách các phương án từ một dòng (hỗ trợ cả trường hợp "A. ... B. ... C. ... D. ...")
        /// </summary>
        private static List<ParsedOption> ExtractOptionsFromLine(string line)
        {
            var options = new List<ParsedOption>();
            if (string.IsNullOrWhiteSpace(line)) return options;

            // Pattern nhận dạng các phương án A. B. C. D. hoặc *A. A*. [A]
            // Sử dụng lookbehind (?<=[a-z0-9\)]) để nhận diện khi phương án dính liền với từ trước (ví dụ: gripsB., giftC.)
            // Pattern nhận dạng các phương án A-G hoặc 1-4 (đặc thù đề thi Sát hạch GPLX, đề thi trắc nghiệm số)
            var regex = new Regex(@"(?:^|[\s\t\.\,\;\–\—]+|(?<=[a-z0-9\)]))(\*?)([A-G]|[1-4])(\*?)[\.\:\)]+\s*");
            var matches = regex.Matches(line);

            if (matches.Count == 0) return options;

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                char rawChar = match.Groups[2].Value[0];
                char letter = char.IsDigit(rawChar) ? (char)('A' + (rawChar - '1')) : char.ToUpperInvariant(rawChar);
                bool isCorrect = match.Groups[1].Value == "*" || match.Groups[3].Value == "*";

                int contentStart = match.Index + match.Length;
                int contentEnd = (i + 1 < matches.Count) ? matches[i + 1].Index : line.Length;

                string content = line.Substring(contentStart, contentEnd - contentStart).Trim();

                // Làm sạch các dấu chấm, tab thừa ở cuối phương án
                content = Regex.Replace(content, @"[\.\s\t]+$", "").Trim();

                options.Add(new ParsedOption
                {
                    Letter = letter,
                    Content = content,
                    IsMarkedCorrect = isCorrect
                });
            }

            return options;
        }

        private static void FinalizeQuestion(DocxImportPreviewDto q)
        {
            if (q.TopicCode == "GENERAL")
            {
                q.TopicCode = InferTopicFromContent(q.Content);
            }

            // Tự động nhận dạng ảnh trong options
            if (q.Options.Any(o => o.Contains("data:image/")))
            {
                q.OptionType = "image";
            }

            // Tự động nhận dạng câu hỏi trả lời ngắn (SHORT_ANSWER) hoặc Tự luận (ESSAY)
            if (q.QuestionType == "SHORT_ANSWER")
            {
                q.Difficulty = Math.Max(q.Difficulty, 4);
                if (string.IsNullOrWhiteSpace(q.SuggestedAnswer))
                {
                    q.SuggestedAnswer = "Điền đáp số / giá trị số.";
                }
            }
            else if (q.QuestionType == "TRUE_FALSE")
            {
                q.Difficulty = Math.Max(q.Difficulty, 3);
            }
            else if (q.Options.Count == 0 && q.QuestionType == "SINGLE")
            {
                q.QuestionType = "SHORT_ANSWER";
            }

            // Tự động nhận diện Đa ngành & Thẻ thông minh
            AutoEnrichTaxonomyAndTags(q);
        }

        public static void AutoEnrichTaxonomyAndTags(DocxImportPreviewDto q)
        {
            q.Tags ??= new List<string>();
            var lowerContent = (q.Content + " " + q.ContextContent + " " + q.TopicCode).ToLowerInvariant();

            // 1. Sát hạch Giao thông / GPLX
            if (q.SubCategory == "SA_HINH" || q.SubCategory == "BIEN_BAO" || q.SubCategory == "LUAT" 
                || lowerContent.Contains("sa hình") || lowerContent.Contains("biển báo") || lowerContent.Contains("vượt ẩu")
                || lowerContent.Contains("tốc độ tối đa") || lowerContent.Contains("gplx") || lowerContent.Contains("xe cơ giới"))
            {
                q.DomainCode = "GOV_DRIVING";
                q.IssuingOrg ??= "Cục Đường bộ Việt Nam";
                q.AssessmentPurpose ??= "Sát hạch Giấy phép Lái xe (GPLX)";
                q.BenchmarkStandard ??= "Nghị định 100/2019/NĐ-CP";
                q.BenchmarkYear ??= 2023;
                AddTagIfNotExists(q.Tags, "gplx");
                AddTagIfNotExists(q.Tags, "sat-hach-lai-xe");
                if (q.SubCategory == "SA_HINH" || lowerContent.Contains("sa hình")) AddTagIfNotExists(q.Tags, "sa-hinh");
                if (q.SubCategory == "BIEN_BAO" || lowerContent.Contains("biển báo")) AddTagIfNotExists(q.Tags, "bien-bao");
                if (q.IsCritical) AddTagIfNotExists(q.Tags, "diem-liet");
                return;
            }

            // 2. Toán học / Giáo dục THPT
            if (lowerContent.Contains("đạo hàm") || lowerContent.Contains("tích phân") || lowerContent.Contains("nguyên hàm")
                || lowerContent.Contains("bảng biến thiên") || lowerContent.Contains("hàm số") || lowerContent.Contains("hình nón")
                || lowerContent.Contains("toan_de_") || lowerContent.Contains("thpt") || lowerContent.Contains("gdpt"))
            {
                q.DomainCode = "EDUCATION";
                q.TargetLevel ??= "Lớp 12";
                q.AssessmentPurpose ??= "Kỳ thi Tốt nghiệp THPT Quốc Gia";
                q.BenchmarkStandard ??= "Chương trình GDPT 2018";
                q.BenchmarkYear ??= 2025;
                AddTagIfNotExists(q.Tags, "toan-hoc");
                AddTagIfNotExists(q.Tags, "tot-nghiep-2025");
                AddTagIfNotExists(q.Tags, "gdpt-2018");
                AddTagIfNotExists(q.Tags, "lop-12");
                return;
            }

            // 3. Tiếng Anh / Ngoại ngữ
            if (lowerContent.Contains("reading passage") || lowerContent.Contains("pronunciation") || lowerContent.Contains("closest in meaning")
                || lowerContent.Contains("opposite in meaning") || lowerContent.Contains("global warming"))
            {
                q.DomainCode = "EDUCATION";
                q.TargetLevel ??= "Lớp 12";
                q.AssessmentPurpose ??= "Kỳ thi Tốt nghiệp THPT Quốc Gia";
                q.BenchmarkStandard ??= "Chương trình GDPT 2018";
                q.BenchmarkYear ??= 2025;
                AddTagIfNotExists(q.Tags, "tieng-anh");
                AddTagIfNotExists(q.Tags, "doc-hieu");
                AddTagIfNotExists(q.Tags, "tot-nghiep-2025");
                return;
            }

            // 4. Ngân hàng & Tài chính
            if (lowerContent.Contains("kho tiền") || lowerContent.Contains("tín dụng") || lowerContent.Contains("ngân quỹ")
                || lowerContent.Contains("rửa tiền") || lowerContent.Contains("thế chấp") || lowerContent.Contains("cic")
                || lowerContent.Contains("vcb") || lowerContent.Contains("vietcombank") || lowerContent.Contains("aml"))
            {
                q.DomainCode = "BANKING";
                q.TargetLevel ??= "Chuyên viên chính";
                q.AssessmentPurpose ??= "Sát hạch Nghiệp vụ Tuân thủ AML Định kỳ";
                q.BenchmarkStandard ??= "Thông tư 41 & Basel III";
                q.BenchmarkYear ??= 2025;
                q.IssuingOrg ??= "Ngân hàng TMCP Ngoại thương (Vietcombank)";
                AddTagIfNotExists(q.Tags, "ngan-hang");
                AddTagIfNotExists(q.Tags, "aml");
                AddTagIfNotExists(q.Tags, "vcb");
                return;
            }

            // 5. Y tế & Dược phẩm
            if (lowerContent.Contains("bác sĩ") || lowerContent.Contains("dược lâm sàng") || lowerContent.Contains("phác đồ điều trị")
                || lowerContent.Contains("bệnh viện") || lowerContent.Contains("bộ y tế"))
            {
                q.DomainCode = "HEALTHCARE";
                q.TargetLevel ??= "Bác sĩ CKI";
                q.AssessmentPurpose ??= "Sát hạch Chuyên môn Y khoa";
                q.BenchmarkStandard ??= "Phác đồ Bộ Y tế 2025";
                q.BenchmarkYear ??= 2025;
                AddTagIfNotExists(q.Tags, "y-te");
                AddTagIfNotExists(q.Tags, "duoc-hoc");
                return;
            }

            // 6. HSE & An toàn Lao động
            if (lowerContent.Contains("an toàn lao động") || lowerContent.Contains("bảo hộ lao động") || lowerContent.Contains("iso 45001")
                || lowerContent.Contains("pccc") || lowerContent.Contains("phòng cháy chữa cháy"))
            {
                q.DomainCode = "HSE";
                q.TargetLevel ??= "Nhóm 2 / Nhóm 3";
                q.AssessmentPurpose ??= "Huấn luyện An toàn VSLĐ định kỳ";
                q.BenchmarkStandard ??= "ISO 45001:2018";
                q.BenchmarkYear ??= 2025;
                AddTagIfNotExists(q.Tags, "an-toan-lao-dong");
                AddTagIfNotExists(q.Tags, "hse");
                return;
            }

            // 7. CNTT & An toàn Thông tin
            if (lowerContent.Contains("an toàn thông tin") || lowerContent.Contains("iso 27001") || lowerContent.Contains("owasp")
                || lowerContent.Contains("mật khẩu") || lowerContent.Contains("phishing") || lowerContent.Contains("cybersecurity"))
            {
                q.DomainCode = "IT_SECURITY";
                q.TargetLevel ??= "Chuyên viên ATTT";
                q.AssessmentPurpose ??= "Tuân thủ An ninh mạng & Bảo mật";
                q.BenchmarkStandard ??= "ISO/IEC 27001:2022";
                q.BenchmarkYear ??= 2025;
                AddTagIfNotExists(q.Tags, "an-toan-thong-tin");
                AddTagIfNotExists(q.Tags, "iso-27001");
                return;
            }

            if (string.IsNullOrWhiteSpace(q.DomainCode) || q.DomainCode == "GENERAL")
            {
                var sniffed = UniversalCoordinateTaxonomyEngine.SniffCoordinates(
                    null,
                    q.TopicCode,
                    new[] { q.ContextTitle ?? "", q.SubCategory ?? "" },
                    new[] { q.Content },
                    new[] { q.AiExplanation });

                if (sniffed.DomainCode != "GENERAL")
                {
                    q.DomainCode = sniffed.DomainCode;
                    q.TargetLevel ??= sniffed.TargetLevel;
                    q.AssessmentPurpose ??= sniffed.AssessmentPurpose;
                    q.IssuingOrg ??= sniffed.IssuingOrg;
                    q.BenchmarkYear ??= sniffed.BenchmarkYear;
                    q.BenchmarkStandard ??= sniffed.BenchmarkStandard;
                    foreach (var t in sniffed.Tags) AddTagIfNotExists(q.Tags, t);
                }
                else
                {
                    q.DomainCode = "GENERAL";
                }
            }
        }

        private static void AddTagIfNotExists(List<string> tags, string tag)
        {
            if (!tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
            {
                tags.Add(tag);
            }
        }

        /// <summary>
        /// Bóc tách các đoạn văn kèm hình ảnh và công thức MathType chuyển đổi sang PNG Base64
        /// </summary>
        private static List<string> ExtractParagraphTokens(
            XDocument doc, Dictionary<string, string> relsMap, Dictionary<string, byte[]> mediaDict)
        {
            var paragraphs = new List<string>();
            var pNodes = doc.Descendants(W + "p");

            foreach (var p in pNodes)
            {
                var sb = new StringBuilder();

                foreach (var r in p.Descendants(W + "r"))
                {
                    // 1. Lấy thuộc tính định dạng (gạch chân cho đề phát âm/trọng âm/tìm lỗi sai)
                    var rPr = r.Element(W + "rPr");
                    bool isUnderline = rPr?.Element(W + "u") != null;

                    var runTextSb = new StringBuilder();
                    foreach (var elem in r.Elements())
                    {
                        if (elem.Name == W + "t")
                        {
                            runTextSb.Append(elem.Value);
                        }
                        else if (elem.Name == W + "tab")
                        {
                            runTextSb.Append("    ");
                        }
                        else if (elem.Name == W + "br")
                        {
                            runTextSb.Append("\n");
                        }
                    }
                    var runText = runTextSb.ToString();

                    if (!string.IsNullOrEmpty(runText))
                    {
                        if (isUnderline)
                        {
                            runText = $"<u>{runText}</u>";
                        }
                        sb.Append(runText);
                    }

                    // 2. Lấy hình vẽ hoặc MathType WMF vector
                    var drawingNodes = r.Descendants().Where(e =>
                        e.Name.LocalName == "drawing" || e.Name.LocalName == "object" || e.Name.LocalName == "pict");

                    foreach (var node in drawingNodes)
                    {
                        var xml = node.ToString();
                        var matches = Regex.Matches(xml, @"(?:r:id|r:embed)=""([^""]+)""");
                        foreach (Match m in matches)
                        {
                            var rId = m.Groups[1].Value;
                            if (relsMap.TryGetValue(rId, out var target))
                            {
                                var cleanTarget = target.Replace("word/", "");
                                if (mediaDict.TryGetValue(cleanTarget, out var bytes))
                                {
                                    var dataUri = ConvertMediaToDataUri(cleanTarget, bytes);
                                    if (!string.IsNullOrEmpty(dataUri))
                                    {
                                        sb.Append($" {dataUri} ");
                                    }
                                }
                            }
                        }
                    }
                }

                var text = sb.ToString().Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    paragraphs.Add(text);
                }
            }

            return paragraphs;
        }

        private static string ConvertMediaToDataUri(string filename, byte[] bytes)
        {
            var ext = Path.GetExtension(filename).ToLowerInvariant();

            // Nếu là WMF (MathType formula vector): Chuyển đổi sang PNG siêu nét
            if (ext == ".wmf")
            {
                try
                {
                    using var msIn = new MemoryStream(bytes);
                    using var img = Image.FromStream(msIn);
                    using var msOut = new MemoryStream();
                    img.Save(msOut, ImageFormat.Png);
                    return "data:image/png;base64," + Convert.ToBase64String(msOut.ToArray());
                }
                catch
                {
                    return string.Empty;
                }
            }

            // Nếu là ảnh raster thông thường (JPG, PNG, WebP)
            string mime = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/png"
            };

            return $"data:{mime};base64," + Convert.ToBase64String(bytes);
        }

        private static Dictionary<string, string> LoadRelationships(ZipArchive archive)
        {
            var map = new Dictionary<string, string>();
            var entry = archive.GetEntry("word/_rels/document.xml.rels");
            if (entry == null) return map;

            using var stream = entry.Open();
            var doc = XDocument.Load(stream);
            foreach (var rel in doc.Descendants(XName.Get("Relationship", "http://schemas.openxmlformats.org/package/2006/relationships")))
            {
                var id = rel.Attribute("Id")?.Value;
                var target = rel.Attribute("Target")?.Value;
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(target))
                {
                    map[id] = target;
                }
            }

            return map;
        }

        private static Dictionary<string, byte[]> LoadMediaFiles(ZipArchive archive)
        {
            var dict = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.StartsWith("word/media/", StringComparison.OrdinalIgnoreCase))
                {
                    using var s = entry.Open();
                    using var ms = new MemoryStream();
                    s.CopyTo(ms);
                    var key = entry.FullName.Substring("word/".Length); // "media/image1.jpg"
                    dict[key] = ms.ToArray();
                }
            }

            return dict;
        }

        private static List<string> ExtractRawParagraphsFromDocx(Stream docxStream)
        {
            var paragraphs = new List<string>();
            try
            {
                using var archive = new ZipArchive(docxStream, ZipArchiveMode.Read, true);
                var documentEntry = archive.GetEntry("word/document.xml");
                if (documentEntry == null) return paragraphs;

                using var entryStream = documentEntry.Open();
                var doc = XDocument.Load(entryStream);

                foreach (var p in doc.Descendants(W + "p"))
                {
                    var textParts = new List<string>();
                    foreach (var t in p.Descendants(W + "t"))
                    {
                        textParts.Add(t.Value);
                    }
                    var line = string.Concat(textParts).Trim();
                    if (!string.IsNullOrEmpty(line))
                    {
                        paragraphs.Add(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DocxParserService] Lỗi giải nén tệp docx thô: {ex.Message}");
            }
            return paragraphs;
        }

        private static string ResolveTopicCode(string raw)
        {
            var clean = raw.Trim();
            if (string.IsNullOrWhiteSpace(clean)) return "GENERAL";

            var lower = clean.ToLowerInvariant();
            if (lower.Contains("toán") || lower.Contains("toan") || lower.Contains("giải tích") || lower.Contains("hình học")) return "MATH";
            if (lower.Contains("tiếng anh") || lower.Contains("english") || lower.Contains("ngoại ngữ") || lower.Contains("ngoaingu") || lower.Contains("toeic") || lower.Contains("ielts")) return "TIENG_ANH";
            if (lower.Contains("tín dụng") || lower.Contains("cho vay")) return "382";
            if (lower.Contains("kế toán") || lower.Contains("ngân quỹ") || lower.Contains("kho tiền")) return "381";
            if (lower.Contains("công nghệ") || lower.Contains("cntt")) return "CNTT";
            if (lower.Contains("đảng") || lower.Contains("chi bộ")) return "PARTY_BUILDING";
            if (lower.Contains("pháp chế") || lower.Contains("luật")) return "LAW";

            return clean;
        }

        private static string InferTopicFromContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return "GENERAL";
            var lower = content.ToLowerInvariant();

            if (Regex.IsMatch(lower, @"\b(mark\s+the\s+letter|pronunciation|primary\s+stress|closest\s+in\s+meaning|opposite\s+in\s+meaning|underlined\s+part|synonym|antonym|read\s+the\s+following\s+passage)\b"))
                return "TIENG_ANH";

            if (lower.Contains("hàm số") || lower.Contains("đồ thị") || lower.Contains("hình lập phương") || lower.Contains("tích phân") || lower.Contains("xác suất") || lower.Contains("tiệm cận") || lower.Contains("vectơ") || lower.Contains("vector") || lower.Contains("tối giản"))
                return "MATH";

            if (lower.Contains("đảng") || lower.Contains("chi bộ") || lower.Contains("đảng viên"))
                return "PARTY_BUILDING";

            if (lower.Contains("kho tiền") || lower.Contains("chìa khóa") || lower.Contains("niêm phong"))
                return "381";

            if (lower.Contains("tín dụng") || lower.Contains("thế chấp") || lower.Contains("cho vay") || lower.Contains("nợ xấu"))
                return "382";

            if (lower.Contains("giao thức") || lower.Contains("mật khẩu") || lower.Contains("tấn công") || lower.Contains("cntt"))
                return "CNTT";

            return "GENERAL";
        }
    }
}
