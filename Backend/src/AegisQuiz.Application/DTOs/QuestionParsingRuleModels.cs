using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Application.DTOs
{
    /// <summary>
    /// Bản đặc tả một Quy tắc Bóc tách Câu hỏi Cấu hình Động (Cognitive Ingestion Rule — CIG 4 chiều + Lifecycle + Topology + Evolution).
    /// Hỗ trợ nạp từ File JSON (<c>parsing_rules_registry.json</c>) hoặc CSDL PostgreSQL (bảng <c>QuestionParsingRules</c>).
    /// </summary>
    public class QuestionParsingRuleDto
    {
        public string RuleCode { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string TargetExamType { get; set; } = "GENERAL";
        /// <summary>Mã ngôn ngữ ISO-639-1 của quy tắc ("vi", "en", "zh", "ko", "ja", "fr", "de") hoặc "all" cho quy tắc phổ quát.</summary>
        public string Language { get; set; } = "vi";
        public int Priority { get; set; } = 100;
        public bool IsActive { get; set; } = true;
        public string Description { get; set; } = string.Empty;

        public AnchorSpecDto AnchorSpec { get; set; } = new();
        public StimulusSpecDto StimulusSpec { get; set; } = new();
        public InteractionSpecDto InteractionSpec { get; set; } = new();
        public EvaluationSpecDto EvaluationSpec { get; set; } = new();
        public ExamLifecycleSpecDto LifecycleSpec { get; set; } = new();
        public TableMatrixSpecDto? TableMatrixSpec { get; set; }
        public TopologySpecDto TopologySpec { get; set; } = new();
        public MetaEvolutionSpecDto MetaEvolutionSpec { get; set; } = new();

        // Thuộc tính Alias tương thích ngược / LLM JSON mapping
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public AnchorSpecDto? Structure
        {
            get => AnchorSpec;
            set { if (value != null) AnchorSpec = value; }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public StimulusSpecDto? Semantics
        {
            get => StimulusSpec;
            set { if (value != null) StimulusSpec = value; }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public InteractionSpecDto? Options
        {
            get => InteractionSpec;
            set { if (value != null) InteractionSpec = value; }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public EvaluationSpecDto? Evaluation
        {
            get => EvaluationSpec;
            set { if (value != null) EvaluationSpec = value; }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ExamLifecycleSpecDto? Lifecycle
        {
            get => LifecycleSpec;
            set { if (value != null) LifecycleSpec = value; }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TopologySpecDto? Topology
        {
            get => TopologySpec;
            set { if (value != null) TopologySpec = value; }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MetaEvolutionSpecDto? Evolution
        {
            get => MetaEvolutionSpec;
            set { if (value != null) MetaEvolutionSpec = value; }
        }
    }

    /// <summary>Chiều 1: Điểm neo nhận diện bắt đầu câu hỏi</summary>
    public class AnchorSpecDto
    {
        public string RegexPattern { get; set; } = @"^(?:Câu|Question|Item|Task|Exercise|Q)\s*(\d+)";
        public int QuestionNumberGroupIndex { get; set; } = 1;
        public int MaxScoreGroupIndex { get; set; } = -1;
        public List<string> SectionMarkers { get; set; } = new();

        // Aliases cho AI / Schema compatibility
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? QuestionHeaderRegexPattern
        {
            get => RegexPattern;
            set { if (value != null) RegexPattern = value; }
        }

        public string DelimiterType { get; set; } = "REGEX_LINE_START";
    }

    /// <summary>Chiều 2: Bối cảnh kích thích trí tuệ (Công thức toán, hình vẽ, bài đọc hiểu)</summary>
    public class StimulusSpecDto
    {
        public bool SupportMathTypeWmf { get; set; } = true;
        public bool SupportVectorSvg { get; set; } = true;
        public bool ExtractInlineImages { get; set; } = true;
        public string? ReadingPassageStartRegex { get; set; }
        public string? ReadingPassageRangeRegex { get; set; }

        /// <summary>Mẫu nhận diện dòng mở đầu bài đọc hiểu (Reading Passage / Advertisement / Leaflet).</summary>
        public List<string> PassageStartPatterns { get; set; } = new()
        {
            @"^(?:Read\s+(?:the\s+)?(?:following\s+)?(?:passage|advertisement|leaflet|text|article|announcement)|Đọc\s+(?:đoạn\s+văn|bài\s+đọc|thông\s+báo|tờ\s+rơi)\s+sau)[\s\S]*",
            @"^(?:阅读|阅读理解|请阅读下面的短文|根据材料回答)[\s\S]*"
        };

        /// <summary>Mẫu bóc tách phạm vi câu hỏi của bài đọc (Group 1 = câu bắt đầu, Group 2 = câu kết thúc).</summary>
        public List<string> PassageRangePatterns { get; set; } = new()
        {
            @"\b(?:from|từ)\s*(\d+)\s*(?:to|đến)\s*(\d+)\b",
            @"questions?\s*(\d+)\s*(?:-|to|through)\s*(\d+)",
            @"(?:第|题)\s*(\d+)\s*(?:-|到|至)\s*(\d+)\s*题"
        };

        /// <summary>
        /// Tự động đóng gói phạm vi bài đọc hiểu khi chuyển sang SectionMarker mới (Dynamic Context Boundary).
        /// </summary>
        public bool AutoTerminateOnNextSection { get; set; } = true;

        /// <summary>
        /// Danh sách các mẫu chỉ thị chuyển dạng bài (Instruction Directives) để tự động ngắt bài đọc hiểu cũ.
        /// Ví dụ: "Mark the letter...", "Choose the word...", "Read the following passage..."
        /// </summary>
        public List<string> InstructionDirectivePatterns { get; set; } = new()
        {
            @"^(?:Mark\s+the\s+letter|Choose\s+the\s+letter|Choose\s+the\s+word|Read\s+the\s+following\s+(?:passage|text|announcement|advertisement|leaflet)|Đọc\s+(?:đoạn\s+văn|bài\s+đọc))\b"
        };

        // Aliases
        public string? MathBlockMarkerRegex { get; set; }
        public string? CodeBlockMarkerRegex { get; set; }
        public string? ImageMarkerRegex { get; set; }
    }

    /// <summary>Chiều 3: Siêu hình thái tương tác (Options, Matrix, Short answer, Stepped Essay...)</summary>
    public class InteractionSpecDto
    {
        public string DefaultQuestionType { get; set; } = "SINGLE"; // SINGLE, MULTI, TRUE_FALSE, SHORT_ANSWER, ESSAY, MATCHING, ORDERING
        public string OptionRegexPattern { get; set; } = @"^(?:([A-D])|[\(]([A-D])[\)])[\.\:\s\-]+(.*)$";
        public int OptionLabelGroupIndex { get; set; } = 1;
        public int OptionContentGroupIndex { get; set; } = 3;
        public string? TrueFalseSubItemRegex { get; set; } = @"^(?:([a-d])|[\(]([a-d])[\)])[\.\:\s\-]+(.*)$";
        public string? ShortAnswerPromptRegex { get; set; }
        public string ShortAnswerInputFormat { get; set; } = "NUMERIC_OR_FRACTION"; // NUMERIC_OR_FRACTION, ALPHANUMERIC
        public double Tolerance { get; set; } = 0.01;

        /// <summary>Cờ bật/tắt Essay Safeguard ngăn bốc nhầm a, b, 1, 2 thành phương án trắc nghiệm.</summary>
        public bool EssaySafeguardEnabled { get; set; } = true;

        /// <summary>Các mẫu Regex nhận diện thang điểm bài tự luận, ví dụ: "(2,0 điểm)", "[1.5đ]".</summary>
        public List<string> EssayScoreMarkerPatterns { get; set; } = new()
        {
            @"(?:[\(\[]\s*[\d\.\,]+\s*(?:điểm|đ)\s*[\)\]])"
        };

        /// <summary>Các từ khóa bài toán tự luận.</summary>
        public List<string> EssayKeywordPatterns { get; set; } = new()
        {
            @"\b(?:Chứng\s*minh|Giải\s*phương\s*trình|Giải\s*hệ|Rút\s*gọn|Lập\s*công\s*thức|Biện\s*luận|Tính\s+(?:giá\s+trị|diện\s+tích|thể\s+tích|tốc\s+độ|chiều\s+cao)|Vẽ\s+đồ\s+thị|Hỏi\s+cần\s+phối\s+trộn)\b"
        };

        /// <summary>
        /// Mẫu tách các phương án nằm cùng một dòng (A. ... B. ... C. ... D. ...).
        /// Group 1/3 = dấu * đánh dấu đáp án đúng, Group 2 = nhãn phương án.
        /// </summary>
        public string InlineOptionDelimiterPattern { get; set; } = @"(?:^|[\s\t\–\—]+|(?<=[\.\:\)\;\!\?]))(?:<u>|\*|\*\*|\<b\>)?(\*?)((?-i:[A-H])|[1-4])(\*?)(?:</u>|\*|\*\*|\</b\>)?[\.\:\)](?:</u>|\</b\>)?[\s\t]+";

        /// <summary>Phân nhóm chuyên đề con (SubCategory) theo từ khóa nội dung, ví dụ GPLX: SA_HINH, BIEN_BAO.</summary>
        public Dictionary<string, List<string>> SubCategoryPatterns { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            ["SA_HINH"] = new() { @"(?:sa\s*hình|thứ\s*tự\s*các\s*xe|xe\s*nào\s*được\s*quyền\s*đi|xe\s*nào\s*phải\s*nhường)" },
            ["BIEN_BAO"] = new() { @"(?:biển\s*nào|biển\s*báo|biển\s*số)" }
        };

        // Đặc tả cho Tự luận phân cấp đa tầng (Hierarchical Stepped Essay)
        public string? SubItemRegexPattern { get; set; }
        public int SubItemLabelGroupIndex { get; set; } = 1;
        public int SubItemScoreGroupIndex { get; set; } = -1;
        public int SubItemContentGroupIndex { get; set; } = 2;

        // Aliases
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? QuestionType
        {
            get => DefaultQuestionType;
            set { if (value != null) DefaultQuestionType = value; }
        }
    }

    /// <summary>Chiều 4: Hàm chân lý phân xử điểm số, lời giải & cờ tử thần (Điểm liệt)</summary>
    public class EvaluationSpecDto
    {
        public string AnswerRegexPattern { get; set; } = @"^(?:ĐÁP\s*ÁN|ANSWER|KEY|CHỌN)[\.\:\s\-]+([A-D0-9a-d\,\s]+)";
        public int AnswerGroupIndex { get; set; } = 1;
        public string ExplanationRegexPattern { get; set; } = @"^(?:Lời\s*giải|Hướng\s*dẫn|HD|Giải\s*thích|Explanation|Căn\s*cứ)\b";
        public string? ExplanationStartRegex
        {
            get => ExplanationRegexPattern;
            set { if (value != null) ExplanationRegexPattern = value; }
        }

        /// <summary>Các mẫu tiền tố dòng lời giải cục bộ ngay dưới câu hỏi (đa ngôn ngữ).</summary>
        public List<string> ExplanationLinePrefixPatterns { get; set; } = new()
        {
            @"^(?:Lời\s*giải|L[\.\?\s]*i\s*gi[\.\?\s]*i|Hướng\s*dẫn\s*giải|Hu?ng\s*d?n\s*gi?i|HƯỚNG\s*DẪN|LỜI\s*GIẢI|HD)[\.\:\s\-]+(.*)",
            @"^(?:Giải\s*thích|Căn\s*cứ|Ghi\s*chú|Explanation|Rationale|Solution|Feedback)[\.\:\s]+(.*)",
            @"^(?:试题解析|详细解析|解析)[\.\:\s]+(.*)"
        };

        public string? CriticalQuestionMarkerRegex { get; set; } = @"(?:\[\s*ĐIỂM\s*LIỆT\s*\]|⚠️|TỬ\s*THẦN)";

        /// <summary>Danh mục biểu thức nhận diện câu điểm liệt / câu bắt buộc đúng (GPLX, HSE...).</summary>
        public List<string> CriticalMarkers { get; set; } = new()
        {
            @"(?:\bDIEM[_\s]*LIET\b|\bĐIỂM[_\s]*LIỆT\b|mất\s*an\s*toàn\s*giao\s*thông\s*nghiêm\s*trọng|bị\s*nghiêm\s*cấm|\bnghiêm\s*cấm\b|nồng\s*độ\s*cồn|sử\s*dụng\s*ma\s*túy|đua\s*xe\s*trái\s*phép)",
            @"(?:\bFATAL\b|\bCRITICAL\b|\bMANDATORY\b|\bEMERGENCY\b)"
        };

        /// <summary>Đặc tả trích xuất đáp án nằm lẫn trong lời giải (Inline Answer Extraction).</summary>
        public InlineAnswerExtractSpecDto InlineExtraction { get; set; } = new();

        public string ScoreStrategy { get; set; } = "ALL_OR_NOTHING"; // ALL_OR_NOTHING, PARTIAL_CREDIT, FATAL_LOCK, EXACT_OR_EQUIVALENT_MATH, RUBRIC_STEPPED
        public string? RubricStepRegexPattern { get; set; }

        // Tâm trắc học hiện đại (Modern Psychometrics & Bloom Taxonomy)
        public double NegativeMarkingPenalty { get; set; } = 0.0;
        public string DefaultCognitiveLevel { get; set; } = "UNDERSTAND"; // REMEMBER, UNDERSTAND, APPLY, ANALYZE
        public double NumericTolerance { get; set; } = 0.01;
    }

    /// <summary>Đặc tả trích xuất đáp án inline theo từng loại câu hỏi (Group 1 = giá trị đáp án).</summary>
    public class InlineAnswerExtractSpecDto
    {
        public List<string> SingleChoiceExtractionPatterns { get; set; } = new()
        {
            @"(?:^|[\.\;\,\!\s]+)(?:Do\s*đó|Suy\s*ra|Vậy|Hence|Therefore|Par\s*conséquent|Folglich)?\s*(?:ta\s*)?(?:Chọn|Ch[oọ\?]n|Choose|Option|Select|Choisir)\s*(?:câu\s*|đáp\s*án\s*|phương\s*án\s*)?([A-G])\b",
            @"(?:^|[\.\;\,\!\s]+)(?:Do\s*đó|Suy\s*ra|Vậy|Hence|Therefore)?\s*(?:ta\s*có\s*)?(?:đáp\s*án|dap\s*an|phương\s*án|answer|key|réponse)(?:\s*(?:đúng\s*)?là|\s*is|\s*=)?[\:\s]+([A-G])\b",
            @"\blà\s+đáp\s*án\s+([A-G])\b",
            @"\b([A-G])\s*(?:là\s*đáp\s*án|đúng|is\s+the\s+(?:correct\s+)?answer|is\s+correct|est\s+la\s+bonne\s+réponse)\b",
            @"(?:故选|答案为|正确答案是|正确选项是)[\:\s]*([A-G])\b"
        };

        public List<string> NumericValueExtractionPatterns { get; set; } = new()
        {
            @"(?:^|[\.\;\,\!\s]+)(?:Do\s*đó|Suy\s*ra|Vậy|Hence|Therefore)?\s*(?:Đáp\s*số|Đáp\s*án|Dap\s*so|Dap\s*an|Kết\s*quả|Giá\s*trị\s*cần\s*tìm|Result|Answer|Value|Valeur)(?:\s*(?:đúng\s*)?(?:là|bằng|\s*is|\s*=))?[\.\:\s\=]+\$?([0-9\,\.\-\+\/]+)\$?",
            @"(?:计算结果为|答案为|最终结果|结果是)[\.\:\s\=]+([0-9\,\.\-\+\/]+)"
        };

        /// <summary>Group 1 = nhãn mệnh đề (a-d), Group 2 = giá trị chân lý.</summary>
        public List<string> TrueFalseExtractionPatterns { get; set; } = new()
        {
            @"(?:^|[\(\s\[])([a-d1-4])[\)\.\:\]]\s*.*?\b(?:chọn\s+)?(đúng|sai|d|s|true|false|t|f|vrai|faux|richtig|falsch)\b",
            @"\b(?:ý|mệnh\s*đề|khẳng\s*định|statement|proposition|assertion)\s+([a-d1-4])\b.*?\b(?:chọn\s+)?(đúng|sai|d|s|true|false|t|f|vrai|faux|richtig|falsch)\b",
            @"(?:^|[\(\s\[])([a-d1-4])[\)\.\:\]]\s*.*?\b(正确|错误|对|错)\b"
        };

        /// <summary>Mẫu nhận diện ý con / SubItem (Group 1 = nhãn a-d/1-4, Group 2 = nội dung còn lại).</summary>
        public List<string> SubItemMarkerPatterns { get; set; } = new()
        {
            @"^(?:(?:ý|mệnh\s*đề|khẳng\s*định|bước|phần|statement|proposition|item|option|step|part)\s*)?[\(\[]?([a-d1-4])[\)\]\.\:]\s*(?:[\-\:\.]\s*)?(.*)",
            @"^(?:第)?\s*([1-4a-dA-D])\s*[小题项点\.\:\)\-]\s*(.*)"
        };

        /// <summary>Mẫu nhận diện dòng bắt đầu Lời giải / Hướng dẫn giải (Group 1 = nội dung sau nhãn lời giải).</summary>
        public List<string> ExplanationStartPatterns { get; set; } = new()
        {
            @"^(?:(?:Lời\s*giải|L[.\?\s]*i\s*gi[.\?\s]*i|Hướng\s*dẫn\s*(?:giải|chấm|ý\s*giải)|Hu?ng\s*d?n\s*(?:gi?i|ch?m)|HƯỚNG\s*DẪN|LỜI\s*GIẢI|HD|Giải\s*thích|Gợi\s*ý(?:\s*làm\s*bài|\s*giải)?|Ý\s*tưởng\s*giải|Sơ\s*đồ\s*giải|Biểu\s*điểm|Thang\s*điểm|Explanation|Rationale|Rubric|Marking\s*Scheme|Solution|Model\s*Answer|Detailed\s*Solution|Explication|Lösung)[\.\:\s\-]+|Chọn\s+[A-D0-9,\.\-]+\s*[\.\:\s\-]+)(.*)",
            @"^(?:参考答案|试题解析|详细解析|解答|解析)[\.\:\s\-]+(.*)"
        };

        /// <summary>Mẫu trích xuất số câu hỏi (Group 1 = số câu hỏi).</summary>
        public List<string> QuestionNumberPatterns { get; set; } = new()
        {
            @"^(?:Câu|Bài|Question|Item|Aufgabe|Exercice|CÂU|BÀI|Problem|Task|Frage|Domanda|Pregunta|第)?\s*(\d+)",
            @"^(\d+)\s*[\.\:\)\-]"
        };
    }

    /// <summary>Chiều 5: Vòng đời tài liệu đề thi — biên giới đề, chủ đề, phân đoạn, pha lời giải và bảng đáp án</summary>
    public class ExamLifecycleSpecDto
    {
        /// <summary>Group 1 = mã đề / số đề.</summary>
        public List<string> ExamHeadingPatterns { get; set; } = new()
        {
            @"^(?:ĐỀ|D[E|Ê]|EXAM|TEST|PAPER|SET|MOCK)\s*(?:SỐ|THI|ÔN|NO|NUM)?\s*[:\-–—]?\s*(\d+|[A-Z])\b",
            @"^(?:试卷|模拟试题)\s*([0-9一二三四五六七八九十]+)"
        };

        /// <summary>Group 1 = tên chủ đề / môn học.</summary>
        public List<string> TopicHeadingPatterns { get; set; } = new()
        {
            @"^(?:Chủ\s*đề|Chương|Lĩnh\s*vực|Môn\s*thi|Môn|Học\s*phần|Mã\s*CĐ|Topic|Subject|Module|Course|Unit)[\.\:\s\-]+([^\n\r]+)",
            @"^(?:科目|课程|单元)[\.\:\s\-]+([^\n\r]+)"
        };

        public List<string> SolutionPhasePatterns { get; set; } = new()
        {
            @"^(?:LỜI\s*GIẢI|L[\.\?\s]*I\s*GI[\.\?\s]*I|HƯỚNG\s*DẪN\s*GIẢI|HU[\.\?\s]*NG\s*D[\.\?\s]*N\s*GI[\.\?\s]*I|SOLUTIONS?|EXPLANATIONS?|MARK\s*SCHEME|RATIONALE)\s+(?:THAM\s*KHẢO|CHI\s*TIẾT|DETAILS?|APPENDIX)[.\:\s\-]*$",
            @"^(?:参考答案|试题解析|详细解析)[.\:\s\-]*$"
        };

        public List<string> AnswerPhasePatterns { get; set; } = new()
        {
            @"^(?:ĐÁP\s*ÁN|D[\.\?\s]*P\s*[\.\?\s]*N|ANSWER\s*KEY|KEY|ANSWERS?)\s*(?:THAM\s*KHẢO|CHI\s*TIẾT)?[.\:\s\-]*$",
            @"^(?:答案|参考答案)[.\:\s\-]*$"
        };

        /// <summary>Mẫu nhận diện ranh giới kết thúc đề thi (HẾT, THE END, END OF TEST, END OF PAPER...).</summary>
        public List<string> EndOfExamPatterns { get; set; } = new()
        {
            @"^(?:[\-\–\—_*\=.\s]*\b(?:HẾT|HET|THE\s*END|END\s*OF\s*(?:TEST|EXAM|PAPER)|HẾT\s+ĐỀ\s+THI)\b[\-\–\—_*\=.\s]*)$",
            @"^(?:[\-\–\—_\s]*HẾT[\-\–\—_\s]*)$"
        };

        /// <summary>Mẫu nhận diện chỉ thị coi thi / ghi chú phụ ngoài nội dung câu hỏi.</summary>
        public List<string> ProctorInstructionPatterns { get; set; } = new()
        {
            @"(?i)(?:Cán\s*bộ\s*coi\s*thi|Giám\s*thị|Giám\s*khảo)\s+không\s+giải\s+thích(?:\s+gì)?\s+thêm",
            @"(?i)Thí\s*sinh\s+không\s+được\s+sử\s+dụng\s+tài\s+liệu",
            @"(?i)Họ\s*(?:và)?\s*tên\s+thí\s*sinh[\.\:\s_]+Số\s*báo\s*danh"
        };

        /// <summary>Ánh xạ tiêu đề phân đoạn sang loại câu hỏi (SINGLE, TRUE_FALSE, SHORT_ANSWER, ESSAY).</summary>
        public Dictionary<string, List<string>> SectionHeaderMapping { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            ["SINGLE"] = new() { @"\b(?:PHẦN|PHAN|PART|SECTION)\s+(?:I|1|A)\b", @"\bTRẮC\s*NGHIỆM\b", @"\bMULTIPLE\s*CHOICE\b", @"\b单项选择\b" },
            ["TRUE_FALSE"] = new() { @"\b(?:PHẦN|PHAN|PART|SECTION)\s+(?:II|2|B)\b", @"\bĐÚNG\s*SAI\b", @"\bTRUE[\s\/]*FALSE\b", @"\b判断题\b" },
            ["SHORT_ANSWER"] = new() { @"\b(?:PHẦN|PHAN|PART|SECTION)\s+(?:III|3|C)\b", @"\bTRẢ\s*LỜI\s*NGẮN\b", @"\bSHORT\s*ANSWER\b", @"\b填空题\b" },
            ["ESSAY"] = new() { @"\b(?:PHẦN|PHAN|PART|SECTION)\s+(?:IV|4|D)\b", @"\bTỰ\s*LUẬN\b", @"\bESSAY\b", @"\bFREE\s*RESPONSE\b", @"\b解答题\b", @"\bXỬ\s*LÝ\s*TÌNH\s*HUỐNG\b" }
        };
    }

    /// <summary>Chiều 6: Bố cục hình học không gian trang & dòng dữ liệu</summary>
    public class TopologySpecDto
    {
        public string LayoutMode { get; set; } = "SINGLE_COLUMN"; // SINGLE_COLUMN, DUAL_COLUMN_NEWSPAPER, GRID_MATRIX
        public bool AllowInlineHorizontalOptions { get; set; } = true;
        public bool DetectEmbeddedDataTables { get; set; } = true;
        public bool SupportRightFloatedMedia { get; set; } = true;
    }

    /// <summary>Chiều 7: Hạt nhân tự tiến hóa, chẩn đoán dị biệt & entropy nhận thức</summary>
    public class MetaEvolutionSpecDto
    {
        public bool IsAiGenerated { get; set; } = false;
        public double ConfidenceThreshold { get; set; } = 90.0; // 0.0 - 100.0%
        public double MaxEntropyTolerance { get; set; } = 0.25;
        public string SyntaxFingerprint { get; set; } = string.Empty;
        public int GenerationIndex { get; set; } = 1;
    }

    /// <summary>Đặc tả bóc tách Ma trận Bảng Đáp Án tách rời (Detached Matrix Table)</summary>
    public class TableMatrixSpecDto
    {
        public List<string> HeaderPatterns { get; set; } = new();
        public string Orientation { get; set; } = "AUTO_DETECT";
        public string CellPattern { get; set; } = @"^(?:(\d+)[\.\-\s\:]+([A-D]|Đ|S|[\\-\\+]?\\d+[\\,\\.]?\\d*))$";

        /// <summary>Hỗ trợ xử lý các ô gộp (Merged Cells w:gridSpan / w:vMerge) trong bảng đáp án ma trận.</summary>
        public bool SupportMergedCells { get; set; } = true;

        /// <summary>Hỗ trợ bảng đáp án chia nhánh đa cột dọc (tiết kiệm trang in).</summary>
        public bool MultiColumnBranching { get; set; } = true;

        /// <summary>Mẫu bóc tách tổ hợp nhiều đáp án trong cùng 1 ô hoặc chuỗi viết liền.</summary>
        public string? CompositeCellPattern { get; set; } = @"\b(\d+)[\.\:\-\s]+([A-D]|Đ|S)\b";
    }

    /// <summary>Yêu cầu kiểm thử nhanh một đoạn snippet đề thi từ Admin UI Sandbox</summary>
    public class ParseSnippetRequestDto
    {
        public string SnippetText { get; set; } = string.Empty;
        public string? SpecificRuleCode { get; set; }
    }

    /// <summary>Yêu cầu AI tự động phân tích đề thi lạ và sinh quy tắc CIG</summary>
    public class AiRuleAutopilotRequestDto
    {
        public string SnippetText { get; set; } = string.Empty;
        public string? ExamHint { get; set; }
        public string Language { get; set; } = "vi";
    }

    /// <summary>Kết quả AI Autopilot tự sinh quy tắc</summary>
    public class AiRuleAutopilotResponseDto
    {
        public bool Success { get; set; } = true;
        public double ConfidenceScore { get; set; } = 95.0; // 0.0 - 100.0%
        public string Reasoning { get; set; } = string.Empty;
        public QuestionParsingRuleDto GeneratedRule { get; set; } = new();
        public ParseSnippetResponseDto TestedParseResult { get; set; } = new();
    }

    /// <summary>Kết quả kiểm thử phân tích đoạn văn bản mẫu đề thi</summary>
    public class ParseSnippetResponseDto
    {
        public bool Success { get; set; } = true;
        public string? MatchedRuleCode { get; set; }
        public int ParsedCount { get; set; } = 0;
        public int TotalQuestionsFound => ParsedCount > 0 ? ParsedCount : Questions.Count;
        public List<DocxImportPreviewDto> Questions { get; set; } = new();
        public List<string> ParsingLogs { get; set; } = new();
    }

    /// <summary>Yêu cầu kiểm thử ngầm (Shadow Testing) cho quy tắc đề xuất trước khi kích hoạt</summary>
    public class ShadowTestRequestDto
    {
        public QuestionParsingRuleDto ProposedRule { get; set; } = new();
        public List<string>? CustomTestSamples { get; set; }
        public bool AutoPromote { get; set; } = false;
        public int MaxTimeoutMs { get; set; } = 3000;
    }

    /// <summary>Kết quả kiểm thử ngầm (Shadow Testing Pipeline)</summary>
    public class ShadowTestResultDto
    {
        public bool Passed { get; set; } = true;
        public double SafetyScore { get; set; } = 100.0; // 0.0 - 100.0%
        public int TotalSamplesTested { get; set; } = 0;
        public int RegressionsCount { get; set; } = 0;
        public int TargetMatchesCount { get; set; } = 0;
        public double AverageExecutionMs { get; set; } = 0.0;
        public List<ShadowTestDetailDto> Details { get; set; } = new();
        public string Recommendation { get; set; } = string.Empty;
        public string? Note { get; set; }
        public bool AutoPromoted { get; set; } = false;
    }

    /// <summary>Chi tiết kết quả từng mẫu thử nghiệm trong Shadow Run</summary>
    public class ShadowTestDetailDto
    {
        public string SampleName { get; set; } = string.Empty;
        public string SampleSnippet { get; set; } = string.Empty;
        public string ExpectedRuleCode { get; set; } = string.Empty;
        public string? ActualMatchedRuleCode { get; set; }
        public bool IsRegression { get; set; } = false;
        public bool IsTargetMatch { get; set; } = false;
        public double ExecutionMs { get; set; } = 0.0;
        public string Status { get; set; } = "PASS";
        public string? Note { get; set; }
    }

    /// <summary>
    /// [SGI - Scope-Guided Directive Window]
    /// Đại diện cho một Cửa sổ Chỉ thị Phạm vi: Từ câu StartQ đến EndQ có hành vi tương tác QuestionType.
    /// Triệt tiêu hoàn toàn sự phụ thuộc cứng nhắc vào các tiêu đề phân đoạn (PHẦN I, II, III).
    /// </summary>
    public class DirectiveScopeWindow
    {
        public int StartQuestion { get; set; }
        public int EndQuestion { get; set; }
        public string QuestionType { get; set; } = "SINGLE";
        public string? RawDirective { get; set; }
        public double Confidence { get; set; } = 1.0;

        public bool Contains(int qNum) => qNum >= StartQuestion && qNum <= EndQuestion;
    }

    /// <summary>
    /// [Context Stimulus Window]
    /// Quản lý ngữ cảnh bài đọc hiểu hoặc tình huống áp dụng cho dải câu hỏi [StartQ, EndQ].
    /// </summary>
    public class ContextStimulusWindow
    {
        public int StartQuestion { get; set; }
        public int EndQuestion { get; set; }
        public string ContextId { get; set; } = string.Empty;
        public string ContextTitle { get; set; } = string.Empty;
        public string ContextContent { get; set; } = string.Empty;

        public bool Contains(int qNum) => qNum >= StartQuestion && qNum <= EndQuestion;
    }
}
