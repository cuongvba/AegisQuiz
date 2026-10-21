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
    public static partial class DocxStateMachineClassifier
    {
        public class ParsedOption : ParsedOptionDto
        {
        }

        public static List<string> LastDebugLogs { get; } = new();

        public static List<DocxImportPreviewDto> ParseParagraphList(
            List<string> paragraphs,
            Dictionary<string, Dictionary<string, Dictionary<int, string>>>? answerKeyMap = null,
            DynamicDomainContext? dynamicContext = null,
            DynamicQuestionIngestionEngine? engineOverride = null)
        {
            LastDebugLogs.Clear();
            var results = new List<DocxImportPreviewDto>();
            var questionMap = new Dictionary<string, DocxImportPreviewDto>(StringComparer.OrdinalIgnoreCase);
            var cig = engineOverride ?? DynamicQuestionIngestionEngine.Instance;

            DocxImportPreviewDto? currentQuestion = null;
            int currentQuestionNumber = 0;
            string currentSection = "SINGLE"; // SINGLE, TRUE_FALSE, SHORT_ANSWER, ESSAY
            string? currentSectionTitle = null;
            string currentExamCode = "DEFAULT";
            string currentTopicCode = "GENERAL";
            string? documentSubject = null;
            string? currentSubCategory = null;
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

            // [Zero-Hardcode Ingestion Directives] Nạp mẫu chỉ thị ngắt bài đọc hiểu từ cấu hình Rule CIG
            var instructionDirectivePatterns = activeRules
                .SelectMany(r => r.StimulusSpec?.InstructionDirectivePatterns ?? Enumerable.Empty<string>())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .ToList();
            if (instructionDirectivePatterns.Count == 0)
            {
                instructionDirectivePatterns.Add(@"^(?:Mark\s+the\s+letter|Choose\s+the\s+letter|Choose\s+the\s+word|Read\s+the\s+following\s+(?:passage|text|announcement|advertisement|leaflet)|Đọc\s+(?:đoạn\s+văn|bài\s+đọc)|(?:Sử\s*dụng|Dựa\s*(?:vào|trên)|Cho|Dùng)\s+(?:các\s+)?(?:thông\s*tin|ngữ\s*liệu|dữ\s*kiện|đoạn\s*văn))\b");
            }

            // [Zero-Hardcode End of Exam & Proctor Sentinels] Nạp mẫu kết thúc đề và chỉ thị coi thi từ Rule CIG & StructureDictionary
            var endOfExamPatterns = activeRules
                .SelectMany(r => r.LifecycleSpec?.EndOfExamPatterns ?? Enumerable.Empty<string>())
                .Concat(cig.StructureDictionary.EndOfExamMarkers ?? Enumerable.Empty<string>())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .ToList();
            if (endOfExamPatterns.Count == 0)
            {
                endOfExamPatterns.Add(@"^(?:[\-\–\—_*\=.\s]*\b(?:HẾT|HET|THE\s*END|END\s*OF\s*(?:TEST|EXAM|PAPER)|HẾT\s+ĐỀ\s+THI)\b[\-\–\—_*\=.\s]*)$");
                endOfExamPatterns.Add(@"^(?:[\-\–\—_\s]*HẾT[\-\–\—_\s]*)$");
            }

            var proctorInstructionPatterns = activeRules
                .SelectMany(r => r.LifecycleSpec?.ProctorInstructionPatterns ?? Enumerable.Empty<string>())
                .Concat(cig.StructureDictionary.ProctorInstructionMarkers ?? Enumerable.Empty<string>())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .ToList();
            if (proctorInstructionPatterns.Count == 0)
            {
                proctorInstructionPatterns.Add(@"(?i)(?:Cán\s*bộ\s*coi\s*thi|Giám\s*thị|Giám\s*khảo)\s+không\s+giải\s+thích(?:\s+gì)?\s+thêm");
                proctorInstructionPatterns.Add(@"(?i)Thí\s*sinh\s+không\s+được\s+sử\s+dụng\s+tài\s+liệu");
                proctorInstructionPatterns.Add(@"(?i)Họ\s*(?:và)?\s*tên\s+thí\s*sinh[\.\:\s_]+Số\s*báo\s*danh");
            }

            // [CHIP v4.0 - Early Reconnaissance & Header Profiling via CIG]
            // Ưu tiên tuyệt đối thông tin môn thi tuyên bố tường minh trên Header (Tầng 1 - SSoT)
            var headerProfile = cig.ProfileHeader(paragraphs.Take(35), paragraphs.Skip(35).Take(15), dynamicContext);
            var headerTaxonomy = headerProfile.TaxonomyCoordinates;
            int headerDurationSeconds = headerProfile.DurationSeconds;
            string? headerDetectedExamCode = headerProfile.DetectedExamCode;

            if (!string.IsNullOrEmpty(headerProfile.DetectedSubject) && headerProfile.DetectedSubject != "GENERAL")
            {
                documentSubject = headerProfile.DetectedSubject;
                currentTopicCode = headerProfile.DetectedSubject;
            }
            else if (languageProfile.SubjectLanguage == "en" && currentTopicCode == "GENERAL")
            {
                // Chỉ dự phòng TIENG_ANH khi Header hoàn toàn không có thông tin môn học
                var englishTopic = dynamicContext?.Topics?.FirstOrDefault(t => t.Code == "TIENG_ANH" || t.Code == "ENGLISH");
                string enCode = englishTopic?.Code ?? "TIENG_ANH";
                currentTopicCode = enCode;
                documentSubject = enCode;
            }

            if (!string.IsNullOrEmpty(headerDetectedExamCode) && currentExamCode == "DEFAULT")
            {
                currentExamCode = headerDetectedExamCode;
            }

            var currentQuestionPendingLines = new List<string>();
            string? currentSubItemExplKey = null;

            Action finalizeCurrentQuestion = () =>
            {
                if (currentQuestion != null && !isSolutionPhase)
                {
                    LastDebugLogs.Add($"[FINALIZE] QNum={currentQuestionNumber}, Type={currentQuestion.QuestionType}, PendingLines={currentQuestionPendingLines.Count}, Content={currentQuestion.Content}");
                    cig.ResolveQuestionBlockBottomUp(currentQuestion, currentQuestionPendingLines, currentSection, currentMatchedRule);
                    cig.FinalizeAndMergeQuestion(results, questionMap, currentQuestion, currentExamCode, currentQuestionNumber, dynamicContext, currentMatchedRule);
                    examQuestionsByNum[currentQuestionNumber] = currentQuestion;
                    LastDebugLogs.Add($"[AFTER MERGE] ResultsCount={results.Count}");
                    currentQuestion = null;
                    currentQuestionPendingLines.Clear();
                    currentSubItemExplKey = null;
                }
                else
                {
                    LastDebugLogs.Add($"[FINALIZE SKIPPED] currentQuestion={(currentQuestion == null ? "NULL" : "EXISTS")}, isSolutionPhase={isSolutionPhase}");
                }
            };

            // [SGI v5.0 - Cognitive Scope Orchestrator]
            // Khởi tạo bộ điều phối phạm vi nhận thức đa tầng thông qua CIG Engine Factory
            var scopeManager = cig.CreateScopeManager();

            // [Phase 4 Enhancements - Structural Boundary Streamer via CIG]
            var flattenedLines = cig.FlattenDocument(paragraphs, languageProfile);

            foreach (var rawLine in flattenedLines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                LastDebugLogs.Add($"[LINE] '{line}'");

                // 00. Bỏ qua dòng/bảng Header thông tin thí sinh, số báo danh, thời gian làm bài nếu chưa vào câu hỏi đầu tiên
                if (currentQuestion == null && cig.IsStudentInfoLine(line))
                {
                    if (cig.TryExtractExamCode(line, out var cCode) && !string.IsNullOrEmpty(cCode))
                    {
                        currentExamCode = cCode;
                    }
                    continue;
                }

                // 0. Nhận dạng Tiêu đề Đề thi động qua CIG: "ĐỀ 1", "EXAM 02", "TEST 3"...
                if (cig.TryMatchExamHeading(line, out string examNumStr) && int.TryParse(examNumStr, out int examNum))
                {
                    if (cig.IsYearLikeNumber(examNum))
                    {
                        continue; // Bỏ qua năm học (2024..2026)
                    }

                    finalizeCurrentQuestion();
                    currentTopicCode = documentSubject ?? "GENERAL"; // Bảo toàn môn học đã nhận diện ở đầu tài liệu khi đổi đề
                    currentSubCategory = null;

                    string targetExamCode = cig.BuildExamCode(examNum, dynamicContext);

                    bool alreadyHasQuestions = results.Any(r => r.SourceExamName == targetExamCode);
                    if (isSolutionPhase && !alreadyHasQuestions)
                    {
                        // Đang ở pha giải nhưng gặp đề mới chưa từng có ở pha đề bài -> bắt đầu đề bài mới
                        isSolutionPhase = false;
                    }

                    currentExamCode = targetExamCode;
                    currentSection = "SINGLE";
                    currentSectionTitle = null;
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

                // 0a. Nhận dạng Dấu hiệu Kết thúc Đề thi (End of Exam Sentinel: "HẾT", "---------- HẾT ----------", "THE END"...)
                // Đóng gói câu hỏi trước đó ngay lập tức và ngắt dòng, tuyệt đối không đưa vào nội dung câu hỏi cuối cùng!
                bool isEndOfExam = cig.TryMatchEndOfExam(line) || endOfExamPatterns.Any(p => Regex.IsMatch(line, p, RegexOptions.IgnoreCase));
                if (isEndOfExam)
                {
                    finalizeCurrentQuestion();
                    inExplanation = false;
                    currentReadingPassage = null;
                    passageStartQ = 0;
                    passageEndQ = 0;
                    continue;
                }

                // 0a1. Nhận dạng Chỉ thị Coi thi / Ghi chú phụ ("Cán bộ coi thi không giải thích gì thêm", "Thí sinh không được sử dụng tài liệu"...)
                bool isProctorLine = cig.IsProctorDisclaimerLine(line) || proctorInstructionPatterns.Any(p => Regex.IsMatch(line, p, RegexOptions.IgnoreCase));
                if (isProctorLine)
                {
                    continue; // Bỏ qua hoàn toàn dòng chỉ thị coi thi/thí sinh
                }

                // 0a2. Nhận diện dòng Đáp án inline ngay dưới câu hỏi trước khi kiểm tra chuyển Phase
                var cleanLinePre = Regex.Replace(line, @"^<[a-z0-9]+>", "", RegexOptions.IgnoreCase).Trim();
                if (currentQuestion != null && !isSolutionPhase && (cig.IsAnswerLine(cleanLinePre, currentMatchedRule, out string inlineAnsPre) || LinguisticMatch(cig, LinguisticKeys.ChoiceAnswerLine, cleanLinePre).Success))
                {
                    if (string.IsNullOrEmpty(inlineAnsPre))
                    {
                        var mAns = LinguisticMatch(cig, LinguisticKeys.ChoiceAnswerLine, cleanLinePre);
                        if (mAns.Success) inlineAnsPre = mAns.Groups[1].Value.Trim();
                    }
                    if (!string.IsNullOrWhiteSpace(inlineAnsPre))
                    {
                        if (inlineAnsPre.Length == 1 && char.IsLetter(inlineAnsPre[0]))
                        {
                            currentQuestion.SuggestedAnswer = (char.ToUpperInvariant(inlineAnsPre[0]) - 'A' + 1).ToString();
                        }
                        else
                        {
                            currentQuestion.SuggestedAnswer = inlineAnsPre;
                        }
                    }
                    continue;
                }

                // 0b. Nhận dạng Tiêu đề Phần Bảng Đáp án hoặc Lời giải tham khảo qua CIG
                // Tiêu đề Phase (LỜI GIẢI THAM KHẢO, ĐÁP ÁN THAM KHẢO, BẢNG ĐÁP ÁN) có mức ưu tiên tài liệu cao nhất,
                // đánh dấu kết thúc pha đề bài và bắt đầu pha lời giải/đáp án chi tiết.
                bool isSolPhaseMatch = cig.TryMatchSolutionPhase(line);
                bool isAnsTableMatch = cig.TryMatchAnswerTablePhase(line);

                if (isSolPhaseMatch || isAnsTableMatch)
                {
                    // [Sovereignty Rule]: Nếu đã đang ở trong Pha Lời giải (isSolutionPhase == true):
                    // Các dòng như "Hướng dẫn giải", "Lời giải", "Đáp án" đứng độc lập bên dưới câu hỏi
                    // TUYỆT ĐỐI KHÔNG được reset currentSection về "SINGLE".
                    // Nếu dòng đó có chứa định danh phân đoạn rõ ràng (ví dụ "HƯỚNG DẪN GIẢI PHẦN III"):
                    // -> Chuyển sang phân đoạn đó.
                    // Nếu dòng đó KHÔNG chứa phân đoạn mới (chỉ là "Hướng dẫn giải" của câu hỏi đang xét):
                    // -> Đây là dòng bắt đầu giải thích (inExplanation = true) của câu hỏi hiện tại!
                    if (isSolutionPhase && isSolPhaseMatch)
                    {
                        if (cig.TryMatchSectionHeader(line, out string secInSol))
                        {
                            finalizeCurrentQuestion();
                            currentSection = secInSol;
                            var mSecTitle = LinguisticMatch(cig, LinguisticKeys.SectionTitle, line);
                            currentSectionTitle = mSecTitle.Success ? mSecTitle.Groups[1].Value.ToUpperInvariant() : secInSol;
                            inExplanation = false;
                            continue;
                        }
                        else
                        {
                            inExplanation = true;
                            continue;
                        }
                    }

                    // Nếu chưa vào pha Lời giải nhưng đang có câu hỏi inline mở, và dòng giải thích không phải macro phase header
                    if (!isSolutionPhase && currentQuestion != null && isSolPhaseMatch)
                    {
                        if (!LinguisticMatch(cig, LinguisticKeys.MacroPhaseHint, line).Success)
                        {
                            inExplanation = true;
                            continue;
                        }
                    }

                    finalizeCurrentQuestion();
                    if (isSolPhaseMatch)
                    {
                        isSolutionPhase = true;
                        // Nếu tiêu đề pha có chứa kèm chỉ định Phân đoạn (ví dụ: "HƯỚNG DẪN GIẢI PHẦN III")
                        if (cig.TryMatchSectionHeader(line, out string secInSol))
                        {
                            currentSection = secInSol;
                            var mSecTitle = LinguisticMatch(cig, LinguisticKeys.SectionTitle, line);
                            currentSectionTitle = mSecTitle.Success ? mSecTitle.Groups[1].Value.ToUpperInvariant() : secInSol;
                        }
                        else
                        {
                            currentSection = "SINGLE"; // Mặc định reset về Phần I khi bắt đầu pha lời giải
                            currentSectionTitle = null;
                        }
                    }
                    inExplanation = false;
                    currentReadingPassage = null;
                    passageStartQ = 0;
                    passageEndQ = 0;
                    continue;
                }

                var upper = line.ToUpperInvariant();
                bool isQuestionLine = cig.TryMatchQuestionAnchor(line, out _, out _, activeRules);
                bool isExplanationLine = cig.IsExplanationLine(line);

                // 1. Nhận dạng thay đổi Chủ đề qua CIG: "Chủ đề: Toán học", "Môn: Tiếng Anh", "Topic: Science"...
                if (!isQuestionLine && cig.TryMatchTopicHeading(line, out string rawTopic))
                {
                    finalizeCurrentQuestion();
                    string resolvedTopic = ResolveTopicCode(rawTopic, dynamicContext);

                    // Phân biệt ranh giới: Nếu dòng bắt đầu bằng các nhãn phân mục con ("Chủ đề 1", "Chương 2", "Bài 3", "Module 4", "Phần I"...)
                    // -> Đây là tiêu đề phân mục con (SubCategory), TUYỆT ĐỐI KHÔNG làm phân mảnh hay đổi môn học của đề!
                    bool isSubCategoryHeading = Regex.IsMatch(line, @"^(?:[\t\s|•\-–—;]*)(?:CHỦ\s*ĐỀ|CHU\s*DE|CHƯƠNG|CHUONG|BÀI|BAI|MODULE|PHẦN|PART|SECTION)\s*(?:[0-9IVXABCD]+)?\b", RegexOptions.IgnoreCase);

                    if (!isSubCategoryHeading && resolvedTopic != "GENERAL" && resolvedTopic != "EDUCATION" && resolvedTopic != "THPT_NATIONAL")
                    {
                        // Khớp Môn học thực sự mới (ví dụ trong đề thi tổ hợp KHTN: "Môn: Hóa học")
                        currentTopicCode = resolvedTopic;
                        currentSubCategory = null;
                    }
                    else
                    {
                        // Tiêu đề Phân mục / Bài học / Chuyên đề con (VD: "Chủ đề 1: Máy tính và cộng đồng", "Chương II: ...")
                        // Lưu trữ trọn vẹn toàn bộ dòng vào SubCategory của các câu hỏi tiếp theo!
                        currentSubCategory = line.Trim();
                        currentSectionTitle = line.Trim();
                    }
                    continue;
                }


                // 2b. Nhận dạng Section Headers động từ CIG & StructureDictionary (hỗ trợ GDPT 2025, Ngoại ngữ, SAT, Gaokao)
                // Ưu tiên cao hơn isExplanationLine để nhận diện chính xác các tiêu đề như "PHẦN III. HƯỚNG DẪN GIẢI CHI TIẾT"
                bool isSecHeader = cig.TryMatchSectionHeader(line, out string detectedSection);
                if (!isQuestionLine && isSecHeader)
                {
                    finalizeCurrentQuestion();
                    currentQuestion = null; // Reset câu hỏi hiện tại để tránh tràn lời giải xuyên phân đoạn
                    currentSection = detectedSection;
                    scopeManager.RegisterMacroSection(detectedSection, line);

                    var mSecTitle = LinguisticMatch(cig, LinguisticKeys.SectionTitle, line);
                    currentSectionTitle = mSecTitle.Success ? mSecTitle.Groups[1].Value.ToUpperInvariant() : (line.Length > 30 ? line.Substring(0, 30) : line);
                    inExplanation = false;
                    currentReadingPassage = null;
                    passageStartQ = 0;
                    passageEndQ = 0;
                    if (currentTopicCode == "GENERAL" && (upper.Contains("ENGLISH") || upper.Contains("TIENG ANH") || upper.Contains("PRONUNCIATION") || upper.Contains("READING")))
                    {
                        currentTopicCode = "TIENG_ANH";
                    }

                    // Nếu trong dòng Section Header này có chứa kèm chỉ thị dải câu (ví dụ: "PHẦN II. ... Thí sinh trả lời từ câu 1 đến câu 4.")
                    // Đăng ký bổ sung vào ScopeManager thông qua CIG Engine
                    if (cig.TryParseScopeDirective(line, out var embeddedScope) && embeddedScope != null)
                    {
                        scopeManager.RegisterScopeDirective(embeddedScope);
                    }

                    continue;
                }

                // [SGI v5.0 Pillar 1]: Nhận diện Dải Chỉ Thị Tác Vụ Tuyến Tính (Scope-Guided Directives) thông qua CIG
                // Ví dụ: "Từ câu 1 đến câu 12: Mỗi câu chỉ chọn một phương án...", "Questions 1-5: Mark the letter A, B, C or D..."
                if (!isQuestionLine && !isExplanationLine && cig.TryParseScopeDirective(line, out var scopeWindow) && scopeWindow != null)
                {
                    finalizeCurrentQuestion();
                    scopeManager.RegisterScopeDirective(scopeWindow);

                    // Đăng ký dòng chỉ dẫn này làm Ngữ cảnh chung (Context Stimulus) cho dải câu hỏi tương ứng
                    scopeManager.RegisterContextPassage(scopeWindow.StartQuestion, scopeWindow.EndQuestion, line, scopeWindow.RawDirective);
                    currentReadingPassage = line;
                    passageStartQ = scopeWindow.StartQuestion;
                    passageEndQ = scopeWindow.EndQuestion > 0 ? scopeWindow.EndQuestion : int.MaxValue;
                    inExplanation = false;

                    // Đồng bộ currentSection nếu dải câu này bao quát từ câu kế tiếp
                    if (scopeWindow.QuestionType != "UNKNOWN")
                    {
                        currentSection = scopeWindow.QuestionType;
                        var mSecTitle = LinguisticMatch(cig, LinguisticKeys.SectionTitle, scopeWindow.RawDirective ?? "");
                        currentSectionTitle = mSecTitle.Success ? mSecTitle.Groups[1].Value.ToUpperInvariant() : (scopeWindow.RawDirective?.Length > 40 ? scopeWindow.RawDirective.Substring(0, 40) + "..." : scopeWindow.RawDirective);
                    }

                    if (currentTopicCode == "GENERAL" && (upper.Contains("TIẾNG ANH") || upper.Contains("ENGLISH") || upper.Contains("PASSAGE") || upper.Contains("CLOZE")))
                    {
                        currentTopicCode = "TIENG_ANH";
                    }
                    continue;
                }

                // [SGI v5.0 Pillar 2]: Nhận diện Dải Ngữ cảnh Bài đọc hiểu / Tình huống (Passage Context Stimulus) thông qua CIG
                // Ví dụ: "Read the following passage and mark the letter A, B, C or D on your answer sheet to indicate the correct answer to each of the questions from 1 to 5."
                if (!isQuestionLine && !isExplanationLine && cig.TryParsePassageContextPrompt(line, out int ctxStartQ, out int ctxEndQ, out string ctxPromptTitle))
                {
                    finalizeCurrentQuestion();
                    currentReadingPassage = line;
                    passageStartQ = ctxStartQ;
                    passageEndQ = ctxEndQ > 0 ? ctxEndQ : int.MaxValue;
                    inExplanation = false;
                    if (currentTopicCode == "GENERAL") currentTopicCode = "TIENG_ANH";
                    continue;
                }

                // 2. Nhận dạng Tiêu đề Đoạn văn Đọc hiểu qua CIG (Passage / Advertisement / Leaflet / Cloze Test)
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

                // 2a. Nhận dạng chỉ dẫn làm bài (Instruction Directive: "Mark the letter...", "Choose the word...")
                // Khi chuyển sang dạng bài tập mới: nếu chỉ dẫn có phạm vi câu hỏi (from X to Y) thì lưu làm Stimulus Context; nếu không thì reset context
                bool isInstructionDirective = instructionDirectivePatterns.Any(p => Regex.IsMatch(line, p, RegexOptions.IgnoreCase));
                if (!isQuestionLine && !isExplanationLine && isInstructionDirective)
                {
                    int dirStart = 0, dirEnd = 0;
                    bool hasRange = cig.TryMatchReadingPassage(line, out dirStart, out dirEnd);
                    if (!hasRange)
                    {
                        var rangePatterns = cig.GetStructureDictionary().PassageRangePatterns;
                        if (rangePatterns != null)
                        {
                            foreach (var rp in rangePatterns)
                            {
                                var mRange = Regex.Match(line, rp, RegexOptions.IgnoreCase);
                                if (mRange.Success && mRange.Groups.Count >= 3)
                                {
                                    if (int.TryParse(mRange.Groups[1].Value, out dirStart) && int.TryParse(mRange.Groups[2].Value, out dirEnd))
                                    {
                                        hasRange = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (hasRange)
                    {
                        finalizeCurrentQuestion();
                        currentReadingPassage = line;
                        passageStartQ = dirStart;
                        passageEndQ = dirEnd > 0 ? dirEnd : int.MaxValue;
                        inExplanation = false;
                        if (currentTopicCode == "GENERAL") currentTopicCode = "TIENG_ANH";
                        continue;
                    }

                    if (currentQuestion == null || currentQuestionPendingLines.Count > 0 || !string.IsNullOrWhiteSpace(currentQuestion.Content))
                    {
                        finalizeCurrentQuestion();
                        currentReadingPassage = null;
                        passageStartQ = 0;
                        passageEndQ = 0;
                        continue;
                    }
                }

                // 2b. Đọc tiếp nội dung đoạn văn đọc hiểu nếu đang ở trạng thái đọc bài và chưa gặp câu hỏi mới
                if (!string.IsNullOrEmpty(currentReadingPassage) && currentQuestion == null && !isQuestionLine)
                {
                    currentReadingPassage += "\n\n" + line;
                    continue;
                }

                // 2d. Nhận diện dòng Đáp án inline ngay dưới câu hỏi
                var cleanLine = Regex.Replace(line, @"^<[a-z0-9]+>", "", RegexOptions.IgnoreCase).Trim();
                if (currentQuestion != null && !isSolutionPhase && (cig.IsAnswerLine(cleanLine, currentMatchedRule, out string inlineAns) || LinguisticMatch(cig, LinguisticKeys.ChoiceAnswerLine, cleanLine).Success))
                {
                    if (string.IsNullOrEmpty(inlineAns))
                    {
                        var mAns = LinguisticMatch(cig, LinguisticKeys.ChoiceAnswerLine, cleanLine);
                        if (mAns.Success) inlineAns = mAns.Groups[1].Value.Trim();
                    }
                    if (!string.IsNullOrWhiteSpace(inlineAns))
                    {
                        if (inlineAns.Length == 1 && char.IsLetter(inlineAns[0]))
                        {
                            currentQuestion.SuggestedAnswer = (char.ToUpperInvariant(inlineAns[0]) - 'A' + 1).ToString();
                        }
                        else
                        {
                            currentQuestion.SuggestedAnswer = inlineAns;
                        }
                    }
                    continue;
                }

                if (cig.TryMatchQuestionAnchor(cleanLine, out int qNum, out var matchedRule, activeRules) && matchedRule?.TargetExamType != "TABLE_MATRIX")
                {
                    LastDebugLogs.Add($"[MATCHED Q] '{cleanLine}' -> qNum={qNum}, isSol={isSolutionPhase}, topic={currentTopicCode}, rule={matchedRule?.RuleCode}");
                    currentMatchedRule = matchedRule;
                    string remainderText = "";
                    var mStandard = LinguisticMatch(cig, LinguisticKeys.StandardQuestionAnchor, cleanLine);
                    if (mStandard.Success)
                    {
                        remainderText = mStandard.Groups[1].Success ? mStandard.Groups[1].Value.Trim() : "";
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
                            currentSubItemExplKey = null;

                            // Kiểm tra xem dòng tiêu đề này có chứa Lời giải thực sự hay không (ủy quyền qua CIG Engine đa ngôn ngữ)
                            if (cig.TryMatchExplanationStart(remainderText, matchedRule, out string explPart))
                            {
                                inExplanation = true;
                                if (!string.IsNullOrWhiteSpace(explPart))
                                {
                                    AppendExplanationLine(currentQuestion, explPart, matchedRule, cig);
                                }
                                TryExtractAnswer(currentQuestion, remainderText, matchedRule, cig);
                            }
                            else if (Has(cig, existingQ.QuestionType, QuestionTypeProfileDto.CapFreeText))
                            {
                                // Đối với câu Tự luận hoặc Trả lời ngắn trong Pha Lời giải: Mọi nội dung đều là lời giải / bước tính
                                inExplanation = true;
                                if (!string.IsNullOrWhiteSpace(remainderText))
                                {
                                    AppendExplanationLine(currentQuestion, remainderText, matchedRule, cig);
                                    TryExtractAnswer(currentQuestion, remainderText, matchedRule, cig);
                                }
                            }
                            else if (Has(cig, existingQ.QuestionType, QuestionTypeProfileDto.CapTrueFalseFamily))
                            {
                                // Đối với câu Đúng/Sai trong Pha Lời giải: Mọi dòng dẫn nhập hoặc sub items đều là lời giải
                                inExplanation = true;
                                if (!string.IsNullOrWhiteSpace(remainderText))
                                {
                                    AppendExplanationLine(currentQuestion, remainderText, matchedRule, cig);
                                }
                            }
                            else if (!string.IsNullOrWhiteSpace(remainderText) && Regex.IsMatch(remainderText, @"^[A-Ga-g][\.\:\)]"))
                            {
                                // Đối với câu Trắc nghiệm đơn: nếu ngay sau số câu là phương án đúng (ví dụ: "Câu 1. D. Chống lại...")
                                inExplanation = true;
                                var mAnsOpt = Regex.Match(remainderText, @"^([A-Ga-g])[\.\:\)]\s*(.*)");
                                if (mAnsOpt.Success)
                                {
                                    string letter = mAnsOpt.Groups[1].Value.ToUpperInvariant();
                                    if (string.IsNullOrWhiteSpace(existingQ.SuggestedAnswer) || existingQ.SuggestedAnswer.Contains("Điền đáp số"))
                                    {
                                        existingQ.SuggestedAnswer = (letter[0] - 'A' + 1).ToString();
                                    }
                                    string restContent = mAnsOpt.Groups[2].Value.Trim();
                                    if (!string.IsNullOrWhiteSpace(restContent))
                                    {
                                        AppendExplanationLine(currentQuestion, restContent, matchedRule, cig);
                                    }
                                }
                            }
                            else
                            {
                                // Dòng này chỉ là đề bài lặp lại của trắc nghiệm -> chưa vào lời giải thực sự
                                inExplanation = false;
                            }
                        }
                        else
                        {
                            currentQuestion = null;
                            currentSubItemExplKey = null;
                            inExplanation = false;
                        }
                        continue;
                    }

                    finalizeCurrentQuestion();

                    currentQuestionNumber = qNum;
                    inExplanation = false;

                    // [SGI v5.0 Pillar 3]: Phân giải Dạng câu hỏi mong đợi từ Bộ điều phối CognitiveScopeManager
                    // Kết hợp 3 tầng suy luận: [Scope Directives -> Section Macros -> CIG Rule Specification]
                    string resolvedScopeType = scopeManager.ResolveExpectedQuestionType(currentQuestionNumber, currentSection);

                    var contentText = remainderText;
                    var sameLineOptions = Has(cig, resolvedScopeType, QuestionTypeProfileDto.CapChoice)
                        ? ExtractUppercaseOptionsFromLine(contentText, matchedRule, cig)
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
                        if (passageStartQ == 0) passageStartQ = currentQuestionNumber;
                        attachedContextId = $"CTX_{currentExamCode}_{passageStartQ}";
                        attachedContextTitle = passageEndQ < int.MaxValue && passageEndQ > passageStartQ
                            ? $"Bài đọc hiểu (Câu {passageStartQ} - {passageEndQ})"
                            : $"Bài đọc hiểu (Từ câu {passageStartQ})";
                        attachedContextContent = currentReadingPassage.Trim();
                    }
                    else
                    {
                        var resolvedContext = scopeManager.ResolveContextForQuestion(currentQuestionNumber);
                        if (resolvedContext != null)
                        {
                            attachedContextId = $"CTX_{currentExamCode}_{resolvedContext.StartQuestion}";
                            attachedContextTitle = resolvedContext.ContextTitle;
                            attachedContextContent = resolvedContext.ContextContent;
                        }
                    }

                    var inferredTopic = currentTopicCode != "GENERAL" 
                        ? currentTopicCode 
                        : (documentSubject ?? InferTopicFromContent(questionBody, dynamicContext));

                    string initialAnswer = "";
                    if (answerKeyMap != null)
                    {
                        var examCodes = new[] { currentExamCode, "DEFAULT" };
                        // Tuân thủ nghiêm ngặt phân cấp Đề-Phần-Câu-Ý: CHỈ tra cứu trong Section tương đương của câu hỏi này
                        var allowedSections = GetEquivalentSections(resolvedScopeType);
                        if (!string.IsNullOrWhiteSpace(currentSection))
                        {
                            var secEquivs = GetEquivalentSections(currentSection);
                            foreach (var s in secEquivs)
                            {
                                if (!allowedSections.Contains(s, StringComparer.OrdinalIgnoreCase)) allowedSections.Add(s);
                            }
                        }
                        var qNums = new[] { currentQuestionNumber };

                        foreach (var ec in examCodes)
                        {
                            if (answerKeyMap.TryGetValue(ec, out var secMap))
                            {
                                foreach (var sec in allowedSections)
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
                    string? subCat = cig.DetectSubCategory(questionBody, matchedRule) ?? currentSubCategory;

                    string determinedQuestionType = resolvedScopeType;
                    if (determinedQuestionType == "SINGLE" && !string.IsNullOrEmpty(matchedRule?.InteractionSpec?.DefaultQuestionType) && matchedRule.InteractionSpec.DefaultQuestionType != "SINGLE")
                    {
                        determinedQuestionType = matchedRule.InteractionSpec.DefaultQuestionType;
                    }

                    currentQuestion = new DocxImportPreviewDto
                    {
                        Content = sameLineOptions.Count > 0 ? questionBody : "",
                        QuestionType = determinedQuestionType,
                        TopicCode = inferredTopic,
                        TopicName = dynamicContext?.Topics?.FirstOrDefault(t => t.Code.Equals(inferredTopic, StringComparison.OrdinalIgnoreCase))?.Name ?? (inferredTopic == "MATH" || inferredTopic == "TOAN" ? "Toán học" : null),
                        Difficulty = isFatal ? 5 : cig.GetDefaultDifficulty(determinedQuestionType),
                        ContentType = questionBody.Contains("data:image/") ? "image" : "text",
                        OptionType = "text",
                        SuggestedAnswer = initialAnswer,
                        IsCritical = isFatal,
                        SubCategory = subCat,
                        ContextId = attachedContextId,
                        ContextTitle = attachedContextTitle,
                        ContextContent = attachedContextContent,
                        SourceExamName = currentExamCode,
                        SourceSectionName = currentSectionTitle ?? currentSection,
                        SourceQuestionNo = currentQuestionNumber > 0 ? currentQuestionNumber.ToString() : null,
                        DurationSeconds = headerDurationSeconds,
                        DomainCode = headerTaxonomy.DomainCode ?? "GENERAL",
                        TargetLevel = headerTaxonomy.TargetLevel,
                        AssessmentPurpose = headerTaxonomy.AssessmentPurpose,
                        IssuingOrg = headerTaxonomy.IssuingOrg,
                        OrgUnitName = headerTaxonomy.IssuingOrg != null && headerTaxonomy.IssuingOrg.Contains("Trường", StringComparison.OrdinalIgnoreCase) ? headerTaxonomy.IssuingOrg : null,
                        BenchmarkYear = headerTaxonomy.BenchmarkYear ?? 2026,
                        BenchmarkStandard = headerTaxonomy.BenchmarkStandard,
                        Tags = new List<string>(headerTaxonomy.Tags)
                    };

                    if (sameLineOptions.Count > 0)
                    {
                        foreach (var opt in sameLineOptions)
                        {
                            currentQuestion.Options.Add(opt.Content);
                            if (opt.IsMarkedCorrect && string.IsNullOrEmpty(currentQuestion.SuggestedAnswer))
                            {
                                currentQuestion.SuggestedAnswer = (opt.Letter - 'A' + 1).ToString();
                            }
                            if (opt.Content.Contains("data:image/"))
                            {
                                currentQuestion.OptionType = "image";
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
                            // Kiểm tra xem dòng này có phải là mở đầu của Lời giải không (CIG Engine đa ngôn ngữ)
                            if (cig.TryMatchExplanationStart(line, currentMatchedRule, out string rest))
                            {
                                inExplanation = true;
                                if (!string.IsNullOrWhiteSpace(rest))
                                {
                                    AppendExplanationLine(currentQuestion, rest, currentMatchedRule, cig);
                                }
                                TryExtractAnswer(currentQuestion, line, currentMatchedRule, cig);
                                continue;
                            }
                            else if (Has(cig, currentQuestion.QuestionType, QuestionTypeProfileDto.CapFreeText))
                            {
                                // Đối với câu Tự luận hoặc Trả lời ngắn trong Pha Lời giải: Mọi dòng đều là bước giải/hướng dẫn giải
                                inExplanation = true;
                                AppendExplanationLine(currentQuestion, line, currentMatchedRule, cig);
                                TryExtractAnswer(currentQuestion, line, currentMatchedRule, cig);
                                continue;
                            }

                            // Kiểm tra xem dòng này có chứa phương án gạch chân / đánh dấu đúng trước khi vào Lời giải hay không
                            if (Has(cig, currentQuestion.QuestionType, QuestionTypeProfileDto.CapChoice))
                            {
                                var sameLineOpts = ExtractUppercaseOptionsFromLine(line, currentMatchedRule, cig);
                                var markedOpt = sameLineOpts.FirstOrDefault(o => o.IsMarkedCorrect);
                                if (markedOpt != null)
                                {
                                    if (string.IsNullOrWhiteSpace(currentQuestion.SuggestedAnswer) || currentQuestion.SuggestedAnswer.Contains("Điền đáp số"))
                                    {
                                        currentQuestion.SuggestedAnswer = (markedOpt.Letter - 'A' + 1).ToString();
                                    }
                                }

                                // Nếu dòng này chứa các phương án trắc nghiệm A, B, C, D lặp lại -> bỏ qua
                                if (sameLineOpts.Count > 0 || Regex.IsMatch(line.Trim(), @"^[A-Ga-g][\.\:\)]\s+"))
                                {
                                    continue;
                                }

                                // Nếu không phải các lựa chọn A, B, C, D lặp lại mà là văn bản giải thích tự nhiên -> BẬT LỜI GIẢI!
                                inExplanation = true;
                                AppendExplanationLine(currentQuestion, line, currentMatchedRule, cig);
                                TryExtractAnswer(currentQuestion, line, currentMatchedRule, cig);
                                continue;
                            }
                            else if (Has(cig, currentQuestion.QuestionType, QuestionTypeProfileDto.CapTrueFalseFamily))
                            {
                                if (cig.TryMatchSubItemMarker(line, currentMatchedRule, out string itemLetter, out string itemContent, out string inlineVal))
                                {
                                    bool isUnderlinedOrMarked = line.Contains($"<u>{itemLetter}") || line.Contains($"<u>({itemLetter}") 
                                                             || line.Contains($"*{itemLetter}") || line.Contains($"<b>{itemLetter}")
                                                             || Regex.IsMatch(line, $@"<u>\s*\(?{itemLetter}[\)\.]?\s*</u>", RegexOptions.IgnoreCase);
                                    string val = !string.IsNullOrEmpty(inlineVal) ? inlineVal : (isUnderlinedOrMarked ? "Đúng" : "Sai");
                                    UpdateTrueFalseAnswer(currentQuestion, itemLetter, val);

                                    inExplanation = true;
                                    currentSubItemExplKey = itemLetter;
                                    AppendExplanationLine(currentQuestion, line, currentMatchedRule, cig);
                                    string explText = string.IsNullOrWhiteSpace(itemContent) ? line : itemContent;
                                    AppendSubItemExplanation(currentQuestion, currentSubItemExplKey, explText);
                                    continue;
                                }
                                else
                                {
                                    // Dòng giải thích chung / công thức dẫn nhập của câu Đúng/Sai
                                    inExplanation = true;
                                    AppendExplanationLine(currentQuestion, line, currentMatchedRule, cig);
                                    continue;
                                }
                            }

                            // Dạng câu hỏi mở rộng (MATCHING, ORDERING, FILL_BLANK, plugin Strategy...): không có Ý con.
                            // Mọi dòng không phải phương án lặp lại đều là lời giải cấp ĐỀ_PHẦN_CÂU -> làm học liệu.
                            if (!Regex.IsMatch(line.Trim(), @"^[A-Ga-g][\.\:\)]\s+"))
                            {
                                inExplanation = true;
                                AppendExplanationLine(currentQuestion, line, currentMatchedRule, cig);
                                TryExtractAnswer(currentQuestion, line, currentMatchedRule, cig);
                            }
                            continue;
                        }

                        AppendExplanationLine(currentQuestion, line, currentMatchedRule, cig);
                        TryExtractAnswer(currentQuestion, line, currentMatchedRule, cig);

                        // [SGI v5.5]: Tự động phân rã Lời giải vi mô vào từng SubItem (Ý con) phục vụ tạo Storyboard / Hoạt họa (CIG Engine)
                        if (Has(cig, currentQuestion.QuestionType, QuestionTypeProfileDto.CapSubItems)
                            && cig.TryMatchSubItemMarker(line, currentMatchedRule, out string subKey, out string subContent, out _))
                        {
                            currentSubItemExplKey = subKey;
                            string explText = string.IsNullOrWhiteSpace(subContent) ? line : subContent;
                            AppendSubItemExplanation(currentQuestion, currentSubItemExplKey, explText);
                        }
                        else if (!string.IsNullOrEmpty(currentSubItemExplKey))
                        {
                            AppendSubItemExplanation(currentQuestion, currentSubItemExplKey, line);
                        }
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
                        || (Has(cig, q.QuestionType, QuestionTypeProfileDto.CapMultiStatement) && !q.SuggestedAnswer.Contains(")"));

                    if (needsAns)
                    {
                        int qNum1Based = i + 1;
                        var examCodes = new[] { q.SourceExamName ?? currentExamCode, currentExamCode, "DEFAULT" };
                        // Tuân thủ nghiêm ngặt phân cấp Đề-Phần-Câu-Ý: CHỈ tra cứu trong Section tương đương cùng dạng
                        var allowedSections = GetEquivalentSections(q.QuestionType);
                        if (!string.IsNullOrWhiteSpace(q.SourceSectionName))
                        {
                            var secEquivs = GetEquivalentSections(q.SourceSectionName);
                            foreach (var s in secEquivs)
                            {
                                if (!allowedSections.Contains(s, StringComparer.OrdinalIgnoreCase)) allowedSections.Add(s);
                            }
                        }

                        cig.TryExtractQuestionNumber(q.Content, null, out int localQNum);
                        int.TryParse(q.SourceQuestionNo, out int sourceQNum);
                        var qNumsToTry = new List<int>();
                        if (sourceQNum > 0) qNumsToTry.Add(sourceQNum);
                        if (localQNum > 0 && !qNumsToTry.Contains(localQNum)) qNumsToTry.Add(localQNum);

                        string foundAns = "";
                        foreach (var ec in examCodes)
                        {
                            if (answerKeyMap.TryGetValue(ec, out var secMap))
                            {
                                foreach (var sec in allowedSections)
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
                            if (Has(cig, q.QuestionType, QuestionTypeProfileDto.CapSingleAnswer))
                            {
                                var mLetter = Regex.Match(foundAns, @"\b([A-G])\b", RegexOptions.IgnoreCase);
                                q.SuggestedAnswer = mLetter.Success ? (char.ToUpperInvariant(mLetter.Value[0]) - 'A' + 1).ToString() : foundAns;
                            }
                            else
                            {
                                q.SuggestedAnswer = foundAns;
                            }

                            // [SGI v5.5]: Đồng bộ đáp án từ bảng vào thực thể SubItems
                            if (Has(cig, q.QuestionType, QuestionTypeProfileDto.CapTrueFalseFamily))
                            {
                                var subMatches = Regex.Matches(foundAns, @"([a-d])[\:\)]\s*(Đúng|Sai|True|False)", RegexOptions.IgnoreCase);
                                foreach (Match m in subMatches)
                                {
                                    string sKey = m.Groups[1].Value.ToLowerInvariant();
                                    string sVal = m.Groups[2].Value.StartsWith("đ", StringComparison.OrdinalIgnoreCase) || m.Groups[2].Value.StartsWith("d", StringComparison.OrdinalIgnoreCase) ? "Đúng" : "Sai";
                                    var sub = q.SubItems.FirstOrDefault(s => string.Equals(s.Key, sKey, StringComparison.OrdinalIgnoreCase));
                                    if (sub == null)
                                    {
                                        sub = new QuestionSubItemDto
                                        {
                                            Key = sKey,
                                            Content = $"Mệnh đề {sKey})",
                                            ExpectedAnswer = sVal,
                                            OrderIndex = q.SubItems.Count + 1
                                        };
                                        q.SubItems.Add(sub);
                                    }
                                    else
                                    {
                                        sub.ExpectedAnswer = sVal;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // [Taxonomy SSoT Delegation] Ủy quyền toàn bộ việc làm giàu tọa độ và taxonomy cấp đề cho UniversalCoordinateTaxonomyEngine
            UniversalCoordinateTaxonomyEngine.EnrichExamTaxonomy(results, dynamicContext);

            return results;
        }

        public static void AppendExplanationLine(DocxImportPreviewDto q, string line, QuestionParsingRuleDto? rule = null, IDynamicQuestionIngestionEngine? engine = null)
        {
            (engine ?? DynamicQuestionIngestionEngine.Instance).AppendExplanationLine(q, line, rule);
        }

        public static void UpdateTrueFalseAnswer(DocxImportPreviewDto q, string itemLetter, string val)
        {
            DynamicQuestionIngestionEngine.Instance.UpdateTrueFalseAnswer(q, itemLetter, val);
        }

        /// <summary>
        /// Nối dòng Lời giải vi mô cho từng Ý con (SubItem), phục vụ Storyboard / Phân cảnh Hoạt họa AI
        /// </summary>
        public static void AppendSubItemExplanation(DocxImportPreviewDto q, string subKey, string text)
        {
            DynamicQuestionIngestionEngine.Instance.AppendSubItemExplanation(q, subKey, text);
        }

        public static void TryExtractAnswer(
            DocxImportPreviewDto q,
            string line,
            QuestionParsingRuleDto? rule = null,
            IDynamicQuestionIngestionEngine? engine = null)
        {
            (engine ?? DynamicQuestionIngestionEngine.Instance).TryExtractAnswer(q, line, rule);
        }

        public static string NormalizeSectionKey(string? rawSec, IDynamicQuestionIngestionEngine? engine = null)
        {
            return (engine ?? DynamicQuestionIngestionEngine.Instance).NormalizeSectionKey(rawSec);
        }

        /// <summary>
        /// Chuẩn hóa khóa phân đoạn: ưu tiên dấu hiệu trong tên phần (alias đa ngôn ngữ từ hồ sơ dạng câu hỏi);
        /// nếu tên phần không nhận diện được (ví dụ "PHẦN IV", "PHẦN TỰ CHỌN") thì dùng dạng câu hỏi đã phân loại
        /// để khóa ĐỀ_PHẦN_CÂU luôn duy nhất.
        /// </summary>
        public static string NormalizeSectionKey(string? rawSec, string? questionType, IDynamicQuestionIngestionEngine? engine = null)
        {
            return (engine ?? DynamicQuestionIngestionEngine.Instance).NormalizeSectionKey(rawSec, questionType);
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
            return DynamicQuestionIngestionEngine.Instance.FindQuestionByHierarchy(questionMap, results, examCode, section, qNum);
        }

        public static List<string> GetEquivalentSections(string section, IDynamicQuestionIngestionEngine? engine = null)
        {
            return (engine ?? DynamicQuestionIngestionEngine.Instance).GetEquivalentSections(section);
        }

        public static List<ParsedOption> ExtractUppercaseOptionsFromLine(string line, QuestionParsingRuleDto? rule = null, IDynamicQuestionIngestionEngine? engine = null)
        {
            var cig = engine ?? DynamicQuestionIngestionEngine.Instance;
            return cig.ExtractUppercaseOptionsFromLine(line, rule)
                .Select(o => new ParsedOption { Letter = o.Letter, Content = o.Content, IsMarkedCorrect = o.IsMarkedCorrect })
                .ToList();
        }

        public static List<ParsedOption> ExtractOptionsFromLine(string line, QuestionParsingRuleDto? rule = null, IDynamicQuestionIngestionEngine? engine = null)
        {
            var cig = engine ?? DynamicQuestionIngestionEngine.Instance;
            return cig.ExtractOptionsFromLine(line, rule)
                .Select(o => new ParsedOption { Letter = o.Letter, Content = o.Content, IsMarkedCorrect = o.IsMarkedCorrect })
                .ToList();
        }

        public static void FinalizeQuestion(
            DocxImportPreviewDto q, 
            DynamicDomainContext? ctx = null,
            IDynamicQuestionIngestionEngine? engine = null,
            QuestionParsingRuleDto? rule = null)
        {
            (engine ?? DynamicQuestionIngestionEngine.Instance).FinalizeQuestion(q, ctx, rule);
        }

        public static void FinalizeAndMergeQuestion(
            List<DocxImportPreviewDto> results,
            Dictionary<string, DocxImportPreviewDto> questionMap,
            DocxImportPreviewDto q,
            string examCode,
            int qNum,
            DynamicDomainContext? ctx = null,
            IDynamicQuestionIngestionEngine? engine = null,
            QuestionParsingRuleDto? rule = null)
        {
            (engine ?? DynamicQuestionIngestionEngine.Instance).FinalizeAndMergeQuestion(results, questionMap, q, examCode, qNum, ctx, rule);
        }

        public static string ResolveTopicCode(string raw, DynamicDomainContext? ctx = null)
        {
            return UniversalCoordinateTaxonomyEngine.ResolveTopicCode(raw, ctx);
        }

        public static string InferTopicFromContent(string content, DynamicDomainContext? ctx = null)
        {
            return UniversalCoordinateTaxonomyEngine.InferTopicFromContent(content, ctx);
        }

        public static void AutoEnrichTaxonomyAndTags(DocxImportPreviewDto q, DynamicDomainContext? ctx = null)
        {
            UniversalCoordinateTaxonomyEngine.AutoEnrichTaxonomyAndTags(q, ctx);
        }

        public static void AutoEnrichTaxonomyAndTags(DocxImportPreviewDto q)
        {
            UniversalCoordinateTaxonomyEngine.AutoEnrichTaxonomyAndTags(q, null);
        }
    }

    /// <summary>
    /// [Clean Architecture Wrapper] Triển khai IDocxStateMachineClassifier phục vụ Dependency Injection
    /// </summary>
    public class DocxStateMachineClassifierService : IDocxStateMachineClassifier
    {
        public List<DocxImportPreviewDto> ParseParagraphList(
            List<string> paragraphs,
            Dictionary<string, Dictionary<string, Dictionary<int, string>>>? answerKeyMap = null,
            DynamicDomainContext? dynamicContext = null)
        {
            return DocxStateMachineClassifier.ParseParagraphList(paragraphs, answerKeyMap, dynamicContext, null);
        }

        public List<DocxImportPreviewDto> ParseParagraphList(
            List<string> paragraphs,
            Dictionary<string, Dictionary<string, Dictionary<int, string>>>? answerKeyMap,
            DynamicDomainContext? dynamicContext,
            DynamicQuestionIngestionEngine? engineOverride)
        {
            return DocxStateMachineClassifier.ParseParagraphList(paragraphs, answerKeyMap, dynamicContext, engineOverride);
        }
    }
}
