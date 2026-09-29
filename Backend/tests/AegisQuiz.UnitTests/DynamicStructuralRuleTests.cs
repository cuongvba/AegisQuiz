using System;
using System.Collections.Generic;
using System.Linq;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Infrastructure.Services;
using Xunit;

namespace AegisQuiz.UnitTests
{
    [Collection("MathDictionaryCollection")]
    public class DynamicStructuralRuleTests
    {
        [Fact]
        public void DynamicStructuralRules_LoadedFromRegistry_Successfully()
        {
            // Arrange
            var service = new DynamicMathDictionaryService();

            // Act
            var registry = service.GetCurrentRegistry();

            // Assert
            Assert.NotNull(registry);
            Assert.NotNull(registry.StructuralSimplificationRules);
            Assert.NotEmpty(registry.StructuralSimplificationRules);

            var ruleIds = registry.StructuralSimplificationRules.Select(r => r.Id).ToList();
            Assert.Contains("RULE_SIMPLE_FRACTION", ruleIds);
            Assert.Contains("RULE_RADICAL_OVERLINE", ruleIds);
            Assert.Contains("RULE_SYSTEM_OF_EQUATIONS", ruleIds);
            Assert.Contains("RULE_LIMIT_EXPRESSION", ruleIds);
            Assert.Contains("RULE_VECTOR_OVERLINE", ruleIds);
        }

        [Fact]
        public void TryMatchStructuralRule_MatchesSimpleFraction()
        {
            // Arrange
            var service = new DynamicMathDictionaryService();
            var lines = new List<(int x1, int y1, int x2, int y2)>
            {
                (10, 20, 60, 20) // Đường kẻ ngang phân số tại Y = 20
            };

            var textItems = new List<WmfSimpleTextItem>
            {
                new() { Text = "1", X = 30, Y = 10, Width = 10, FontSize = 14 }, // Tử số (Above)
                new() { Text = "2", X = 30, Y = 30, Width = 10, FontSize = 14 }  // Mẫu số (Below)
            };

            // Act
            bool matched = service.TryMatchStructuralRule(lines, textItems, 0, 0, 14, out var resultItems);

            // Assert
            Assert.True(matched);
            Assert.Single(resultItems);
            Assert.Equal("\\frac{1}{2}", resultItems[0].Text);
        }

        [Fact]
        public void TryMatchStructuralRule_MatchesRadicalOverline()
        {
            // Arrange
            var service = new DynamicMathDictionaryService();
            var lines = new List<(int x1, int y1, int x2, int y2)>
            {
                (25, 12, 55, 12) // Nóc căn tại Y = 12
            };

            var textItems = new List<WmfSimpleTextItem>
            {
                new() { Text = "\\sqrt", X = 10, Y = 18, Width = 15, FontSize = 14 },
                new() { Text = "3", X = 32, Y = 20, Width = 10, FontSize = 14 } // Biểu thức dưới căn
            };

            // Act
            bool matched = service.TryMatchStructuralRule(lines, textItems, 0, 0, 14, out var resultItems);

            // Assert
            Assert.True(matched);
            Assert.Single(resultItems);
            Assert.Equal("\\sqrt{3}", resultItems[0].Text);
        }

        [Fact]
        public void TryMatchStructuralRule_MatchesSystemOfEquations()
        {
            // Arrange
            var service = new DynamicMathDictionaryService();
            var lines = new List<(int x1, int y1, int x2, int y2)>(); // Không có nét vẽ

            var textItems = new List<WmfSimpleTextItem>
            {
                new() { Text = "{", X = 10, Y = 25, Width = 12, FontSize = 28, IsSymbol = true },
                new() { Text = "x+y=1", X = 25, Y = 16, Width = 45, FontSize = 14 }, // Dòng 1 (Upper)
                new() { Text = "x-y=0", X = 25, Y = 34, Width = 45, FontSize = 14 }  // Dòng 2 (Lower)
            };

            // Act
            bool matched = service.TryMatchStructuralRule(lines, textItems, 0, 0, 14, out var resultItems);

            // Assert
            Assert.True(matched);
            Assert.Single(resultItems);
            Assert.Contains("\\begin{cases}", resultItems[0].Text);
            Assert.Contains("x+y=1", resultItems[0].Text);
            Assert.Contains("x-y=0", resultItems[0].Text);
            Assert.Contains("\\end{cases}", resultItems[0].Text);
        }

        [Fact]
        public void TryMatchStructuralRule_MatchesLimitExpression()
        {
            // Arrange
            var service = new DynamicMathDictionaryService();
            var lines = new List<(int x1, int y1, int x2, int y2)>();

            var textItems = new List<WmfSimpleTextItem>
            {
                new() { Text = "\\lim", X = 10, Y = 18, Width = 25, FontSize = 14 },
                new() { Text = "x\\to0", X = 12, Y = 28, Width = 22, FontSize = 10 }, // Dưới lim
                new() { Text = "f(x)", X = 40, Y = 18, Width = 25, FontSize = 14 }   // Bên phải
            };

            // Act
            bool matched = service.TryMatchStructuralRule(lines, textItems, 0, 0, 14, out var resultItems);

            // Assert
            Assert.True(matched);
            Assert.Single(resultItems);
            Assert.Equal("\\lim_{x\\to0} f(x)", resultItems[0].Text);
        }

        [Fact]
        public void HotReload_AppliesNewStructuralRule_ZeroDowntime()
        {
            // Arrange
            var service = new DynamicMathDictionaryService();
            string customRuleJson = @"{
  ""version"": ""2026.3.TEST"",
  ""symbolGlyphs"": [],
  ""greekLetters"": [],
  ""structuralSimplificationRules"": [
    {
      ""id"": ""RULE_CUSTOM_EQUATION"",
      ""name"": ""Quy tắc tùy biến động"",
      ""priority"": 200,
      ""enabled"": true,
      ""anchor"": {
        ""type"": ""GEOMETRIC_LINE"",
        ""orientation"": ""HORIZONTAL"",
        ""count"": 1,
        ""maxSlope"": 0.1
      },
      ""slots"": [
        {
          ""name"": ""numerator"",
          ""relativePosition"": ""ABOVE_ANCHOR"",
          ""maxDistanceYRatio"": 2.0,
          ""paddingX"": 20,
          ""maxTokens"": 5
        },
        {
          ""name"": ""denominator"",
          ""relativePosition"": ""BELOW_ANCHOR"",
          ""maxDistanceYRatio"": 2.0,
          ""paddingX"": 20,
          ""maxTokens"": 5
        }
      ],
      ""template"": ""CUSTOM_FRAC({{numerator}} / {{denominator}})""
    }
  ]
}";

            // Act: Nạp nóng cấu hình mới
            bool reloadSuccess = service.HotReload(customRuleJson);

            var lines = new List<(int x1, int y1, int x2, int y2)> { (10, 20, 50, 20) };
            var textItems = new List<WmfSimpleTextItem>
            {
                new() { Text = "A", X = 25, Y = 10, Width = 10, FontSize = 14 },
                new() { Text = "B", X = 25, Y = 30, Width = 10, FontSize = 14 }
            };

            bool matched = service.TryMatchStructuralRule(lines, textItems, 0, 0, 14, out var resultItems);

            try
            {
                // Assert
                Assert.True(reloadSuccess);
                Assert.True(matched);
                Assert.Single(resultItems);
                Assert.Equal("CUSTOM_FRAC(A / B)", resultItems[0].Text);
            }
            finally
            {
                // Khôi phục registry mặc định để đảm bảo tính cô lập của các test khác
                string defaultConfigPath = Path.Combine(AppContext.BaseDirectory, "Config/math_dictionary_registry.json");
                if (File.Exists(defaultConfigPath))
                {
                    service.HotReload(File.ReadAllText(defaultConfigPath));
                }
            }
        }
    }
}
