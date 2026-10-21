using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Services.Docx
{
    /// <summary>
    /// [Cognitive Scope Manager - SGI Engine]
    /// Bộ quản lý Phạm vi Nhận thức Khảo thí Hợp nhất:
    /// Tự động bao trùm cả 4 hình thái cấu trúc:
    ///   1. Cây phân cấp chuẩn (Phần I, II, III...)
    ///   2. Dải chỉ thị tuyến tính ("Từ câu 1 đến câu 12...", "Questions 1-5...")
    ///   3. Ngữ cảnh lồng nhau (Passage Context + Directives)
    ///   4. Dòng chảy phẳng (Zero Header Fallback)
    /// </summary>
    public class CognitiveScopeManager : ICognitiveScopeManager
    {
        private readonly List<DirectiveScopeWindow> _scopeWindows = new();
        private readonly List<ContextStimulusWindow> _contextWindows = new();

        public string CurrentMacroSection { get; private set; } = "SINGLE";
        public string? CurrentSectionTitle { get; private set; }

        public IReadOnlyList<DirectiveScopeWindow> ScopeWindows => _scopeWindows;
        public IReadOnlyList<ContextStimulusWindow> ContextWindows => _contextWindows;

        /// <summary>
        /// Cập nhật Phân đoạn Vĩ mô khi gặp tiêu đề phân đoạn lớn (PHẦN I, PHẦN II, SECTION A, TỰ LUẬN...).
        /// </summary>
        public void RegisterMacroSection(string sectionKey, string? sectionTitle)
        {
            if (!string.IsNullOrWhiteSpace(sectionKey))
            {
                CurrentMacroSection = sectionKey.Trim().ToUpperInvariant();
                CurrentSectionTitle = sectionTitle?.Trim();
            }
        }

        /// <summary>
        /// Đăng ký một Cửa sổ Chỉ thị Phạm vi mới.
        /// </summary>
        public void RegisterScopeDirective(DirectiveScopeWindow window)
        {
            if (window == null || window.StartQuestion <= 0) return;
            if (window.EndQuestion < window.StartQuestion) window.EndQuestion = window.StartQuestion;
            _scopeWindows.Add(window);
        }

        /// <summary>
        /// Đăng ký một Cửa sổ Chỉ thị Phạm vi mới qua các tham số nguyên thủy.
        /// </summary>
        public void RegisterScopeDirective(int startQ, int endQ, string questionType, string? rawDirective, double confidence = 1.0)
        {
            if (startQ <= 0) return;
            if (endQ < startQ) endQ = startQ;

            _scopeWindows.Add(new DirectiveScopeWindow
            {
                StartQuestion = startQ,
                EndQuestion = endQ,
                QuestionType = questionType,
                RawDirective = rawDirective,
                Confidence = confidence
            });
        }

        /// <summary>
        /// Đăng ký ngữ cảnh bài đọc / tình huống thực tế áp dụng cho dải câu hỏi.
        /// </summary>
        public void RegisterContextPassage(int startQ, int endQ, string content, string? title = null, string? contextId = null)
        {
            if (startQ <= 0) return;
            if (endQ < startQ) endQ = startQ;

            string safeTitle = !string.IsNullOrWhiteSpace(title) 
                ? title.Trim() 
                : (endQ < int.MaxValue && endQ > startQ ? $"Bài đọc hiểu (Câu {startQ} - {endQ})" : $"Bài đọc hiểu (Từ câu {startQ})");

            _contextWindows.Add(new ContextStimulusWindow
            {
                StartQuestion = startQ,
                EndQuestion = endQ,
                ContextId = contextId ?? $"CTX_{startQ}_{endQ}",
                ContextTitle = safeTitle,
                ContextContent = content.Trim()
            });
        }

        /// <summary>
        /// Suy luận loại câu hỏi kỳ vọng cho câu hỏi số qNum:
        /// Ưu tiên 1: Cửa sổ chỉ thị phạm vi hẹp nhất bao trùm qNum.
        /// Ưu tiên 2: Fallback chỉ định hoặc Phân đoạn vĩ mô hiện tại (CurrentMacroSection).
        /// </summary>
        public string ResolveExpectedQuestionType(int qNum, string? fallbackType = null)
        {
            if (qNum > 0)
            {
                var matched = _scopeWindows.LastOrDefault(w => w.Contains(qNum));
                if (matched != null && !string.IsNullOrWhiteSpace(matched.QuestionType) && matched.QuestionType != "UNKNOWN")
                {
                    return matched.QuestionType;
                }
            }

            return !string.IsNullOrWhiteSpace(fallbackType) ? fallbackType : CurrentMacroSection;
        }

        /// <summary>
        /// Tìm ngữ cảnh bài đọc hiểu / tình huống tương ứng cho câu hỏi số qNum.
        /// </summary>
        public ContextStimulusWindow? ResolveContextForQuestion(int qNum)
        {
            if (qNum <= 0) return null;
            return _contextWindows.LastOrDefault(w => w.Contains(qNum));
        }

        // ─────────────────────────────────────────────────────────────────────
        // BỘ PHÂN TÍCH CÚ PHÁP CHỈ THỊ ĐA NGÔN NGỮ (SGI SCANNER)
        // ─────────────────────────────────────────────────────────────────────

        private static readonly Regex RangePatternVi = new(
            @"(?i)(?:từ\s+(?:các\s+)?câu(?:\s+số)?|ở\s+mỗi\s+câu(?:\s+hỏi)?\s*(?:từ)?|(?:các\s+)?câu(?:\s+số|\s+hỏi)?)\s*(\d+)\s*(?:đến\s*(?:các\s+)?câu(?:\s+số)?|đến|[-–—])\s*(\d+)",
            RegexOptions.Compiled);

        private static readonly Regex RangePatternOpenVi = new(
            @"(?i)(?:từ\s+câu|từ)\s*(\d+)\s*(?:đến\s*hết|trở\s*đi)",
            RegexOptions.Compiled);

        private static readonly Regex RangePatternEn = new(
            @"(?i)(?:questions?|items?|from\s+question|for\s+questions?|in\s+questions?)\s*(\d+)\s*(?:to|through|[-–—])\s*(?:questions?\s*)?(\d+)",
            RegexOptions.Compiled);

        private static readonly Regex RangePatternEnSuffix = new(
            @"(?i)(?:questions?|items?)\s+from\s*(\d+)\s*to\s*(\d+)",
            RegexOptions.Compiled);

        /// <summary>
        /// Phân tích một dòng văn bản để trích xuất Chỉ thị Phạm vi Khảo thí (SGI).
        /// Trả về true nếu dòng văn bản chứa chỉ thị phạm vi kèm hành vi tương tác hợp lệ.
        /// </summary>
        public static bool TryParseScopeDirective(string line, out DirectiveScopeWindow? window)
        {
            window = null;
            if (string.IsNullOrWhiteSpace(line)) return false;

            int startQ = 0, endQ = 0;
            bool foundRange = false;

            // 1. Quét dải số câu (Tiếng Việt & Tiếng Anh)
            var mVi = RangePatternVi.Match(line);
            if (mVi.Success)
            {
                startQ = int.Parse(mVi.Groups[1].Value);
                endQ = int.Parse(mVi.Groups[2].Value);
                foundRange = true;
            }
            else
            {
                var mEn = RangePatternEn.Match(line);
                if (mEn.Success)
                {
                    startQ = int.Parse(mEn.Groups[1].Value);
                    endQ = int.Parse(mEn.Groups[2].Value);
                    foundRange = true;
                }
                else
                {
                    var mEnSuffix = RangePatternEnSuffix.Match(line);
                    if (mEnSuffix.Success)
                    {
                        startQ = int.Parse(mEnSuffix.Groups[1].Value);
                        endQ = int.Parse(mEnSuffix.Groups[2].Value);
                        foundRange = true;
                    }
                    else
                    {
                        var mOpen = RangePatternOpenVi.Match(line);
                        if (mOpen.Success)
                        {
                            startQ = int.Parse(mOpen.Groups[1].Value);
                            endQ = 9999;
                            foundRange = true;
                        }
                    }
                }
            }

            if (!foundRange || startQ <= 0) return false;
            if (endQ < startQ) endQ = startQ;

            // 2. Nhận diện Hành vi / Động từ Khảo thí (Cognitive Action Verb)
            string lower = line.ToLowerInvariant();
            string? detectedType = null;

            // Ưu tiên 1: Đúng / Sai 4 ý (Multi True/False)
            if (Regex.IsMatch(lower, @"(?i)(?:mỗi\s+ý\s+[a-d]|các\s+mệnh\s+đề|mỗi\s+câu\s+có\s+4\s+ý).*?(?:chọn\s+đúng\s+(?:hoặc|hay)\s+sai|đúng\s*[-–\/\s]*sai|true\s*[-–\/\s]*false)")
                || Regex.IsMatch(lower, @"(?i)(?:chọn\s+đúng\s+(?:hoặc|hay)\s+sai|thí\s*sinh\s*chọn\s*đúng\s*hoặc\s*sai|mệnh\s+đề\s+đúng\s+hay\s+sai|indicate\s+whether\s+each\s+statement\s+is\s+true\s+or\s+false)"))
            {
                detectedType = "MULTI_TRUE_FALSE";
            }
            // Ưu tiên 2: Trả lời ngắn / Điền khuyết (Short Answer / Fill in the blank)
            else if (Regex.IsMatch(lower, @"(?i)(?:(?:phần\s+iii|part\s+iii|section\s+iii).*?trả\s*lời|trả\s*lời\s*ngắn|yêu\s*cầu\s*trả\s*lời\s*ngắn|điền\s*(?:đáp\s*số|vào\s*chỗ\s*trống)|viết\s*câu\s*trả\s*lời|kết\s*quả\s*làm\s*tròn|fill\s*in\s*the\s*blank|short\s*answer|write\s*your\s*answer)"))
            {
                detectedType = "SHORT_ANSWER";
            }
            // Ưu tiên 3: Sắp xếp thứ tự quy trình (Ordering)
            else if (Regex.IsMatch(lower, @"(?i)(?:sắp\s*xếp\s*(?:các\s*(?:bước|câu|đoạn|ý))?|thứ\s*tự\s*(?:thực\s*hiện|đúng)|arrange\s+the\s+(?:utterances|sentences|steps)|chronological\s+order)"))
            {
                detectedType = "ORDERING";
            }
            // Ưu tiên 4: Nối cặp / Ghép cột (Matching)
            else if (Regex.IsMatch(lower, @"(?i)(?:ghép\s*(?:nội\s*dung|thông\s*tin)?\s*(?:ở\s*)?cột\s+[a-b1-2]|nối\s*(?:mệnh\s*đề|cột)|match\s*(?:each\s*item|the\s*items)?\s*in\s*column)"))
            {
                detectedType = "MATCHING";
            }
            // Ưu tiên 5: Chọn nhiều phương án (Multi Choice)
            else if (Regex.IsMatch(lower, @"(?i)(?:chọn\s+(?:tất\s*cả|các)\s+phương\s*án\s+đúng|có\s+thể\s+chọn\s+nhiều\s+đáp\s+án|choose\s+all\s+that\s+apply|select\s+all\s+correct)"))
            {
                detectedType = "MULTI";
            }
            // Ưu tiên 6: Chọn một phương án (Single Choice)
            else if (Regex.IsMatch(lower, @"(?i)(?:chỉ\s*chọn\s*(?:một|01|1)\s*phương\s*án|(?:mỗi\s+câu(?:\s+hỏi)?(?:\s+thí\s+sinh)?\s+)?chọn\s+(?:một|01|1)\s+phương\s*án|chọn\s*(?:một|01|1)\s*phương\s*án\s*đúng|chọn\s+phương\s*án\s*(?:đúng\s*nhất|chính\s*xác\s*nhất)|mark\s+the\s+letter\s+[a-d]|choose\s+the\s+(?:best|correct)\s+answer|select\s+one\s+answer|chọn\s+câu\s+trả\s+lời\s+đúng)"))
            {
                detectedType = "SINGLE";
            }
            // Ưu tiên 7: Tự luận (Essay)
            else if (Regex.IsMatch(lower, @"(?i)(?:tự\s*luận|thí\s*sinh\s*giải\s*(?:các\s*)?bài\s*toán|trình\s*bày\s*(?:chi\s*tiết\s*)?lời\s*giải|thang\s*điểm.*?điểm|solve\s+the\s+following|show\s+your\s+work)"))
            {
                detectedType = "ESSAY";
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
        public static bool TryParsePassageContextPrompt(string line, out int startQ, out int endQ, out string contextTitle)
        {
            startQ = 0;
            endQ = 0;
            contextTitle = string.Empty;
            if (string.IsNullOrWhiteSpace(line)) return false;

            var lower = line.ToLowerInvariant();
            bool isPassagePrompt = lower.Contains("đọc đoạn văn") || lower.Contains("đọc bài văn") || lower.Contains("đọc thông tin")
                                  || lower.Contains("read the following passage") || lower.Contains("read the passage")
                                  || lower.Contains("read the following text") || lower.Contains("read the following advertisement")
                                  || lower.Contains("sử dụng thông tin") || lower.Contains("dựa vào thông tin") || lower.Contains("dựa trên thông tin")
                                  || lower.Contains("cho thông tin") || lower.Contains("dùng thông tin") || lower.Contains("tình huống sau")
                                  || lower.Contains("dưới đây để trả lời") || lower.Contains("sau để trả lời");

            if (!isPassagePrompt) return false;

            var mVi = RangePatternVi.Match(line);
            if (mVi.Success)
            {
                startQ = int.Parse(mVi.Groups[1].Value);
                endQ = int.Parse(mVi.Groups[2].Value);
            }
            else
            {
                var mEn = RangePatternEn.Match(line);
                if (mEn.Success)
                {
                    startQ = int.Parse(mEn.Groups[1].Value);
                    endQ = int.Parse(mEn.Groups[2].Value);
                }
                else
                {
                    var mEnSuffix = RangePatternEnSuffix.Match(line);
                    if (mEnSuffix.Success)
                    {
                        startQ = int.Parse(mEnSuffix.Groups[1].Value);
                        endQ = int.Parse(mEnSuffix.Groups[2].Value);
                    }
                }
            }

            if (startQ <= 0) return false;
            if (endQ < startQ) endQ = startQ;

            contextTitle = startQ == endQ
                ? $"Bài đọc hiểu (Câu {startQ})"
                : (endQ >= 9999 ? $"Bài đọc hiểu (Từ câu {startQ})" : $"Bài đọc hiểu (Câu {startQ} - {endQ})");

            return true;
        }
    }
}
