using System;
using System.Collections.Generic;

namespace AegisQuiz.Application.DTOs
{
    /// <summary>
    /// Bản tóm tắt Danh mục Ngành Động nạp vào Ingestion Pipeline
    /// </summary>
    public class DynamicDomainSummaryDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? ColorBadge { get; set; }
        public bool IsSystemStandard { get; set; } = true;
        public string? ParentDomainCode { get; set; }
        public List<string> Synonyms { get; set; } = new();
    }

    /// <summary>
    /// Bản tóm tắt Preset Tọa độ Tri thức của Tenant
    /// </summary>
    public class TenantCoordinatePresetDto
    {
        public Guid Id { get; set; }
        public string DomainCode { get; set; } = string.Empty;
        /// <summary>
        /// TARGET_LEVEL, ISSUING_ORG, BENCHMARK_STANDARD, ASSESSMENT_PURPOSE, DOMAIN_KEYWORD
        /// </summary>
        public string CoordinateType { get; set; } = string.Empty;
        public string PresetCode { get; set; } = string.Empty;
        public string PresetLabel { get; set; } = string.Empty;
        public List<string> Synonyms { get; set; } = new();
        public bool IsDefault { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
    }

    /// <summary>
    /// Bản tóm tắt Chủ đề Ngân hàng câu hỏi
    /// </summary>
    public class BankTopicSummaryDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? DomainCode { get; set; }
        public List<string> Keywords { get; set; } = new();
    }

    /// <summary>
    /// [Dynamic Domain Context]
    /// Ngữ cảnh Đa ngành và Cấu trúc Tài liệu Động truyền vào bộ bóc tách Ingestion.
    /// Giúp loại bỏ 100% hardcode chuỗi và cho phép hệ thống tự động thích ứng với cấu hình riêng của từng Tenant.
    /// </summary>
    public class DynamicDomainContext
    {
        public Guid TenantId { get; set; } = Guid.Empty;
        public string DefaultExamPrefix { get; set; } = "EXAM";
        public string Language { get; set; } = "vi";
        public List<DynamicDomainSummaryDto> Domains { get; set; } = new();
        public List<TenantCoordinatePresetDto> CoordinatePresets { get; set; } = new();
        public List<BankTopicSummaryDto> Topics { get; set; } = new();
        public DocumentStructureDictionaryDto StructureDictionary { get; set; } = new();

        /// <summary>
        /// Tạo ngữ cảnh mặc định từ chuẩn hệ thống
        /// </summary>
        public static DynamicDomainContext CreateDefault()
        {
            var ctx = new DynamicDomainContext();

            ctx.Domains = new List<DynamicDomainSummaryDto>
            {
                new() { Code = "BANKING", Name = "Tài chính - Ngân hàng", Description = "Tín dụng, ngân quỹ, thanh toán quốc tế, AML, thẩm định tài sản", Synonyms = new() { "tín dụng", "ngân hàng", "chi nhánh", "kho tiền", "ngân quỹ", "aml", "vcb", "bidv", "agribank", "vietinbank", "cic" } },
                new() { Code = "EDUCATION", Name = "Giáo dục & Học thuật", Description = "Toán học, vật lý, hóa học, sinh học, ngữ văn, tiếng anh, GDPT 2018, THPT", Synonyms = new() { "toán", "đạo hàm", "tích phân", "hàm số", "vật lý", "hóa học", "tiếng anh", "gdpt", "thpt", "lớp 12" } },
                new() { Code = "GOV_DRIVING", Name = "Sát hạch Lái xe Quốc gia", Description = "GPLX B2, C, sa hình, biển báo, điểm liệt, luật giao thông đường bộ", Synonyms = new() { "gplx", "sa hình", "biển báo", "lái xe", "vượt ẩu", "điểm liệt", "cục đường bộ" } },
                new() { Code = "HEALTHCARE", Name = "Y tế & Dược phẩm", Description = "Y khoa, bác sĩ, dược lâm sàng, phác đồ điều trị, điều dưỡng, bệnh viện", Synonyms = new() { "bác sĩ", "dược lâm sàng", "phác đồ", "bệnh viện", "bộ y tế", "y khoa", "điều trị" } },
                new() { Code = "HSE", Name = "An toàn - Môi trường LĐ", Description = "An toàn vệ sinh lao động, bảo hộ lao động, PCCC, ISO 45001", Synonyms = new() { "an toàn lao động", "bảo hộ lao động", "pccc", "phòng cháy", "iso 45001", "vslđ" } },
                new() { Code = "IT_SECURITY", Name = "CNTT & An Toàn Thông Tin", Description = "An ninh mạng, ISO 27001, SOC, Pentest, DevSecOps, phần mềm", Synonyms = new() { "an toàn thông tin", "an ninh mạng", "cntt", "công nghệ thông tin", "iso 27001", "bảo mật" } },
                new() { Code = "GENERAL", Name = "Tổng hợp / Đại cương", Description = "Nâng ngạch, tuyển dụng, kỹ năng mềm, văn hóa doanh nghiệp", Synonyms = new() { "tổng hợp", "đại cương", "tuyển dụng", "kỹ năng mềm" } }
            };

            ctx.CoordinatePresets = new List<TenantCoordinatePresetDto>
            {
                // BANKING
                new() { DomainCode = "BANKING", CoordinateType = "TARGET_LEVEL", PresetCode = "CHUYEN_VIEN_CHINH", PresetLabel = "Chuyên viên chính", Synonyms = new() { "chuyên viên chính", "cvc", "chuyên viên" } },
                new() { DomainCode = "BANKING", CoordinateType = "TARGET_LEVEL", PresetCode = "TIN_DUNG_KHDN", PresetLabel = "Tín dụng Khách hàng Doanh nghiệp", Synonyms = new() { "tín dụng khdn", "khdn", "khách hàng doanh nghiệp", "chuyên viên khách hàng doanh nghiệp" } },
                new() { DomainCode = "BANKING", CoordinateType = "TARGET_LEVEL", PresetCode = "TIN_DUNG_KHCN", PresetLabel = "Tín dụng Khách hàng Cá nhân", Synonyms = new() { "tín dụng khcn", "khcn", "khách hàng cá nhân", "chuyên viên khách hàng cá nhân" } },
                new() { DomainCode = "BANKING", CoordinateType = "TARGET_LEVEL", PresetCode = "KE_TOAN_NGAN_QUY", PresetLabel = "Kế toán & Ngân quỹ", Synonyms = new() { "kế toán ngân quỹ", "ngân quỹ", "kế toán viên" } },
                new() { DomainCode = "BANKING", CoordinateType = "TARGET_LEVEL", PresetCode = "KIEM_SOAT_VIEN", PresetLabel = "Kiểm soát viên", Synonyms = new() { "kiểm soát viên", "ksv" } },
                new() { DomainCode = "BANKING", CoordinateType = "TARGET_LEVEL", PresetCode = "GIAO_DICH_VIEN", PresetLabel = "Giao dịch viên", Synonyms = new() { "giao dịch viên", "gdv" } },
                new() { DomainCode = "BANKING", CoordinateType = "BENCHMARK_STANDARD", PresetCode = "THONG_TU_41_BASEL", PresetLabel = "Thông tư 41 & Basel III", Synonyms = new() { "thông tư 41", "basel iii", "basel 3" } },
                new() { DomainCode = "BANKING", CoordinateType = "BENCHMARK_STANDARD", PresetCode = "CHIEU_CHUAN_NGAN_HANG", PresetLabel = "Chuẩn mực Nghiệp vụ Ngân hàng", Synonyms = new() { "chuẩn mực nghiệp vụ ngân hàng", "nghiệp vụ ngân hàng", "quy chế kiểm tra nghiệp vụ" } },
                new() { DomainCode = "BANKING", CoordinateType = "ASSESSMENT_PURPOSE", PresetCode = "AML_COMPLIANCE", PresetLabel = "Sát hạch Nghiệp vụ Phòng chống Rửa tiền (AML)", Synonyms = new() { "sát hạch định kỳ", "kiểm tra nghiệp vụ", "tuân thủ aml", "phòng chống rửa tiền", "aml" } },
                new() { DomainCode = "BANKING", CoordinateType = "ASSESSMENT_PURPOSE", PresetCode = "KIEM_TRA_DINH_KY", PresetLabel = "Kiểm tra chuyên môn nghiệp vụ định kỳ", Synonyms = new() { "kiểm tra nghiệp vụ định kỳ", "chuyên môn nghiệp vụ định kỳ", "kiểm tra chuyên môn", "kiểm tra định kỳ" } },
                new() { DomainCode = "BANKING", CoordinateType = "ISSUING_ORG", PresetCode = "AGRIBANK", PresetLabel = "Agribank", Synonyms = new() { "agribank", "nhno", "ngân hàng nông nghiệp", "nông nghiệp và phát triển nông thôn" } },
                new() { DomainCode = "BANKING", CoordinateType = "ISSUING_ORG", PresetCode = "VIETCOMBANK", PresetLabel = "Vietcombank", Synonyms = new() { "vietcombank", "vcb", "ngoại thương" } },
                new() { DomainCode = "BANKING", CoordinateType = "ISSUING_ORG", PresetCode = "BIDV", PresetLabel = "BIDV", Synonyms = new() { "bidv", "đầu tư và phát triển" } },
                new() { DomainCode = "BANKING", CoordinateType = "ISSUING_ORG", PresetCode = "VIETINBANK", PresetLabel = "VietinBank", Synonyms = new() { "vietinbank", "ctg", "công thương" } },

                // EDUCATION
                new() { DomainCode = "EDUCATION", CoordinateType = "TARGET_LEVEL", PresetCode = "GRADE_12", PresetLabel = "Lớp 12", Synonyms = new() { "lớp 12", "lop 12", "k12", "khối 12", "thpt" } },
                new() { DomainCode = "EDUCATION", CoordinateType = "TARGET_LEVEL", PresetCode = "GRADE_11", PresetLabel = "Lớp 11", Synonyms = new() { "lớp 11", "lop 11", "k11", "khối 11" } },
                new() { DomainCode = "EDUCATION", CoordinateType = "TARGET_LEVEL", PresetCode = "GRADE_10", PresetLabel = "Lớp 10", Synonyms = new() { "lớp 10", "lop 10", "k10", "khối 10", "tuyển sinh 10", "tuyển sinh vào lớp 10" } },
                new() { DomainCode = "EDUCATION", CoordinateType = "BENCHMARK_STANDARD", PresetCode = "GDPT_2018", PresetLabel = "Chương trình GDPT 2018", Synonyms = new() { "gdpt 2018", "gdpt", "chương trình mới", "sgk mới" } },
                new() { DomainCode = "EDUCATION", CoordinateType = "ASSESSMENT_PURPOSE", PresetCode = "THPT_NATIONAL", PresetLabel = "Kỳ thi Tốt nghiệp THPT Quốc Gia", Synonyms = new() { "tốt nghiệp thpt", "thpt quốc gia", "thi đại học" } },
                new() { DomainCode = "EDUCATION", CoordinateType = "ISSUING_ORG", PresetCode = "BO_GIAO_DUC", PresetLabel = "Bộ Giáo dục và Đào tạo", Synonyms = new() { "bộ giáo dục", "bộ giáo dục và đào tạo", "bo giao duc", "bộ gd&đt", "bgd&dt" } },
                new() { DomainCode = "EDUCATION", CoordinateType = "ISSUING_ORG", PresetCode = "AMSTERDAM", PresetLabel = "THPT Chuyên Hà Nội - Amsterdam", Synonyms = new() { "chuyên amsterdam", "amsterdam" } },

                // GOV_DRIVING
                new() { DomainCode = "GOV_DRIVING", CoordinateType = "TARGET_LEVEL", PresetCode = "GPLX_B2", PresetLabel = "GPLX Hạng B2", Synonyms = new() { "hạng b2", "bằng b2", "b2" } },
                new() { DomainCode = "GOV_DRIVING", CoordinateType = "TARGET_LEVEL", PresetCode = "GPLX_B1", PresetLabel = "GPLX Hạng B1", Synonyms = new() { "hạng b1", "bằng b1", "b1" } },
                new() { DomainCode = "GOV_DRIVING", CoordinateType = "TARGET_LEVEL", PresetCode = "GPLX_C", PresetLabel = "GPLX Hạng C", Synonyms = new() { "hạng c", "bằng c" } },
                new() { DomainCode = "GOV_DRIVING", CoordinateType = "ISSUING_ORG", PresetCode = "CUC_DUONG_BO", PresetLabel = "Cục Đường bộ Việt Nam", Synonyms = new() { "cục đường bộ", "bộ gtvt" } },
                new() { DomainCode = "GOV_DRIVING", CoordinateType = "BENCHMARK_STANDARD", PresetCode = "ND_100_2019", PresetLabel = "Nghị định 100/2019/NĐ-CP & QC 41:2019", Synonyms = new() { "nghị định 100", "qc 41", "luật giao thông đường bộ" } },
                new() { DomainCode = "GOV_DRIVING", CoordinateType = "ASSESSMENT_PURPOSE", PresetCode = "SAT_HACH_GPLX", PresetLabel = "Sát hạch Giấy phép Lái xe (GPLX)", Synonyms = new() { "sát hạch gplx", "thi bằng lái", "sát hạch lái xe", "gplx" } },

                // HEALTHCARE
                new() { DomainCode = "HEALTHCARE", CoordinateType = "TARGET_LEVEL", PresetCode = "BAC_SI_CKI", PresetLabel = "Bác sĩ CKI", Synonyms = new() { "bác sĩ cki", "bác sĩ chuyên khoa 1", "bác sĩ" } },
                new() { DomainCode = "HEALTHCARE", CoordinateType = "BENCHMARK_STANDARD", PresetCode = "PHAC_DO_BYT_2025", PresetLabel = "Phác đồ Điều trị Bộ Y tế 2025", Synonyms = new() { "phác đồ bộ y tế", "phác đồ điều trị", "bộ y tế 2025" } },
                new() { DomainCode = "HEALTHCARE", CoordinateType = "ISSUING_ORG", PresetCode = "BO_Y_TE", PresetLabel = "Bộ Y tế", Synonyms = new() { "bộ y tế", "so y te", "sở y tế" } },

                // HSE
                new() { DomainCode = "HSE", CoordinateType = "TARGET_LEVEL", PresetCode = "NHOM_2_3", PresetLabel = "Nhóm 2 / Nhóm 3", Synonyms = new() { "nhóm 2", "nhóm 3", "công nhân", "kỹ sư an toàn" } },
                new() { DomainCode = "HSE", CoordinateType = "BENCHMARK_STANDARD", PresetCode = "ISO_45001_2018", PresetLabel = "Nghị định 44/2016/NĐ-CP & ISO 45001", Synonyms = new() { "iso 45001", "nghị định 44", "qcvn" } },
                new() { DomainCode = "HSE", CoordinateType = "ASSESSMENT_PURPOSE", PresetCode = "HUAN_LUYEN_VSLD", PresetLabel = "Huấn luyện An toàn VSLĐ định kỳ", Synonyms = new() { "huấn luyện an toàn", "vsld", "an toàn lao động", "atvsld", "vệ sinh lao động" } },

                // IT_SECURITY
                new() { DomainCode = "IT_SECURITY", CoordinateType = "BENCHMARK_STANDARD", PresetCode = "ISO_27001_2022", PresetLabel = "ISO/IEC 27001:2022", Synonyms = new() { "iso 27001", "iso/iec 27001", "an toàn thông tin", "an ninh mạng" } }
            };

            // [Zero-Hardcode Taxonomy] Khởi tạo danh mục Chủ đề & Môn học mặc định
            ctx.Topics = new List<BankTopicSummaryDto>
            {
                new() { Code = "MATH", Name = "Toán học", DomainCode = "EDUCATION", Keywords = new() { "toán", "toan", "toán học", "toan hoc", "môn toán", "mon toan", "giải tích", "hình học", "hàm số", "đồ thị", "tích phân", "đạo hàm", "nguyên hàm", "xác suất", "tiệm cận", "vectơ", "vector", "phương trình", "bất phương trình", "tọa độ", "oxyz", "bảng biến thiên", "cực trị", "tối giản", "đường tròn", "tiếp tuyến", "vuông góc", "song song", "tam giác", "tứ giác", "nội tiếp", "ngoại tiếp", "hình trụ", "hình nón", "hình cầu", "bán kính", "hình lập phương", "hình chóp", "lăng trụ", "rút gọn", "biểu thức", "chứng minh", "vận tốc", "quãng đường", "phối trộn", "chiều cao", "mực nước", @"\frac", @"\sqrt", @"\int", @"\lim", @"\log", @"\ln", @"\vec", @"\delta", "f(x)", "y =", "x =", "dx", "dy" } },
                new() { Code = "VAT_LY", Name = "Vật lý", DomainCode = "EDUCATION", Keywords = new() { "vật lí", "vật lý", "vat li", "vat ly", "môn vật lí", "môn vật lý", "physics", "nhiệt lượng", "nhiệt dung", "chuyển thể", "khí lí tưởng", "đẳng nhiệt", "đẳng tích", "đẳng áp", "phóng xạ", "hạt nhân", "tia alpha", "tia beta", "tia gamma", "quang điện", "lực từ", "cảm ứng từ", "suất điện động", "từ trường", "bước sóng", "tần số", "dao động", "dòng điện" } },
                new() { Code = "HOA_HOC", Name = "Hóa học", DomainCode = "EDUCATION", Keywords = new() { "hoá học", "hóa học", "hoá", "hóa", "hoa hoc", "hoa", "môn hoá học", "môn hóa học", "môn hoá", "môn hóa", "chemistry", "chimie", "axit", "acid", "bazo", "bazơ", "muối", "ancol", "alcohol", "este", "ester", "amino acid", "polime", "polymer", "thế điện cực", "galvani", "chỉ số octane", "số sóng", "phổ hồng ngoại", "chất giặt rửa", "chất hữu cơ", "chất vô cơ", "glycine", "glucose", "fructose", "dung dịch", "kết tủa", "kim loại", "đồng đẳng", "đồng phân", "mol", "hcl", "h2so4", "naoh", "fe", "cu", "al", "chất khử", "chất oxi hóa" } },
                new() { Code = "TIENG_ANH", Name = "Tiếng Anh", DomainCode = "EDUCATION", Keywords = new() { "tiếng anh", "tieng anh", "môn tiếng anh", "bài thi tiếng anh", "english", "ngoại ngữ 1", "ngoại ngữ", "ngoaingu", "toeic", "ielts", "mark the letter", "pronunciation", "primary stress", "closest in meaning", "opposite in meaning", "underlined part", "synonym", "antonym", "read the following passage", "in paragraph", "according to paragraph", "which of the following", "according to the passage" } },
                new() { Code = "TIENG_PHAP", Name = "Tiếng Pháp", DomainCode = "EDUCATION", Keywords = new() { "tiếng pháp", "tieng phap", "môn tiếng pháp", "français", "french", "langue française", "exercice", "conjuguez", "grammaire", "vocabulaire" } },
                new() { Code = "TIENG_TRUNG", Name = "Tiếng Trung Quốc", DomainCode = "EDUCATION", Keywords = new() { "tiếng trung", "tiếng hoa", "tieng trung", "môn tiếng trung", "chinese", "hsk", "hán ngữ", "chữ hán", "pinyin", "汉语", "中文" } },
                new() { Code = "TIENG_NHAT", Name = "Tiếng Nhật Bản", DomainCode = "EDUCATION", Keywords = new() { "tiếng nhật", "tieng nhat", "môn tiếng nhật", "japanese", "jlpt", "nihongo", "kanji", "hiragana", "katakana", "日本語" } },
                new() { Code = "TIENG_HAN", Name = "Tiếng Hàn Quốc", DomainCode = "EDUCATION", Keywords = new() { "tiếng hàn", "tieng han", "môn tiếng hàn", "korean", "topik", "hangul", "한국어" } },
                new() { Code = "TIENG_DUC", Name = "Tiếng Đức", DomainCode = "EDUCATION", Keywords = new() { "tiếng đức", "tieng duc", "môn tiếng đức", "german", "deutsch", "goethe" } },
                new() { Code = "TIENG_NGA", Name = "Tiếng Nga", DomainCode = "EDUCATION", Keywords = new() { "tiếng nga", "tieng nga", "môn tiếng nga", "russian", "русский" } },
                new() { Code = "SINH_HOC", Name = "Sinh học", DomainCode = "EDUCATION", Keywords = new() { "sinh học", "sinh", "môn sinh học", "môn sinh", "biology", "gen", "adn", "arn", "nhiễm sắc thể", "đột biến", "quần thể", "hệ sinh thái", "tiêu hóa", "tuần hoàn", "alen", "thế hệ f1", "thế hệ f2", "di truyền" } },
                new() { Code = "NGU_VAN", Name = "Ngữ văn", DomainCode = "EDUCATION", Keywords = new() { "ngữ văn", "văn học", "văn", "môn văn", "tác phẩm", "nhà thơ", "nhà văn", "đoạn trích", "nghị luận xã hội", "nghị luận văn học" } },
                new() { Code = "LICH_SU", Name = "Lịch sử", DomainCode = "EDUCATION", Keywords = new() { "lịch sử", "sử", "môn sử", "history", "chiến dịch", "khởi nghĩa", "hiệp định", "cách mạng", "chiến tranh" } },
                new() { Code = "DIA_LY", Name = "Địa lý", DomainCode = "EDUCATION", Keywords = new() { "địa lý", "địa lí", "địa", "dia", "môn địa", "geography", "khí hậu", "địa hình", "dân số", "đồng bằng", "sông ngòi", "vùng kinh tế" } },
                new() { Code = "GDKT_PL", Name = "Giáo dục Kinh tế & Pháp luật", DomainCode = "EDUCATION", Keywords = new() { "kinh tế", "pháp luật", "gdkt", "gdkt&pl", "thị trường", "lạm phát", "ngân sách", "quyền công dân" } },
                new() { Code = "382", Name = "Nghiệp vụ Tín dụng", DomainCode = "BANKING", Keywords = new() { "tín dụng", "cho vay", "thế chấp", "nợ xấu", "tài sản bảo đảm", "giải ngân", "hạn mức", "cic" } },
                new() { Code = "381", Name = "Nghiệp vụ Ngân quỹ - Kế toán", DomainCode = "BANKING", Keywords = new() { "ngân quỹ", "kế toán", "kho tiền", "chìa khóa", "niêm phong", "kiểm quỹ", "chứng từ" } },
                new() { Code = "TIN_HOC", Name = "Tin học", DomainCode = "EDUCATION", Keywords = new() { "tin học", "tin hoc", "môn tin học", "mon tin hoc", "môn tin", "mon tin", "bài thi tin học", "informatics", "computer science", "tin", "lập trình", "thuật toán", "python", "pascal", "c++", "cơ sở dữ liệu", "sql", "mạng máy tính", "máy tính", "an toàn thông tin", "excel", "access", "word", "soạn thảo văn bản", "bảng tính", "hệ điều hành", "phần cứng", "phần mềm", "internet", "trí tuệ nhân tạo", "ai", "cntt", "công nghệ thông tin" } },
                new() { Code = "CNTT", Name = "Công nghệ Thông tin", DomainCode = "EDUCATION", Keywords = new() { "công nghệ thông tin", "cntt", "tin học", "môn tin", "giao thức", "mật khẩu", "mã độc", "tấn công mạng", "phần mềm" } },
                new() { Code = "PARTY_BUILDING", Name = "Công tác Xây dựng Đảng", DomainCode = "GENERAL", Keywords = new() { "đảng", "chi bộ", "đảng viên", "nghị quyết", "ban chấp hành", "điều lệ đảng" } },
                new() { Code = "LAW", Name = "Pháp chế & Tuân thủ", DomainCode = "GENERAL", Keywords = new() { "pháp chế", "luật", "nghị định", "thông tư", "xử phạt", "quy chuẩn" } }
            };

            return ctx;
        }
    }
}
