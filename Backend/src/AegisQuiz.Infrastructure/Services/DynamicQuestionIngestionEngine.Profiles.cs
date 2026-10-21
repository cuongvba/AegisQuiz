using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// Truy vấn hồ sơ dạng câu hỏi (OCP) và mẫu ngôn ngữ học đa ngôn ngữ cho bộ phân loại Docx.
    /// Toàn bộ dữ liệu đến từ <see cref="DocumentStructureDictionaryDto"/> (JSON/DB hot-reload).
    /// </summary>
    public partial class DynamicQuestionIngestionEngine
    {
        private QuestionTypeProfileDto? FindTypeProfile(string? questionType)
        {
            if (string.IsNullOrWhiteSpace(questionType)) return null;
            var profiles = _structureDictionary.QuestionTypeProfiles;
            if (profiles == null) return null;
            return profiles.FirstOrDefault(p => string.Equals(p.Key, questionType, System.StringComparison.OrdinalIgnoreCase)).Value;
        }

        /// <summary>Kiểm tra dạng câu hỏi có năng lực (FREE_TEXT, CHOICE, TRUE_FALSE_FAMILY...) hay không.</summary>
        public bool HasTypeCapability(string? questionType, string capability)
        {
            var profile = FindTypeProfile(questionType);
            return profile != null && profile.Capabilities.Any(c => string.Equals(c, capability, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Các dạng tương đương khi tra cứu ĐỀ_PHẦN_CÂU; dạng lạ trả về chính nó.</summary>
        public IReadOnlyList<string> GetEquivalentTypes(string? questionType)
        {
            var key = (questionType ?? string.Empty).ToUpperInvariant();
            var profile = FindTypeProfile(key);
            return profile != null && profile.EquivalentTypes.Count > 0
                ? profile.EquivalentTypes.Select(t => t.ToUpperInvariant()).ToList()
                : new List<string> { key };
        }

        /// <summary>Độ khó mặc định theo hồ sơ dạng câu hỏi.</summary>
        public int GetDefaultDifficulty(string? questionType) => FindTypeProfile(questionType)?.DefaultDifficulty ?? 4;

        /// <summary>Quy tên phần/section (đa ngôn ngữ) về khóa phân đoạn chuẩn theo hồ sơ; null nếu không nhận diện được.</summary>
        public string? TryResolveSectionKeyByAlias(string? rawSection)
        {
            if (string.IsNullOrWhiteSpace(rawSection)) return null;
            var upper = rawSection.ToUpperInvariant();
            var profiles = _structureDictionary.QuestionTypeProfiles;
            if (profiles == null) return null;

            foreach (var profile in profiles.Values.OrderBy(p => p.AliasOrder))
            {
                if (profile.SectionAliasPatterns.Count > 0 && MatchesAny(upper, profile.SectionAliasPatterns))
                    return profile.CanonicalSectionKey?.ToUpperInvariant();
            }
            return null;
        }

        /// <summary>Khớp mẫu ngôn ngữ học theo khóa (<see cref="LinguisticKeys"/>); trả về Match đầu tiên thành công.</summary>
        public bool TryMatchLinguisticPattern(string patternKey, string line, out Match match)
        {
            match = Match.Empty;
            if (string.IsNullOrWhiteSpace(line)) return false;
            if (_structureDictionary.LinguisticPatterns == null
                || !_structureDictionary.LinguisticPatterns.TryGetValue(patternKey, out var patterns)
                || patterns == null) return false;

            foreach (var pattern in patterns)
            {
                var m = GetOrAddRegex(pattern).Match(line);
                if (m.Success)
                {
                    match = m;
                    return true;
                }
            }
            return false;
        }
    }
}
