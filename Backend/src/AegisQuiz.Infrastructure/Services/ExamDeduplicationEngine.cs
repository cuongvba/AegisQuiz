using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [Động cơ Nhận diện Bản thể học Đề thi & Khử trùng lặp Đa tầng v2026]
    /// Hiện thực hóa trọn vẹn:
    /// 1. Canonical Fingerprinting (Chuẩn hóa câu hỏi & công thức toán, băm SHA-256)
    /// 2. Jaccard Exam Clustering (Nhận diện đề thi con nằm trong tệp tuyển tập lớn)
    /// 3. Multi-Provenance Merge (Hợp nhất lịch sử xuất hiện đa nguồn vào JSONB Payload)
    /// </summary>
    public class ExamDeduplicationEngine : IExamDeduplicationEngine
    {
        private readonly AegisQuizDbContext _db;
        private readonly ILogger<ExamDeduplicationEngine> _logger;

        public ExamDeduplicationEngine(AegisQuizDbContext db, ILogger<ExamDeduplicationEngine> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ── 1. CANONICAL CONTENT FINGERPRINTING ───────────────────────────────────

        public string ComputeCanonicalContentHash(string content, IEnumerable<string>? options)
            => GenerateCanonicalFingerprint(content, options);

        public static string GenerateCanonicalFingerprint(string content, IEnumerable<string>? options)
        {
            var normalizedContent = NormalizeQuestionText(content);
            var normalizedOptions = (options ?? Enumerable.Empty<string>())
                .Select(NormalizeOptionText)
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .OrderBy(o => o, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var combinedSb = new StringBuilder(normalizedContent);
            if (normalizedOptions.Count > 0)
            {
                combinedSb.Append("||OPTS:");
                foreach (var opt in normalizedOptions)
                {
                    combinedSb.Append('|').Append(opt);
                }
            }

            var bytes = Encoding.UTF8.GetBytes(combinedSb.ToString());
            var hashBytes = SHA256.HashData(bytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private static string NormalizeQuestionText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var cleaned = text;

            // 1. Loại bỏ các tiền tố đánh số thứ tự câu ("Câu 1:", "Bài 12.", "Question 4 -", "1.")
            cleaned = Regex.Replace(cleaned, @"^(?i)(?:câu|cau|bài|bai|question|item|q)\s*\d+[\.\:\s\-]*", "", RegexOptions.Compiled);
            cleaned = Regex.Replace(cleaned, @"^\d+[\.\:\)\-]\s*", "", RegexOptions.Compiled);

            // 2. Loại bỏ các thẻ HTML và markdown formatting (không xóa dấu * nhân toán học đơn lẻ)
            cleaned = Regex.Replace(cleaned, @"<[^>]+>", " ", RegexOptions.Compiled);
            cleaned = Regex.Replace(cleaned, @"\*{2,}", "", RegexOptions.Compiled);
            cleaned = Regex.Replace(cleaned, @"_{2,}", "", RegexOptions.Compiled);
            cleaned = Regex.Replace(cleaned, @"[`~]+", "", RegexOptions.Compiled);

            // 3. Chuẩn hóa các toán tử tương đương (Unicode & LaTeX) trước khi chuẩn hóa khoảng trắng
            cleaned = cleaned.Replace(@"\times", "*")
                             .Replace(@"\cdot", "*")
                             .Replace(@"\div", "/")
                             .Replace(@"\le", "<=")
                             .Replace(@"\leq", "<=")
                             .Replace(@"\ge", ">=")
                             .Replace(@"\geq", ">=")
                             .Replace(@"\ne", "!=")
                             .Replace(@"\neq", "!=")
                             .Replace("×", "*")
                             .Replace("⋅", "*")
                             .Replace("÷", "/")
                             .Replace("≤", "<=")
                             .Replace("≥", ">=")
                             .Replace("≠", "!=")
                             .Replace("–", "-")
                             .Replace("—", "-");

            // 4. Chuẩn hóa công thức toán LaTeX & MathType (loại bỏ khoảng trắng bên trong công thức)
            cleaned = Regex.Replace(cleaned, @"\$([^$]+)\$", m => "$" + Regex.Replace(m.Groups[1].Value, @"\s+", "") + "$", RegexOptions.Compiled);

            // 5. Thu gọn khoảng trắng và chuyển về chữ thường
            cleaned = Regex.Replace(cleaned, @"\s+", " ", RegexOptions.Compiled).Trim();
            return cleaned.ToLowerInvariant();
        }

        private static string NormalizeOptionText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            var cleaned = text;

            // Loại bỏ nhãn phương án ("A. ", "B) ", "1. ", "a - ")
            cleaned = Regex.Replace(cleaned, @"^(?i)[a-d\d]+[\.\:\)\-]\s*", "", RegexOptions.Compiled);

            // Thu gọn khoảng trắng và chuyển chữ thường
            cleaned = Regex.Replace(cleaned, @"\s+", " ", RegexOptions.Compiled).Trim();
            return cleaned.ToLowerInvariant();
        }

        // ── 2. JACCARD SIMILARITY / EXAM CLUSTERING ──────────────────────────────

        public double ComputeJaccardSimilarity(IEnumerable<string> setA, IEnumerable<string> setB)
        {
            var hashSetA = new HashSet<string>(setA ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var hashSetB = new HashSet<string>(setB ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            if (hashSetA.Count == 0 && hashSetB.Count == 0) return 1.0;
            if (hashSetA.Count == 0 || hashSetB.Count == 0) return 0.0;

            int intersectionCount = hashSetA.Intersect(hashSetB).Count();
            int unionCount = hashSetA.Union(hashSetB).Count();

            return unionCount > 0 ? (double)intersectionCount / unionCount : 0.0;
        }

        // ── 3. DEDUPLICATION ANALYSIS & TAGGING ──────────────────────────────────

        public async Task<ExamDeduplicationReport> AnalyzeAndTagDuplicatesAsync(
            List<DocxImportPreviewDto> incomingQuestions,
            Guid tenantId,
            CancellationToken ct = default)
        {
            var report = new ExamDeduplicationReport
            {
                TotalIncomingQuestions = incomingQuestions?.Count ?? 0
            };

            if (incomingQuestions == null || incomingQuestions.Count == 0)
            {
                return report;
            }

            // 1. Tính toán CanonicalContentHash cho từng câu hỏi đến
            foreach (var q in incomingQuestions)
            {
                if (string.IsNullOrWhiteSpace(q.CanonicalContentHash))
                {
                    q.CanonicalContentHash = ComputeCanonicalContentHash(q.Content, q.Options);
                }
            }

            var incomingHashes = incomingQuestions
                .Select(q => q.CanonicalContentHash!)
                .Distinct()
                .ToList();

            // 2. Tra cứu cơ sở dữ liệu để tìm các câu hỏi đã có cùng Hash
            // Tối ưu hóa: Truy vấn các câu hỏi có sẵn trong cùng Tenant hoặc Community Commons
            var existingCandidates = await _db.Questions
                .AsNoTracking()
                .Where(q => q.TenantId == tenantId || q.TenantId == Guid.Empty || q.TenantId == Tenant.CommunityTenantId)
                .Select(q => new
                {
                    q.Id,
                    q.CategoryCode,
                    q.Payload
                })
                .ToListAsync(ct);

            var dbHashMap = new Dictionary<string, (Guid QuestionId, string TopicCode, JsonDocument Payload)>(StringComparer.OrdinalIgnoreCase);
            foreach (var candidate in existingCandidates)
            {
                try
                {
                    if (candidate.Payload != null && candidate.Payload.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        if (candidate.Payload.RootElement.TryGetProperty("canonicalContentHash", out var hashEl) 
                            && hashEl.ValueKind == JsonValueKind.String)
                        {
                            var h = hashEl.GetString();
                            if (!string.IsNullOrWhiteSpace(h) && !dbHashMap.ContainsKey(h))
                            {
                                dbHashMap[h] = (candidate.Id, candidate.CategoryCode, candidate.Payload);
                            }
                        }
                    }
                }
                catch
                {
                    // Bỏ qua nếu payload lỗi
                }
            }

            // 3. Đánh dấu câu hỏi trùng lặp
            int duplicates = 0;
            foreach (var q in incomingQuestions)
            {
                if (dbHashMap.TryGetValue(q.CanonicalContentHash!, out var match))
                {
                    q.IsDuplicate = true;
                    q.ExistingQuestionId = match.QuestionId;
                    q.DuplicateReason = $"Trùng khớp 100% với câu hỏi có sẵn (ID: {match.QuestionId}, Chủ đề: {match.TopicCode})";
                    duplicates++;

                    // Đọc lịch sử xuất hiện cũ nếu có
                    try
                    {
                        if (match.Payload.RootElement.TryGetProperty("_provenance", out var provEl) 
                            && provEl.ValueKind == JsonValueKind.Object)
                        {
                            if (provEl.TryGetProperty("occurrences", out var occEl) && occEl.ValueKind == JsonValueKind.Number)
                            {
                                q.OccurrenceCount = occEl.GetInt32() + 1;
                            }
                        }
                    }
                    catch { }
                }
                else
                {
                    q.IsDuplicate = false;
                    q.ExistingQuestionId = null;
                }
            }

            report.DuplicateCount = duplicates;
            report.UniqueCount = incomingQuestions.Count - duplicates;

            // 4. Phát hiện Cụm Đề Thi (Exam Clustering / Composite Ingestion Detection)
            var incomingGroups = incomingQuestions
                .GroupBy(q => string.IsNullOrWhiteSpace(q.SourceExamName) ? (q.TopicName ?? "DEFAULT") : q.SourceExamName)
                .ToList();

            foreach (var group in incomingGroups)
            {
                var groupHashes = group.Select(q => q.CanonicalContentHash!).ToList();
                int matchedInDb = group.Count(q => q.IsDuplicate);

                if (group.Count() >= 5) // Chỉ đánh giá cụm đề khi có từ 5 câu trở lên
                {
                    double overlapRatio = (double)matchedInDb / group.Count();
                    if (overlapRatio >= 0.70)
                    {
                        var firstMatch = group.First(q => q.IsDuplicate);
                        var topicCode = dbHashMap[firstMatch.CanonicalContentHash!].TopicCode;

                        report.ClusterMatches.Add(new ExamClusterMatch
                        {
                            ExistingTopicCode = topicCode,
                            ExistingTopicName = topicCode,
                            TotalQuestionsInCluster = group.Count(),
                            MatchedQuestionsCount = matchedInDb,
                            JaccardSimilarityScore = Math.Round(overlapRatio, 2),
                            CanonicalExamUrn = $"urn:aegis:exam:{topicCode.ToLowerInvariant()}",
                            Recommendation = overlapRatio >= 0.85 ? "MERGE" : "REVIEW"
                        });

                        _logger.LogInformation(
                            "[ExamDeduplication] Phát hiện cụm đề trùng lặp cao: '{ExamName}' khớp {Overlap:P0} với chủ đề '{TopicCode}'",
                            group.Key, overlapRatio, topicCode);
                    }
                }
            }

            return report;
        }

        // ── 4. MULTI-PROVENANCE MERGE ─────────────────────────────────────────────

        public void MergeProvenanceAppearance(QuestionBase existingQuestion, ProvenanceAppearance appearance)
        {
            if (existingQuestion == null || appearance == null) return;

            try
            {
                var rootDict = new Dictionary<string, object?>();

                // Sao chép các thuộc tính hiện tại của Payload
                if (existingQuestion.Payload != null && existingQuestion.Payload.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in existingQuestion.Payload.RootElement.EnumerateObject())
                    {
                        if (prop.Name != "_provenance" && prop.Name != "occurrenceCount")
                        {
                            rootDict[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText());
                        }
                    }
                }

                // Đọc hoặc khởi tạo block _provenance
                var appearancesList = new List<ProvenanceAppearance>();
                int currentOccurrences = 1;
                string? firstIngestedAt = null;

                if (existingQuestion.Payload != null 
                    && existingQuestion.Payload.RootElement.TryGetProperty("_provenance", out var provEl)
                    && provEl.ValueKind == JsonValueKind.Object)
                {
                    if (provEl.TryGetProperty("occurrences", out var occEl) && occEl.ValueKind == JsonValueKind.Number)
                    {
                        currentOccurrences = occEl.GetInt32();
                    }
                    if (provEl.TryGetProperty("firstIngestedAt", out var firstEl) && firstEl.ValueKind == JsonValueKind.String)
                    {
                        firstIngestedAt = firstEl.GetString();
                    }
                    if (provEl.TryGetProperty("appearances", out var appArray) && appArray.ValueKind == JsonValueKind.Array)
                    {
                        var parsed = JsonSerializer.Deserialize<List<ProvenanceAppearance>>(appArray.GetRawText());
                        if (parsed != null) appearancesList.AddRange(parsed);
                    }
                }
                else
                {
                    // Tạo appearance đầu tiên từ thông tin có sẵn của question nếu chưa có
                    appearancesList.Add(new ProvenanceAppearance
                    {
                        SourceFile = existingQuestion.SourceFileName ?? "OriginalSource",
                        ExamTitleInFile = existingQuestion.SourceExamName,
                        SectionInFile = existingQuestion.SourceSectionName,
                        OriginalQuestionNo = existingQuestion.SourceQuestionNo,
                        RowIndex = existingQuestion.SourceRowIndex,
                        ImportedAt = existingQuestion.CreatedAt
                    });
                }

                // Bổ sung appearance mới nếu chưa tồn tại
                bool exists = appearancesList.Any(a => 
                    string.Equals(a.SourceFile, appearance.SourceFile, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(a.ExamTitleInFile, appearance.ExamTitleInFile, StringComparison.OrdinalIgnoreCase)
                    && a.RowIndex == appearance.RowIndex);

                if (!exists)
                {
                    appearancesList.Add(appearance);
                    currentOccurrences++;
                }

                var updatedProvenance = new Dictionary<string, object?>
                {
                    ["canonicalContentHash"] = existingQuestion.CanonicalContentHash,
                    ["occurrences"] = currentOccurrences,
                    ["firstIngestedAt"] = firstIngestedAt ?? existingQuestion.CreatedAt.ToString("o"),
                    ["lastSeenAt"] = DateTime.UtcNow.ToString("o"),
                    ["appearances"] = appearancesList
                };

                rootDict["_provenance"] = updatedProvenance;
                rootDict["occurrenceCount"] = currentOccurrences;

                existingQuestion.Payload = JsonSerializer.SerializeToDocument(rootDict);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ExamDeduplication] Lỗi hợp nhất nguồn gốc cho câu hỏi {QuestionId}", existingQuestion.Id);
            }
        }

        // ── 5. MULTI-TOPIC LINKING & UNLINKING (N - M) ───────────────────────────

        public void AssociateTopicToQuestion(QuestionBase question, string topicCode, string? topicName = null)
        {
            if (question == null || string.IsNullOrWhiteSpace(topicCode)) return;

            var cleanCode = topicCode.Trim().TrimStart('#');
            if (cleanCode.StartsWith("topic:", StringComparison.OrdinalIgnoreCase))
            {
                cleanCode = cleanCode.Substring(6).Trim();
            }

            var topicTag = "#topic:" + cleanCode;
            var simpleTag = "#" + cleanCode;

            // 1. Cập nhật Tags (text[])
            question.Tags ??= new List<string>();
            if (!question.Tags.Any(t => string.Equals(t, topicTag, StringComparison.OrdinalIgnoreCase)))
            {
                question.Tags.Add(topicTag);
            }
            if (!question.Tags.Any(t => string.Equals(t, simpleTag, StringComparison.OrdinalIgnoreCase)))
            {
                question.Tags.Add(simpleTag);
            }

            // 2. Cập nhật linkedTopicCodes trong Payload (JSONB)
            try
            {
                var rootDict = new Dictionary<string, object?>();
                if (question.Payload != null && question.Payload.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in question.Payload.RootElement.EnumerateObject())
                    {
                        if (prop.Name != "linkedTopicCodes")
                        {
                            rootDict[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText());
                        }
                    }
                }

                var currentCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(question.CategoryCode)) currentCodes.Add(question.CategoryCode);

                if (question.Payload != null && question.Payload.RootElement.TryGetProperty("linkedTopicCodes", out var ltcEl)
                    && ltcEl.ValueKind == JsonValueKind.Array)
                {
                    var parsed = JsonSerializer.Deserialize<List<string>>(ltcEl.GetRawText());
                    if (parsed != null)
                    {
                        foreach (var c in parsed) currentCodes.Add(c);
                    }
                }

                currentCodes.Add(cleanCode);
                rootDict["linkedTopicCodes"] = currentCodes.ToList();

                question.Payload = JsonSerializer.SerializeToDocument(rootDict);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ExamDeduplication] Lỗi liên kết chủ đề {TopicCode} cho câu hỏi {QuestionId}", topicCode, question.Id);
            }
        }

        public void DisassociateTopicFromQuestion(QuestionBase question, string topicCode, string? fallbackCode = "GENERAL")
        {
            if (question == null || string.IsNullOrWhiteSpace(topicCode)) return;

            var cleanCode = topicCode.Trim().TrimStart('#');
            if (cleanCode.StartsWith("topic:", StringComparison.OrdinalIgnoreCase))
            {
                cleanCode = cleanCode.Substring(6).Trim();
            }

            // 1. Gỡ bỏ khỏi Tags
            if (question.Tags != null && question.Tags.Count > 0)
            {
                question.Tags.RemoveAll(t => 
                    string.Equals(t, cleanCode, StringComparison.OrdinalIgnoreCase) 
                    || string.Equals(t, "#" + cleanCode, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t, "#topic:" + cleanCode, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t, "topic:" + cleanCode, StringComparison.OrdinalIgnoreCase));
            }

            // 2. Gỡ bỏ khỏi linkedTopicCodes trong Payload
            try
            {
                var rootDict = new Dictionary<string, object?>();
                if (question.Payload != null && question.Payload.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in question.Payload.RootElement.EnumerateObject())
                    {
                        if (prop.Name != "linkedTopicCodes")
                        {
                            rootDict[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText());
                        }
                    }
                }

                var currentCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (question.Payload != null && question.Payload.RootElement.TryGetProperty("linkedTopicCodes", out var ltcEl)
                    && ltcEl.ValueKind == JsonValueKind.Array)
                {
                    var parsed = JsonSerializer.Deserialize<List<string>>(ltcEl.GetRawText());
                    if (parsed != null)
                    {
                        foreach (var c in parsed) currentCodes.Add(c);
                    }
                }

                currentCodes.Remove(cleanCode);

                // 3. Nếu CategoryCode chính trùng với chủ đề bị xóa
                if (string.Equals(question.CategoryCode, cleanCode, StringComparison.OrdinalIgnoreCase))
                {
                    var nextTopic = currentCodes.FirstOrDefault();
                    question.CategoryCode = !string.IsNullOrWhiteSpace(nextTopic) ? nextTopic : (fallbackCode ?? "GENERAL");
                }

                rootDict["linkedTopicCodes"] = currentCodes.ToList();
                question.Payload = JsonSerializer.SerializeToDocument(rootDict);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ExamDeduplication] Lỗi gỡ liên kết chủ đề {TopicCode} khỏi câu hỏi {QuestionId}", topicCode, question.Id);
            }
        }
    }
}
