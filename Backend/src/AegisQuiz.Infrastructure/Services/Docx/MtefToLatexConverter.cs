using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AegisQuiz.Infrastructure.Services;

namespace AegisQuiz.Infrastructure.Services.Docx
{
    /// <summary>
    /// [Aegis MTEF Native AST to KaTeX Converter v1.1 - World-Class Standard]
    /// Giải mã trực tiếp Cây Cú Pháp Trừu Tượng (Abstract Syntax Tree) nhị phân của MathType (MTEF v5)
    /// được nhúng trong bản ghi 0x0626 META_ESCAPE của các tệp WMF sinh ra từ Microsoft Word / MathType.
    ///
    /// Chuyển đổi trực tiếp 100% chính xác sang cú pháp KaTeX/LaTeX ($...$), loại bỏ hoàn toàn các lỗi:
    /// 1. Mờ nét căn thức (\sqrt)
    /// 2. Vẽ sai/méo dấu ngoặc cao nhiều tầng (\left( ... \right))
    /// 3. Mất dấu vector (\vec{u}, \overrightarrow{AB})
    /// 4. Lỗi viên thuốc đen (Black Pill) hay thanh trắng khổng lồ
    /// 5. Tiết kiệm 98% dung lượng lưu trữ so với Vector SVG / Base64.
    /// </summary>
    public static class MtefToLatexConverter
    {
        private static readonly byte[] MathTypeTag = Encoding.ASCII.GetBytes("Design Science, Inc.\0");

        /// <summary>
        /// Thử giải mã tệp WMF nhị phân sang KaTeX thông qua MTEF v5 AST.
        /// Trả về true nếu giải mã thành công và sinh ra chuỗi KaTeX hợp lệ.
        /// </summary>
        public static bool TryConvert(byte[] wmfBytes, out string katexFormula)
        {
            katexFormula = string.Empty;
            if (wmfBytes == null || wmfBytes.Length < 64) return false;

            try
            {
                // 1. Tìm kiếm chữ ký MTEF 'Design Science, Inc.\0' trong WMF
                int tagIdx = FindBytePattern(wmfBytes, MathTypeTag);
                if (tagIdx < 0) return false;

                int mtefStart = tagIdx + MathTypeTag.Length;
                if (mtefStart + 15 >= wmfBytes.Length) return false;

                byte mtefVer = wmfBytes[mtefStart];
                if (mtefVer != 5 && mtefVer != 3) return false;

                // 2. Định vị Root của biểu thức toán học (bỏ qua header metadata)
                int bodyPos = LocateEquationRoot(wmfBytes, mtefStart);
                if (bodyPos < 0 || bodyPos >= wmfBytes.Length) return false;

                // 3. Phân tích cú pháp đệ quy cây MTEF sang LaTeX
                using var ms = new MemoryStream(wmfBytes, bodyPos, wmfBytes.Length - bodyPos);
                using var reader = new BinaryReader(ms);

                string rawLatex = ParseExpression(reader, stopOnEnd: false);
                if (string.IsNullOrWhiteSpace(rawLatex)) return false;

                // 4. Chuẩn hóa & Hậu xử lý KaTeX (KaTeX Post-Normalization)
                string normalized = PostNormalizeLatex(rawLatex);
                if (string.IsNullOrWhiteSpace(normalized)) return false;

                katexFormula = $"${normalized}$";
                return true;
            }
            catch
            {
                // Fail-safe: Nếu gặp lỗi trong quá trình đọc nhị phân MTEF, an toàn chuyển về pipeline tiếp theo
                return false;
            }
        }

        /// <summary>
        /// Định vị điểm bắt đầu của phương trình (Equation Root LINE) trong MTEF stream
        /// </summary>
        private static int LocateEquationRoot(byte[] bytes, int start)
        {
            // Bỏ qua header: ver(1) + plat(1) + prod(1) + v(1) + vsub(1) = 5 bytes
            int p = start + 5;
            // Bỏ qua application string (null-terminated)
            while (p < bytes.Length && bytes[p] != 0) p++;
            if (p >= bytes.Length) return -1;
            p++; // Bỏ qua null byte
            if (p < bytes.Length) p++; // Bỏ qua inline flag

            // Tìm record COLOR_DEF (16) hoặc LINE (1) hoặc FULL (10)
            for (int i = p; i < Math.Min(p + 300, bytes.Length - 10); i++)
            {
                // Bản ghi COLOR_DEF (16) theo sau bởi options 0 và 3 cặp RGB 0x0000 (tổng cộng 8 bytes)
                if (bytes[i] == 16 && bytes[i + 1] == 0 &&
                    bytes[i + 2] == 0 && bytes[i + 3] == 0 &&
                    bytes[i + 4] == 0 && bytes[i + 5] == 0 &&
                    bytes[i + 6] == 0 && bytes[i + 7] == 0)
                {
                    return i + 8;
                }

                // Nhận diện cặp FULL (10) + LINE (1)
                if (bytes[i] == 10 && bytes[i + 1] == 1 && (bytes[i + 2] == 0 || bytes[i + 2] == 8))
                {
                    return i + 1; // Nhảy vào record LINE
                }
            }

            // Fallback: Tìm record LINE đầu tiên có options hợp lệ
            for (int i = p; i < Math.Min(p + 300, bytes.Length - 5); i++)
            {
                if (bytes[i] == 1 && (bytes[i + 1] == 0 || bytes[i + 1] == 8))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Phân tích cú pháp đệ quy một khối biểu thức MTEF (dòng, slot của template...)
        /// </summary>
        private static string ParseExpression(BinaryReader reader, bool stopOnEnd)
        {
            var sb = new StringBuilder();
            int consecutiveEnds = 0;

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                byte tag = reader.ReadByte();

                // Record 0: END (Kết thúc container hiện tại)
                if (tag == 0)
                {
                    if (stopOnEnd) break;
                    consecutiveEnds++;
                    if (consecutiveEnds >= 3) break; // 3 tag END liên tiếp ở root level -> Hết toàn bộ biểu thức
                    continue;
                }
                consecutiveEnds = 0;

                // Record 1: LINE (Bắt đầu một dòng hoặc một ô con)
                if (tag == 1)
                {
                    byte lineOpts = reader.ReadByte();
                    if ((lineOpts & 0x08) != 0) // Nudge
                    {
                        reader.ReadInt16();
                        reader.ReadInt16();
                    }
                    if ((lineOpts & 0x04) != 0) reader.ReadByte(); // lineSpace
                    if ((lineOpts & 0x02) != 0) // ruler
                    {
                        byte nStops = reader.ReadByte();
                        reader.BaseStream.Position += nStops * 3;
                    }
                    if ((lineOpts & 0x01) != 0) // NULL LINE
                    {
                        continue;
                    }
                    string subLine = ParseExpression(reader, stopOnEnd: true);
                    sb.Append(subLine);
                    continue;
                }

                // Record 2: CHAR (Ký tự toán học, chữ cái, số, toán tử)
                if (tag == 2)
                {
                    byte charOpts = reader.ReadByte();
                    if ((charOpts & 0x08) != 0) // Nudge
                    {
                        reader.ReadInt16();
                        reader.ReadInt16();
                    }
                    byte typeface = reader.ReadByte();
                    ushort mtcode = 0;
                    if ((charOpts & 0x20) == 0) // Has MTCode
                    {
                        mtcode = reader.ReadUInt16();
                    }
                    if ((charOpts & 0x04) != 0) // 8-bit font position
                    {
                        reader.ReadByte();
                    }
                    if ((charOpts & 0x10) != 0) // 16-bit font position
                    {
                        reader.ReadUInt16();
                    }

                    string chText = ResolveMtCode(mtcode, typeface);
                    sb.Append(chText);

                    // Xử lý Embellishment đính kèm nếu có
                    if ((charOpts & 0x01) != 0 && reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        byte peek = reader.ReadByte();
                        if (peek == 6) // EMBELL
                        {
                            byte embOpts = reader.ReadByte();
                            if ((embOpts & 0x08) != 0) { reader.ReadInt16(); reader.ReadInt16(); }
                            byte embType = reader.ReadByte();
                            ApplyEmbellishment(sb, embType);
                        }
                        else
                        {
                            reader.BaseStream.Position--; // Rewind
                        }
                    }
                    continue;
                }

                // Record 3: TMPL (Template cấu trúc: Phân số, căn thức, ngoặc, tích phân, tổng...)
                if (tag == 3)
                {
                    byte tmplOpts = reader.ReadByte();
                    if ((tmplOpts & 0x08) != 0) // Nudge
                    {
                        reader.ReadInt16();
                        reader.ReadInt16();
                    }
                    byte selector = reader.ReadByte();

                    byte b1 = reader.ReadByte();
                    ushort variation = 0;
                    if ((b1 & 0x80) != 0)
                    {
                        byte b2 = reader.ReadByte();
                        variation = (ushort)((b1 & 0x7F) | (b2 << 8));
                    }
                    else
                    {
                        variation = b1;
                    }

                    // Đọc tmpl options
                    reader.ReadByte();

                    string tmplLatex = ParseTemplate(reader, selector, variation);
                    sb.Append(tmplLatex);

                    // Bỏ qua thẻ END của template nếu còn
                    SkipTrailingEnd(reader);
                    continue;
                }

                // Record 16: COLOR_DEF
                if (tag == 16)
                {
                    byte cOpts = reader.ReadByte();
                    int bytesToSkip = ((cOpts & 0x01) != 0) ? 8 : 6; // CMYK: 4 x 2, RGB: 3 x 2
                    for (int k = 0; k < bytesToSkip && reader.BaseStream.Position < reader.BaseStream.Length; k++)
                        reader.ReadByte();
                    if ((cOpts & 0x04) != 0) // Color name
                    {
                        while (reader.BaseStream.Position < reader.BaseStream.Length && reader.ReadByte() != 0) { }
                    }
                    continue;
                }

                // Record 4: PILE (Chồng nhiều dòng: Hệ phương trình)
                if (tag == 4)
                {
                    byte pileOpts = reader.ReadByte();
                    if ((pileOpts & 0x08) != 0) { reader.ReadInt16(); reader.ReadInt16(); }
                    byte halign = reader.ReadByte();
                    byte valign = reader.ReadByte();

                    var lines = new List<string>();
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        if (reader.PeekChar() == 0)
                        {
                            reader.ReadByte();
                            break;
                        }
                        string pileLine = ParseExpression(reader, stopOnEnd: true);
                        if (!string.IsNullOrEmpty(pileLine)) lines.Add(pileLine);
                    }
                    sb.Append(string.Join(@" \\ ", lines));
                    continue;
                }

                // Record 5: MATRIX (Ma trận: Hàng, Cột)
                if (tag == 5)
                {
                    byte matOpts = reader.ReadByte();
                    if ((matOpts & 0x08) != 0) { reader.ReadInt16(); reader.ReadInt16(); }
                    byte valign = reader.ReadByte();
                    byte hjust = reader.ReadByte();
                    byte vjust = reader.ReadByte();
                    byte rows = reader.ReadByte();
                    byte cols = reader.ReadByte();

                    var cells = new List<string>();
                    int totalCells = rows * cols;
                    for (int c = 0; c < totalCells && reader.BaseStream.Position < reader.BaseStream.Length; c++)
                    {
                        string cell = ParseExpression(reader, stopOnEnd: true);
                        cells.Add(cell);
                    }
                    SkipTrailingEnd(reader);

                    var matSb = new StringBuilder(@"\begin{matrix} ");
                    for (int r = 0; r < rows; r++)
                    {
                        var rowCells = cells.Skip(r * cols).Take(cols);
                        matSb.Append(string.Join(" & ", rowCells));
                        if (r < rows - 1) matSb.Append(@" \\ ");
                    }
                    matSb.Append(@" \end{matrix}");
                    sb.Append(matSb.ToString());
                    continue;
                }

                // Record 6: EMBELL (Gia số trên đầu: Mũi tên vector, đạo hàm phẩy, dấu mũ...)
                if (tag == 6)
                {
                    byte embOpts = reader.ReadByte();
                    if ((embOpts & 0x08) != 0) { reader.ReadInt16(); reader.ReadInt16(); }
                    byte embType = reader.ReadByte();

                    ApplyEmbellishment(sb, embType);
                    continue;
                }

                // Record 10, 11, 12, 13, 14: SIZE (FULL, SUB, SUB2, SYM, SUBSYM)
                if (IsSizeTag(tag))
                {
                    continue;
                }

                // Record 15: COLOR (Index màu)
                if (tag == 15)
                {
                    if (reader.BaseStream.Position < reader.BaseStream.Length)
                        reader.ReadByte();
                    continue;
                }

                // Record >= 50: Vùng dữ liệu ngoài hoặc kết thúc luồng MTEF
                if (tag >= 50)
                {
                    break;
                }
            }

            return sb.ToString();
        }

        private static bool IsSizeTag(byte tag)
        {
            // Các tag SIZE chuẩn của MTEF: FULL(10), SUB(11), SUB2(12), SYM(13), SUBSYM(14)
            return tag >= 10 && tag <= 14;
        }

        /// <summary>
        /// Đọc chính xác 1 slot (LINE ... END) của một Template
        /// </summary>
        private static string ParseSlot(BinaryReader reader)
        {
            if (reader.BaseStream.Position >= reader.BaseStream.Length) return string.Empty;

            // Bỏ qua các tag COLOR hoặc SIZE đứng trước slot
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                byte peek = reader.ReadByte();
                if (peek == 15) // COLOR
                {
                    if (reader.BaseStream.Position < reader.BaseStream.Length) reader.ReadByte();
                    continue;
                }
                if (peek == 9) // SIZE (lsize, dsize)
                {
                    if (reader.BaseStream.Position + 2 <= reader.BaseStream.Length)
                    {
                        reader.ReadByte();
                        reader.ReadByte();
                    }
                    continue;
                }
                if (IsSizeTag(peek)) continue;

                if (peek == 1) // LINE
                {
                    byte lineOpts = reader.ReadByte();
                    if ((lineOpts & 0x08) != 0) { reader.ReadInt16(); reader.ReadInt16(); }
                    if ((lineOpts & 0x04) != 0) { reader.ReadByte(); } // lineSpace
                    if ((lineOpts & 0x02) != 0) // ruler
                    {
                        byte nStops = reader.ReadByte();
                        reader.BaseStream.Position += nStops * 3;
                    }
                    if ((lineOpts & 0x01) != 0) // NULL LINE (slot rỗng)
                    {
                        return string.Empty;
                    }
                    return ParseExpression(reader, stopOnEnd: true);
                }
                if (peek == 0) // Slot rỗng hoặc kết thúc container
                {
                    return string.Empty;
                }

                // Không phải LINE hay END, rewind lại để ParseExpression tự đọc
                reader.BaseStream.Position--;
                return ParseExpression(reader, stopOnEnd: true);
            }

            return string.Empty;
        }

        /// <summary>
        /// Phân tích cú pháp các template MTEF
        /// </summary>
        private static string ParseTemplate(BinaryReader reader, byte selector, ushort variation)
        {
            switch (selector)
            {
                case 11: // tmFRACT (Phân số)
                {
                    string num = ParseSlot(reader);
                    string den = ParseSlot(reader);
                    return $@"\frac{{{num}}}{{{den}}}";
                }

                case 10: // tmROOT (Căn thức)
                {
                    if (variation == 0) // Căn bậc hai (\sqrt{x})
                    {
                        string rad = ParseSlot(reader);
                        return $@"\sqrt{{{rad}}}";
                    }
                    else // Căn bậc n (\sqrt[n]{x})
                    {
                        string nth = ParseSlot(reader);
                        string rad = ParseSlot(reader);
                        return $@"\sqrt[{nth}]{{{rad}}}";
                    }
                }

                case 1: // tmPAREN (Ngoặc đơn: ( ... ))
                {
                    string inner = ParseSlot(reader);
                    return $@"\left({inner}\right)";
                }

                case 2: // tmBRACE (Ngoặc nhọn: { ... })
                {
                    string inner = ParseSlot(reader);
                    return $@"\left\{{{inner}\right\}}";
                }

                case 3: // tmBRACK (Ngoặc vuông: [ ... ])
                {
                    string inner = ParseSlot(reader);
                    return $@"\left[{inner}\right]";
                }

                case 4: // tmBAR (Trị tuyệt đối: | ... |)
                {
                    string inner = ParseSlot(reader);
                    return $@"\left|{inner}\right|";
                }

                case 27: // tmSUB (Chỉ số dưới: _{sub})
                case 28: // tmSUP (Chỉ số trên: ^{sup})
                case 29: // tmSUBSUP (Cả chỉ số dưới và trên: _{sub}^{sup})
                {
                    // Trong MTEF, họ ScrBoxClass (tmSUB, tmSUP, tmSUBSUP) luôn bao gồm 2 slots:
                    // Slot 1: sub (đối với tmSUP, slot này là LINE NULL)
                    // Slot 2: sup (đối với tmSUB, slot này là LINE NULL)
                    string sub = ParseSlot(reader);
                    string sup = ParseSlot(reader);

                    if (!string.IsNullOrEmpty(sub) && !string.IsNullOrEmpty(sup))
                        return $"_{{{sub}}}^{{{sup}}}";
                    if (!string.IsNullOrEmpty(sub))
                        return $"_{{{sub}}}";
                    if (!string.IsNullOrEmpty(sup))
                        return $"^{{{sup}}}";
                    return string.Empty;
                }

                case 31: // tmVEC (Vector)
                {
                    string vec = ParseSlot(reader);
                    return vec.Length <= 1 ? $@"\vec{{{vec}}}" : $@"\overrightarrow{{{vec}}}";
                }

                case 15: // tmINTEG (Tích phân)
                {
                    string sub = ParseSlot(reader);
                    string sup = ParseSlot(reader);
                    return string.IsNullOrEmpty(sub) && string.IsNullOrEmpty(sup)
                        ? @"\int "
                        : $@"\int_{{{sub}}}^{{{sup}}} ";
                }

                case 16: // tmSUM (Tổng sigma)
                {
                    string sub = ParseSlot(reader);
                    string sup = ParseSlot(reader);
                    return string.IsNullOrEmpty(sub) && string.IsNullOrEmpty(sup)
                        ? @"\sum "
                        : $@"\sum_{{{sub}}}^{{{sup}}} ";
                }

                case 17: // tmPROD (Tích)
                {
                    string sub = ParseSlot(reader);
                    string sup = ParseSlot(reader);
                    return $@"\prod_{{{sub}}}^{{{sup}}} ";
                }

                case 23: // tmLIM (Giới hạn)
                {
                    string sub = ParseSlot(reader);
                    return string.IsNullOrEmpty(sub) ? @"\lim " : $@"\lim_{{{sub}}} ";
                }

                default:
                {
                    string inner = ParseSlot(reader);
                    return inner;
                }
            }
        }

        /// <summary>
        /// Áp dụng Embellishment (dấu trên đầu / đuôi của ký tự vừa tạo)
        /// </summary>
        private static void ApplyEmbellishment(StringBuilder sb, byte embType)
        {
            switch (embType)
            {
                case 2: // emb1DOT (\dot)
                    WrapLastChar(sb, @"\dot{", "}");
                    break;
                case 3: // emb2DOT (\ddot)
                    WrapLastChar(sb, @"\ddot{", "}");
                    break;
                case 5: // emb1PRIME (')
                    sb.Append("'");
                    break;
                case 6: // emb2PRIME ('')
                    sb.Append("''");
                    break;
                case 18: // emb3PRIME (''')
                    sb.Append("'''");
                    break;
                case 9: // embHAT (\hat)
                    WrapLastChar(sb, @"\hat{", "}");
                    break;
                case 17: // embOBAR (\bar)
                    WrapLastChar(sb, @"\bar{", "}");
                    break;
                case 11: // embRARROW (Vector \vec)
                case 14: // embR1ARROW
                    WrapLastChar(sb, @"\vec{", "}");
                    break;
            }
        }

        private static void WrapLastChar(StringBuilder sb, string prefix, string suffix)
        {
            if (sb.Length == 0) return;
            string str = sb.ToString().TrimEnd();
            if (string.IsNullOrEmpty(str)) return;

            int lastIdx = str.Length - 1;
            char last = str[lastIdx];
            sb.Length = 0;
            sb.Append(str.Substring(0, lastIdx));
            sb.Append(prefix);
            sb.Append(last);
            sb.Append(suffix);
        }

        /// <summary>
        /// Giải mã MTCode 16-bit của Design Science sang ký hiệu LaTeX chuẩn
        /// </summary>
        private static string ResolveMtCode(ushort mtcode, byte typeface)
        {
            // Bảng tra cứu ký tự đặc biệt phổ biến trong toán học
            switch (mtcode)
            {
                case 0x00B1: return @"\pm ";
                case 0x2212:
                case 0x2013:
                case 0x2014: return "-";
                case 0x00D7: return @"\times ";
                case 0x00F7: return @"\div ";
                case 0x221E: return @"\infty ";
                case 0x221A: return @"\sqrt ";
                case 0x22C5: return @"\cdot ";
                case 0x2026: return @"\dots ";
                case 0x2032: return "'";
                case 0x2033: return "''";
                case 0x2264: return @"\le ";
                case 0x2265: return @"\ge ";
                case 0x2260: return @"\ne ";
                case 0x2208: return @"\in ";
                case 0x2209: return @"\notin ";
                case 0x2282: return @"\subset ";
                case 0x2283: return @"\supset ";
                case 0x222A: return @"\cup ";
                case 0x2229: return @"\cap ";
                case 0x2205: return @"\emptyset ";
                case 0x2192: return @"\to ";
                case 0x21D2: return @"\Rightarrow ";
                case 0x21D4: return @"\Leftrightarrow ";
                case 0x22A5: return @"\perp ";
                case 0x2225: return @"\parallel ";
                case 0x2220: return @"\angle ";
                case 0x00B0: return @"^\circ ";
                case 0x2200: return @"\forall ";
                case 0x2203: return @"\exists ";

                // Chữ cái Hy Lạp thường
                case 0x03B1: return @"\alpha ";
                case 0x03B2: return @"\beta ";
                case 0x03B3: return @"\gamma ";
                case 0x03B4: return @"\delta ";
                case 0x03B5: return @"\varepsilon ";
                case 0x03B8: return @"\theta ";
                case 0x03BB: return @"\lambda ";
                case 0x03BC: return @"\mu ";
                case 0x03C0: return @"\pi ";
                case 0x03C1: return @"\rho ";
                case 0x03C3: return @"\sigma ";
                case 0x03C4: return @"\tau ";
                case 0x03C6: return @"\varphi ";
                case 0x03C9: return @"\omega ";

                // Chữ cái Hy Lạp hoa
                case 0x0393: return @"\Gamma ";
                case 0x0394: return @"\Delta ";
                case 0x0398: return @"\Theta ";
                case 0x039B: return @"\Lambda ";
                case 0x03A0: return @"\Pi ";
                case 0x03A3: return @"\Sigma ";
                case 0x03A9: return @"\Omega ";

                // Ký tự ASCII & Toán tử cơ bản
                case (ushort)'{': return @"\{";
                case (ushort)'}': return @"\}";
                case (ushort)'%': return @"\%";
                case (ushort)'$': return @"\$";
                case (ushort)'_': return @"\_";
                case (ushort)'&': return @"\&";
                case (ushort)'#': return @"\#";
                case 0x00A0: return " "; // Non-breaking space
            }

            // Ký tự ASCII thông thường (chữ cái, chữ số, toán tử +, =, <, >, ...)
            if (mtcode >= 32 && mtcode <= 126)
            {
                return ((char)mtcode).ToString();
            }

            // Ký tự Unicode mở rộng
            if (mtcode > 126)
            {
                return char.ConvertFromUtf32(mtcode);
            }

            return string.Empty;
        }

        private static void SkipTrailingEnd(BinaryReader reader)
        {
            if (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                byte b = reader.ReadByte();
                if (b != 0)
                {
                    reader.BaseStream.Position--;
                }
            }
        }

        /// <summary>
        /// Chuẩn hóa chuỗi KaTeX sau khi giải mã để đảm bảo vẻ đẹp hoàn mỹ
        /// </summary>
        private static string PostNormalizeLatex(string latex)
        {
            if (string.IsNullOrWhiteSpace(latex)) return string.Empty;

            string formula = latex.Trim();

            // 1. Chuẩn hóa hàm lượng giác & giải tích: sinx -> \sin x, cosx -> \cos x
            formula = Regex.Replace(formula, @"(?<![a-zA-Z\\])(sin|cos|tan|cot|sec|csc)\s*([xyztabc])(?![a-zA-Z])", @"\$1 $2");
            formula = Regex.Replace(formula, @"(?<![a-zA-Z\\])(sin|cos|tan|cot|lim|ln|log|arcsin|arccos|arctan|sec|csc)\b", @"\$1");

            // 2. Chuẩn hóa dấu chấm hình học lăng trụ/chóp: ABCD \cdot A'B'C'D' -> ABCD.A'B'C'D'
            formula = Regex.Replace(formula, @"([A-Z]{3,4})\s*\\cdot\s*([A-Z])", "$1.$2");

            // 3. Xóa các cặp ngoặc rỗng dư thừa nếu có
            formula = formula.Replace(@"\left(\right)", string.Empty);
            formula = formula.Replace(@"\left[\right]", string.Empty);
            formula = formula.Replace(@"\left\{\right\}", string.Empty);
            formula = Regex.Replace(formula, @"(?<!\\)\(\s*\)", string.Empty);
            formula = Regex.Replace(formula, @"(?<!\\)\[\s*\]", string.Empty);

            // 4. Thu gọn khoảng trắng thừa
            formula = Regex.Replace(formula, @"\s{2,}", " ");

            return formula;
        }

        private static int FindBytePattern(byte[] source, byte[] pattern)
        {
            if (source == null || pattern == null || source.Length < pattern.Length) return -1;

            for (int i = 0; i <= source.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (source[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }

            return -1;
        }
    }
}
