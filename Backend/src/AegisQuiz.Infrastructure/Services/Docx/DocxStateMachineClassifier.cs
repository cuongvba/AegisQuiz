using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Services.Docx
{
    /// <summary>
    /// [Docx State Machine Classifier]
    /// Máy trạng thái phân loại và bóc tách câu hỏi trắc nghiệm:
    /// - Nhận diện phần thi (Phần I: Đơn, Phần II: Đúng/Sai, Phần III: Trả lời ngắn, Tự luận).
    /// - Nhận diện mã đề thi, bài đọc chia sẻ (Reading Passage).
    /// - Tự động suy luận môn thi và gắn thẻ tri thức thông minh (Taxonomy & Tags).
    /// </summary>
    public static class DocxStateMachineClassifier
    {
        public class ParsedOption
        {
            public char Letter { get; set; }
            public string Content { get; set; } = string.Empty;
            public bool IsMarkedCorrect { get; set; }
        }

        public static List<DocxImportPreviewDto> ParseParagraphList(
            List<string> paragraphs,
            Dictionary<string, Dictionary<string, Dictionary<int, string>>>? answerKeyMap = null,
            DynamicDomainContext? dynamicContext = null,
            DynamicQuestionIngestionEngine? engineOverride = null)
        {
            var results = new List<DocxImportPreviewDto>();
            var questionMap = new Dictionary<string, DocxImportPreviewDto>(StringComparer.OrdinalIgnoreCase);
            var cig = engineOverride ?? DynamicQuestionIngestionEngine.Instance;

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
            QuestionParsingRuleDto? currentMatchedRule = null;

            // [CHIP v3.0 - Pha 1: Document Pre-flight Profiling]
            // Nhận diện hồ sơ ngôn ngữ kép (HostLanguage & SubjectLanguage) cấp tài liệu 100% từ Data
            var languageProfile = cig.DetectDocumentLanguageProfile(paragraphs, dynamicContext?.Language ?? "vi");
            var activeRules = cig.GetRulesForProfile(languageProfile);

            if (languageProfile.SubjectLanguage == "en" && currentTopicCode == "GENERAL")
            {
                currentTopicCode = "TIENG_ANH";
            }

            // Quét tìm tiêu đề môn học trong phần đầu tài liệu (ví dụ: MÔN: TOÁN 12, Môn thi: Vật lý...)
            foreach (var p in paragraphs.Take(30))
            {
                var matchSubject = Regex.Match(p, @"\b(?:MÔN|MÔN\s*THI|HỌC\s*PHẦN)[\:\s\-]+([A-Za-zÀ-ỹ0-9\s]+?)(?:\(|\d|\-|$)", RegexOptions.IgnoreCase);
                if (matchSubject.Success)
                {
                    string subjRaw = matchSubject.Groups[1].Value.Trim();
                    string resolved = ResolveTopicCode(subjRaw, dynamicContext);
                    if (resolved != "GENERAL")
                    {
                        currentTopicCode = resolved;
                        break;
                    }
                }
            }

            var currentQuestionPendingLines = new List<string>();

            Action finalizeCurrentQuestion = () =>
            {
                if (currentQuestion != null && !isSolutionPhase)
                {
                    ResolveQuestionBlockBottomUp(currentQuestion, currentQuestionPendingLines, currentSection, currentMatchedRule, cig);
                    FinalizeAndMergeQuestion(results, questionMap, currentQuestion, currentExamCode, currentQuestionNumber, dynamicContext);
                    examQuestionsByNum[currentQuestionNumber] = currentQuestion;
                    currentQuestion = null;
                    currentQuestionPendingLines.Clear();
                }
            };

            foreach (var rawLine in paragraphs)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // 0. Nhận dạng Tiêu đề Đề thi động qua CIG: "ĐỀ 1", "EXAM 02", "TEST 3"...
                if (cig.TryMatchExamHeading(line, out string examNumStr) && int.TryParse(examNumStr, out int examNum))
                {
                    if (examNum >= 1900 && examNum <= 2099)
                    {
                        continue; // Bỏ qua năm học (2024..2026)
                    }

                    finalizeCurrentQuestion();
                    currentTopicCode = "GENERAL"; // [Sprint 1 - 7.3] Reset topic moi lan doi de

                    // [Sprint 1 - 7.2] Xay examPrefix dong tu DynamicDomainContext
                    string examPrefix = "DE_";
                    if (dynamicContext != null && !string.IsNullOrWhiteSpace(dynamicContext.DefaultExamPrefix)
                        && dynamicContext.DefaultExamPrefix != "EXAM" && dynamicContext.DefaultExamPrefix != "DE")
                    {
                        examPrefix = $"{dynamicContext.DefaultExamPrefix}_";
                    }
                    string targetExamCode = $"{examPrefix}{examNum:D2}";

                    bool alreadyHasQuestions = results.Any(r => r.SourceExamName == targetExamCode);
                    if (isSolutionPhase && !alreadyHasQuestions)
                    {
                        // Đang ở pha giải nhưng gặp đề mới chưa từng có ở pha đề bài -> bắt đầu đề bài mới
                        isSolutionPhase = false;
                    }

                    currentExamCode = targetExamCode;
                    currentSection = "SINGLE";
                    inExplanation = false;
                    currentReadingPassage = null;
                    passageStartQ = 0;
                    passageEndQ = int.MaxValue;
                    examQuestionsByNum.Clear();
                    foreach (var eq in results.Where(r => r.SourceExamName == currentExamCode && int.TryParse(r.SourceQuestionNo, out _)))
                    {
                        if (int.TryParse(eq.SourceQuestionNo, out int num))
                        {
                            examQuestionsByNum[num] = eq;
                        }
                    }
                    currentQuestion = null;
                    continue;
                }

                // 0b. Nhận dạng Tiêu đề Phần Bảng Đáp án hoặc Lời giải tham khảo qua CIG
                // Tiêu đề Phase (LỜI GIẢI THAM KHẢO, ĐÁP ÁN THAM KHẢO, BẢNG ĐÁP ÁN) có mức ưu tiên tài liệu cao nhất,
                // đánh dấu kết thúc pha đề bài và bắt đầu pha lời giải/đáp án chi tiết.
                bool isSolPhaseMatch = cig.TryMatchSolutionPhase(line);
                bool isAnsTableMatch = cig.TryMatchAnswerTablePhase(line);
                if (isSolPhaseMatch || isAnsTableMatch)
                {
                    finalizeCurrentQuestion();
                    if (isSolPhaseMatch)
                    {
                        isSolutionPhase = true;
                    }
                    inExplanation = false;
                    currentReadingPassage = null;
                    passageStartQ = 0;
                    passageEndQ = 0;
                    continue;
                }

                // 1. Nhận dạng thay đổi Chủ đề qua CIG: "Chủ đề: Toán học", "Môn: Tiếng Anh", "Topic: Science"...
                if (cig.TryMatchTopicHeading(line, out string rawTopic))
                {
                    currentTopicCode = ResolveTopicCode(rawTopic, dynamicContext);
                    continue;
                }

                var upper = line.ToUpperInvariant();
                bool isQuestionLine = cig.TryMatchQuestionAnchor(line, out _, out _, activeRules);
                bool isExplanationLine = cig.IsExplanationLine(line);

                // 2. Nhận dạng Tiêu đề Đoạn văn Đọc hiểu qua CIG (Passage / Advertisement / Leaflet)
                if (cig.TryMatchReadingPassage(line, out int pStart, out int pEnd))
                {
                    finalizeCurrentQuestion();
                    currentReadingPassage = line;
                    inExplanation = false;
                    passageStartQ = pStart;
                    passageEndQ = pEnd > 0 ? pEnd : int.MaxValue;
                    if (currentTopicCode == "GENERAL") currentTopicCode = "TIENG_ANH";
                    continue;
                }

                // 2b. Nhận dạng Section Headers động từ CIG & StructureDictionary (hỗ trợ GDPT 2025, Ngoại ngữ, SAT, Gaokao)
                bool isSecHeader = cig.TryMatchSectionHeader(line, out string detectedSection);
                if (!isQuestionLine && !isExplanationLine && isSecHeader)
                {
                    finalizeCurrentQuestion();
                    currentSection = detectedSection;
                    inExplanation = false;
                    currentReadingPassage = null;
                    passageStartQ = 0;
                    passageEndQ = 0;
                    if (currentTopicCode == "GENERAL" && (upper.Contains("ENGLISH") || upper.Contains("TIENG ANH") || upper.Contains("PRONUNCIATION") || upper.Contains("READING")))
                    {
                        currentTopicCode = "TIENG_ANH";
                    }
                    continue;
                }

                // 2c. Thu thập các đoạn văn tiếp theo của bài đọc hiểu khi chưa vào câu hỏi con
                if (!string.IsNullOrEmpty(currentReadingPassage) && currentQuestion == null && !isQuestionLine)
                {
                    currentReadingPassage += "\n\n" + line;
                    continue;
                }

                // 3. Nhận dạng câu hỏi mới: Hỗ trợ hoàn toàn thông qua CIG Engine
                var cleanLine = Regex.Replace(line, @"^<[a-z0-9]+>", "", RegexOptions.IgnoreCase).Trim();
                if (cig.TryMatchQuestionAnchor(cleanLine, out int qNum, out var matchedRule, activeRules))
                {
                    currentMatchedRule = matchedRule;
                    string remainderText = "";
                    var mStandard = Regex.Match(cleanLine, @"^(?:Câu|Question|Item|Task|Exercise|Q)\s*\d+[\.\:\s\-]+(.*)", RegexOptions.IgnoreCase);
                    if (mStandard.Success)
                    {
                        remainderText = mStandard.Groups[1].Value.Trim();
                    }
                    else if (matchedRule != null)
                    {
                        var dynRegex = new Regex(matchedRule.AnchorSpec.RegexPattern, RegexOptions.IgnoreCase);
                        var mDyn = dynRegex.Match(cleanLine);
                        remainderText = mDyn.Success ? cleanLine.Substring(mDyn.Length).Trim() : cleanLine;
                        remainderText = Regex.Replace(remainderText, @"^[\.\:\s\-]+", "").Trim();
                    }

                    if (isSolutionPhase)
                    {
                        var existingQ = FindQuestionByHierarchy(questionMap, results, currentExamCode, currentSection, qNum);
                        if (existingQ != null)
                        {
                            currentQuestion = existingQ;

                            // Kiểm tra xem dòng tiêu đề này có chứa Lời giải thực sự hay không
                            var mExpl = Regex.Match(remainderText, @"^(?:(?:Lời\s*giải|L[.\?\s]*i\s*gi[.\?\s]*i|Hướng\s*dẫn\s*giải|Hu?ng\s*d?n\s*gi?i|HƯỚNG\s*DẪN|LỜI\s*GIẢI|HD|Giải\s*thích|Explanation|Rationale)[\.\:\s\-]+|Chọn\s+[A-D0-9,\.\-]+\s*[\.\:\s\-]+)(.*)", RegexOptions.IgnoreCase);
                            if (mExpl.Success)
                            {
                                inExplanation = true;
                                string explPart = mExpl.Groups[1].Value.Trim();
                                if (!string.IsNullOrWhiteSpace(explPart))
                                {
                                    AppendExplanationLine(currentQuestion, explPart, matchedRule);
                                }
                                TryExtractAnswer(currentQuestion, remainderText, matchedRule);
                            }
                            else
                            {
                                // Dòng này chỉ là đề bài lặp lại -> chưa vào lời giải thực sự
                                inExplanation = false;
                            }
                        }
                        else
                        {
                            currentQuestion = null;
                            inExplanation = false;
                        }
                        continue;
                    }

                    finalizeCurrentQuestion();

                    currentQuestionNumber = qNum;
                    inExplanation = false;

                    var contentText = remainderText;
                    var sameLineOptions = (currentSection == "SINGLE" || currentSection == "MULTI")
                        ? ExtractUppercaseOptionsFromLine(contentText, matchedRule)
                        : new List<ParsedOption>();
                    if (sameLineOptions.Count < 2 && !sameLineOptions.Any(o => o.IsMarkedCorrect))
                    {
                        sameLineOptions.Clear();
                    }

                    string questionBody = contentText;

                    if (sameLineOptions.Count > 0)
                    {
                        var firstOptRegex = new Regex(@"(?:^|[\s\t\–\—]+)(\*?)(?-i:" + sameLineOptions[0].Letter + @")(\*?)[\.\:\)][\s\t]+");
                        var mFirst = firstOptRegex.Match(contentText);
                        if (mFirst.Success)
                        {
                            questionBody = contentText.Substring(0, mFirst.Index).Trim();
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
                        attachedContextId = $"CTX_{currentExamCode}_{passageStartQ}_{passageEndQ}";
                        attachedContextTitle = $"Bài đọc hiểu (Câu {passageStartQ} - {passageEndQ})";
                        attachedContextContent = currentReadingPassage.Trim();
                    }

                    var inferredTopic = currentTopicCode != "GENERAL" ? currentTopicCode : InferTopicFromContent(questionBody, dynamicContext);

                    string initialAnswer = "";
                    if (answerKeyMap != null)
                    {
                        var examCodes = new[] { currentExamCode, "DEFAULT" };
                        var sections = new[] { currentSection, "SINGLE", "TRUE_FALSE", "MULTI_TRUE_FALSE", "SHORT_ANSWER" };
                        var qNums = new[] { currentQuestionNumber, results.Count + 1 };

                        foreach (var ec in examCodes)
                        {
                            if (answerKeyMap.TryGetValue(ec, out var secMap))
                            {
                                foreach (var sec in sections)
                                {
                                    if (secMap.TryGetValue(sec, out var qAnsMap))
                                    {
                                        foreach (var qn in qNums)
                                        {
                                            if (qn > 0 && qAnsMap.TryGetValue(qn, out var tableAns) && !string.IsNullOrWhiteSpace(tableAns))
                                            {
                                                initialAnswer = tableAns;
                                                break;
                                            }
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(initialAnswer)) break;
                                }
                            }
                            if (!string.IsNullOrEmpty(initialAnswer)) break;
                        }
                    }

                    bool isFatal = cig.IsCriticalQuestion(questionBody, matchedRule);
                    string? subCat = cig.DetectSubCategory(questionBody, matchedRule);

                    string determinedQuestionType = currentSection;
                    if (currentSection == "SINGLE" && !string.IsNullOrEmpty(matchedRule?.InteractionSpec?.DefaultQuestionType) && matchedRule.InteractionSpec.DefaultQuestionType != "SINGLE")
                    {
                        determinedQuestionType = matchedRule.InteractionSpec.DefaultQuestionType;
                    }

                    currentQuestion = new DocxImportPreviewDto
                    {
                        Content = sameLineOptions.Count > 0 ? questionBody : "",
                        QuestionType = determinedQuestionType,
                        TopicCode = inferredTopic,
                        Difficulty = isFatal ? 5 : (determinedQuestionType == "SINGLE" ? 2 : (determinedQuestionType == "TRUE_FALSE" ? 3 : 4)),
                        ContentType = questionBody.Contains("data:image/") ? "image" : "text",
                        OptionType = "text",
                        SuggestedAnswer = initialAnswer,
                        IsCritical = isFatal,
                        SubCategory = subCat,
                        ContextId = attachedContextId,
                        ContextTitle = attachedContextTitle,
                        ContextContent = attachedContextContent,
                        SourceExamName = currentExamCode,
                        SourceSectionName = currentSection,
                        SourceQuestionNo = currentQuestionNumber > 0 ? currentQuestionNumber.ToString() : null
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
                    else if (!string.IsNullOrWhiteSpace(remainderText))
                    {
                        currentQuestionPendingLines.Add(remainderText);
                    }

                    examQuestionsByNum[currentQuestionNumber] = currentQuestion;
                    continue;
                }

                // 4. Nếu đang trong ngữ cảnh của câu hỏi
                if (currentQuestion != null)
                {
                    if (isSolutionPhase)
                    {
                        if (!inExplanation)
                        {
                            // Kiểm tra xem dòng này có phải là mở đầu của Lời giải không
                            var mExplLine = Regex.Match(line, @"^(?:Lời\s*giải|L[.\?\s]*i\s*gi[.\?\s]*i|Hướng\s*dẫn\s*giải|Hu?ng\s*d?n\s*gi?i|HƯỚNG\s*DẪN|LỜI\s*GIẢI|HD|Giải\s*thích|Explanation|Rationale)[\.\:\s\-]*(.*)", RegexOptions.IgnoreCase);
                            if (mExplLine.Success)
                            {
                                inExplanation = true;
                                string rest = mExplLine.Groups[1].Value.Trim();
                                if (!string.IsNullOrWhiteSpace(rest))
                                {
                                    AppendExplanationLine(currentQuestion, rest, currentMatchedRule);
                                }
                                continue;
                            }
                            else if (Regex.IsMatch(line, @"^(?:Chọn|Đáp\s*án|Đáp\s*số)\s+[A-D0-9,\.\-]+", RegexOptions.IgnoreCase))
                            {
                                inExplanation = true;
                                AppendExplanationLine(currentQuestion, line, currentMatchedRule);
                                TryExtractAnswer(currentQuestion, line, currentMatchedRule);
                                continue;
                            }

                            // Kiểm tra xem dòng này có chứa phương án gạch chân / đánh dấu đúng trước khi vào Lời giải hay không
                            if (currentQuestion.QuestionType == "SINGLE" || currentQuestion.QuestionType == "MULTI")
                            {
                                var sameLineOpts = ExtractUppercaseOptionsFromLine(line, currentMatchedRule);
                                var markedOpt = sameLineOpts.FirstOrDefault(o => o.IsMarkedCorrect);
                                if (markedOpt != null)
                                {
                                    if (string.IsNullOrWhiteSpace(currentQuestion.SuggestedAnswer) || currentQuestion.SuggestedAnswer.Contains("Điền đáp số"))
                                    {
                                        currentQuestion.SuggestedAnswer = (markedOpt.Letter - 'A' + 1).ToString();
                                    }
                                }
                            }
                            else if (currentQuestion.QuestionType == "TRUE_FALSE" || currentQuestion.QuestionType == "MULTI_TRUE_FALSE")
                            {
                                var matchTfItem = Regex.Match(line, @"^(?:<u>|\*|\*\*|\<b\>)?[\(\s]*([a-d1-4])[\)\.\:]\s*(?:</u>|\*|\*\*|\</b\>)?(.*)", RegexOptions.IgnoreCase);
                                if (matchTfItem.Success)
                                {
                                    char itemChar = char.ToLowerInvariant(matchTfItem.Groups[1].Value[0]);
                                    string itemLetter = (char.IsDigit(itemChar) ? (char)('a' + (itemChar - '1')) : itemChar).ToString();
                                    bool isUnderlinedOrMarked = line.Contains($"<u>{itemLetter}") || line.Contains($"<u>({itemLetter}") 
                                                             || line.Contains($"*{itemLetter}") || line.Contains($"<b>{itemLetter}")
                                                             || Regex.IsMatch(line, $@"<u>\s*\(?{itemLetter}[\)\.]?\s*</u>", RegexOptions.IgnoreCase);
                                    string val = isUnderlinedOrMarked ? "Đúng" : "Sai";
                                    UpdateTrueFalseAnswer(currentQuestion, itemLetter, val);
                                }
                            }

                            // Chưa vào lời giải và dòng này là các lựa chọn A, B, C, D lặp lại -> bỏ qua
                            continue;
                        }

                        AppendExplanationLine(currentQuestion, line, currentMatchedRule);
                        TryExtractAnswer(currentQuestion, line, currentMatchedRule);
                        continue;
                    }

                    currentQuestionPendingLines.Add(line);
                }
            }

            finalizeCurrentQuestion();

            // [Post-Process Pha 1: Bù đắp đáp án từ bảng đáp án cho các câu còn trống hoặc giữ placeholder]
            if (answerKeyMap != null && answerKeyMap.Count > 0)
            {
                for (int i = 0; i < results.Count; i++)
                {
                    var q = results[i];
                    bool needsAns = string.IsNullOrWhiteSpace(q.SuggestedAnswer) 
                        || q.SuggestedAnswer.Contains("Điền đáp số")
                        || (q.QuestionType == "MULTI_TRUE_FALSE" && !q.SuggestedAnswer.Contains(")"));

                    if (needsAns)
                    {
                        int qNum1Based = i + 1;
                        var examCodes = new[] { q.SourceExamName ?? currentExamCode, currentExamCode, "DEFAULT" };
                        var sections = new[] { q.QuestionType, "TRUE_FALSE", "MULTI_TRUE_FALSE", "SINGLE", "SHORT_ANSWER" };
                        var matchQNum = Regex.Match(q.Content, @"^(?:Câu|Bài|Question|Item|Aufgabe|Exercice|CÂU|BÀI)?\s*(\d+)", RegexOptions.IgnoreCase);
                        int localQNum = matchQNum.Success ? int.Parse(matchQNum.Groups[1].Value) : 0;
                        var qNumsToTry = localQNum > 0 && localQNum != qNum1Based ? new[] { qNum1Based, localQNum } : new[] { qNum1Based };

                        string foundAns = "";
                        foreach (var ec in examCodes)
                        {
                            if (answerKeyMap.TryGetValue(ec, out var secMap))
                            {
                                foreach (var sec in sections)
                                {
                                    if (secMap.TryGetValue(sec, out var qAnsMap))
                                    {
                                        foreach (var qn in qNumsToTry)
                                        {
                                            if (qn > 0 && qAnsMap.TryGetValue(qn, out var ansVal) && !string.IsNullOrWhiteSpace(ansVal))
                                            {
                                                foundAns = ansVal;
                                                break;
                                            }
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(foundAns)) break;
                                }
                            }
                            if (!string.IsNullOrEmpty(foundAns)) break;
                        }

                        if (!string.IsNullOrEmpty(foundAns))
                        {
                            q.SuggestedAnswer = foundAns;
                        }
                    }
                }
            }

            // [Post-Process Pha 2: Đồng nhất Chủ đề cho toàn bộ Đề thi (Tránh phân mảnh 1 đề thành 2 chủ đề)]
            var nonGeneralTopics = results
                .Where(q => !string.IsNullOrEmpty(q.TopicCode) && q.TopicCode != "GENERAL")
                .GroupBy(q => q.TopicCode)
                .OrderByDescending(g => g.Count())
                .ToList();

            if (nonGeneralTopics.Count > 0)
            {
                var dominantTopic = nonGeneralTopics.First();
                // Nếu chủ đề này chiếm từ 40% câu hỏi trở lên (ví dụ Toán học chiếm 16/22 = 73%)
                if (dominantTopic.Count() >= results.Count * 0.4)
                {
                    string domCode = dominantTopic.Key;
                    string domName = domCode switch
                    {
                        "MATH" => "Toán học",
                        "VAT_LY" => "Vật lý",
                        "HOA_HOC" => "Hóa học",
                        "TIENG_ANH" => "Tiếng Anh",
                        "SINH_HOC" => "Sinh học",
                        "LICH_SU" => "Lịch sử",
                        "DIA_LY" => "Địa lý",
                        "TIN_HOC" => "Tin học",
                        _ => domCode
                    };

                    foreach (var q in results)
                    {
                        if (string.IsNullOrEmpty(q.TopicCode) || q.TopicCode == "GENERAL")
                        {
                            q.TopicCode = domCode;
                        }
                        if (string.IsNullOrEmpty(q.TopicName))
                        {
                            q.TopicName = domName;
                        }
                    }
                }
            }

            // [Post-Process Pha 3: Tự động bổ sung Hệ 5 Trục Tọa Độ và Thẻ thông minh cấp Đề thi qua UniversalCoordinateTaxonomyEngine]
            var examGroups = results.GroupBy(q => q.SourceExamName ?? "DEFAULT").ToList();
            foreach (var eg in examGroups)
            {
                var examQuestions = eg.ToList();
                var sampleQ = examQuestions.Take(5).Select(q => q.Content).ToList();
                var sampleRefs = examQuestions.Take(5).Select(q => q.AiExplanation ?? "").Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
                var dominantTopic = examQuestions.Select(q => q.TopicCode).FirstOrDefault(t => !string.IsNullOrEmpty(t) && t != "GENERAL") ?? "GENERAL";

                var sniffed = UniversalCoordinateTaxonomyEngine.SniffCoordinates(
                    fileName: null,
                    sheetName: dominantTopic,
                    headerBannerLines: new[] { eg.Key },
                    sampleContents: sampleQ,
                    sampleReferences: sampleRefs,
                    dynamicContext: dynamicContext);

                foreach (var q in examQuestions)
                {
                    if (string.IsNullOrWhiteSpace(q.DomainCode) || q.DomainCode == "GENERAL")
                    {
                        q.DomainCode = !string.IsNullOrWhiteSpace(sniffed.DomainCode) && sniffed.DomainCode != "GENERAL" ? sniffed.DomainCode : q.TopicCode;
                    }
                    if (string.IsNullOrWhiteSpace(q.TargetLevel)) q.TargetLevel = sniffed.TargetLevel;
                    q.AssessmentPurpose ??= sniffed.AssessmentPurpose;
                    q.IssuingOrg ??= sniffed.IssuingOrg;
                    q.BenchmarkYear ??= sniffed.BenchmarkYear;
                    q.BenchmarkStandard ??= sniffed.BenchmarkStandard;

                    q.Tags ??= new List<string>();
                    foreach (var t in sniffed.Tags)
                    {
                        AddTagIfNotExists(q.Tags, t);
                    }
                }
            }

            return results;
        }

        public static void AppendExplanationLine(DocxImportPreviewDto q, string line, QuestionParsingRuleDto? rule = null)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            if (Regex.IsMatch(line, @"^(?:Lời\s*giải|L?i\s*gi?i|Hướng\s*dẫn\s*giải|Hu?ng\s*d?n\s*gi?i|HƯỚNG\s*DẪN|LỜI\s*GIẢI|HD|Explanation|Rationale)[\.\:\s\-]*$", RegexOptions.IgnoreCase))
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

        public static void UpdateTrueFalseAnswer(DocxImportPreviewDto q, string itemLetter, string val)
        {
            var validLetters = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a", "b", "c", "d" };
            if (!validLetters.Contains(itemLetter)) return;

            var parts = (q.SuggestedAnswer ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => {
                    var m = Regex.Match(p, @"^([a-d])[\:\)]", RegexOptions.IgnoreCase);
                    return m.Success && !m.Groups[1].Value.Equals(itemLetter, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
            parts.Add($"{itemLetter.ToLowerInvariant()}) {val}");
            parts.Sort();
            q.SuggestedAnswer = string.Join(", ", parts);
        }

        public static void TryExtractAnswer(DocxImportPreviewDto q, string line, QuestionParsingRuleDto? rule = null)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            if (DynamicQuestionIngestionEngine.Instance.TryExtractInlineAnswer(line, q.QuestionType, rule, out string inlineAns))
            {
                if (q.QuestionType == "TRUE_FALSE" || q.QuestionType == "MULTI_TRUE_FALSE")
                {
                    var matchTf = Regex.Match(inlineAns, @"^([a-d1-4])[\:\)]\s*(.+)", RegexOptions.IgnoreCase);
                    if (matchTf.Success)
                    {
                        char rawChar = matchTf.Groups[1].Value[0];
                        string itemLetter = (char.IsDigit(rawChar) ? (char)('a' + (rawChar - '1')) : char.ToLowerInvariant(rawChar)).ToString();
                        string val = matchTf.Groups[2].Value.Trim();
                        string formattedVal = val.StartsWith("Đ", StringComparison.OrdinalIgnoreCase) || val.StartsWith("D", StringComparison.OrdinalIgnoreCase) ? "Đúng" : "Sai";
                        UpdateTrueFalseAnswer(q, itemLetter, formattedVal);
                        return;
                    }
                }
                q.SuggestedAnswer = inlineAns;
                return;
            }

            // Fallback tương thích ngược nếu rule không trích xuất được
            if (q.QuestionType == "SINGLE")
            {
                var matchChon = Regex.Match(line, @"(?:^|[\.\:\s])(?:Chọn|Ch[oọ\?]n|Choose|Option)\s*(?:câu\s*|đáp\s*án\s*)?([A-D])\b", RegexOptions.IgnoreCase);
                if (matchChon.Success)
                {
                    char letter = char.ToUpperInvariant(matchChon.Groups[1].Value[0]);
                    q.SuggestedAnswer = (letter - 'A' + 1).ToString();
                    return;
                }

                var matchDapAn = Regex.Match(line, @"^(?:suy\s*ra\s*)?(?:đáp\s*án|dap\s*an|d[\?\.]*p\s*[\?\.]*n|answer|key)(?:\s*(?:đúng\s*)?là)?[\:\s]+([A-D])\b", RegexOptions.IgnoreCase);
                if (matchDapAn.Success)
                {
                    char letter = char.ToUpperInvariant(matchDapAn.Groups[1].Value[0]);
                    q.SuggestedAnswer = (letter - 'A' + 1).ToString();
                    return;
                }

                if (string.IsNullOrWhiteSpace(q.SuggestedAnswer) || q.SuggestedAnswer == "0")
                {
                    var matchLaDapAn = Regex.Match(line, @"\b([A-D])\s*(?:là\s*đáp\s*án|đúng)\b", RegexOptions.IgnoreCase);
                    if (matchLaDapAn.Success)
                    {
                        char letter = char.ToUpperInvariant(matchLaDapAn.Groups[1].Value[0]);
                        q.SuggestedAnswer = (letter - 'A' + 1).ToString();
                        return;
                    }
                }
            }
            else if (q.QuestionType == "SHORT_ANSWER")
            {
                var matchShort = Regex.Match(line, @"^(?:Đáp\s*số|Đáp\s*án|Dap\s*so|Dap\s*an|Da['\s]*p\s*a['\s]*n|Kết\s*quả)(?:\s*bằng)?[\.\:\s\=]+\$?([0-9\,\.\-]+)\$?", RegexOptions.IgnoreCase);
                if (matchShort.Success)
                {
                    q.SuggestedAnswer = matchShort.Groups[1].Value.Trim().Trim('$').Trim();
                    return;
                }
            }
            else if (q.QuestionType == "TRUE_FALSE" || q.QuestionType == "MULTI_TRUE_FALSE")
            {
                var matchTf = Regex.Match(line, @"(?:^|[\(\s])([a-d1-4])[\)\.\:]\s*.*?\b(?:chọn\s+)?(đúng|sai|true|false)\b", RegexOptions.IgnoreCase);
                if (!matchTf.Success)
                {
                    matchTf = Regex.Match(line, @"\b(?:ý|mệnh\s*đề|khẳng\s*định)\s+([a-d1-4])\b.*?\b(?:chọn\s+)?(đúng|sai|true|false)\b", RegexOptions.IgnoreCase);
                }
                if (matchTf.Success)
                {
                    char raw = matchTf.Groups[1].Value[0];
                    string itemLetter = (char.IsDigit(raw) ? (char)('a' + (raw - '1')) : char.ToLowerInvariant(raw)).ToString();
                    string valToken = matchTf.Groups[2].Value.ToLowerInvariant();
                    string val = (valToken.StartsWith("đ") || valToken.StartsWith("d") || valToken.StartsWith("t")) ? "Đúng" : "Sai";
                    UpdateTrueFalseAnswer(q, itemLetter, val);
                }
            }
        }

        public static void FinalizeAndMergeQuestion(
            List<DocxImportPreviewDto> results,
            Dictionary<string, DocxImportPreviewDto> questionMap,
            DocxImportPreviewDto q,
            string examCode,
            int qNum,
            DynamicDomainContext? ctx = null)
        {
            FinalizeQuestion(q, ctx);

            string key = $"{examCode}_{q.QuestionType}_{qNum}";

            if (questionMap.TryGetValue(key, out var existing))
            {
                if (string.IsNullOrWhiteSpace(existing.AiExplanation) && !string.IsNullOrWhiteSpace(q.AiExplanation))
                {
                    existing.AiExplanation = q.AiExplanation;
                }
                else if (!string.IsNullOrWhiteSpace(q.AiExplanation) && q.AiExplanation.Length > existing.AiExplanation.Length)
                {
                    existing.AiExplanation = q.AiExplanation;
                }

                if ((string.IsNullOrWhiteSpace(existing.SuggestedAnswer) || existing.SuggestedAnswer.Contains("Điền đáp số"))
                    && !string.IsNullOrWhiteSpace(q.SuggestedAnswer))
                {
                    existing.SuggestedAnswer = q.SuggestedAnswer;
                }

                if (existing.Options.Count == 0 && q.Options.Count > 0)
                {
                    existing.Options = q.Options;
                    existing.OptionType = q.OptionType;
                }

                if (q.Content.Length > existing.Content.Length && q.Content.Contains("data:image/"))
                {
                    existing.Content = q.Content;
                    existing.ContentType = q.ContentType;
                }
            }
            else
            {
                questionMap[key] = q;
                if (!string.IsNullOrEmpty(q.SourceSectionName))
                {
                    questionMap[$"{examCode}_{q.SourceSectionName}_{qNum}"] = q;
                }
                if (q.QuestionType == "MULTI_TRUE_FALSE")
                {
                    questionMap[$"{examCode}_TRUE_FALSE_{qNum}"] = q;
                }
                else if (q.QuestionType == "TRUE_FALSE")
                {
                    questionMap[$"{examCode}_MULTI_TRUE_FALSE_{qNum}"] = q;
                }
                results.Add(q);
            }
        }

        /// <summary>
        /// Tìm câu hỏi tương ứng trong pha lời giải theo đúng phân cấp Đề -> Phần -> Câu hỏi
        /// </summary>
        public static DocxImportPreviewDto? FindQuestionByHierarchy(
            Dictionary<string, DocxImportPreviewDto> questionMap,
            List<DocxImportPreviewDto> results,
            string examCode,
            string section,
            int qNum)
        {
            var candidateSections = GetEquivalentSections(section);

            // 1. Tra cứu theo questionMap với các candidate sections
            foreach (var sec in candidateSections)
            {
                string key = $"{examCode}_{sec}_{qNum}";
                if (questionMap.TryGetValue(key, out var found))
                {
                    return found;
                }
            }

            // 2. Tra cứu trong danh sách kết quả đã bóc tách
            var matchedInResults = results.FirstOrDefault(r =>
                (string.Equals(r.SourceExamName, examCode, StringComparison.OrdinalIgnoreCase) || examCode == "DEFAULT") &&
                candidateSections.Any(s => string.Equals(r.SourceSectionName, s, StringComparison.OrdinalIgnoreCase) 
                                        || string.Equals(r.QuestionType, s, StringComparison.OrdinalIgnoreCase)) &&
                r.SourceQuestionNo == qNum.ToString());

            if (matchedInResults != null)
            {
                return matchedInResults;
            }

            // 3. Fallback an toàn: CHỈ KHI toàn bộ đề này có DUY NHẤT một câu mang số qNum đó (đề không chia phần hoặc đánh số câu liên tục từ 1..N)
            var allWithQNum = results.Where(r =>
                (string.Equals(r.SourceExamName, examCode, StringComparison.OrdinalIgnoreCase) || examCode == "DEFAULT") &&
                r.SourceQuestionNo == qNum.ToString()).ToList();

            if (allWithQNum.Count == 1)
            {
                return allWithQNum[0];
            }

            return null;
        }

        public static List<string> GetEquivalentSections(string section)
        {
            var secUpper = (section ?? "SINGLE").ToUpperInvariant();
            if (secUpper == "TRUE_FALSE" || secUpper == "MULTI_TRUE_FALSE")
                return new List<string> { "MULTI_TRUE_FALSE", "TRUE_FALSE" };
            if (secUpper == "SINGLE" || secUpper == "MULTI")
                return new List<string> { "SINGLE", "MULTI" };
            if (secUpper == "SHORT_ANSWER")
                return new List<string> { "SHORT_ANSWER" };
            if (secUpper == "ESSAY")
                return new List<string> { "ESSAY" };
            return new List<string> { secUpper };
        }

        public static List<ParsedOption> ExtractUppercaseOptionsFromLine(string line, QuestionParsingRuleDto? rule = null)
        {
            var options = new List<ParsedOption>();
            if (string.IsNullOrWhiteSpace(line)) return options;

            var cigOpts = DynamicQuestionIngestionEngine.Instance.ExtractOptions(line, rule);
            if (cigOpts.Count > 0)
            {
                foreach (var opt in cigOpts)
                {
                    char rawChar = opt.Letter;
                    if (char.IsUpper(rawChar) || char.IsDigit(rawChar))
                    {
                        char letter = char.IsDigit(rawChar) ? (char)('A' + (rawChar - '1')) : rawChar;
                        options.Add(new ParsedOption
                        {
                            Letter = letter,
                            Content = opt.Content,
                            IsMarkedCorrect = opt.IsMarkedCorrect
                        });
                    }
                }
                if (options.Count > 0) return options;
            }

            bool isGplx = rule?.RuleCode == "VN_GPLX_600_STANDARD" || rule?.TargetExamType == "GPLX";
            string pattern = isGplx
                ? @"(?:^|[\s\t\–\—]+)(?:<u>|\*|\*\*|\<b\>)?(\*?)((?-i:[A-H])|[1-4])(\*?)(?:</u>|\*|\*\*|\</b\>)?[\.\:\)](?:</u>|\</b\>)?[\s\t]+"
                : @"(?:^|[\s\t\–\—]+)(?:<u>|\*|\*\*|\<b\>)?(\*?)(?-i:([A-H]))(\*?)(?:</u>|\*|\*\*|\</b\>)?[\.\:\)](?:</u>|\</b\>)?[\s\t]+";

            var regex = new Regex(pattern);
            var matches = regex.Matches(line);
            if (matches.Count == 0) return options;

            if (matches.Count == 1 && char.IsDigit(matches[0].Groups[2].Value[0]) && !isGplx)
            {
                return options;
            }

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                char rawChar = match.Groups[2].Value[0];
                char letter = char.IsDigit(rawChar) ? (char)('A' + (rawChar - '1')) : char.ToUpperInvariant(rawChar);
                bool isCorrect = match.Value.Contains("<u>") || match.Value.Contains("<b>") || match.Groups[1].Value == "*" || match.Groups[3].Value == "*";

                int contentStart = match.Index + match.Length;
                int contentEnd = (i + 1 < matches.Count) ? matches[i + 1].Index : line.Length;

                string content = line.Substring(contentStart, contentEnd - contentStart).Trim();
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

        private static bool TryExtractOptionsBottomUp(
            List<string> lines,
            string currentSection,
            QuestionParsingRuleDto? rule,
            out List<ParsedOption> options,
            out int optionStartIndex,
            out string questionType)
        {
            options = new List<ParsedOption>();
            optionStartIndex = lines.Count;
            questionType = currentSection;

            if (lines.Count == 0) return false;

            bool isTfSection = currentSection == "TRUE_FALSE" || currentSection == "MULTI_TRUE_FALSE";
            bool hasExplicitTfPrompt = lines.Any(l => Regex.IsMatch(l, @"(?:đúng\s*[-–\/\s]*sai|chọn\s+đúng\s+hoặc\s+sai|mệnh\s+đề\s+đúng\s+hay\s+sai|true\s*[-–\/\s]*false)", RegexOptions.IgnoreCase));

            // [ƯU TIÊN SECTION ĐÚNG/SAI GDPT 2025]: Nếu đang ở Section TRUE_FALSE hoặc có prompt Đúng/Sai rõ ràng
            if (isTfSection || hasExplicitTfPrompt)
            {
                if (TryExtractTrueFalseOptions(lines, out var tfOptions, out int tfStartIdx))
                {
                    options = tfOptions;
                    optionStartIndex = tfStartIdx;
                    questionType = "MULTI_TRUE_FALSE";
                    return true;
                }
            }

            // 1. ƯU TIÊN SỐ 1: Phương án in HOA (Chuẩn SINGLE / MULTI: A., B., C., D.)
            // 1A. Quét Inline Options ở cuối (dòng chứa >= 2 options hoa, ví dụ A... B... C... D... hoặc A... B... và C... D...)
            int inlineStart = lines.Count;
            var inlineOpts = new List<ParsedOption>();
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                var lineOpts = ExtractUppercaseOptionsFromLine(lines[i], rule);
                if (lineOpts.Count >= 2)
                {
                    inlineOpts.InsertRange(0, lineOpts);
                    inlineStart = i;
                    if (lineOpts[0].Letter == 'A') break;
                }
                else if (lineOpts.Count == 1 && inlineOpts.Count > 0)
                {
                    inlineOpts.InsertRange(0, lineOpts);
                    inlineStart = i;
                    if (lineOpts[0].Letter == 'A') break;
                }
                else
                {
                    break;
                }
            }

            if (inlineOpts.Count >= 2 && (inlineOpts[0].Letter == 'A' || inlineOpts[0].Letter == '1'))
            {
                options = inlineOpts;
                optionStartIndex = inlineStart;
                questionType = "SINGLE";
                return true;
            }

            // 1B. Quét Multiline Options ở cuối (A., B., C., D. trên nhiều dòng, bao gồm công thức toán hoặc ảnh xuống dòng riêng)
            if (TryExtractUppercaseMultilineOptions(lines, out var multilineOpts, out int multilineStart))
            {
                options = multilineOpts;
                optionStartIndex = multilineStart;
                questionType = "SINGLE";
                return true;
            }

            // Fallback cho trường hợp đề Đúng/Sai nhưng Section ban đầu chưa xác định
            if (!isTfSection && !hasExplicitTfPrompt)
            {
                if (TryExtractTrueFalseOptions(lines, out var fallbackTfOpts, out int fallbackTfStart))
                {
                    options = fallbackTfOpts;
                    optionStartIndex = fallbackTfStart;
                    questionType = "MULTI_TRUE_FALSE";
                    return true;
                }
            }

            // 3. ƯU TIÊN SỐ 3: Phương án chữ thường cá biệt (a., b., c., d.) khi không có chữ hoa
            if (currentSection == "SINGLE")
            {
                var lowerOpts = new List<ParsedOption>();
                int lowerStart = lines.Count;
                char expectedLower = '\0';
                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    var match = Regex.Match(lines[i], @"^(\*?)(?:([a-d])|[\(]([a-d])[\)])[\.\:\)][\s\t]+(.*)", RegexOptions.None);
                    if (match.Success)
                    {
                        char ltr = (!string.IsNullOrEmpty(match.Groups[2].Value) ? match.Groups[2].Value : match.Groups[3].Value)[0];
                        bool isCorrect = match.Groups[1].Value == "*";
                        string cnt = match.Groups[4].Value.Trim();

                        if (expectedLower == '\0')
                        {
                            if (ltr >= 'b' && ltr <= 'd')
                            {
                                expectedLower = (char)(ltr - 1);
                                lowerOpts.Insert(0, new ParsedOption { Letter = char.ToUpperInvariant(ltr), Content = cnt, IsMarkedCorrect = isCorrect });
                                lowerStart = i;
                            }
                            else break;
                        }
                        else if (ltr == expectedLower)
                        {
                            lowerOpts.Insert(0, new ParsedOption { Letter = char.ToUpperInvariant(ltr), Content = cnt, IsMarkedCorrect = isCorrect });
                            lowerStart = i;
                            if (ltr == 'a') break;
                            expectedLower = (char)(ltr - 1);
                        }
                        else break;
                    }
                    else break;
                }

                if (lowerOpts.Count >= 2 && lowerOpts[0].Letter == 'A')
                {
                    bool hasMoreItemsAbove = lowerStart > 0 && Regex.IsMatch(lines[lowerStart - 1], @"^[a-z][\.\)]", RegexOptions.IgnoreCase);
                    if (!hasMoreItemsAbove)
                    {
                        options = lowerOpts;
                        optionStartIndex = lowerStart;
                        questionType = "SINGLE";
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryExtractTrueFalseOptions(
            List<string> lines,
            out List<ParsedOption> options,
            out int startIndex)
        {
            options = new List<ParsedOption>();
            startIndex = lines.Count;

            var tfSubItems = new List<ParsedOption>();
            int tfStart = -1;
            char expectedLetter = 'a';
            ParsedOption? currentTfOpt = null;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Match nhãn đúng sai: a), a., (a), <u>b)</u>, <b>c.</b>, etc.
                var matchTf = Regex.Match(line, @"^(?:<u>|\*|\*\*|\<b\>|\<strong\>|\<em\>)*[\(\[\s]*((?-i:[a-d])|[1-4])(?:\</[a-z0-9]+\>)*[\)\]\.\:\-]+(?:\</[a-z0-9]+\>)*\s*(.*)");
                if (matchTf.Success)
                {
                    char rawChar = matchTf.Groups[1].Value[0];
                    char letterChar = char.IsDigit(rawChar) ? (char)('a' + (rawChar - '1')) : char.ToLowerInvariant(rawChar);

                    if (letterChar == expectedLetter)
                    {
                        if (tfStart == -1) tfStart = i;
                        string content = matchTf.Groups[2].Value.Trim();
                        bool isCorrect = line.Contains("<u>") || line.Contains("<b>") || line.Contains("*");
                        currentTfOpt = new ParsedOption
                        {
                            Letter = letterChar,
                            Content = $"{letterChar}) {content}",
                            IsMarkedCorrect = isCorrect
                        };
                        tfSubItems.Add(currentTfOpt);
                        expectedLetter = (char)(expectedLetter + 1);
                        continue;
                    }
                }

                // Nếu đã bắt đầu vào khối phương án a, b, c, d
                if (currentTfOpt != null)
                {
                    // Dòng này là dòng nội dung nhiều dòng (công thức toán, hình ảnh, văn bản xuống dòng) của option hiện tại
                    currentTfOpt.Content = string.IsNullOrEmpty(currentTfOpt.Content) ? line : $"{currentTfOpt.Content}\n{line}";
                    if (line.Contains("<u>") || line.Contains("<b>") || line.Contains("*"))
                    {
                        currentTfOpt.IsMarkedCorrect = true;
                    }
                }
            }

            if (tfSubItems.Count >= 2)
            {
                options = tfSubItems;
                startIndex = tfStart;
                return true;
            }

            return false;
        }

        private static bool TryExtractUppercaseMultilineOptions(
            List<string> lines,
            out List<ParsedOption> options,
            out int startIndex)
        {
            options = new List<ParsedOption>();
            startIndex = lines.Count;

            var subItems = new List<ParsedOption>();
            int singleStart = -1;
            char expectedUpper = 'A';
            ParsedOption? currentUpperOpt = null;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Match nhãn in hoa: A., A), [A], <u>A.</u>, etc.
                var matchUpper = Regex.Match(line, @"^(?:<u>|\*|\*\*|\<b\>|\<strong\>|\<em\>)*[\(\[\s]*((?-i:[A-H])|[1-4])(?:\</[a-z0-9]+\>)*[\)\]\.\:\-]+(?:\</[a-z0-9]+\>)*\s*(.*)");
                if (matchUpper.Success)
                {
                    char rawChar = matchUpper.Groups[1].Value[0];
                    char letterChar = char.IsDigit(rawChar) ? (char)('A' + (rawChar - '1')) : rawChar;

                    if (letterChar == expectedUpper)
                    {
                        if (singleStart == -1) singleStart = i;
                        string content = matchUpper.Groups[2].Value.Trim();
                        bool isCorrect = line.Contains("<u>") || line.Contains("<b>") || line.Contains("*");
                        currentUpperOpt = new ParsedOption
                        {
                            Letter = letterChar,
                            Content = content,
                            IsMarkedCorrect = isCorrect
                        };
                        subItems.Add(currentUpperOpt);
                        expectedUpper = (char)(expectedUpper + 1);
                        continue;
                    }
                }

                if (currentUpperOpt != null)
                {
                    currentUpperOpt.Content = string.IsNullOrEmpty(currentUpperOpt.Content) ? line : $"{currentUpperOpt.Content}\n{line}";
                    if (line.Contains("<u>") || line.Contains("<b>") || line.Contains("*"))
                    {
                        currentUpperOpt.IsMarkedCorrect = true;
                    }
                }
            }

            if (subItems.Count >= 2 && subItems[0].Letter == 'A')
            {
                options = subItems;
                startIndex = singleStart;
                return true;
            }

            return false;
        }

        private static void ResolveQuestionBlockBottomUp(
            DocxImportPreviewDto currentQuestion,
            List<string> rawLines,
            string currentSection,
            QuestionParsingRuleDto? matchedRule,
            DynamicQuestionIngestionEngine cig)
        {
            var lines = new List<string>(rawLines.Where(l => !string.IsNullOrWhiteSpace(l)));
            if (lines.Count == 0) return;

            if (currentQuestion.Options.Count > 0)
            {
                while (lines.Count > 0)
                {
                    var lastLine = lines[^1].Trim();
                    if (cig.IsAnswerLine(lastLine, matchedRule, out string ansToken))
                    {
                        currentQuestion.SuggestedAnswer = ansToken;
                        lines.RemoveAt(lines.Count - 1);
                        continue;
                    }
                    if (cig.IsExplanationLine(lastLine, matchedRule))
                    {
                        AppendExplanationLine(currentQuestion, lastLine, matchedRule);
                        lines.RemoveAt(lines.Count - 1);
                        continue;
                    }
                    break;
                }
                foreach (var line in lines)
                {
                    currentQuestion.Content += "\n" + line;
                }
                return;
            }

            // 1. Tách dòng Lời giải hoặc Đáp án ở cuối (nếu có)
            int expStart = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (cig.IsExplanationLine(lines[i], matchedRule) || Regex.IsMatch(lines[i], @"^(?:Lời\s*giải|L[\.\?\s]*i\s*gi[\.\?\s]*i|Hướng\s*dẫn\s*giải|Hu?ng\s*d?n\s*gi?i|HƯỚNG\s*DẪN|LỜI\s*GIẢI|HD|Giải\s*thích|Explanation|Rationale)[\.\:\s\-]*$", RegexOptions.IgnoreCase))
                {
                    expStart = i;
                    break;
                }
            }
            if (expStart >= 0)
            {
                var expLines = lines.Skip(expStart).ToList();
                foreach (var exp in expLines)
                {
                    AppendExplanationLine(currentQuestion, exp, matchedRule);
                    TryExtractAnswer(currentQuestion, exp, matchedRule);
                }
                lines = lines.Take(expStart).ToList();
            }

            while (lines.Count > 0)
            {
                var lastLine = lines[^1].Trim();
                if (cig.IsAnswerLine(lastLine, matchedRule, out string ansToken))
                {
                    if (ansToken.Length == 1 && char.IsLetter(ansToken[0]))
                    {
                        int optIndex = char.ToUpperInvariant(ansToken[0]) - 'A' + 1;
                        currentQuestion.SuggestedAnswer = optIndex.ToString();
                    }
                    else
                    {
                        currentQuestion.SuggestedAnswer = ansToken;
                    }
                    lines.RemoveAt(lines.Count - 1);
                    continue;
                }

                var matchExp = Regex.Match(lastLine, @"^(?:Giải\s*thích|Căn\s*cứ|Ghi\s*chú)[\.\:\s]+(.*)", RegexOptions.IgnoreCase);
                if (matchExp.Success)
                {
                    currentQuestion.AiExplanation = matchExp.Groups[1].Value.Trim();
                    lines.RemoveAt(lines.Count - 1);
                    continue;
                }

                if (cig.IsExplanationLine(lastLine, matchedRule))
                {
                    AppendExplanationLine(currentQuestion, lastLine, matchedRule);
                    lines.RemoveAt(lines.Count - 1);
                    continue;
                }

                break;
            }

            // 2. Trích xuất phương án trắc nghiệm Bottom-Up (từ cuối trở lên kẹp giữa 2 câu)
            if (TryExtractOptionsBottomUp(lines, currentSection, matchedRule, out var extractedOptions, out int optStartIdx, out string detectedType))
            {
                currentQuestion.QuestionType = detectedType;
                foreach (var opt in extractedOptions)
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

                lines = lines.Take(optStartIdx).ToList();
            }

            // 3. Toàn bộ các dòng còn lại thuộc về Thân câu hỏi (Content)
            var contentBuilder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(currentQuestion.Content))
            {
                contentBuilder.Append(currentQuestion.Content);
            }

            foreach (var line in lines)
            {
                if (line.StartsWith("data:image/") || line.StartsWith("![Hình vẽ]") || line.Contains("data:image/"))
                {
                    if (!contentBuilder.ToString().Contains("data:image/"))
                    {
                        contentBuilder.Append("\n\n" + (line.StartsWith("![") ? line : $"![Hình minh họa]({line})"));
                        currentQuestion.ContentType = "image";
                    }
                    else
                    {
                        contentBuilder.Append("\n\n" + line);
                    }
                }
                else
                {
                    if (contentBuilder.Length > 0) contentBuilder.Append("\n");
                    contentBuilder.Append(line);
                }
            }

            currentQuestion.Content = contentBuilder.ToString().Trim();
            if (string.IsNullOrWhiteSpace(currentQuestion.Content))
            {
                currentQuestion.Content = $"Chọn phương án đúng nhất điền vào vị trí ({currentQuestion.SourceQuestionNo}):";
            }

            if (cig.IsCriticalQuestion(currentQuestion.Content, matchedRule))
            {
                currentQuestion.IsCritical = true;
                currentQuestion.Difficulty = 5;
            }

            var subCat = cig.DetectSubCategory(currentQuestion.Content, matchedRule);
            if (!string.IsNullOrEmpty(subCat))
            {
                currentQuestion.SubCategory = subCat;
            }
        }

        public static List<ParsedOption> ExtractOptionsFromLine(string line, QuestionParsingRuleDto? rule = null)
        {
            var options = new List<ParsedOption>();
            if (string.IsNullOrWhiteSpace(line)) return options;

            var cigOpts = DynamicQuestionIngestionEngine.Instance.ExtractOptions(line, rule);
            if (cigOpts.Count > 0)
            {
                foreach (var opt in cigOpts)
                {
                    char rawChar = opt.Letter;
                    char letter = char.IsDigit(rawChar) ? (char)('A' + (rawChar - '1')) : char.ToUpperInvariant(rawChar);
                    options.Add(new ParsedOption
                    {
                        Letter = letter,
                        Content = opt.Content,
                        IsMarkedCorrect = opt.IsMarkedCorrect
                    });
                }
                return options;
            }

            var regex = new Regex(@"(?:^|[\s\t\–\—]+)(?:<u>|\*|\*\*|\<b\>)?(\*?)((?-i:[A-H]|[a-h])|[1-4])(\*?)(?:</u>|\*|\*\*|\</b\>)?[\.\:\)](?:</u>|\</b\>)?[\s\t]+");
            var matches = regex.Matches(line);

            if (matches.Count == 0) return options;

            // Nếu chỉ có 1 nhãn số trên dòng (1., 2...) và quy tắc không phải là GPLX,
            // thì đây là mệnh đề liệt kê trong đề bài (ví dụ: 1. Nước sôi..., 2. Kim loại...), không phải phương án trắc nghiệm
            if (matches.Count == 1 && char.IsDigit(matches[0].Groups[2].Value[0]))
            {
                bool isGplx = rule?.RuleCode == "VN_GPLX_600_STANDARD" || rule?.TargetExamType == "GPLX";
                if (!isGplx) return options;
            }

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                char rawChar = match.Groups[2].Value[0];
                char letter = char.IsDigit(rawChar) ? (char)('A' + (rawChar - '1')) : char.ToUpperInvariant(rawChar);
                bool isCorrect = match.Value.Contains("<u>") || match.Value.Contains("<b>") || match.Groups[1].Value == "*" || match.Groups[3].Value == "*";

                int contentStart = match.Index + match.Length;
                int contentEnd = (i + 1 < matches.Count) ? matches[i + 1].Index : line.Length;

                string content = line.Substring(contentStart, contentEnd - contentStart).Trim();
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

        public static void FinalizeQuestion(DocxImportPreviewDto q, DynamicDomainContext? ctx = null)
        {
            if (q.TopicCode == "GENERAL")
            {
                q.TopicCode = InferTopicFromContent(q.Content, ctx);
            }

            if (q.Options.Any(o => o.Contains("data:image/")))
            {
                q.OptionType = "image";
            }

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

            if (string.IsNullOrWhiteSpace(q.CanonicalContentHash) && !string.IsNullOrWhiteSpace(q.Content))
            {
                q.CanonicalContentHash = ExamDeduplicationEngine.GenerateCanonicalFingerprint(q.Content, q.Options);
            }

            if (string.IsNullOrWhiteSpace(q.DomainCode) || q.DomainCode == "GENERAL")
            {
                q.DomainCode = q.TopicCode;
            }
        }

        public static string ResolveTopicCode(string raw, DynamicDomainContext? ctx = null)
        {
            var clean = raw.Trim();
            if (string.IsNullOrWhiteSpace(clean)) return "GENERAL";

            // 1. Khớp theo danh mục Topics động từ DB của Tenant
            if (ctx?.Topics != null && ctx.Topics.Count > 0)
            {
                var foundTopic = ctx.Topics.FirstOrDefault(t =>
                    t.Name.Contains(clean, StringComparison.OrdinalIgnoreCase)
                    || clean.Contains(t.Name, StringComparison.OrdinalIgnoreCase)
                    || t.Code.Equals(clean, StringComparison.OrdinalIgnoreCase)
                    || t.Keywords.Any(k => clean.Contains(k, StringComparison.OrdinalIgnoreCase)));
                if (foundTopic != null) return foundTopic.Code;
            }

            // 2. Khớp theo danh mục DynamicDomains động từ DB
            if (ctx?.Domains != null && ctx.Domains.Count > 0)
            {
                var foundDomain = ctx.Domains.FirstOrDefault(d =>
                    d.Code.Equals(clean, StringComparison.OrdinalIgnoreCase)
                    || d.Name.Contains(clean, StringComparison.OrdinalIgnoreCase)
                    || clean.Contains(d.Name, StringComparison.OrdinalIgnoreCase)
                    || d.Synonyms.Any(s => clean.Contains(s, StringComparison.OrdinalIgnoreCase)));
                if (foundDomain != null) return foundDomain.Code;
            }

            // 3. Khớp theo TenantCoordinatePresets động từ DB
            if (ctx?.CoordinatePresets != null && ctx.CoordinatePresets.Count > 0)
            {
                var foundPreset = ctx.CoordinatePresets.FirstOrDefault(p =>
                    p.PresetCode.Equals(clean, StringComparison.OrdinalIgnoreCase)
                    || p.PresetLabel.Contains(clean, StringComparison.OrdinalIgnoreCase)
                    || clean.Contains(p.PresetLabel, StringComparison.OrdinalIgnoreCase)
                    || p.Synonyms.Any(s => clean.Contains(s, StringComparison.OrdinalIgnoreCase)));
                if (foundPreset != null) return foundPreset.PresetCode;
            }

            // 4. Chuẩn hóa Topic Code theo slug không dấu (Ví dụ: "Tiếng Anh" -> "TIENG_ANH", "Toán học" -> "MATH")
            var slug = NormalizeToCodeSlug(clean);
            if (slug == "TOAN" || slug == "TOAN_HOC") return "MATH";
            if (!string.IsNullOrEmpty(slug) && slug.Length >= 2 && slug != "DE" && slug != "EXAM")
            {
                return slug;
            }

            // 5. Đối chiếu từ khóa nhanh chuẩn hóa hệ thống (0ms)
            var lower = clean.ToLowerInvariant();
            if (lower.Contains("toán") || lower.Contains("toan") || lower.Contains("giải tích") || lower.Contains("hình học")) return "MATH";
            if (lower.Contains("hóa") || lower.Contains("hoá") || lower.Contains("hoa") || lower.Contains("chemistry")) return "HOA_HOC";
            if (lower.Contains("vật lí") || lower.Contains("vật lý") || lower.Contains("vat li") || lower.Contains("vat ly") || lower.Contains("physics")) return "VAT_LY";
            if (lower.Contains("tiếng anh") || lower.Contains("english") || lower.Contains("ngoại ngữ") || lower.Contains("ngoaingu") || lower.Contains("toeic") || lower.Contains("ielts")) return "TIENG_ANH";
            if (lower.Contains("sinh") || lower.Contains("biology")) return "SINH_HOC";
            if (lower.Contains("sử") || lower.Contains("history")) return "LICH_SU";
            if (lower.Contains("địa") || lower.Contains("dia") || lower.Contains("geography")) return "DIA_LY";
            if (lower.Contains("kinh tế") || lower.Contains("pháp luật") || lower.Contains("gdkt")) return "GDKT_PL";
            if (lower.Contains("tín dụng") || lower.Contains("cho vay")) return "382";
            if (lower.Contains("kế toán") || lower.Contains("ngân quỹ") || lower.Contains("kho tiền")) return "381";
            if (lower.Contains("công nghệ") || lower.Contains("cntt") || lower.Contains("tin học")) return "CNTT";
            if (lower.Contains("đảng") || lower.Contains("chi bộ")) return "PARTY_BUILDING";
            if (lower.Contains("pháp chế") || lower.Contains("luật")) return "LAW";

            return clean;
        }

        private static string NormalizeToCodeSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var ch in normalized)
            {
                var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
                if (ch == 'đ' || ch == 'Đ') sb.Append('D');
                else sb.Append(ch);
            }
            var cleanAscii = sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
            return Regex.Replace(cleanAscii, @"[^a-zA-Z0-9]+", "_").Trim('_').ToUpperInvariant();
        }

        public static string InferTopicFromContent(string content, DynamicDomainContext? ctx = null)
        {
            if (string.IsNullOrWhiteSpace(content)) return "GENERAL";

            // 1. Ưu tiên đối chiếu danh mục Topics động của Tenant
            if (ctx?.Topics != null && ctx.Topics.Count > 0)
            {
                var lowerContent = content.ToLowerInvariant();
                foreach (var t in ctx.Topics)
                {
                    if (t.Keywords.Any(k => lowerContent.Contains(k.ToLowerInvariant())) || lowerContent.Contains(t.Name.ToLowerInvariant()))
                        return t.Code;
                }
            }

            var lower = content.ToLowerInvariant();

            if (Regex.IsMatch(lower, @"\b(mark\s+the\s+letter|pronunciation|primary\s+stress|closest\s+in\s+meaning|opposite\s+in\s+meaning|underlined\s+part|synonym|antonym|read\s+the\s+following\s+passage|in\s+paragraph|according\s+to\s+paragraph)\b"))
                return "TIENG_ANH";

            if (lower.Contains("hóa học") || lower.Contains("hoá học") || lower.Contains("axit") || lower.Contains("acid") || lower.Contains("ancol") || lower.Contains("alcohol")
                || lower.Contains("este") || lower.Contains("ester") || lower.Contains("amino acid") || lower.Contains("polime") || lower.Contains("polymer") || lower.Contains("thế điện cực")
                || lower.Contains("galvani") || lower.Contains("chỉ số octane") || lower.Contains("số sóng") || lower.Contains("phổ hồng ngoại") || lower.Contains("chất giặt rửa")
                || lower.Contains("chất hữu cơ") || lower.Contains("chất vô cơ") || lower.Contains("glycine") || lower.Contains("glucose") || lower.Contains("fructose")
                || lower.Contains("dung dịch") || lower.Contains("kết tủa") || lower.Contains("kim loại") || lower.Contains("đồng đẳng") || lower.Contains("đồng phân"))
                return "HOA_HOC";

            if (lower.Contains("vật lí") || lower.Contains("vật lý") || lower.Contains("nhiệt lượng") || lower.Contains("nhiệt dung") || lower.Contains("chuyển thể")
                || lower.Contains("khí lí tưởng") || lower.Contains("đẳng nhiệt") || lower.Contains("đẳng tích") || lower.Contains("đẳng áp") || lower.Contains("phóng xạ")
                || lower.Contains("hạt nhân") || lower.Contains("tia alpha") || lower.Contains("tia beta") || lower.Contains("tia gamma") || lower.Contains("quang điện")
                || lower.Contains("lực từ") || lower.Contains("cảm ứng từ") || lower.Contains("suất điện động") || lower.Contains("từ trường") || lower.Contains("bước sóng")
                || lower.Contains("tần số") || lower.Contains("dao động") || lower.Contains("dòng điện"))
                return "VAT_LY";

            if (lower.Contains("hàm số") || lower.Contains("đồ thị") || lower.Contains("hình lập phương") || lower.Contains("hình chóp") || lower.Contains("lăng trụ")
                || lower.Contains("tích phân") || lower.Contains("đạo hàm") || lower.Contains("nguyên hàm") || lower.Contains("xác suất") || lower.Contains("tiệm cận")
                || lower.Contains("vectơ") || lower.Contains("vector") || lower.Contains("phương trình") || lower.Contains("bất phương trình") || lower.Contains("tọa độ")
                || lower.Contains("oxyz") || lower.Contains("bảng biến thiên") || lower.Contains("cực trị") || lower.Contains("tối giản"))
                return "MATH";

            if (lower.Contains("đảng") || lower.Contains("chi bộ") || lower.Contains("đảng viên"))
                return "PARTY_BUILDING";

            if (lower.Contains("kho tiền") || lower.Contains("chìa khóa") || lower.Contains("niêm phong"))
                return "381";

            if (lower.Contains("tín dụng") || lower.Contains("thế chấp") || lower.Contains("cho vay") || lower.Contains("nợ xấu"))
                return "382";

            if (lower.Contains("giao thức") || lower.Contains("mật khẩu") || lower.Contains("tấn công") || lower.Contains("cntt") || lower.Contains("mã độc"))
                return "CNTT";

            return "GENERAL";
        }

        public static void AutoEnrichTaxonomyAndTags(DocxImportPreviewDto q, DynamicDomainContext? ctx = null)
        {
            q.Tags ??= new List<string>();

            // Chuyển việc phân tích Tọa độ và Thẻ sang UniversalCoordinateTaxonomyEngine (hỗ trợ DynamicDomainContext)
            var sniffed = UniversalCoordinateTaxonomyEngine.SniffCoordinates(
                fileName: null,
                sheetName: q.TopicCode,
                headerBannerLines: new[] { q.ContextTitle ?? "", q.SubCategory ?? "" },
                sampleContents: new[] { q.Content },
                sampleReferences: string.IsNullOrWhiteSpace(q.AiExplanation) ? null : new[] { q.AiExplanation },
                dynamicContext: ctx);

            if (string.IsNullOrWhiteSpace(q.DomainCode) || q.DomainCode == "GENERAL")
            {
                q.DomainCode = sniffed.DomainCode;
            }

            if (string.IsNullOrWhiteSpace(q.TargetLevel))
            {
                q.TargetLevel = sniffed.TargetLevel;
            }

            q.AssessmentPurpose ??= sniffed.AssessmentPurpose;
            q.IssuingOrg ??= sniffed.IssuingOrg;
            q.BenchmarkYear ??= sniffed.BenchmarkYear;
            q.BenchmarkStandard ??= sniffed.BenchmarkStandard;

            foreach (var t in sniffed.Tags)
            {
                AddTagIfNotExists(q.Tags, t);
            }
        }

        public static void AutoEnrichTaxonomyAndTags(DocxImportPreviewDto q)
        {
            AutoEnrichTaxonomyAndTags(q, null);
        }

        private static void AddTagIfNotExists(List<string> tags, string tag)
        {
            if (!tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
            {
                tags.Add(tag);
            }
        }
    }
}
