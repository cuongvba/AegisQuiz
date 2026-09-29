using System.Text.RegularExpressions;
using System.Xml.Linq;
using AegisQuiz.Infrastructure.Services;
using Xunit;

namespace AegisQuiz.UnitTests
{
    public class MathRefinementTests
    {
        [Fact]
        public void Verify_LatinLetters_NotMappedToGreekDelta()
        {
            // 'D' trong Times New Roman phải là 'D', không được là '\Delta'
            string res = DynamicMathDictionaryService.Instance.ResolveSymbolLatex("Times New Roman", 'D', "DEFAULT");
            Assert.Equal("DEFAULT", res);

            // 'D' trong Symbol font mới là '\Delta'
            string resSym = DynamicMathDictionaryService.Instance.ResolveSymbolLatex("Symbol", 'D', "DEFAULT");
            Assert.Contains("\\Delta", resSym);
        }

        [Fact]
        public void Verify_Prime_MappedToApostrophe_NotSlashPrime()
        {
            // Ký tự 0xA2 (Prime) phải là "'" chứ không phải "\prime" để tránh lỗi cú pháp \primeB
            string res = DynamicMathDictionaryService.Instance.ResolveSymbolLatex("Symbol", (char)0xA2, "DEFAULT");
            Assert.Equal("'", res);
        }

        [Fact]
        public void Verify_PolyhedronAndRoot_Normalization()
        {
            string raw = "Cho hình lập phương ABCD \\cdot A'B'C'D' có cạnh bằng a√3 và độ dài đường chéo là a√2";
            string normalized = DocxParserService.NormalizeMathInlineText(raw);

            // Dấu chấm phân cách khối đa diện phải là . thay vì \cdot
            Assert.Contains("ABCD.A'B'C'D'", normalized);
            Assert.DoesNotContain("\\cdot", normalized);

            // Căn bậc hai Unicode √3 và √2 phải thành $\sqrt{3}$ và $\sqrt{2}$
            Assert.Contains("$\\sqrt{3}$", normalized);
            Assert.Contains("$\\sqrt{2}$", normalized);
        }

        [Fact]
        public void Verify_Image12_TryExtractSimpleMathText()
        {
            var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "../../../image12.wmf"));
            bool ok = MathTypeWmfEngine.TryExtractSimpleMathText(bytes, out var math);
            Assert.True(ok, $"Expected extraction to succeed, but returned false. math='{math}'");
            Assert.Contains("\\sin", math);
            Assert.Contains("\\frac{1}{2}", math);
        }

        /// <summary>
        /// Kiểm tra OMML sSup không bọc {} thừa quanh base ký tự đơn.
        /// Ví dụ: t^2 không được thành {t}^{2}
        /// </summary>
        [Fact]
        public void OmmlToLatex_sSup_NoBraceOverwrap_SingleChar()
        {
            XNamespace m = "http://schemas.openxmlformats.org/officeDocument/2006/math";
            var sSup = new XElement(m + "sSup",
                new XElement(m + "e", new XElement(m + "r", new XElement(m + "t", "t"))),
                new XElement(m + "sup", new XElement(m + "r", new XElement(m + "t", "2")))
            );
            string result = OmmlToLatexConverter.ConvertElement(sSup);
            // Phải là t^{2} không phải {t}^{2}
            Assert.Equal("t^{2}", result);
            Assert.DoesNotContain("{t}", result);
        }

        /// <summary>
        /// Kiểm tra OMML sSup với base là biểu thức nhiều số hạng — phải bọc {}
        /// </summary>
        [Fact]
        public void OmmlToLatex_sSup_MultiTermBase_WrapsBrace()
        {
            XNamespace m = "http://schemas.openxmlformats.org/officeDocument/2006/math";
            var sSup = new XElement(m + "sSup",
                new XElement(m + "e",
                    new XElement(m + "r", new XElement(m + "t", "a")),
                    new XElement(m + "r", new XElement(m + "t", "+")),
                    new XElement(m + "r", new XElement(m + "t", "b"))
                ),
                new XElement(m + "sup", new XElement(m + "r", new XElement(m + "t", "2")))
            );
            string result = OmmlToLatexConverter.ConvertElement(sSup);
            // (a+b)^{2} phải được bọc: {a+b}^{2}
            Assert.Contains("{a+b}^{2}", result);
        }
    }
}
