using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.AI.Selection;

namespace AegisQuiz.UnitTests
{
    public class EpistemicRouterTests
    {
        private readonly List<UniversalAiCouncil> _testCouncils;

        public EpistemicRouterTests()
        {
            _testCouncils = new List<UniversalAiCouncil>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    CouncilCode = "LIFE_SCIENCES",
                    DisplayName = "Viện Sinh Học Tiến Hóa & Y Sinh Phân Tử",
                    DisciplineArea = "Sinh học phân tử, Di truyền học, Hệ sinh thái",
                    MaterializedTaxonomyPath = "/NATURE/BIOLOGY/GENETICS",
                    TriggerKeywordsJson = "[\"gen\", \"di truyền\", \"sinh học\", \"hệ sinh thái\", \"loài\", \"sinh vật\"]",
                    ReadinessStatus = "READY",
                    BenchmarkPrestigeElo = 2950
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    CouncilCode = "ECONOMICS",
                    DisplayName = "Viện Kinh Tế Học & Chính Sách Tài Khóa",
                    DisciplineArea = "Kinh tế học vĩ mô, Thị trường tài chính",
                    MaterializedTaxonomyPath = "/SOCIETY/ECONOMICS/FINANCE",
                    TriggerKeywordsJson = "[\"kinh tế\", \"tài chính\", \"vốn\", \"lạm phát\", \"thị trường\"]",
                    ReadinessStatus = "READY",
                    BenchmarkPrestigeElo = 2900
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    CouncilCode = "COSMOLOGY",
                    DisplayName = "Viện Vật Lý Vũ Trụ & Thiên Văn Lượng Tử",
                    DisciplineArea = "Vật lý lý thuyết, Lượng tử học, Vũ trụ học",
                    MaterializedTaxonomyPath = "/UNIVERSE/PHYSICS/QUANTUM",
                    TriggerKeywordsJson = "[\"lượng tử\", \"vật lý\", \"vũ trụ\", \"nghịch lý\", \"hạt\"]",
                    ReadinessStatus = "READY",
                    BenchmarkPrestigeElo = 2990
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    CouncilCode = "PHILOSOPHY",
                    DisplayName = "Viện Triết Học & Bản Thể Luận Đương Đại",
                    DisciplineArea = "Bản thể luận, Nhận thức luận, Đạo đức học",
                    MaterializedTaxonomyPath = "/HUMANITY/PHILOSOPHY/EPISTEMOLOGY",
                    TriggerKeywordsJson = "[\"triết học\", \"nhận thức luận\", \"bản thể\", \"đạo đức\"]",
                    ReadinessStatus = "READY",
                    BenchmarkPrestigeElo = 2980
                }
            };
        }

        [Fact]
        public async Task FastEpistemicIntentRouter_PolysemyFilter_CorrectlyIdentifiesLifeSciences_InsteadOfEconomics()
        {
            var router = new FastEpistemicIntentRouter(NullLogger<FastEpistemicIntentRouter>.Instance, null);

            var intent = await router.RouteEpistemicIntentAsync(
                "Bảo tồn vốn gen và đa dạng sinh học trong rừng ngập mặn Cần Giờ",
                "Làm sao bảo vệ quần thể loài trước nguy cơ tuyệt chủng và suy thoái nguồn gen?",
                _testCouncils);

            Assert.Equal("LIFE_SCIENCES", intent.PrimaryDomain);
            Assert.Equal("LIFE_SCIENCES", intent.RecommendedLeadCouncilCode);
            Assert.Equal("ECONOMICS", intent.RecommendedContrarianCouncilCode);
            Assert.False(string.IsNullOrWhiteSpace(intent.DialecticalReason));
        }

        [Fact]
        public async Task FastEpistemicIntentRouter_QuantumPhysics_RecommendsPhilosophyAsDialecticalContrarian()
        {
            var router = new FastEpistemicIntentRouter(NullLogger<FastEpistemicIntentRouter>.Instance, null);

            var intent = await router.RouteEpistemicIntentAsync(
                "Hiện tượng rối lượng tử và nghịch lý EPR",
                "Tính phi cục bộ của cơ học lượng tử có phá vỡ nguyên lý nhân quả trong thuyết tương đối hẹp?",
                _testCouncils);

            Assert.Equal("QUANTUM_PHYSICS", intent.PrimaryDomain);
            Assert.Equal("COSMOLOGY", intent.RecommendedLeadCouncilCode);
            Assert.Equal("PHILOSOPHY", intent.RecommendedContrarianCouncilCode);
            Assert.Contains("Bản thể luận", intent.DialecticalReason);
        }

        [Fact]
        public async Task DiverseEpistemicCouncilSelectionStrategy_UsesRouterToAvoidPolysemyTrap()
        {
            var router = new FastEpistemicIntentRouter(NullLogger<FastEpistemicIntentRouter>.Instance, null);
            var strategy = new DiverseEpistemicCouncilSelectionStrategy(
                NullLogger<DiverseEpistemicCouncilSelectionStrategy>.Instance,
                null,
                router);

            var context = new CouncilSelectionContext
            {
                TopicTitle = "Bảo tồn vốn gen và sinh thái",
                CoreProblemStatement = "Đánh giá sự đa dạng di truyền trong quần xã",
                MaxCouncils = 3,
                IncludeContrarianPerspective = true
            };

            var selected = await strategy.SelectCouncilsAsync(context, _testCouncils);

            Assert.NotEmpty(selected);
            // Viện đầu tiên phải là LIFE_SCIENCES
            Assert.Equal("LIFE_SCIENCES", selected.First().CouncilCode);
            // Có mặt Viện Đối Trọng Biện Chứng (ECONOMICS hoặc PHILOSOPHY)
            Assert.True(selected.Any(c => c.CouncilCode == "ECONOMICS" || c.CouncilCode == "PHILOSOPHY"));
        }
    }
}
