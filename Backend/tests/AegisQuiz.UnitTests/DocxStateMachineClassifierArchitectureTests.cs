using System;
using System.Collections.Generic;
using System.Linq;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Services.Docx;
using Xunit;

namespace AegisQuiz.UnitTests
{
    public class DocxStateMachineClassifierArchitectureTests
    {
        private readonly Xunit.Abstractions.ITestOutputHelper _output;

        public DocxStateMachineClassifierArchitectureTests(Xunit.Abstractions.ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void IDocxStateMachineClassifier_Implementation_ShouldParseSuccessfully()
        {
            IDocxStateMachineClassifier classifier = new DocxStateMachineClassifierService();

            var paragraphs = new List<string>
            {
                "Câu 1. Thủ đô của Việt Nam là gì?",
                "A. Hà Nội",
                "B. Hải Phòng",
                "C. Đà Nẵng",
                "D. Cần Thơ",
                "Lời giải: Hà Nội là thủ đô."
            };

            var results = classifier.ParseParagraphList(paragraphs);

            foreach (var q in results)
            {
                _output.WriteLine($"[Test Q] Type={q.QuestionType}, Content={q.Content}, Opts={q.Options.Count}, Ans={q.SuggestedAnswer}");
            }

            Assert.NotNull(results);
            Assert.Single(results);
            Assert.Equal("SINGLE", results[0].QuestionType);
            Assert.Equal(4, results[0].Options.Count);
        }

        [Fact]
        public void ZeroHardcode_DynamicDomainContext_ShouldInferCustomTopicFromDatabase()
        {
            // Giả lập một Tenant đặc thù có chủ đề tùy biến không hề có trong code C#
            var customContext = new DynamicDomainContext
            {
                Topics = new List<BankTopicSummaryDto>
                {
                    new()
                    {
                        Code = "TRIET_HOC_MAC_LENIN",
                        Name = "Triết học Mác - Lênin",
                        Keywords = new() { "vật chất", "ý thức", "biện chứng duy vật", "hình thái kinh tế xã hội" }
                    }
                }
            };

            string questionContent = "Quy luật nào là hạt nhân của phép biện chứng duy vật?";
            string inferredTopic = DocxStateMachineClassifier.InferTopicFromContent(questionContent, customContext);

            Assert.Equal("TRIET_HOC_MAC_LENIN", inferredTopic);

            string resolvedFromSubject = DocxStateMachineClassifier.ResolveTopicCode("Môn: Triết học Mác - Lênin", customContext);
            Assert.Equal("TRIET_HOC_MAC_LENIN", resolvedFromSubject);
        }

        [Fact]
        public void CustomInstructionDirective_FromCigRule_ShouldTerminateReadingPassage()
        {
            IDocxStateMachineClassifier classifier = new DocxStateMachineClassifierService();

            var paragraphs = new List<string>
            {
                "Read the following passage and mark the letter A, B, C, or D:",
                "Ocean plastic pollution is a grave threat to marine life worldwide.",
                "Question 1: What is the main idea of the passage?",
                "A. Ocean pollution",
                "B. Forests",
                "C. Desert",
                "D. Space",
                "Đáp án: A",
                "Choose the letter A, B, C, or D to indicate the word whose underlined part differs:",
                "Question 2: Which word has a different stress?",
                "A. climate",
                "B. animal",
                "C. pollution",
                "D. danger",
                "Đáp án: C"
            };

            var results = classifier.ParseParagraphList(paragraphs);

            foreach (var q in results)
            {
                _output.WriteLine($"[Test 3 Q] No={q.SourceQuestionNo}, Content='{q.Content}', OptsCount={q.Options.Count}, Opts=[{string.Join("; ", q.Options)}], Ans='{q.SuggestedAnswer}'");
            }

            Assert.Equal(2, results.Count);

            // Câu 1 phải gắn liền với bài đọc
            Assert.NotNull(results[0].ContextContent);
            Assert.Contains("Ocean plastic pollution", results[0].ContextContent);

            // Câu 2 phải được ngắt context sạch sẽ bởi Instruction Directive
            Assert.Null(results[1].ContextContent);
        }

        [Fact]
        public void DynamicEssaySafeguard_ShouldPreventFalseOptions_AndClassifyAsEssay()
        {
            IDocxStateMachineClassifier classifier = new DocxStateMachineClassifierService();

            var paragraphs = new List<string>
            {
                "Câu 1 (2,0 điểm).",
                "Cho biểu thức P = (sqrt(x) + 1)/(sqrt(x) - 1).",
                "a) Rút gọn biểu thức P.",
                "b) Tìm tất cả các giá trị của x để P < 1."
            };

            var results = classifier.ParseParagraphList(paragraphs);

            Assert.Single(results);
            Assert.Equal("ESSAY", results[0].QuestionType);
            Assert.Empty(results[0].Options);
            Assert.Contains("a) Rút gọn", results[0].Content);
            Assert.Contains("b) Tìm tất cả", results[0].Content);
        }

        [Fact]
        public void EndOfExam_And_ProctorInstructions_ShouldBeExcludedFromFinalQuestion()
        {
            IDocxStateMachineClassifier classifier = new DocxStateMachineClassifierService();

            var paragraphs = new List<string>
            {
                "Câu 1. Số nghiệm nguyên của phương trình x^2 - 4 = 0 là:",
                "A. 1",
                "B. 2",
                "C. 3",
                "D. 4",
                "Câu 2. Cho hình chóp S.ABCD có đáy là hình vuông. Thể tích khối chóp là:",
                "A. V = 1/3 B.h",
                "B. V = B.h",
                "C. V = 1/2 B.h",
                "D. V = 2 B.h",
                "---------- HẾT ----------",
                "Cán bộ coi thi không giải thích gì thêm.",
                "Thí sinh không được sử dụng tài liệu."
            };

            var results = classifier.ParseParagraphList(paragraphs);

            foreach (var q in results)
            {
                _output.WriteLine($"[EndExam Test] Q{q.SourceQuestionNo}: Content='{q.Content}', OptsCount={q.Options.Count}");
            }

            Assert.Equal(2, results.Count);

            var lastQ = results[1];
            Assert.Equal("SINGLE", lastQ.QuestionType);
            Assert.Equal(4, lastQ.Options.Count);

            // Xác thực: Thân câu hỏi tuyệt đối sạch, không chứa các từ khóa kết thúc hoặc chỉ thị coi thi
            Assert.DoesNotContain("HẾT", lastQ.Content);
            Assert.DoesNotContain("Cán bộ coi thi", lastQ.Content);
            Assert.DoesNotContain("Thí sinh không được sử dụng tài liệu", lastQ.Content);
        }

        [Fact]
        public void Standalone_TheEnd_And_ExamBoundary_InEnglish_ShouldBeExcluded()
        {
            IDocxStateMachineClassifier classifier = new DocxStateMachineClassifierService();

            var paragraphs = new List<string>
            {
                "Question 1: What is the capital of Vietnam?",
                "A. Hanoi",
                "B. Danang",
                "C. Saigon",
                "D. Hue",
                "Question 2: Choose the correct preposition: She is good ___ math.",
                "A. in",
                "B. on",
                "C. at",
                "D. for",
                "--- THE END ---"
            };

            var results = classifier.ParseParagraphList(paragraphs);

            Assert.Equal(2, results.Count);
            var lastQ = results[1];
            Assert.DoesNotContain("THE END", lastQ.Content);
            Assert.Equal(4, lastQ.Options.Count);
        }

        [Fact]
        public void PipelineV4_EarlyReconnaissance_And_Cognitive27Fields_ShouldEnrichExamMetadata()
        {
            IDocxStateMachineClassifier classifier = new DocxStateMachineClassifierService();

            var paragraphs = new List<string>
            {
                "SỞ GD&ĐT HÀ NỘI",
                "TRƯỜNG THPT CHU VĂN AN",
                "KỲ THI TUYỂN SINH LỚP 10 - GIỮA HỌC KỲ I",
                "NĂM HỌC 2025 - 2026",
                "BÀI THI MÔN: TOÁN HỌC",
                "Thời gian làm bài: 90 phút (không kể thời gian giao đề)",
                "MÃ ĐỀ THI: 102",
                "PHẦN II. Câu trắc nghiệm đúng sai. Thí sinh trả lời từ câu 1 đến câu 4.",
                "Câu 1. Cho hàm số y = f(x) có đồ thị như hình vẽ.",
                "a) Hàm số đồng biến trên khoảng (-1; 1).",
                "b) Giá trị cực đại của hàm số là 2.",
                "c) Đồ thị có 2 điểm cực trị.",
                "d) Phương trình f(x) = 0 có 3 nghiệm thực.",
                "Lời giải: Xét hàm số f(x)...",
                "------------------ HẾT ------------------"
            };

            var results = classifier.ParseParagraphList(paragraphs);

            Assert.NotNull(results);
            Assert.Single(results);
            var q = results[0];

            // 1. Cấu trúc khảo thí
            Assert.Equal("1", q.SourceQuestionNo);
            Assert.Equal("PHẦN II", q.SourceSectionName);
            Assert.True(q.QuestionType == "TRUE_FALSE" || q.QuestionType == "MULTI_TRUE_FALSE");
            Assert.Equal("DE_102", q.SourceExamName);

            // 2. Môn học & Nhận thức
            Assert.True(q.TopicCode == "MATH" || q.TopicCode == "TOAN");
            Assert.Equal("Toán học", q.TopicName);
            Assert.Equal("EDUCATION", q.DomainCode);

            // 3. Hệ 5 trục tọa độ khảo thí
            Assert.True(q.BenchmarkYear == 2025 || q.BenchmarkYear == 2026);
            Assert.Equal(5400, q.DurationSeconds); // 90 phút = 5400 giây
            Assert.NotNull(q.IssuingOrg);
            Assert.NotNull(q.TargetLevel);
            Assert.NotNull(q.AssessmentPurpose);

            // 4. Thẻ thông minh
            Assert.NotEmpty(q.Tags);
            Assert.Contains(q.Tags, t => t.Contains("math", StringComparison.OrdinalIgnoreCase) || t.Contains("giao-duc", StringComparison.OrdinalIgnoreCase));

            // 5. Định danh chống trùng
            Assert.False(string.IsNullOrWhiteSpace(q.CanonicalExamUrn));
            Assert.False(string.IsNullOrWhiteSpace(q.CanonicalContentHash));
        }

        [Fact]
        public void NationalExamHeader_ShouldNotCreateSpuriousThptNationalTopic()
        {
            var ctx = DynamicDomainContext.CreateDefault();

            // 1. Phải không bao giờ giải mã "THPT Quốc gia" thành TopicCode "THPT_NATIONAL"
            string resolvedThpt = DocxStateMachineClassifier.ResolveTopicCode("Kỳ thi Tốt nghiệp THPT Quốc Gia", ctx);
            Assert.NotEqual("THPT_NATIONAL", resolvedThpt);
            Assert.Equal("GENERAL", resolvedThpt);

            // 2. Môn thi: HÓA HỌC phải giải mã thành "HOA_HOC"
            string resolvedHoa = DocxStateMachineClassifier.ResolveTopicCode("MÔN: HÓA HỌC (ĐỀ CHÍNH THỨC)", ctx);
            Assert.Equal("HOA_HOC", resolvedHoa);

            // 3. Môn thi: SINH HỌC phải giải mã thành "SINH_HOC"
            string resolvedSinh = DocxStateMachineClassifier.ResolveTopicCode("Môn thi: Sinh học - Khối 12", ctx);
            Assert.Equal("SINH_HOC", resolvedSinh);

            // 4. Môn thi: TIẾNG ANH phải giải mã thành "TIENG_ANH"
            string resolvedAnh = DocxStateMachineClassifier.ResolveTopicCode("BÀI THI: TIẾNG ANH", ctx);
            Assert.Equal("TIENG_ANH", resolvedAnh);

            // 5. Môn thi: TOÁN HỌC phải giải mã thành "MATH"
            string resolvedToan = DocxStateMachineClassifier.ResolveTopicCode("Môn: Toán học", ctx);
            Assert.Equal("MATH", resolvedToan);

            // 6. Kiểm tra bóc tách đề Hóa thi tốt nghiệp THPT Quốc gia
            IDocxStateMachineClassifier classifier = new DocxStateMachineClassifierService();
            var paragraphs = new List<string>
            {
                "BỘ GIÁO DỤC VÀ ĐÀO TẠO",
                "KỲ THI TỐT NGHIỆP TRUNG HỌC PHỔ THÔNG NĂM 2026",
                "Bài thi: HÓA HỌC",
                "Thời gian làm bài: 50 phút",
                "MÃ ĐỀ: 201",
                "Câu 1. Dung dịch chất nào sau đây làm quỳ tím chuyển sang màu đỏ?",
                "A. HCl.",
                "B. NaOH.",
                "C. NaCl.",
                "D. Ba(OH)2.",
                "Đáp án: A"
            };

            var questions = classifier.ParseParagraphList(paragraphs, dynamicContext: ctx);
            Assert.Single(questions);
            var q = questions[0];

            Assert.Equal("HOA_HOC", q.TopicCode);
            Assert.Equal("Hóa học", q.TopicName);
            Assert.Equal("EDUCATION", q.DomainCode);
            Assert.NotEqual("THPT_NATIONAL", q.TopicCode);
            Assert.Contains("Tốt Nghiệp", q.AssessmentPurpose, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void CognitiveScopeManager_LinearDirective_ShouldInferQuestionTypesAcrossRanges()
        {
            // Kiểm tra khả năng suy luận dạng câu hỏi từ chỉ thị tuyến tính không cần "PHẦN I", "PHẦN II"
            IDocxStateMachineClassifier classifier = new DocxStateMachineClassifierService();
            var paragraphs = new List<string>
            {
                "KỲ THI ĐÁNH GIÁ NĂNG LỰC DOANH NGHIỆP",
                "MÃ ĐỀ: NV_01",
                "Từ câu 1 đến câu 2: Mỗi câu chỉ chọn một phương án trả lời đúng nhất.",
                "Câu 1: Thủ đô của Việt Nam là gì?",
                "A. Hà Nội",
                "B. Hải Phòng",
                "C. Đà Nẵng",
                "D. Cần Thơ",
                "Câu 2: Đơn vị tiền tệ của Nhật Bản là gì?",
                "A. Won",
                "B. Yên",
                "C. USD",
                "D. Euro",
                "Từ câu 3 đến câu 4: Thí sinh chọn đúng hoặc sai cho từng ý kiến sau.",
                "Câu 3: Đánh giá các mệnh đề về kinh tế vĩ mô:",
                "a) Lạm phát làm giảm sức mua của đồng tiền.",
                "b) Tăng lãi suất cơ bản nhằm kích thích đầu tư mở rộng.",
                "c) GDP danh nghĩa chưa trừ yếu tố lạm phát.",
                "d) Cán cân thương mại thâm hụt khi xuất khẩu lớn hơn nhập khẩu.",
                "Từ câu 5 đến câu 6: Thí sinh điền câu trả lời ngắn gọn vào phiếu trả lời.",
                "Câu 5: Cho hàm doanh thu R(x) = 100x - 2x^2. Tính doanh thu tối đa.",
                "------------------ HẾT ------------------"
            };

            var questions = classifier.ParseParagraphList(paragraphs);

            Assert.Equal(4, questions.Count);

            // Câu 1 & 2: Phải tự động suy luận SINGLE từ chỉ thị "Từ câu 1 đến câu 2: Mỗi câu chỉ chọn một phương án..."
            Assert.Equal("SINGLE", questions[0].QuestionType);
            Assert.Equal(4, questions[0].Options.Count);
            Assert.Equal("SINGLE", questions[1].QuestionType);

            // Câu 3: Phải tự động suy luận MULTI_TRUE_FALSE từ chỉ thị "Từ câu 3 đến câu 4: Thí sinh chọn đúng hoặc sai..."
            Assert.True(questions[2].QuestionType == "TRUE_FALSE" || questions[2].QuestionType == "MULTI_TRUE_FALSE");
            Assert.Equal(4, questions[2].Options.Count);

            // Câu 5 (vị trí index 3): Phải tự động suy luận SHORT_ANSWER từ chỉ thị "Từ câu 5 đến câu 6: Thí sinh điền câu trả lời ngắn..."
            Assert.Equal("SHORT_ANSWER", questions[3].QuestionType);
            Assert.Empty(questions[3].Options);
        }

        [Fact]
        public void SingleChoice_WithZeroOrOneOption_ShouldFallbackSafelyToShortAnswerOrEssay()
        {
            // Kiểm tra quy tắc Thép Single Choice Fallback: nếu đề không có hoặc chỉ có 1 phương án -> không thể là SINGLE
            IDocxStateMachineClassifier classifier = new DocxStateMachineClassifierService();
            var paragraphs = new List<string>
            {
                "ĐỀ THI TOÁN HỌC TỰ DO",
                "PHẦN I. Trắc nghiệm",
                "Câu 1: Giải phương trình 2x + 6 = 0.", // Có động từ 'Giải phương trình' -> CIG nhận diện là ESSAY tự luận
                "Câu 2: Điểm A(1; 2) thuộc đồ thị hàm số nào?",
                "A. y = 2x." // Chỉ có 1 phương án duy nhất -> fallback SHORT_ANSWER
            };

            var questions = classifier.ParseParagraphList(paragraphs);

            Assert.Equal(2, questions.Count);

            // Câu 1: Không có options và có yêu cầu giải phương trình -> Phải fallback an toàn sang ESSAY
            Assert.NotEqual("SINGLE", questions[0].QuestionType);
            Assert.Equal("ESSAY", questions[0].QuestionType);

            // Câu 2: Chỉ có 1 option và không có từ khóa giải tự luận -> Phải gộp option vào content và fallback sang SHORT_ANSWER
            Assert.NotEqual("SINGLE", questions[1].QuestionType);
            Assert.Equal("SHORT_ANSWER", questions[1].QuestionType);
            Assert.Empty(questions[1].Options);
            Assert.Contains("y = 2x", questions[1].Content);
        }

        [Fact]
        public void EssayExplanation_PhaseAndInline_ShouldExtractDetailedExplanationForVideoStoryboarding()
        {
            IDocxStateMachineClassifier classifier = new DocxStateMachineClassifierService();

            var paragraphs = new List<string>
            {
                "ĐỀ THI TỰ LUẬN TOÁN HỌC",
                "PHẦN TỰ LUẬN",
                "Câu 1: Cho hình chóp S.ABCD có đáy là hình vuông cạnh a. Tính thể tích khối chóp.",
                "Hướng dẫn ý giải:",
                "+ Bước 1: Xác định chiều cao h của khối chóp.",
                "+ Bước 2: Diện tích đáy S = a^2.",
                "+ Bước 3: Thể tích V = 1/3 * S * h = a^3 / 3.",
                "Câu 2: Giải bất phương trình log2(x - 1) < 3.",
                "HƯỚNG DẪN CHẤM VÀ BIỂU ĐIỂM",
                "Câu 2.",
                "Điều kiện: x > 1.",
                "Bất phương trình tương đương: x - 1 < 8 <=> x < 9.",
                "Kết luận: Tập nghiệm S = (1; 9)."
            };

            var cig = AegisQuiz.Infrastructure.Services.DynamicQuestionIngestionEngine.Instance;
            foreach (var p in paragraphs)
            {
                bool isQ = cig.TryMatchQuestionAnchor(p, out int qNum, out var rule, null);
                bool isSol = cig.TryMatchSolutionPhase(p);
                bool isSec = cig.TryMatchSectionHeader(p, out string sec);
                bool isExpl = cig.IsExplanationLine(p);
                _output.WriteLine($"P: '{p}' => isQ={isQ} (qNum={qNum}), isSol={isSol}, isSec={isSec} ({sec}), isExpl={isExpl}");
            }

            var questions = classifier.ParseParagraphList(paragraphs);

            _output.WriteLine($"[EssayExplanation] questions.Count={questions.Count}");
            for (int i = 0; i < questions.Count; i++)
            {
                _output.WriteLine($"  Q[{i}]: Type={questions[i].QuestionType}, No={questions[i].SourceQuestionNo}, Content={questions[i].Content}, Expl={questions[i].AiExplanation}");
            }

            Assert.Equal(2, questions.Count);

            // Câu 1: Tự luận có Hướng dẫn ý giải ngay bên dưới (Inline)
            Assert.Equal("ESSAY", questions[0].QuestionType);
            Assert.NotNull(questions[0].AiExplanation);
            Assert.Contains("Bước 1: Xác định chiều cao", questions[0].AiExplanation);
            Assert.Contains("Bước 3: Thể tích", questions[0].AiExplanation);
            Assert.DoesNotContain("Bước 1: Xác định chiều cao", questions[0].Content); // Đã bóc tách sạch ra khỏi thân đề bài

            // Câu 2: Tự luận có Lời giải / Biểu điểm trong Pha Lời giải (Phase-based)
            Assert.Equal("ESSAY", questions[1].QuestionType);
            Assert.NotNull(questions[1].AiExplanation);
            Assert.Contains("Điều kiện: x > 1", questions[1].AiExplanation);
            Assert.Contains("Tập nghiệm S = (1; 9)", questions[1].AiExplanation);
        }

        [Fact]
        public void AdaptiveHierarchy_CompoundMultiTrueFalseWithSteppedStem_ShouldPopulateSubItemsAndGranularExplanations()
        {
            IDocxStateMachineClassifier classifier = new DocxStateMachineClassifierService();

            var paragraphs = new List<string>
            {
                "PHẦN II. Câu trắc nghiệm đúng sai. Thí sinh trả lời từ câu 1 đến câu 1.",
                "Câu 1. Cho hàm số y = 2x hãy trả lời các ý sau:",
                "a) Hàm số đồng biến trên toàn trục số thực R.",
                "b) Đồ thị hàm số đi qua gốc tọa độ O(0; 0).",
                "c) Hệ số góc của đường thẳng bằng 1.",
                "d) Giá trị của hàm số tại x = 2 bằng 4.",
                "PHẦN III. HƯỚNG DẪN GIẢI CHI TIẾT",
                "PHẦN II",
                "Câu 1",
                "Lời giải",
                "a) Đúng. Vì đạo hàm y' = 2 > 0 với mọi x thuộc R.",
                "b) Đúng. Vì thay x = 0 ta được y = 0 nên đồ thị đi qua gốc tọa độ.",
                "c) Sai. Vì phương trình y = 2x có hệ số góc k = 2 khác 1.",
                "d) Đúng. Vì y(2) = 2*2 = 4."
            };

            var questions = classifier.ParseParagraphList(paragraphs);

            Assert.Single(questions);
            var q = questions[0];

            _output.WriteLine($"[Test Compound] Type={q.QuestionType}, SubItems={q.SubItems.Count}, HasGranular={q.HasGranularExplanations}");
            _output.WriteLine($"[Test Compound] AiExplanation='{q.AiExplanation}'");
            foreach (var sub in q.SubItems)
            {
                _output.WriteLine($" -> Sub [{sub.Key}]: Ans={sub.ExpectedAnswer}, Expl={sub.Explanation}");
            }

            // Kiểm tra phân tầng cấp Câu
            Assert.Equal("MULTI_TRUE_FALSE", q.QuestionType);
            Assert.Contains("Cho hàm số y = 2x hãy trả lời các ý sau:", q.Content);
            Assert.True(q.HasSubItems);
            Assert.Equal(4, q.SubItems.Count);

            // Kiểm tra phân tầng cấp Ý (Sub-Item Level)
            Assert.Equal("a", q.SubItems[0].Key);
            Assert.Equal("Đúng", q.SubItems[0].ExpectedAnswer);
            Assert.Contains("đạo hàm y' = 2 > 0", q.SubItems[0].Explanation);

            Assert.Equal("b", q.SubItems[1].Key);
            Assert.Equal("Đúng", q.SubItems[1].ExpectedAnswer);

            Assert.Equal("c", q.SubItems[2].Key);
            Assert.Equal("Sai", q.SubItems[2].ExpectedAnswer);
            Assert.Contains("hệ số góc k = 2 khác 1", q.SubItems[2].Explanation);

            Assert.Equal("d", q.SubItems[3].Key);
            Assert.Equal("Đúng", q.SubItems[3].ExpectedAnswer);

            // Kiểm tra tính toàn vẹn của Lời giải toàn cục (Dual Explanation Pipeline)
            Assert.True(q.HasGranularExplanations);
            Assert.Contains("đạo hàm y' = 2 > 0", q.AiExplanation);
            Assert.Contains("hệ số góc k = 2 khác 1", q.AiExplanation);
        }

        [Fact]
        public void AdaptiveHierarchy_SingleChoiceQuestion_ShouldMaintainFlatModelWithoutSubItems()
        {
            IDocxStateMachineClassifier classifier = new DocxStateMachineClassifierService();

            var paragraphs = new List<string>
            {
                "PHẦN I. Câu trắc nghiệm nhiều phương án lựa chọn.",
                "Câu 1. Nghiệm của phương trình 2x = 4 là:",
                "A. x = 1",
                "B. x = 2",
                "C. x = 3",
                "D. x = 4",
                "Lời giải: Ta có 2x = 4 <=> x = 2. Chọn B."
            };

            var questions = classifier.ParseParagraphList(paragraphs);

            Assert.Single(questions);
            var q = questions[0];

            // Câu trắc nghiệm đơn phải giữ mô hình phẳng, không tạo SubItems dư thừa
            Assert.Equal("SINGLE", q.QuestionType);
            Assert.Equal(4, q.Options.Count);
            Assert.Equal("2", q.SuggestedAnswer); // B tương ứng index 2
            Assert.False(q.HasSubItems);
            Assert.Empty(q.SubItems);
            Assert.Contains("Chọn B", q.AiExplanation);
        }

        [Fact]
        public void QuestionLevelExplanation_DetachedPhase_ShouldCoverSingleShortAnswerAndEssay()
        {
            IDocxStateMachineClassifier classifier = new DocxStateMachineClassifierService();

            var paragraphs = new List<string>
            {
                "PHẦN I. Câu trắc nghiệm nhiều phương án lựa chọn.",
                "Câu 1. Nghiệm của phương trình 2x = 4 là:",
                "A. x = 1",
                "B. x = 2",
                "C. x = 3",
                "D. x = 4",
                "Câu 2. Nghiệm của phương trình 3x = 9 là:",
                "A. x = 1",
                "B. x = 2",
                "C. x = 3",
                "D. x = 4",
                "PHẦN III. Câu trắc nghiệm trả lời ngắn.",
                "Câu 1. Tính 5 + 7.",
                "PHẦN IV. Tự luận.",
                "Câu 1. Giải phương trình x^2 - 4 = 0.",
                "LỜI GIẢI CHI TIẾT",
                "PHẦN I",
                "Câu 1",
                "Lời giải: Ta có 2x = 4 nên x = 2. Chọn B.",
                "Câu 2",
                "Lời giải: Ta có 3x = 9 nên x = 3. Chọn C.",
                "PHẦN III",
                "Câu 1",
                "Ta có 5 + 7 = 12.",
                "Đáp số: 12",
                "PHẦN IV",
                "Câu 1",
                "Ta có x^2 = 4.",
                "Suy ra x = 2 hoặc x = -2."
            };

            var questions = classifier.ParseParagraphList(paragraphs);

            var single1 = questions.Single(q => q.QuestionType == "SINGLE" && q.SourceQuestionNo == "1");
            var single2 = questions.Single(q => q.QuestionType == "SINGLE" && q.SourceQuestionNo == "2");
            var shortQ = questions.Single(q => q.QuestionType == "SHORT_ANSWER");
            var essayQ = questions.Single(q => q.QuestionType == "ESSAY");

            Assert.Contains("x = 2", single1.AiExplanation);
            Assert.Equal("2", single1.SuggestedAnswer);
            // Lời giải không được rò rỉ sang câu khác trong cùng phần
            Assert.DoesNotContain("3x = 9", single1.AiExplanation);
            Assert.Contains("3x = 9", single2.AiExplanation);
            Assert.Equal("3", single2.SuggestedAnswer);

            Assert.Contains("5 + 7 = 12", shortQ.AiExplanation);
            Assert.Contains("12", shortQ.SuggestedAnswer);

            Assert.Contains("x^2 = 4", essayQ.AiExplanation);
            Assert.Contains("x = -2", essayQ.AiExplanation);
            Assert.Empty(single1.SubItems);
        }

        [Fact]
        public void QuestionLevelExplanation_Inline_ShouldCoverShortAnswerAndMultiLineSingle()
        {
            IDocxStateMachineClassifier classifier = new DocxStateMachineClassifierService();

            var paragraphs = new List<string>
            {
                "PHẦN I. Câu trắc nghiệm nhiều phương án lựa chọn.",
                "Câu 1. Nghiệm của phương trình 2x = 4 là:",
                "A. x = 1",
                "B. x = 2",
                "C. x = 3",
                "D. x = 4",
                "Lời giải:",
                "Chia hai vế cho 2 ta được x = 2.",
                "Chọn B.",
                "Câu 2. Nghiệm của phương trình 3x = 9 là:",
                "A. x = 1",
                "B. x = 2",
                "C. x = 3",
                "D. x = 4",
                "Lời giải: Chia hai vế cho 3 ta được x = 3. Chọn C.",
                "PHẦN III. Câu trắc nghiệm trả lời ngắn.",
                "Câu 1. Tính 5 + 7.",
                "Lời giải: Ta có 5 + 7 = 12.",
                "Đáp số: 12"
            };

            var questions = classifier.ParseParagraphList(paragraphs);

            var single1 = questions.Single(q => q.QuestionType == "SINGLE" && q.SourceQuestionNo == "1");
            var single2 = questions.Single(q => q.QuestionType == "SINGLE" && q.SourceQuestionNo == "2");
            var shortQ = questions.Single(q => q.QuestionType == "SHORT_ANSWER");

            Assert.Contains("x = 2", single1.AiExplanation);
            Assert.DoesNotContain("Chia hai vế cho 3", single1.AiExplanation);
            Assert.Contains("Chia hai vế cho 3", single2.AiExplanation);
            Assert.Contains("5 + 7 = 12", shortQ.AiExplanation);
            Assert.Contains("12", shortQ.SuggestedAnswer);
        }
    }
}
