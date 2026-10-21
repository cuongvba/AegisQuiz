using System;
using System.Text.RegularExpressions;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Services.Docx
{
    /// <summary>
    /// [Aegis UPMP v2.0 - Universal Polyglot Math Normalizer]
    /// Hiện thực hóa chuẩn hóa toán học đa ngôn ngữ, đơn vị khoa học và ký hiệu SGK.
    /// Tuân thủ Clean Architecture (Infrastructure implementation) và OCP.
    /// </summary>
    public class MathLocaleNormalizer : IMathLocaleNormalizer
    {
        public static MathLocaleNormalizer Default { get; } = new MathLocaleNormalizer();

        // ── 1. Regex cho dấu phẩy số thập phân tiếng Việt / Pháp (VD: 3,14 -> 3{,}14) ──
        private static readonly Regex DecimalCommaRegex = new(
            @"(?<=\d),(?=\d)",
            RegexOptions.Compiled);

        // ── 2. Regex cho hàm lượng giác & giải tích đa ngôn ngữ ───────────────────────
        private static readonly Regex LocalTrigRegex = new(
            @"(?<![a-zA-Z\\])(?:\\operatorname\{)?(tg|cotg|arctg|arcctg|sh|ch|th)(?:\})?\s*(?=[\(a-zA-Z0-9\\])",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // ── 3. Regex đơn vị đo lường khoa học đứng sau số / biểu thức ─────────────────
        // VD: 12 dm^3, 20 cm, 9.8 m/s^2, 100 kWh
        private static readonly Regex ScientificUnitsRegex = new(
            @"(?<=(?:\d|\}|[\)\]]))\s*(?<!\\text\{)(?<![a-zA-Z\\])(dm\^3|cm\^3|mm\^3|m\^3|dm\^2|cm\^2|mm\^2|m\^2|dm|cm|mm|km|m\/s\^2|m\/s|km\/h|kWh|kg|gam|mg|mol|rad|Pa|Hz|lít|lit)(?![a-zA-Z0-9])",
            RegexOptions.Compiled);

        // ── 4. Regex chuẩn hóa Vector hai điểm & góc SGK Việt Nam ──────────────────────
        private static readonly Regex VectorSegmentRegex = new(
            @"\\vec\{([A-Z]{2,3})\}",
            RegexOptions.Compiled);

        public string Normalize(string latex, string locale = "vi")
        {
            if (string.IsNullOrWhiteSpace(latex)) return string.Empty;

            string res = latex;

            // 1. Chuẩn hóa dấu phẩy số thập phân (áp dụng cho locale "vi", "fr"...)
            if (string.Equals(locale, "vi", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(locale, "fr", StringComparison.OrdinalIgnoreCase))
            {
                res = NormalizeDecimalCommas(res, locale);
            }

            // 2. Chuẩn hóa hàm lượng giác & hàm đặc thù quốc gia
            res = NormalizeTrigonometryAndFunctions(res, locale);

            // 3. Chuẩn hóa đơn vị đo lường khoa học
            res = NormalizeScientificUnits(res, locale);

            // 4. Chuẩn hóa Vector hình học (đoạn thẳng có hướng AB -> \overrightarrow{AB})
            res = VectorSegmentRegex.Replace(res, @"\overrightarrow{$1}");

            return res;
        }

        public string NormalizeDecimalCommas(string latex, string locale = "vi")
        {
            if (string.IsNullOrEmpty(latex) || !latex.Contains(',')) return latex;
            return DecimalCommaRegex.Replace(latex, "{,}");
        }

        public string NormalizeTrigonometryAndFunctions(string latex, string locale = "vi")
        {
            if (string.IsNullOrEmpty(latex)) return latex;

            return LocalTrigRegex.Replace(latex, m =>
            {
                string fn = m.Groups[1].Value.ToLowerInvariant();
                return fn switch
                {
                    "tg"     => @"\tan ",
                    "cotg"   => @"\cot ",
                    "arctg"  => @"\arctan ",
                    "arcctg" => @"\arccot ",
                    "sh"     => @"\sinh ",
                    "ch"     => @"\cosh ",
                    "th"     => @"\tanh ",
                    _        => m.Value
                };
            });
        }

        public string NormalizeScientificUnits(string latex, string locale = "vi")
        {
            if (string.IsNullOrEmpty(latex)) return latex;

            return ScientificUnitsRegex.Replace(latex, m =>
            {
                string unit = m.Groups[1].Value;
                if (unit.Contains('^'))
                {
                    // Tách phần base và exponent (vd: dm^3 -> \text{ dm}^3)
                    int caret = unit.IndexOf('^');
                    string baseUnit = unit.Substring(0, caret);
                    string exp = unit.Substring(caret + 1);
                    return $@" \text{{ {baseUnit}}}^{{{exp}}}";
                }
                return $@" \text{{ {unit}}}";
            });
        }
    }
}
