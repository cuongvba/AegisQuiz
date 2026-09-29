using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AegisQuiz.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [CIG Engine — CHIP v3.0 Dual-Layer Language Cognition]
    /// Tầng 1 Pre-flight Analysis: nhận diện hồ sơ ngôn ngữ kép cấp tài liệu
    /// (HostLanguage = ngôn ngữ khung tài liệu, SubjectLanguage = ngôn ngữ câu hỏi) và
    /// lọc tập quy tắc thích ứng (Adaptive Rule Pruning) để loại bỏ xung đột nhận diện chéo giữa 14 quốc gia.
    ///
    /// Vector chấm điểm ngôn ngữ (docs §6.2):
    ///   Score(L) = w_script·Σ Count(ScriptPatterns_L) + w_diacritic·Σ Count(DiacriticPattern_L) + w_kw·Σ Count(KeywordHints_L)
    /// </summary>
    public partial class DynamicQuestionIngestionEngine
    {
        private const double ScriptWeight = 2.5;
        private const double DiacriticWeight = 2.0;
        private const double KeywordWeight = 15.0;
        private const int HeadSampleSize = 50;
        private const int TailSampleSize = 40;

        private static readonly HashSet<string> UniversalLanguageCodes = new(StringComparer.OrdinalIgnoreCase)
            { "all", "general", "universal", "any", "*", "" };

        /// <summary>Chữ ký ngôn ngữ lõi — chỉ dùng khi từ điển cấu trúc chưa cấu hình <c>languageSignatures</c>.</summary>
        private static readonly Dictionary<string, LanguageSignatureDto> BuiltInLanguageSignatures = new(StringComparer.OrdinalIgnoreCase)
        {
            ["vi"] = new()
            {
                Name = "Vietnamese",
                DiacriticPattern = "[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴĐ]",
                KeywordHints = new() { "Câu", "Bài", "Đáp án", "Lời giải", "Hướng dẫn", "Chọn", "Phần", "Đề", "Môn", "Trắc nghiệm", "Tự luận", "Đúng", "Sai" }
            },
            ["en"] = new()
            {
                Name = "English",
                KeywordHints = new() { "Question", "Choose", "Answer", "Correct", "Which", "Following", "Part", "Section", "Listen", "Read", "Passage", "Complete", "Underlined", "Pronounced", "Task", "Item" }
            },
            ["zh"] = new() { Name = "Chinese", ScriptPatterns = new() { @"[\u4E00-\u9FFF]" }, KeywordHints = new() { "分", "下列", "题", "正确的", "选择题", "填空题", "解答题" }, RequireEvidenceForAnchor = true },
            ["ko"] = new() { Name = "Korean", ScriptPatterns = new() { @"[\uAC00-\uD7AF\u1100-\u11FF]" }, KeywordHints = new() { "점", "다음", "대화", "밑줄", "물음에", "답하시오" }, RequireEvidenceForAnchor = true },
            ["ja"] = new() { Name = "Japanese", ScriptPatterns = new() { @"[\u3040-\u309F\u30A0-\u30FF\u31F0-\u31FF]" }, KeywordHints = new() { "問い", "答えよ", "選択肢", "正しいもの", "次の文章" }, RequireEvidenceForAnchor = true },
            ["fr"] = new() { Name = "French", DiacriticPattern = "[éèêëàâùûôîïçœæÉÈÊËÀÂÙÛÔÎÏÇŒÆ]", KeywordHints = new() { "Exercice", "Partie", "Points", "Justifier", "Montrer que", "Question", "Problème", "Calculer" }, RequireEvidenceForAnchor = true },
            ["de"] = new() { Name = "German", DiacriticPattern = "[äöüßÄÖÜ]", KeywordHints = new() { "Aufgabe", "Berechnen", "Bestimmen", "Punkte", "Begründen", "Frage", "Lösung" }, RequireEvidenceForAnchor = true }
        };

        private Dictionary<string, LanguageSignatureDto> GetLanguageSignatures()
        {
            var configured = _structureDictionary.LanguageSignatures;
            return configured != null && configured.Count > 0 ? configured : BuiltInLanguageSignatures;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Tầng 1: Pre-flight Document Language Profiling
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Quét mẫu 50 dòng đầu + 40 dòng cuối tài liệu, chấm điểm vector ngôn ngữ cho từng dòng và phân bổ vào
        /// hai lớp: khung tài liệu (tiêu đề đề/phân đoạn/lời giải/bảng đáp án → <c>HostLanguage</c>) và
        /// nội dung câu hỏi (→ <c>SubjectLanguage</c>). Khi hai lớp khác ngôn ngữ → chế độ Bilingual Exam.
        /// </summary>
        public DocumentLanguageProfileDto DetectDocumentLanguageProfile(IEnumerable<string> paragraphs, string defaultLanguage = "vi")
        {
            var fallback = string.IsNullOrWhiteSpace(defaultLanguage) ? "vi" : defaultLanguage.Trim().ToLowerInvariant();
            var profile = new DocumentLanguageProfileDto { HostLanguage = fallback, SubjectLanguage = fallback };

            var lines = (paragraphs ?? Enumerable.Empty<string>())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .ToList();
            if (lines.Count == 0) return profile;

            var sample = lines.Count <= HeadSampleSize + TailSampleSize
                ? lines
                : lines.Take(HeadSampleSize).Concat(lines.Skip(lines.Count - TailSampleSize)).ToList();

            var signatures = GetLanguageSignatures();
            var hostScores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var subjectScores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var totalScores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            bool inSolutionPhase = false;
            foreach (var line in sample)
            {
                if (TryMatchExamHeading(line, out _)) inSolutionPhase = false;
                bool isPhaseMarker = TryMatchSolutionPhase(line) || TryMatchAnswerTablePhase(line);
                if (isPhaseMarker) inSolutionPhase = true;

                bool structural = isPhaseMarker || inSolutionPhase || IsStructuralLine(line);
                var bucket = structural ? hostScores : subjectScores;

                foreach (var (lang, signature) in signatures)
                {
                    double score = ScoreLine(line, signature);
                    if (score <= 0) continue;
                    Accumulate(bucket, lang, score);
                    Accumulate(totalScores, lang, score);
                }
            }

            profile.SampledParagraphs = sample.Count;
            profile.LanguageScores = totalScores;

            string host = ArgMax(hostScores) ?? ArgMax(totalScores) ?? fallback;
            string subject = ArgMax(subjectScores) ?? host;
            profile.HostLanguage = host;
            profile.SubjectLanguage = subject;

            double bucketSum = hostScores.Values.Sum() + subjectScores.Values.Sum();
            if (bucketSum > 0)
            {
                double winning = (hostScores.TryGetValue(host, out var h) ? h : 0) + (subjectScores.TryGetValue(subject, out var s) ? s : 0);
                profile.Confidence = Math.Round(Math.Clamp(winning / bucketSum * 100.0, 0, 100), 2);
            }

            _logger?.LogDebug("[DynamicIngestionEngine] DetectDocumentLanguageProfile | Sampled={Sampled} -> {Profile}", sample.Count, profile);
            return profile;
        }

        /// <summary>Dòng thuộc khung tài liệu: tiêu đề đề / chủ đề / phân đoạn / pha lời giải, hoặc dòng tiêu đề viết hoa toàn bộ.</summary>
        private bool IsStructuralLine(string line)
        {
            if (TryMatchQuestionAnchor(line, out _, out _)) return false;
            if (TryMatchExamHeading(line, out _) || TryMatchTopicHeading(line, out _) || TryMatchSectionHeader(line, out _)) return true;

            int letters = 0, upper = 0;
            foreach (var ch in line)
            {
                if (!char.IsLetter(ch)) continue;
                letters++;
                if (char.IsUpper(ch)) upper++;
            }
            return letters >= 5 && upper >= letters * 0.8;
        }

        private double ScoreLine(string line, LanguageSignatureDto signature)
        {
            double score = 0;

            foreach (var pattern in signature.ScriptPatterns ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(pattern)) continue;
                score += ScriptWeight * GetOrAddRegex(pattern).Matches(line).Count;
            }

            if (!string.IsNullOrWhiteSpace(signature.DiacriticPattern))
            {
                score += DiacriticWeight * GetOrAddRegex(signature.DiacriticPattern).Matches(line).Count;
            }

            foreach (var keyword in signature.KeywordHints ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(keyword)) continue;
                score += KeywordWeight * GetOrAddRegex(BuildKeywordPattern(keyword)).Matches(line).Count;
            }

            return score;
        }

        /// <summary>Từ khóa khớp nguyên từ (không dính liền chữ cái khác), khoảng trắng trong cụm từ chấp nhận nhiều khoảng trắng.</summary>
        private static string BuildKeywordPattern(string keyword)
        {
            var escaped = Regex.Replace(Regex.Escape(keyword.Trim()), @"(\\\s|\s)+", @"\s+");
            return @"(?<!\p{L})" + escaped + @"(?!\p{L})";
        }

        private static void Accumulate(Dictionary<string, double> scores, string lang, double value)
        {
            scores[lang] = scores.TryGetValue(lang, out var current) ? current + value : value;
        }

        private static string? ArgMax(Dictionary<string, double> scores)
            => scores.Count == 0 ? null : scores.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).First().Key;

        // ─────────────────────────────────────────────────────────────────────
        // Adaptive Rule Pruning (docs §6.3)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Sắp xếp tập quy tắc theo ma trận ưu tiên của hồ sơ ngôn ngữ:
        /// Nhóm 1 = ngôn ngữ chuyên môn (Subject) → Nhóm 2 = ngôn ngữ khung (Host, khi song ngữ) →
        /// Nhóm 3 = quy tắc phổ quát ("all") → Nhóm 4 = còn lại (dự phòng). Trong mỗi nhóm giữ thứ tự Priority.
        /// </summary>
        public IReadOnlyList<QuestionParsingRuleDto> GetRulesForProfile(DocumentLanguageProfileDto profile, Guid? tenantId = null)
        {
            var rules = SnapshotRules();
            if (profile == null) return rules;

            string subject = profile.SubjectLanguage ?? string.Empty;
            string host = profile.HostLanguage ?? subject;
            bool bilingual = profile.IsBilingual;

            int Group(QuestionParsingRuleDto rule)
            {
                var lang = rule.Language ?? string.Empty;
                if (string.Equals(lang, subject, StringComparison.OrdinalIgnoreCase)) return 0;
                if (bilingual && string.Equals(lang, host, StringComparison.OrdinalIgnoreCase)) return 1;
                if (IsUniversalLanguage(lang)) return 2;
                return 3;
            }

            return rules
                .Select((rule, index) => (rule, index))
                .OrderBy(x => Group(x.rule))
                .ThenBy(x => x.rule.Priority)
                .ThenBy(x => x.index)
                .Select(x => x.rule)
                .ToList();
        }

        private static bool IsUniversalLanguage(string? language) => UniversalLanguageCodes.Contains(language ?? string.Empty);

        /// <summary>
        /// Quy tắc mang ngôn ngữ yêu cầu bằng chứng (RequireEvidenceForAnchor) chỉ được thử khớp điểm neo khi dòng
        /// chứa hệ chữ viết / dấu / từ khóa của ngôn ngữ đó — chặn nhận diện chéo (ví dụ quy tắc Hàn Quốc trên đề Việt).
        /// </summary>
        private bool HasLanguageEvidenceForAnchor(string? language, string line)
        {
            if (IsUniversalLanguage(language)) return true;
            if (!GetLanguageSignatures().TryGetValue(language!, out var signature) || !signature.RequireEvidenceForAnchor) return true;

            if (signature.ScriptPatterns != null && signature.ScriptPatterns.Any(p => !string.IsNullOrWhiteSpace(p) && GetOrAddRegex(p).IsMatch(line))) return true;
            if (!string.IsNullOrWhiteSpace(signature.DiacriticPattern) && GetOrAddRegex(signature.DiacriticPattern).IsMatch(line)) return true;
            return signature.KeywordHints != null && signature.KeywordHints.Any(k => !string.IsNullOrWhiteSpace(k) && GetOrAddRegex(BuildKeywordPattern(k)).IsMatch(line));
        }
    }
}
