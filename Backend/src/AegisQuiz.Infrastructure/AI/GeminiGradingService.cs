using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.AI
{
    // [FIX CRITICAL] GeminiGradingService — dùng IHttpClientFactory
    // TRƯỚC: new HttpClient() mỗi request → Socket Exhaustion + Memory Leak production!
    // SAU:   IHttpClientFactory quản lý pool HTTP connection, an toàn và hiệu quả.
    public class GeminiGradingService : IGeminiGradingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string             _apiKey;

        public GeminiGradingService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey            = config["Gemini:ApiKey"] ?? string.Empty;
        }

        public async Task<GradingResult> GradeEssayAsync(
            string questionContent,
            string rubric,
            string studentAnswer)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new GradingResult
                {
                    Score       = 5.0,
                    Explanation = "Chưa cấu hình Gemini API Key. Vui lòng cấu hình biến môi trường Gemini:ApiKey."
                };
            }

            var prompt = $@"Bạn là giám khảo chuyên nghiệp trong lĩnh vực tài chính ngân hàng.
Đề bài: {questionContent}
Barem điểm (Rubric): {rubric}
Bài làm của học viên: {studentAnswer}

Hãy chấm điểm từ 0.0 đến 10.0 theo rubric đã cho và đưa ra nhận xét chi tiết, xây dựng.
Trả về ĐÚNG định dạng JSON sau (không có markdown, không có ký tự thừa):
{{""Score"": 8.5, ""Explanation"": ""Nhận xét chi tiết của giám khảo""}}";

            try
            {
                var responseText = await CallGeminiAsync(prompt);

                // Tách JSON từ phản hồi (phòng LLM bọc markdown ```json)
                var start = responseText.IndexOf('{');
                var end   = responseText.LastIndexOf('}');
                if (start >= 0 && end >= 0)
                    responseText = responseText.Substring(start, end - start + 1);

                var result = JsonSerializer.Deserialize<GradingResult>(responseText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result ?? new GradingResult
                {
                    Score       = 0,
                    Explanation = "Không thể phân tích kết quả chấm điểm từ AI."
                };
            }
            catch (Exception ex)
            {
                return new GradingResult
                {
                    Score       = 0,
                    Explanation = $"Lỗi kết nối Gemini API: {ex.Message}. Vui lòng thử lại sau."
                };
            }
        }

        // [FIX] Dùng CreateClient("gemini") từ IHttpClientFactory
        // → Connection pooling, tự động retry, không tạo socket mới mỗi request
        private async Task<string> CallGeminiAsync(string prompt)
        {
            var client = _httpClientFactory.CreateClient("gemini");
            var url    = $"v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };

            var json    = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc    = JsonDocument.Parse(responseJson);

            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;
        }
    }
}
