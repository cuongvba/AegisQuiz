using System.Collections.Generic;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Infrastructure.Services;
using AegisQuiz.Infrastructure.Services.Docx;
using Xunit;

namespace AegisQuiz.UnitTests
{
    /// <summary>
    /// Kiểm chứng DocxStateMachineClassifier tuân thủ OCP (hồ sơ dạng câu hỏi), Multi-language
    /// (mẫu ngôn ngữ học từ từ điển) và DIP (engine được tiêm, không phụ thuộc singleton).
    /// </summary>
    public class DocxClassifierRegistryComplianceTests
    {
        private static DynamicQuestionIngestionEngine NewIsolatedEngine() => new();

        [Theory]
        [InlineData("PHẦN III. Câu trắc nghiệm trả lời ngắn", "SHORT_ANSWER")]
        [InlineData("PHẦN II. Câu trắc nghiệm đúng sai", "MULTI_TRUE_FALSE")]
        [InlineData("PHẦN TỰ LUẬN", "ESSAY")]
        [InlineData("PART I", "SINGLE")]
        [InlineData("PHẦN IV", "SINGLE")] // không nhận diện được → mặc định
        public void NormalizeSectionKey_ShouldResolveViaProfileAliases(string raw, string expected)
        {
            var engine = NewIsolatedEngine();
            Assert.Equal(expected, DocxStateMachineClassifier.NormalizeSectionKey(raw, engine));
        }

        [Fact]
        public void NormalizeSectionKey_UnknownSectionTitle_ShouldFallBackToQuestionType()
        {
            var engine = NewIsolatedEngine();
            Assert.Equal("ESSAY", DocxStateMachineClassifier.NormalizeSectionKey("PHẦN IV", "ESSAY", engine));
        }

        [Fact]
        public void CustomQuestionType_ShouldBeSupportedByProfileOnly_WithoutCoreChange()
        {
            var engine = NewIsolatedEngine();
            var dict = engine.GetStructureDictionary();
            dict.QuestionTypeProfiles["ORAL_RECORDING"] = new QuestionTypeProfileDto
            {
                AliasOrder = 5,
                CanonicalSectionKey = "ORAL_RECORDING",
                DefaultDifficulty = 5,
                Capabilities = { QuestionTypeProfileDto.CapFreeText },
                EquivalentTypes = { "ORAL_RECORDING", "SPEAKING" },
                SectionAliasPatterns = { "SPEAKING", "NÓI" }
            };
            engine.UpdateStructureDictionary(dict, persistToFile: false);

            Assert.True(engine.HasTypeCapability("ORAL_RECORDING", QuestionTypeProfileDto.CapFreeText));
            Assert.Equal(5, engine.GetDefaultDifficulty("oral_recording"));
            Assert.Equal("ORAL_RECORDING", DocxStateMachineClassifier.NormalizeSectionKey("Part 3 - Speaking", engine));
            Assert.Contains("SPEAKING", DocxStateMachineClassifier.GetEquivalentSections("ORAL_RECORDING", engine));
        }

        [Theory]
        [InlineData("PHẦN IV. Tự luận", "PHẦN IV")]
        [InlineData("Part III: Short answers", "PART III")]
        [InlineData("TEIL II - Aufgaben", "TEIL II")]
        [InlineData("PARTIE I", "PARTIE I")]
        public void SectionTitlePattern_ShouldBeMultilingual(string line, string expected)
        {
            var engine = NewIsolatedEngine();
            Assert.True(engine.TryMatchLinguisticPattern(LinguisticKeys.SectionTitle, line, out var m));
            Assert.Equal(expected, m.Groups[1].Value.ToUpperInvariant());
        }

        [Theory]
        [InlineData(LinguisticKeys.ChoiceAnswerLine, "Antwort: B", "B")]
        [InlineData(LinguisticKeys.ChoiceAnswerLine, "Đáp án: C", "C")]
        [InlineData(LinguisticKeys.InlineExplanationNote, "Erklärung: weil x=2", "weil x=2")]
        [InlineData(LinguisticKeys.StandardQuestionAnchor, "Frage 7. Wie viel ist 2+2?", "Wie viel ist 2+2?")]
        public void LinguisticPatterns_ShouldCoverNonVietnameseKeywords(string key, string line, string expectedGroup1)
        {
            var engine = NewIsolatedEngine();
            Assert.True(engine.TryMatchLinguisticPattern(key, line, out var m));
            Assert.Equal(expectedGroup1, m.Groups[1].Value.Trim());
        }

        [Fact]
        public void ParseParagraphList_ShouldUseInjectedEngine_AndStripTrailingExamNoise()
        {
            var engine = DynamicQuestionIngestionEngine.Instance;
            var paragraphs = new List<string>
            {
                "Câu 1. 2 + 3 bằng bao nhiêu?", "A. 4", "B. 5", "C. 6", "D. 7",
                "---HẾT---"
            };

            var results = DocxStateMachineClassifier.ParseParagraphList(paragraphs, engineOverride: engine);

            var q = Assert.Single(results);
            Assert.DoesNotContain("HẾT", q.Content);
            Assert.Equal(4, q.Options.Count);
        }
    }
}
