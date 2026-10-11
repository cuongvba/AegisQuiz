using System;
using System.Collections.Generic;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Infrastructure.AI.Selection
{
    /// <summary>
    /// [AegisAI Council Seeder] Cung cấp danh mục các Viện Hàn Lâm và Hội Đồng Hệ Thống chuẩn mực.
    /// Tách rời dữ liệu mẫu khỏi Engine để bảo đảm OCP và nguyên lý Single Responsibility.
    /// Quản lý đồng dạng 4 Nhóm Councils: Academic, Secretariat, Arbitration, SystemCore.
    /// </summary>
    public class PreconfiguredCouncilProvider : IPreconfiguredCouncilProvider
    {
        public List<UniversalAiCouncil> GetPreconfiguredCouncils()
        {
            return new List<UniversalAiCouncil>
            {
                // ==========================================
                // NHÓM 1: ACADEMIC COUNCILS (VIỆN HÀN LÂM HỌC THUẬT)
                // ==========================================
                new()
                {
                    CouncilCode = "PHILOSOPHY",
                    DisplayName = "Viện Triết Học Bản Thể & Nhận Thức Luận",
                    Category = CouncilCategory.Academic,
                    DisciplineArea = "Siêu hình học, Đạo đức học & Bản thể luận",
                    MaterializedTaxonomyPath = "/HUMANITIES/PHILOSOPHY/METAPHYSICS",
                    EpistemicManifesto = "Mọi chân lý đều bắt đầu từ việc hoài nghi và truy vấn đến tận cùng của bản thể tồn tại.",
                    BenchmarkPrestigeElo = 2950,
                    Seats = new()
                    {
                        new() { SeatTitle = "Chủ Tịch Hội Đồng Nhận Thức", Role = CouncilSeatRole.President, AlphaModelCode = "claude-3-5-sonnet", PreferredModelCode = "claude-3-5-sonnet", SwarmModelCodesJson = @"[""claude-3-5-sonnet"", ""deepseek-r1"", ""gemini-2.0-flash""]", IdealArchetype = CognitiveArchetype.LinguisticArbiter },
                        new() { SeatTitle = "Biện Chứng Gia Đối Kháng (Devil's Advocate)", Role = CouncilSeatRole.AdversarialCritic, AlphaModelCode = "deepseek-r1", PreferredModelCode = "deepseek-r1", SwarmModelCodesJson = @"[""deepseek-r1"", ""claude-3-5-sonnet"", ""qwen/qwen3.8-27b""]", IdealArchetype = CognitiveArchetype.DeepReasoner }
                    }
                },
                new()
                {
                    CouncilCode = "COSMOLOGY",
                    DisplayName = "Viện Vũ Trụ Học & Vật Lý Nền Tảng",
                    Category = CouncilCategory.Academic,
                    DisciplineArea = "Vật lý lượng tử, Thuyết tương đối & Vũ trụ học",
                    MaterializedTaxonomyPath = "/NATURE/PHYSICS/COSMOLOGY",
                    EpistemicManifesto = "Vũ trụ được viết bằng ngôn ngữ toán học và các định luật bảo toàn bất biến.",
                    BenchmarkPrestigeElo = 2980,
                    Seats = new()
                    {
                        new() { SeatTitle = "Trưởng Ban Vật Lý Lý Thuyết", Role = CouncilSeatRole.President, AlphaModelCode = "deepseek-r1", PreferredModelCode = "deepseek-r1", SwarmModelCodesJson = @"[""deepseek-r1"", ""claude-3-5-sonnet"", ""gemini-2.0-flash""]", IdealArchetype = CognitiveArchetype.DeepReasoner },
                        new() { SeatTitle = "Chuyên Viên Phản Biện Thực Nghiệm", Role = CouncilSeatRole.EmpiricalAuditor, AlphaModelCode = "gpt-4o", PreferredModelCode = "gpt-4o", SwarmModelCodesJson = @"[""gpt-4o"", ""claude-3-5-sonnet"", ""gemini-2.0-flash""]", IdealArchetype = CognitiveArchetype.LinguisticArbiter }
                    }
                },
                new()
                {
                    CouncilCode = "MATHEMATICS",
                    DisplayName = "Viện Toán Học Thuần Túy & Logic Hình Thức",
                    Category = CouncilCategory.Academic,
                    DisciplineArea = "Hình học đại số, Lý thuyết số & Topo học",
                    MaterializedTaxonomyPath = "/FORMAL/MATHEMATICS/LOGIC",
                    EpistemicManifesto = "Chân lý toán học là dạng thức duy nhất của sự chính xác tuyệt đối không bị hoen ố bởi trực giác cảm tính.",
                    BenchmarkPrestigeElo = 3000,
                    Seats = new()
                    {
                        new() { SeatTitle = "Chủ Nhiệm Khoa Logic Toán", Role = CouncilSeatRole.President, AlphaModelCode = "deepseek-r1", PreferredModelCode = "deepseek-r1", SwarmModelCodesJson = @"[""deepseek-r1"", ""claude-3-5-sonnet"", ""qwen/qwen3.8-27b""]", IdealArchetype = CognitiveArchetype.DeepReasoner },
                        new() { SeatTitle = "Kiểm Định Viên Chứng Minh Hình Thức", Role = CouncilSeatRole.EmpiricalAuditor, AlphaModelCode = "deepseek-r1", PreferredModelCode = "deepseek-r1", SwarmModelCodesJson = @"[""deepseek-r1"", ""gpt-4o"", ""claude-3-5-sonnet""]", IdealArchetype = CognitiveArchetype.DeepReasoner }
                    }
                },
                new()
                {
                    CouncilCode = "AGI_CYBERNETICS",
                    DisplayName = "Viện Trí Tuệ Nhân Tạo & Điều Khiển Học",
                    Category = CouncilCategory.Academic,
                    DisciplineArea = "Siêu trí tuệ, Mạng thần kinh & Lý thuyết thông tin",
                    MaterializedTaxonomyPath = "/TECH/AI/CYBERNETICS",
                    EpistemicManifesto = "Trí tuệ là khả năng tối ưu hóa mục tiêu phức hợp trong không gian vô hạn chiều.",
                    BenchmarkPrestigeElo = 2920,
                    Seats = new()
                    {
                        new() { SeatTitle = "Trưởng Ban An Toàn & Căn Chỉnh AGI", Role = CouncilSeatRole.President, AlphaModelCode = "claude-3-5-sonnet", PreferredModelCode = "claude-3-5-sonnet", SwarmModelCodesJson = @"[""claude-3-5-sonnet"", ""deepseek-r1"", ""gemini-2.0-flash""]", IdealArchetype = CognitiveArchetype.LinguisticArbiter },
                        new() { SeatTitle = "Chuyên Gia Mô Phỏng Rủi Ro Hiện Sinh", Role = CouncilSeatRole.AdversarialCritic, AlphaModelCode = "deepseek-r1", PreferredModelCode = "deepseek-r1", SwarmModelCodesJson = @"[""deepseek-r1"", ""claude-3-5-sonnet"", ""qwen/qwen3.8-27b""]", IdealArchetype = CognitiveArchetype.DeepReasoner }
                    }
                },
                new()
                {
                    CouncilCode = "ECONOMICS",
                    DisplayName = "Viện Kinh Tế Chính Trị & Địa Chiến Lược",
                    Category = CouncilCategory.Academic,
                    DisciplineArea = "Kinh tế vĩ mô, Lý thuyết trò chơi & Trật tự thế giới",
                    MaterializedTaxonomyPath = "/SOCIETY/ECONOMICS/GEOPOLITICS",
                    EpistemicManifesto = "Dòng chảy giá trị và phân bổ tài nguyên quyết định sự hưng thịnh của mọi nền văn minh.",
                    BenchmarkPrestigeElo = 2850,
                    Seats = new()
                    {
                        new() { SeatTitle = "Viện Trưởng Kinh Tế Lượng", Role = CouncilSeatRole.President, AlphaModelCode = "gemini-2.0-flash", PreferredModelCode = "gemini-2.0-flash", SwarmModelCodesJson = @"[""gemini-2.0-flash"", ""claude-3-5-sonnet"", ""deepseek-r1""]", IdealArchetype = CognitiveArchetype.MassiveContext },
                        new() { SeatTitle = "Chuyên Viên Mô Phỏng Đối Đầu Nash", Role = CouncilSeatRole.AdversarialCritic, AlphaModelCode = "deepseek-r1", PreferredModelCode = "deepseek-r1", SwarmModelCodesJson = @"[""deepseek-r1"", ""claude-3-5-sonnet"", ""qwen/qwen3.8-27b""]", IdealArchetype = CognitiveArchetype.DeepReasoner }
                    }
                },
                new()
                {
                    CouncilCode = "LIFE_SCIENCES",
                    DisplayName = "Viện Khoa Học Sự Sống & Sinh Quyển",
                    Category = CouncilCategory.Academic,
                    DisciplineArea = "Sinh học phân tử, Di truyền biểu sinh & Sinh thái hành tinh",
                    MaterializedTaxonomyPath = "/NATURE/BIOLOGY/EVOLUTION",
                    EpistemicManifesto = "Sự sống là cấu trúc tiêu tán vi mô chống lại entropy nhiệt động lực học.",
                    BenchmarkPrestigeElo = 2880,
                    Seats = new()
                    {
                        new() { SeatTitle = "Trưởng Ban Sinh Học Tiến Hóa", Role = CouncilSeatRole.President, AlphaModelCode = "deepseek-r1", PreferredModelCode = "deepseek-r1", SwarmModelCodesJson = @"[""deepseek-r1"", ""claude-3-5-sonnet"", ""gemini-2.0-flash""]", IdealArchetype = CognitiveArchetype.DeepReasoner },
                        new() { SeatTitle = "Thẩm Định Đa Dạng Sinh Thái", Role = CouncilSeatRole.EmpiricalAuditor, AlphaModelCode = "claude-3-5-sonnet", PreferredModelCode = "claude-3-5-sonnet", SwarmModelCodesJson = @"[""claude-3-5-sonnet"", ""gemini-2.0-flash"", ""gpt-4o""]", IdealArchetype = CognitiveArchetype.LinguisticArbiter }
                    }
                },
                new()
                {
                    CouncilCode = "ARTS_AESTHETICS",
                    DisplayName = "Viện Nghệ Thuật, Mỹ Học & Văn Hóa Đương Đại",
                    Category = CouncilCategory.Academic,
                    DisciplineArea = "Mỹ học, Lý luận phê bình văn học - nghệ thuật & Biểu tượng thị giác",
                    MaterializedTaxonomyPath = "/CULTURE/ARTS/AESTHETICS",
                    EpistemicManifesto = "Cái đẹp và sự thăng hoa nghệ thuật là biểu đạt trực giác tối thượng của tâm thức, vượt ngoài khả năng hình thức hóa bằng logic thuần túy.",
                    BenchmarkPrestigeElo = 2920,
                    Seats = new()
                    {
                        new() { SeatTitle = "Viện Trưởng Mỹ Học So Sánh", Role = CouncilSeatRole.President, AlphaModelCode = "claude-3-5-sonnet", PreferredModelCode = "claude-3-5-sonnet", SwarmModelCodesJson = @"[""claude-3-5-sonnet"", ""gemini-2.0-flash"", ""gpt-4o""]", IdealArchetype = CognitiveArchetype.LinguisticArbiter, SortOrder = 1 },
                        new() { SeatTitle = "Nhà Phê Bình Biểu Tượng & Thị Giác", Role = CouncilSeatRole.AdversarialCritic, AlphaModelCode = "deepseek-r1", PreferredModelCode = "deepseek-r1", SwarmModelCodesJson = @"[""deepseek-r1"", ""claude-3-5-sonnet"", ""qwen/qwen3.8-27b""]", IdealArchetype = CognitiveArchetype.DeepReasoner, SortOrder = 2 }
                    }
                },
                new()
                {
                    CouncilCode = "JURISPRUDENCE",
                    DisplayName = "Viện Pháp Lý & Quản Trị Văn Minh",
                    Category = CouncilCategory.Academic,
                    DisciplineArea = "Triết lý pháp quyền, Luật quốc tế, Hiến pháp & Khế ước xã hội số",
                    MaterializedTaxonomyPath = "/SOCIETY/LAW/JURISPRUDENCE",
                    EpistemicManifesto = "Công lý và chuẩn mực pháp lý là rào chắn bảo vệ trật tự và nhân phẩm văn minh trước sự bành trướng quyền lực vô kiểm soát.",
                    BenchmarkPrestigeElo = 2890,
                    Seats = new()
                    {
                        new() { SeatTitle = "Chánh Án Tòa Luận Đề Hiến Pháp", Role = CouncilSeatRole.President, AlphaModelCode = "claude-3-5-sonnet", PreferredModelCode = "claude-3-5-sonnet", SwarmModelCodesJson = @"[""claude-3-5-sonnet"", ""deepseek-r1"", ""gpt-4o""]", IdealArchetype = CognitiveArchetype.LinguisticArbiter, SortOrder = 1 },
                        new() { SeatTitle = "Công Tố Viên Phản Biện Khế Ước", Role = CouncilSeatRole.AdversarialCritic, AlphaModelCode = "deepseek-r1", PreferredModelCode = "deepseek-r1", SwarmModelCodesJson = @"[""deepseek-r1"", ""claude-3-5-sonnet"", ""qwen/qwen3.8-27b""]", IdealArchetype = CognitiveArchetype.DeepReasoner, SortOrder = 2 }
                    }
                },
                new()
                {
                    CouncilCode = "NEUROSCIENCE",
                    DisplayName = "Viện Não Bộ, Thần Kinh & Nhận Thức Học",
                    Category = CouncilCategory.Academic,
                    DisciplineArea = "Thần kinh học, Khoa học nhận thức, Cảm xúc sinh học & Tâm thức",
                    MaterializedTaxonomyPath = "/NATURE/NEUROSCIENCE/COGNITION",
                    EpistemicManifesto = "Trải nghiệm chủ quan và xúc cảm con người bắt nguồn từ sự tương tác mạng lưới nơ-ron phức tạp.",
                    BenchmarkPrestigeElo = 2940,
                    Seats = new()
                    {
                        new() { SeatTitle = "Viện Trưởng Khoa Học Nhận Thức", Role = CouncilSeatRole.President, AlphaModelCode = "deepseek-r1", PreferredModelCode = "deepseek-r1", SwarmModelCodesJson = @"[""deepseek-r1"", ""claude-3-5-sonnet"", ""gemini-2.0-flash""]", IdealArchetype = CognitiveArchetype.DeepReasoner, SortOrder = 1 },
                        new() { SeatTitle = "Nhà Thần Kinh Phản Biện Duy Não", Role = CouncilSeatRole.AdversarialCritic, AlphaModelCode = "claude-3-5-sonnet", PreferredModelCode = "claude-3-5-sonnet", SwarmModelCodesJson = @"[""claude-3-5-sonnet"", ""deepseek-r1"", ""gpt-4o""]", IdealArchetype = CognitiveArchetype.LinguisticArbiter, SortOrder = 2 }
                    }
                },

                // ==========================================
                // NHÓM 2: BAN THƯ KÝ HỢP NHẤT ĐẠI HỘI THẢO (SECRETARIAT COUNCIL)
                // ==========================================
                new()
                {
                    CouncilCode = "SYS_SECRETARIAT",
                    DisplayName = "Ban Thư Ký Hợp Nhất Đại Hội Thảo",
                    Category = CouncilCategory.Secretariat,
                    DisciplineArea = "Dung hợp siêu lý thuyết, Logic học liên ngành & Biên bản kỷ yếu",
                    MaterializedTaxonomyPath = "/SYSTEM/SECRETARIAT/SYNTHESIS",
                    EpistemicManifesto = "Dung hòa mọi đối kháng học thuật, kết tinh chân lý siêu lý thuyết vượt lên các góc nhìn đơn lẻ.",
                    BenchmarkPrestigeElo = 2990,
                    Seats = new()
                    {
                        new()
                        {
                            SeatTitle = "Thư Ký Trưởng Hợp Nhất Siêu Lý Thuyết",
                            Role = CouncilSeatRole.Synthesizer,
                            AlphaModelCode = "claude-3-5-sonnet",
                            PreferredModelCode = "claude-3-5-sonnet",
                            SwarmModelCodesJson = @"[""claude-3-5-sonnet"", ""gemini-2.0-flash"", ""deepseek-r1"", ""gpt-4o""]",
                            IdealArchetype = CognitiveArchetype.MassiveContext,
                            SortOrder = 1
                        },
                        new()
                        {
                            SeatTitle = "Thư Ký Trích Dẫn & Thẩm Tra Bằng Chứng",
                            Role = CouncilSeatRole.EmpiricalAuditor,
                            AlphaModelCode = "gemini-2.0-flash",
                            PreferredModelCode = "gemini-2.0-flash",
                            SwarmModelCodesJson = @"[""gemini-2.0-flash"", ""claude-3-5-sonnet"", ""deepseek-r1""]",
                            IdealArchetype = CognitiveArchetype.LinguisticArbiter,
                            SortOrder = 2
                        }
                    }
                },

                // ==========================================
                // NHÓM 3: TỔ TRỌNG TÀI ĐỘC LẬP & GIÁM KHẢO (ARBITRATION COUNCIL)
                // Đảm bảo đủ chân TẤT CẢ các họ Sói trong hệ thống để thực hiện Chấm Mù Độc Lập Tuyệt Đối.
                // ==========================================
                new()
                {
                    CouncilCode = "SYS_ARBITRATION",
                    DisplayName = "Tổ Trọng Tài Độc Lập & Giám Khảo Chấm Mù",
                    Category = CouncilCategory.Arbitration,
                    DisciplineArea = "Đánh giá ma trận Rubric 5 trục, Kiểm định Popperian & Thẩm định khách quan",
                    MaterializedTaxonomyPath = "/SYSTEM/ARBITRATION/EVALUATION",
                    EpistemicManifesto = "Sự công tâm tuyệt đối chỉ đạt được khi người phán xét hoàn toàn độc lập với các bên tranh luận.",
                    BenchmarkPrestigeElo = 3000,
                    Seats = new()
                    {
                        new()
                        {
                            SeatTitle = "Chánh Trọng Tài Độc Lập Chấm Mù",
                            Role = CouncilSeatRole.President,
                            AlphaModelCode = "deepseek-r1",
                            PreferredModelCode = "deepseek-r1",
                            SwarmModelCodesJson = @"[""deepseek-r1"", ""gemini-2.0-flash"", ""claude-3-5-sonnet"", ""groq-llama-3-3-70b"", ""ollama-qwen-local"", ""gpt-4o""]",
                            IdealArchetype = CognitiveArchetype.DeepReasoner,
                            SortOrder = 1,
                            PersonaPromptDirective = "Chánh Trọng Tài tối cao: Thẩm định Trục 1 (Logical Rigor) và tính toán Chỉ số Tiến Hóa Nhận Thức (EEV) tổng thể của phiên tranh luận."
                        },
                        new()
                        {
                            SeatTitle = "Trọng Tài Kiểm Chứng Thực Nghiệm & Sổ Cái Bằng Chứng",
                            Role = CouncilSeatRole.EmpiricalAuditor,
                            AlphaModelCode = "gemini-2.0-flash",
                            PreferredModelCode = "gemini-2.0-flash",
                            SwarmModelCodesJson = @"[""gemini-2.0-flash"", ""deepseek-r1"", ""claude-3-5-sonnet"", ""groq-llama-3-3-70b"", ""ollama-qwen-local"", ""gpt-4o""]",
                            IdealArchetype = CognitiveArchetype.MassiveContext,
                            SortOrder = 2,
                            PersonaPromptDirective = "Thẩm định khắt khe Trục 2 (Empirical Falsifiability) và Trục 4 (Pragmatic Actionability) qua việc đối chiếu từng bằng chứng mã hóa [E1..En] trong Sổ Cái Thực Địa."
                        },
                        new()
                        {
                            SeatTitle = "Trọng Tài Phản Biện Đối Kháng & Giải Bẫy Nhận Thức",
                            Role = CouncilSeatRole.AdversarialCritic,
                            AlphaModelCode = "claude-3-5-sonnet",
                            PreferredModelCode = "claude-3-5-sonnet",
                            SwarmModelCodesJson = @"[""claude-3-5-sonnet"", ""deepseek-r1"", ""gemini-2.0-flash"", ""groq-llama-3-3-70b"", ""ollama-qwen-local"", ""gpt-4o""]",
                            IdealArchetype = CognitiveArchetype.LinguisticArbiter,
                            SortOrder = 3,
                            PersonaPromptDirective = "Thẩm định Trục 3 (Ontological Depth) và Trục 5 (Trap Resistance) qua Tiêu chuẩn Phản nghiệm Popperian, vạch trần các bẫy ngụy biện và thiên kiến xác nhận."
                        },
                        new()
                        {
                            SeatTitle = "Bồi Thẩm Đoàn Đa Mô Hình Đồng Thuận",
                            Role = CouncilSeatRole.Synthesizer,
                            AlphaModelCode = "groq-llama-3-3-70b",
                            PreferredModelCode = "groq-llama-3-3-70b",
                            SwarmModelCodesJson = @"[""groq-llama-3-3-70b"", ""deepseek-r1"", ""gemini-2.0-flash"", ""claude-3-5-sonnet"", ""ollama-qwen-local"", ""gpt-4o""]",
                            IdealArchetype = CognitiveArchetype.MassiveContext,
                            SortOrder = 4,
                            PersonaPromptDirective = "Đóng vai trò Bồi thẩm đoàn tổng hợp, điều hòa sự dị biệt quan điểm giữa các họ mô hình AI để đưa ra kết luận trung dung khách quan nhất."
                        }
                    }
                },

                // ==========================================
                // NHÓM 4: HỘI ĐỒNG HỆ THỐNG CỐT LÕI (SYS_COUNCIL)
                // Phụ trách toàn bộ các tác vụ nền tảng: Audit câu hỏi, Sinh đề, Gameshow, Bảo mật
                // ==========================================
                new()
                {
                    CouncilCode = "SYS_CORE",
                    DisplayName = "Hội Đồng Hệ Thống Cốt Lõi AegisQuiz",
                    Category = CouncilCategory.SystemCore,
                    DisciplineArea = "Khảo thí câu hỏi, Sinh đề thi, Gameshow real-time & Bảo mật Intranet",
                    MaterializedTaxonomyPath = "/SYSTEM/CORE/SWARM_GATEWAY",
                    EpistemicManifesto = "5 Bầy Sói nòng cốt vận hành liên tục đảm bảo độ tin cậy và chất lượng học thuật toàn nền tảng.",
                    BenchmarkPrestigeElo = 3000,
                    Seats = new()
                    {
                        new()
                        {
                            SeatTitle = "Tiểu Ban Suy Luận Logic Chuỗi Dài (CoT)",
                            Role = CouncilSeatRole.President,
                            AlphaModelCode = "deepseek-r1",
                            PreferredModelCode = "deepseek-r1",
                            SwarmModelCodesJson = @"[""deepseek-r1"", ""qwen/qwen3.8-27b"", ""claude-3-5-sonnet""]",
                            IdealArchetype = CognitiveArchetype.DeepReasoner,
                            SortOrder = 1
                        },
                        new()
                        {
                            SeatTitle = "Tiểu Ban Bóc Tách Ngữ Cảnh Cực Đại",
                            Role = CouncilSeatRole.Synthesizer,
                            AlphaModelCode = "gemini-2.0-flash",
                            PreferredModelCode = "gemini-2.0-flash",
                            SwarmModelCodesJson = @"[""gemini-2.0-flash"", ""claude-3-5-sonnet"", ""gpt-4o-mini""]",
                            IdealArchetype = CognitiveArchetype.MassiveContext,
                            SortOrder = 2
                        },
                        new()
                        {
                            SeatTitle = "Tiểu Ban Trọng Tài Ngôn Ngữ & Sư Phạm",
                            Role = CouncilSeatRole.AdversarialCritic,
                            AlphaModelCode = "claude-3-5-sonnet",
                            PreferredModelCode = "claude-3-5-sonnet",
                            SwarmModelCodesJson = @"[""claude-3-5-sonnet"", ""gemini-2.0-flash"", ""deepseek-r1""]",
                            IdealArchetype = CognitiveArchetype.LinguisticArbiter,
                            SortOrder = 3
                        },
                        new()
                        {
                            SeatTitle = "Tiểu Ban Siêu Tốc Độ Real-Time (<250ms)",
                            Role = CouncilSeatRole.EmpiricalAuditor,
                            AlphaModelCode = "qwen/qwen3.8-27b",
                            PreferredModelCode = "qwen/qwen3.8-27b",
                            SwarmModelCodesJson = @"[""qwen/qwen3.8-27b"", ""gemini-2.0-flash"", ""mistral-small-latest""]",
                            IdealArchetype = CognitiveArchetype.UltraFastScanner,
                            SortOrder = 4
                        },
                        new()
                        {
                            SeatTitle = "Tiểu Ban Bảo Mật Chủ Quyền Nội Bộ On-Premise",
                            Role = CouncilSeatRole.EmpiricalAuditor,
                            AlphaModelCode = "ollama/deepseek-r1:7b",
                            PreferredModelCode = "ollama/deepseek-r1:7b",
                            SwarmModelCodesJson = @"[""ollama/deepseek-r1:7b"", ""ollama/llama3.3:8b""]",
                            IdealArchetype = CognitiveArchetype.SovereignLocal,
                            SortOrder = 5
                        }
                    }
                }
            };
        }
    }
}
