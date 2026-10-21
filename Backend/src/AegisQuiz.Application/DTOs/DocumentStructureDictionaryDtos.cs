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
        public Dictionary<string, List<string>> SectionMarkers { get; set; } = GetDefaultSectionMarkers();

        public static Dictionary<string, List<string>> GetDefaultSectionMarkers() => new(StringComparer.OrdinalIgnoreCase)
        {
            ["ESSAY"] = new()
            {
                @"(?i)(?:TỰ\s*LUẬN|TU\s*LUAN|\bESSAY\b|FREE\s*RESPONSE|XỬ\s*LÝ\s*TÌNH\s*HUỐNG|解答题)",
                @"^(?:PHẦN\s+)?TỰ\s*LUẬN",
                @"^BÀI\s*TẬP\s*TỰ\s*LUẬN",
                @"^(?:PHẦN\s+)?XỬ\s*LÝ\s*TÌNH\s*HUỐNG"
            },
            ["SHORT_ANSWER"] = new()
            {
                @"(?i)(?:TRẢ\s*LỜI\s*NGẮN|TRA\s*LOI\s*NGAN|SHORT\s*ANSWER|ĐIỀN\s*(?:ĐÁP\s*SỐ|KẾT\s*QUẢ)|填空题)",
                @"^(?:PHẦN|PHAN)\s+III\b",
                @"^(?:PART|SECTION)\s+III\b",
                @"^(?:PHẦN|PHAN)\s+3\b",
                @"^(?:CÂU\s*)?TRẢ\s*LỜI\s*NGẮN$",
                @"^ĐIỀN\s*(?:ĐÁP\s*SỐ|KẾT\s*QUẢ)$"
            },
            ["TRUE_FALSE"] = new()
            {
                @"(?i)(?:ĐÚNG\s*[-–/\s]*SAI|DUNG\s*[-–/\s]*SAI|TRUE\s*[-–/\s]*FALSE|判断题)",
                @"^(?:PHẦN|PHAN)\s+II(?:[\.\:\s\-]+.*(?:ĐÚNG|TRUE|SAI|FALSE).*)?$",
                @"^(?:PART|SECTION)\s+II(?:[\.\:\s\-]+.*(?:TRUE|FALSE|ĐÚNG|SAI).*)?$",
                @"^(?:PHẦN|PHAN)\s+2(?:[\.\:\s\-]+.*(?:ĐÚNG|TRUE|SAI|FALSE).*)?$",
                @"^TRẮC\s*NGHIỆM\s*ĐÚNG\s*[-–\s\/]*SAI",
                @"^CÂU\s*HỎI\s*ĐÚNG\s*[-–\s\/]*SAI"
            },
            ["SINGLE"] = new()
            {
                @"(?i)(?:TRẮC\s*NGHIỆM(?:\s*(?:NHIỀU\s*LỰA\s*CHỌN|ĐƠN|4\s*PHƯƠNG\s*ÁN))?|MULTIPLE\s*CHOICE|ĐƠN\s*LỰA\s*CHỌN|NHIỀU\s*LỰA\s*CHỌN|单项选择|ĐỌC\s*HIỂU|READING|LISTENING)",
                @"^(?:PHẦN|PHAN)\s+I\b",
                @"^(?:PART|SECTION)\s+I\b",
                @"^(?:PHẦN|PHAN)\s+1\b",
                @"^(?:CHƯƠNG|CHUONG)\s+[I|V|X\d]+",
                @"^TRẮC\s*NGHIỆM(?:\\s*(?:NHIỀU\s*LỰA\s*CHỌN|ĐƠN|4\s*PHƯƠNG\s*ÁN))?$"
            }
        };

        /// <summary>Mẫu nhận diện dòng chuyển sang pha "Lời giải tham khảo / Hướng dẫn chấm".</summary>
        public List<string> SolutionPhaseMarkers { get; set; } = GetDefaultSolutionPhaseMarkers();

        /// <summary>Mẫu nhận diện dòng tiêu đề "Bảng đáp án".</summary>
        public List<string> AnswerPhaseMarkers { get; set; } = GetDefaultAnswerPhaseMarkers();

        /// <summary>Mẫu nhận diện dòng mở đầu Lời giải / Hướng dẫn giải / Ý tưởng giải / Biểu điểm (cấu hình động 100%).</summary>
        public List<string> ExplanationMarkers { get; set; } = GetDefaultExplanationMarkers();

        /// <summary>Mẫu nhận diện biên giới đề thi (Group 1 = mã đề).</summary>
        public List<string> ExamBoundaryMarkers { get; set; } = new()
        {
            @"^(?:ĐỀ\s*(?:THI)?\s*(?:SỐ|KIỂM\s*TRA\s*SỐ|MT)?|BỘ\s*ĐỀ\s*SỐ)\s*(\d+)",
            @"^MÃ\s*ĐỀ\s*[:\-]?\s*(\d+)"
        };

        /// <summary>Mẫu nhận diện tiêu đề chủ đề / môn học (Group 1 = tên chủ đề).</summary>
        public List<string> TopicHeadingPatterns { get; set; } = new();

        /// <summary>Mẫu nhận diện câu điểm liệt / câu bắt buộc đúng ở cấp tài liệu.</summary>
        public List<string> CriticalQuestionMarkers { get; set; } = new();

        /// <summary>Mẫu nhận diện dòng mở đầu bài đọc hiểu ở cấp tài liệu.</summary>
        public List<string> PassageStartPatterns { get; set; } = GetDefaultPassageStartPatterns();

        /// <summary>Mẫu bóc tách phạm vi câu hỏi của bài đọc (Group 1 = từ, Group 2 = đến).</summary>
        public List<string> PassageRangePatterns { get; set; } = GetDefaultPassageRangePatterns();

        /// <summary>Hồ sơ năng lực theo dạng câu hỏi (OCP) — thêm dạng mới không cần sửa mã lõi bộ phân loại.</summary>
        public Dictionary<string, QuestionTypeProfileDto> QuestionTypeProfiles { get; set; } = QuestionTypeProfileDto.GetDefaults();

        /// <summary>Mẫu ngôn ngữ học đa ngôn ngữ của bộ phân loại (khóa theo <see cref="LinguisticKeys"/>).</summary>
        public Dictionary<string, List<string>> LinguisticPatterns { get; set; } = QuestionTypeProfileDto.GetDefaultLinguisticPatterns();

        public static List<string> GetDefaultSolutionPhaseMarkers() => new()
        {
            @"^(?:HƯỚNG\s*DẪN\s*(?:CHẤM|GIẢI)|ĐÁP\s*ÁN\s*(?:VÀ|&)?\s*(?:BIỂU|THANG)?\s*ĐIỂM|ĐÁP\s*ÁN\s*THAM\s*KHẢO|ĐÁP\s*ÁN\s*CHI\s*TIẾT|LỜI\s*GIẢI\s*THAM\s*KHẢO|LỜI\s*GIẢI\s*CHI\s*TIẾT|HƯỚNG\s*DẪN\s*GIẢI\s*CHI\s*TIẾT|BIỂU\s*ĐIỂM\s*(?:VÀ|&)?\s*HƯỚNG\s*DẪN|THANG\s*ĐIỂM\s*CHI\s*TIẾT|HƯỚNG\s*DẪN\s*TỰ\s*LUẬN|MARKING\s*SCHEME|RUBRICS?|MODEL\s*ANSWERS?|SOLUTIONS?)[\.\:\s\-]*$",
            @"^(?:(?:BẢNG\s*)?ĐÁP\s*ÁN|D[\.\?\s]*P\s*[\.\?\s]*N)\s+(?:THAM\s*KHẢO|CHI\s*TIẾT|VÀ\s*LỜI\s*GIẢI.*)[\.\:\s\-]*$",
            @"^(?:HƯỚNG\s*DẪN\s*GIẢI\s*VÀ\s*ĐÁP\s*ÁN|ĐÁP\s*ÁN\s*VÀ\s*HƯỚNG\s*DẪN\s*GIẢI)[\.\:\s\-]*$"
        };

        public static List<string> GetDefaultAnswerPhaseMarkers() => new()
        {
            @"^(?:BẢNG\s*)?(?:ĐÁP\s*ÁN|D[\.\?\s]*P\s*[\.\?\s]*N|ANSWER\s*KEY|KEY|ANSWERS?)\s*(?:THAM\s*KHẢO|CHI\s*TIẾT|TRẮC\s*NGHIỆM)?[\.\:\s\-]*$",
            @"^(?:答案|参考答案)[\.\:\s\-]*$"
        };

        public static List<string> GetDefaultExplanationMarkers() => new()
        {
            @"(?i)^(?:Lời\s*giải|L[\.\?\s]*i\s*gi[\.\?\s]*i|Hướng\s*dẫn\s*(?:giải|chấm|ý\s*giải)|Hu?ng\s*d?n\s*(?:gi?i|ch?m)|HƯỚNG\s*DẪN|LỜI\s*GIẢI|HD|Giải\s*thích|Gợi\s*ý(?:\s*làm\s*bài|\s*giải)?|Ý\s*tưởng\s*giải|Sơ\s*đồ\s*giải|Căn\s*cứ|Biểu\s*điểm|Thang\s*điểm|Explanation|Rationale|Rubric|Marking\s*Scheme|Solution|Model\s*Answer|Detailed\s*Solution)[\.\:\s\-]*",
            @"(?i)^(?:Phương\s*pháp\s*giải|Các\s*bước\s*giải|Cách\s*(?:1|2|3|một|hai|ba))[\.\:\s\-]*",
            @"(?i)^(?:Chọn|Đáp\s*án|Đáp\s*số)\s+[A-D0-9,\.\-]+"
        };

        public static List<string> GetDefaultPassageStartPatterns() => new()
        {
            @"(?i)(?:đọc\s+(?:đoạn|bài)\s*văn|đọc\s+thông\s*tin|read\s+the\s+following\s+(?:passage|text|advertisement|announcement)|read\s+the\s+passage).*?(?:trả\s*lời|answer)",
            @"(?i)(?:sử\s*dụng|dựa\s*vào|dựa\s*trên|cho|dùng)\s+thông\s*tin.*?(?:để\s*trả\s*lời|trả\s*lời\s*(?:các\s*)?câu)",
            @"(?i)(?:thông\s*tin|dữ\s*liệu|tình\s*huống)\s+(?:dưới\s*đây|sau)\s+để\s*trả\s*lời",
            @"(?i)^bài\s*đọc\s*hiểu"
        };

        public static List<string> GetDefaultPassageRangePatterns() => new()
        {
            @"(?i)(?:từ\s+(?:các\s+)?câu(?:\s+số)?|ở\s+mỗi\s+câu(?:\s+hỏi)?\s*(?:từ)?|(?:các\s+)?câu(?:\s+số|\s+hỏi)?)\s*(\d+)\s*(?:đến\s*(?:các\s+)?câu(?:\s+số)?|đến|[-–—])\s*(\d+)",
            @"(?i)(?:questions?|items?|from\s+question|for\s+questions?|in\s+questions?)\s*(\d+)\s*(?:to|through|[-–—])\s*(?:questions?\s*)?(\d+)",
            @"(?i)(?:questions?|items?)\s+from\s*(\d+)\s*to\s*(\d+)",
            @"(?i)(?:từ\s+câu|từ)\s*(\d+)\s*(?:đến\s*hết|trở\s*đi)"
        };

        /// <summary>Mẫu nhận diện ranh giới kết thúc đề thi (HẾT, THE END, END OF TEST, END OF PAPER...).</summary>
        public List<string> EndOfExamMarkers { get; set; } = new()
        {
            @"^(?:[\-\–\—_*\=.\s]*\b(?:HẾT|HET|THE\s*END|END\s*OF\s*(?:TEST|EXAM|PAPER)|HẾT\s+ĐỀ\s+THI)\b[\-\–\—_*\=.\s]*)$",
            @"^(?:[\-\–\—_\s]*HẾT[\-\–\—_\s]*)$"
        };

        /// <summary>Mẫu nhận diện chỉ thị coi thi / ghi chú phụ ngoài nội dung câu hỏi (Cán bộ coi thi..., Thí sinh không được...).</summary>
        public List<string> ProctorInstructionMarkers { get; set; } = new()
        {
            @"(?i)(?:Cán\s*bộ\s*coi\s*thi|Giám\s*thị|Giám\s*khảo)\s+không\s+giải\s+thích(?:\s+gì)?\s+thêm",
            @"(?i)Thí\s*sinh\s+không\s+được\s+sử\s+dụng\s+tài\s+liệu",
            @"(?i)Họ\s*(?:và)?\s*tên\s+thí\s*sinh[\.\:\s_]+Số\s*báo\s*danh"
        };

        /// <summary>Đặc tả bảng đáp án ma trận (tiêu đề cột Câu / Đáp án).</summary>
        public AnswerTableDictionaryDto AnswerTableSpecs { get; set; } = new();

        /// <summary>Đặc tả trích xuất đáp án inline ở cấp tài liệu (ưu tiên thấp hơn đặc tả của quy tắc).</summary>
        public InlineAnswerExtractSpecDto InlineAnswerSpecs { get; set; } = new();

        /// <summary>Từ điển danh mục phân loại môn học / chủ đề nạp động từ config/DB.</summary>
        public List<TopicTaxonomyEntryDto> TopicTaxonomyEntries { get; set; } = GetDefaultTopicTaxonomy();

        /// <summary>Từ điển danh mục phân loại miền tri thức (Domain) nạp động từ config/DB.</summary>
        public List<DomainTaxonomyEntryDto> DomainTaxonomyEntries { get; set; } = GetDefaultDomainTaxonomy();

        /// <summary>Chữ ký nhận diện ngôn ngữ theo mã ISO-639-1 ("vi", "en", "zh", "ko", "ja", "fr", "de"...).</summary>
        public Dictionary<string, LanguageSignatureDto> LanguageSignatures { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Bảng tiền tố / ký tự phân loại nhanh O(1) từng dòng văn bản.</summary>
        public FastClassificationSpecDto FastClassificationSpec { get; set; } = new();

        /// <summary>
        /// Danh sách mẫu chỉ thị dải câu hỏi tuyến tính theo dạng câu (SINGLE, TRUE_FALSE, SHORT_ANSWER, MULTI, ORDERING, MATCHING, ESSAY).
        /// Cấu hình động 100% từ parsing_rules_registry.json / DB (Zero-Hardcode).
        /// </summary>
        public Dictionary<string, List<string>> ScopeDirectiveMarkers { get; set; } = GetDefaultScopeDirectiveMarkers();

        /// <summary>Mẫu bóc tách dải số câu hỏi tổng quát [StartQ..EndQ] (đa ngôn ngữ VI, EN, ZH...).</summary>
        public List<string> ScopeRangePatterns { get; set; } = new()
        {
            @"(?i)(?:từ\s+(?:các\s+)?câu(?:\s+số)?|ở\s+mỗi\s+câu(?:\s+hỏi)?\s*(?:từ)?|(?:các\s+)?câu(?:\s+số|\s+hỏi)?)\s*(\d+)\s*(?:đến\s*(?:các\s+)?câu(?:\s+số)?|đến|[-–—])\s*(\d+)",
            @"(?i)(?:questions?|items?|from\s+question|for\s+questions?|in\s+questions?)\s*(\d+)\s*(?:to|through|[-–—])\s*(?:questions?\s*)?(\d+)",
            @"(?i)(?:questions?|items?)\s+from\s*(\d+)\s*to\s*(\d+)",
            @"(?i)(?:từ\s+câu|từ)\s*(\d+)\s*(?:đến\s*hết|trở\s*đi)"
        };

        public static Dictionary<string, List<string>> GetDefaultScopeDirectiveMarkers() => new(StringComparer.OrdinalIgnoreCase)
        {
            ["MULTI_TRUE_FALSE"] = new()
            {
                @"(?i)(?:mỗi\s+ý\s+[a-d]|các\s+mệnh\s+đề|mỗi\s+câu\s+có\s+4\s+ý).*?(?:chọn\s+đúng\s+(?:hoặc|hay)\s+sai|đúng\s*[-–\/\s]*sai|true\s*[-–\/\s]*false)",
                @"(?i)(?:chọn\s+đúng\s+(?:hoặc|hay)\s+sai|thí\s*sinh\s*chọn\s*đúng\s*hoặc\s*sai|mệnh\s+đề\s+đúng\s+hay\s+sai|indicate\s+whether\s+each\s+statement\s+is\s+true\s+or\s+false)"
            },
            ["SHORT_ANSWER"] = new()
            {
                @"(?i)(?:(?:phần\s+iii|part\s+iii|section\s+iii).*?trả\s*lời|trả\s*lời\s*ngắn|yêu\s*cầu\s*trả\s*lời\s*ngắn|điền\s*(?:đáp\s*số|vào\s*chỗ\s*trống)|viết\s*câu\s*trả\s*lời|kết\s*quả\s*làm\s*tròn|fill\s*in\s*the\s*blank|short\s*answer|write\s*your\s*answer)"
            },
            ["ORDERING"] = new()
            {
                @"(?i)(?:sắp\s*xếp\s*(?:các\s*(?:bước|câu|đoạn|ý))?|thứ\s*tự\s*(?:thực\s*hiện|đúng)|arrange\s+the\s+(?:utterances|sentences|steps)|chronological\s+order)"
            },
            ["MATCHING"] = new()
            {
                @"(?i)(?:ghép\s*(?:nội\s*dung|thông\s*tin)?\s*(?:ở\s*)?cột\s+[a-b1-2]|nối\s*(?:mệnh\s*đề|cột)|match\s*(?:each\s*item|the\s*items)?\s*in\s*column)"
            },
            ["MULTI"] = new()
            {
                @"(?i)(?:chọn\s+(?:tất\s*cả|các)\s+phương\s*án\s+đúng|có\s+thể\s+chọn\s+nhiều\s+đáp\s+án|choose\s+all\s+that\s+apply|select\s+all\s+correct)"
            },
            ["SINGLE"] = new()
            {
                @"(?i)(?:chỉ\s*chọn\s*(?:một|01|1)\s*phương\s*án|(?:mỗi\s+câu(?:\s+hỏi)?(?:\s+thí\s+sinh)?\s+)?chọn\s+(?:một|01|1)\s+phương\s*án|chọn\s*(?:một|01|1)\s*phương\s*án\s*đúng|chọn\s+phương\s*án\s*(?:đúng\s*nhất|chính\s*xác\s*nhất)|mark\s+the\s+letter\s+[a-d]|choose\s+the\s+(?:best|correct)\s+answer|select\s+one\s+answer|chọn\s+câu\s+trả\s+lời\s+đúng)"
            },
            ["ESSAY"] = new()
            {
                @"(?i)(?:tự\s*luận|thí\s*sinh\s*giải\s*(?:các\s*)?bài\s*toán|trình\s*bày\s*(?:chi\s*tiết\s*)?lời\s*giải|thang\s*điểm.*?điểm|solve\s+the\s+following|show\s+your\s+work)"
            }
        };

        public static List<TopicTaxonomyEntryDto> GetDefaultTopicTaxonomy() => new()
        {
            new() { TopicCode = "TIENG_ANH", TopicName = "Tiếng Anh", DomainCode = "EDUCATION", Keywords = new() { "tiếng anh", "tieng anh", "môn tiếng anh", "bài thi tiếng anh", "english", "ngoại ngữ 1", "ngoại ngữ", "foreign language", "reading comprehension", "pronunciation", "grammar" } },
            new() { TopicCode = "TIENG_PHAP", TopicName = "Tiếng Pháp", DomainCode = "EDUCATION", Keywords = new() { "tiếng pháp", "tieng phap", "môn tiếng pháp", "français", "french", "langue française" } },
            new() { TopicCode = "TIENG_TRUNG", TopicName = "Tiếng Trung Quốc", DomainCode = "EDUCATION", Keywords = new() { "tiếng trung", "tiếng hoa", "tieng trung", "môn tiếng trung", "chinese", "hsk", "hán ngữ", "chữ hán", "汉语", "中文" } },
            new() { TopicCode = "TIENG_NHAT", TopicName = "Tiếng Nhật Bản", DomainCode = "EDUCATION", Keywords = new() { "tiếng nhật", "tieng nhat", "môn tiếng nhật", "japanese", "jlpt", "nihongo", "日本語" } },
            new() { TopicCode = "TIENG_HAN", TopicName = "Tiếng Hàn Quốc", DomainCode = "EDUCATION", Keywords = new() { "tiếng hàn", "tieng han", "môn tiếng hàn", "korean", "topik", "한국어" } },
            new() { TopicCode = "TIENG_DUC", TopicName = "Tiếng Đức", DomainCode = "EDUCATION", Keywords = new() { "tiếng đức", "tieng duc", "môn tiếng đức", "german", "deutsch" } },
            new() { TopicCode = "TIENG_NGA", TopicName = "Tiếng Nga", DomainCode = "EDUCATION", Keywords = new() { "tiếng nga", "tieng nga", "môn tiếng nga", "russian", "русский" } },
            new() { TopicCode = "MATH", TopicName = "Toán học", DomainCode = "EDUCATION", Keywords = new() { "toán", "toán học", "toan", "toan hoc", "môn toán", "mon toan", "math", "mathematics", "đạo hàm", "tích phân", "hình học", "đại số", "giải tích" } },
            new() { TopicCode = "HOA_HOC", TopicName = "Hóa học", DomainCode = "EDUCATION", Keywords = new() { "hoá học", "hóa học", "hoá", "hóa", "hoa hoc", "hoa", "môn hoá học", "môn hóa học", "môn hoá", "môn hóa", "chemistry", "chimie", "phản ứng", "dung dịch", "mol", "axit", "acid", "bazo", "bazơ" } },
            new() { TopicCode = "SINH_HOC", TopicName = "Sinh học", DomainCode = "EDUCATION", Keywords = new() { "sinh học", "sinh", "môn sinh học", "môn sinh", "biology", "di truyền", "nhiễm sắc thể", "tế bào", "gen", "adn" } },
            new() { TopicCode = "VAT_LY", TopicName = "Vật lý", DomainCode = "EDUCATION", Keywords = new() { "vật lý", "vật lí", "vat ly", "vat li", "môn vật lý", "môn vật lí", "physics", "dao động", "sóng", "điện xoay chiều", "quang học", "cơ học" } },
            new() { TopicCode = "LICH_SU", TopicName = "Lịch sử", DomainCode = "EDUCATION", Keywords = new() { "lịch sử", "sử", "môn lịch sử", "môn sử", "history", "cách mạng", "chiến dịch", "kháng chiến", "hiệp định" } },
            new() { TopicCode = "DIA_LY", TopicName = "Địa lý", DomainCode = "EDUCATION", Keywords = new() { "địa lý", "địa lí", "dia ly", "môn địa lý", "môn địa lí", "geography", "khí hậu", "địa hình", "kinh tế vùng", "biển đảo" } },
            new() { TopicCode = "NGU_VAN", TopicName = "Ngữ văn", DomainCode = "EDUCATION", Keywords = new() { "ngữ văn", "văn học", "văn", "môn văn", "môn ngữ văn", "literature", "tác phẩm", "thơ", "truyện ngắn", "nghị luận" } },
            new() { TopicCode = "TIN_HOC", TopicName = "Tin học", DomainCode = "EDUCATION", Keywords = new() { "tin học", "tin hoc", "môn tin học", "mon tin hoc", "môn tin", "mon tin", "bài thi tin học", "informatics", "computer science", "tin", "công nghệ thông tin", "cntt", "lập trình", "thuật toán", "python", "máy tính", "mạng máy tính" } },
            new() { TopicCode = "CNTT", TopicName = "Tin học", DomainCode = "EDUCATION", Keywords = new() { "tin học", "công nghệ thông tin", "cntt", "môn tin", "informatics", "computer science", "lập trình", "thuật toán" } },
            new() { TopicCode = "GDKT_PL", TopicName = "Giáo dục Kinh tế & Pháp luật", DomainCode = "EDUCATION", Keywords = new() { "giáo dục kinh tế và pháp luật", "gdkt&pl", "gdkt", "kinh tế pháp luật", "pháp luật", "kinh tế" } }
        };

        public static List<DomainTaxonomyEntryDto> GetDefaultDomainTaxonomy() => new()
        {
            new() { DomainCode = "BANKING", DomainName = "Tài chính - Ngân hàng", Weight = 2, Keywords = new() { "tín dụng", "khdn", "khcn", "tiền gửi", "ngân hàng", "chi nhánh", "rửa tiền", "aml", "thế chấp", "tài sản bảo đảm", "e-banking", "ebanking", "agribank", "vietcombank", "bidv", "vietinbank", "cic", "kho tiền", "ngân quỹ", "thẩm định", "cho vay", "bảo lãnh", "lãi suất", "sổ tiết kiệm", "thẻ tín dụng", "quy định số 1686", "nhno", "hạn mức" } },
            new() { DomainCode = "EDUCATION", DomainName = "Giáo dục phổ thông & Đại học", Weight = 2, Keywords = new() { "toán", "đạo hàm", "tích phân", "hàm số", "ngữ văn", "tiếng anh", "vật lý", "hóa học", "gdpt", "thpt", "lớp 12", "lớp 11", "lớp 10", "k-12", "reading passage", "pronunciation", "chuyên amsterdam" } },
            new() { DomainCode = "GOV_DRIVING", DomainName = "Giao thông - Sát hạch lái xe", Weight = 3, Keywords = new() { "gplx", "sa hình", "biển báo", "lái xe", "vượt ẩu", "xe cơ giới", "tốc độ tối đa", "điểm liệt", "cục đường bộ" } },
            new() { DomainCode = "HEALTHCARE", DomainName = "Y tế & Chăm sóc sức khỏe", Weight = 3, Keywords = new() { "bác sĩ", "dược lâm sàng", "phác đồ", "điều trị", "bệnh nhân", "bệnh viện", "bộ y tế", "y khoa" } },
            new() { DomainCode = "HSE", DomainName = "An toàn - Sức khỏe - Môi trường", Weight = 3, Keywords = new() { "an toàn lao động", "bảo hộ lao động", "pccc", "phòng cháy chữa cháy", "vslđ", "iso 45001", "hóa chất", "an toàn điện" } },
            new() { DomainCode = "IT_SECURITY", DomainName = "Công nghệ thông tin & An ninh mạng", Weight = 3, Keywords = new() { "an toàn thông tin", "iso 27001", "owasp", "an ninh mạng", "phishing", "mật khẩu", "cybersecurity", "devsecops" } }
        };
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

        /// <summary>
        /// Ngưỡng độ tin cậy mô hình ngôn ngữ (NLP / FastText / MiniLM) khi thẩm định đề thi đa ngữ/song ngữ.
        /// </summary>
        public double NlpConfidenceThreshold { get; set; } = 0.85;
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

    }

    /// <summary>Thực thể từ điển phân loại môn học / chủ đề nạp động.</summary>
    public class TopicTaxonomyEntryDto
    {
        public string TopicCode { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public string DomainCode { get; set; } = "GENERAL";
        public List<string> Keywords { get; set; } = new();
    }

    /// <summary>Thực thể từ điển phân loại miền tri thức (Domain) nạp động.</summary>
    public class DomainTaxonomyEntryDto
    {
        public string DomainCode { get; set; } = string.Empty;
        public string DomainName { get; set; } = string.Empty;
        public int Weight { get; set; } = 2;
        public List<string> Keywords { get; set; } = new();
    }

    /// <summary>Kết quả nhận diện Hệ 5 Trục Tọa Độ Tri Thức + Smart Tags (Vỏ bọc Nhận thức Đề thi).</summary>
    public class CognitiveCoordinateTaxonomyResult
    {
        public string DomainCode { get; set; } = "GENERAL";
        public string? TopicCode { get; set; }
        public string? TopicName { get; set; }
        public string? TargetLevel { get; set; }
        public string? AssessmentPurpose { get; set; }
        public string? IssuingOrg { get; set; }
        public int? BenchmarkYear { get; set; }
        public string? BenchmarkStandard { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Phương án trắc nghiệm đã bóc tách (Parsed Option).
    /// </summary>
    public class ParsedOptionDto
    {
        public char Letter { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsMarkedCorrect { get; set; }
    }

    /// <summary>
    /// Hồ sơ cấu trúc phần đầu tài liệu (Header Profile) trích xuất từ các đoạn đầu đề thi.
    /// </summary>
    public class DocumentHeaderProfileDto
    {
        public int DurationSeconds { get; set; } = 60;
        public string? DetectedExamCode { get; set; }
        public string? DetectedSubject { get; set; }
        public CognitiveCoordinateTaxonomyResult? TaxonomyCoordinates { get; set; }
    }
}

