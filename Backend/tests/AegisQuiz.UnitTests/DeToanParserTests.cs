using System;
using System.IO;
using System.Linq;
using AegisQuiz.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace AegisQuiz.UnitTests
{
    public class DeToanParserTests
    {
        private readonly ITestOutputHelper _output;

        public DeToanParserTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ParseDeToan_ShouldExtractMathQuestionsAndImages()
        {
            var filePath = @"D:\Cuong\DuAn\mybank\AegisQuiz\DeToan.docx";
            if (!File.Exists(filePath))
            {
                return;
            }

            var service = new DocxParserService();
            using var fs = File.OpenRead(filePath);
            var results = service.ParseDocxFile(fs);

            for (int i = 0; i < results.Count; i++)
            {
                var q = results[i];
                _output.WriteLine($"Question {i+1}: Type={q.QuestionType}, Options={q.Options.Count}, OptionType={q.OptionType}, Content={q.Content.Substring(0, Math.Min(60, q.Content.Length))}...");
            }

            Assert.NotNull(results);
            Assert.True(results.Count >= 20, $"Kỳ vọng ít nhất 20 câu hỏi từ DeToan.docx nhưng chỉ tìm thấy {results.Count} câu.");

            // Kiểm tra Câu 1: Có đồ thị hàm số và các phương án công thức toán
            var q1 = results[0];
            Assert.Contains("Đường cong", q1.Content);
            Assert.True(q1.ContentType == "image" || q1.Content.Contains("data:image/"), "Câu 1 phải chứa hình vẽ đồ thị hàm số.");
            Assert.True(q1.Options.Count >= 4, $"Câu 1 phải có 4 phương án, thực tế: {q1.Options.Count}");
            Assert.True(q1.OptionType == "image" || q1.Options.Any(o => o.Contains("data:image/")), "Câu 1 các phương án phải là công thức MathType chuyển thành ảnh PNG Base64.");

            // Kiểm tra có câu hỏi Đúng/Sai (TRUE_FALSE)
            var hasTrueFalse = results.Any(q => q.QuestionType == "TRUE_FALSE");
            Assert.True(hasTrueFalse, "Phải nhận diện được phần II: Trắc nghiệm Đúng / Sai.");

            // Kiểm tra có câu hỏi Trả lời ngắn (SHORT_ANSWER)
            var hasShortAnswer = results.Any(q => q.QuestionType == "SHORT_ANSWER");
            Assert.True(hasShortAnswer, "Phải nhận diện được phần III: Trắc nghiệm trả lời ngắn.");

            // Output tóm tắt kết quả
            Console.WriteLine($"[TEST THÀNH CÔNG] Bóc tách tổng cộng: {results.Count} câu hỏi từ DeToan.docx");
            Console.WriteLine($" - Số câu có chứa ảnh minh họa: {results.Count(q => q.ContentType == "image")}");
            Console.WriteLine($" - Số câu có phương án công thức MathType: {results.Count(q => q.OptionType == "image")}");
        }

        [Fact]
        public void ParseDeToanGiaiChiTiet_ShouldExtractQuestionsWithAnswersAndExplanations()
        {
            var filePath = @"D:\Cuong\DuAn\mybank\AegisQuiz\DeToanGiaiChiTiet.docx";
            if (!File.Exists(filePath))
            {
                return;
            }

            var service = new DocxParserService();
            // Dùng FileShare.ReadWrite để không bị chặn khi file đang mở trong Word
            using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var results = service.ParseDocxFile(fs);

            Assert.NotNull(results);
            Assert.True(results.Count >= 20, $"Kỳ vọng ít nhất 20 câu hỏi nhưng thực tế chỉ bóc tách được {results.Count} câu.");

            int countWithAnswer = results.Count(q => !string.IsNullOrWhiteSpace(q.SuggestedAnswer) && !q.SuggestedAnswer.Contains("Điền đáp số"));
            int countWithExplanation = results.Count(q => !string.IsNullOrWhiteSpace(q.AiExplanation));
            int countWithImages = results.Count(q => q.ContentType == "image" || q.OptionType == "image");

            _output.WriteLine($"[DeToanGiaiChiTiet] Tổng số câu hỏi bóc tách: {results.Count}");
            _output.WriteLine($" - Số câu có ĐÁP ÁN ĐÚNG tự động: {countWithAnswer} / {results.Count} ({countWithAnswer * 100.0 / results.Count:F1}%)");
            _output.WriteLine($" - Số câu có LỜI GIẢI CHI TIẾT: {countWithExplanation} / {results.Count} ({countWithExplanation * 100.0 / results.Count:F1}%)");
            _output.WriteLine($" - Số câu có đồ thị / công thức MathType: {countWithImages} / {results.Count}");

            // In mẫu 5 câu hỏi đầu tiên
            for (int i = 0; i < Math.Min(5, results.Count); i++)
            {
                var q = results[i];
                _output.WriteLine($"--- CÂU {i+1} [{q.TopicCode}] [{q.QuestionType}] ---");
                _output.WriteLine($"  Nội dung: {q.Content.Substring(0, Math.Min(80, q.Content.Length))}...");
                _output.WriteLine($"  Đáp án đúng: {q.SuggestedAnswer}");
                _output.WriteLine($"  Lời giải chi tiết: {(q.AiExplanation.Length > 80 ? q.AiExplanation.Substring(0, 80) + "..." : q.AiExplanation)}");
            }

            // Kỳ vọng tỷ lệ có đáp án và lời giải chi tiết cao vượt trội
            Assert.True(countWithAnswer > 10, "Phải tự động trích xuất được đáp án đúng từ bảng đáp án hoặc lời giải.");
            Assert.True(countWithExplanation > 10, "Phải tự động bóc tách được lời giải chi tiết cho các câu hỏi.");
        }

        [Fact]
        public void ParseEnglishExamParagraphs_ShouldHandlePhoneticsUnderlineAndReadingPassages()
        {
            var paragraphs = new List<string>
            {
                "Môn: Tiếng Anh",
                "Mark the letter A, B, C, or D on your answer sheet to indicate the word whose underlined part differs from the other three in pronunciation in each of the following questions.",
                "Question 1: Choose the word with a different sound in the underlined part.",
                "A. br<u>ea</u>k   B. st<u>ea</u>k   C. cl<u>ea</u>n   D. gr<u>ea</u>t",
                "Lời giải: Đáp án C phát âm là /i:/, các đáp án còn lại phát âm là /ei/. Chọn C",
                "Question 2: Choose the word that differs from the other three in the position of primary stress.",
                "A. apply   B. student   C. teacher   D. doctor",
                "Lời giải: Chọn A",
                "Read the following passage and mark the letter A, B, C, or D on your answer sheet to indicate the correct answer to each of the questions.",
                "Artificial intelligence is transforming education worldwide. Students can now learn at their own pace with personalized recommendations.",
                "Question 3: According to the passage, how is AI transforming education?",
                "A. By replacing all human teachers.",
                "B. By providing personalized learning at each student's pace.",
                "C. By making school obsolete.",
                "D. By eliminating homework.",
                "Lời giải: Chọn B"
            };

            var results = DocxParserService.ParseParagraphs(paragraphs);

            _output.WriteLine($"Results count: {results.Count}");
            for (int i = 0; i < results.Count; i++)
            {
                _output.WriteLine($"[{i}] Content: {results[i].Content.Substring(0, Math.Min(60, results[i].Content.Length))}... Type: {results[i].QuestionType}, Options: {results[i].Options.Count}, Ans: '{results[i].SuggestedAnswer}', Expl: '{results[i].AiExplanation}'");
            }

            Assert.NotNull(results);
            Assert.Equal(3, results.Count);

            // Kiểm tra Câu 1: Giữ nguyên thẻ <u>...</u> phát âm
            var q1 = results[0];
            Assert.Equal("TIENG_ANH", q1.TopicCode);
            Assert.Equal("SINGLE", q1.QuestionType);
            Assert.Equal("3", q1.SuggestedAnswer); // Chọn C -> 3
            Assert.Contains("<u>ea</u>", q1.Options[0]); // A. br<u>ea</u>k
            Assert.Contains("<u>ea</u>", q1.Options[2]); // C. cl<u>ea</u>n

            // Kiểm tra Câu 2: Trọng âm (Stress)
            var q2 = results[1];
            Assert.Equal("1", q2.SuggestedAnswer); // Chọn A -> 1

            // Kiểm tra Câu 3: Đoạn văn đọc hiểu được gắn ngữ cảnh vào câu hỏi (qua Content hoặc ContextContent)
            var q3 = results[2];
            Assert.True(
                q3.Content.Contains("Artificial intelligence") || 
                (!string.IsNullOrEmpty(q3.ContextContent) && q3.ContextContent.Contains("Artificial intelligence")),
                "Đoạn văn đọc hiểu phải được gắn vào Content hoặc ContextContent");
            Assert.Equal("2", q3.SuggestedAnswer); // Chọn B -> 2
        }

        [Fact]
        public void ParseDeTiengAnhChiTiet_ShouldExtract40QuestionsWithAnswersAndExplanations()
        {
            string filePath = @"D:\Cuong\DuAn\mybank\AegisQuiz\DeTiengAnhChiTiet.docx";
            if (!File.Exists(filePath))
            {
                _output.WriteLine("Tệp DeTiengAnhChiTiet.docx không tồn tại.");
                return;
            }

            var service = new DocxParserService();
            using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var results = service.ParseDocxFile(fs);

            Assert.NotNull(results);
            _output.WriteLine($"[DeTiengAnhChiTiet] Tổng số câu hỏi bóc tách: {results.Count}");

            int countWithAnswer = results.Count(q => !string.IsNullOrWhiteSpace(q.SuggestedAnswer) && !q.SuggestedAnswer.Contains("Điền đáp số"));
            int countWithExplanation = results.Count(q => !string.IsNullOrWhiteSpace(q.AiExplanation));

            _output.WriteLine($" - Số câu có ĐÁP ÁN: {countWithAnswer} / {results.Count} ({countWithAnswer * 100.0 / Math.Max(1, results.Count):F1}%)");
            _output.WriteLine($" - Số câu có GIẢI THÍCH CHI TIẾT: {countWithExplanation} / {results.Count} ({countWithExplanation * 100.0 / Math.Max(1, results.Count):F1}%)");

            for (int i = 0; i < results.Count; i++)
            {
                var q = results[i];
                _output.WriteLine($"Câu {i+1} [Type: {q.QuestionType}] [Topic: {q.TopicCode}] [Opts: {q.Options.Count}] [Ans: {q.SuggestedAnswer}] [Expl len: {q.AiExplanation?.Length ?? 0}]");
                _output.WriteLine($"   Content: {q.Content.Substring(0, Math.Min(100, q.Content.Length))}...");
            }

            Assert.True(results.Count >= 35, $"Kỳ vọng ít nhất 35/40 câu hỏi nhưng thực tế chỉ được {results.Count} câu.");
        }

        [Fact]
        public void ParseGPLXQuestions_ShouldIdentifyCriticalQuestionsAndSubCategories()
        {
            var paragraphs = new List<string>
            {
                "BỘ ĐỀ SÁT HẠCH LÁI XE QUỐC GIA 600 CÂU HỎI",
                "Câu 1: [DIEM_LIET] Hành vi điều khiển xe cơ giới chạy quá tốc độ quy định, giành đường, vượt ẩu có bị nghiêm cấm hay không?",
                "A. Bị nghiêm cấm.",
                "B. Bị nghiêm cấm tùy từng trường hợp.",
                "C. Không bị nghiêm cấm.",
                "Lời giải: Chọn A. Hành vi vi phạm an toàn giao thông nghiêm trọng bị nghiêm cấm.",
                "Câu 2: Theo hướng mũi tên, thứ tự các xe đi như thế nào là đúng quy tắc giao thông trong sa hình?",
                "A. Xe công an, xe quân sự.",
                "B. Xe quân sự, xe công an.",
                "Lời giải: Chọn B. Áp dụng khẩu quyết Nhì ưu.",
                "Câu 3: Biển nào dưới đây cấm các phương tiện rẽ trái?",
                "A. Biển 1.",
                "B. Biển 2.",
                "Lời giải: Chọn A."
            };

            var results = DocxParserService.ParseParagraphs(paragraphs);

            Assert.NotNull(results);
            Assert.Equal(3, results.Count);

            // Câu 1: Điểm liệt
            var q1 = results[0];
            Assert.True(q1.IsCritical, "Câu 1 phải được đánh dấu là câu điểm liệt (IsCritical = true).");
            Assert.Equal(5, q1.Difficulty);
            Assert.Equal("1", q1.SuggestedAnswer);

            // Câu 2: Sa hình
            var q2 = results[1];
            Assert.False(q2.IsCritical);
            Assert.Equal("SA_HINH", q2.SubCategory);
            Assert.Equal("2", q2.SuggestedAnswer);

            // Câu 3: Biển báo
            var q3 = results[2];
            Assert.False(q3.IsCritical);
            Assert.Equal("BIEN_BAO", q3.SubCategory);
            Assert.Equal("1", q3.SuggestedAnswer);
        }

        [Fact]
        public async Task ArenaPlugins_ShouldCorrectlyHandleGameshowRules()
        {
            var plugins = new AegisQuiz.Application.Interfaces.IArenaGamePlugin[]
            {
                new AegisQuiz.Infrastructure.Arena.OlympiaPlugin(),
                new AegisQuiz.Infrastructure.Arena.GoldenBellPlugin(),
                new AegisQuiz.Infrastructure.Arena.LuckyWheelPlugin(),
                new AegisQuiz.Infrastructure.Arena.LightningQuizPlugin()
            };

            var manager = new AegisQuiz.Infrastructure.Arena.ArenaRoomManager(plugins);

            // 1. Kiểm tra Olympia Supreme: Bấm chuông & Ngôi sao hy vọng
            var olympiaRoom = manager.CreateRoom("OLYMPIA", "Olympia Chung Kết", "user-1", "Nguyễn Văn A");
            Assert.NotNull(olympiaRoom);
            Assert.Equal("OLYMPIA", olympiaRoom.GameCode);

            // Thí sinh 1 bấm chuông thắng
            bool buzzed = await manager.PressBuzzerAsync(olympiaRoom.RoomId, "user-1", 1000);
            Assert.True(buzzed);
            Assert.Equal("user-1", olympiaRoom.BuzzerWinnerPlayerId);

            // Kích hoạt Ngôi sao hy vọng
            await manager.ExecuteActionAsync(olympiaRoom.RoomId, "user-1", "USE_STAR_OF_HOPE", "");
            // Trả lời đúng -> x2 điểm
            await manager.SubmitAnswerAsync(olympiaRoom.RoomId, "user-1", olympiaRoom.Questions[0].AnswerRaw);
            Assert.Equal(20, olympiaRoom.Players["user-1"].Score); // 10 x 2 = 20đ

            // 2. Kiểm tra Rung Chuông Vàng: Loại thí sinh & Thầy cô cứu trợ
            var bellRoom = manager.CreateRoom("GOLDEN_BELL", "Rung Chuông Vàng Trường THPT", "host", "Thầy Hiệu Trưởng");
            manager.JoinRoom(bellRoom.RoomId, new AegisQuiz.Application.Interfaces.ArenaPlayer { Id = "stu-1", Name = "Học sinh 1" });
            manager.JoinRoom(bellRoom.RoomId, new AegisQuiz.Application.Interfaces.ArenaPlayer { Id = "stu-2", Name = "Học sinh 2" });

            // stu-1 trả lời sai -> Bị loại
            await manager.SubmitAnswerAsync(bellRoom.RoomId, "stu-1", "DAP_AN_SAI");
            Assert.Equal("ELIMINATED", bellRoom.Players["stu-1"].Status);

            // Thầy cô cứu trợ -> Hồi sinh quay lại sàn đấu
            await manager.ExecuteActionAsync(bellRoom.RoomId, "host", "TEACHER_RESCUE", "");
            Assert.Equal("ACTIVE", bellRoom.Players["stu-1"].Status);

            // 3. Kiểm tra Nhanh Như Chớp: Leo dốc và tụt dốc về 0
            var lightningRoom = manager.CreateRoom("LIGHTNING", "Nhanh Như Chớp Đỉnh Cao", "player-x", "Lê Văn B");
            // Trả lời đúng 2 câu -> Nấc 2
            await manager.SubmitAnswerAsync(lightningRoom.RoomId, "player-x", lightningRoom.Questions[0].AnswerRaw);
            Assert.Equal(1, lightningRoom.Players["player-x"].StepPosition);
            await manager.SubmitAnswerAsync(lightningRoom.RoomId, "player-x", lightningRoom.Questions[1].AnswerRaw);
            Assert.Equal(2, lightningRoom.Players["player-x"].StepPosition);

            // Trả lời sai câu 3 -> TỤT VỀ VẠCH SỐ 0
            await manager.SubmitAnswerAsync(lightningRoom.RoomId, "player-x", "SAI_HOAN_TOAN");
            Assert.Equal(0, lightningRoom.Players["player-x"].StepPosition);
        }

        [Fact]
        public void Parse600CauGPLX_Pdf_ShouldExtractAllQuestionsAndCriticalFlags()
        {
            var filePath = @"D:\Cuong\DuAn\mybank\AegisQuiz\600caugplx.pdf";
            if (!File.Exists(filePath))
            {
                _output.WriteLine("Tệp 600caugplx.pdf không tồn tại.");
                return;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var pdfParser = new PdfParserService();
            using var fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var results = pdfParser.ParsePdfQuestions(fs);
            sw.Stop();

            _output.WriteLine($"[600caugplx.pdf] Thời gian xử lý: {sw.ElapsedMilliseconds} ms ({sw.Elapsed.TotalSeconds:F2}s)");
            _output.WriteLine($"[600caugplx.pdf] Tổng số câu hỏi bóc tách: {results.Count}");

            int criticalCount = results.Count(q => q.IsCritical);
            int countWithAnswer = results.Count(q => !string.IsNullOrWhiteSpace(q.SuggestedAnswer));
            int saHinhCount = results.Count(q => q.SubCategory == "SA_HINH" || q.Content.Contains("sa hình") || q.Content.Contains("thứ tự các xe"));
            int bienBaoCount = results.Count(q => q.SubCategory == "BIEN_BAO" || q.Content.Contains("Biển nào") || q.Content.Contains("biển báo"));

            _output.WriteLine($" - Số CÂU ĐIỂM LIỆT phát hiện tự động: {criticalCount}");
            _output.WriteLine($" - Số câu có ĐÁP ÁN trích xuất: {countWithAnswer} / {results.Count}");
            _output.WriteLine($" - Số câu Sa hình: {saHinhCount}");
            _output.WriteLine($" - Số câu Biển báo: {bienBaoCount}");

            // In mẫu 5 câu hỏi đầu tiên
            for (int i = 0; i < Math.Min(5, results.Count); i++)
            {
                var q = results[i];
                _output.WriteLine($"--- CÂU {i+1} [Điểm liệt: {q.IsCritical}] [Đáp án: {q.SuggestedAnswer}] ---");
                _output.WriteLine($"  Nội dung: {q.Content.Substring(0, Math.Min(80, q.Content.Length))}...");
                _output.WriteLine($"  Số phương án: {q.Options.Count}");
            }

            Assert.NotNull(results);
            Assert.True(results.Count >= 500, $"Kỳ vọng ít nhất 500 câu hỏi từ 600caugplx.pdf nhưng thực tế bóc tách được {results.Count} câu.");
            Assert.True(criticalCount >= 10, $"Kỳ vọng phát hiện được ít nhất 10 câu điểm liệt từ khóa nghiêm cấm nhưng chỉ tìm thấy {criticalCount} câu.");
        }

        [Fact]
        public void InspectGPLXf2023_Pdf_ImagesAndContent()
        {
            var filePath = @"D:\Cuong\DuAn\mybank\AegisQuiz\GPLXf2023.pdf";
            if (!File.Exists(filePath))
            {
                _output.WriteLine("Tệp GPLXf2023.pdf không tồn tại.");
                return;
            }

            using var doc = UglyToad.PdfPig.PdfDocument.Open(filePath);
            _output.WriteLine($"[GPLXf2023.pdf] Số trang: {doc.NumberOfPages}");

            int totalImages = 0;
            int pagesWithImages = 0;
            int pagesWithText = 0;
            int totalTextLength = 0;
            for (int p = 1; p <= doc.NumberOfPages; p++)
            {
                var page = doc.GetPage(p);
                var images = page.GetImages().ToList();
                var text = page.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    pagesWithText++;
                    totalTextLength += text.Length;
                }
                if (images.Count > 0)
                {
                    pagesWithImages++;
                    totalImages += images.Count;
                }
            }
            _output.WriteLine($"[GPLXf2023.pdf] Số trang có Text: {pagesWithText}, Tổng ký tự text: {totalTextLength}");
            _output.WriteLine($"[GPLXf2023.pdf] Số trang có Ảnh: {pagesWithImages}, Tổng số ảnh: {totalImages}");

            // Trích xuất thử ảnh trang 1 của GPLXf2023 và kiểm tra Magic Bytes
            var p1 = doc.GetPage(1);
            var p1Imgs = p1.GetImages().ToList();
            _output.WriteLine($"[GPLXf2023.pdf - Trang 1]: {p1Imgs.Count} ảnh.");
            foreach (var img in p1Imgs)
            {
                var raw = img.RawBytes.ToArray();
                string magic = raw.Length >= 4 ? $"{raw[0]:X2} {raw[1]:X2} {raw[2]:X2} {raw[3]:X2}" : "";
                string mime = "image/png";
                if (raw.Length >= 2 && raw[0] == 0xFF && raw[1] == 0xD8) mime = "image/jpeg";
                else if (raw.Length >= 4 && raw[0] == 0x89 && raw[1] == 0x50) mime = "image/png";

                _output.WriteLine($"  Ảnh: Size={raw.Length} bytes, Magic={magic}, Mime={mime}, BoundingBox: W={img.BoundingBox.Width:F1}, H={img.BoundingBox.Height:F1}");
            }

            // Kiểm tra trang 59, 70 (phần biển báo) và trang 95, 105, 110, 111 (phần sa hình)
            foreach (int pNum in new[] { 59, 70, 95, 105, 110, 111 })
            {
                if (pNum <= doc.NumberOfPages)
                {
                    var p = doc.GetPage(pNum);
                    var imgs = p.GetImages().ToList();
                    _output.WriteLine($"[GPLXf2023.pdf - Trang {pNum}]: {imgs.Count} ảnh.");
                    foreach (var img in imgs)
                    {
                        bool canPng = img.TryGetPng(out byte[] png);
                        _output.WriteLine($"   Ảnh: Size={img.RawBytes.Length}, CanPng={canPng}, PngLen={png?.Length ?? 0}, BBox=W:{img.BoundingBox.Width:F0}xH:{img.BoundingBox.Height:F0}");
                    }
                }
            }

            // Kiểm tra các trang cuối của 600caugplx.pdf (nơi có sa hình câu 580-600)
            var file600 = @"D:\Cuong\DuAn\mybank\AegisQuiz\600caugplx.pdf";
            if (File.Exists(file600))
            {
                using var doc600 = UglyToad.PdfPig.PdfDocument.Open(file600);
                var scratchDir = @"C:\Users\cuongnguyenviet8.CORP\.gemini\antigravity-ide\brain\68f0672b-f717-4837-ae1d-eaad30a9c9f5\scratch";
                Directory.CreateDirectory(scratchDir);
                var fileGplx2023 = @"D:\Cuong\DuAn\mybank\AegisQuiz\GPLXf2023.pdf";
                if (File.Exists(fileGplx2023))
                {
                    using var doc2023 = UglyToad.PdfPig.PdfDocument.Open(fileGplx2023);
                    var p110 = doc2023.GetPage(110);
                    var imgs110 = p110.GetImages().ToList();
                    for (int i = 0; i < imgs110.Count; i++)
                    {
                        var raw = imgs110[i].RawBytes.ToArray();
                        string ext = (raw.Length >= 2 && raw[0] == 0xFF && raw[1] == 0xD8) ? "jpg" : "png";
                        string outPath = Path.Combine(scratchDir, $"gplx2023_p110_img_{i}.{ext}");
                        File.WriteAllBytes(outPath, raw);
                        _output.WriteLine($"Xuất trang 110 GPLX2023: {outPath} ({raw.Length} bytes)");
                    }
                }
                var p186 = doc600.GetPage(186);
                _output.WriteLine("=== TRANG 186 TEXT ===");
                _output.WriteLine(p186.Text);
                _output.WriteLine("=== TRANG 186 IMAGES ===");
                foreach (var img in p186.GetImages())
                {
                    _output.WriteLine($"Image: Bottom={img.BoundingBox.Bottom:F1}, Top={img.BoundingBox.Top:F1}, W={img.BoundingBox.Width:F1}, H={img.BoundingBox.Height:F1}");
                }
                var gplxQuestions = new List<AegisQuiz.Application.Interfaces.DocxImportPreviewDto>();
                for (int p = 1; p <= doc600.NumberOfPages; p++)
                {
                    var page = doc600.GetPage(p);
                    string raw = page.Text;
                    var words = page.GetWords().ToList();
                    var pageImages = page.GetImages().ToList();

                    // Tìm vị trí Y của từng "Câu {n}"
                    var questionYPositions = new List<(int qNum, double topY, double bottomY)>();
                    for (int w = 0; w < words.Count; w++)
                    {
                        var word = words[w];
                        var m = System.Text.RegularExpressions.Regex.Match(word.Text, @"^Câu\s*(\d+)[\.\:]?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (!m.Success && word.Text.Equals("Câu", StringComparison.OrdinalIgnoreCase) && w + 1 < words.Count)
                        {
                            var nextWord = words[w + 1];
                            var m2 = System.Text.RegularExpressions.Regex.Match(nextWord.Text, @"^(\d+)[\.\:]?");
                            if (m2.Success)
                            {
                                int qNum = int.Parse(m2.Groups[1].Value);
                                questionYPositions.Add((qNum, word.BoundingBox.Top, word.BoundingBox.Bottom));
                                w++;
                                continue;
                            }
                        }
                        else if (m.Success)
                        {
                            int qNum = int.Parse(m.Groups[1].Value);
                            questionYPositions.Add((qNum, word.BoundingBox.Top, word.BoundingBox.Bottom));
                        }
                    }

                    // Sắp xếp các câu hỏi theo Y giảm dần (từ trên xuống dưới)
                    questionYPositions = questionYPositions.OrderByDescending(q => q.topY).ToList();

                    var qMatches = System.Text.RegularExpressions.Regex.Matches(raw, @"(Câu\s*(\d+)[\.\:])");
                    if (qMatches.Count == 0) continue;

                    for (int i = 0; i < qMatches.Count; i++)
                    {
                        int qNum = int.Parse(qMatches[i].Groups[2].Value);
                        int start = qMatches[i].Index;
                        int end = (i + 1 < qMatches.Count) ? qMatches[i + 1].Index : raw.Length;
                        string block = raw.Substring(start, end - start).Trim();

                        var optMatch = System.Text.RegularExpressions.Regex.Match(block, @"(?:\s+|^)1\.\s+");
                        string stem = optMatch.Success ? block.Substring(0, optMatch.Index).Trim() : block;
                        string optSection = optMatch.Success ? block.Substring(optMatch.Index).Trim() : "";

                        var options = new List<string>();
                        if (!string.IsNullOrEmpty(optSection))
                        {
                            var optIndices = new List<(int num, int index, int matchStart)>();
                            for (int n = 1; n <= 4; n++)
                            {
                                int searchFrom = optIndices.Count > 0 ? optIndices.Last().index : 0;
                                if (searchFrom >= optSection.Length) break;
                                var om = System.Text.RegularExpressions.Regex.Match(optSection.Substring(searchFrom), $@"(?<!(?:Biển|Hình|xe|Xe|tải|mô tô)\s*)(?:^|\s+){n}\.\s+");
                                if (om.Success)
                                {
                                    optIndices.Add((n, searchFrom + om.Index + om.Length, searchFrom + om.Index));
                                }
                            }
                            for (int k = 0; k < optIndices.Count; k++)
                            {
                                int startPos = optIndices[k].index;
                                int endPos = (k + 1 < optIndices.Count) ? optIndices[k + 1].matchStart : optSection.Length;
                                string optText = optSection.Substring(startPos, Math.Max(0, endPos - startPos)).Trim();
                                optText = System.Text.RegularExpressions.Regex.Replace(optText, @"[\.\s]+$", "").Trim();
                                if (!string.IsNullOrEmpty(optText))
                                {
                                    options.Add(optText);
                                }
                            }
                        }

                        // Tìm tọa độ Y của câu hỏi hiện tại và câu hỏi tiếp theo
                        var currentQPos = questionYPositions.FirstOrDefault(qp => qp.qNum == qNum);
                        double upperY = currentQPos.topY > 0 ? currentQPos.topY : page.Height;
                        
                        // Câu tiếp theo có Y nhỏ hơn
                        var nextQPos = questionYPositions.Where(qp => qp.topY < upperY - 5).OrderByDescending(qp => qp.topY).FirstOrDefault();
                        double lowerY = nextQPos.topY > 0 ? nextQPos.topY : 0;

                        // Các ảnh thuộc về câu hỏi này phải có tâm Y (center Y) nằm giữa [lowerY, upperY]
                        // Loại bỏ các ảnh quá nhỏ (như icon < 20px) nếu có
                        var matchedImages = pageImages
                            .Where(img => {
                                double imgCenterY = (img.BoundingBox.Top + img.BoundingBox.Bottom) / 2.0;
                                return imgCenterY <= upperY + 10 && imgCenterY >= lowerY - 5 &&
                                       img.Bounds.Width > 20 && img.Bounds.Height > 20;
                            })
                            .OrderByDescending(img => img.BoundingBox.Bottom)
                            .ToList();

                        string subCategory = (qNum >= 487) ? "SA_HINH" : ((qNum >= 305) ? "BIEN_BAO" : "LUAT_GIAO_THONG");
                        gplxQuestions.Add(new AegisQuiz.Application.Interfaces.DocxImportPreviewDto
                        {
                            TempId = qNum.ToString(),
                            Content = stem,
                            Options = options,
                            SubCategory = subCategory,
                            Difficulty = (subCategory == "SA_HINH") ? 4 : (subCategory == "BIEN_BAO" ? 3 : 2),
                            ContentType = matchedImages.Count > 0 ? "image" : "text",
                            AiExplanation = $"Trang {p} | Số ảnh khớp: {matchedImages.Count} | Y=[{lowerY:F1}..{upperY:F1}]"
                        });
                    }
                }

                _output.WriteLine($"TỔNG CỘNG PARSE ĐƯỢC: {gplxQuestions.Count} câu hỏi!");
                var saHinh = gplxQuestions.Where(q => q.SubCategory == "SA_HINH").ToList();
                var bienBao = gplxQuestions.Where(q => q.SubCategory == "BIEN_BAO").ToList();
                _output.WriteLine($"-> Sa hình: {saHinh.Count} câu ({saHinh.Count(q => q.ContentType == "image")} câu có ảnh)");
                _output.WriteLine($"-> Biển báo: {bienBao.Count} câu ({bienBao.Count(q => q.ContentType == "image")} câu có ảnh)");

                // In thử Câu 598, 599, 600
                var q598 = gplxQuestions.FirstOrDefault(q => q.Content.Contains("598"));
                if (q598 != null)
                {
                    _output.WriteLine($"[CÂU 598]: {q598.Content}");
                    _output.WriteLine($"  Có ảnh: {q598.ContentType}, Phương án: {string.Join(" | ", q598.Options)}");
                }
                var q600 = gplxQuestions.FirstOrDefault(q => q.Content.Contains("600"));
                if (q600 != null)
                {
                    _output.WriteLine($"[CÂU 600]: {q600.Content}");
                    _output.WriteLine($"  Có ảnh: {q600.ContentType}, Phương án: {string.Join(" | ", q600.Options)}");
                }
            }
        }
    }
}

