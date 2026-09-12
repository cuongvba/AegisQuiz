using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.AI
{
    // [Cảnh giới 7: AI-Native] Gia sư ảo Gemini kết nối API thật của Google
    public class GeminiMentorService : IGeminiMentorService
    {
        private readonly string _apiKey;

        public GeminiMentorService(IConfiguration config)
        {
            _apiKey = config["Gemini:ApiKey"] ?? string.Empty;
        }

        public async Task<string> GeneratePersonalizedStudyPlan(Guid userId, List<QuestionAttempt> last7DaysHistory, bool isPremium)
        {
            int totalAttempted = last7DaysHistory.Count;
            int totalCorrect = last7DaysHistory.Count(q => q.IsCorrect);
            
            // Logic Freemium (Dùng thử có giới hạn chức năng)
            if (!isPremium)
            {
                double accuracy = totalAttempted > 0 ? (double)totalCorrect * 100 / totalAttempted : 0;
                return $@"
[BẢN DÙNG THỬ - AI MENTOR]
Trong 7 ngày qua, em đã thực hiện tổng cộng {totalAttempted} câu hỏi luyện tập.
Tỉ lệ trả lời chính xác của em đạt: {accuracy:F1}%.

Hệ thống ghi nhận hoạt động ôn luyện của em, tuy nhiên:
⚠️ ĐỂ XEM CHI TIẾT PHÂN TÍCH TÂM LÝ LÀM BÀI, LỖ HỔNG KIẾN THỨC VÀ NHẬN LỘ TRÌNH 3 BƯỚC KHẮC PHỤC TỪ GIA SƯ AI GEMINI, VUI LÒNG NÂNG CẤP VIP BẰNG METAMASK POLYGON.
";
            }

            if (string.IsNullOrEmpty(_apiKey))
            {
                return @"
[GIA SƯ AI - BẢN ĐẦY ĐỦ]
⚠️ Hệ thống chưa được cấu hình API Key của Google Gemini.
Vui lòng liên hệ với quản trị viên để hoàn tất cấu hình. Dưới đây là lộ trình ôn tập cơ bản của bạn:
1. Đọc lại các tài liệu ôn tập và làm lại các câu hỏi bị sai.
2. Thực hiện làm thêm ít nhất 5 đề thi ở mức độ Dễ.
";
            }

            // Chuẩn bị dữ liệu lịch sử chi tiết cho prompt
            var historyLines = string.Join("\n", last7DaysHistory.Take(15).Select(h => 
                $"- Môn học: {h.Category}, Độ khó: {h.Difficulty}, Lựa chọn: {h.SelectedAnswer}, Đúng/Sai: {(h.IsCorrect ? "Đúng" : "Sai")}, Thời gian: {h.TimeSpentSeconds} giây"));

            var prompt = $@"
                Ngươi là một chuyên gia giáo dục thông thái và gia sư AI tận tâm của AegisQuiz.
                Dưới đây là lịch sử làm bài thi của học sinh trong 7 ngày qua:
                - Tổng số câu đã làm: {totalAttempted}
                - Đúng: {totalCorrect} câu, Sai: {totalAttempted - totalCorrect} câu.
                - Chi tiết lịch sử làm bài (tối đa 15 câu):
                {historyLines}
                
                Nhiệm vụ của ngươi:
                1. Phân tích chi tiết hành vi học tập và các sai sót (ví dụ: làm nhanh bị sai hay do thiếu kiến thức nền, điểm yếu ở môn học nào).
                2. Tìm ra lỗ hổng kiến thức cốt lõi.
                3. Đề xuất Lộ trình 3 bước cụ thể, hành động được ngay để học sinh khắc phục điểm yếu và tối ưu điểm số.
                
                Hãy phản hồi trực tiếp cho học sinh bằng ngôn ngữ tiếng Việt tự nhiên, ấm áp, định dạng Markdown đẹp, không chứa thông tin meta.
            ";

            try
            {
                return await CallGeminiApiAsync(_apiKey, prompt);
            }
            catch (Exception ex)
            {
                return $@"
[GIA SƯ AI - BẢN ĐẦY ĐỦ]
Gia sư AI tạm thời gặp sự cố kết nối: {ex.Message}.
Tuy nhiên, dựa trên kết quả làm bài của bạn ({totalCorrect}/{totalAttempted} câu đúng), hãy tập trung ôn luyện lại các câu hỏi đã trả lời sai và điều chỉnh thời gian làm bài hợp lý hơn.
";
            }
        }

        private async Task<string> CallGeminiApiAsync(string apiKey, string prompt)
        {
            using var client = new HttpClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
            
            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };
            
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
                
            return text ?? string.Empty;
        }
    }
}
