using System;
using System.Collections.Generic;
using System.Diagnostics;
using SkiaSharp;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [Enterprise MathType & WMF Vector Engine v8.5]
    /// Chuyên biệt hóa toàn diện việc giải mã và chuẩn hóa công thức toán học từ MathType WMF:
    /// 1. Ánh xạ 256 ký tự Adobe Symbol sang Unicode W3C chuẩn xác (Prime ′, Tích phân ∫, Vuông góc ⊥, Vô cực ∞, v.v.).
    /// 2. Ghép nối chuỗi ký tự MT Extra (u...r) thành mũi tên Vector SVG sắc nét.
    /// 3. Hỗ trợ Fallback 3 tầng: Linux CLI (wmf2svg), Windows GDI+, và Pure C# Vector Decoder.
    /// 4. Đảm bảo 100% chuẩn W3C SVG tương thích tuyệt đối với thẻ <img> trên mọi trình duyệt.
    /// </summary>
    public static class MathTypeWmfEngine
    {
        // [Tier-3 Fail-Safe Fallback]: Bảng tra cứu tĩnh dự phòng tối thiểu khi DynamicMathDictionaryService chưa khởi tạo
        private static readonly Dictionary<char, string> FallbackSymbolGlyphMap = new()
        {
            // Các ký hiệu toán học đặc thù hay bị hiển thị lỗi nhất
            { '\u00A2', "&#x2032;" }, // 0xA2 (¢) -> Prime ′ (Dấu phẩy trên A', B', C', D', y')
            { '\u00F2', "&#x222B;" }, // 0xF2 (ò) -> Integral ∫ (Dấu tích phân)
            { '\u00A5', "&#x221E;" }, // 0xA5 (¥) -> Infinity ∞ (Vô cùng / vô cực)
            { '\u00C5', "&#x22A5;" }, // 0xC5 (Å) -> Perpendicular ⊥ (Vuông góc)
            { '\u00CE', "&#x2208;" }, // 0xCE (Î) -> Element of ∈ (Thuộc)
            { '\u00CF', "&#x2209;" }, // 0xCF (Ï) -> Not element of ∉ (Không thuộc)
            { '\u00C8', "&#x222A;" }, // 0xC8 (È) -> Union ∪ (Hợp tập hợp)
            { '\u00C7', "&#x2229;" }, // 0xC7 (Ç) -> Intersection ∩ (Giao tập hợp)
            { '\u00C6', "&#x2205;" }, // 0xC6 (Æ) -> Empty set ∅ (Tập rỗng)
            { '\u00CC', "&#x2282;" }, // 0xCC (Ì) -> Subset ⊂ (Tập con)
            { '\u00CD', "&#x2283;" }, // 0xCD -> Superset ⊃ (Chứa tập)
            { '\u00DA', "&#x2286;" }, // 0xDA (Ú) -> Subset or equal ⊆ (Tập con hoặc bằng)
            { '\u00A3', "&#x2264;" }, // 0xA3 (£) -> Less than or equal ≤ (Nhỏ hơn hoặc bằng)
            { '\u00B3', "&#x2265;" }, // 0xB3 (³) -> Greater than or equal ≥ (Lớn hơn hoặc bằng)
            { '\u00B9', "&#x2260;" }, // 0xB9 (¹) -> Not equal ≠ (Khác / Không bằng)
            { '\u00AE', "&#x2192;" }, // 0xAE (®) -> Right arrow → (Mũi tên sang phải / tiến tới)
            { '\u00AC', "&#x2190;" }, // 0xAC (¬) -> Left arrow ← (Mũi tên sang trái)
            { '\u00AB', "&#x2194;" }, // 0xAB («) -> Left right arrow ↔ (Mũi tên hai chiều)
            { '\u00DE', "&#x21D2;" }, // 0xDE (Þ) -> Rightwards double arrow ⇒ (Kéo theo / Suy ra)
            { '\u00DB', "&#x21D4;" }, // 0xDB -> Left right double arrow ⇔ (Tương đương)
            { '\u00DC', "&#x21D0;" }, // 0xDC -> Leftwards double arrow ⇐
            { '\u00D6', "&#x221A;" }, // 0xD6 (Ö) -> Square root √ (Căn bậc hai)
            { '\u00D7', "&#x22C5;" }, // 0xD7 (×) trong MathType là Dấu chấm nhân / Tích vô hướng ⋅
            { '\u00B7', "&#x22C5;" }, // 0xB7 (·) -> Center dot ⋅
            { '\u00B1', "&#x00B1;" }, // 0xB1 (±) -> Plus-minus ± (Cộng trừ)
            { '\u00B0', "&#x00B0;" }, // 0xB0 (°) -> Degree ° (Độ)
            { '\u00BB', "&#x2248;" }, // 0xBB (») -> Almost equal ≈ (Xấp xỉ)
            { '\u00E5', "&#x2211;" }, // 0xE5 (å) -> Summation ∑ (Tổng sigma)
            { '\u00D5', "&#x220F;" }, // 0xD5 (Õ) -> Product ∏ (Tích pi)
            { '\u00C2', "&#x2220;" }, // 0xC2 (Â) -> Angle ∠ (Góc)
            { '@', "&#x2245;" },      // 0x40 (@) -> Congruent ≅ (Đồng dạng và bằng)
            { '~', "&#x223C;" },      // 0x7E (~) -> Similar ∼ (Đồng dạng)
            { '"', "&#x2200;" },      // 0x22 (") -> For all ∀ (Với mọi)
            { '$', "&#x2203;" },      // 0x24 ($) -> There exists ∃ (Tồn tại)
            { '\\', "&#x2234;" },     // 0x5C (\) -> Therefore ∴ (Do đó / Vì vậy)
            { '\u0082', "\\" },       // 0x82 (‚) -> Setminus / Backslash \ (Tập xác định R \ {2})
            { '\u00E6', "&#x239B;" }, // 0xE6 (æ) -> Left paren top hook ⎛
            { '\u00E7', "&#x239C;" }, // 0xE7 (ç) -> Left paren extender ⎜
            { '\u00E8', "&#x239D;" }, // 0xE8 (è) -> Left paren bottom hook ⎝
            { '\u00F6', "&#x239E;" }, // 0xF6 (ö) -> Right paren top hook ⎞
            { '\u00F7', "&#x239F;" }, // 0xF7 (÷) -> Right paren extender ⎟
            { '\u00F8', "&#x23A0;" }  // 0xF8 (ø) -> Right paren bottom hook ⎠
        };

        // [Tier-3 Fail-Safe Fallback]: Bảng tra cứu chữ cái Hy Lạp dự phòng tối thiểu
        private static readonly Dictionary<char, string> FallbackGreekLettersMap = new()
        {
            { 'a', "&#x03B1;" }, // Alpha α
            { 'b', "&#x03B2;" }, // Beta β
            { 'g', "&#x03B3;" }, // Gamma γ
            { 'd', "&#x03B4;" }, // Delta δ
            { 'e', "&#x03B5;" }, // Epsilon ε
            { 'z', "&#x03B6;" }, // Zeta ζ
            { 'h', "&#x03B7;" }, // Eta η
            { 'q', "&#x03B8;" }, // Theta θ
            { 'i', "&#x03B9;" }, // Iota ι
            { 'k', "&#x03BA;" }, // Kappa κ
            { 'l', "&#x03BB;" }, // Lambda λ
            { 'm', "&#x03BC;" }, // Mu μ
            { 'n', "&#x03BD;" }, // Nu ν
            { 'x', "&#x03BE;" }, // Xi ξ
            { 'o', "&#x03BF;" }, // Omicron ο
            { 'p', "&#x03C0;" }, // Pi π
            { 'r', "&#x03C1;" }, // Rho ρ
            { 's', "&#x03C3;" }, // Sigma σ
            { 't', "&#x03C4;" }, // Tau τ
            { 'u', "&#x03C5;" }, // Upsilon υ
            { 'f', "&#x03C6;" }, // Phi φ
            { 'j', "&#x03D5;" }, // Phi biến thể ϕ
            { 'c', "&#x03C7;" }, // Chi χ
            { 'y', "&#x03C8;" }, // Psi ψ
            { 'w', "&#x03C9;" }, // Omega ω
            { 'A', "&#x0391;" }, // Alpha hoa
            { 'B', "&#x0392;" }, // Beta hoa
            { 'G', "&#x0393;" }, // Gamma hoa Γ
            { 'D', "&#x0394;" }, // Delta hoa Δ
            { 'E', "&#x0395;" },
            { 'Z', "&#x0396;" },
            { 'H', "&#x0397;" },
            { 'Q', "&#x0398;" }, // Theta hoa Θ
            { 'I', "&#x0399;" },
            { 'K', "&#x039A;" },
            { 'L', "&#x039B;" }, // Lambda hoa Λ
            { 'M', "&#x039C;" },
            { 'N', "&#x039D;" },
            { 'X', "&#x039E;" }, // Xi hoa Ξ
            { 'O', "&#x039F;" },
            { 'P', "&#x03A0;" }, // Pi hoa Π
            { 'R', "&#x03A1;" },
            { 'S', "&#x03A3;" }, // Sigma hoa Σ
            { 'T', "&#x03A4;" },
            { 'U', "&#x03A5;" },
            { 'F', "&#x03A6;" }, // Phi hoa Φ
            { 'C', "&#x03A7;" },
            { 'Y', "&#x03A8;" }, // Psi hoa Ψ
            { 'W', "&#x03A9;" }  // Omega hoa Ω
        };

        /// <summary>
        /// Tối ưu hóa tính tương phản và làm phẳng nền trắng (Alpha Flattening) cho hình ảnh trích xuất:
        /// - Xử lý triệt để các ảnh đồ thị, sơ đồ có kênh Alpha trong suốt (thường gặp khi copy từ Word/GeoGebra/Cabri)
        /// - Chuyển đổi mọi pixel trong suốt (alpha < 255) thành nền trắng tinh khiết (#FFFFFF)
        /// - Đảm bảo hình ảnh giữ trọn vẹn nét vẽ gốc (đen/màu), hiển thị rõ ràng, tương phản hoàn hảo trên mọi nền (Dark/Light mode)
        /// </summary>
        public static byte[] FlattenImageToWhiteBackground(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return bytes;

            try
            {
                using var msIn = new MemoryStream(bytes);
                using var srcBitmap = SKBitmap.Decode(msIn);
                if (srcBitmap == null) return bytes;

                // Nếu ảnh đã hoàn toàn đục (Opaque), không có kênh trong suốt thì giữ nguyên
                if (srcBitmap.AlphaType == SKAlphaType.Opaque)
                {
                    return bytes;
                }

                int w = srcBitmap.Width;
                int h = srcBitmap.Height;
                bool hasTransparency = false;

                // Kiểm tra nhanh mẫu pixel xem có độ trong suốt không
                int stepY = Math.Max(1, h / 25);
                int stepX = Math.Max(1, w / 25);
                for (int y = 0; y < h && !hasTransparency; y += stepY)
                {
                    for (int x = 0; x < w; x += stepX)
                    {
                        if (srcBitmap.GetPixel(x, y).Alpha < 250)
                        {
                            hasTransparency = true;
                            break;
                        }
                    }
                }

                if (!hasTransparency)
                {
                    return bytes;
                }

                // Tạo surface nền trắng tinh khiết cùng kích thước
                var info = new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var surface = SKSurface.Create(info);
                if (surface == null) return bytes;

                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White); // Phủ nền trắng #FFFFFF
                canvas.DrawBitmap(srcBitmap, 0, 0); // Vẽ đè hình ảnh lên nền trắng
                canvas.Flush();

                using var image = surface.Snapshot();
                using var pngData = image.Encode(SKEncodedImageFormat.Png, 100);
                using var msOut = new MemoryStream();
                pngData.SaveTo(msOut);
                return msOut.ToArray();
            }
            catch
            {
                return bytes;
            }
        }

        /// <summary>
        /// Chuyển đổi tệp media nhúng sang chuỗi Data URI chuẩn W3C
        /// </summary>
        public static string ConvertMediaToDataUri(string filename, byte[] bytes)
        {
            var ext = Path.GetExtension(filename).ToLowerInvariant();

            if (ext == ".wmf")
            {
                var svgDataUri = ConvertWmfToSvgOrPng(bytes);
                if (!string.IsNullOrEmpty(svgDataUri))
                {
                    return svgDataUri;
                }
                // Fallback: Trả về SVG trong suốt chuẩn W3C để trình duyệt không bao giờ bị icon vỡ
                return "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIxIiBoZWlnaHQ9IjEiIHZpZXdCb3g9IjAgMCAxIDEiPjwvc3ZnPg==";
            }

            // Tự động làm phẳng nền trắng cho ảnh có kênh alpha (PNG, WebP, GIF...)
            // Đảm bảo đồ thị tọa độ và sơ đồ không bị tối đen khi hiển thị trên giao diện nền tối
            if (ext == ".png" || ext == ".webp" || ext == ".gif")
            {
                bytes = FlattenImageToWhiteBackground(bytes);
            }

            string mime = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/png"
            };

            return $"data:{mime};base64," + Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Đường dẫn binary wmf2svg (Linux container: /usr/bin/wmf2svg hoặc trên PATH). Null nếu không có — khi đó bỏ qua tầng 1
        /// để không tạo tiến trình / file tạm vô ích trên mỗi công thức WMF.
        /// </summary>
        private static readonly Lazy<string?> Wmf2SvgBinaryPath = new(() =>
        {
            if (File.Exists("/usr/bin/wmf2svg")) return "/usr/bin/wmf2svg";
            var pathDirs = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            foreach (var dir in pathDirs)
            {
                foreach (var candidate in new[] { "wmf2svg", "wmf2svg.exe" })
                {
                    try
                    {
                        var full = Path.Combine(dir.Trim(), candidate);
                        if (File.Exists(full)) return full;
                    }
                    catch { }
                }
            }
            return null;
        });

        /// <summary>
        /// Chuyển đổi WMF sang SVG hoặc PNG qua kiến trúc Fallback 4 tầng
        /// </summary>
        public static string ConvertWmfToSvgOrPng(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return string.Empty;

            // 1. Thử dùng wmf2svg CLI (Vector SVG siêu nét chuẩn W3C trên Linux Container)
            //    Chỉ chạy khi binary thực sự tồn tại — tránh ghi file tạm vô ích (rò rỉ hàng trăm nghìn file trong %TEMP%) trên Windows.
            var wmfBin = Wmf2SvgBinaryPath.Value;
            if (wmfBin != null)
            {
                var tempWmf = Path.Combine(Path.GetTempPath(), $"wmf_{Guid.NewGuid():N}.wmf");
                var tempSvg = Path.Combine(Path.GetTempPath(), $"wmf_{Guid.NewGuid():N}.svg");
                try
                {
                    File.WriteAllBytes(tempWmf, bytes);

                    var psi = new ProcessStartInfo
                    {
                        FileName = wmfBin,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    psi.ArgumentList.Add("-o");
                    psi.ArgumentList.Add(tempSvg);
                    psi.ArgumentList.Add(tempWmf);

                    using var process = Process.Start(psi);
                    if (process != null)
                    {
                        if (!process.WaitForExit(8000))
                        {
                            try { process.Kill(entireProcessTree: true); } catch { }
                        }
                        if (File.Exists(tempSvg))
                        {
                            var svgBytes = File.ReadAllBytes(tempSvg);
                            if (svgBytes.Length > 0)
                            {
                                var svgStr = Encoding.Latin1.GetString(svgBytes);
                                svgStr = SanitizeMathTypeSvg(svgStr);
                                return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svgStr));
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback nếu wmf2svg lỗi
                }
                finally
                {
                    try { if (File.Exists(tempWmf)) File.Delete(tempWmf); } catch { }
                    try { if (File.Exists(tempSvg)) File.Delete(tempSvg); } catch { }
                }
            }

            // 2. Thử decode WMF bytes bằng SkiaSharp (cross-platform)
            // Guard OperatingSystem.IsWindows() giữ nguyên vì WMF là định dạng Windows-native
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    using var msIn = new MemoryStream(bytes);
                    using var srcBitmap = SKBitmap.Decode(msIn);
                    if (srcBitmap != null)
                    {
                        var info = new SKImageInfo(srcBitmap.Width, srcBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
                        using var surface = SKSurface.Create(info);
                        if (surface != null)
                        {
                            var canvas = surface.Canvas;
                            canvas.Clear(SKColors.White);
                            canvas.DrawBitmap(srcBitmap, 0, 0);
                            canvas.Flush();
                            using var skImage = surface.Snapshot();
                            using var pngData = skImage.Encode(SKEncodedImageFormat.Png, 100);
                            using var msOut = new MemoryStream();
                            pngData.SaveTo(msOut);
                            return "data:image/png;base64," + Convert.ToBase64String(msOut.ToArray());
                        }
                    }
                }
                catch { }
            }

            // 3. Fallback: Bộ thông dịch Vector WMF thuần C# (Pure C# WMF-to-SVG)
            try
            {
                var svg = PureWmfToSvg(bytes);
                if (!string.IsNullOrEmpty(svg))
                {
                    svg = SanitizeMathTypeSvg(svg);
                    return "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));
                }
            }
            catch { }

            // 4. Fallback cuối cùng: Trả về SVG trong suốt chuẩn W3C để trình duyệt không bao giờ bị icon vỡ
            return "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIxIiBoZWlnaHQ9IjEiIHZpZXdCb3g9IjAgMCAxIDEiPjwvc3ZnPg==";
        }

        /// <summary>
        /// Chuẩn hóa và làm sạch chuỗi SVG của MathType:
        /// - Chuyển đổi toàn diện các ký tự Symbol font sang Unicode Entity
        /// - Ghép chuỗi 'u...r' của font MT Extra thành mũi tên Vector SVG sắc nét
        /// - Chuẩn hóa thuộc tính SVG W3C (xmlns, viewBox, display inline)
        /// </summary>
        public static string SanitizeMathTypeSvg(string svgStr)
        {
            if (string.IsNullOrWhiteSpace(svgStr)) return svgStr;

            // 0. Triệt tiêu các khối polygon rác che phủ (giữ lại các đa giác toán học hợp lệ như căn thức có class="math-shape")
            svgStr = Regex.Replace(svgStr, @"<polygon\b(?![^>]*class=""math-shape"")[^>]*\/?>", "", RegexOptions.IgnoreCase);

            var settings = DynamicMathDictionaryService.Instance.GetEngineSettings() ?? new AegisQuiz.Application.DTOs.MathEngineTypographyDto();

            // 1. Chuẩn hóa các thẻ <text> thuộc font Symbol / MT Extra
            svgStr = Regex.Replace(svgStr, @"<text([^>]*font-family:[^;]*Symbol[^>]*)>\s*([\s\S]*?)\s*</text>", m =>
            {
                string open = m.Groups[1].Value;
                string content = m.Groups[2].Value;

                var sb = new StringBuilder();
                foreach (char c in content)
                {
                    // Tier 1: Dynamic Math Dictionary Service
                    string entity = DynamicMathDictionaryService.Instance.ResolveSymbolGlyph("Symbol", c, string.Empty);
                    if (string.IsNullOrEmpty(entity))
                    {
                        entity = DynamicMathDictionaryService.Instance.ResolveGreekLetter("Symbol", c, string.Empty);
                    }

                    // Tier 3: Hardcoded Fail-Safe Fallbacks
                    if (string.IsNullOrEmpty(entity) && FallbackSymbolGlyphMap.TryGetValue(c, out var mapped))
                    {
                        entity = mapped;
                    }
                    if (string.IsNullOrEmpty(entity) && FallbackGreekLettersMap.TryGetValue(c, out var greek))
                    {
                        entity = greek;
                    }

                    if (!string.IsNullOrEmpty(entity))
                    {
                        sb.Append(entity);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                content = sb.ToString();

                // Đổi font-family từ Symbol sang font Serif chuẩn để trình duyệt hiển thị đúng glyph Unicode
                open = Regex.Replace(open, @"font-family:[^;]+;?", "font-family:'Times New Roman', 'Cambria Math', serif;");
                return $"<text{open}>{content}</text>";
            }, RegexOptions.IgnoreCase);

            // 1b. Quét toàn cục bổ sung cho các ký tự Symbol hay bị rơi ra ngoài thẻ font-family
            foreach (var kvp in FallbackSymbolGlyphMap)
            {
                string glyph = DynamicMathDictionaryService.Instance.ResolveSymbolGlyph("Symbol", kvp.Key, kvp.Value);
                string charPattern = Regex.Escape(kvp.Key.ToString());
                svgStr = Regex.Replace(svgStr, $@"(<text[^>]*>)\s*{charPattern}\s*(</text>)", $"$1{glyph}$2", RegexOptions.IgnoreCase);
            }

            // 2. Chuyển đổi các chuỗi ký tự 'u...r' trong MT Extra thành mũi tên Vector SVG sắc nét
            try
            {
                var textElemRegexAll = new Regex(@"<text\s+([^>]*font-size:([\d\.]+)[^>]*)transform=""matrix\([^)]+\s+([\d\.]+)\s+([\d\.]+)\)""[^>]*>([\s\S]*?)</text>", RegexOptions.IgnoreCase);
                var allTextMatches = textElemRegexAll.Matches(svgStr);
                if (allTextMatches.Count > 0)
                {
                    var allItems = new List<(Match Match, string Attrs, float FontSize, float X, float Y, string Text, bool IsItalic, bool IsMtExtra)>();
                    foreach (Match tm in allTextMatches)
                    {
                        if (float.TryParse(tm.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float fs) &&
                            float.TryParse(tm.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                            float.TryParse(tm.Groups[4].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                        {
                            string attrs = tm.Groups[1].Value;
                            string text = tm.Groups[5].Value.Trim();
                            bool isItalic = attrs.IndexOf("font-style:italic", StringComparison.OrdinalIgnoreCase) >= 0;
                            bool isMtExtra = attrs.IndexOf("MT Extra", StringComparison.OrdinalIgnoreCase) >= 0;
                            allItems.Add((tm, attrs, fs, x, y, text, isItalic, isMtExtra));
                        }
                    }

                    // Nhặt các phần tử 'u' và 'r' không in nghiêng (font MT Extra thường xuất ra normal)
                    var urItems = allItems.Where(i => (i.Text == "u" || i.Text == "r") && !i.IsItalic).ToList();
                    var groups = new List<List<(Match Match, string Attrs, float FontSize, float X, float Y, string Text, bool IsItalic, bool IsMtExtra)>>();
                    var currentGroup = new List<(Match Match, string Attrs, float FontSize, float X, float Y, string Text, bool IsItalic, bool IsMtExtra)>();
                    foreach (var it in urItems)
                    {
                        currentGroup.Add(it);
                        if (it.Text == "r")
                        {
                            groups.Add(new List<(Match Match, string Attrs, float FontSize, float X, float Y, string Text, bool IsItalic, bool IsMtExtra)>(currentGroup));
                            currentGroup.Clear();
                        }
                    }

                    foreach (var grp in groups)
                    {
                        bool hasShaft = grp.Any(g => g.Text == "u");
                        bool isMtExtra = grp.Any(g => g.IsMtExtra);
                        var rItem = grp[grp.Count - 1];

                        if (hasShaft || isMtExtra)
                        {
                            // Vector đa ký tự (u...r)
                            float minX = grp.Min(g => g.X);
                            float maxX = grp.Max(g => g.X);
                            float y = grp[0].Y;
                            float fs = grp[0].FontSize;

                            float arrowY = y - fs * 0.22f;
                            float x1 = minX;
                            float x2 = maxX + fs * 0.20f;
                            float sw = Math.Max(1.5f, fs * 0.038f);
                            float headLen = fs * 0.15f;
                            float headH = fs * 0.09f;

                            string arrowSvg = string.Format(CultureInfo.InvariantCulture,
                                @"<g class=""math-vector-arrow""><line x1=""{0:F2}"" y1=""{1:F2}"" x2=""{2:F2}"" y2=""{1:F2}"" stroke=""currentColor"" stroke-width=""{3:F2}"" stroke-linecap=""round""/><path d=""M {4:F2} {5:F2} L {2:F2} {1:F2} L {4:F2} {6:F2}"" fill=""none"" stroke=""currentColor"" stroke-width=""{3:F2}"" stroke-linecap=""round"" stroke-linejoin=""round""/></g>",
                                x1, arrowY, x2, sw, x2 - headLen, arrowY - headH, arrowY + headH);

                            svgStr = svgStr.Replace(grp[0].Match.Value, arrowSvg);
                            for (int j = 1; j < grp.Count; j++)
                            {
                                svgStr = svgStr.Replace(grp[j].Match.Value, "");
                            }
                        }
                        else if (grp.Count == 1 && rItem.Text == "r")
                        {
                            // Vector đơn ký tự (chữ 'r' đơn lẻ nằm trên ký tự chữ cái như v, a, b, x, y, z, i, j, k, ...)
                            var candidates = allItems.Where(c =>
                                c.Match != rItem.Match &&
                                c.Y > rItem.Y &&
                                (c.Y - rItem.Y) >= rItem.FontSize * 0.35f &&
                                (c.Y - rItem.Y) <= rItem.FontSize * 1.35f &&
                                Math.Abs(c.X - rItem.X) <= rItem.FontSize * 0.85f
                            ).ToList();

                            if (candidates.Count > 0)
                            {
                                // Ưu tiên ký tự chữ cái trước, sau đó là khoảng cách ngang nhỏ nhất
                                candidates.Sort((a, b) =>
                                {
                                    bool aAlpha = Regex.IsMatch(a.Text, @"^[a-zA-Z]$");
                                    bool bAlpha = Regex.IsMatch(b.Text, @"^[a-zA-Z]$");
                                    if (aAlpha != bAlpha) return bAlpha.CompareTo(aAlpha);
                                    return Math.Abs(a.X - rItem.X).CompareTo(Math.Abs(b.X - rItem.X));
                                });

                                var underChar = candidates[0];
                                float fs = underChar.FontSize;
                                float x1 = Math.Min(underChar.X, rItem.X);
                                float x2 = x1 + fs * 0.55f;
                                float arrowY = rItem.Y + rItem.FontSize * 0.15f;
                                float sw = Math.Max(1.5f, fs * settings.VectorStrokeWidthRatio);
                                float headLen = fs * settings.VectorHeadLengthRatio;
                                float headH = fs * settings.VectorHeadSpreadRatio;

                                string arrowSvg = string.Format(CultureInfo.InvariantCulture,
                                    @"<g class=""math-vector-arrow""><line x1=""{0:F2}"" y1=""{1:F2}"" x2=""{2:F2}"" y2=""{1:F2}"" stroke=""currentColor"" stroke-width=""{3:F2}"" stroke-linecap=""round""/><path d=""M {4:F2} {5:F2} L {2:F2} {1:F2} L {4:F2} {6:F2}"" fill=""none"" stroke=""currentColor"" stroke-width=""{3:F2}"" stroke-linecap=""round"" stroke-linejoin=""round""/></g>",
                                    x1, arrowY, x2, sw, x2 - headLen, arrowY - headH, arrowY + headH);

                                svgStr = svgStr.Replace(rItem.Match.Value, arrowSvg);
                            }
                        }
                    }
                }
            }
            catch { }

            // 2b. Khắc phục lỗi dấu ngoặc đơn / ngoặc vuông lớn bao quanh phân số (MathType tall parentheses)
            try
            {
                var textNodesAll = Regex.Matches(svgStr, @"<text\b([^>]*)>([\s\S]*?)</text>", RegexOptions.IgnoreCase);
                if (textNodesAll.Count > 0)
                {
                    var allItems = new List<(Match Match, float FontSize, float X, float Y, string Text)>();
                    foreach (Match tm in textNodesAll)
                    {
                        string attrs = tm.Groups[1].Value;
                        string text = tm.Groups[2].Value.Trim();
                        float x = 0, y = 0, fs = 14;
                        bool hasCoords = false;

                        var matMatch = Regex.Match(attrs, @"transform=""matrix\([^)]+\s+([-+]?[\d\.]+)\s+([-+]?[\d\.]+)\)""", RegexOptions.IgnoreCase);
                        if (matMatch.Success &&
                            float.TryParse(matMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                            float.TryParse(matMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                        {
                            hasCoords = true;
                        }
                        else
                        {
                            var xMatch = Regex.Match(attrs, @"\bx=""([-+]?[\d\.]+)""", RegexOptions.IgnoreCase);
                            var yMatch = Regex.Match(attrs, @"\by=""([-+]?[\d\.]+)""", RegexOptions.IgnoreCase);
                            if (xMatch.Success && yMatch.Success &&
                                float.TryParse(xMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                                float.TryParse(yMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                            {
                                hasCoords = true;
                            }
                        }

                        if (hasCoords)
                        {
                            var fsMatch = Regex.Match(attrs, @"font-size[:=""]+([-+]?[\d\.]+)", RegexOptions.IgnoreCase);
                            if (fsMatch.Success && float.TryParse(fsMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedFs))
                            {
                                fs = parsedFs;
                            }
                            allItems.Add((tm, fs, x, y, text));
                        }
                    }

                    // Hàm nhận diện các mảnh dấu ngoặc lớn hoặc ký tự toán học bị lỗi font
                    bool IsDelimiterPiece(string t)
                    {
                        if (string.IsNullOrEmpty(t)) return true;
                        if (t == "\uFFFD" || t == "&#xFFFD;") return true;
                        if (t == "&#x239B;" || t == "&#x239C;" || t == "&#x239D;" ||
                            t == "&#x239E;" || t == "&#x239F;" || t == "&#x23A0;") return true;
                        if (t == "\u239B" || t == "\u239C" || t == "\u239D" ||
                            t == "\u239E" || t == "\u239F" || t == "\u23A0") return true;
                        if (t == "&#x23A1;" || t == "&#x23A2;" || t == "&#x23A3;" ||
                            t == "&#x23A4;" || t == "&#x23A5;" || t == "&#x23A6;") return true;
                        if (t == "æ" || t == "ç" || t == "è" || t == "ö" || t == "÷" || t == "ø") return true;
                        return false;
                    }

                    // Tìm các ký tự bị lỗi hiển thị hoặc là mảnh ghép delimiter của MathType
                    var brokenItems = allItems.Where(i => IsDelimiterPiece(i.Text)).ToList();
                    if (brokenItems.Count > 0)
                    {
                        // Nhóm theo tọa độ X (trong bán kính 25px)
                        var groupsByX = new List<List<(Match Match, float FontSize, float X, float Y, string Text)>>();
                        foreach (var it in brokenItems)
                        {
                            var existing = groupsByX.FirstOrDefault(g => Math.Abs(g[0].X - it.X) < 25);
                            if (existing != null)
                            {
                                existing.Add(it);
                            }
                            else
                            {
                                groupsByX.Add(new List<(Match Match, float FontSize, float X, float Y, string Text)> { it });
                            }
                        }

                        // Sắp xếp các nhóm theo tọa độ X từ trái sang phải
                        groupsByX = groupsByX.OrderBy(g => g[0].X).ToList();

                        var validItems = allItems.Where(i => !IsDelimiterPiece(i.Text)).ToList();
                        float centerX = validItems.Count > 0
                            ? (validItems.Min(i => i.X) + validItems.Max(i => i.X)) / 2f
                            : 384f;

                        for (int gi = 0; gi < groupsByX.Count; gi++)
                        {
                            var grp = groupsByX[gi];
                            float x = grp[0].X;
                            if (grp.Count >= 2)
                            {
                                // Đây là dấu ngoặc cao nhiều tầng bao quanh phân số!
                                float minY = grp.Min(g => g.Y) - grp[0].FontSize * 0.65f;
                                float maxY = grp.Max(g => g.Y) + grp[0].FontSize * 0.15f;
                                float h = maxY - minY;
                                float fs = grp[0].FontSize;
                                float strokeW = Math.Max(14f, fs * (settings.ParenthesisStrokeWidthRatio > 0 ? settings.ParenthesisStrokeWidthRatio : 0.045f));
                                bool isLeft = groupsByX.Count == 2 ? (gi == 0) : (x < centerX);

                                float bow = Math.Max(15f, Math.Min(35f, fs * (settings.ParenthesisBowRatio > 0 ? settings.ParenthesisBowRatio : 0.18f)));
                                string pathD;
                                if (isLeft)
                                {
                                    float xOuter = x + bow;
                                    float xInner = x - bow * 0.6f;
                                    pathD = string.Format(CultureInfo.InvariantCulture,
                                        "M {0:F1} {1:F1} C {2:F1} {3:F1}, {2:F1} {4:F1}, {0:F1} {5:F1}",
                                        xOuter, minY, xInner, minY + h * 0.25f, maxY - h * 0.25f, maxY);
                                }
                                else
                                {
                                    float xOuter = x - bow;
                                    float xInner = x + bow * 0.6f;
                                    pathD = string.Format(CultureInfo.InvariantCulture,
                                        "M {0:F1} {1:F1} C {2:F1} {3:F1}, {2:F1} {4:F1}, {0:F1} {5:F1}",
                                        xOuter, minY, xInner, minY + h * 0.25f, maxY - h * 0.25f, maxY);
                                }

                                string parenSvg = string.Format(CultureInfo.InvariantCulture,
                                    @"<path d=""{0}"" fill=""none"" stroke=""rgb(0,0,0)"" stroke-width=""{1:F1}"" stroke-linecap=""round"" class=""math-delimiter""/>",
                                    pathD, strokeW);

                                svgStr = svgStr.Replace(grp[0].Match.Value, parenSvg);
                                for (int j = 1; j < grp.Count; j++)
                                {
                                    svgStr = svgStr.Replace(grp[j].Match.Value, "");
                                }
                            }
                            else if (grp.Count == 1)
                            {
                                // Ký tự toán học đơn lẻ bị thiếu (dấu setminus \ trong R \ {2})
                                var it = grp[0];
                                string repl = string.Format(CultureInfo.InvariantCulture,
                                    @"<text x=""{1:F1}"" y=""{2:F1}"" font-family=""'Times New Roman',serif"" font-size=""{0:F1}"" fill=""rgb(0,0,0)"">\</text>",
                                    it.FontSize, it.X, it.Y);
                                svgStr = svgStr.Replace(it.Match.Value, repl);
                            }
                        }
                    }
                }
            }
            catch { }

            // 2c. Giãn nhẹ khoảng cách giữa các ký tự trong công thức (Letter-Spacing động cho chuỗi văn bản dài có style="...")
            try
            {
                string letterSpacing = !string.IsNullOrEmpty(settings.LetterSpacing) ? settings.LetterSpacing : "0.1em";
                if (!svgStr.Contains("letter-spacing"))
                {
                    svgStr = Regex.Replace(svgStr, @"(<text\s+[^>]*(?<![-_\w])style=""[^""]*)""", $"$1; letter-spacing:{letterSpacing};\"", RegexOptions.IgnoreCase);
                }
            }
            catch { }

            // 2d. Tính toán hộp giới hạn thực tế (Auto-Crop True Bounding Box) triệt tiêu 100% khoảng trắng thừa
            try
            {
                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                var alphaFs = new List<float>();
                var allFs = new List<float>();

                // Quét văn bản <text> (hỗ trợ cả transform matrix của wmf2svg lẫn tọa độ x,y trực tiếp của PureWmfToSvg)
                var textNodes = Regex.Matches(svgStr, @"<text\b([^>]*)>([\s\S]*?)</text>", RegexOptions.IgnoreCase);
                foreach (Match tm in textNodes)
                {
                    string attrs = tm.Groups[1].Value;
                    string rawText = tm.Groups[2].Value.Trim();
                    float x = 0, y = 0, fs = 14;
                    bool hasCoords = false;

                    var matMatch = Regex.Match(attrs, @"transform=""matrix\([^)]+\s+([-+]?[\d\.]+)\s+([-+]?[\d\.]+)\)""", RegexOptions.IgnoreCase);
                    if (matMatch.Success &&
                        float.TryParse(matMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                        float.TryParse(matMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                    {
                        hasCoords = true;
                    }
                    else
                    {
                        var xMatch = Regex.Match(attrs, @"\bx=""([-+]?[\d\.]+)""", RegexOptions.IgnoreCase);
                        var yMatch = Regex.Match(attrs, @"\by=""([-+]?[\d\.]+)""", RegexOptions.IgnoreCase);
                        if (xMatch.Success && yMatch.Success &&
                            float.TryParse(xMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                            float.TryParse(yMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                        {
                            hasCoords = true;
                        }
                    }

                    if (hasCoords)
                    {
                        var fsMatch = Regex.Match(attrs, @"font-size[:=""]+([-+]?[\d\.]+)", RegexOptions.IgnoreCase);
                        if (fsMatch.Success && float.TryParse(fsMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedFs))
                        {
                            fs = parsedFs;
                        }

                        string decodedText = Regex.Replace(rawText, @"&#x?[0-9a-fA-F]+;|&[a-zA-Z]+;", "X");
                        int len = Math.Max(1, decodedText.Length);

                        allFs.Add(fs);
                        if (Regex.IsMatch(rawText, @"[a-zA-Z0-9]"))
                        {
                            alphaFs.Add(fs);
                        }

                        // Hệ số mở rộng ký tự động để đảm bảo công thức dài không bị cắt xén mép phải
                        float w = len * fs * settings.CharWidthFactor;
                        minX = Math.Min(minX, x);
                        minY = Math.Min(minY, y - fs * settings.AscenderFactor);
                        maxX = Math.Max(maxX, x + w);
                        maxY = Math.Max(maxY, y + fs * settings.DescenderFactor);
                    }
                }

                // Quét đoạn thẳng <line>
                var lMatches = Regex.Matches(svgStr, @"<line\s+[^>]*x1=""([\d\.]+)""\s+y1=""([\d\.]+)""\s+x2=""([\d\.]+)""\s+y2=""([\d\.]+)""", RegexOptions.IgnoreCase);
                foreach (Match lm in lMatches)
                {
                    if (float.TryParse(lm.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float x1) &&
                        float.TryParse(lm.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float y1) &&
                        float.TryParse(lm.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float x2) &&
                        float.TryParse(lm.Groups[4].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float y2))
                    {
                        minX = Math.Min(minX, Math.Min(x1, x2));
                        maxX = Math.Max(maxX, Math.Max(x1, x2));
                        minY = Math.Min(minY, Math.Min(y1, y2));
                        maxY = Math.Max(maxY, Math.Max(y1, y2));
                    }
                }

                // Quét đường gấp khúc <polyline> (nét căn thức, biểu đồ)
                var plMatches = Regex.Matches(svgStr, @"<polyline\s+[^>]*points=""([^""]+)""", RegexOptions.IgnoreCase);
                foreach (Match pl in plMatches)
                {
                    var numMatches = Regex.Matches(pl.Groups[1].Value, @"[-+]?[\d\.]+");
                    for (int i = 0; i + 1 < numMatches.Count; i += 2)
                    {
                        if (float.TryParse(numMatches[i].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float px) &&
                            float.TryParse(numMatches[i + 1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float py))
                        {
                            minX = Math.Min(minX, px);
                            maxX = Math.Max(maxX, px);
                            minY = Math.Min(minY, py);
                            maxY = Math.Max(maxY, py);
                        }
                    }
                }

                // Quét đường cong <path> (bao gồm Bézier ngoặc đơn, mũi tên vector)
                var pMatches = Regex.Matches(svgStr, @"<path\s+[^>]*d=""([^""]+)""", RegexOptions.IgnoreCase);
                foreach (Match pm in pMatches)
                {
                    var numMatches = Regex.Matches(pm.Groups[1].Value, @"[-+]?[\d\.]+");
                    for (int i = 0; i + 1 < numMatches.Count; i += 2)
                    {
                        if (float.TryParse(numMatches[i].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float px) &&
                            float.TryParse(numMatches[i + 1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float py))
                        {
                            minX = Math.Min(minX, px);
                            maxX = Math.Max(maxX, px);
                            minY = Math.Min(minY, py);
                            maxY = Math.Max(maxY, py);
                        }
                    }
                }

                if (minX < maxX && minY < maxY && minX != float.MaxValue)
                {
                    // Auto-Trim chuẩn xác: Không cưỡng bức kẹp 0 để không bị cắt xén phần trên âm của dấu ngoặc/tích phân
                    float padX = settings.CropPadX > 0 ? settings.CropPadX : 10f;
                    float padY = settings.CropPadY > 0 ? settings.CropPadY : 6f;
                    float cropX = minX - padX;
                    float cropY = minY - padY;
                    float cropW = (maxX - minX) + padX * 2f;
                    float cropH = (maxY - minY) + padY * 2f;

                    // Tính font-size cơ sở chuẩn hóa (FS_base): Lọc bỏ các cỡ chữ giả định (<= 30) nếu có font thực tế lớn hơn
                    var targetFsList = alphaFs.Count > 0 ? alphaFs : allFs;
                    var significantFs = targetFsList.Where(f => f > 30).ToList();
                    if (significantFs.Count > 0) targetFsList = significantFs;
                    targetFsList.Sort();
                    float fsBase = targetFsList.Count > 0 ? targetFsList[targetFsList.Count / 2] : 200f;
                    if (fsBase <= 0f) fsBase = 200f;

                    // Tỷ lệ dòng chuẩn hóa: lineFactor = cropH / fsBase
                    float lineFactor = Math.Clamp(cropH / fsBase, 0.8f, 10.0f);
                    float scale = lineFactor > settings.MultiLineThreshold ? Math.Max(1.55f, lineFactor) : 1.0f;

                    var svgTagMatch = Regex.Match(svgStr, @"<svg\b[^>]*>", RegexOptions.IgnoreCase);
                    if (svgTagMatch.Success)
                    {
                        string svgTag = svgTagMatch.Value;
                        if (svgTag.Contains("viewBox="))
                            svgTag = Regex.Replace(svgTag, @"viewBox=""[^""]*""", $"viewBox=\"{cropX:F1} {cropY:F1} {cropW:F1} {cropH:F1}\"", RegexOptions.IgnoreCase);
                        else
                            svgTag = svgTag.Insert(4, $" viewBox=\"{cropX:F1} {cropY:F1} {cropW:F1} {cropH:F1}\"");

                        if (Regex.IsMatch(svgTag, @"(?<![\w-])width=""[^""]*""", RegexOptions.IgnoreCase))
                            svgTag = Regex.Replace(svgTag, @"(?<![\w-])width=""[^""]*""", $"width=\"{cropW:F1}\"", RegexOptions.IgnoreCase);
                        else
                            svgTag = svgTag.Insert(4, $" width=\"{cropW:F1}\"");

                        if (Regex.IsMatch(svgTag, @"(?<![\w-])height=""[^""]*""", RegexOptions.IgnoreCase))
                            svgTag = Regex.Replace(svgTag, @"(?<![\w-])height=""[^""]*""", $"height=\"{cropH:F1}\"", RegexOptions.IgnoreCase);
                        else
                            svgTag = svgTag.Insert(4, $" height=\"{cropH:F1}\"");

                        if (svgTag.Contains("data-scale="))
                            svgTag = Regex.Replace(svgTag, @"data-scale=""[^""]*""", $"data-scale=\"{scale:F2}\"", RegexOptions.IgnoreCase);
                        else
                            svgTag = svgTag.Insert(4, $" data-scale=\"{scale:F2}\"");

                        if (svgTag.Contains("data-line-factor="))
                            svgTag = Regex.Replace(svgTag, @"data-line-factor=""[^""]*""", $"data-line-factor=\"{lineFactor:F3}\"", RegexOptions.IgnoreCase);
                        else
                            svgTag = svgTag.Insert(4, $" data-line-factor=\"{lineFactor:F3}\"");

                        if (svgTag.Contains("data-fs-base="))
                            svgTag = Regex.Replace(svgTag, @"data-fs-base=""[^""]*""", $"data-fs-base=\"{fsBase:F1}\"", RegexOptions.IgnoreCase);
                        else
                            svgTag = svgTag.Insert(4, $" data-fs-base=\"{fsBase:F1}\"");

                        svgStr = svgStr.Substring(0, svgTagMatch.Index) + svgTag + svgStr.Substring(svgTagMatch.Index + svgTagMatch.Length);
                    }
                }
            }
            catch { }

            // 3. Chuẩn hóa W3C SVG tương thích 100% với thẻ <img> trên mọi trình duyệt:
            // 3a. Loại bỏ <!DOCTYPE> ngoài
            svgStr = Regex.Replace(svgStr, @"<!DOCTYPE[\s\S]*?>", "", RegexOptions.IgnoreCase);

            // 3b. Cưỡng bức bổ sung xmlns="http://www.w3.org/2000/svg"
            if (!svgStr.Contains("xmlns=\"http://www.w3.org/2000/svg\""))
            {
                svgStr = svgStr.Replace("<svg", "<svg xmlns=\"http://www.w3.org/2000/svg\"");
            }

            // 3c. Đảm bảo có style hiển thị co giãn chuẩn W3C
            if (!svgStr.Contains("display:inline-block"))
            {
                svgStr = svgStr.Replace("<svg", "<svg style=\"display:inline-block;vertical-align:-0.2em;max-width:100%;height:auto;\"");
            }

            // 3d. Bổ sung viewBox nếu có width/height mà chưa có viewBox
            var mW = Regex.Match(svgStr, @"width=""([0-9.]+)""");
            var mH = Regex.Match(svgStr, @"height=""([0-9.]+)""");
            if (mW.Success && mH.Success && !svgStr.Contains("viewBox"))
            {
                svgStr = svgStr.Replace("<svg", $"<svg viewBox=\"0 0 {mW.Groups[1].Value} {mH.Groups[1].Value}\"");
            }

            // 3e. Thoát 100% ký tự XML đặc thù (<, >, &) trong nội dung thẻ <text>...</text>
            // Đặc biệt là dấu '<' trong bất phương trình (như Câu 12) khiến SVG bị lỗi cú pháp XML (malformed XML) và trình duyệt từ chối render
            try
            {
                svgStr = Regex.Replace(svgStr, @"(<text\b[^>]*>)([\s\S]*?)(</text>)", m =>
                {
                    string open = m.Groups[1].Value;
                    string content = m.Groups[2].Value;
                    string close = m.Groups[3].Value;

                    // Thoát dấu & trần nếu không phải entity
                    content = Regex.Replace(content, @"&(?!(?:[a-zA-Z]+|#[0-9]+|#x[0-9a-fA-F]+);)", "&amp;");
                    // Thoát < và >
                    content = content.Replace("<", "&lt;").Replace(">", "&gt;");

                    return open + content + close;
                }, RegexOptions.IgnoreCase);
            }
            catch { }

            return svgStr;
        }

        /// <summary>
        /// Trình dịch nhị phân Vector WMF thuần C# (không phụ thuộc thư viện ngoài)
        /// </summary>
        public static string PureWmfToSvg(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 44) return string.Empty;

            int offset = 0;
            int minX = 0, minY = 0, width = 300, height = 100;

            // Kiểm tra Aldus Placeable Header (0x9AC6CDD7)
            uint magic = BitConverter.ToUInt32(bytes, 0);
            if (magic == 0x9AC6CDD7)
            {
                short left = BitConverter.ToInt16(bytes, 6);
                short top = BitConverter.ToInt16(bytes, 8);
                short right = BitConverter.ToInt16(bytes, 10);
                short bottom = BitConverter.ToInt16(bytes, 12);
                minX = left;
                minY = top;
                width = Math.Max(10, right - left);
                height = Math.Max(10, bottom - top);
                offset = 22;
            }

            offset += 18; // Bỏ qua WMF Standard Header

            int dominantFontSize = 384;
            for (int scanOff = offset; scanOff < bytes.Length - 6; )
            {
                uint szW = BitConverter.ToUInt32(bytes, scanOff);
                ushort fn = BitConverter.ToUInt16(bytes, scanOff + 4);
                if (szW == 0) break;
                if (fn == 0x02FB) // CreateFontIndirect
                {
                    short h = Math.Abs(BitConverter.ToInt16(bytes, scanOff + 6));
                    if (h > dominantFontSize) dominantFontSize = h;
                }
                scanOff += (int)(szW * 2);
            }

            var sbElements = new StringBuilder();
            var gdiTable = new Dictionary<int, WmfGdiObj>();
            string curFont = "serif";
            int curFontSize = dominantFontSize > 0 ? dominantFontSize : 384;
            bool curFontItalic = false;
            string curFontWeight = "normal";
            string curTextColor = "rgb(0,0,0)";
            int curPenWidth = 0;
            string curPenColor = "rgb(0,0,0)";
            string curBrushColor = "rgb(0,0,0)";
            int curX = 0, curY = 0;
            ushort textAlign = 0;

            int GetFreeSlot()
            {
                int slot = 0;
                while (gdiTable.ContainsKey(slot)) slot++;
                return slot;
            }

            while (offset < bytes.Length - 6)
            {
                uint sizeWords = BitConverter.ToUInt32(bytes, offset);
                ushort func = BitConverter.ToUInt16(bytes, offset + 4);
                if (sizeWords == 0 || func == 0) break;

                switch (func)
                {
                    case 0x02FB: // CreateFontIndirect
                        {
                            short h = Math.Abs(BitConverter.ToInt16(bytes, offset + 6));
                            short weight = BitConverter.ToInt16(bytes, offset + 14);
                            bool italic = bytes[offset + 16] != 0;
                            int faceOffset = offset + 24;
                            int nullIdx = 0;
                            while (nullIdx < 32 && faceOffset + nullIdx < bytes.Length && bytes[faceOffset + nullIdx] != 0)
                            {
                                nullIdx++;
                            }
                            string face = Encoding.ASCII.GetString(bytes, faceOffset, nullIdx);
                            int slot = GetFreeSlot();
                            gdiTable[slot] = new WmfGdiFont
                            {
                                Face = string.IsNullOrWhiteSpace(face) ? "serif" : face,
                                Size = h > 0 ? h : 14,
                                Italic = italic,
                                Weight = weight >= 700 ? "bold" : "normal"
                            };
                            break;
                        }
                    case 0x02FA: // CreatePenIndirect
                        {
                            ushort style = BitConverter.ToUInt16(bytes, offset + 6);
                            short pw = BitConverter.ToInt16(bytes, offset + 8);
                            uint colorRef = BitConverter.ToUInt32(bytes, offset + 12);
                            byte r = (byte)(colorRef & 0xFF);
                            byte g = (byte)((colorRef >> 8) & 0xFF);
                            byte b = (byte)((colorRef >> 16) & 0xFF);
                            int slot = GetFreeSlot();
                            gdiTable[slot] = new WmfGdiPen
                            {
                                Style = style,
                                Width = pw,
                                Color = $"rgb({r},{g},{b})"
                            };
                            break;
                        }
                    case 0x02FC: // CreateBrushIndirect
                        {
                            ushort style = BitConverter.ToUInt16(bytes, offset + 6);
                            uint colorRef = BitConverter.ToUInt32(bytes, offset + 8);
                            byte r = (byte)(colorRef & 0xFF);
                            byte g = (byte)((colorRef >> 8) & 0xFF);
                            byte b = (byte)((colorRef >> 16) & 0xFF);
                            int slot = GetFreeSlot();
                            gdiTable[slot] = new WmfGdiBrush
                            {
                                Style = style,
                                Color = $"rgb({r},{g},{b})"
                            };
                            break;
                        }
                    case 0x01F7: // CreatePatternBrush
                    case 0x01F9: // CreatePalette
                    case 0x06FE: // CreateBitmapIndirect
                    case 0x06FF: // CreateBitmap
                    case 0x0142: // CreateRegion
                        {
                            int slot = GetFreeSlot();
                            gdiTable[slot] = new WmfGdiDummy();
                            break;
                        }
                    case 0x01F0: // DeleteObject
                        {
                            ushort objId = BitConverter.ToUInt16(bytes, offset + 6);
                            gdiTable.Remove(objId);
                            break;
                        }
                    case 0x012D: // SelectObject
                        {
                            ushort objId = BitConverter.ToUInt16(bytes, offset + 6);
                            if (gdiTable.TryGetValue(objId, out var obj))
                            {
                                if (obj is WmfGdiFont f)
                                {
                                    curFont = f.Face;
                                    curFontSize = f.Size;
                                    curFontItalic = f.Italic;
                                    curFontWeight = f.Weight;
                                }
                                else if (obj is WmfGdiPen p)
                                {
                                    curPenWidth = p.Width;
                                    curPenColor = p.Color;
                                }
                                else if (obj is WmfGdiBrush br)
                                {
                                    curBrushColor = br.Color;
                                }
                            }
                            break;
                        }
                    case 0x012E: // SetTextAlign
                        {
                            textAlign = BitConverter.ToUInt16(bytes, offset + 6);
                            break;
                        }
                    case 0x0209: // SetTextColor
                        {
                            uint colorRef = BitConverter.ToUInt32(bytes, offset + 6);
                            byte r = (byte)(colorRef & 0xFF);
                            byte g = (byte)((colorRef >> 8) & 0xFF);
                            byte b = (byte)((colorRef >> 16) & 0xFF);
                            curTextColor = $"rgb({r},{g},{b})";
                            break;
                        }
                    case 0x0214: // MoveTo
                        {
                            curY = BitConverter.ToInt16(bytes, offset + 6);
                            curX = BitConverter.ToInt16(bytes, offset + 8);
                            break;
                        }
                    case 0x0213: // LineTo
                        {
                            short ey = BitConverter.ToInt16(bytes, offset + 6);
                            short ex = BitConverter.ToInt16(bytes, offset + 8);
                            float sw = curPenWidth > 0 ? curPenWidth : Math.Max(14f, curFontSize * 0.045f);
                            string swStr = sw.ToString("0.##", CultureInfo.InvariantCulture);
                            string strokeCol = !string.IsNullOrEmpty(curPenColor) && curPenColor != "rgb(0,0,0)" ? curPenColor : curTextColor;
                            sbElements.AppendLine($"<line x1=\"{curX}\" y1=\"{curY}\" x2=\"{ex}\" y2=\"{ey}\" stroke=\"{strokeCol}\" stroke-width=\"{swStr}\" stroke-linecap=\"round\" />");
                            curX = ex;
                            curY = ey;
                            break;
                        }
                    case 0x0325: // Polyline
                        {
                            short count = BitConverter.ToInt16(bytes, offset + 6);
                            var pts = new List<string>();
                            for (int i = 0; i < count && offset + 8 + i * 4 + 3 < bytes.Length; i++)
                            {
                                short px = BitConverter.ToInt16(bytes, offset + 8 + i * 4);
                                short py = BitConverter.ToInt16(bytes, offset + 10 + i * 4);
                                pts.Add($"{px},{py}");
                            }
                            if (pts.Count > 0)
                            {
                                float sw = curPenWidth > 0 ? curPenWidth : Math.Max(14f, curFontSize * 0.045f);
                                string swStr = sw.ToString("0.##", CultureInfo.InvariantCulture);
                                string strokeCol = !string.IsNullOrEmpty(curPenColor) && curPenColor != "rgb(0,0,0)" ? curPenColor : curTextColor;
                                sbElements.AppendLine($"<polyline points=\"{string.Join(" ", pts)}\" fill=\"none\" stroke=\"{strokeCol}\" stroke-width=\"{swStr}\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />");
                            }
                            break;
                        }
                    case 0x0626: // META_ESCAPE (chứa MathType MTEF / OLE metadata - KHÔNG PHẢI hình học)
                        break;
                    case 0x0324: // Polygon (đa giác đặc như căn thức MathType, ký hiệu hình học)
                        {
                            short count = BitConverter.ToInt16(bytes, offset + 6);
                            if (count >= 3 && offset + 8 + count * 4 <= bytes.Length)
                            {
                                var pts = new List<string>();
                                short pMinX = short.MaxValue, pMaxX = short.MinValue;
                                short pMinY = short.MaxValue, pMaxY = short.MinValue;
                                for (int i = 0; i < count; i++)
                                {
                                    short px = BitConverter.ToInt16(bytes, offset + 8 + i * 4);
                                    short py = BitConverter.ToInt16(bytes, offset + 10 + i * 4);
                                    pts.Add($"{px},{py}");
                                    if (px < pMinX) pMinX = px;
                                    if (px > pMaxX) pMaxX = px;
                                    if (py < pMinY) pMinY = py;
                                    if (py > pMaxY) pMaxY = py;
                                }
                                int polyW = pMaxX - pMinX;
                                int polyH = pMaxY - pMinY;
                                bool isBackgroundPill = (polyW >= width * 0.92f && polyH >= height * 0.92f);
                                if (!isBackgroundPill)
                                {
                                    string fillCol = !string.IsNullOrEmpty(curBrushColor) && curBrushColor != "rgb(255,255,255)"
                                        ? curBrushColor
                                        : curTextColor;
                                    sbElements.AppendLine($"<polygon class=\"math-shape\" points=\"{string.Join(" ", pts)}\" fill=\"{fillCol}\" stroke=\"{fillCol}\" stroke-width=\"1\" />");
                                }
                            }
                            break;
                        }
                    case 0x0538: // META_POLYPOLYGON (bỏ qua hoàn toàn)
                        break;
                    case 0x0A32: // ExtTextOut
                        {
                            short y = BitConverter.ToInt16(bytes, offset + 6);
                            short x = BitConverter.ToInt16(bytes, offset + 8);
                            short count = BitConverter.ToInt16(bytes, offset + 10);
                            short opts = BitConverter.ToInt16(bytes, offset + 12);
                            int strOffset = offset + 14;
                            if ((opts & 0x0006) != 0) strOffset += 8;

                            if (count > 0 && strOffset + count <= bytes.Length)
                            {
                                int dxOffset = strOffset + count + (count % 2);
                                int recEnd = offset + (int)(sizeWords * 2);

                                bool updateCp = ((textAlign & 0x0001) != 0) || (x == 0 && y == 0);
                                int startX = updateCp ? curX : x;
                                int startY = updateCp ? curY : y;
                                int charX = startX;

                                bool isSymbol = curFont.Contains("Symbol", StringComparison.OrdinalIgnoreCase);
                                string displayFont = isSymbol
                                    ? "'Times New Roman', 'Cambria Math', serif"
                                    : $"'{curFont}', serif";
                                string fontStyle = curFontItalic ? "italic" : "normal";

                                for (int i = 0; i < count; i++)
                                {
                                    byte b = bytes[strOffset + i];
                                    char c = (char)b;
                                    string glyph = null;

                                    if (isSymbol)
                                    {
                                        // Tier 1: Dynamic Math Dictionary Service
                                        glyph = DynamicMathDictionaryService.Instance.ResolveSymbolGlyph(curFont, c, string.Empty);
                                        if (string.IsNullOrEmpty(glyph))
                                        {
                                            glyph = DynamicMathDictionaryService.Instance.ResolveGreekLetter(curFont, c, string.Empty);
                                        }

                                        // Tier 3: Hardcoded Fail-Safe Fallbacks
                                        if (string.IsNullOrEmpty(glyph) && FallbackSymbolGlyphMap.TryGetValue(c, out var mapped))
                                        {
                                            glyph = mapped;
                                        }
                                        if (string.IsNullOrEmpty(glyph) && FallbackGreekLettersMap.TryGetValue(c, out var greekFallback))
                                        {
                                            glyph = greekFallback;
                                        }
                                    }

                                    if (string.IsNullOrEmpty(glyph))
                                    {
                                        // Thoát các ký tự XML đặc thù
                                        switch (c)
                                        {
                                            case '&': glyph = "&amp;"; break;
                                            case '<': glyph = "&lt;"; break;
                                            case '>': glyph = "&gt;"; break;
                                            case '"': glyph = "&quot;"; break;
                                            default: glyph = c.ToString(); break;
                                        }
                                    }

                                    sbElements.AppendLine($"<text x=\"{charX}\" y=\"{startY}\" font-family=\"{displayFont}\" font-size=\"{curFontSize}\" font-style=\"{fontStyle}\" font-weight=\"{curFontWeight}\" fill=\"{curTextColor}\">{glyph}</text>");

                                    // Tịnh tiến vị trí ký tự tiếp theo theo khoảng cách Dx trong WMF
                                    short dx = 0;
                                    if (dxOffset + i * 2 + 1 < recEnd)
                                    {
                                        dx = BitConverter.ToInt16(bytes, dxOffset + i * 2);
                                    }

                                    if (dx != 0)
                                    {
                                        charX += dx;
                                    }
                                    else
                                    {
                                        charX += (int)(curFontSize * 0.55f);
                                    }
                                }

                                if (updateCp)
                                {
                                    curX = charX;
                                    curY = startY;
                                }
                            }
                            break;
                        }
                }

                offset += (int)(sizeWords * 2);
            }

            if (sbElements.Length == 0) return string.Empty;

            string rawSvg = $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{minX} {minY} {width} {height}\" style=\"display:inline-block;vertical-align:-0.2em;max-width:100%;height:auto;\">\n{sbElements}\n</svg>";
            return SanitizeMathTypeSvg(rawSvg);
        }

        // [Tier-3 Fail-Safe Fallback]: Bảng tra cứu tĩnh LaTeX dự phòng tối thiểu khi DynamicMathDictionaryService chưa khởi tạo
        private static readonly Dictionary<char, string> FallbackSymbolToLatexMap = new()
        {
            { '\u00A2', "'" },           // 0xA2 -> Prime (A', B', y')
            { '\u00B1', "\\pm " },       // 0xB1 -> \pm
            { '\u00A5', "\\infty " },    // 0xA5 -> \infty
            { '\u00CE', "\\in " },       // 0xCE -> \in
            { '\u00CF', "\\notin " },    // 0xCF -> \notin
            { '\u00C8', "\\cup " },      // 0xC8 -> \cup
            { '\u00C7', "\\cap " },      // 0xC7 -> \cap
            { '\u00C6', "\\emptyset " }, // 0xC6 -> \emptyset
            { '\u00CC', "\\subset " },   // 0xCC -> \subset
            { '\u00CD', "\\supset " },   // 0xCD -> \supset
            { '\u00DA', "\\subseteq " }, // 0xDA -> \subseteq
            { '\u00A3', "\\le " },       // 0xA3 -> \le
            { '\u00B3', "\\ge " },       // 0xB3 -> \ge
            { '\u00B9', "\\ne " },       // 0xB9 -> \ne
            { '\u00AE', "\\rightarrow " }, // 0xAE -> \rightarrow
            { '\u00AC', "\\leftarrow " },  // 0xAC -> \leftarrow
            { '\u00AB', "\\leftrightarrow " }, // 0xAB -> \leftrightarrow
            { '\u00DE', "\\Rightarrow " }, // 0xDE -> \Rightarrow
            { '\u00DB', "\\Leftrightarrow " }, // 0xDB -> \Leftrightarrow
            { '\u00D7', "\\cdot " },     // 0xD7 -> \cdot
            { '\u00B7', "\\cdot " },     // 0xB7 -> \cdot
            { '\u00B0', "^\\circ " },    // 0xB0 -> ^\circ
            { '\u0082', "\\setminus " }, // 0x82 -> \setminus (như R \ {2})
            { '\u00C2', "\\angle " },    // 0xC2 -> \angle
            { '@', "\\cong " },          // 0x40 -> \cong
            { '~', "\\sim " },           // 0x7E -> \sim
            { '"', "\\forall " },        // 0x22 -> \forall
            { '$', "\\exists " }         // 0x24 -> \exists
        };

        // [Tier-3 Fail-Safe Fallback]: Bảng tra cứu chữ cái Hy Lạp LaTeX dự phòng tối thiểu
        private static readonly Dictionary<char, string> FallbackGreekToLatexMap = new()
        {
            { 'a', "\\alpha " },
            { 'b', "\\beta " },
            { 'g', "\\gamma " },
            { 'd', "\\delta " },
            { 'e', "\\epsilon " },
            { 'z', "\\zeta " },
            { 'h', "\\eta " },
            { 'q', "\\theta " },
            { 'i', "\\iota " },
            { 'k', "\\kappa " },
            { 'l', "\\lambda " },
            { 'm', "\\mu " },
            { 'n', "\\nu " },
            { 'x', "\\xi " },
            { 'o', "o" },
            { 'p', "\\pi " },
            { 'r', "\\rho " },
            { 's', "\\sigma " },
            { 't', "\\tau " },
            { 'u', "\\upsilon " },
            { 'f', "\\phi " },
            { 'j', "\\phi " },
            { 'c', "\\chi " },
            { 'y', "\\psi " },
            { 'w', "\\omega " },
            { 'D', "\\Delta " },
            { 'G', "\\Gamma " },
            { 'L', "\\Lambda " },
            { 'P', "\\Pi " },
            { 'S', "\\Sigma " },
            { 'W', "\\Omega " },
            { 'Q', "\\Theta " }
        };

        /// <summary>
        /// [AI Intelligent WMF Simplifier v8.25]
        /// Nhận diện và chuyển hóa thông minh các WMF đơn giản sang KaTeX Inline ($...$) siêu nhẹ:
        /// 1. Biến đơn, biểu thức 1 dòng: $x, y$, điểm $A(1;2;3)$, hàm số $y=f(x)$, mặt phẳng $Oxyz$, đoạn thẳng $MN$.
        /// 2. Vector đơn giản: $\vec{u}, \vec{v}, \vec{a}, \vec{b}, \vec{i}, \vec{j}, \vec{k}, \overrightarrow{AB}, \overrightarrow{MN}, \overrightarrow{OM}$.
        /// 3. Phân số số học đơn giản: $\frac{1}{2}, \frac{3}{4}, \frac{17}{8}, \frac{a}{c}, \frac{-1}{2}$.
        /// 4. Xử lý triệt để ký tự khoảng trắng không ngắt (0xA0) và ký tự toán học mở rộng.
        /// Chỉ giữ lại WMF phức tạp (đa tầng: phân số hàm số, ma trận, tích phân có cận, hình vẽ đồ thị) sang Vector SVG.
        /// 100% Dynamic qua DynamicMathDictionaryService.Instance (Zero-Code Extension).
        /// </summary>
        public static bool TryExtractSimpleMathText(byte[] bytes, out string inlineMathText)
        {
            inlineMathText = string.Empty;
            if (bytes == null || bytes.Length < 44) return false;

            // [Tier 0: MTEF Native AST Decoder - World-Class Standard]
            // Ưu tiên giải mã trực tiếp Cây Cú Pháp Gốc (Abstract Syntax Tree) nhị phân MTEF v5
            // Chuyển đổi chính xác 100% mọi cấu trúc phức tạp: Phân số, căn thức, ngoặc cao, tích phân, vector...
            if (Docx.MtefToLatexConverter.TryConvert(bytes, out var mtefText) && !string.IsNullOrWhiteSpace(mtefText))
            {
                inlineMathText = mtefText;
                return true;
            }

            try
            {
                var settings = DynamicMathDictionaryService.Instance.GetEngineSettings() ?? new AegisQuiz.Application.DTOs.MathEngineTypographyDto();

                int offset = 0;
                uint magic = BitConverter.ToUInt32(bytes, 0);
                if (magic == 0x9AC6CDD7)
                {
                    offset = 22;
                }
                offset += 18; // Bỏ qua WMF Standard Header

                var lines = new List<(int x1, int y1, int x2, int y2)>();
                int polyCount = 0;
                int complexShapes = 0;
                int curX = 0, curY = 0;
                ushort textAlign = 0;
                string curFont = "serif";
                int curFontSize = 14;

                var gdiTable = new Dictionary<int, WmfGdiObj>();
                int GetFreeSlot()
                {
                    int slot = 0;
                    while (gdiTable.ContainsKey(slot)) slot++;
                    return slot;
                }

                var textItems = new List<WmfSimpleTextItem>();

                while (offset < bytes.Length - 6)
                {
                    uint sizeWords = BitConverter.ToUInt32(bytes, offset);
                    ushort func = BitConverter.ToUInt16(bytes, offset + 4);
                    if (sizeWords == 0 || func == 0) break;

                    switch (func)
                    {
                        case 0x0213: // LineTo
                            {
                                short ey = BitConverter.ToInt16(bytes, offset + 6);
                                short ex = BitConverter.ToInt16(bytes, offset + 8);
                                lines.Add((curX, curY, ex, ey));
                                curX = ex;
                                curY = ey;
                                break;
                            }
                        case 0x0325: // Polyline
                            {
                                short count = BitConverter.ToInt16(bytes, offset + 6);
                                if (count >= 2)
                                {
                                    for (int i = 0; i < count - 1 && offset + 8 + (i + 1) * 4 + 3 < bytes.Length; i++)
                                    {
                                        short px1 = BitConverter.ToInt16(bytes, offset + 8 + i * 4);
                                        short py1 = BitConverter.ToInt16(bytes, offset + 10 + i * 4);
                                        short px2 = BitConverter.ToInt16(bytes, offset + 8 + (i + 1) * 4);
                                        short py2 = BitConverter.ToInt16(bytes, offset + 10 + (i + 1) * 4);
                                        lines.Add((px1, py1, px2, py2));
                                    }
                                }
                                break;
                            }
                        case 0x0324: // Polygon
                        case 0x0538: // PolyPolygon
                            polyCount++;
                            break;
                        case 0x081A: // BitBlt
                        case 0x0B41: // StretchBlt
                        case 0x0940: // StretchDIBits
                            complexShapes++;
                            break;
                        case 0x02FB: // CreateFontIndirect
                            {
                                short h = Math.Abs(BitConverter.ToInt16(bytes, offset + 6));
                                short weight = BitConverter.ToInt16(bytes, offset + 14);
                                bool italic = bytes[offset + 16] != 0;
                                int faceOffset = offset + 24;
                                int nullIdx = 0;
                                while (nullIdx < 32 && faceOffset + nullIdx < bytes.Length && bytes[faceOffset + nullIdx] != 0)
                                {
                                    nullIdx++;
                                }
                                string face = Encoding.ASCII.GetString(bytes, faceOffset, nullIdx);
                                int slot = GetFreeSlot();
                                gdiTable[slot] = new WmfGdiFont
                                {
                                    Face = string.IsNullOrWhiteSpace(face) ? "serif" : face,
                                    Size = h > 0 ? h : 14,
                                    Italic = italic,
                                    Weight = weight >= 700 ? "bold" : "normal"
                                };
                                break;
                            }
                        case 0x02FA: // CreatePenIndirect
                            {
                                ushort style = BitConverter.ToUInt16(bytes, offset + 6);
                                short pw = BitConverter.ToInt16(bytes, offset + 8);
                                uint colorRef = BitConverter.ToUInt32(bytes, offset + 12);
                                byte r = (byte)(colorRef & 0xFF);
                                byte g = (byte)((colorRef >> 8) & 0xFF);
                                byte b = (byte)((colorRef >> 16) & 0xFF);
                                int slot = GetFreeSlot();
                                gdiTable[slot] = new WmfGdiPen { Style = style, Width = pw, Color = $"rgb({r},{g},{b})" };
                                break;
                            }
                        case 0x02FC: // CreateBrushIndirect
                            {
                                ushort style = BitConverter.ToUInt16(bytes, offset + 6);
                                uint colorRef = BitConverter.ToUInt32(bytes, offset + 8);
                                byte r = (byte)(colorRef & 0xFF);
                                byte g = (byte)((colorRef >> 8) & 0xFF);
                                byte b = (byte)((colorRef >> 16) & 0xFF);
                                int slot = GetFreeSlot();
                                gdiTable[slot] = new WmfGdiBrush { Style = style, Color = $"rgb({r},{g},{b})" };
                                break;
                            }
                        case 0x01F7: // CreatePatternBrush
                        case 0x01F9: // CreatePalette
                        case 0x06FE: // CreateBitmapIndirect
                        case 0x06FF: // CreateBitmap
                        case 0x0142: // CreateRegion
                            {
                                int slot = GetFreeSlot();
                                gdiTable[slot] = new WmfGdiDummy();
                                break;
                            }
                        case 0x01F0: // DeleteObject
                            {
                                ushort objId = BitConverter.ToUInt16(bytes, offset + 6);
                                gdiTable.Remove(objId);
                                break;
                            }
                        case 0x012D: // SelectObject
                            {
                                ushort objId = BitConverter.ToUInt16(bytes, offset + 6);
                                if (gdiTable.TryGetValue(objId, out var obj))
                                {
                                    if (obj is WmfGdiFont f)
                                    {
                                        curFont = f.Face;
                                        curFontSize = f.Size;
                                    }
                                }
                                break;
                            }
                        case 0x012E: // SetTextAlign
                            textAlign = BitConverter.ToUInt16(bytes, offset + 6);
                            break;
                        case 0x0214: // MoveTo
                            curY = BitConverter.ToInt16(bytes, offset + 6);
                            curX = BitConverter.ToInt16(bytes, offset + 8);
                            break;
                        case 0x0A32: // ExtTextOut
                            {
                                short y = BitConverter.ToInt16(bytes, offset + 6);
                                short x = BitConverter.ToInt16(bytes, offset + 8);
                                short count = BitConverter.ToInt16(bytes, offset + 10);
                                short opts = BitConverter.ToInt16(bytes, offset + 12);
                                int strOffset = offset + 14;
                                if ((opts & 0x0006) != 0) strOffset += 8;

                                if (count > 0 && strOffset + count <= bytes.Length)
                                {
                                    int dxOffset = strOffset + count + (count % 2);
                                    int recEnd = offset + (int)(sizeWords * 2);

                                    bool updateCp = ((textAlign & 0x0001) != 0) || (x == 0 && y == 0);
                                    int startX = updateCp ? curX : x;
                                    int startY = updateCp ? curY : y;
                                    int charX = startX;

                                    bool isSymbol = curFont.Contains("Symbol", StringComparison.OrdinalIgnoreCase);
                                    bool isMtExtra = curFont.Contains("Extra", StringComparison.OrdinalIgnoreCase);

                                    for (int i = 0; i < count; i++)
                                    {
                                        byte b = bytes[strOffset + i];
                                        char c = (char)b;
                                        string latex = null;

                                        if (isMtExtra)
                                        {
                                            if (c == 'u' || c == 'r')
                                            {
                                                latex = c.ToString();
                                            }
                                        }
                                        else if (isSymbol)
                                        {
                                            // Tier 1: Dynamic Math Dictionary Service
                                            latex = DynamicMathDictionaryService.Instance.ResolveSymbolLatex(curFont, c, string.Empty);
                                            if (string.IsNullOrEmpty(latex))
                                            {
                                                latex = DynamicMathDictionaryService.Instance.ResolveGreekLatex(curFont, c, string.Empty);
                                            }

                                            // Tier 3: Hardcoded Fail-Safe Fallbacks
                                            if (string.IsNullOrEmpty(latex) && FallbackSymbolToLatexMap.TryGetValue(c, out var symVal))
                                                latex = symVal;
                                            if (string.IsNullOrEmpty(latex) && FallbackGreekToLatexMap.TryGetValue(c, out var grkVal))
                                                latex = grkVal;

                                            if (string.IsNullOrEmpty(latex) && c >= 32 && c <= 126 && "+-=<>()[]{}/*:;,.".Contains(c))
                                            {
                                                latex = c switch
                                                {
                                                    '{' => "\\{",
                                                    '}' => "\\}",
                                                    _ => c.ToString()
                                                };
                                            }
                                        }
                                        else
                                        {
                                            // Non-symbol font: Latin, số, ký hiệu toán thông thường
                                            if (b == 0xA0)
                                                latex = " "; // Khoảng trắng không ngắt
                                            else if (b == 0xA2)
                                                latex = "'";
                                            else if (b == 0x82)
                                                latex = "\\setminus ";
                                            else if (b == 0x96 || b == 0x97)
                                                latex = "-";
                                            else if (b == 0x85)
                                                latex = "\\dots ";
                                            else if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                                            {
                                                // Chữ cái Latin và chữ số trong font thường (Times New Roman, Arial...) luôn là chính nó.
                                                // Tuyệt đối không tra sang ký tự Hy Lạp (ngăn ngừa D biến thành \Delta)
                                                latex = c.ToString();
                                            }
                                            else
                                            {
                                                // Tier 1: Dynamic Math Dictionary Service
                                                latex = DynamicMathDictionaryService.Instance.ResolveSymbolLatex(curFont, c, string.Empty);

                                                // Tier 3: Hardcoded Fail-Safe Fallbacks
                                                if (string.IsNullOrEmpty(latex) && FallbackSymbolToLatexMap.TryGetValue(c, out var symVal))
                                                    latex = symVal;

                                                if (string.IsNullOrEmpty(latex) && c >= 32 && c <= 126)
                                                {
                                                    latex = c switch
                                                    {
                                                        '{' => "\\{",
                                                        '}' => "\\}",
                                                        '%' => "\\%",
                                                        '$' => "\\$",
                                                        _ => c.ToString()
                                                    };
                                                }
                                            }
                                        }

                                        // Nếu gặp ký tự lạ không thể nhận diện được -> Coi là WMF phức tạp để giữ nguyên SVG an toàn
                                        if (string.IsNullOrEmpty(latex))
                                        {
                                            return false;
                                        }

                                        short dx = 0;
                                        if (dxOffset + i * 2 + 1 < recEnd)
                                        {
                                            dx = BitConverter.ToInt16(bytes, dxOffset + i * 2);
                                        }
                                        int advanceW = dx > 0 ? dx : (int)(curFontSize * 0.55f);
                                        int visualW = Math.Min(advanceW, (int)(curFontSize * 0.75f));

                                        textItems.Add(new WmfSimpleTextItem
                                        {
                                            Text = latex,
                                            X = charX,
                                            Y = startY,
                                            Width = visualW,
                                            FontSize = curFontSize,
                                            IsSymbol = isSymbol,
                                            IsMtExtra = isMtExtra
                                        });

                                        charX += advanceW;
                                    }

                                    if (updateCp)
                                    {
                                        curX = charX;
                                        curY = startY;
                                    }
                                }
                                break;
                            }
                    }

                    offset += (int)(sizeWords * 2);
                }

                // Nếu có hình vẽ đa giác phức tạp hoặc quá nhiều đường nét -> Giữ nguyên Vector SVG
                if (complexShapes > 0 || lines.Count > 18)
                {
                    return false;
                }

                int maxSimpleChars = settings.SimplifierMaxSimpleCharCount > 0 ? settings.SimplifierMaxSimpleCharCount : 150;
                if (textItems.Count == 0 || textItems.Count > maxSimpleChars)
                {
                    return false;
                }

                // [P2 Dynamic Declarative Structural Rules]: Ưu tiên thử qua Hệ quy tắc cấu trúc động nạp từ DB/JSON
                if (DynamicMathDictionaryService.Instance.TryMatchStructuralRule(
                        lines, textItems, polyCount, complexShapes, curFontSize > 0 ? curFontSize : 14, out var ruleMatchedItems))
                {
                    textItems = ruleMatchedItems;
                    lines.Clear(); // Các đường nét hình học đã được quy tắc cấu trúc hấp thu thành công
                    if (textItems.Count == 1 && (textItems[0].Text.StartsWith("\\") || textItems[0].Text.Contains("{")))
                    {
                        inlineMathText = $"${textItems[0].Text}$";
                        return true;
                    }
                }

                // 1. Nhận diện và gom Mũi Tên Vector (MT Extra 'u...r' và 'r' đơn lẻ)
                var vectorItems = textItems.Where(t => t.IsMtExtra && (t.Text == "u" || t.Text == "r")).ToList();
                var normalItems = textItems.Where(t => !t.IsMtExtra).ToList();

                if (vectorItems.Count > 0 && normalItems.Count > 0)
                {
                    var arrowGroups = new List<List<WmfSimpleTextItem>>();
                    var currentGroup = new List<WmfSimpleTextItem>();
                    foreach (var v in vectorItems.OrderBy(t => t.X))
                    {
                        currentGroup.Add(v);
                        if (v.Text == "r")
                        {
                            arrowGroups.Add(new List<WmfSimpleTextItem>(currentGroup));
                            currentGroup.Clear();
                        }
                    }

                    var resolvedVectors = new List<(List<WmfSimpleTextItem> Under, WmfSimpleTextItem Replacement)>();
                    foreach (var grp in arrowGroups)
                    {
                        int minVx = grp.Min(t => t.X);
                        int maxVx = grp.Max(t => t.X + t.Width);
                        double avgVy = grp.Average(t => t.Y);
                        double vCenterX = (minVx + maxVx) / 2.0;
                        double vSpan = maxVx - minVx;

                        // Chỉ tìm trong tập các ký tự chữ cái (a-zA-Z') nằm bên dưới mũi tên (Y > avgVy)
                        // Tuyệt đối không để lẫn số hoặc toán tử (+, -, =)
                        var letterCandidates = normalItems
                            .Where(t => t.Y > avgVy && Regex.IsMatch(t.Text, @"^[a-zA-Z\']+$"))
                            .OrderBy(t => t.X)
                            .ToList();

                        var under = letterCandidates
                            .Where(t =>
                            {
                                double tCenter = t.X + t.Width / 2.0;
                                return Math.Abs(tCenter - vCenterX) <= (vSpan + t.Width) / 2.0;
                            })
                            .OrderBy(t => t.X)
                            .ToList();

                        if (under.Count == 0 && letterCandidates.Count > 0)
                        {
                            var nearest = letterCandidates.MinBy(t => Math.Abs((t.X + t.Width / 2.0) - vCenterX));
                            if (nearest != null && Math.Abs((nearest.X + nearest.Width / 2.0) - vCenterX) < (grp[0].FontSize * 0.8))
                            {
                                under.Add(nearest);
                            }
                        }

                        if (under.Count > 0 && under.Count <= 3)
                        {
                            string underText = string.Join("", under.Select(u => u.Text));
                            string vecLatex = under.Count > 1 ? $"\\overrightarrow{{{underText}}}" : $"\\vec{{{underText}}}";
                            resolvedVectors.Add((under, new WmfSimpleTextItem
                            {
                                Text = vecLatex,
                                X = under[0].X,
                                Y = under[0].Y,
                                Width = under.Sum(u => u.Width),
                                FontSize = under[0].FontSize,
                                IsSymbol = false,
                                IsMtExtra = false
                            }));
                        }
                    }

                    if (resolvedVectors.Count > 0)
                    {
                        var usedUnder = new HashSet<WmfSimpleTextItem>();
                        var newNormal = new List<WmfSimpleTextItem>();
                        foreach (var (underList, rep) in resolvedVectors)
                        {
                            foreach (var u in underList) usedUnder.Add(u);
                            newNormal.Add(rep);
                        }
                        foreach (var n in normalItems)
                        {
                            if (!usedUnder.Contains(n))
                                newNormal.Add(n);
                        }
                        normalItems = newNormal.OrderBy(t => t.X).ToList();
                        textItems = normalItems;
                    }
                    else
                    {
                        textItems = normalItems;
                    }
                }
                else
                {
                    textItems = normalItems;
                }

                if (textItems.Count == 0) return false;

                bool hasStructuralMath = false;

                // 2. [Giai Đoạn 3] Nhận diện và gom Căn Thức (Radical Expressions: \sqrt{...})
                var roofCandidates = lines
                    .Where(l => Math.Abs(l.y1 - l.y2) <= 8 && Math.Abs(l.x2 - l.x1) >= 15)
                    .OrderByDescending(l => Math.Abs(l.x2 - l.x1))
                    .ToList();

                foreach (var roofCandidate in roofCandidates)
                {
                    int roofMinX = Math.Min(roofCandidate.x1, roofCandidate.x2);
                    int roofMaxX = Math.Max(roofCandidate.x1, roofCandidate.x2);
                    double roofY = (roofCandidate.y1 + roofCandidate.y2) / 2.0;

                    // Kiểm tra xem có nét vấu căn bên trái hoặc ký hiệu căn đứng trước
                    var otherLines = lines.Where(l => !(l.x1 == roofCandidate.x1 && l.y1 == roofCandidate.y1 && l.x2 == roofCandidate.x2 && l.y2 == roofCandidate.y2)).ToList();
                    bool hasVTick = otherLines.Any(l => Math.Min(l.x1, l.x2) <= roofMinX + 35 && Math.Abs(l.y1 - l.y2) >= 8);
                    var radSignItem = textItems.FirstOrDefault(t => (t.Text.Contains("\\sqrt") || t.Text == "√" || t.Text == "\u00D6") && Math.Abs(t.X + t.Width - roofMinX) <= 40);

                    if (hasVTick || radSignItem != null)
                    {
                        var radItems = textItems.Where(t => t != radSignItem && t.Y > roofY - 12 && t.X >= roofMinX - 35 && (t.X + Math.Min(t.Width, (int)(t.FontSize * 0.75f)) / 2) <= roofMaxX + 45).OrderBy(t => t.X).ToList();
                        if (radItems.Count > 0)
                        {
                            string radLatex = FormatMathSubExpression(radItems, textItems[0].FontSize);
                            string sqrtLatex = $"\\sqrt{{{radLatex}}}";

                            var radSet = new HashSet<WmfSimpleTextItem>(radItems);
                            if (radSignItem != null) radSet.Add(radSignItem);
                            var remainingText = textItems.Where(t => !radSet.Contains(t)).ToList();

                            int baseBaselineY = remainingText.Count > 0 ? remainingText[0].Y : (radItems.Count > 0 ? radItems[0].Y : (int)roofY);
                            remainingText.Add(new WmfSimpleTextItem
                            {
                                Text = sqrtLatex,
                                X = radSignItem != null ? radSignItem.X : roofMinX - 10,
                                Y = baseBaselineY,
                                Width = Math.Max(20, roofMaxX - (radSignItem != null ? radSignItem.X : roofMinX)),
                                FontSize = textItems[0].FontSize,
                                IsSymbol = false,
                                IsMtExtra = false
                            });
                            textItems = remainingText.OrderBy(t => t.X).ToList();

                            // Loại bỏ đường nóc căn và các đường vấu căn bên trái
                            lines.Remove(roofCandidate);
                            lines.RemoveAll(l => Math.Min(l.x1, l.x2) <= roofMinX + 35 && Math.Abs(l.y1 - l.y2) >= 8);
                            hasStructuralMath = true;
                            break;
                        }
                    }
                }

                // 3. [Giai Đoạn 3] Nhận diện và gom Đa Phân Số (Multi-Fraction & Recursive Fractions)
                var horizontalFractionLines = lines
                    .Where(l => Math.Abs(l.y1 - l.y2) <= 8 && Math.Abs(l.x2 - l.x1) >= 15)
                    .OrderBy(l => Math.Abs(l.x2 - l.x1)) // Xử lý từ đường phân số ngắn đến dài (lồng nhau nếu có)
                    .ToList();

                foreach (var hLine in horizontalFractionLines)
                {
                    double lineY = (hLine.y1 + hLine.y2) / 2.0;
                    int lineMinX = Math.Min(hLine.x1, hLine.x2);
                    int lineMaxX = Math.Max(hLine.x1, hLine.x2);

                    int padX = Math.Max(35, (int)((curFontSize > 0 ? curFontSize : 14) * 0.15f));
                    bool IsInFractionSpan(WmfSimpleTextItem t)
                    {
                        int charVisualW = Math.Min(t.Width, (int)(t.FontSize * 0.75f));
                        int centerX = t.X + charVisualW / 2;
                        return t.X >= lineMinX - padX && centerX <= lineMaxX + padX;
                    }

                    var numItems = textItems.Where(t => t.Y < lineY && IsInFractionSpan(t)).OrderBy(t => t.X).ToList();
                    var denItems = textItems.Where(t => t.Y > lineY && IsInFractionSpan(t)).OrderBy(t => t.X).ToList();

                    if (numItems.Count > 0 && denItems.Count > 0)
                    {
                        string numText = FormatMathSubExpression(numItems, textItems[0].FontSize);
                        string denText = FormatMathSubExpression(denItems, textItems[0].FontSize);
                        string fracLatex = $"\\frac{{{numText}}}{{{denText}}}";

                        var fracIds = new HashSet<WmfSimpleTextItem>(numItems.Concat(denItems));
                        var outerItems = textItems.Where(t => !fracIds.Contains(t)).ToList();

                        int baseFractionY = outerItems.Count > 0 ? outerItems[0].Y : (int)lineY;
                        int baseFractionFs = outerItems.Count > 0 ? outerItems[0].FontSize : textItems[0].FontSize;

                        outerItems.Add(new WmfSimpleTextItem
                        {
                            Text = fracLatex,
                            X = lineMinX,
                            Y = baseFractionY,
                            Width = Math.Max(10, lineMaxX - lineMinX),
                            FontSize = baseFractionFs,
                            IsSymbol = false,
                            IsMtExtra = false
                        });
                        textItems = outerItems.OrderBy(t => t.X).ToList();
                        lines.Remove(hLine);
                        hasStructuralMath = true;
                    }
                }

                // Nếu vẫn còn đường nét lạ không giải mã được sau khi xử lý phân số/căn thức
                if (lines.Count > 2)
                {
                    return false;
                }

                // 4. Nhận diện và gom Cụm Dấu Ngoặc Cao Nhiều Tầng (MathType Delimiter Pieces \xE6..\xE8, \xF6..\xF8)
                var delimChars = new HashSet<string> { "(", ")", "|", "[", "]", "{", "}", "&#x239B;", "&#x239C;", "&#x239D;", "&#x239E;", "&#x239F;", "&#x23A0;" };
                var delimCandidates = textItems.Where(t => delimChars.Contains(t.Text)).ToList();
                if (delimCandidates.Count >= 2)
                {
                    var delimGroupsByX = new List<List<WmfSimpleTextItem>>();
                    foreach (var d in delimCandidates.OrderBy(t => t.X))
                    {
                        var existing = delimGroupsByX.FirstOrDefault(g => Math.Abs(g[0].X - d.X) < 30);
                        if (existing != null)
                        {
                            existing.Add(d);
                        }
                        else
                        {
                            delimGroupsByX.Add(new List<WmfSimpleTextItem> { d });
                        }
                    }

                    var tallDelimGroups = delimGroupsByX.Where(g => g.Count >= 2).ToList();
                    if (tallDelimGroups.Count > 0)
                    {
                        var tallItemSet = new HashSet<WmfSimpleTextItem>(tallDelimGroups.SelectMany(g => g));
                        var nonDelimItems = textItems.Where(t => !tallItemSet.Contains(t)).ToList();

                        int commonBaselineY = nonDelimItems.Count > 0
                            ? nonDelimItems.GroupBy(t => t.Y).OrderByDescending(g => g.Count()).First().Key
                            : textItems[0].Y;

                        int minTotalX = textItems.Min(t => t.X);
                        int maxTotalX = textItems.Max(t => t.X);
                        double totalMidX = (minTotalX + maxTotalX) / 2.0;

                        tallDelimGroups = tallDelimGroups.OrderBy(g => g[0].X).ToList();
                        var collapsedItems = new List<WmfSimpleTextItem>(nonDelimItems);

                        for (int gi = 0; gi < tallDelimGroups.Count; gi++)
                        {
                            var g = tallDelimGroups[gi];
                            bool isLeft = tallDelimGroups.Count == 2 ? (gi == 0) : (g[0].X < totalMidX);
                            collapsedItems.Add(new WmfSimpleTextItem
                            {
                                Text = isLeft ? "(" : ")",
                                X = g[0].X,
                                Y = commonBaselineY,
                                Width = g[0].Width,
                                FontSize = g[0].FontSize,
                                IsSymbol = false,
                                IsMtExtra = false
                            });
                        }
                        textItems = collapsedItems;
                        hasStructuralMath = true;
                    }
                }

                // Sắp xếp các ký tự theo tọa độ X
                var sortedItems = textItems.OrderBy(t => t.X).ToList();

                // Xác định Font Size cơ sở (Median Font Size)
                var fontSizes = sortedItems.Select(t => t.FontSize).OrderBy(f => f).ToList();
                int baseFs = fontSizes[fontSizes.Count / 2];
                if (baseFs <= 0) baseFs = 14;

                // Kiểm tra độ phân tán trục Y theo tỷ lệ cấu hình động
                float maxDeltaYRatio = settings.SimplifierMaxDeltaYRatio > 0 ? Math.Max(0.70f, settings.SimplifierMaxDeltaYRatio) : 0.70f;
                if (hasStructuralMath)
                {
                    maxDeltaYRatio = Math.Max(maxDeltaYRatio, 2.50f); // Nới lỏng độ phân tán Y cho công thức đa tầng
                }
                int minY = sortedItems.Min(t => t.Y);
                int maxY = sortedItems.Max(t => t.Y);
                if (maxY - minY > baseFs * maxDeltaYRatio)
                {
                    inlineMathText = $"FAIL_1480: maxY={maxY}, minY={minY}, diff={maxY - minY}, maxAllowed={baseFs * maxDeltaYRatio}, items={string.Join(";", sortedItems.Select(x => $"{x.Text}@(x={x.X},y={x.Y},w={x.Width},fs={x.FontSize})"))}";
                    return false;
                }

                // Tính baseY từ các ký tự có cỡ chữ chính (>= 80% baseFs)
                var mainItems = sortedItems.Where(t => t.FontSize >= baseFs * 0.80f).ToList();
                if (mainItems.Count == 0)
                {
                    inlineMathText = "FAIL_1487";
                    return false;
                }
                var mainYs = mainItems.Select(t => t.Y).OrderBy(y => y).ToList();
                int baseY = mainYs[mainYs.Count / 2];

                var sb = new StringBuilder();
                bool inSup = false;
                bool inSub = false;

                float supRatio = settings.SimplifierSuperscriptThresholdRatio > 0 ? settings.SimplifierSuperscriptThresholdRatio : 0.18f;
                float subRatio = settings.SimplifierSubscriptThresholdRatio > 0 ? settings.SimplifierSubscriptThresholdRatio : 0.12f;
                float wordGapRatio = settings.SimplifierWordGapRatio > 0 ? settings.SimplifierWordGapRatio : 0.40f;

                for (int i = 0; i < sortedItems.Count; i++)
                {
                    var item = sortedItems[i];

                    // Khoảng cách ngang: Chèn dấu cách nếu có khoảng trống lớn giữa 2 ký tự thông thường
                    if (i > 0)
                    {
                        var prev = sortedItems[i - 1];
                        int gap = item.X - (prev.X + prev.Width);
                        if (gap >= baseFs * wordGapRatio &&
                            !prev.Text.EndsWith(" ") &&
                            !item.Text.StartsWith(" ") &&
                            !item.Text.StartsWith("'") &&
                            item.Text != "," && item.Text != ";" && item.Text != "." && item.Text != ")" && item.Text != "]" && item.Text != "}")
                        {
                            sb.Append(' ');
                        }
                    }

                    // Nhận diện số mũ và chỉ số dưới theo tỷ lệ Heuristic động
                    bool isSup = (baseY - item.Y) >= baseFs * supRatio && item.FontSize <= baseFs * 0.85f;
                    bool isSub = (item.Y - baseY) >= baseFs * subRatio && item.FontSize <= baseFs * 0.85f;

                    if (item.Text.StartsWith("\\frac") || item.Text.StartsWith("\\sqrt") || item.Text.StartsWith("\\begin"))
                    {
                        isSup = false;
                        isSub = false;
                    }

                    if (isSup)
                    {
                        if (!inSup)
                        {
                            if (inSub) { sb.Append('}'); inSub = false; }
                            sb.Append("^{");
                            inSup = true;
                        }
                        sb.Append(item.Text);
                    }
                    else if (isSub)
                    {
                        if (!inSub)
                        {
                            if (inSup) { sb.Append('}'); inSup = false; }
                            sb.Append("_{");
                            inSub = true;
                        }
                        sb.Append(item.Text);
                    }
                    else
                    {
                        if (inSup) { sb.Append('}'); inSup = false; }
                        if (inSub) { sb.Append('}'); inSub = false; }
                        sb.Append(item.Text);
                    }
                }

                if (inSup || inSub)
                {
                    sb.Append('}');
                }

                string formula = sb.ToString().Trim();
                if (string.IsNullOrWhiteSpace(formula)) return false;

                // Chuẩn hóa dấu chấm hình lập phương/lăng trụ (ABCD \cdot A'B'C'D' -> ABCD.A'B'C'D')
                formula = Regex.Replace(formula, @"([A-Z]{3,4})\s*\\cdot\s*([A-Z])", "$1.$2");

                // [Trụ Cột 3 Post-Norm]: Chuẩn hóa các hàm lượng giác & giải tích chuẩn KaTeX (\sin, \cos, \tan, \cot, \lim, \ln, \log)
                // Hỗ trợ cả trường hợp dính liền biến (sinx -> \sin x, cosx -> \cos x) và trường hợp độc lập
                formula = Regex.Replace(formula, @"(?<![a-zA-Z\\])(sin|cos|tan|cot|sec|csc)\s*([xyztabc])(?![a-zA-Z])", @"\$1 $2");
                formula = Regex.Replace(formula, @"(?<![a-zA-Z\\])(sin|cos|tan|cot|lim|ln|log|arcsin|arccos|arctan|sec|csc)\b", @"\$1");

                inlineMathText = $"${formula}$";
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// [Giai Đoạn 3] Định dạng biểu thức toán con (tử số, mẫu số, biểu thức dưới căn)
        /// Bảo toàn số mũ (^{...}), chỉ số dưới (_{...}) và ghép nối theo trật tự hoành độ X.
        /// </summary>
        private static string FormatMathSubExpression(List<WmfSimpleTextItem> items, int baseFs)
        {
            if (items == null || items.Count == 0) return string.Empty;
            var sorted = items.OrderBy(t => t.X).ToList();
            if (sorted.Count == 1) return sorted[0].Text;

            var ys = sorted.Select(t => t.Y).OrderBy(y => y).ToList();
            int medianY = ys[ys.Count / 2];

            var sb = new StringBuilder();
            bool inSup = false;
            bool inSub = false;

            for (int i = 0; i < sorted.Count; i++)
            {
                var item = sorted[i];

                bool isSup = (medianY - item.Y) >= baseFs * 0.20f && item.FontSize <= baseFs * 0.85f;
                bool isSub = (item.Y - medianY) >= baseFs * 0.15f && item.FontSize <= baseFs * 0.85f;

                if (item.Text.StartsWith("\\frac") || item.Text.StartsWith("\\sqrt") || item.Text.StartsWith("\\begin") || item.Text.StartsWith("\\overrightarrow"))
                {
                    isSup = false;
                    isSub = false;
                }

                if (isSup)
                {
                    if (!inSup)
                    {
                        if (inSub) { sb.Append('}'); inSub = false; }
                        sb.Append("^{");
                        inSup = true;
                    }
                    sb.Append(item.Text);
                }
                else if (isSub)
                {
                    if (!inSub)
                    {
                        if (inSup) { sb.Append('}'); inSup = false; }
                        sb.Append("_{");
                        inSub = true;
                    }
                    sb.Append(item.Text);
                }
                else
                {
                    if (inSup) { sb.Append('}'); inSup = false; }
                    if (inSub) { sb.Append('}'); inSub = false; }
                    sb.Append(item.Text);
                }
            }

            if (inSup || inSub) sb.Append('}');
            return sb.ToString().Trim();
        }

        private abstract class WmfGdiObj { }
        private class WmfGdiFont : WmfGdiObj
        {
            public string Face { get; set; } = "serif";
            public int Size { get; set; } = 14;
            public bool Italic { get; set; }
            public string Weight { get; set; } = "normal";
        }
        private class WmfGdiPen : WmfGdiObj
        {
            public ushort Style { get; set; }
            public int Width { get; set; }
            public string Color { get; set; } = "rgb(0,0,0)";
        }
        private class WmfGdiBrush : WmfGdiObj
        {
            public ushort Style { get; set; }
            public string Color { get; set; } = "rgb(0,0,0)";
        }
        private class WmfGdiDummy : WmfGdiObj { }
    }
}
