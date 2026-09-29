using System;
using System.Collections.Generic;

namespace AegisQuiz.Application.DTOs
{
    /// <summary>
    /// Đặc tả mỏ neo hình học hoặc ký hiệu nhận diện của một cấu trúc toán học 2D
    /// </summary>
    public class MathAnchorSpecDto
    {
        /// <summary>
        /// Kiểu mỏ neo: GEOMETRIC_LINE, COMPOUND, DELIMITER_SYMBOL, TEXT_ANCHOR
        /// </summary>
        public string Type { get; set; } = "GEOMETRIC_LINE";

        /// <summary>
        /// Hướng của nét vẽ: HORIZONTAL, VERTICAL, ANY
        /// </summary>
        public string Orientation { get; set; } = "HORIZONTAL";

        /// <summary>
        /// Số lượng nét vẽ mỏ neo yêu cầu (mặc định 1)
        /// </summary>
        public int Count { get; set; } = 1;

        /// <summary>
        /// Độ dốc tối đa cho phép của đường kẻ ngang
        /// </summary>
        public float MaxSlope { get; set; } = 0.08f;

        /// <summary>
        /// Ký hiệu mỏ neo dạng text/token nếu có (ví dụ: "\\sqrt", "{", "\\lim")
        /// </summary>
        public string? Symbol { get; set; }

        /// <summary>
        /// Tên đường nét đính kèm (ví dụ: "TOP_OVERLINE" cho nóc căn)
        /// </summary>
        public string? AttachedLine { get; set; }

        /// <summary>
        /// Tỷ lệ chiều cao tối thiểu so với cỡ chữ cơ sở (cho dấu ngoặc nhọn { lớn)
        /// </summary>
        public float MinHeightRatio { get; set; } = 1.0f;
    }

    /// <summary>
    /// Đặc tả vùng không gian slot tương đối quanh mỏ neo
    /// </summary>
    public class SpatialSlotDto
    {
        /// <summary>
        /// Tên biến slot đại diện trong template (ví dụ: "numerator", "denominator", "radicand", "row1")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Vị trí không gian tương đối:
        /// ABOVE_ANCHOR, BELOW_ANCHOR, UNDER_OVERLINE, TOP_LEFT_CORNER,
        /// RIGHT_ADJACENT, RIGHT_UPPER, RIGHT_LOWER, DIRECTLY_BELOW, DIRECTLY_ABOVE
        /// </summary>
        public string RelativePosition { get; set; } = "ABOVE_ANCHOR";

        /// <summary>
        /// Khoảng cách tối đa theo trục Y tính theo tỷ lệ cỡ chữ cơ sở
        /// </summary>
        public float MaxDistanceYRatio { get; set; } = 1.5f;

        /// <summary>
        /// Dung sai theo trục X (pixels)
        /// </summary>
        public int PaddingX { get; set; } = 15;

        /// <summary>
        /// Số token tối đa cho phép trong slot
        /// </summary>
        public int MaxTokens { get; set; } = 12;

        /// <summary>
        /// Biểu thức chính quy kiểm duyệt token an toàn (Fail-Safe Guard)
        /// </summary>
        public string? AllowedPattern { get; set; }

        /// <summary>
        /// Cờ cho biết slot này có bắt buộc hay không (ví dụ bậc căn thức là tùy chọn)
        /// </summary>
        public bool Optional { get; set; } = false;
    }

    /// <summary>
    /// Các ràng buộc an toàn (Fail-Safe Guard) cho cấu trúc toán học
    /// </summary>
    public class RuleConstraintsDto
    {
        /// <summary>
        /// Cấm xuất hiện các nét vẽ đa giác phức tạp (Polygon, Polyline, Bézier)
        /// </summary>
        public bool DisallowPolygons { get; set; } = true;

        /// <summary>
        /// Giới hạn tổng độ dài ký tự tối đa của toàn bộ cấu trúc sau khi rút gọn
        /// </summary>
        public int MaxTotalChars { get; set; } = 60;

        /// <summary>
        /// Số lượng nét vẽ hình học tối đa cho phép xuất hiện trong cấu trúc này
        /// </summary>
        public int MaxLines { get; set; } = 1;
    }

    /// <summary>
    /// [Declarative Structural Simplification Rule]
    /// Quy tắc bóc tách cấu trúc toán học WMF động nạp từ DB/JSON:
    /// Cho phép mở rộng việc bóc tách phân số, căn thức, hệ phương trình, giới hạn...
    /// mà không cần sửa đổi mã nguồn C#.
    /// </summary>
    public class MathStructuralRuleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Priority { get; set; } = 100;
        public bool Enabled { get; set; } = true;
        public MathAnchorSpecDto Anchor { get; set; } = new();
        public List<SpatialSlotDto> Slots { get; set; } = new();
        public RuleConstraintsDto Constraints { get; set; } = new();

        /// <summary>
        /// Mẫu sinh mã KaTeX với các placeholder:
        /// Ví dụ: "\\frac{{{numerator}}}{{{denominator}}}"
        /// hoặc "\\begin{cases} {{row1}} \\\\ {{row2}} \\end{cases}"
        /// </summary>
        public string Template { get; set; } = string.Empty;
    }

    /// <summary>
    /// Bản ghi văn bản / ký tự bóc tách từ WMF phục vụ bộ nhận diện cấu trúc
    /// </summary>
    public class WmfSimpleTextItem
    {
        public string Text { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int FontSize { get; set; }
        public bool IsSymbol { get; set; }
        public bool IsMtExtra { get; set; }
    }
}
