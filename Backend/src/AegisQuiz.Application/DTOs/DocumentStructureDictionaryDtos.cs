using System;
using System.Collections.Generic;

namespace AegisQuiz.Application.DTOs
{
    /// <summary>
    /// Từ điển cấu trúc văn bản toàn diện (Document Structure Taxonomy Dictionary).
    /// Cấu hình hóa 100% (Zero-Hardcode) việc nhận diện Phân đoạn, Lời giải, Bảng đáp án, Biên giới đề thi,
    /// chữ ký ngôn ngữ (Language Signatures) và bảng phân loại nhanh O(1) (Fast Classification Spec).
    /// Nạp từ khóa <c>documentStructureDictionary</c> trong <c>parsing_rules_registry.json</c>.
    /// </summary>
    public class DocumentStructureDictionaryDto
    {
        /// <summary>Ánh xạ tiêu đề phân đoạn → loại câu hỏi (SINGLE, TRUE_FALSE, SHORT_ANSWER, ESSAY).</summary>
        public Dictionary<string, List<string>> SectionMarkers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Mẫu nhận diện dòng chuyển sang pha "Lời giải tham khảo / Hướng dẫn chấm".</summary>
        public List<string> SolutionPhaseMarkers { get; set; } = new();

        /// <summary>Mẫu nhận diện dòng tiêu đề "Bảng đáp án".</summary>
        public List<string> AnswerPhaseMarkers { get; set; } = new();

        /// <summary>Mẫu nhận diện biên giới đề thi (Group 1 = mã đề).</summary>
        public List<string> ExamBoundaryMarkers { get; set; } = new();

        /// <summary>Mẫu nhận diện tiêu đề chủ đề / môn học (Group 1 = tên chủ đề).</summary>
        public List<string> TopicHeadingPatterns { get; set; } = new();

        /// <summary>Mẫu nhận diện câu điểm liệt / câu bắt buộc đúng ở cấp tài liệu.</summary>
        public List<string> CriticalQuestionMarkers { get; set; } = new();

        /// <summary>Mẫu nhận diện dòng mở đầu bài đọc hiểu ở cấp tài liệu.</summary>
        public List<string> PassageStartPatterns { get; set; } = new();

        /// <summary>Mẫu bóc tách phạm vi câu hỏi của bài đọc (Group 1 = từ, Group 2 = đến).</summary>
        public List<string> PassageRangePatterns { get; set; } = new();

        /// <summary>Đặc tả bảng đáp án ma trận (tiêu đề cột Câu / Đáp án).</summary>
        public AnswerTableDictionaryDto AnswerTableSpecs { get; set; } = new();

        /// <summary>Đặc tả trích xuất đáp án inline ở cấp tài liệu (ưu tiên thấp hơn đặc tả của quy tắc).</summary>
        public InlineAnswerExtractSpecDto? InlineAnswerSpecs { get; set; }

        /// <summary>Chữ ký nhận diện ngôn ngữ theo mã ISO-639-1 ("vi", "en", "zh", "ko", "ja", "fr", "de"...).</summary>
        public Dictionary<string, LanguageSignatureDto> LanguageSignatures { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Bảng tiền tố / ký tự phân loại nhanh O(1) từng dòng văn bản.</summary>
        public FastClassificationSpecDto FastClassificationSpec { get; set; } = new();
    }

    /// <summary>Cấu hình đặc tả bảng đáp án động</summary>
    public class AnswerTableDictionaryDto
    {
        public string QuestionHeaderPattern { get; set; } = @"^(?:Câu|C[\.\?\s]*u|CÂU|TT|Q|Question)$";
        public string AnswerHeaderPattern { get; set; } = @"^(?:Chọn|Ch[\.\?\s]*n|Đáp\s*án|D[\.\?\s]*p\s*[\.\?\s]*n|Answer|Key|Đ[\/\.]?A|Phương\s*án)$";
        public List<string> TrueFalseColumnLabels { get; set; } = new() { "a", "b", "c", "d" };
    }

    /// <summary>
    /// Chữ ký nhận diện một ngôn ngữ dùng cho thuật toán chấm điểm vector ngôn ngữ
    /// (Score = w_script·Script + w_diacritic·Diacritic + w_kw·Keyword).
    /// </summary>
    public class LanguageSignatureDto
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Biểu thức nhận diện hệ chữ viết đặc thù (Hangul, Kana, Hanzi...). Mỗi ký tự khớp = 1 điểm script.</summary>
        public List<string> ScriptPatterns { get; set; } = new();

        /// <summary>Biểu thức nhận diện ký tự có dấu đặc thù (nguyên âm tiếng Việt, é/è/ç tiếng Pháp, ä/ö/ü/ß tiếng Đức).</summary>
        public string? DiacriticPattern { get; set; }

        /// <summary>Từ khóa định danh ngữ cảnh (Câu, Question, Aufgabe, Exercice, 다음, 下列...). Mỗi từ khớp = 1 điểm keyword.</summary>
        public List<string> KeywordHints { get; set; } = new();

        /// <summary>
        /// Khi true: quy tắc mang ngôn ngữ này chỉ được thử khớp điểm neo nếu dòng có bằng chứng
        /// (script, dấu hoặc từ khóa) của ngôn ngữ đó — chống nhận diện chéo (False-Positive).
        /// </summary>
        public bool RequireEvidenceForAnchor { get; set; } = false;
    }

    /// <summary>Bảng tiền tố phân loại nhanh từng dòng văn bản (không dùng Regex, độ phức tạp O(1)).</summary>
    public class FastClassificationSpecDto
    {
        public List<string> SectionPrefixes { get; set; } = new();
        public List<string> ExamPrefixes { get; set; } = new();
        public List<string> QuestionPrefixes { get; set; } = new();
        public List<string> QuestionAnchorChars { get; set; } = new();
        public List<string> OptionDelimiters { get; set; } = new();
        public List<CharRangeDto> OptionCharRanges { get; set; } = new();
        public List<string> EvaluationPrefixes { get; set; } = new();
    }

    /// <summary>Khoảng ký tự [Start..End] dùng cho nhãn phương án (A-Z, a-z, 1-9).</summary>
    public class CharRangeDto
    {
        public string Start { get; set; } = "A";
        public string End { get; set; } = "Z";
    }

    /// <summary>
    /// Hồ sơ ngôn ngữ 2 lớp cấp tài liệu (Dual-Layer Language Cognition):
    /// <see cref="HostLanguage"/> = ngôn ngữ bản địa của khung tài liệu (tiêu đề trường, đề, phân đoạn, lời giải);
    /// <see cref="SubjectLanguage"/> = ngôn ngữ chuyên môn của câu hỏi và phương án.
    /// </summary>
    public class DocumentLanguageProfileDto
    {
        public string HostLanguage { get; set; } = "vi";
        public string SubjectLanguage { get; set; } = "vi";

        /// <summary>Đề song ngữ khi ngôn ngữ khung khác ngôn ngữ chuyên môn (ví dụ đề Tiếng Anh tại Việt Nam).</summary>
        public bool IsBilingual => !string.Equals(HostLanguage, SubjectLanguage, StringComparison.OrdinalIgnoreCase);

        /// <summary>Độ tin cậy 0 - 100 (%).</summary>
        public double Confidence { get; set; }

        /// <summary>Điểm số tổng theo từng ngôn ngữ (phục vụ chẩn đoán).</summary>
        public Dictionary<string, double> LanguageScores { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public int SampledParagraphs { get; set; }

        public override string ToString()
            => $"Host={HostLanguage} | Subject={SubjectLanguage} | Bilingual={IsBilingual} | Confidence={Confidence:F1}% | Sampled={SampledParagraphs}";
    }
}
