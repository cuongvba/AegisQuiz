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
                new() { DomainCode = "BANKING", CoordinateType = "TARGET_LEVEL", PresetCode = "CHUYEN_VIEN_CHINH", PresetLabel = "Chuyên viên chính", Synonyms = new() { "chuyên viên chính", "cvc", "chuyên viên" } },
                new() { DomainCode = "BANKING", CoordinateType = "BENCHMARK_STANDARD", PresetCode = "THONG_TU_41_BASEL", PresetLabel = "Thông tư 41 & Basel III", Synonyms = new() { "thông tư 41", "basel iii", "basel 3" } },
                new() { DomainCode = "BANKING", CoordinateType = "ASSESSMENT_PURPOSE", PresetCode = "AML_COMPLIANCE", PresetLabel = "Sát hạch Nghiệp vụ Tuân thủ AML Định kỳ", Synonyms = new() { "sát hạch định kỳ", "kiểm tra nghiệp vụ", "tuân thủ aml" } },

                new() { DomainCode = "EDUCATION", CoordinateType = "TARGET_LEVEL", PresetCode = "GRADE_12", PresetLabel = "Lớp 12", Synonyms = new() { "lớp 12", "lop 12", "k12", "thpt" } },
                new() { DomainCode = "EDUCATION", CoordinateType = "BENCHMARK_STANDARD", PresetCode = "GDPT_2018", PresetLabel = "Chương trình GDPT 2018", Synonyms = new() { "gdpt 2018", "gdpt", "chương trình mới" } },
                new() { DomainCode = "EDUCATION", CoordinateType = "ASSESSMENT_PURPOSE", PresetCode = "THPT_NATIONAL", PresetLabel = "Kỳ thi Tốt nghiệp THPT Quốc Gia", Synonyms = new() { "tốt nghiệp thpt", "thpt quốc gia", "thi đại học" } },

                new() { DomainCode = "GOV_DRIVING", CoordinateType = "ISSUING_ORG", PresetCode = "CUC_DUONG_BO", PresetLabel = "Cục Đường bộ Việt Nam", Synonyms = new() { "cục đường bộ", "bộ gtvt" } },
                new() { DomainCode = "GOV_DRIVING", CoordinateType = "BENCHMARK_STANDARD", PresetCode = "ND_100_2019", PresetLabel = "Nghị định 100/2019/NĐ-CP", Synonyms = new() { "nghị định 100", "luật giao thông đường bộ" } },
                new() { DomainCode = "GOV_DRIVING", CoordinateType = "ASSESSMENT_PURPOSE", PresetCode = "SAT_HACH_GPLX", PresetLabel = "Sát hạch Giấy phép Lái xe (GPLX)", Synonyms = new() { "sát hạch gplx", "thi bằng lái" } },

                new() { DomainCode = "HEALTHCARE", CoordinateType = "TARGET_LEVEL", PresetCode = "BAC_SI_CKI", PresetLabel = "Bác sĩ CKI", Synonyms = new() { "bác sĩ cki", "bác sĩ chuyên khoa 1", "bác sĩ" } },
                new() { DomainCode = "HEALTHCARE", CoordinateType = "BENCHMARK_STANDARD", PresetCode = "PHAC_DO_BYT_2025", PresetLabel = "Phác đồ Bộ Y tế 2025", Synonyms = new() { "phác đồ bộ y tế", "phác đồ điều trị" } },
                new() { DomainCode = "HEALTHCARE", CoordinateType = "ISSUING_ORG", PresetCode = "BO_Y_TE", PresetLabel = "Bộ Y tế", Synonyms = new() { "bộ y tế", "so y te" } },

                new() { DomainCode = "HSE", CoordinateType = "TARGET_LEVEL", PresetCode = "NHOM_2_3", PresetLabel = "Nhóm 2 / Nhóm 3", Synonyms = new() { "nhóm 2", "nhóm 3", "công nhân", "kỹ sư an toàn" } },
                new() { DomainCode = "HSE", CoordinateType = "BENCHMARK_STANDARD", PresetCode = "ISO_45001_2018", PresetLabel = "ISO 45001:2018", Synonyms = new() { "iso 45001", "qcvn" } },
                new() { DomainCode = "HSE", CoordinateType = "ASSESSMENT_PURPOSE", PresetCode = "HUAN_LUYEN_VSLD", PresetLabel = "Huấn luyện An toàn VSLĐ định kỳ", Synonyms = new() { "huấn luyện an toàn", "vsld" } }
            };

            return ctx;
        }
    }
}
