using System.Collections.Generic;

namespace AegisQuiz.Application.DTOs
{
    /// <summary>
    /// Hồ sơ năng lực của một dạng câu hỏi (OCP): thêm dạng câu hỏi mới chỉ cần khai báo hồ sơ trong
    /// từ điển cấu trúc (JSON/DB), KHÔNG sửa mã lõi của bộ phân loại Docx.
    /// </summary>
    public class QuestionTypeProfileDto
    {
        /// <summary>Thứ tự ưu tiên khi chuẩn hóa tên phần → khóa phân đoạn (nhỏ hơn = kiểm tra trước).</summary>
        public int AliasOrder { get; set; } = 100;

        /// <summary>Khóa phân đoạn chuẩn mà các tên phần khớp <see cref="SectionAliasPatterns"/> được quy về (rỗng = chính dạng này).</summary>
        public string? CanonicalSectionKey { get; set; }

        /// <summary>Tập năng lực: FREE_TEXT, CHOICE, TRUE_FALSE_FAMILY, NUMERIC_ANSWER.</summary>
        public List<string> Capabilities { get; set; } = new();

        /// <summary>Các dạng tương đương khi tra cứu ĐỀ_PHẦN_CÂU (chứa cả chính nó).</summary>
        public List<string> EquivalentTypes { get; set; } = new();

        /// <summary>Độ khó mặc định khi chưa có dữ liệu IRT.</summary>
        public int DefaultDifficulty { get; set; } = 4;

        /// <summary>Regex đa ngôn ngữ nhận diện tên phần/section thuộc dạng này (so khớp trên chuỗi viết hoa).</summary>
        public List<string> SectionAliasPatterns { get; set; } = new();

        public const string CapFreeText = "FREE_TEXT";
        public const string CapChoice = "CHOICE";
        public const string CapTrueFalseFamily = "TRUE_FALSE_FAMILY";
        public const string CapNumericAnswer = "NUMERIC_ANSWER";
        public const string CapSubItems = "SUB_ITEMS";
        public const string CapSingleAnswer = "SINGLE_ANSWER";
        public const string CapMultiStatement = "MULTI_STATEMENT";

        public static Dictionary<string, QuestionTypeProfileDto> GetDefaults() => new()
        {
            ["SHORT_ANSWER"] = new()
            {
                AliasOrder = 10, CanonicalSectionKey = "SHORT_ANSWER", DefaultDifficulty = 4,
                Capabilities = { CapFreeText, CapNumericAnswer },
                EquivalentTypes = { "SHORT_ANSWER" },
                SectionAliasPatterns = { @"III", @"SHORT_ANSWER", @"TRẢ LỜI NGẮN", @"ĐIỀN SỐ", @"SHORT ANSWER" }
            },
            ["MULTI_TRUE_FALSE"] = new()
            {
                AliasOrder = 20, CanonicalSectionKey = "MULTI_TRUE_FALSE", DefaultDifficulty = 4,
                Capabilities = { CapTrueFalseFamily, CapSubItems, CapMultiStatement },
                EquivalentTypes = { "MULTI_TRUE_FALSE", "TRUE_FALSE" },
                SectionAliasPatterns = { @"II", @"TRUE_FALSE", @"MULTI_TRUE_FALSE", @"ĐÚNG SAI", @"TRUE[ /-]FALSE" }
            },
            ["TRUE_FALSE"] = new()
            {
                AliasOrder = 25, CanonicalSectionKey = "MULTI_TRUE_FALSE", DefaultDifficulty = 3,
                Capabilities = { CapTrueFalseFamily, CapSubItems },
                EquivalentTypes = { "MULTI_TRUE_FALSE", "TRUE_FALSE" }
            },
            ["ESSAY"] = new()
            {
                AliasOrder = 30, CanonicalSectionKey = "ESSAY", DefaultDifficulty = 4,
                Capabilities = { CapFreeText, CapSubItems },
                EquivalentTypes = { "ESSAY" },
                SectionAliasPatterns = { @"ESSAY", @"TỰ LUẬN", @"FREE[ -]RESPONSE" }
            },
            ["SINGLE"] = new()
            {
                AliasOrder = 40, CanonicalSectionKey = "SINGLE", DefaultDifficulty = 2,
                Capabilities = { CapChoice, CapSingleAnswer },
                EquivalentTypes = { "SINGLE", "MULTI" },
                SectionAliasPatterns = { @"SINGLE", @"^PHẦN I$", @"^PART I$", @"TRẮC NGHIỆM", @"MULTIPLE[ -]CHOICE" }
            },
            ["MULTI"] = new()
            {
                AliasOrder = 45, CanonicalSectionKey = "SINGLE", DefaultDifficulty = 2,
                Capabilities = { CapChoice },
                EquivalentTypes = { "SINGLE", "MULTI" }
            }
        };

        /// <summary>Mẫu ngôn ngữ học đa ngôn ngữ (khóa → danh sách regex) dùng cho bộ phân loại Docx, thay cho regex nhúng trong mã.</summary>
        public static Dictionary<string, List<string>> GetDefaultLinguisticPatterns() => new()
        {
            // Group 1 = tiêu đề phần (PHẦN IV / PART IV / SECTION IV / TEIL IV / PARTIE IV / 第IV部分)
            [LinguisticKeys.SectionTitle] = new()
            {
                @"(?:^|\b)(PHẦN\s+[IVXLCDM\d]+|PART\s+[IVXLCDM\d]+|SECTION\s+[IVXLCDM\d]+|TEIL\s+[IVXLCDM\d]+|PARTIE\s+[IVXLCDM\d]+|第\s*[IVXLCDM\d一二三四五六七八九十]+\s*部分)"
            },
            // Dòng chứa từ khóa tiêu đề vĩ mô (không được coi là lời giải inline)
            [LinguisticKeys.MacroPhaseHint] = new()
            {
                @"\b(?:CHI\s*TIẾT|THAM\s*KHẢO|VÀ\s*(?:BIỂU|THANG)?\s*ĐIỂM|PHẦN|PART|SECTION|BẢNG|TEIL|PARTIE)\b",
                @"(?:详细|参考|部分)"
            },
            // Tiêu đề lời giải trần (không kèm nội dung)
            [LinguisticKeys.BareExplanationHeader] = new()
            {
                @"^(?:Lời\s*giải|L?i\s*gi?i|Hướng\s*dẫn\s*giải|Hu?ng\s*d?n\s*gi?i|HƯỚNG\s*DẪN|LỜI\s*GIẢI|HD|Explanation|Rationale|Lösung|Solution|解答|解析)[\.\:\s\-]*$"
            },
            // Group 1 = đáp án trắc nghiệm inline
            [LinguisticKeys.ChoiceAnswerLine] = new()
            {
                @"^(?:ĐÁP\s*ÁN|ANSWER|KEY|CHỌN|ANTWORT|RÉPONSE|答案)[\.\:\s\-]+([A-G0-9a-g\,\s]+)$"
            },
            // Group 1 = phần còn lại sau mã câu
            [LinguisticKeys.StandardQuestionAnchor] = new()
            {
                @"^(?:Câu|Question|Item|Task|Exercise|Q|Bài|Bai|Frage|Aufgabe|Exercice|问题|题)\s*\d+(?:[\.\:\s\-]+(.*)|$)"
            },
            // Group 1 = nội dung ghi chú giải thích inline
            [LinguisticKeys.InlineExplanationNote] = new()
            {
                @"^(?:Giải\s*thích|Căn\s*cứ|Ghi\s*chú|Explanation|Note|Erklärung|解释|说明)[\.\:\s]+(.*)"
            }
        };
    }

    /// <summary>Khóa chuẩn của các mẫu ngôn ngữ học trong từ điển cấu trúc.</summary>
    public static class LinguisticKeys
    {
        public const string SectionTitle = "sectionTitle";
        public const string MacroPhaseHint = "macroPhaseHint";
        public const string BareExplanationHeader = "bareExplanationHeader";
        public const string ChoiceAnswerLine = "choiceAnswerLine";
        public const string StandardQuestionAnchor = "standardQuestionAnchor";
        public const string InlineExplanationNote = "inlineExplanationNote";
    }
}
