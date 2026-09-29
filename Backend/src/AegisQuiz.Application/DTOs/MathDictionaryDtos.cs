using System;
using System.Collections.Generic;

namespace AegisQuiz.Application.DTOs
{
    /// <summary>
    /// Bản ghi ánh xạ một ký tự Glyph từ font MathType sang thực thể HTML/Unicode hoặc LaTeX
    /// </summary>
    public class SymbolGlyphEntryDto
    {
        public string Font { get; set; } = "Symbol";
        public string CharHex { get; set; } = string.Empty; // Ví dụ: "0x00A2"
        public string Replacement { get; set; } = string.Empty; // Ví dụ: "&#x2032;"
        public string? Latex { get; set; } // Ví dụ: "\\prime"
        public string? Desc { get; set; }
    }

    /// <summary>
    /// Bản ghi ánh xạ chữ cái Hy Lạp từ font MathType
    /// </summary>
    public class GreekLetterEntryDto
    {
        public string Font { get; set; } = "Symbol";
        public string Char { get; set; } = string.Empty; // Ví dụ: "a", "D", "W"
        public string Replacement { get; set; } = string.Empty; // Ví dụ: "&#x03B1;"
        public string? Latex { get; set; } // Ví dụ: "\\alpha"
        public string? Desc { get; set; }
    }

    /// <summary>
    /// Quy tắc chuẩn hóa văn bản toán học inline qua Regex
    /// </summary>
    public class InlineNormalizationRuleDto
    {
        public string Pattern { get; set; } = string.Empty;
        public string Replacement { get; set; } = string.Empty;
        public string? Desc { get; set; }
    }

    /// <summary>
    /// Đăng bạ từ điển toán học động toàn hệ thống
    /// </summary>
    public class MathDictionaryRegistryDto
    {
        public string Version { get; set; } = "2026.1";
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Bảng ánh xạ mã Glyph font MathType sang Unicode Entity
        /// </summary>
        public List<SymbolGlyphEntryDto> SymbolGlyphs { get; set; } = new();

        /// <summary>
        /// Bảng ánh xạ chữ cái Hy Lạp MathType
        /// </summary>
        public List<GreekLetterEntryDto> GreekLetters { get; set; } = new();

        /// <summary>
        /// Bảng ánh xạ ký hiệu OpenXML OMML sang lệnh KaTeX LaTeX
        /// </summary>
        public Dictionary<string, string> OmmlTokens { get; set; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Bảng ánh xạ dấu mũ / accents (<m:acc>)
        /// </summary>
        public Dictionary<string, string> AccentMappings { get; set; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Danh sách quy tắc chuẩn hóa văn bản inline
        /// </summary>
        public List<InlineNormalizationRuleDto> InlineNormalizations { get; set; } = new();

        /// <summary>
        /// Danh sách quy tắc bóc tách cấu trúc toán học WMF động (Declarative Structural Simplification Rules)
        /// </summary>
        public List<MathStructuralRuleDto> StructuralSimplificationRules { get; set; } = new();

        /// <summary>
        /// Cấu hình động toàn diện cho bộ sinh và hiển thị công thức Math Engine (Typography, Bounding Box, Hình học Vector, Ngoặc đơn)
        /// </summary>
        public MathEngineTypographyDto EngineSettings { get; set; } = new();
    }

    /// <summary>
    /// Cấu hình hiển thị theo từng môn học chuyên biệt (Toán, Lý, Hóa, Sinh)
    /// </summary>
    public class SubjectMathProfileDto
    {
        public string SubjectCode { get; set; } = "MATH";
        public string SubjectName { get; set; } = "Toán Học";
        public float BaseFontUnit { get; set; } = 1.45f;
        public string LetterSpacing { get; set; } = "0.1em";
        public string MinInlineHeight { get; set; } = "1.40em";
        public float CropPadX { get; set; } = 10.0f;
        public float CropPadY { get; set; } = 6.0f;
    }

    /// <summary>
    /// Đối tượng cấu hình động 23 tham số của Math Engine
    /// </summary>
    public class MathEngineTypographyDto
    {
        // 1. Nhóm Typography & Visual
        public float BaseFontUnit { get; set; } = 1.45f;
        public string LetterSpacing { get; set; } = "0.1em";
        public string MinInlineHeight { get; set; } = "1.40em";
        public string MaxFormulaHeight { get; set; } = "14em";
        public string FormulaPaddingX { get; set; } = "0.5rem";
        public string FormulaPaddingY { get; set; } = "0.125rem";
        public string FormulaMarginX { get; set; } = "0.25rem";
        public string FontFamilySerif { get; set; } = "'Times New Roman', 'Cambria Math', serif";

        // 2. Nhóm Vertical Alignment Heuristics
        public float VAlignSlope { get; set; } = 0.48f;
        public float VAlignIntercept { get; set; } = 0.12f;
        public float MultiLineThreshold { get; set; } = 1.8f;

        // 3. Nhóm Bounding Box & Trim
        public float CropPadX { get; set; } = 10.0f;
        public float CropPadY { get; set; } = 6.0f;
        public float CharWidthFactor { get; set; } = 0.72f;
        public float AscenderFactor { get; set; } = 0.85f;
        public float DescenderFactor { get; set; } = 0.25f;

        // 4. Nhóm Hình học Vector & Dấu ngoặc
        public float VectorHeadLengthRatio { get; set; } = 0.16f;
        public float VectorHeadSpreadRatio { get; set; } = 0.09f;
        public float VectorStrokeWidthRatio { get; set; } = 0.04f;
        public float ParenthesisBowRatio { get; set; } = 0.18f;
        public float ParenthesisStrokeWidthRatio { get; set; } = 0.04f;

        // 5. Nhóm Intelligent WMF Simplifier Dynamic Heuristics
        public float SimplifierMaxDeltaYRatio { get; set; } = 0.55f;
        public float SimplifierSuperscriptThresholdRatio { get; set; } = 0.18f;
        public float SimplifierSubscriptThresholdRatio { get; set; } = 0.12f;
        public float SimplifierWordGapRatio { get; set; } = 0.40f;
        public int SimplifierMaxSimpleCharCount { get; set; } = 70;

        // 6. Cấu hình đặc thù theo môn học
        public Dictionary<string, SubjectMathProfileDto> SubjectProfiles { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            ["MATH"] = new() { SubjectCode = "MATH", SubjectName = "Toán Học", BaseFontUnit = 1.45f, LetterSpacing = "0.1em", CropPadX = 10f, CropPadY = 6f },
            ["CHEMISTRY"] = new() { SubjectCode = "CHEMISTRY", SubjectName = "Hóa Học", BaseFontUnit = 1.30f, LetterSpacing = "0.04em", CropPadX = 8f, CropPadY = 5f },
            ["PHYSICS"] = new() { SubjectCode = "PHYSICS", SubjectName = "Vật Lý", BaseFontUnit = 1.40f, LetterSpacing = "0.08em", CropPadX = 10f, CropPadY = 6f },
            ["BIOLOGY"] = new() { SubjectCode = "BIOLOGY", SubjectName = "Sinh Học", BaseFontUnit = 1.35f, LetterSpacing = "0.06em", CropPadX = 8f, CropPadY = 5f }
        };
    }
}
