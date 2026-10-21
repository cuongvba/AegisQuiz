using System;
using AegisQuiz.Infrastructure.Services.Docx;
using Xunit;

namespace AegisQuiz.UnitTests
{
    public class MathLocaleAndAstOptimizationTests
    {
        [Theory]
        [InlineData("3,14", "3{,}14")]
        [InlineData("f(x) = 2,5x + 1", "f(x) = 2{,}5x + 1")]
        [InlineData("x = 0,001", "x = 0{,}001")]
        [InlineData("A(1; 2; 3)", "A(1; 2; 3)")] // Không phải số thập phân, giữ nguyên
        public void NormalizeDecimalCommas_ShouldFormatProperlyForVietnamese(string input, string expected)
        {
            var result = MathLocaleNormalizer.Default.NormalizeDecimalCommas(input, "vi");
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("tg(x) + cotg(x) = 1", @"\tan (x) + \cot (x) = 1")]
        [InlineData(@"\operatorname{tg} x", @"\tan x")]
        [InlineData("arctg(1) = \\pi/4", @"\arctan (1) = \pi/4")]
        [InlineData("sh(x) + ch(x)", @"\sinh (x) + \cosh (x)")]
        public void NormalizeTrigonometry_ShouldConvertLocalFunctionsToStandardKaTeX(string input, string expected)
        {
            var result = MathLocaleNormalizer.Default.NormalizeTrigonometryAndFunctions(input, "vi");
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(@"V = 12 dm^3", @"V = 12 \text{ dm}^{3}")]
        [InlineData(@"a = 9.8 m/s^2", @"a = 9.8 \text{ m/s}^{2}")]
        [InlineData(@"AB = 25 cm", @"AB = 25 \text{ cm}")]
        [InlineData(@"E = 100 kWh", @"E = 100 \text{ kWh}")]
        public void NormalizeScientificUnits_ShouldWrapInTextTag(string input, string expected)
        {
            var result = MathLocaleNormalizer.Default.NormalizeScientificUnits(input, "vi");
            Assert.Equal(expected, result);
        }

        [Fact]
        public void FullNormalization_VectorAndUnitsAndDecimal()
        {
            string input = @"\vec{AB} = 3,5 cm, tg(B) = 1";
            string result = MathLocaleNormalizer.Default.Normalize(input, "vi");

            Assert.Contains(@"\overrightarrow{AB}", result);
            Assert.Contains(@"3{,}5", result);
            Assert.Contains(@"\text{ cm}", result);
            Assert.Contains(@"\tan ", result);
        }
    }
}
