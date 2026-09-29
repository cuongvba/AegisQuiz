using System;
using System.IO;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Infrastructure.Services;
using Xunit;

namespace AegisQuiz.UnitTests
{
    public class DynamicMathDictionaryTests
    {
        [Fact]
        public void DynamicMathDictionary_LoadsInitialRegistry_Successfully()
        {
            // Arrange
            var service = new DynamicMathDictionaryService();

            // Act
            var registry = service.GetCurrentRegistry();

            // Assert
            Assert.NotNull(registry);
            Assert.NotEmpty(registry.SymbolGlyphs);
            Assert.NotEmpty(registry.GreekLetters);
            Assert.NotEmpty(registry.OmmlTokens);
            Assert.NotEmpty(registry.AccentMappings);
        }

        [Fact]
        public void ResolveSymbolGlyph_ResolvesKnownGlyphs()
        {
            // Arrange
            var service = new DynamicMathDictionaryService();

            // Act: 0x00A2 is Prime ′
            string prime = service.ResolveSymbolGlyph("Symbol", '\u00A2', "fallback");
            // Act: 0x00F2 is Integral ∫
            string integral = service.ResolveSymbolGlyph("Symbol", '\u00F2', "fallback");
            // Act: Unknown glyph should return fallback
            string unknown = service.ResolveSymbolGlyph("Symbol", '\uFFFF', "fallback");

            // Assert
            Assert.Equal("&#x2032;", prime);
            Assert.Equal("&#x222B;", integral);
            Assert.Equal("fallback", unknown);
        }

        [Fact]
        public void ResolveGreekLetter_ResolvesAlphabet()
        {
            // Arrange
            var service = new DynamicMathDictionaryService();

            // Act
            string alpha = service.ResolveGreekLetter("Symbol", 'a', "fallback");
            string pi = service.ResolveGreekLetter("Symbol", 'p', "fallback");
            string omega = service.ResolveGreekLetter("Symbol", 'w', "fallback");

            // Assert
            Assert.Equal("&#x03B1;", alpha);
            Assert.Equal("&#x03C0;", pi);
            Assert.Equal("&#x03C9;", omega);
        }

        [Fact]
        public void ResolveOmmlToken_ResolvesLatexTokens()
        {
            // Arrange
            var service = new DynamicMathDictionaryService();

            // Act
            string plusMinus = service.ResolveOmmlToken("±", "fallback");
            string subset = service.ResolveOmmlToken("⊂", "fallback");
            string forall = service.ResolveOmmlToken("∀", "fallback");
            string unknown = service.ResolveOmmlToken("UNKNOWN_TOKEN", "fallback");

            // Assert
            Assert.Equal("\\pm ", plusMinus);
            Assert.Equal("\\subset ", subset);
            Assert.Equal("\\forall ", forall);
            Assert.Equal("fallback", unknown);
        }

        [Fact]
        public void ResolveAccent_FormatsVectorsAndAccentsCorrectly()
        {
            // Arrange
            var service = new DynamicMathDictionaryService();

            // Act: Vector 2 chars
            string vecShort = service.ResolveAccent("⃗", "AB");
            // Act: Vector > 2 chars
            string vecLong = service.ResolveAccent("⃗", "ABC");
            // Act: Hat ^
            string hat = service.ResolveAccent("^", "A");
            // Act: Prime '
            string prime = service.ResolveAccent("′", "y");

            // Assert
            Assert.Equal("\\vec{AB}", vecShort);
            Assert.Equal("\\overrightarrow{ABC}", vecLong);
            Assert.Equal("\\widehat{A}", hat);
            Assert.Equal("{y}'", prime);
        }

        [Fact]
        public void NormalizeInlineText_ReplacesQuotesAndUnicodeVectors()
        {
            // Arrange
            var service = new DynamicMathDictionaryService();
            string raw = "Tam giác A’B’C’ có vector AB\u20D7 và a\u20D7";

            // Act
            string normalized = service.NormalizeInlineText(raw);

            // Assert
            Assert.Contains("A'B'C'", normalized);
            Assert.Contains("$\\overrightarrow{AB}$", normalized);
            Assert.Contains("$\\vec{a}$", normalized);
        }

        [Fact]
        public void HotReload_AppliesNewJsonChanges_ZeroDowntime()
        {
            // Arrange
            var service = new DynamicMathDictionaryService();
            string customJson = @"{
                ""version"": ""2026.TEST"",
                ""symbolGlyphs"": [
                    { ""font"": ""Symbol"", ""charHex"": ""0x1234"", ""replacement"": ""&#x1234;"", ""latex"": ""\\custom"" }
                ],
                ""ommlTokens"": {
                    ""CUSTOM_OP"": ""\\customOp ""
                },
                ""accentMappings"": {},
                ""inlineNormalizations"": []
            }";

            // Act
            bool reloaded = service.HotReload(customJson);
            string resolvedGlyph = service.ResolveSymbolGlyph("Symbol", (char)0x1234, "fallback");
            string resolvedToken = service.ResolveOmmlToken("CUSTOM_OP", "fallback");
            var currentRegistry = service.GetCurrentRegistry();

            // Assert
            Assert.True(reloaded);
            Assert.Equal("&#x1234;", resolvedGlyph);
            Assert.Equal("\\customOp ", resolvedToken);
            Assert.Equal("2026.TEST", currentRegistry.Version);
        }

        [Fact]
        public void UpsertSymbolAndToken_UpdatesOnTheFly()
        {
            // Arrange
            var service = new DynamicMathDictionaryService();

            // Act
            service.UpsertOmmlToken("NEW_SYM", "\\newsymbol ");
            service.UpsertSymbolGlyph(new SymbolGlyphEntryDto
            {
                Font = "Symbol",
                CharHex = "0x9999",
                Replacement = "&#x9999;",
                Latex = "\\newglyph"
            });

            // Assert
            Assert.Equal("\\newsymbol ", service.ResolveOmmlToken("NEW_SYM", ""));
            Assert.Equal("&#x9999;", service.ResolveSymbolGlyph("Symbol", (char)0x9999, ""));
        }
    }
}
