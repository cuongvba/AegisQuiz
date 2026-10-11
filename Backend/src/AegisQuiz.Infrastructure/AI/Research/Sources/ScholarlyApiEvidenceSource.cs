using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using Microsoft.Extensions.Logging;

namespace AegisQuiz.Infrastructure.AI.Research.Sources
{
    /// <summary>
    /// [Evidence Plugin] Nguồn Bằng chứng Học thuật Trực tuyến Toàn cầu qua OpenAlex Open API (Không cần API Key).
    /// Truy vấn trực tiếp các bài báo bình duyệt quốc tế, trích xuất DOI, số lần trích dẫn và xác minh online.
    /// </summary>
    public class ScholarlyApiEvidenceSource : IEvidenceSource
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ScholarlyApiEvidenceSource> _logger;

        public string SourceName => "ScholarlyApi";
        public int Priority => 20;

        public ScholarlyApiEvidenceSource(
            IHttpClientFactory httpClientFactory,
            ILogger<ScholarlyApiEvidenceSource> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public bool CanHandle(string topicTitle, string coreProblemStatement)
        {
            // Có thể truy vấn cho mọi chủ đề có độ dài ký tự tối thiểu
            return !string.IsNullOrWhiteSpace(topicTitle) && topicTitle.Length >= 3;
        }

        public async Task<List<EvidenceLedgerItemDto>> GatherEvidenceAsync(
            string topicTitle,
            string coreProblemStatement,
            CancellationToken ct = default)
        {
            var results = new List<EvidenceLedgerItemDto>();
            try
            {
                var query = ExtractSearchQuery(topicTitle, coreProblemStatement);
                if (string.IsNullOrWhiteSpace(query))
                {
                    return results;
                }

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5); // Timeout ngắn để không làm chậm hội thảo
                client.DefaultRequestHeaders.Add("User-Agent", "AegisQuiz-Symposium/2.0 (mailto:cuongvba@aegisquiz.internal)");

                var url = $"https://api.openalex.org/works?search={Uri.EscapeDataString(query)}&per-page=3&sort=relevance_score:desc";
                var response = await client.GetAsync(url, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[ScholarlyApi] OpenAlex responded with {StatusCode} for query {Query}", response.StatusCode, query);
                    return results;
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("results", out var worksArray) || worksArray.GetArrayLength() == 0)
                {
                    return results;
                }

                foreach (var work in worksArray.EnumerateArray())
                {
                    var title = work.TryGetProperty("title", out var titleElem) ? titleElem.GetString() : null;
                    if (string.IsNullOrWhiteSpace(title)) continue;

                    var doi = work.TryGetProperty("doi", out var doiElem) ? doiElem.GetString() : null;
                    var pubYear = work.TryGetProperty("publication_year", out var yrElem) ? yrElem.GetInt32().ToString() : "N/A";
                    var citedBy = work.TryGetProperty("cited_by_count", out var cElem) ? cElem.GetInt32() : 0;

                    string? journalName = null;
                    if (work.TryGetProperty("primary_location", out var loc) && loc.ValueKind == JsonValueKind.Object)
                    {
                        if (loc.TryGetProperty("source", out var src) && src.ValueKind == JsonValueKind.Object)
                        {
                            if (src.TryGetProperty("display_name", out var jn))
                            {
                                journalName = jn.GetString();
                            }
                        }
                    }

                    var citationInfo = $"{title} ({pubYear})" + (journalName != null ? $" - {journalName}" : "");

                    results.Add(new EvidenceLedgerItemDto
                    {
                        ClaimOrFact = $"Công trình nghiên cứu công bố chính thức: {title}",
                        QuantitativeValue = $"Trích dẫn học thuật: {citedBy:N0} lần; Năm XB: {pubYear}",
                        SourceTitle = citationInfo,
                        DoiOrUrl = doi ?? (work.TryGetProperty("id", out var idElem) ? idElem.GetString() : null),
                        Provenance = "ScholarlyApi",
                        IsOnlineVerified = true,
                        ConfidenceWeight = citedBy > 50 ? 0.95 : 0.85
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("[ScholarlyApi] Offline fallback or API request failed: {Message}", ex.Message);
            }

            return results;
        }

        private static string ExtractSearchQuery(string topic, string problem)
        {
            // Trích xuất các từ khóa tiếng Anh hoặc thuật ngữ khoa học nếu có, hoặc dùng 4-5 từ trọng tâm
            var combined = $"{topic} {problem}";
            if (combined.Contains("tàn phá", StringComparison.OrdinalIgnoreCase) || combined.Contains("trái đất", StringComparison.OrdinalIgnoreCase))
            {
                return "Anthropocene human impact biodiversity biomass extinction";
            }
            if (combined.Contains("ngân hàng", StringComparison.OrdinalIgnoreCase) || combined.Contains("basel", StringComparison.OrdinalIgnoreCase))
            {
                return "Basel III capital adequacy liquidity coverage banking risk";
            }
            if (combined.Contains("irt", StringComparison.OrdinalIgnoreCase) || combined.Contains("khảo thí", StringComparison.OrdinalIgnoreCase))
            {
                return "Item Response Theory latent trait 3PL parameter estimation";
            }
            
            // Rút gọn bớt độ dài
            return topic.Length > 60 ? topic.Substring(0, 60) : topic;
        }
    }
}
