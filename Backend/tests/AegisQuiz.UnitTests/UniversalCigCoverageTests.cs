using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Infrastructure.Services;
using AegisQuiz.Infrastructure.Services.Docx;
using Xunit;

namespace AegisQuiz.UnitTests
{
    /// <summary>
    /// Bộ kiểm thử bao trùm kiểm chứng Cognitive Ingestion Grammar (CIG) v2.0
    /// Xác thực khả năng bóc tách đa quốc gia (GDPT 2025, GPLX 600 câu, SAT/ACT Mỹ, Gaokao Trung Quốc)
    /// và khẳng định DocxStateMachineClassifier hoàn toàn Zero-Hardcode.
    /// </summary>
    public class UniversalCigCoverageTests
    {
        private readonly DynamicQuestionIngestionEngine _cig;
        private readonly Xunit.Abstractions.ITestOutputHelper _output;

        public UniversalCigCoverageTests(Xunit.Abstractions.ITestOutputHelper output)
        {
            _cig = DynamicQuestionIngestionEngine.Instance;
            _output = output;
        }

        [Fact]
        public void CIG_ShouldMatchExamHeadings_Dynamically()
        {
            // Các định dạng tiêu đề đề thi Việt Nam và quốc tế
            Assert.True(_cig.TryMatchExamHeading("ĐỀ SỐ 01 - ÔN THI TỐT NGHIỆP THPT", out var exam1));
            Assert.Equal("1", exam1.TrimStart('0'));

            Assert.True(_cig.TryMatchExamHeading("ĐỀ THI THỬ 02", out var exam2));
            Assert.Equal("2", exam2.TrimStart('0'));

            Assert.True(_cig.TryMatchExamHeading("EXAM 03", out var exam3));
            Assert.Equal("3", exam3.TrimStart('0'));

            Assert.True(_cig.TryMatchExamHeading("TEST NO 4", out var exam4));
            Assert.Equal("4", exam4.TrimStart('0'));
        }

        [Fact]
        public void CIG_ShouldMatchSectionHeaders_MultiNational()
        {
            // GDPT 2025 Việt Nam
            Assert.True(_cig.TryMatchSectionHeader("PHẦN I. Câu trắc nghiệm nhiều phương án lựa chọn.", out var sec1));
            Assert.Equal("SINGLE", sec1);

            Assert.True(_cig.TryMatchSectionHeader("PHẦN II. Câu trắc nghiệm đúng sai.", out var sec2));
            Assert.Equal("TRUE_FALSE", sec2);

            Assert.True(_cig.TryMatchSectionHeader("PHẦN III. Câu trắc nghiệm trả lời ngắn.", out var sec3));
            Assert.Equal("SHORT_ANSWER", sec3);

            // Quốc tế SAT / Gaokao
            Assert.True(_cig.TryMatchSectionHeader("PART A: Multiple Choice Questions", out var secSatA));
            Assert.Equal("SINGLE", secSatA);

            Assert.True(_cig.TryMatchSectionHeader("SECTION 2: True/False Logic Matrix", out var secSatB));
            Assert.Equal("TRUE_FALSE", secSatB);

            Assert.True(_cig.TryMatchSectionHeader("PART C: Short Answer Numeric Value", out var secSatC));
            Assert.Equal("SHORT_ANSWER", secSatC);

            Assert.True(_cig.TryMatchSectionHeader("一、单项选择题 (20分)", out var secGk1));
            Assert.Equal("SINGLE", secGk1);

            Assert.True(_cig.TryMatchSectionHeader("二、填空题 (30分)", out var secGk2));
            Assert.Equal("SHORT_ANSWER", secGk2);
        }

        [Fact]
        public void CIG_ShouldDetectCriticalQuestionsAndSubCategories_GPLX()
        {
            // GPLX 600 câu - Điểm liệt và Phân nhóm
            var fatalPrompt1 = "Câu 15: Hành vi điều khiển xe cơ giới chạy quá tốc độ quy định, giành đường, vượt ẩu có bị nghiêm cấm hay không? (ĐIỂM LIỆT)";
            Assert.True(_cig.IsCriticalQuestion(fatalPrompt1));

            var fatalPrompt2 = "Câu 20: Người điều khiển xe mô tô, ô tô mà trong máu hoặc hơi thở có nồng độ cồn có bị nghiêm cấm không?";
            Assert.True(_cig.IsCriticalQuestion(fatalPrompt2));

            var saHinhPrompt = "Câu 450: Thứ tự các xe đi như thế nào là đúng quy tắc giao thông theo sa hình dưới đây?";
            Assert.Equal("SA_HINH", _cig.DetectSubCategory(saHinhPrompt));

            var bienBaoPrompt = "Câu 320: Biển nào cấm máy kéo kéo theo rơ-moóc?";
            Assert.Equal("BIEN_BAO", _cig.DetectSubCategory(bienBaoPrompt));
        }

        [Fact]
        public void CIG_ShouldExtractInlineAnswers_Accurately()
        {
            // Single Choice
            Assert.True(_cig.TryExtractInlineAnswer("Chọn đáp án B", "SINGLE", null, out var ansB));
            Assert.Equal("2", ansB); // B -> 2

            Assert.True(_cig.TryExtractInlineAnswer("Key: C", "SINGLE", null, out var ansC));
            Assert.Equal("3", ansC); // C -> 3

            Assert.True(_cig.TryExtractInlineAnswer("正确答案是: D", "SINGLE", null, out var ansD));
            Assert.Equal("4", ansD); // D -> 4

            // Short Answer
            Assert.True(_cig.TryExtractInlineAnswer("Đáp số: 25.5", "SHORT_ANSWER", null, out var ansNum));
            Assert.Equal("25.5", ansNum);

            Assert.True(_cig.TryExtractInlineAnswer("Result = -12.4", "SHORT_ANSWER", null, out var ansNeg));
            Assert.Equal("-12.4", ansNeg);

            // True False
            Assert.True(_cig.TryExtractInlineAnswer("a) Mệnh đề đúng", "TRUE_FALSE", null, out var ansTfA));
            Assert.Equal("a:Đ", ansTfA);

            Assert.True(_cig.TryExtractInlineAnswer("b) Sai", "TRUE_FALSE", null, out var ansTfB));
            Assert.Equal("b:S", ansTfB);
        }

        [Fact]
        public void CIG_ShouldExtractInlineOptions_Robustly()
        {
            var line4Opts = "A. Phương án 1    B. Phương án 2    C. Phương án 3    D. Phương án 4";
            var parsed = _cig.ExtractOptions(line4Opts);
            Assert.Equal(4, parsed.Count);
            Assert.Equal('A', parsed[0].Letter);
            Assert.Equal("Phương án 1", parsed[0].Content);
            Assert.Equal('D', parsed[3].Letter);
            Assert.Equal("Phương án 4", parsed[3].Content);

            var lineMarkedOpt = "*A. Đáp án đúng    B. Đáp án sai";
            var parsedMarked = _cig.ExtractOptions(lineMarkedOpt);
            Assert.Equal(2, parsedMarked.Count);
            Assert.True(parsedMarked[0].IsMarkedCorrect);
            Assert.False(parsedMarked[1].IsMarkedCorrect);
        }

        [Fact]
        public void DocxStateMachineClassifier_ShouldParseFullExam_ViaCIG()
        {
            // Giả lập luồng văn bản đề thi trọn vẹn qua Stream Orchestrator
            var paragraphs = new List<string>
            {
                "ĐỀ 1",
                "Môn: Toán học",
                "PHẦN I. Câu trắc nghiệm nhiều phương án lựa chọn.",
                "Câu 1. Cho hàm số y = f(x) có bảng biến thiên...",
                "A. Đồng biến    B. Nghịch biến    C. Cực đại    D. Cực tiểu",
                "Lời giải:",
                "Dựa vào bảng biến thiên ta chọn đáp án A.",
                "PHẦN II. Câu trắc nghiệm đúng sai.",
                "Câu 1. Cho hình lập phương ABCD.A'B'C'D' có cạnh bằng 1.",
                "a) Độ dài đoạn thẳng AC bằng sqrt(2).",
                "b) Góc giữa AC và BD bằng 90 độ.",
                "Lời giải:",
                "a) Mệnh đề đúng.",
                "b) Mệnh đề đúng.",
                "PHẦN III. Câu trắc nghiệm trả lời ngắn.",
                "Câu 1. Tính tích phân I từ 0 đến 1 của 2x dx.",
                "Đáp số: 1"
            };

            _output.WriteLine($"TryMatchSolutionPhase('Lời giải:'): {_cig.TryMatchSolutionPhase("Lời giải:")}");
            _output.WriteLine($"TryMatchAnswerTablePhase('Lời giải:'): {_cig.TryMatchAnswerTablePhase("Lời giải:")}");

            var results = DocxStateMachineClassifier.ParseParagraphList(paragraphs, null, null);

            _output.WriteLine($"Total parsed results: {results.Count}");
            for (int i = 0; i < results.Count; i++)
            {
                _output.WriteLine($"Result[{i}]: Type={results[i].QuestionType}, Topic={results[i].TopicCode}, Content={results[i].Content}");
                for (int j = 0; j < results[i].Options.Count; j++)
                {
                    _output.WriteLine($"   Opt[{j}]: {results[i].Options[j]}");
                }
            }

            // Kiểm tra số lượng và phân loại chính xác
            Assert.Equal(3, results.Count);

            var q1 = results[0];
            Assert.Equal("SINGLE", q1.QuestionType);
            Assert.Equal("MATH", q1.TopicCode);
            Assert.Equal(4, q1.Options.Count);
            Assert.Equal("1", q1.SuggestedAnswer); // Chọn đáp án A -> 1

            var q2 = results[1];
            Assert.True(q2.QuestionType == "MULTI_TRUE_FALSE" || q2.QuestionType == "TRUE_FALSE");
            Assert.Equal(2, q2.Options.Count);
            Assert.Contains("a) Đúng", q2.SuggestedAnswer);
            Assert.Contains("b) Đúng", q2.SuggestedAnswer);

            var q3 = results[2];
            Assert.Equal("SHORT_ANSWER", q3.QuestionType);
            Assert.Equal("1", q3.SuggestedAnswer);
        }

        [Fact]
        public void CIG_ShouldDetectDualLayerLanguageProfile_AndPruneRules()
        {
            // 1. Kịch bản Đề song ngữ: Đề Tiếng Anh lớp 7 tại Việt Nam (Global Success)
            var bilingualSample = new List<string>
            {
                "PHÒNG GD&ĐT THỊ XÃ ...",
                "ĐỀ KIỂM TRA CUỐI HỌC KỲ II - NĂM HỌC 2025-2026",
                "MÔN: TIẾNG ANH 7 (GLOBAL SUCCESS)",
                "PART A: LISTENING",
                "Question 1. Listen and choose the correct answer to complete the sentence.",
                "A. energetic      B. traditional     C. delicious      D. festival",
                "PART B: LANGUAGE FOCUS",
                "Question 2. Choose the word whose underlined part is pronounced differently.",
                "LỜI GIẢI CHI TIẾT:",
                "Câu 1: Chọn đáp án D vì trong bài nghe..."
            };

            var profileBilingual = _cig.DetectDocumentLanguageProfile(bilingualSample);
            _output.WriteLine($"Bilingual Profile: {profileBilingual}");
            Assert.Equal("vi", profileBilingual.HostLanguage);
            Assert.Equal("en", profileBilingual.SubjectLanguage);
            Assert.True(profileBilingual.IsBilingual);

            // Kiểm tra Adaptive Rule Pruning: Rule tiếng Anh THCS và tiếng Việt được đưa lên đầu
            var prunedRules = _cig.GetRulesForProfile(profileBilingual);
            Assert.NotEmpty(prunedRules);
            var topRule = prunedRules[0];
            Assert.True(topRule.Language == "en" || topRule.Language == "vi");

            // 2. Kịch bản Đề Hàn Quốc Suneung
            var koreanSample = new List<string>
            {
                "2026학년도 대학수학능력시험 6월 모의평가 문제지",
                "제 1 교시 국어 영역",
                "1. 다음 대화를 읽고 물음에 답하시오.",
                "① 1번    ② 2번    ③ 3번    ④ 4번    ⑤ 5번"
            };

            var profileKo = _cig.DetectDocumentLanguageProfile(koreanSample);
            _output.WriteLine($"Korean Profile: {profileKo}");
            Assert.Equal("ko", profileKo.HostLanguage);
            Assert.Equal("ko", profileKo.SubjectLanguage);
            Assert.False(profileKo.IsBilingual);

            // 3. Kịch bản Đề Trung Quốc Gaokao
            var chineseSample = new List<string>
            {
                "2026年普通高等学校招生全国统一考试",
                "数学（理工农医类）",
                "一、选择题：本大题共12小题，每小题5分，共60分。",
                "1. 设集合A = {x | x > 0}，则下列选项正确的是"
            };

            var profileZh = _cig.DetectDocumentLanguageProfile(chineseSample);
            _output.WriteLine($"Chinese Profile: {profileZh}");
            Assert.Equal("zh", profileZh.HostLanguage);
            Assert.Equal("zh", profileZh.SubjectLanguage);

            // 4. Kịch bản Đề Tiếng Việt thuần túy
            var viSample = new List<string>
            {
                "ĐỀ THI THỬ TỐT NGHIỆP THPT NĂM 2026",
                "MÔN: TOÁN HỌC",
                "PHẦN I. Câu trắc nghiệm nhiều phương án lựa chọn.",
                "Câu 1. Cho hàm số y = f(x) liên tục trên đoạn [0; 2]...",
                "Lời giải: Dựa vào bảng biến thiên ta chọn đáp án A."
            };

            var profileVi = _cig.DetectDocumentLanguageProfile(viSample);
            _output.WriteLine($"Vietnamese Profile: {profileVi}");
            Assert.Equal("vi", profileVi.HostLanguage);
            Assert.Equal("vi", profileVi.SubjectLanguage);
            Assert.False(profileVi.IsBilingual);
        }
    }
}

