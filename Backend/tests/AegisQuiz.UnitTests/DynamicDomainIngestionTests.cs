using System;
using System.Collections.Generic;
using System.Linq;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Infrastructure.Services;
using AegisQuiz.Infrastructure.Services.Docx;
using Xunit;

namespace AegisQuiz.UnitTests
{
    public class DynamicDomainIngestionTests
    {
        [Fact]
        public void DynamicDomain_CustomTenantDomain_ShouldSniffAccurately()
        {
            // 1. Arrange: Cấu hình Tenant định nghĩa thêm ngành LOGISTICS & AVIATION hoàn toàn mới
            var tenantId = Guid.NewGuid();
            var context = new DynamicDomainContext
            {
                TenantId = tenantId,
                DefaultExamPrefix = "LOGISTICS_TEST",
                Domains = new List<DynamicDomainSummaryDto>
                {
                    new DynamicDomainSummaryDto
                    {
                        Code = "LOGISTICS_MARITIME",
                        Name = "Logistics và Vận tải Biển Quốc tế",
                        Description = "Khai thác cảng, xếp dỡ container, vận đơn đường biển, hải quan xuất nhập khẩu",
                        Synonyms = new List<string> { "cảng biển", "container", "hải quan", "bill of lading", "vận tải biển", "kho bãi CFS" }
                    },
                    new DynamicDomainSummaryDto
                    {
                        Code = "AVIATION_SAFETY",
                        Name = "An toàn Hàng không",
                        Description = "Kiểm soát không lưu, an ninh sân bay, bảo trì tàu bay",
                        Synonyms = new List<string> { "không lưu", "sân bay", "tàu bay", "phi công", "an toàn bay" }
                    }
                },
                CoordinatePresets = new List<TenantCoordinatePresetDto>
                {
                    new TenantCoordinatePresetDto
                    {
                        DomainCode = "LOGISTICS_MARITIME",
                        CoordinateType = "TARGET_LEVEL",
                        PresetCode = "PORT_OPERATOR_LV2",
                        PresetLabel = "Điều độ viên Cảng bậc 2",
                        Synonyms = new List<string> { "điều độ cảng", "khai thác bến bãi", "trực ban cảng" }
                    },
                    new TenantCoordinatePresetDto
                    {
                        DomainCode = "LOGISTICS_MARITIME",
                        CoordinateType = "ISSUING_ORG",
                        PresetCode = "VIETNAM_MARITIME_ADMIN",
                        PresetLabel = "Cục Hàng hải Việt Nam",
                        Synonyms = new List<string> { "cục hàng hải", "vinamarine", "cảng vụ hàng hải" }
                    },
                    new TenantCoordinatePresetDto
                    {
                        DomainCode = "LOGISTICS_MARITIME",
                        CoordinateType = "BENCHMARK_STANDARD",
                        PresetCode = "INCOTERMS_2020",
                        PresetLabel = "Tập quán Thương mại Quốc tế Incoterms 2020",
                        Synonyms = new List<string> { "incoterms 2020", "fob", "cif", "dap", "ddp" }
                    }
                }
            };

            // 2. Act: Thực hiện nhận diện tọa độ với văn bản nghiệp vụ Logistics
            var topicName = "Đề thi Sát hạch Điều độ Cảng biển Quốc tế 2026";
            var headerLines = new[]
            {
                "CỤC HÀNG HẢI VIỆT NAM - CẢNG VỤ HÀNG HẢI HẢI PHÒNG",
                "KỲ THI SÁT HẠCH NĂNG LỰC ĐIỀU ĐỘ VIÊN CẢNG BẬC 2",
                "Chuẩn mực: Incoterms 2020 và Công ước SOLAS về kiểm định tải trọng container"
            };
            var sampleContents = new List<string>
            {
                "Theo điều kiện CIF Incoterms 2020, nghĩa vụ mua bảo hiểm hàng hải thuộc về bên nào?",
                "Quy trình xác nhận khối lượng toàn bộ container (VGM) theo công ước SOLAS được thực hiện tại kho bãi CFS như thế nào?"
            };

            var taxonomy = UniversalCoordinateTaxonomyEngine.SniffCoordinates(
                topicName,
                "LOG_PORT_01",
                headerLines,
                sampleContents,
                null,
                context);

            // 3. Assert: Kiểm chứng hệ thống tự động gắn đúng Domain và các trục tọa độ từ Dynamic Domain Context
            Assert.NotNull(taxonomy);
            Assert.Equal("LOGISTICS_MARITIME", taxonomy.DomainCode);
            Assert.Equal("Điều độ viên Cảng bậc 2", taxonomy.TargetLevel);
            Assert.Equal("Cục Hàng hải Việt Nam", taxonomy.IssuingOrg);
            Assert.Equal("Tập quán Thương mại Quốc tế Incoterms 2020", taxonomy.BenchmarkStandard);
            Assert.Contains(taxonomy.Tags, t => t.Contains("port-operator-lv2", StringComparison.OrdinalIgnoreCase) || t.Contains("dieu-do", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(taxonomy.Tags, t => t.Contains("vietnam-maritime-admin", StringComparison.OrdinalIgnoreCase) || t.Contains("cuc-hang-hai", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(taxonomy.Tags, t => t.Contains("incoterms-2020", StringComparison.OrdinalIgnoreCase) || t.Contains("incoterms", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void DynamicDomain_FallbackGracefully_WhenContextIsNull()
        {
            // Kiểm tra tính tương thích ngược tuyệt đối (Backward Compatibility):
            // Khi context = null, hệ thống vẫn nhận diện chuẩn xác các ngành mặc định cũ
            var topicName = "Nghiệp vụ Tín dụng Ngân hàng Nông nghiệp";
            var headerLines = new[]
            {
                "NGÂN HÀNG NÔNG NGHIỆP VÀ PHÁT TRIỂN NÔNG THÔN VIỆT NAM (AGRIBANK)",
                "ĐỀ THI TUYỂN DỤNG CÁN BỘ TÍN DỤNG KHDN NĂM 2026",
                "Khung năng lực: Chuyên viên Khách hàng Doanh nghiệp"
            };

            var taxonomy = UniversalCoordinateTaxonomyEngine.SniffCoordinates(
                topicName,
                "AGRI_TD_01",
                headerLines,
                new List<string> { "Hạn mức tín dụng tuần hoàn cho doanh nghiệp được xác định theo công thức nào?" },
                null,
                null);

            Assert.NotNull(taxonomy);
            Assert.Equal("BANKING", taxonomy.DomainCode);
            Assert.Contains(taxonomy.Tags, t => t.Contains("agribank", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(taxonomy.Tags, t => t.Contains("ngan-hang", StringComparison.OrdinalIgnoreCase) || t.Contains("tin-dung", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void DynamicDomain_DocxStateMachine_UsesDynamicTopicAndPrefix()
        {
            // Kiểm tra bộ giải mã tài liệu DocxStateMachineClassifier nhận DynamicDomainContext
            var context = new DynamicDomainContext
            {
                DefaultExamPrefix = "HAQ_CUSTOM",
                Domains = new List<DynamicDomainSummaryDto>
                {
                    new DynamicDomainSummaryDto
                    {
                        Code = "PHARMACEUTICAL",
                        Name = "Dược học và Quản lý Dược",
                        Synonyms = new List<string> { "dược lâm sàng", "thuốc", "kê đơn", "gmp", "dược thư" }
                    }
                },
                Topics = new List<BankTopicSummaryDto>
                {
                    new BankTopicSummaryDto
                    {
                        Code = "DUOC_LAM_SANG_2026",
                        Name = "Dược Lâm Sàng Bệnh Viện 2026",
                        Description = "Kiểm soát tương tác thuốc và liều lượng kháng sinh"
                    }
                }
            };

            var parser = new DocxParserService(context);
            Assert.NotNull(parser);

            // Bóc tách đoạn văn bản thuộc chủ đề Dược lâm sàng
            var sampleParagraphs = new List<string>
            {
                "CHỦ ĐỀ: DƯỢC LÂM SÀNG BỆNH VIỆN 2026",
                "Phần 1. Câu hỏi trắc nghiệm nhiều phương án lựa chọn",
                "Câu 1. Kháng sinh nhóm Aminoglycosid có độc tính điển hình nào sau đây?",
                "A. Độc tính trên thận và thính giác.",
                "B. Độc tính trên gan cấp tính.",
                "C. Gây loét dạ dày tá tràng.",
                "D. Hội chứng người đỏ.",
                "Lời giải chi tiết: Nhóm Aminoglycosid có nguy cơ gây độc tính cao trên thận và tai (thính giác) không hồi phục."
            };

            var questions = DocxParserService.ParseParagraphs(sampleParagraphs, context);
            Assert.NotEmpty(questions);
            var q = questions.First();
            Assert.Contains("Aminoglycosid", q.Content);
            Assert.Equal("DUOC_LAM_SANG_2026", q.TopicCode);
            Assert.Equal("PHARMACEUTICAL", q.DomainCode);
        }
    }
}
