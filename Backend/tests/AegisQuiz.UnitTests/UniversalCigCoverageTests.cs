using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
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

        [Fact]
        public void CIG_HeaderProfile_ShouldCorrectlyDetectChemistrySubject_EvenInTwoColumnHeader()
        {
            // Kiểm chứng đề thi thực tế: Header bảng 2 cột phân cách bằng tab, 'Môn thi: HOÁ HỌC'
            var twoColumnHeader = new List<string>
            {
                "BỘ GIÁO DỤC VÀ ĐÀO TẠO\tKỲ THI TỐT NGHIỆP TRUNG HỌC PHỔ THÔNG NĂM 2026",
                "ĐỀ THI CHÍNH THỨC\tMôn thi: HOÁ HỌC",
                "(Đề thi có 04 trang)\tThời gian làm bài: 50 phút, không kể thời gian phát đề",
                "\tMã đề thi: 101"
            };

            var headerProfile = _cig.ProfileHeader(twoColumnHeader);
            _output.WriteLine($"Detected Subject: {headerProfile.DetectedSubject}");
            Assert.Equal("HOA_HOC", headerProfile.DetectedSubject);

            // Kiểm chứng cả 2 kiểu gõ dấu thanh: 'HOÁ HỌC' và 'HÓA HỌC'
            var topicHoas = UniversalCoordinateTaxonomyEngine.ResolveTopicCode("HOÁ HỌC");
            var topicHoas2 = UniversalCoordinateTaxonomyEngine.ResolveTopicCode("HÓA HỌC");
            Assert.Equal("HOA_HOC", topicHoas);
            Assert.Equal("HOA_HOC", topicHoas2);
        }

        [Fact]
        public void CIG_TaxonomyEngine_ShouldBeDatabaseDriven_ForForeignLanguagesAndSubjects()
        {
            var ctx = DynamicDomainContext.CreateDefault();

            // Kiểm tra các môn ngoại ngữ đa dạng được nạp từ DynamicDomainContext
            Assert.Equal("TIENG_PHAP", UniversalCoordinateTaxonomyEngine.ResolveTopicCode("Tiếng Pháp", ctx));
            Assert.Equal("TIENG_TRUNG", UniversalCoordinateTaxonomyEngine.ResolveTopicCode("Tiếng Trung Quốc", ctx));
            Assert.Equal("TIENG_NHAT", UniversalCoordinateTaxonomyEngine.ResolveTopicCode("Tiếng Nhật", ctx));
            Assert.Equal("TIENG_HAN", UniversalCoordinateTaxonomyEngine.ResolveTopicCode("Tiếng Hàn", ctx));
            Assert.Equal("TIENG_DUC", UniversalCoordinateTaxonomyEngine.ResolveTopicCode("Tiếng Đức", ctx));
            Assert.Equal("TIENG_NGA", UniversalCoordinateTaxonomyEngine.ResolveTopicCode("Tiếng Nga", ctx));
            Assert.Equal("HOA_HOC", UniversalCoordinateTaxonomyEngine.ResolveTopicCode("Môn Hóa", ctx));
            Assert.Equal("SINH_HOC", UniversalCoordinateTaxonomyEngine.ResolveTopicCode("Môn Sinh học", ctx));
        }

        [Fact]
        public void CIG_TaxonomyEngine_InferTopicFromContent_ShouldNotMisclassifyChemistryWithEnglishStopWords()
        {
            // Câu hỏi Hóa học chứa các từ tiếng Anh và ký hiệu khoa học quốc tế ('at 25°C', 'in', 'and', 'mol', 'is')
            var chemContent = "Đốt cháy hoàn toàn 0,1 mol hỗn hợp X in at 25°C and 1 atm, dẫn toàn bộ sản phẩm cháy vào dung dịch Ca(OH)2 dư thu được m gam kết tủa. Giá trị của m là:";
            
            var inferred = UniversalCoordinateTaxonomyEngine.InferTopicFromContent(chemContent);
            _output.WriteLine($"Inferred Topic: {inferred}");
            // Tuyệt đối không được nhầm thành TIENG_ANH vì chứa 'in', 'at', 'and'
            Assert.NotEqual("TIENG_ANH", inferred);
            Assert.Equal("HOA_HOC", inferred);
        }

        [Fact]
        public void UCTE_TwoTierArchitecture_ExamEnvelopeMustBeInheritedWithoutCognitiveDrift()
        {
            // 1. TẦNG 1: Khởi tạo Vỏ bọc Nhận thức Đề thi (Exam Cognitive Envelope) từ Header
            var headerBanners = new List<string>
            {
                "BỘ GIÁO DỤC VÀ ĐÀO TẠO\tKỲ THI TỐT NGHIỆP TRUNG HỌC PHỔ THÔNG NĂM 2026",
                "ĐỀ THI CHÍNH THỨC\tMôn thi: HOÁ HỌC",
                "(Đề thi có 04 trang)\tThời gian làm bài: 50 phút"
            };

            var envelope = UniversalCoordinateTaxonomyEngine.SniffCoordinates(
                fileName: "De_Thi_Hoa_Hoc_2026.docx",
                sheetName: null,
                headerBannerLines: headerBanners);

            Assert.Equal("HOA_HOC", envelope.TopicCode);
            Assert.Equal("Hóa học", envelope.TopicName);
            Assert.Equal("EDUCATION", envelope.DomainCode);
            Assert.Equal("Bộ Giáo dục và Đào tạo", envelope.IssuingOrg);
            Assert.Equal(2026, envelope.BenchmarkYear);

            // 2. TẦNG 2: Thực thể Câu hỏi kế thừa Vỏ bọc Header và chỉ làm giàu Item-Delta
            var q = new DocxImportPreviewDto
            {
                // Nội dung câu hỏi cố tình chứa từ khóa nhiễu (ngân hàng, tài chính, từ tiếng Anh)
                Content = "Ngân hàng đề thi: Cho 1 mol kim loại tác dụng với axit in at 25°C thu được muối. Tính khối lượng?",
                QuestionType = "SINGLE_CHOICE",
                SourceExamName = "DE_101",
                SourceSectionName = "PHẦN I",
                SourceQuestionNo = "1",
                DomainCode = envelope.DomainCode,
                TopicCode = envelope.TopicCode,
                TopicName = envelope.TopicName,
                IssuingOrg = envelope.IssuingOrg,
                AssessmentPurpose = envelope.AssessmentPurpose,
                BenchmarkYear = envelope.BenchmarkYear,
                BenchmarkStandard = envelope.BenchmarkStandard,
                Tags = new List<string>(envelope.Tags)
            };

            // Thực hiện làm giàu tọa độ
            UniversalCoordinateTaxonomyEngine.AutoEnrichTaxonomyAndTags(q);

            // Xác minh tính bất biến của Vỏ bọc nhận thức (Không bị lệch sang BANKING hay TIENG_ANH)
            Assert.Equal("EDUCATION", q.DomainCode);
            Assert.Equal("HOA_HOC", q.TopicCode);
            Assert.Equal("Bộ Giáo dục và Đào tạo", q.IssuingOrg);
            Assert.Equal(2026, q.BenchmarkYear);

            // Xác minh Thẻ vi mô ở cấp Câu hỏi (Item-Level Delta Tags)
            Assert.Contains("phan-i", q.Tags);
            Assert.Contains("hoa-hoc", q.Tags);
            Assert.Contains("de-101", q.Tags);
        }

        [Fact]
        public void UCTE_ZeroHardcode_CustomTenantPresets_ResolvedDynamically()
        {
            // 1. Arrange: Tạo ngữ cảnh tenant hoàn toàn tùy biến (Hàng không Vũ trụ - AEROSPACE)
            var customContext = new DynamicDomainContext
            {
                Domains = new List<DynamicDomainSummaryDto>
                {
                    new() { Code = "AEROSPACE", Name = "Hàng không & Vũ trụ", Description = "Khí động học, tên lửa, vệ tinh", Synonyms = new() { "hàng không", "vũ trụ", "aerospace", "khí động" } }
                },
                CoordinatePresets = new List<TenantCoordinatePresetDto>
                {
                    new() { DomainCode = "AEROSPACE", CoordinateType = "TARGET_LEVEL", PresetCode = "SENIOR_FLIGHT_ENGINEER", PresetLabel = "Kỹ sư bay cấp cao", Synonyms = new() { "kỹ sư bay", "senior flight engineer" } },
                    new() { DomainCode = "AEROSPACE", CoordinateType = "ISSUING_ORG", PresetCode = "VAST_VNSC", PresetLabel = "Trung tâm Vũ trụ Việt Nam (VNSC)", Synonyms = new() { "vnsc", "trung tâm vũ trụ", "viện hàn lâm" } },
                    new() { DomainCode = "AEROSPACE", CoordinateType = "BENCHMARK_STANDARD", PresetCode = "AS9100D", PresetLabel = "Tiêu chuẩn Quản lý Hàng không Vũ trụ AS9100D", Synonyms = new() { "as9100d", "as9100", "iso 9100" } },
                    new() { DomainCode = "AEROSPACE", CoordinateType = "ASSESSMENT_PURPOSE", PresetCode = "FLIGHT_CERTIFICATION", PresetLabel = "Sát hạch Chứng chỉ Năng định Bay", Synonyms = new() { "năng định bay", "flight certification" } }
                },
                Topics = new List<BankTopicSummaryDto>
                {
                    new() { Code = "PROPULSION", Name = "Hệ thống Lực đẩy Tên lửa", DomainCode = "AEROSPACE", Keywords = new() { "lực đẩy", "động cơ phản lực", "tên lửa", "nhiên liệu lỏng" } }
                }
            };

            var header = new[]
            {
                "TRUNG TÂM VŨ TRỤ VIỆT NAM (VNSC) - VIỆN HÀN LÂM KH&CN",
                "KỲ THI SÁT HẠCH CHỨNG CHỈ NĂNG ĐỊNH BAY NĂM 2026",
                "Khung năng lực: Kỹ sư bay cấp cao",
                "Môn thi: Hệ thống Lực đẩy Tên lửa",
                "Căn cứ: Tiêu chuẩn Quản lý Hàng không Vũ trụ AS9100D"
            };

            // 2. Act: Bóc tách tọa độ
            var result = UniversalCoordinateTaxonomyEngine.SniffCoordinates(
                fileName: "DE_THI_VNSC_2026.docx",
                sheetName: null,
                headerBannerLines: header,
                sampleContents: new[] { "Áp suất buồng đốt của động cơ tên lửa nhiên liệu lỏng được xác định theo công thức nào?" },
                sampleReferences: null,
                dynamicContext: customContext);

            // 3. Assert: 100% tọa độ và thẻ thông minh được trích xuất động từ Tenant Presets
            Assert.NotNull(result);
            Assert.Equal("AEROSPACE", result.DomainCode);
            Assert.Equal("PROPULSION", result.TopicCode);
            Assert.Equal("Hệ thống Lực đẩy Tên lửa", result.TopicName);
            Assert.Equal("Kỹ sư bay cấp cao", result.TargetLevel);
            Assert.Equal("Trung tâm Vũ trụ Việt Nam (VNSC)", result.IssuingOrg);
            Assert.Equal("Tiêu chuẩn Quản lý Hàng không Vũ trụ AS9100D", result.BenchmarkStandard);
            Assert.Equal("Sát hạch Chứng chỉ Năng định Bay", result.AssessmentPurpose);
            Assert.Equal(2026, result.BenchmarkYear);

            // Xác minh thẻ thông minh được tạo tự động không hard-code
            Assert.Contains(result.Tags, t => t.Contains("senior-flight-engineer") || t.Contains("ky-su-bay"));
            Assert.Contains(result.Tags, t => t.Contains("as9100d") || t.Contains("as9100"));
            Assert.Contains(result.Tags, t => t.Contains("vnsc"));
        }

        [Fact]
        public void UCTE_ITaxonomyEngine_DependencyInjection_ContractCompliance()
        {
            // Kiểm tra tính tuân thủ Clean Architecture & Dependency Injection qua ITaxonomyEngine
            AegisQuiz.Application.Interfaces.ITaxonomyEngine engine = new UniversalCoordinateTaxonomyEngine();

            var result = engine.SniffCoordinates(
                fileName: "De_Thi_THPT_2026.docx",
                sheetName: null,
                headerBannerLines: new[] { "BỘ GIÁO DỤC VÀ ĐÀO TẠO", "KỲ THI TỐT NGHIỆP THPT NĂM 2026", "Bài thi: MÔN HÓA HỌC" },
                sampleContents: new[] { "Dung dịch NaOH phản ứng với dung dịch HCl tạo thành muối gì?" },
                sampleReferences: null,
                dynamicContext: null);

            Assert.NotNull(result);
            Assert.Equal("EDUCATION", result.DomainCode);
            Assert.Equal("HOA_HOC", result.TopicCode);
            Assert.Equal("Bộ Giáo dục và Đào tạo", result.IssuingOrg);
            Assert.Equal("Kỳ thi Tốt nghiệp THPT Quốc Gia", result.AssessmentPurpose);
            Assert.Equal("Chương trình GDPT 2018", result.BenchmarkStandard);
        }

        [Fact]
        public void UCTE_DefaultStandardFallback_NoSwitchCase()
        {
            // Kiểm tra fallback chuẩn mực của tất cả các domain tiêu chuẩn đọc trực tiếp từ CoordinatePresets
            var ctx = DynamicDomainContext.CreateDefault();

            var govDriving = UniversalCoordinateTaxonomyEngine.SniffCoordinates(
                fileName: "SAT_HACH_GPLX.docx",
                sheetName: null,
                headerBannerLines: new[] { "CỤC ĐƯỜNG BỘ VIỆT NAM", "SÁT HẠCH LÁI XE HẠNG B2" },
                sampleContents: null,
                sampleReferences: null,
                dynamicContext: ctx);

            Assert.Equal("GOV_DRIVING", govDriving.DomainCode);
            Assert.Equal("Nghị định 100/2019/NĐ-CP & QC 41:2019", govDriving.BenchmarkStandard);

            var healthcare = UniversalCoordinateTaxonomyEngine.SniffCoordinates(
                fileName: "BAC_SI_NOI_KHOA.docx",
                sheetName: null,
                headerBannerLines: new[] { "BỘ Y TẾ", "KỲ THI SÁT HẠCH BÁC SĨ CKI" },
                sampleContents: null,
                sampleReferences: null,
                dynamicContext: ctx);

            Assert.Equal("HEALTHCARE", healthcare.DomainCode);
            Assert.Equal("Phác đồ Điều trị Bộ Y tế 2025", healthcare.BenchmarkStandard);
        }

        [Fact]
        public void InformaticsExam_With17SubCategories_MustMaintainSingleTopicAndMapSubCategories()
        {
            // Mô phỏng tài liệu đề thi thử môn Tin học có Header chuẩn và 17 phân mục/chủ đề bài học bên trong
            var paragraphs = new List<string>
            {
                "SỞ GIÁO DỤC VÀ ĐÀO TẠO HÀ NỘI",
                "KỲ THI THỬ TỐT NGHIỆP THPT NĂM 2025",
                "BÀI THI: TIN HỌC",
                "Thời gian làm bài: 50 phút"
            };

            for (int i = 1; i <= 17; i++)
            {
                paragraphs.Add($"Chủ đề {i}: Chuyên đề kiến thức tin học phần {i}");
                paragraphs.Add($"Câu {i}: Nội dung câu hỏi trắc nghiệm của chủ đề {i} là gì?");
                paragraphs.Add("A. Phương án A");
                paragraphs.Add("B. Phương án B");
                paragraphs.Add("C. Phương án C");
                paragraphs.Add("D. Phương án D");
                paragraphs.Add("Đáp án: A");
            }

            var langProfile = _cig.DetectDocumentLanguageProfile(paragraphs, "vi");
            var activeRules = _cig.GetRulesForProfile(langProfile);
            _output.WriteLine($"LangProfile: Host={langProfile.HostLanguage}, Subj={langProfile.SubjectLanguage}, RulesCount={activeRules.Count}");
            foreach (var r in activeRules.Take(5))
            {
                _output.WriteLine($"  Rule: {r.RuleCode}, Lang={r.Language}, Anchor={r.AnchorSpec?.RegexPattern}");
            }

            for (int i = 0; i < Math.Min(10, paragraphs.Count); i++)
            {
                bool isQWithActive = _cig.TryMatchQuestionAnchor(paragraphs[i], out int qn1, out var r1, activeRules);
                bool isQWithout = _cig.TryMatchQuestionAnchor(paragraphs[i], out int qn2, out var r2);
                _output.WriteLine($"Line [{i}]: '{paragraphs[i]}' -> WithActive={isQWithActive} (Rule={r1?.RuleCode}), Without={isQWithout} (Rule={r2?.RuleCode})");
            }

            IDocxStateMachineClassifier classifier = new DocxStateMachineClassifierService();
            var results = classifier.ParseParagraphList(paragraphs);

            _output.WriteLine($"Total parsed questions: {results.Count}");
            _output.WriteLine($"FSM Debug Logs ({DocxStateMachineClassifier.LastDebugLogs.Count} entries):");
            foreach (var log in DocxStateMachineClassifier.LastDebugLogs.Take(30))
            {
                _output.WriteLine("  " + log);
            }
            foreach (var r in results)
            {
                _output.WriteLine($"Q: {r.SourceQuestionNo}, Content: {r.Content}, Topic: {r.TopicCode}, SubCat: {r.SubCategory}");
            }

            // 1. Phải nhận dạng chính xác 17 câu hỏi
            Assert.Equal(17, results.Count);

            // 2. TẤT CẢ câu hỏi BẮT BUỘC có cùng 1 Topic duy nhất là TIN_HOC (hoặc CNTT)
            // TUYỆT ĐỐI KHÔNG sinh ra 17 TopicCode khác nhau!
            var distinctTopics = results.Select(r => r.TopicCode).Distinct().ToList();
            Assert.Single(distinctTopics);
            Assert.True(distinctTopics[0] == "TIN_HOC" || distinctTopics[0] == "CNTT");

            // 3. Toàn bộ 17 chủ đề phân mục phải được ánh xạ chính xác vào SubCategory
            for (int i = 1; i <= 17; i++)
            {
                var q = results[i - 1];
                Assert.NotNull(q.SubCategory);
                Assert.Contains($"Chủ đề {i}", q.SubCategory);
            }
        }
    }
}

