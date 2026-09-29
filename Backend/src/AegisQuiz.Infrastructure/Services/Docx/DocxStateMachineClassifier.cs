using System;
using System.Collections.Generic;
using System.Linq;
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

            foreach (var rawLine in paragraphs)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // 0. Nhận dạng Tiêu đề Đề thi động qua CIG: "ĐỀ 1", "EXAM 02", "TEST 3"...
                if (cig.TryMatchExamHeading(line, out string examNumStr) && int.TryParse(examNumStr, out int examNum))
                {
                    if (currentQuestion != null && !isSolutionPhase)
                    {
                        FinalizeAndMergeQuestion(results, questionMap, currentQuestion, currentExamCode, currentQuestionNumber, dynamicContext);
                        examQuestionsByNum[currentQuestionNumber] = currentQuestion;
                        currentQuestion = null;
                        currentTopicCode = "GENERAL"; // [Sprint 1 - 7.3] Reset topic moi lan doi de
                    }

                    // [Sprint 1 - 7.2] Xay examPrefix dong tu DynamicDomainContext
                    string examPrefix = "DE_";
                    if (dynamicContext != null && !string.IsNullOrWhiteSpace(dynamicContext.DefaultExamPrefix)
                        && dynamicContext.DefaultExamPrefix != "EXAM" && dynamicContext.DefaultExamPrefix != "DE")
                    {
                        examPrefix = $"{dynamicContext.DefaultExamPrefix}_";
                    }
                    currentExamCode = $"{examPrefix}{examNum:D2}";
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

                // 0b. Nhận dạng Tiêu đề Phần Bảng Đáp án hoặc Lời giải tham khảo qua CIG
                bool isQuestionLocalExplanation = currentQuestion != null && cig.IsExplanationLine(line, currentMatchedRule);
                bool isQuestionLocalAnswer = currentQuestion != null && cig.IsAnswerLine(line, currentMatchedRule, out _);
                bool isSolPhaseMatch = !isQuestionLocalExplanation && !isQuestionLocalAnswer && cig.TryMatchSolutionPhase(line);
                bool isAnsTableMatch = !isQuestionLocalExplanation && !isQuestionLocalAnswer && cig.TryMatchAnswerTablePhase(line);
                if (isSolPhaseMatch || isAnsTableMatch)
                {
                    if (currentQuestion != null && !isSolutionPhase)
                    {
                        FinalizeAndMergeQuestion(results, questionMap, currentQuestion, currentExamCode, currentQuestionNumber, dynamicContext);
                        examQuestionsByNum[currentQuestionNumber] = currentQuestion;
                        currentQuestion = null;
                    }
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
                    if (currentQuestion != null && !isSolutionPhase)
                    {
                        FinalizeAndMergeQuestion(results, questionMap, currentQuestion, currentExamCode, currentQuestionNumber, dynamicContext);
                        examQuestionsByNum[currentQuestionNumber] = currentQuestion;
                        currentQuestion = null;
                    }
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
                    if (currentQuestion != null && !isSolutionPhase)
                    {
                        FinalizeAndMergeQuestion(results, questionMap, currentQuestion, currentExamCode, currentQuestionNumber, dynamicContext);
                        examQuestionsByNum[currentQuestionNumber] = currentQuestion;
                        currentQuestion = null;
                    }
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
                                AppendExplanationLine(currentQuestion, remainder, matchedRule);
                                TryExtractAnswer(currentQuestion, remainder, matchedRule);
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
                        FinalizeAndMergeQuestion(results, questionMap, currentQuestion, currentExamCode, currentQuestionNumber, dynamicContext);
                        examQuestionsByNum[currentQuestionNumber] = currentQuestion;
                    }

                    currentQuestionNumber = qNum;
                    inExplanation = false;

                    var contentText = remainderText;
                    var sameLineOptions = (currentSection == "SINGLE" || currentSection == "MULTI")
                        ? ExtractOptionsFromLine(contentText, matchedRule)
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
                    if (answerKeyMap != null &&
                        answerKeyMap.TryGetValue(currentExamCode, out var secMap) &&
                        secMap.TryGetValue(currentSection, out var qAnsMap) &&
                        qAnsMap.TryGetValue(currentQuestionNumber, out var tableAns))
                    {
                        initialAnswer = tableAns;
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
                        Content = questionBody,
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
                    if (isSolutionPhase)
                    {
                        AppendExplanationLine(currentQuestion, line, currentMatchedRule);
                        TryExtractAnswer(currentQuestion, line, currentMatchedRule);
                        continue;
                    }

                    if (cig.IsExplanationLine(line, currentMatchedRule))
                    {
                        inExplanation = true;
                        var remainder = Regex.Replace(line, @"^(?:Lời\s*giải|L[\.\?\s]*i\s*gi[\.\?\s]*i|Hướng\s*dẫn\s*giải|Hu?ng\s*d?n\s*gi?i|HƯỚNG\s*DẪN|LỜI\s*GIẢI|HD|Giải\s*thích|Căn\s*cứ|Explanation|Rationale)[\.\:\s\-]*", "", RegexOptions.IgnoreCase).Trim();
                        if (!string.IsNullOrEmpty(remainder))
                        {
                            AppendExplanationLine(currentQuestion, remainder, currentMatchedRule);
                            TryExtractAnswer(currentQuestion, remainder, currentMatchedRule);
                        }
                        continue;
                    }

                    if (inExplanation)
                    {
                        AppendExplanationLine(currentQuestion, line, currentMatchedRule);
                        TryExtractAnswer(currentQuestion, line, currentMatchedRule);
                        continue;
                    }

                    if (cig.IsAnswerLine(line, currentMatchedRule, out string ansToken))
                    {
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

                    var matchExplanationLine = Regex.Match(line, @"^(?:Giải\s*thích|Căn\s*cứ|Ghi\s*chú)[\.\:\s]+(.*)", RegexOptions.IgnoreCase);
                    if (matchExplanationLine.Success)
                    {
                        currentQuestion.AiExplanation = matchExplanationLine.Groups[1].Value.Trim();
                        continue;
                    }

                    if (currentQuestion.QuestionType == "SINGLE" || currentQuestion.QuestionType == "MULTI")
                    {
                        var parsedOptions = ExtractOptionsFromLine(line, currentMatchedRule);
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

                    if (currentQuestion.QuestionType == "TRUE_FALSE" || currentQuestion.QuestionType == "MULTI_TRUE_FALSE")
                    {
                        var matchSubItem = Regex.Match(line, @"^([a-z0-9]+)[\)\.]\s*(.*)", RegexOptions.IgnoreCase);
                        if (matchSubItem.Success)
                        {
                            string itemLetter = matchSubItem.Groups[1].Value.ToLowerInvariant();
                            string itemContent = matchSubItem.Groups[2].Value.Trim();
                            currentQuestion.Options.Add($"{itemLetter}) {itemContent}");
                            currentQuestion.QuestionType = "MULTI_TRUE_FALSE";
                            if (itemContent.Contains("data:image/"))
                            {
                                currentQuestion.OptionType = "image";
                            }
                            continue;
                        }
                    }

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

                    if (currentQuestion.Options.Count == 0)
                    {
                        currentQuestion.Content += "\n" + line;
                        if (!currentQuestion.IsCritical && cig.IsCriticalQuestion(line, currentMatchedRule))
                        {
                            currentQuestion.IsCritical = true;
                            currentQuestion.Difficulty = 5;
                        }
                    }
                    else
                    {
                        currentQuestion.Options[currentQuestion.Options.Count - 1] += " " + line;
                    }
                }
            }

            if (currentQuestion != null && !isSolutionPhase)
            {
                FinalizeAndMergeQuestion(results, questionMap, currentQuestion, currentExamCode, currentQuestionNumber, dynamicContext);
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

        public static void TryExtractAnswer(DocxImportPreviewDto q, string line, QuestionParsingRuleDto? rule = null)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            if (DynamicQuestionIngestionEngine.Instance.TryExtractInlineAnswer(line, q.QuestionType, rule, out string inlineAns))
            {
                if (q.QuestionType == "TRUE_FALSE")
                {
                    var matchTf = Regex.Match(inlineAns, @"^([a-d])[\:\)]\s*(.+)", RegexOptions.IgnoreCase);
                    if (matchTf.Success)
                    {
                        string itemLetter = matchTf.Groups[1].Value.ToLowerInvariant();
                        string val = matchTf.Groups[2].Value.Trim();
                        string formattedVal = val.StartsWith("Đ", StringComparison.OrdinalIgnoreCase) || val.StartsWith("D", StringComparison.OrdinalIgnoreCase) ? "Đúng" : "Sai";
                        var parts = (q.SuggestedAnswer ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .Where(p => !p.StartsWith(itemLetter + ":", StringComparison.OrdinalIgnoreCase) && !p.StartsWith(itemLetter + ")", StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        parts.Add($"{itemLetter}) {formattedVal}");
                        parts.Sort();
                        q.SuggestedAnswer = string.Join(", ", parts);
                        return;
                    }
                }
                q.SuggestedAnswer = inlineAns;
                return;
            }

            // Fallback tương thích ngược nếu rule không trích xuất được
            if (q.QuestionType == "SINGLE")
            {
                var matchChon = Regex.Match(line, @"(?:Chọn|Ch[oọ\?]n|Choose|Option)\s*(?:câu\s*|đáp\s*án\s*)?([A-D])\b", RegexOptions.IgnoreCase);
                if (matchChon.Success)
                {
                    char letter = char.ToUpperInvariant(matchChon.Groups[1].Value[0]);
                    q.SuggestedAnswer = (letter - 'A' + 1).ToString();
                    return;
                }

                var matchDapAn = Regex.Match(line, @"(?:suy\s*ra\s*)?(?:đáp\s*án|dap\s*an|d[\?\.]*p\s*[\?\.]*n|answer|key)(?:\s*(?:đúng\s*)?là)?[\:\s]*([A-D])\b", RegexOptions.IgnoreCase);
                if (matchDapAn.Success)
                {
                    char letter = char.ToUpperInvariant(matchDapAn.Groups[1].Value[0]);
                    q.SuggestedAnswer = (letter - 'A' + 1).ToString();
                    return;
                }

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
                var matchShort = Regex.Match(line, @"^(?:Đáp\s*số|Đáp\s*án|Dap\s*so|Dap\s*an|Da['\s]*p\s*a['\s]*n|Kết\s*quả)(?:\s*bằng)?[\.\:\s]+([0-9\,\.\-]+)", RegexOptions.IgnoreCase);
                if (matchShort.Success)
                {
                    q.SuggestedAnswer = matchShort.Groups[1].Value.Trim();
                    return;
                }
            }
            else if (q.QuestionType == "TRUE_FALSE" || q.QuestionType == "MULTI_TRUE_FALSE")
            {
                var matchTf = Regex.Match(line, @"(?:^|\b)([a-z0-9]+)[\)\.]?.*?\b(?:mệnh\s*đề\s+|khẳng\s*định\s+)?(đúng|sai|d|s|true|false)\b", RegexOptions.IgnoreCase);
                if (matchTf.Success)
                {
                    string itemLetter = matchTf.Groups[1].Value.ToLowerInvariant();
                    string valToken = matchTf.Groups[2].Value.ToLowerInvariant();
                    string val = (valToken.StartsWith("đ") || valToken.StartsWith("d") || valToken.StartsWith("t")) ? "Đúng" : "Sai";

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
                results.Add(q);
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

            var regex = new Regex(@"(?:^|[\s\t\–\—]+)(\*?)((?-i:[A-H])|[1-4])(\*?)[\.\:\)][\s\t]+");
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

            AutoEnrichTaxonomyAndTags(q, ctx);
        }

        public static string ResolveTopicCode(string raw, DynamicDomainContext? ctx = null)
        {
            var clean = raw.Trim();
            if (string.IsNullOrWhiteSpace(clean)) return "GENERAL";

            if (ctx?.Topics != null && ctx.Topics.Count > 0)
            {
                var foundTopic = ctx.Topics.FirstOrDefault(t =>
                    t.Name.Contains(clean, StringComparison.OrdinalIgnoreCase)
                    || clean.Contains(t.Name, StringComparison.OrdinalIgnoreCase)
                    || t.Code.Equals(clean, StringComparison.OrdinalIgnoreCase)
                    || t.Keywords.Any(k => clean.Contains(k, StringComparison.OrdinalIgnoreCase)));
                if (foundTopic != null) return foundTopic.Code;
            }

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

        public static string InferTopicFromContent(string content, DynamicDomainContext? ctx = null)
        {
            if (string.IsNullOrWhiteSpace(content)) return "GENERAL";

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

            if (lowerContains(lower, "giao thức") || lowerContains(lower, "mật khẩu") || lowerContains(lower, "tấn công") || lowerContains(lower, "cntt") || lowerContains(lower, "mã độc"))
                return "CNTT";

            return "GENERAL";

            static bool lowerContains(string s, string p) => s.Contains(p);
        }

        public static void AutoEnrichTaxonomyAndTags(DocxImportPreviewDto q, DynamicDomainContext? ctx = null)
        {
            q.Tags ??= new List<string>();

            // Chuyển toàn bộ việc phân tích Tọa độ và Thẻ sang UniversalCoordinateTaxonomyEngine (hỗ trợ DynamicDomainContext)
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
