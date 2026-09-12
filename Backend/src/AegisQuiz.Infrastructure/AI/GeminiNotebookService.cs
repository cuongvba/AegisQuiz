using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Infrastructure.AI
{
    // [Pipeline Giai đoạn 2+3: Structure + Generate]
    // Gọi Gemini 1.5 Pro để biến Raw Text thành Giáo trình có cấu trúc VÀ sinh Quiz
    public class GeminiNotebookService
    {
        // ===== GIAI ĐOẠN 2: Cấu trúc hóa Giáo trình =====
        public async Task<List<NotebookChapter>> StructurizeContentAsync(string rawPdfText)
        {
            var prompt = $@"
                Ngươi là một chuyên gia giáo dục hàng đầu thế giới.
                Dưới đây là nội dung trích xuất từ một tài liệu PDF:
                
                ---BEGIN DOCUMENT---
                {rawPdfText}
                ---END DOCUMENT---
                
                Nhiệm vụ: Hãy phân tích tài liệu này và tạo ra cấu trúc giáo trình gồm các Chương.
                Mỗi Chương gồm: Tiêu đề (title), Tóm tắt (summary), và Các ý chính (keyPoints).
                
                Trả về kết quả dưới dạng JSON:
                [
                  {{
                    ""title"": ""Chương 1: ..., 
                    ""summary"": ""Tóm tắt nội dung..."",
                    ""keyPoints"": [""Ý chính 1"", ""Ý chính 2""],
                    ""rawContent"": ""Nội dung gốc của chương này...""
                  }}
                ]
            ";

            // Gọi Gemini 1.5 Pro (Context window 1 triệu token)
            await Task.Delay(5000); // Giả lập gọi AI

            // Giả lập kết quả trả về từ Gemini
            var chapters = new List<NotebookChapter>
            {
                new NotebookChapter
                {
                    OrderIndex = 1,
                    Title = "Chương 1: Tổng quan Hệ thống Ngân hàng",
                    Summary = "Giới thiệu cấu trúc tổ chức, các phòng ban chức năng và quy trình vận hành cơ bản.",
                    KeyPointsJson = JsonSerializer.Serialize(new[] { 
                        "Cơ cấu tổ chức ngân hàng", 
                        "Vai trò của từng phòng ban", 
                        "Quy trình giao dịch cơ bản" 
                    })
                },
                new NotebookChapter
                {
                    OrderIndex = 2,
                    Title = "Chương 2: Quy trình Cho vay và Tín dụng",
                    Summary = "Chi tiết quy trình thẩm định, phê duyệt và giải ngân khoản vay.",
                    KeyPointsJson = JsonSerializer.Serialize(new[] { 
                        "Điều kiện vay vốn", 
                        "Quy trình thẩm định tài sản", 
                        "Quản lý rủi ro tín dụng" 
                    })
                }
            };

            Console.WriteLine($"[GeminiNotebook] Đã cấu trúc hóa thành {chapters.Count} chương.");
            return chapters;
        }

        // ===== GIAI ĐOẠN 3: Tự động Sinh Quiz từ mỗi Chương =====
        public async Task<List<GeneratedQuizItem>> GenerateQuizFromChapterAsync(NotebookChapter chapter)
        {
            var prompt = $@"
                Ngươi là chuyên gia ra đề thi.
                Dựa trên nội dung sau:
                
                Tiêu đề: {chapter.Title}
                Tóm tắt: {chapter.Summary}
                Các ý chính: {chapter.KeyPointsJson}
                
                Hãy sinh ra:
                - 10 câu hỏi trắc nghiệm (4 đáp án, 1 đáp án đúng, type='Single')
                - 3 câu Đúng/Sai (type='True/False')
                - 2 câu Tự luận ngắn (type='Essay')
                
                Gán difficulty từ 1-5 và durationSeconds hợp lý.
                Trả về dạng JSON array.
            ";

            await Task.Delay(3000); // Giả lập gọi AI

            // Giả lập kết quả
            var quizItems = new List<GeneratedQuizItem>
            {
                new GeneratedQuizItem
                {
                    Content = "Cơ quan nào quản lý hoạt động ngân hàng tại Việt Nam?",
                    Options = new[] { "Ngân hàng Nhà nước", "Bộ Tài chính", "Bộ Công thương", "Chính phủ" },
                    CorrectAnswer = "Ngân hàng Nhà nước",
                    Type = "Single",
                    Difficulty = 1,
                    DurationSeconds = 30
                },
                new GeneratedQuizItem
                {
                    Content = "Ngân hàng thương mại được phép in tiền. Đúng hay sai?",
                    Options = new[] { "TRUE", "FALSE" },
                    CorrectAnswer = "FALSE",
                    Type = "True/False",
                    Difficulty = 1,
                    DurationSeconds = 15
                }
            };

            Console.WriteLine($"[GeminiNotebook] Đã sinh {quizItems.Count} câu hỏi từ chương '{chapter.Title}'.");
            return quizItems;
        }
    }

    // DTO cho câu hỏi do AI sinh ra (trước khi chuyển thành QuestionBase Entity)
    public class GeneratedQuizItem
    {
        public string Content { get; set; } = string.Empty;
        public string[] Options { get; set; } = Array.Empty<string>();
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Type { get; set; } = "Single";
        public int Difficulty { get; set; } = 1;
        public int DurationSeconds { get; set; } = 45;
    }
}
