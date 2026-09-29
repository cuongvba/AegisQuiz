using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AegisQuiz.Domain.Entities
{
    /// <summary>
    /// [GDPT 2025 — Trắc nghiệm Đúng/Sai đa mệnh đề] Một câu hỏi gồm nhiều ý a), b), c), d)...
    /// mỗi ý là một mệnh đề Đúng/Sai độc lập. Chấm điểm theo tỷ lệ ý đúng, hoặc theo thang
    /// TNPT 2025 (4 ý: 1 đúng = 0.1; 2 đúng = 0.25; 3 đúng = 0.5; 4 đúng = 1.0) khi Payload có <c>scoringScheme = "TNPT_2025"</c>.
    /// Đáp án chuẩn lấy từ Payload (<c>correctMap</c> / <c>correctAnswers</c> / <c>subItems</c>) hoặc từ chuỗi <c>CorrectOption</c>
    /// ("a) Đúng, b) Sai, c) Đúng, d) Đúng").
    /// </summary>
    public class MultiTrueFalseQuestion : QuestionBase
    {
        private static readonly Regex LabeledTruthPattern =
            new(@"(?:^|\b|\,)([a-z0-9]+)[\)\:\.\s]+(đúng|sai|true|false|đ|s|t|f)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public MultiTrueFalseQuestion()
        {
            QuestionType = "MULTI_TRUE_FALSE";
        }

        public override double EvaluateAnswer(string studentAnswerJson)
            => EvaluateMultiTf(Payload, CorrectOption, Options, studentAnswerJson);

        /// <summary>Chấm điểm thuần túy (không phụ thuộc instance) — dùng lại cho Arena, Sandbox và kiểm thử.</summary>
        public static double EvaluateMultiTf(JsonDocument? payload, string? correctOption, List<string>? options, string studentAnswerJson)
        {
            if (string.IsNullOrWhiteSpace(studentAnswerJson)) return 0.0;

            var expected = ExtractExpectedMap(payload, correctOption, options);
            int totalItems = options != null && options.Count > 0
                ? options.Count
                : (expected.Count > 0 ? expected.Count : 4);
            if (totalItems <= 0) return 0.0;

            var actual = ParseStudentAnswers(studentAnswerJson);
            if (actual.Count == 0) return 0.0;

            int correctCount = 0;
            foreach (var (key, expectedValue) in expected)
            {
                if (actual.TryGetValue(key, out var studentValue) && studentValue == expectedValue)
                {
                    correctCount++;
                }
            }

            if (payload != null
                && TryGetPayloadProperty(payload, "scoringScheme", out var scheme)
                && string.Equals(scheme.GetString()?.Trim(), "TNPT_2025", StringComparison.OrdinalIgnoreCase)
                && totalItems == 4)
            {
                return correctCount switch
                {
                    4 => 1.0,
                    3 => 0.5,
                    2 => 0.25,
                    1 => 0.1,
                    _ => 0.0
                };
            }

            return Math.Round((double)correctCount / totalItems, 4);
        }

        /// <summary>Trích bản đồ đáp án chuẩn {a: true, b: false, ...} từ Payload hoặc chuỗi CorrectOption.</summary>
        public static Dictionary<string, bool> ExtractExpectedMap(JsonDocument? payload, string? correctOption, List<string>? options)
        {
            var map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            if (payload != null)
            {
                if (TryGetPayloadProperty(payload, "correctMap", out var correctMap) && correctMap.ValueKind == JsonValueKind.Object)
                {
                    ReadBooleanObject(correctMap, map);
                    if (map.Count > 0) return map;
                }

                if (TryGetPayloadProperty(payload, "correctAnswers", out var correctAnswers) && correctAnswers.ValueKind == JsonValueKind.Object)
                {
                    ReadBooleanObject(correctAnswers, map);
                    if (map.Count > 0) return map;
                }

                if (TryGetPayloadProperty(payload, "subItems", out var subItems) && subItems.ValueKind == JsonValueKind.Array)
                {
                    int index = 0;
                    foreach (var item in subItems.EnumerateArray())
                    {
                        string id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty;
                        if (string.IsNullOrEmpty(id)) id = ((char)('a' + index)).ToString();

                        if (item.TryGetProperty("expected", out var expectedEl))
                        {
                            map[NormalizeKey(id)] = expectedEl.ValueKind == JsonValueKind.True;
                        }
                        else if (item.TryGetProperty("correctAnswer", out var correctEl))
                        {
                            map[NormalizeKey(id)] = ParseBoolToken(correctEl.GetString());
                        }
                        index++;
                    }
                    if (map.Count > 0) return map;
                }
            }

            var raw = correctOption ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) return map;

            var labeled = LabeledTruthPattern.Matches(raw);
            if (labeled.Count > 0)
            {
                foreach (Match m in labeled)
                {
                    map[NormalizeKey(m.Groups[1].Value)] = ParseBoolToken(m.Groups[2].Value);
                }
                return map;
            }

            // Dạng rút gọn "Đ,S,Đ,Đ" hoặc "true false true true"
            var tokens = raw.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tokens.Length; i++)
            {
                map[((char)('a' + i)).ToString()] = ParseBoolToken(tokens[i]);
            }
            return map;
        }

        /// <summary>Đọc bài làm thí sinh: JSON {"a":true,...}, {"matching":{...}} hoặc chuỗi "a) Đúng, b) Sai".</summary>
        public static Dictionary<string, bool> ParseStudentAnswers(string studentAnswerJson)
        {
            var map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var doc = JsonDocument.Parse(studentAnswerJson);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("matching", out var matching) && matching.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in matching.EnumerateObject())
                    {
                        map[NormalizeKey(prop.Name)] = ParseBoolToken(prop.Value.ToString());
                    }
                    return map;
                }

                if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in root.EnumerateObject())
                    {
                        map[NormalizeKey(prop.Name)] = prop.Value.ValueKind is JsonValueKind.True or JsonValueKind.False
                            ? prop.Value.GetBoolean()
                            : ParseBoolToken(prop.Value.ToString());
                    }
                    return map;
                }
            }
            catch (JsonException)
            {
                foreach (Match m in LabeledTruthPattern.Matches(studentAnswerJson))
                {
                    map[NormalizeKey(m.Groups[1].Value)] = ParseBoolToken(m.Groups[2].Value);
                }
            }
            return map;
        }

        private static void ReadBooleanObject(JsonElement obj, Dictionary<string, bool> target)
        {
            foreach (var prop in obj.EnumerateObject())
            {
                if (prop.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    target[NormalizeKey(prop.Name)] = prop.Value.GetBoolean();
                }
                else if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    target[NormalizeKey(prop.Name)] = ParseBoolToken(prop.Value.GetString());
                }
            }
        }

        /// <summary>Chuẩn hóa nhãn ý: "1".."26" → "a".."z"; còn lại hạ chữ thường.</summary>
        private static string NormalizeKey(string rawKey)
        {
            var key = rawKey.Trim().ToLowerInvariant();
            if (int.TryParse(key, out var n) && n >= 1 && n <= 26)
            {
                return ((char)('a' + n - 1)).ToString();
            }
            return key;
        }

        private static bool ParseBoolToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            var t = token.Trim().ToLowerInvariant();
            return t.StartsWith("đ") || t.StartsWith("t") || t == "d" || t == "1" || t == "true";
        }
    }
}
