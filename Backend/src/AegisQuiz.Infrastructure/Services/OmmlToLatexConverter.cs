using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [Enterprise OpenXML OMML to KaTeX Converter v8.5]
    /// Chuyên trách chuyển đổi cây cú pháp công thức toán học OpenXML (m:oMath) sang KaTeX LaTeX:
    /// 1. Hỗ trợ toàn diện thẻ m:acc (Math Accent): Dấu vector (\vec, \overrightarrow), Dấu mũ góc (\widehat), Dấu gạch đầu (\overline), Dấu phẩy trên (prime ').
    /// 2. Hỗ trợ thẻ m:eqArr (Equation Array): Hệ phương trình xếp chồng (\begin{cases}...\end{cases}).
    /// 3. Hỗ trợ thẻ m:groupChr: Dấu ngoặc ôm \underbrace, \overbrace.
    /// 4. Từ điển ánh xạ MapMathText 60+ ký hiệu toán học phổ biến trong đề thi phổ thông.
    /// </summary>
    public static class OmmlToLatexConverter
    {
        private static readonly XNamespace M = "http://schemas.openxmlformats.org/officeDocument/2006/math";

        public static string Convert(XElement oMath)
        {
            try
            {
                var sb = new StringBuilder();
                foreach (var child in oMath.Elements())
                {
                    sb.Append(ConvertElement(child));
                }
                var latex = sb.ToString().Trim();
                return string.IsNullOrEmpty(latex) ? "" : $"${latex}$";
            }
            catch
            {
                return "";
            }
        }

        public static string ConvertContainer(XElement? container)
        {
            if (container == null) return "";
            var sb = new StringBuilder();
            foreach (var child in container.Elements())
            {
                sb.Append(ConvertElement(child));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Bọc base vào {} chỉ khi cần thiết — tránh tạo ra {-t}^{exp} thay vì -t^{exp}.
        /// Chỉ bọc khi base chứa nhiều số hạng (có +, -, khoảng trắng bên ngoài nhóm).
        /// </summary>
        private static string WrapBaseIfNeeded(string baseStr)
        {
            if (string.IsNullOrEmpty(baseStr)) return baseStr;

            // Không cần bọc nếu base là 1 ký tự đơn
            if (baseStr.Length == 1) return baseStr;

            // Không cần bọc dấu +/- trước ký tự đơn: -t, +x, -2
            if (baseStr.Length == 2 && (baseStr[0] == '-' || baseStr[0] == '+') && char.IsLetterOrDigit(baseStr[1]))
                return baseStr;

            // Không cần bọc 2 ký tự đều là chữ/số: AB, t1, xy
            if (baseStr.Length == 2 && char.IsLetterOrDigit(baseStr[0]) && char.IsLetterOrDigit(baseStr[1]))
                return baseStr;

            // Không cần bọc lệnh LaTeX khi dấu +/- chỉ nằm BÊN TRONG nhóm {}: \vec{AB}, \overrightarrow{A-B}
            if (baseStr.StartsWith("\\"))
            {
                int depth = 0;
                bool hasTopLevelOperator = false;
                bool firstBackslash = true;
                foreach (char ch in baseStr)
                {
                    if (firstBackslash && ch == '\\') { firstBackslash = false; continue; }
                    if (ch == '{') depth++;
                    else if (ch == '}') depth--;
                    else if (depth == 0 && (ch == '+' || ch == '-' || ch == ' '))
                        hasTopLevelOperator = true;
                }
                if (!hasTopLevelOperator) return baseStr;
            }

            // Không cần bọc nếu base đã được bọc bởi một cặp {} hoàn chỉnh: {expr}
            if (baseStr.StartsWith("{") && baseStr.EndsWith("}"))
            {
                int depth = 0;
                bool singleGroup = true;
                for (int i = 0; i < baseStr.Length; i++)
                {
                    if (baseStr[i] == '{') depth++;
                    else if (baseStr[i] == '}') depth--;
                    if (depth == 0 && i < baseStr.Length - 1) { singleGroup = false; break; }
                }
                if (singleGroup) return baseStr;
            }

            // Cần bọc: biểu thức phức tạp nhiều số hạng
            return $"{{{baseStr}}}";
        }

        public static string ConvertElement(XElement el)
        {
            var local = el.Name.LocalName;
            switch (local)
            {
                case "r": // math run
                    var t = el.Element(M + "t")?.Value ?? "";
                    return MapMathText(t);

                case "f": // fraction \frac{num}{den}
                    var num = ConvertContainer(el.Element(M + "num"));
                    var den = ConvertContainer(el.Element(M + "den"));
                    return $"\\frac{{{num}}}{{{den}}}";

                case "rad": // radical / square root
                    var deg = el.Element(M + "deg");
                    var degStr = deg != null && !string.IsNullOrWhiteSpace(deg.Value) ? $"[{ConvertContainer(deg)}]" : "";
                    var radBase = ConvertContainer(el.Element(M + "e"));
                    return $"\\sqrt{degStr}{{{radBase}}}";

                case "sSup": // superscript
                    var supBase = ConvertContainer(el.Element(M + "e"));
                    var sup = ConvertContainer(el.Element(M + "sup"));
                    return $"{WrapBaseIfNeeded(supBase)}^{{{sup}}}";

                case "sSub": // subscript
                    var subBase = ConvertContainer(el.Element(M + "e"));
                    var sub = ConvertContainer(el.Element(M + "sub"));
                    return $"{WrapBaseIfNeeded(subBase)}_{{{sub}}}";

                case "sSubSup": // sub & sup
                    var subSupBase = ConvertContainer(el.Element(M + "e"));
                    var sSub = ConvertContainer(el.Element(M + "sub"));
                    var sSup = ConvertContainer(el.Element(M + "sup"));
                    return $"{WrapBaseIfNeeded(subSupBase)}_{{{sSub}}}^{{{sSup}}}";

                case "d": // delimiter
                    var dPr = el.Element(M + "dPr");
                    var begChr = dPr?.Element(M + "begChr")?.Attribute(M + "val")?.Value ?? "(";
                    var endChr = dPr?.Element(M + "endChr")?.Attribute(M + "val")?.Value ?? ")";
                    var dContent = ConvertContainer(el.Element(M + "e"));

                    bool hasLines = dContent.Contains(@"\\");
                    if (begChr == "{" && (string.IsNullOrEmpty(endChr) || endChr == "."))
                    {
                        return $"\\begin{{cases}} {dContent} \\end{{cases}}";
                    }
                    if (begChr == "[" && (string.IsNullOrEmpty(endChr) || endChr == "."))
                    {
                        if (hasLines && !dContent.Contains(@"\begin{"))
                            return $"\\left[\\begin{{matrix}} {dContent} \\end{{matrix}}\\right.";
                        return $"\\left[{dContent}\\right.";
                    }

                    string rightDelim = string.IsNullOrEmpty(endChr) ? "." : endChr;
                    if (hasLines && !dContent.Contains(@"\begin{"))
                    {
                        dContent = $"\\begin{{matrix}} {dContent} \\end{{matrix}}";
                    }
                    return $"\\left{begChr}{dContent}\\right{rightDelim}";

                case "acc": // [MỚI] Accent: Vector, Dấu mũ góc, Dấu gạch trên, Prime
                    {
                        var accPr = el.Element(M + "accPr");
                        var chr = accPr?.Element(M + "chr")?.Attribute(M + "val")?.Value ?? "⃗";
                        var baseContent = ConvertContainer(el.Element(M + "e"));

                        // Tier 1: Dynamic math dictionary lookup
                        var dynamicAccent = DynamicMathDictionaryService.Instance.ResolveAccent(chr, baseContent);
                        if (!string.IsNullOrEmpty(dynamicAccent))
                        {
                            return dynamicAccent;
                        }

                        // Tier 3: Hardcoded Fail-Safe Fallback
                        return chr switch
                        {
                            "⃗" or "→" or "⟶" => baseContent.Length >= 2 ? $"\\overrightarrow{{{baseContent}}}" : $"\\vec{{{baseContent}}}",
                            "^" or "̂" => $"\\widehat{{{baseContent}}}",
                            "¯" or "_" or "‾" => $"\\overline{{{baseContent}}}",
                            "′" or "'" => $"{{{baseContent}}}'",
                            "″" or "\"" => $"{{{baseContent}}}''",
                            "~" or "̃" => $"\\tilde{{{baseContent}}}",
                            "." or "̇" => $"\\dot{{{baseContent}}}",
                            _ => baseContent.Length >= 2 ? $"\\overrightarrow{{{baseContent}}}" : $"\\vec{{{baseContent}}}"
                        };
                    }

                case "eqArr": // [MỚI] Equation Array (Hệ phương trình nhiều dòng)
                    {
                        var lines = el.Elements(M + "e").Select(ConvertContainer).ToList();
                        if (lines.Count > 1)
                        {
                            return string.Join(" \\\\ ", lines);
                        }
                        return string.Join(" ", lines);
                    }

                case "groupChr": // [MỚI] Group character (\underbrace, \overbrace)
                    {
                        var grpPr = el.Element(M + "groupChrPr");
                        var chr = grpPr?.Element(M + "chr")?.Attribute(M + "val")?.Value ?? "⏟";
                        var pos = grpPr?.Element(M + "pos")?.Attribute(M + "val")?.Value ?? "bot";
                        var eContent = ConvertContainer(el.Element(M + "e"));

                        if (chr == "⏟" || pos == "bot")
                        {
                            return $"\\underbrace{{{eContent}}}";
                        }
                        return $"\\overbrace{{{eContent}}}";
                    }

                case "nary": // integral, sum, prod
                    var naryPr = el.Element(M + "naryPr");
                    var nChr = naryPr?.Element(M + "chr")?.Attribute(M + "val")?.Value ?? "∫";
                    var nSub = ConvertContainer(el.Element(M + "sub"));
                    var nSup = ConvertContainer(el.Element(M + "sup"));
                    var nBody = ConvertContainer(el.Element(M + "e"));
                    string op = nChr switch
                    {
                        "∫" => "\\int",
                        "∑" => "\\sum",
                        "∏" => "\\prod",
                        "∬" => "\\iint",
                        "∭" => "\\iiint",
                        "∮" => "\\oint",
                        _ => nChr
                    };
                    bool hasSubNary = !string.IsNullOrEmpty(nSub);
                    bool hasSupNary = !string.IsNullOrEmpty(nSup);
                    if (op == "\\int")
                    {
                        if (hasSubNary && hasSupNary)
                            return $"{op}_{{{nSub}}}^{{{nSup}}}{{{nBody}}}";
                        if (hasSubNary)
                            return $"{op}_{{{nSub}}}{{{nBody}}}";
                        if (hasSupNary)
                            return $"{op}_{{}}^{{{nSup}}}{{{nBody}}}";
                        return $"{op}_{{}}{{{nBody}}}";
                    }

                    string limits = "";
                    if (hasSubNary) limits += $"_{{{nSub}}}";
                    if (hasSupNary) limits += $"^{{{nSup}}}";
                    return $"{op}{limits} {{{nBody}}}";

                case "func": // function
                    var fName = ConvertContainer(el.Element(M + "fName"));
                    var fArg = ConvertContainer(el.Element(M + "e"));
                    return $"{fName}{{{fArg}}}";

                case "limLow": // limit
                    var limE = ConvertContainer(el.Element(M + "e"));
                    var lim = ConvertContainer(el.Element(M + "lim"));
                    return $"\\lim_{{{lim}}} {{{limE}}}";

                case "m": // matrix
                    var rows = el.Elements(M + "mr").Select(r =>
                        string.Join(" & ", r.Elements(M + "e").Select(ConvertContainer))
                    );
                    return $"\\begin{{pmatrix}} {string.Join(" \\\\ ", rows)} \\end{{pmatrix}}";

                case "bar": // overline
                    var barE = ConvertContainer(el.Element(M + "e"));
                    return $"\\overline{{{barE}}}";

                case "oMath":
                case "oMathPara":
                case "e":
                case "num":
                case "den":
                    return ConvertContainer(el);

                default:
                    return ConvertContainer(el);
            }
        }

        public static string MapMathText(string t)
        {
            if (string.IsNullOrEmpty(t)) return t;

            // Tier 1: Dynamic math dictionary lookup
            var dynamicToken = DynamicMathDictionaryService.Instance.ResolveOmmlToken(t, string.Empty);
            if (!string.IsNullOrEmpty(dynamicToken))
            {
                return dynamicToken;
            }

            // Tier 3: Hardcoded Fail-Safe Fallback
            return t switch
            {
                "±" => "\\pm ",
                "∓" => "\\mp ",
                "×" => "\\times ",
                "÷" => "\\div ",
                "·" => "\\cdot ",
                "*" => "\\cdot ",
                "≤" => "\\le ",
                "≥" => "\\ge ",
                "≠" => "\\ne ",
                "≈" => "\\approx ",
                "≡" => "\\equiv ",
                "∼" or "~" => "\\sim ",
                "≅" => "\\cong ",
                "∈" => "\\in ",
                "∉" => "\\notin ",
                "⊂" => "\\subset ",
                "⊃" => "\\supset ",
                "⊆" => "\\subseteq ",
                "⊇" => "\\supseteq ",
                "∪" => "\\cup ",
                "∩" => "\\cap ",
                "∅" => "\\emptyset ",
                "→" or "⟶" => "\\to ",
                "←" or "⟵" => "\\leftarrow ",
                "↔" or "⟷" => "\\leftrightarrow ",
                "⇒" or "⟹" => "\\Rightarrow ",
                "⇐" or "⟸" => "\\Leftarrow ",
                "⇔" or "⟺" => "\\Leftrightarrow ",
                "⇌" => "\\rightleftharpoons ",
                "⊥" => "\\perp ",
                "∥" => "\\parallel ",
                "∠" => "\\angle ",
                "△" or "▲" => "\\triangle ",
                "°" => "^\\circ ",
                "∀" => "\\forall ",
                "∃" => "\\exists ",
                "∴" => "\\therefore ",
                "π" => "\\pi ",
                "α" => "\\alpha ",
                "β" => "\\beta ",
                "γ" => "\\gamma ",
                "θ" => "\\theta ",
                "Δ" => "\\Delta ",
                "λ" => "\\lambda ",
                "ω" => "\\omega ",
                "Ω" => "\\Omega ",
                "σ" => "\\sigma ",
                "Σ" => "\\Sigma ",
                "φ" or "ϕ" => "\\phi ",
                "Φ" => "\\Phi ",
                "μ" => "\\mu ",
                "ρ" => "\\rho ",
                "∞" => "\\infty ",
                "ℝ" => "\\mathbb{R}",
                "ℤ" => "\\mathbb{Z}",
                "ℕ" => "\\mathbb{N}",
                "ℚ" => "\\mathbb{Q}",
                "ℂ" => "\\mathbb{C}",
                "′" => "'",
                "″" => "''",
                _ => t
            };
        }
    }
}
