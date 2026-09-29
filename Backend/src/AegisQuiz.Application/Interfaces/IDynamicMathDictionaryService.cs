using System;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// Giao diện dịch vụ Từ điển Toán học Động (Universal Dynamic Math Dictionary Service)
    /// </summary>
    public interface IDynamicMathDictionaryService
    {
        /// <summary>
        /// Tra cứu thực thể thay thế cho ký tự Glyph theo Font và mã Hex/Char
        /// </summary>
        string ResolveSymbolGlyph(string font, char c, string defaultFallback);

        /// <summary>
        /// Tra cứu lệnh KaTeX LaTeX tương ứng cho ký tự Glyph theo Font và mã Hex/Char
        /// </summary>
        string ResolveSymbolLatex(string font, char c, string defaultFallback);

        /// <summary>
        /// Tra cứu thực thể thay thế cho chữ cái Hy Lạp MathType
        /// </summary>
        string ResolveGreekLetter(string font, char c, string defaultFallback);

        /// <summary>
        /// Tra cứu lệnh KaTeX LaTeX tương ứng cho chữ cái Hy Lạp MathType
        /// </summary>
        string ResolveGreekLatex(string font, char c, string defaultFallback);

        /// <summary>
        /// Tra cứu lệnh KaTeX LaTeX tương ứng với ký hiệu OMML
        /// </summary>
        string ResolveOmmlToken(string token, string defaultFallback);

        /// <summary>
        /// Tra cứu lệnh KaTeX tương ứng với dấu mũ accent (<m:acc>)
        /// </summary>
        string ResolveAccent(string accentChar, string baseContent);

        /// <summary>
        /// Chuẩn hóa các ký tự toán học inline trong đoạn văn bản
        /// </summary>
        string NormalizeInlineText(string text);

        /// <summary>
        /// Nạp lại từ điển từ chuỗi JSON (Hot-Reload 0ms Downtime)
        /// </summary>
        bool HotReload(string json);

        /// <summary>
        /// Lấy toàn bộ bản đăng bạ từ điển hiện tại trong bộ nhớ
        /// </summary>
        MathDictionaryRegistryDto GetCurrentRegistry();

        /// <summary>
        /// Thêm hoặc cập nhật một ký hiệu mới trực tiếp
        /// </summary>
        void UpsertSymbolGlyph(SymbolGlyphEntryDto entry);

        /// <summary>
        /// Thêm hoặc cập nhật một ký hiệu OMML mới trực tiếp
        /// </summary>
        void UpsertOmmlToken(string token, string latex);

        /// <summary>
        /// Lấy toàn bộ 23 tham số cấu hình động của Math Engine
        /// </summary>
        MathEngineTypographyDto GetEngineSettings();

        /// <summary>
        /// Cập nhật cấu hình động cho Math Engine
        /// </summary>
        void UpdateEngineSettings(MathEngineTypographyDto settings);

        /// <summary>
        /// Lấy cấu hình profile đặc thù theo môn học (Toán, Lý, Hóa, Sinh)
        /// </summary>
        SubjectMathProfileDto GetSubjectProfile(string? subjectCode);

        /// <summary>
        /// Khớp và rút gọn cấu trúc toán học WMF qua hệ quy tắc cấu trúc động (Declarative Structural Rules)
        /// </summary>
        bool TryMatchStructuralRule(
            IReadOnlyList<(int x1, int y1, int x2, int y2)> lines,
            List<WmfSimpleTextItem> textItems,
            int polyCount,
            int complexShapes,
            int baseFs,
            out List<WmfSimpleTextItem> modifiedItems);

        /// <summary>
        /// Thêm hoặc cập nhật một quy tắc cấu trúc toán học động
        /// </summary>
        void UpsertStructuralRule(MathStructuralRuleDto rule);

        /// <summary>
        /// Xóa một quy tắc cấu trúc toán học động theo mã ID
        /// </summary>
        bool DeleteStructuralRule(string ruleId);
    }
}
