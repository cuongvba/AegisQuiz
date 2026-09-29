using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AegisQuiz.Application.DTOs;

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
            { "SHORT_ANSWER", "TRUE_FALSE", "MULTI", "MATCHING", "ORDERING", "ESSAY", "SINGLE" };

        private const string BuiltInExamHeadingPattern = @"^(?:ĐỀ|D[E|Ê]|EXAM|TEST|MOCK\s*TEST)\b[^\d\n\r]*?(\d+)\b";
        private const string BuiltInTopicHeadingPattern = @"^(?:Chủ\s*đề|Chương|Lĩnh\s*vực|Môn\s*thi|Môn|Học\s*phần|Mã\s*CĐ|Topic|Subject)[\.\:\s\-]+([^\n\r]+)";
        private const string BuiltInSolutionPhasePattern = @"^(?:LỜI\s*GIẢI|L[\.\?\s]*I\s*GI[\.\?\s]*I|HƯỚNG\s*DẪN\s*GIẢI|HU[\.\?\s]*NG\s*D[\.\?\s]*N\s*GI[\.\?\s]*I|SOLUTIONS?|EXPLANATIONS?|MARK\s*SCHEME)\s+(?:THAM\s*KHẢO|CHI\s*TIẾT|DETAILS?|APPENDIX)[.\:\s\-]*$";
        private const string BuiltInAnswerPhasePattern = @"^(?:ĐÁP\s*ÁN|D[\.\?\s]*P\s*[\.\?\s]*N|ANSWER\s*KEY|KEY|ANSWERS?)\s*(?:THAM\s*KHẢO|CHI\s*TIẾT)?[.\:\s\-]*$";
        private const string BuiltInPassageStartPattern = @"^(?:Read\s+(?:the\s+)?(?:following\s+)?(?:passage|advertisement|leaflet|text|article|announcement)|Đọc\s+(?:đoạn\s+văn|bài\s+đọc|thông\s+báo|tờ\s+rơi)\s+sau)[\s\S]*";
        private const string BuiltInPassageRangePattern = @"\b(?:from|từ|questions?)\s*(\d+)\s*(?:to|đến|-|through)\s*(\d+)\b";

        // ─────────────────────────────────────────────────────────────────────
        // Biên giới đề thi & chủ đề
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Nhận diện tiêu đề đề thi ("ĐỀ 1", "ĐỀ SỐ 01", "EXAM 02", "TEST NO 4"...). Trả về mã / số đề qua <paramref name="examCode"/>.</summary>
        public bool TryMatchExamHeading(string line, out string examCode)
        {
            examCode = string.Empty;
            if (string.IsNullOrWhiteSpace(line)) return false;

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
        /// </summary>
        public bool TryMatchSectionHeader(string line, out string sectionKey)
        {
            sectionKey = string.Empty;
            if (string.IsNullOrWhiteSpace(line)) return false;

            if (TryMatchSectionMapping(line, _structureDictionary.SectionMarkers, out sectionKey)) return true;

            foreach (var rule in SnapshotRules())
            {
                if (TryMatchSectionMapping(line, rule.LifecycleSpec?.SectionHeaderMapping, out sectionKey)) return true;
            }

            // Mẫu lõi dự phòng — kiểm tra từ đặc thù đến tổng quát
            var upper = line.ToUpperInvariant();
            if (GetOrAddRegex(@"\b(?:PHẦN|PHAN|PART|SECTION)\s+(?:III|3|C)\b").IsMatch(upper) || upper.Contains("TRẢ LỜI NGẮN") || upper.Contains("SHORT ANSWER") || upper.Contains("填空题"))
            {
                sectionKey = "SHORT_ANSWER";
                return true;
            }
            if (GetOrAddRegex(@"\b(?:PHẦN|PHAN|PART|SECTION)\s+(?:II|2|B)\b").IsMatch(upper) || upper.Contains("ĐÚNG SAI") || upper.Contains("TRUE FALSE") || upper.Contains("TRUE/FALSE") || upper.Contains("判断题"))
            {
                sectionKey = "TRUE_FALSE";
                return true;
            }
            if (GetOrAddRegex(@"\b(?:PHẦN|PHAN|PART|SECTION)\s+(?:I|1|A)\b").IsMatch(upper) || upper.Contains("TRẮC NGHIỆM") || upper.Contains("MULTIPLE CHOICE") || upper.Contains("单项选择"))
            {
                sectionKey = "SINGLE";
                return true;
            }
            if (upper.Contains("TỰ LUẬN") || upper.Contains("ESSAY") || upper.Contains("FREE RESPONSE") || upper.Contains("XỬ LÝ TÌNH HUỐNG") || upper.Contains("解答题"))
            {
                sectionKey = "ESSAY";
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
    }
}
