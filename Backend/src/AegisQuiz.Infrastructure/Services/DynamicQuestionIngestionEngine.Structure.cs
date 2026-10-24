using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Services.Docx;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [CIG Engine — Document Structure] Nhận diện cấu trúc tài liệu đề thi 100% từ dữ liệu:
    /// biên giới đề, tiêu đề chủ đề, phân đoạn (PHẦN I/II/III), pha lời giải, bảng đáp án, bài đọc hiểu.
    /// Thứ tự ưu tiên nguồn mẫu: Từ điển cấu trúc (JSON) → LifecycleSpec của từng quy tắc → mẫu lõi dự phòng.
    /// </summary>
    public partial class DynamicQuestionIngestionEngine
    {
        /// <summary>Phân loại nhanh O(1) một dòng văn bản (không dùng Regex).</summary>
        public enum FastLineCategory
        {
            Empty,
            SectionHeader,
            ExamHeader,
            QuestionAnchor,
            OptionOrSubItem,
            EvaluationOrSolution,
            GeneralContent
        }

        /// <summary>Thứ tự kiểm tra phân đoạn từ đặc thù đến tổng quát (SINGLE cuối cùng vì "trắc nghiệm" là từ bao trùm).</summary>
        private static readonly string[] SectionKeySpecificityOrder =
            { "ESSAY", "SHORT_ANSWER", "TRUE_FALSE", "MULTI", "MATCHING", "ORDERING", "SINGLE" };

        private const string BuiltInExamHeadingPattern = @"^(?:ĐỀ|D[E|Ê]|EXAM|TEST|MOCK\s*TEST)\s*(?:SỐ|THI(?:\s*THỬ)?|KIỂM\s*TRA|TEST|NO\.?|NUMBER|CODE|MÃ)?\s*(\d{1,3})\b";
        private const string BuiltInTopicHeadingPattern = @"(?i)^(?:[\t\s|•\-–—;]*)(?:BÀI\s*THI\s*(?:MÔN)?|MÔN\s*THI|MÔN|HỌC\s*PHẦN|CHỦ\s*ĐỀ|CHƯƠNG|LĨNH\s*VỰC|MÃ\s*CĐ|TOPIC|SUBJECT|COURSE|MODULE)[\.\:\s\-]+([^\n\r\t|;]+)";
        private const string BuiltInSolutionPhasePattern = @"^(?:(?:PHẦN|PART|SECTION)\s+[IVXLCDM\d]+[\.\:\-\s]+)?(?:ĐÁP\s*ÁN\s*(?:VÀ|&)?\s*(?:LỜI\s*GIẢI|HƯỚNG\s*DẪN\s*(?:CHẤM|GIẢI))|HƯỚNG\s*DẪN\s*(?:CHẤM|GIẢI)\s*(?:VÀ|&)?\s*(?:BIỂU|THANG|ĐÁP)?\s*(?:ĐIỂM|ÁN)?|BIỂU\s*ĐIỂM\s*(?:VÀ|&)?\s*ĐÁP\s*ÁN|THANG\s*ĐIỂM\s*(?:VÀ|&)?\s*HƯỚNG\s*DẪN|(?:LỜI\s*GIẢI|HƯỚNG\s*DẪN\s*GIẢI)\s+(?:CHI\s*TIẾT|THAM\s*KHẢO|CÁC\s*ĐỀ|ĐỀ\s*THI)|SOLUTIONS?|EXPLANATIONS?|MARK\s*SCHEME)(?:\s+(?:THAM\s*KHẢO|CHI\s*TIẾT|DETAILS?|APPENDIX|THI))?[.\:\s\-]*$";
        private const string BuiltInAnswerPhasePattern = @"^(?:(?:PHẦN|PART|SECTION)\s+[IVXLCDM\d]+[\.\:\-\s]+)?(?:BẢNG\s*ĐÁP\s*ÁN(?:\s*TRẮC\s*NGHIỆM)?|ĐÁP\s*ÁN|D[\.\?\s]*P\s*[\.\?\s]*N|ANSWER\s*KEY|KEY|ANSWERS?)\s*(?:TRẮC\s*NGHIỆM|THAM\s*KHẢO|CHI\s*TIẾT)?[.\:\s\-]*$";
        private const string BuiltInPassageStartPattern = @"^(?:Read\s+(?:the\s+)?(?:following\s+)?(?:passage|advertisement|leaflet|text|article|announcement)|Đọc\s+(?:đoạn\s+văn|bài\s+đọc|thông\s+báo|tờ\s+rơi)\s+sau|(?:Sử\s*dụng|Dựa\s*(?:vào|trên)|Cho|Dùng)\s+(?:các\s+)?(?:thông\s*tin|ngữ\s*liệu|đoạn\s*văn|bài\s*đọc|dữ\s*kiện|bảng|hình)(?:\s+(?:cho|dưới\s*đây|sau))?.*?(?:trả\s*lời|làm\s*bài|cho)|(?:Mark|Choose)\s+the\s+letter.*(?:fits?\s+each|fill.*blank|arrangement|cohesive|coherent|exchange|meaningful|conversation|pronunciation|stress))[\s\S]*";
        private const string BuiltInPassageRangePattern = @"\b(?:from|từ\s+(?:các\s+)?câu(?:\s+số)?|(?:các\s+)?(?:questions?|câu)(?:\s+số)?)\s*(?:from\s+)?(\d+)\s*(?:to|đến|through|[-–—])\s*(\d+)\b";
        private const string BuiltInEndOfExamPattern = @"^(?:[\-\–\—_*\=.\s]*\b(?:HẾT|HET|THE\s*END|END\s*OF\s*(?:TEST|EXAM|PAPER)|HẾT\s+ĐỀ\s+THI)\b[\-\–\—_*\=.\s]*)$";
        private const string BuiltInProctorPattern = @"(?i)(?:Cán\s*bộ\s*coi\s*thi|Giám\s*thị|Giám\s*khảo)\s+không\s+giải\s+thích(?:\s+gì)?\s+thêm|Thí\s*sinh\s+không\s+được\s+sử\s+dụng\s+tài\s+liệu|Họ\s*(?:và)?\s*tên\s+thí\s*sinh[\.\:\s_]+Số\s*báo\s*danh";

        // ─────────────────────────────────────────────────────────────────────
        // Biên giới đề thi & chủ đề
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Nhận diện tiêu đề đề thi ("ĐỀ 1", "ĐỀ SỐ 01", "EXAM 02", "TEST NO 4"...). Trả về mã / số đề qua <paramref name="examCode"/>.</summary>
        public bool TryMatchExamHeading(string line, out string examCode)
        {
            examCode = string.Empty;
            if (string.IsNullOrWhiteSpace(line)) return false;

            // Loại trừ dòng tiêu đề tài liệu chứa năm thi / tốt nghiệp nếu không có định danh số đề rõ ràng
            if (Regex.IsMatch(line, @"(?i)\b(?:TỐT\s*NGHIỆP|THPT|ĐẠI\s*HỌC|HỌC\s*KỲ|HK\d|NĂM\s*HỌC|NĂM\s*\d{4})\b")
                && !Regex.IsMatch(line, @"(?i)\b(?:ĐỀ|EXAM|TEST|MÃ\s*ĐỀ)\s*(?:SỐ\s*|NO\.?\s*)?\d{1,3}\b"))
            {
                return false;
            }

            var captured = FirstCapture(line, _structureDictionary.ExamBoundaryMarkers);
            if (captured == null)
            {
                foreach (var rule in SnapshotRules())
                {
                    captured = FirstCapture(line, rule.LifecycleSpec?.ExamHeadingPatterns);
                    if (captured != null) break;
                }
            }
            captured ??= FirstCapture(line, new[] { BuiltInExamHeadingPattern });

            if (captured == null) return false;

            // Loại trừ hoàn toàn số năm (1900..2099)
            if (int.TryParse(captured, out int examNum))
            {
                if (examNum >= 1900 && examNum <= 2099)
                {
                    return false;
                }

                // Loại trừ số khối lớp học (Lớp 10, Khối 12, Grade 9...) nếu không có tiền tố Đề/Exam/Mã đề
                if (Regex.IsMatch(line, @"(?i)\b(?:LỚP|KHỐI|GRADE)\s*" + examNum + @"\b")
                    && !Regex.IsMatch(line, @"(?i)\b(?:ĐỀ|EXAM|TEST|MÃ\s*ĐỀ)\s*(?:SỐ\s*|NO\.?\s*)?" + examNum + @"\b"))
                {
                    return false;
                }
            }

            examCode = captured;
            return true;
        }

        /// <summary>Nhận diện tiêu đề chủ đề / môn học ("Chủ đề: Toán học", "Môn: Tiếng Anh", "Topic: Science"...).</summary>
        public bool TryMatchTopicHeading(string line, out string topicName)
        {
            topicName = string.Empty;
            if (string.IsNullOrWhiteSpace(line)) return false;

            var captured = FirstCapture(line, _structureDictionary.TopicHeadingPatterns);
            if (captured == null)
            {
                foreach (var rule in SnapshotRules())
                {
                    captured = FirstCapture(line, rule.LifecycleSpec?.TopicHeadingPatterns);
                    if (captured != null) break;
                }
            }
            captured ??= FirstCapture(line, new[] { BuiltInTopicHeadingPattern });

            if (string.IsNullOrWhiteSpace(captured)) return false;
            topicName = captured;
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Phân đoạn (Section) & pha tài liệu
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Nhận diện tiêu đề phân đoạn và trả về loại câu hỏi tương ứng (SINGLE, TRUE_FALSE, SHORT_ANSWER, ESSAY...).
        /// Hỗ trợ GDPT 2025 (PHẦN I/II/III), SAT (PART A/B/C), Gaokao (单项选择 / 填空题 / 解答题).
        /// 100% Data-Driven (Zero Hard-code): Khớp từ điển cấu trúc tài liệu -> Khớp LifecycleSpec từng quy tắc -> Dự phòng La Mã khi từ điển rỗng.
        /// </summary>
        public bool TryMatchSectionHeader(string line, out string sectionKey)
        {
            sectionKey = string.Empty;
            if (string.IsNullOrWhiteSpace(line)) return false;

            // 1. Khớp theo từ điển cấu trúc của tài liệu (Ưu tiên số 1 - cấu hình nạp từ registry JSON hoặc CSDL)
            if (TryMatchSectionMapping(line, _structureDictionary.SectionMarkers, out sectionKey)) return true;

            // 2. Khớp theo LifecycleSpec của từng quy tắc CIG đang kích hoạt
            foreach (var rule in SnapshotRules())
            {
                if (TryMatchSectionMapping(line, rule.LifecycleSpec?.SectionHeaderMapping, out sectionKey)) return true;
            }

            // 3. Mẫu lõi dự phòng theo số thứ tự La Mã thuần túy (chỉ dùng khi không khớp từ điển hoặc từ điển rỗng)
            var upper = line.ToUpperInvariant();
            if (GetOrAddRegex(@"\b(?:PHẦN|PHAN|PART|SECTION)\s+(?:III|3|C)\b").IsMatch(upper))
            {
                sectionKey = "SHORT_ANSWER";
                return true;
            }
            if (GetOrAddRegex(@"\b(?:PHẦN|PHAN|PART|SECTION)\s+(?:II|2)\b").IsMatch(upper))
            {
                sectionKey = "TRUE_FALSE";
                return true;
            }
            if (GetOrAddRegex(@"\b(?:PHẦN|PHAN|PART|SECTION)\s+(?:I|1|A)\b").IsMatch(upper))
            {
                sectionKey = "SINGLE";
                return true;
            }
            return false;
        }

        private bool TryMatchSectionMapping(string line, Dictionary<string, List<string>>? mapping, out string sectionKey)
        {
            sectionKey = string.Empty;
            if (mapping == null || mapping.Count == 0) return false;

            foreach (var key in OrderSectionKeys(mapping.Keys))
            {
                if (MatchesAny(line, mapping[key]))
                {
                    sectionKey = key;
                    return true;
                }
            }
            return false;
        }

        private static IEnumerable<string> OrderSectionKeys(IEnumerable<string> keys)
        {
            return keys.OrderBy(k =>
            {
                int idx = Array.IndexOf(SectionKeySpecificityOrder, k.ToUpperInvariant());
                return idx >= 0 ? idx : SectionKeySpecificityOrder.Length - 1.5; // khóa lạ xếp ngay trước SINGLE
            });
        }

        /// <summary>Nhận diện dòng chuyển sang pha "Lời giải tham khảo / Hướng dẫn chấm" (sau pha này các "Câu N" là tham chiếu lời giải).</summary>
        public bool TryMatchSolutionPhase(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            if (MatchesAny(line, _structureDictionary.SolutionPhaseMarkers)) return true;
            if (SnapshotRules().Any(rule => MatchesAny(line, rule.LifecycleSpec?.SolutionPhasePatterns))) return true;
            return GetOrAddRegex(BuiltInSolutionPhasePattern).IsMatch(line);
        }

        /// <summary>Nhận diện tiêu đề "Bảng đáp án" / "Answer Key".</summary>
        public bool TryMatchAnswerTablePhase(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            if (MatchesAny(line, _structureDictionary.AnswerPhaseMarkers)) return true;
            if (SnapshotRules().Any(rule => MatchesAny(line, rule.LifecycleSpec?.AnswerPhasePatterns))) return true;
            return GetOrAddRegex(BuiltInAnswerPhasePattern).IsMatch(line);
        }

        /// <summary>Nhận diện ranh giới kết thúc đề thi (HẾT, THE END, END OF TEST, END OF PAPER...).</summary>
        public bool TryMatchEndOfExam(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            var clean = line.Trim();
            if (MatchesAny(clean, _structureDictionary.EndOfExamMarkers)) return true;
            if (SnapshotRules().Any(rule => MatchesAny(clean, rule.LifecycleSpec?.EndOfExamPatterns))) return true;
            return GetOrAddRegex(BuiltInEndOfExamPattern).IsMatch(clean);
        }

        /// <summary>Nhận diện dòng chỉ thị coi thi / ghi chú phụ ngoài nội dung câu hỏi (Cán bộ coi thi..., Thí sinh không được...).</summary>
        public bool IsProctorDisclaimerLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            var clean = line.Trim();
            if (MatchesAny(clean, _structureDictionary.ProctorInstructionMarkers)) return true;
            if (SnapshotRules().Any(rule => MatchesAny(clean, rule.LifecycleSpec?.ProctorInstructionPatterns))) return true;
            return GetOrAddRegex(BuiltInProctorPattern).IsMatch(clean);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Bài đọc hiểu (Reading Passage Scoping)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Nhận diện dòng mở đầu bài đọc hiểu và (nếu có) phạm vi câu hỏi áp dụng.
        /// <paramref name="startQ"/>/<paramref name="endQ"/> = 0 khi không xác định được phạm vi.
        /// </summary>
        public bool TryMatchReadingPassage(string line, out int startQ, out int endQ)
        {
            startQ = 0;
            endQ = 0;
            if (string.IsNullOrWhiteSpace(line)) return false;

            var rules = SnapshotRules();
            bool isPassageStart = MatchesAny(line, _structureDictionary.PassageStartPatterns)
                                  || rules.Any(rule => MatchesAny(line, rule.StimulusSpec?.PassageStartPatterns))
                                  || GetOrAddRegex(BuiltInPassageStartPattern).IsMatch(line);
            if (!isPassageStart) return false;

            if (TryCaptureRange(line, _structureDictionary.PassageRangePatterns, out startQ, out endQ)) return true;
            foreach (var rule in rules)
            {
                if (TryCaptureRange(line, rule.StimulusSpec?.PassageRangePatterns, out startQ, out endQ)) return true;
            }
            TryCaptureRange(line, new[] { BuiltInPassageRangePattern }, out startQ, out endQ);
            return true;
        }

        private bool TryCaptureRange(string line, IEnumerable<string>? patterns, out int start, out int end)
        {
            start = 0;
            end = 0;
            if (patterns == null) return false;

            foreach (var pattern in patterns)
            {
                if (string.IsNullOrWhiteSpace(pattern)) continue;
                var match = GetOrAddRegex(pattern).Match(line);
                if (match.Success && match.Groups.Count >= 3
                    && int.TryParse(match.Groups[1].Value, out var s)
                    && int.TryParse(match.Groups[2].Value, out var e))
                {
                    start = s;
                    end = e;
                    return true;
                }
            }
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // API tương thích ngược (chỉ dùng từ điển cấu trúc / SectionMarkers của quy tắc)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Khớp tiêu đề phân đoạn theo từ điển cấu trúc hoặc <c>AnchorSpec.SectionMarkers</c> của quy tắc (API cũ).</summary>
        public bool MatchSectionHeader(string line, out string questionType)
        {
            questionType = string.Empty;
            if (string.IsNullOrWhiteSpace(line)) return false;

            var text = line.Trim();
            if (text.Length > 120 && !text.StartsWith("PHẦN", StringComparison.OrdinalIgnoreCase) && !text.StartsWith("PART", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (TryMatchSectionMapping(text, _structureDictionary.SectionMarkers, out questionType)) return true;

            foreach (var rule in SnapshotRules())
            {
                if (rule.AnchorSpec?.SectionMarkers == null) continue;
                foreach (var marker in rule.AnchorSpec.SectionMarkers)
                {
                    if (string.IsNullOrWhiteSpace(marker)) continue;
                    if (text.Equals(marker, StringComparison.OrdinalIgnoreCase)
                        || Regex.IsMatch(text, @"^(?:\d+[\.\s]+)?" + Regex.Escape(marker) + @"\b", RegexOptions.IgnoreCase))
                    {
                        questionType = rule.InteractionSpec?.DefaultQuestionType ?? "SINGLE";
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>Khớp pha lời giải chỉ theo từ điển cấu trúc (API cũ).</summary>
        public bool MatchSolutionPhase(string line)
            => !string.IsNullOrWhiteSpace(line) && MatchesAny(line.Trim(), _structureDictionary.SolutionPhaseMarkers);

        /// <summary>Khớp biên giới đề thi chỉ theo từ điển cấu trúc (API cũ).</summary>
        public bool MatchExamBoundary(string line, out string examCode)
        {
            examCode = string.Empty;
            if (string.IsNullOrWhiteSpace(line)) return false;
            var captured = FirstCapture(line.Trim(), _structureDictionary.ExamBoundaryMarkers);
            if (captured == null) return false;
            examCode = captured;
            return true;
        }

        /// <summary>
        /// Bóc tách bảng đáp án ma trận (các cặp cột "Câu | Đáp án" lặp lại) vào
        /// <paramref name="map"/>[ExamCode][QuestionType][QuestionNumber] = đáp án (SINGLE lưu chỉ số 1-4, còn lại lưu nguyên văn).
        /// </summary>
        public bool TryExtractAnswerGrid(List<List<string>> rowData, string currentExam, Dictionary<string, Dictionary<string, Dictionary<int, string>>> map)
        {
            if (rowData == null || rowData.Count < 2) return false;
            var header = rowData[0];
            if (header.Count < 2) return false;

            var specs = _structureDictionary.AnswerTableSpecs ?? new AnswerTableDictionaryDto();
            var questionHeaderRegex = GetOrAddRegex(specs.QuestionHeaderPattern);
            var answerHeaderRegex = GetOrAddRegex(specs.AnswerHeaderPattern);

            var columnPairs = new List<(int QuestionCol, int AnswerCol)>();
            for (int i = 0; i < header.Count - 1; i += 2)
            {
                if (questionHeaderRegex.IsMatch(header[i].Trim()) && answerHeaderRegex.IsMatch(header[i + 1].Trim()))
                {
                    columnPairs.Add((i, i + 1));
                }
            }
            if (columnPairs.Count == 0) return false;

            if (!map.ContainsKey(currentExam))
            {
                map[currentExam] = new Dictionary<string, Dictionary<int, string>>();
            }

            int extracted = 0;
            for (int r = 1; r < rowData.Count; r++)
            {
                var row = rowData[r];
                foreach (var (qCol, aCol) in columnPairs)
                {
                    if (qCol >= row.Count || aCol >= row.Count) continue;
                    if (!int.TryParse(Regex.Replace(row[qCol], @"[^\d]", ""), out int qNum)) continue;

                    var answer = row[aCol].Trim();
                    if (string.IsNullOrEmpty(answer)) continue;

                    string type = Regex.IsMatch(answer, "^[A-D]$", RegexOptions.IgnoreCase) ? "SINGLE" : "SHORT_ANSWER";
                    if (!map[currentExam].ContainsKey(type))
                    {
                        map[currentExam][type] = new Dictionary<int, string>();
                    }
                    map[currentExam][type][qNum] = type == "SINGLE"
                        ? (char.ToUpperInvariant(answer[0]) - 'A' + 1).ToString()
                        : answer;
                    extracted++;
                }
            }
            return extracted > 0;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Phân loại nhanh O(1) & chẩn đoán dị biệt cú pháp (Autopilot trigger)
        // ─────────────────────────────────────────────────────────────────────

        private static readonly FastClassificationSpecDto BuiltInFastSpec = new()
        {
            SectionPrefixes = new() { "PHẦN", "PHAN", "PART", "SECTION", "CHƯƠNG", "CHUONG", "BẢNG ĐÁP ÁN", "B. TỰ LUẬN", "II. TỰ LUẬN" },
            ExamPrefixes = new() { "ĐỀ", "EXAM", "TEST", "MÃ ĐỀ", "BỘ ĐỀ" },
            QuestionPrefixes = new() { "CÂU", "BÀI", "QUESTION", "ITEM", "TASK", "EXERCISE", "AUFGABE", "EXERCICE", "FRAGE" },
            QuestionAnchorChars = new() { "第", "問", "문", "제", "【", "〔" },
            OptionDelimiters = new() { ".", ")", ":", " ", "、", "．" },
            OptionCharRanges = new() { new CharRangeDto { Start = "A", End = "D" }, new CharRangeDto { Start = "a", End = "d" }, new CharRangeDto { Start = "1", End = "4" } },
            EvaluationPrefixes = new() { "ĐÁP ÁN", "ANSWER", "KEY", "LỜI GIẢI", "HƯỚNG DẪN", "ĐÁP SỐ", "KẾT QUẢ", "SOLUTION", "EXPLANATION" }
        };

        private FastClassificationSpecDto GetFastSpec()
        {
            var spec = _structureDictionary.FastClassificationSpec;
            bool hasData = spec != null && (spec.SectionPrefixes.Count > 0 || spec.QuestionPrefixes.Count > 0 || spec.OptionCharRanges.Count > 0);
            return hasData ? spec! : BuiltInFastSpec;
        }

        /// <summary>Phân loại nhanh một dòng theo bảng tiền tố / ký tự nạp từ từ điển cấu trúc (không Regex).</summary>
        public FastLineCategory FastClassifyLine(string? line)
        {
            var text = (line ?? string.Empty).TrimStart();
            if (text.Length == 0) return FastLineCategory.Empty;

            var spec = GetFastSpec();
            if (StartsWithAny(text, spec.SectionPrefixes)) return FastLineCategory.SectionHeader;
            if (StartsWithAny(text, spec.ExamPrefixes)) return FastLineCategory.ExamHeader;
            if (StartsWithAny(text, spec.QuestionPrefixes)) return FastLineCategory.QuestionAnchor;

            if (text.Length >= 2 && InAnyRange(text[0], spec.OptionCharRanges)
                && spec.OptionDelimiters.Any(d => d.Length == 1 && text[1] == d[0]))
            {
                return FastLineCategory.OptionOrSubItem;
            }

            if (StartsWithAny(text, spec.EvaluationPrefixes)) return FastLineCategory.EvaluationOrSolution;
            if (spec.QuestionAnchorChars.Any(c => !string.IsNullOrEmpty(c) && text.StartsWith(c, StringComparison.Ordinal))) return FastLineCategory.QuestionAnchor;

            return FastLineCategory.GeneralContent;
        }

        private static bool StartsWithAny(string text, IEnumerable<string> prefixes)
            => prefixes.Any(p => !string.IsNullOrEmpty(p) && text.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        private static bool InAnyRange(char c, IEnumerable<CharRangeDto> ranges)
            => ranges.Any(r => !string.IsNullOrEmpty(r.Start) && !string.IsNullOrEmpty(r.End) && c >= r.Start[0] && c <= r.End[0]);

        /// <summary>
        /// Đo chỉ số dị biệt cú pháp của một đoạn văn bản: tỷ lệ dòng "trông như" điểm neo/tiêu đề nhưng không khớp quy tắc nào.
        /// Chỉ số ≥ 0.35 (hoặc phát hiện mẫu lạ) → gợi ý kích hoạt AI Rule Autopilot.
        /// </summary>
        public bool DetectSyntaxAnomaly(string sampleText, out double anomalyIndex, out string detectedPatternHint)
        {
            anomalyIndex = 0.0;
            detectedPatternHint = string.Empty;
            if (string.IsNullOrWhiteSpace(sampleText)) return false;

            var lines = sampleText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 3) return false;

            int structuralLines = 0;
            int matchedAnchors = 0;
            foreach (var raw in lines)
            {
                var text = raw.Trim();
                if (string.IsNullOrEmpty(text)) continue;

                var category = FastClassifyLine(text);
                if (category is FastLineCategory.QuestionAnchor or FastLineCategory.ExamHeader or FastLineCategory.SectionHeader)
                {
                    structuralLines++;
                }

                if (TryMatchQuestionAnchor(text, out _, out _))
                {
                    matchedAnchors++;
                }
                else if (GetOrAddRegex(@"^(?:\[\s*Câu|\#\s*\d+|Task\s*\#?\d+|Q\d+|Item\s*\d+|Mục\s*\d+)").IsMatch(text))
                {
                    detectedPatternHint = text.Length > 40 ? text.Substring(0, 40) + "..." : text;
                }
            }

            if (structuralLines > 0)
            {
                anomalyIndex = 1.0 - (double)matchedAnchors / structuralLines;
            }
            else if (lines.Any(l => GetOrAddRegex(@"^(?:>>\s*[A-D]\s*<<|\[\s*[A-D]\s*\]|Phương\s*án\s*\d+)").IsMatch(l.Trim())))
            {
                anomalyIndex = 0.85;
                detectedPatternHint = "Phương án trắc nghiệm định dạng phi truyền thống";
            }

            return anomalyIndex >= 0.35 || !string.IsNullOrEmpty(detectedPatternHint);
        }

        // ─────────────────────────────────────────────────────────────────────
        // SGI — Phạm vi nhận thức chỉ thị dải câu & ngữ cảnh bài đọc (Zero-Hardcode)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Tạo một đối tượng CognitiveScopeManager mới tuân thủ interface ICognitiveScopeManager.
        /// </summary>
        public ICognitiveScopeManager CreateScopeManager()
        {
            return new AegisQuiz.Infrastructure.Services.Docx.CognitiveScopeManager();
        }

        /// <summary>
        /// Phân tích dòng văn bản để trích xuất chỉ thị dải câu (Scope Directive) 100% từ dữ liệu cấu hình.
        /// </summary>
        public bool TryParseScopeDirective(string line, out DirectiveScopeWindow? window)
        {
            window = null;
            if (string.IsNullOrWhiteSpace(line)) return false;

            int startQ = 0, endQ = 0;
            bool foundRange = false;

            // 1. Quét dải số câu từ cấu hình ScopeRangePatterns
            var rangePatterns = _structureDictionary.ScopeRangePatterns ?? new List<string>();
            foreach (var pat in rangePatterns)
            {
                var match = GetOrAddRegex(pat).Match(line);
                if (match.Success)
                {
                    if (match.Groups.Count >= 3 && int.TryParse(match.Groups[1].Value, out var s) && int.TryParse(match.Groups[2].Value, out var e))
                    {
                        startQ = s;
                        endQ = e;
                        foundRange = true;
                        break;
                    }
                    else if (match.Groups.Count >= 2 && int.TryParse(match.Groups[1].Value, out var sSingle))
                    {
                        startQ = sSingle;
                        endQ = 9999;
                        foundRange = true;
                        break;
                    }
                }
            }

            if (!foundRange || startQ <= 0) return false;
            if (endQ < startQ) endQ = startQ;

            // 2. Nhận diện hành vi khảo thí từ cấu hình ScopeDirectiveMarkers
            string? detectedType = null;
            var directiveMarkers = _structureDictionary.ScopeDirectiveMarkers;
            if (directiveMarkers != null)
            {
                // Duyệt theo thứ tự ưu tiên: MULTI_TRUE_FALSE -> SHORT_ANSWER -> ORDERING -> MATCHING -> MULTI -> SINGLE -> ESSAY
                var priorityOrder = new[] { "MULTI_TRUE_FALSE", "SHORT_ANSWER", "ORDERING", "MATCHING", "MULTI", "SINGLE", "ESSAY" };
                foreach (var type in priorityOrder)
                {
                    if (directiveMarkers.TryGetValue(type, out var patterns) && patterns != null)
                    {
                        if (patterns.Any(p => GetOrAddRegex(p).IsMatch(line)))
                        {
                            detectedType = type;
                            break;
                        }
                    }
                }
            }

            if (detectedType == null) return false;

            window = new DirectiveScopeWindow
            {
                StartQuestion = startQ,
                EndQuestion = endQ,
                QuestionType = detectedType,
                RawDirective = line.Trim(),
                Confidence = 0.95
            };
            return true;
        }

        /// <summary>
        /// Nhận diện dòng bài đọc / tình huống thực tế áp dụng cho dải câu hỏi.
        /// </summary>
        public bool TryParsePassageContextPrompt(string line, out int startQ, out int endQ, out string contextTitle)
        {
            startQ = 0;
            endQ = 0;
            contextTitle = string.Empty;
            if (string.IsNullOrWhiteSpace(line)) return false;

            var rules = SnapshotRules();
            bool isPassagePrompt = MatchesAny(line, _structureDictionary.PassageStartPatterns)
                                  || rules.Any(rule => MatchesAny(line, rule.StimulusSpec?.PassageStartPatterns))
                                  || GetOrAddRegex(BuiltInPassageStartPattern).IsMatch(line);

            if (!isPassagePrompt) return false;

            if (TryCaptureRange(line, _structureDictionary.ScopeRangePatterns, out startQ, out endQ)
                || TryCaptureRange(line, _structureDictionary.PassageRangePatterns, out startQ, out endQ))
            {
                // Thỏa mãn
            }
            else
            {
                foreach (var rule in rules)
                {
                    if (TryCaptureRange(line, rule.StimulusSpec?.PassageRangePatterns, out startQ, out endQ)) break;
                }
            }

            if (startQ <= 0)
            {
                TryCaptureRange(line, new[] { BuiltInPassageRangePattern }, out startQ, out endQ);
            }

            if (startQ <= 0) return false;
            if (endQ < startQ) endQ = startQ;

            contextTitle = BuildContextTitle(startQ, endQ);
            return true;
        }

        /// <summary>
        /// [CIG v4.0 - Structural Boundary Streamer]
        /// Làm phẳng danh sách paragraphs thành các dòng văn bản đơn vị (tokens),
        /// chỉ phân tách tại các ranh giới cấu trúc khách quan (Sau ảnh Media hoặc Sau dấu kết thúc câu logic).
        /// Tuyệt đối không ngắt giữa câu văn tự do hay trong cụm chỉ thị phạm vi (từ... đến...).
        /// </summary>
        public List<string> FlattenDocument(IEnumerable<string> paragraphs, DocumentLanguageProfileDto? languageProfile = null)
        {
            var flattenedLines = new List<string>();
            const string questionBoundaryLookahead = @"(?=(?:Câu|Question|Item|Task|Exercise|Bài|Bai)\s*\d+(?:[\.\:\)\–—\-]|[\s\t]*[\.\:\)\–—\-])\s*)";

            foreach (var p in paragraphs)
            {
                if (string.IsNullOrWhiteSpace(p)) continue;

                // 1. Tách ranh giới cấu trúc nếu câu hỏi đứng ngay sau ảnh/media Markdown: !\[...\]\(...\) hoặc data:image
                var normalizedP = Regex.Replace(p, @"(!\[[^\]]*\]\([^)]+\)|data:image\/[a-z0-9\+\-\.]+;base64,[A-Za-z0-9+/=]+)\s*" + questionBoundaryLookahead, "$1\n\n", RegexOptions.IgnoreCase);

                // 2. Tách nếu hai câu hỏi bị dính chùm trên cùng một paragraph sau dấu ngắt câu (. ; ! ? ) ])
                // Phía trước KHÔNG ĐƯỢC là giới từ hoặc từ chỉ phạm vi (từ, đến, from, to, ở, theo, in, at, xem, vd...)
                normalizedP = Regex.Replace(normalizedP, @"(?<=[\.\!\?;\)\]])\s*(?<!\b(?:từ|đến|ở|theo|trong|from|to|at|in|see|vd|ví\s*dụ)\s+)" + questionBoundaryLookahead, "\n\n", RegexOptions.IgnoreCase);

                var subLines = normalizedP.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var sl in subLines)
                {
                    var trimmed = sl.Trim();
                    if (!string.IsNullOrEmpty(trimmed)) flattenedLines.Add(trimmed);
                }
            }
            return flattenedLines;
        }

        /// <summary>
        /// [CHIP v4.0 - Header Reconnaissance & Profiling]
        /// Quét và trích xuất hồ sơ tổng quan của tài liệu từ các đoạn văn bản đầu đề thi.
        /// </summary>
        public DocumentHeaderProfileDto ProfileHeader(
            IEnumerable<string> headerParagraphs,
            IEnumerable<string>? sampleContents = null,
            DynamicDomainContext? dynamicContext = null)
        {
            var headerList = headerParagraphs?.ToList() ?? new List<string>();
            var profile = new DocumentHeaderProfileDto
            {
                DurationSeconds = 60,
                TaxonomyCoordinates = UniversalCoordinateTaxonomyEngine.SniffCoordinates(
                    fileName: null,
                    sheetName: null,
                    headerBannerLines: headerList,
                    sampleContents: sampleContents ?? Enumerable.Empty<string>(),
                    dynamicContext: dynamicContext)
            };

            // Ưu tiên TopicCode đã được phát hiện từ Vỏ bọc Header đề thi
            if (!string.IsNullOrEmpty(profile.TaxonomyCoordinates?.TopicCode) && profile.TaxonomyCoordinates.TopicCode != "GENERAL")
            {
                profile.DetectedSubject = profile.TaxonomyCoordinates.TopicCode;
            }

            foreach (var p in headerList)
            {
                if (TryMatchTopicHeading(p, out string rawSubj) && (profile.DetectedSubject == null || profile.DetectedSubject == "GENERAL"))
                {
                    string cleanSubj = SanitizeSubjectHeading(rawSubj);
                    string resolved = UniversalCoordinateTaxonomyEngine.ResolveTopicCode(cleanSubj, dynamicContext);
                    if (resolved != "GENERAL" && resolved != "EDUCATION" && resolved != "THPT_NATIONAL" && resolved != "MON" && resolved != "BAI_THI")
                    {
                        profile.DetectedSubject = resolved;
                        if (profile.TaxonomyCoordinates != null)
                        {
                            profile.TaxonomyCoordinates.TopicCode = resolved;
                        }
                    }
                }

                var dur = TryExtractDurationMinutes(p);
                if (dur.HasValue && dur.Value > 0)
                {
                    profile.DurationSeconds = dur.Value * 60;
                }

                if (TryExtractExamCode(p, out var examCode) && !string.IsNullOrEmpty(examCode))
                {
                    profile.DetectedExamCode = examCode;
                }
            }

            if (profile.TaxonomyCoordinates != null && !string.IsNullOrEmpty(profile.DetectedSubject) && profile.DetectedSubject != "GENERAL")
            {
                var matched = dynamicContext?.Topics?.FirstOrDefault(t => t.Code.Equals(profile.DetectedSubject, StringComparison.OrdinalIgnoreCase))
                    ?? DynamicDomainContext.CreateDefault().Topics.FirstOrDefault(t => t.Code.Equals(profile.DetectedSubject, StringComparison.OrdinalIgnoreCase));
                if (matched != null)
                {
                    profile.TaxonomyCoordinates.TopicName = matched.Name;
                    if (!string.IsNullOrWhiteSpace(matched.DomainCode) && matched.DomainCode != "GENERAL")
                    {
                        profile.TaxonomyCoordinates.DomainCode = matched.DomainCode;
                    }
                }
            }

            return profile;
        }

        /// <summary>
        /// Kiểm tra dòng thông tin thí sinh, giám thị, số báo danh, thời gian làm bài nếu chưa vào câu hỏi.
        /// </summary>
        public bool IsStudentInfoLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            return Regex.IsMatch(line, @"(?i)(?:thời\s*gian\s*làm\s*bài|không\s*kể\s*thời\s*gian|số\s*báo\s*danh|họ\s*(?:và)?\s*tên\s*thí\s*sinh)");
        }

        /// <summary>
        /// Trích xuất mã đề thi ("DE_01", "DE_02"...) từ dòng văn bản.
        /// </summary>
        public bool TryExtractExamCode(string line, out string? examCode)
        {
            examCode = null;
            if (string.IsNullOrWhiteSpace(line)) return false;
            var match = Regex.Match(line, @"(?i)(?:Mã\s*đề(?:\s*thi)?|Đề(?:\s*thi)?\s*số|Exam\s*Code|Test\s*Code)[\:\s\-]*([A-Za-z0-9_\-]+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var rawCode = match.Groups[1].Value.Trim().Trim('_');
                if (!string.IsNullOrEmpty(rawCode))
                {
                    // Nếu là mã đề thi chuẩn (độ dài >= 3 chữ số như "0323", "101", "204") -> bảo toàn nguyên vẹn chuỗi số có số 0 ở đầu
                    if (rawCode.Length >= 3)
                    {
                        examCode = rawCode;
                        return true;
                    }
                    if (int.TryParse(rawCode, out int cNum))
                    {
                        examCode = $"DE_{cNum:D2}";
                        return true;
                    }
                    examCode = rawCode;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Trích xuất thời lượng thi (phút) từ dòng văn bản.
        /// </summary>
        public int? TryExtractDurationMinutes(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;
            var matchDuration = Regex.Match(line, @"(?:thời\s*gian(?:\s*làm\s*bài)?|thời\s*lượng|duration)[\:\s\-]*(\d+)\s*(?:phút|min|minutes)", RegexOptions.IgnoreCase);
            if (matchDuration.Success && int.TryParse(matchDuration.Groups[1].Value, out int durMins) && durMins > 0)
            {
                return durMins;
            }
            return null;
        }

        /// <summary>
        /// Kiểm tra số đề có phải là năm học (1900..2099) để tránh nhầm mã đề.
        /// </summary>
        public bool IsYearLikeNumber(int examNum)
        {
            return examNum >= 1900 && examNum <= 2099;
        }

        /// <summary>
        /// Làm sạch tiêu đề môn học, loại bỏ các từ khóa rác phía sau như: đề, chính thức, thử, mã, khối...
        /// </summary>
        public string SanitizeSubjectHeading(string rawSubject)
        {
            if (string.IsNullOrWhiteSpace(rawSubject)) return string.Empty;
            return Regex.Replace(rawSubject, @"\b(?:đề|de|chính\s*thức|chinh\s*thuc|thử|thu|mã|khối|khoi|lớp|lop|năm|nam|thời\s*gian|thoi\s*gian|học\s*kỳ|hoc\s*ky)\b.*$", "", RegexOptions.IgnoreCase).Trim();
        }

        /// <summary>
        /// Xây dựng mã đề chuẩn hóa theo tiền tố của ngữ cảnh miền (VD: DE_01, EXAM_01...).
        /// </summary>
        public string BuildExamCode(int examNum, DynamicDomainContext? dynamicContext = null)
        {
            string examPrefix = "DE_";
            if (dynamicContext != null && !string.IsNullOrWhiteSpace(dynamicContext.DefaultExamPrefix)
                && dynamicContext.DefaultExamPrefix != "EXAM" && dynamicContext.DefaultExamPrefix != "DE")
            {
                examPrefix = $"{dynamicContext.DefaultExamPrefix}_";
            }
            return $"{examPrefix}{examNum:D2}";
        }

        /// <summary>
        /// Tạo tiêu đề ngữ cảnh bài đọc hiểu chuẩn hóa theo khoảng câu hỏi.
        /// </summary>
        public string BuildContextTitle(int startQ, int endQ)
        {
            return startQ == endQ
                ? $"Bài đọc hiểu (Câu {startQ})"
                : (endQ >= 9999 ? $"Bài đọc hiểu (Từ câu {startQ})" : $"Bài đọc hiểu (Câu {startQ} - {endQ})");
        }
    }
}
