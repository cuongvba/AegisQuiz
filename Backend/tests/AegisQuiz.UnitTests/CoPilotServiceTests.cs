using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Infrastructure.Services;
using AegisQuiz.Infrastructure.Services.CoPilot;
using Xunit;

namespace AegisQuiz.UnitTests
{
    public class CoPilotServiceTests
    {
        private class FakeAiRouter : IUniversalAiRouter
        {
            public Task<AiExecutionResponse> ExecuteAsync(AiExecutionRequest request)
            {
                return Task.FromResult(new AiExecutionResponse
                {
                    Success = true,
                    Content = "Fake AI response",
                    ModelUsed = "fake-model",
                    DurationMs = 25,
                    TokensUsed = 100
                });
            }

            public Task<AiExecutionResponse> ExecuteModelDirectAsync(string modelCode, AiExecutionRequest request, Guid? userId = null, Guid? tenantId = null, CancellationToken ct = default)
                => ExecuteAsync(request);

#pragma warning disable CS0618
            public Task<PedagogicalWalkthroughDto> GenerateWalkthroughAsync(string questionContent, List<string> options, string correctAnswer, string domainCode, AgentPersonaType persona = AgentPersonaType.SupremeArbiter, string? tenantId = null)
                => Task.FromResult(new PedagogicalWalkthroughDto());
#pragma warning restore CS0618

            public Task<IReadOnlyList<AiModelRegistryDto>> GetActiveNormalizedModelsAsync(CognitiveArchetype? archetypeFilter = null, CancellationToken ct = default)
                => Task.FromResult<IReadOnlyList<AiModelRegistryDto>>(new List<AiModelRegistryDto>());

            public Task<IReadOnlyList<GatewayModelPresetDto>> GetDynamicGatewayPresetsAsync(string? gateway = null, CancellationToken ct = default)
                => Task.FromResult<IReadOnlyList<GatewayModelPresetDto>>(new List<GatewayModelPresetDto>());

            public AiSystemConfigDto GetSystemConfig() => new AiSystemConfigDto();
            public void RefreshRegistryFromDatabase() { }
            public Task RefreshRegistryFromDatabaseAsync() => Task.CompletedTask;
            public void RegisterTenantByok(TenantByokConfigDto byokConfig) { }
            public Task<Dictionary<string, (bool Success, long LatencyMs, string Message)>> TestProvidersAsync()
                => Task.FromResult(new Dictionary<string, (bool Success, long LatencyMs, string Message)>());
            public void UpdateSystemConfig(AiSystemConfigDto config) { }
        }

        private class FakeSymposiumEngine : IUniversalSymposiumEngine
        {
            public Task<List<UniversalAiCouncil>> GetActiveCouncilsAsync(CancellationToken ct = default)
                => Task.FromResult(new List<UniversalAiCouncil>());

            public Task<SymposiumProceedingsResult> ConveneSymposiumAsync(ConveneSymposiumRequest request, CancellationToken ct = default)
            {
                return Task.FromResult(new SymposiumProceedingsResult
                {
                    SessionId = Guid.NewGuid(),
                    TopicTitle = request.TopicTitle,
                    RoundNumber = 1,
                    DialogueLog = new List<SymposiumDialogueTurn>
                    {
                        new SymposiumDialogueTurn
                        {
                            CouncilCode = "EDU_MEASUREMENT",
                            CouncilDisplayName = "Viện Hàn Lâm Sư Phạm & Đo Lường Khảo Thí",
                            SpeakerTitle = "Chủ nhiệm Viện Khảo Thí",
                            ModelCode = "claude-3-5-sonnet",
                            Content = "AI cần hỗ trợ cá nhân hóa năng lực Latent Trait Theta.",
                            PhaseName = "Formulation",
                            PhaseNumber = 1
                        }
                    },
                    EvaluatedRubric = new SymposiumRubricScore
                    {
                        IsEvaluated = true,
                        LogicalRigor = 9.0,
                        EmpiricalFalsifiability = 9.0,
                        OntologicalDepth = 9.0,
                        PragmaticActionability = 9.0,
                        TrapResistance = 9.0
                    }
                });
            }

            public Task<Guid> ExportSymposiumToNotebookAsync(Guid sessionId, Guid? targetNotebookId, Guid userId, Guid tenantId, CancellationToken ct = default)
                => Task.FromResult(Guid.NewGuid());

            public Task<UniversalAiCouncil> CreateOrUpdateCouncilAsync(UniversalAiCouncil council, CancellationToken ct = default)
                => Task.FromResult(council);

            public Task<bool> DeleteCouncilAsync(Guid councilId, CancellationToken ct = default)
                => Task.FromResult(true);

            public Task<UniversalCouncilSeat> AddCouncilSeatAsync(Guid councilId, UniversalCouncilSeat seat, CancellationToken ct = default)
                => Task.FromResult(seat);

            public Task<bool> RemoveCouncilSeatAsync(Guid seatId, CancellationToken ct = default)
                => Task.FromResult(true);

            public Task<UniversalCouncilSeat?> UpdateCouncilSeatSwarmAsync(
                Guid seatId, string alphaModelCode, string swarmModelCodesJson, string swarmPolicy,
                string? personaPromptDirective = null, string? seatTitle = null, CouncilSeatRole? role = null,
                double? decisionWeight = null, CancellationToken ct = default)
                => Task.FromResult<UniversalCouncilSeat?>(new UniversalCouncilSeat());
        }

        private class FakeSwarmEngine : IWolfPackSwarmEngine
        {
            public Task<WolfPackSwarmAuditResult> AuditQuestionWithSwarmAsync(WolfPackAuditRequest request, CancellationToken ct = default)
            {
                return Task.FromResult(new WolfPackSwarmAuditResult
                {
                    ConsensusIndex = 0.96,
                    ConsensusLevel = "HIGH_CONSENSUS",
                    AlphaModelCode = "claude-3-5-sonnet",
                    AlphaModelDisplayName = "Claude 3.5 Sonnet (Alpha Arbiter)",
                    WinningAnswer = "A. $S_p = 0.50$",
                    SynthesisSummary = "Bầy Sói xác thực 100% cú pháp KaTeX và bẫy phương án.",
                    TotalLatencyMs = 320,
                    MemberVerdicts = new List<WolfPackMemberVerdict>
                    {
                        new WolfPackMemberVerdict
                        {
                            DisplayName = "DeepSeek-R1",
                            ProviderCompany = "DeepSeek",
                            Archetype = CognitiveArchetype.DeepReasoner,
                            ConfidenceScore = 0.98,
                            LatencyMs = 280,
                            IsSuccess = true
                        },
                        new WolfPackMemberVerdict
                        {
                            DisplayName = "Claude 3.5 Sonnet",
                            ProviderCompany = "Anthropic",
                            Archetype = CognitiveArchetype.LinguisticArbiter,
                            ConfidenceScore = 0.95,
                            LatencyMs = 320,
                            IsSuccess = true
                        }
                    }
                });
            }

            public Task<TestAiKeyResult> PingModelKeyAsync(TestAiKeyRequest request, CancellationToken ct = default)
                => Task.FromResult(new TestAiKeyResult { Success = true });

            public Task<List<AiModelRegistryDto>> GetActiveModelsAsync(Guid? tenantId = null, CancellationToken ct = default)
            {
                // Trả về danh sách nạp từ DB (mô phỏng)
                return Task.FromResult(new List<AiModelRegistryDto>
                {
                    new() { ModelCode = "claude-3-5-sonnet", DisplayName = "Claude 3.5 Sonnet", ProviderCompany = "Anthropic", CognitiveArchetype = CognitiveArchetype.LinguisticArbiter, AvgLatencyMs = 620, CostOutputPerMillion = 3.0, IsEnabled = true },
                    new() { ModelCode = "gemini-2-5-flash", DisplayName = "Gemini 2.5 Flash", ProviderCompany = "Google", CognitiveArchetype = CognitiveArchetype.UltraFastScanner, AvgLatencyMs = 180, CostOutputPerMillion = 0.15, IsEnabled = true },
                    new() { ModelCode = "gpt-4o", DisplayName = "GPT-4o (Omni)", ProviderCompany = "OpenAI", CognitiveArchetype = CognitiveArchetype.MassiveContext, AvgLatencyMs = 550, CostOutputPerMillion = 2.5, IsEnabled = true },
                    new() { ModelCode = "deepseek-r1-local", DisplayName = "DeepSeek-R1", ProviderCompany = "DeepSeek", CognitiveArchetype = CognitiveArchetype.DeepReasoner, AvgLatencyMs = 450, CostOutputPerMillion = 0.55, IsEnabled = true }
                });
            }

            public Task<AiModelRegistryDto> UpsertModelAsync(UpsertAiModelRequest request, Guid tenantId, CancellationToken ct = default)
                => Task.FromResult(new AiModelRegistryDto());

            public Task<bool> DeleteModelAsync(Guid id, Guid tenantId, CancellationToken ct = default)
                => Task.FromResult(true);

            public Task<List<UserAiKeyDto>> GetUserKeysAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
                => Task.FromResult(new List<UserAiKeyDto>());

            public Task<UserAiKeyDto> SaveUserKeyAsync(SaveUserAiKeyRequest request, Guid userId, Guid tenantId, CancellationToken ct = default)
                => Task.FromResult(new UserAiKeyDto());

            public Task<bool> DeleteUserKeyAsync(Guid keyId, Guid userId, Guid tenantId, CancellationToken ct = default)
                => Task.FromResult(true);

            public Task<List<CommunityContributorHonorDto>> GetCommunityHallOfFameAsync(int limit = 10, CancellationToken ct = default)
                => Task.FromResult(new List<CommunityContributorHonorDto>());

            public Task<List<GatewayModelPresetDto>> GetGatewayPresetsAsync(string gateway, CancellationToken ct = default)
                => Task.FromResult(new List<GatewayModelPresetDto>());

            public Task<ImportGatewayModelsResult> ImportGatewayModelsAsync(ImportGatewayModelsRequest request, Guid tenantId, CancellationToken ct = default)
                => Task.FromResult(new ImportGatewayModelsResult());
        }

        private readonly FakeAiRouter _fakeAiRouter = new();
        private readonly FakeSymposiumEngine _fakeSymposiumEngine = new();
        private readonly FakeSwarmEngine _fakeSwarmEngine = new();
        private readonly AegisQuizDbContext _dbContext;

        public CoPilotServiceTests()
        {
            var options = new DbContextOptionsBuilder<AegisQuizDbContext>()
                .UseInMemoryDatabase(databaseName: $"AegisCoPilotTestDb_{Guid.NewGuid()}")
                .Options;
            _dbContext = new AegisQuizDbContext(options);
        }

        [Fact]
        public async Task GetAvailableCommands_Should_Return_Standard_Slash_Commands()
        {
            // Arrange
            var service = new CoPilotService(
                _fakeAiRouter,
                _fakeSymposiumEngine,
                _fakeSwarmEngine,
                _dbContext,
                NullLogger<CoPilotService>.Instance);

            // Act
            var commands = await service.GetAvailableCommandsAsync();

            // Assert
            Assert.NotEmpty(commands);
            Assert.Contains(commands, c => c.Command == "/soan");
            Assert.Contains(commands, c => c.Command == "/hoithao");
            Assert.Contains(commands, c => c.Command == "/ma-tran");
            Assert.Contains(commands, c => c.Command == "/gia-su");
            Assert.Contains(commands, c => c.Command == "/kiem-dinh");
        }

        [Fact]
        public async Task ProcessPrompt_ComposeQuestionCommand_Should_Generate_Interactive_Question_Artifact()
        {
            // Arrange
            var service = new CoPilotService(
                _fakeAiRouter,
                _fakeSymposiumEngine,
                _fakeSwarmEngine,
                _dbContext,
                NullLogger<CoPilotService>.Instance);

            var req = new CoPilotRequestDto
            {
                Message = "/soan câu hỏi trắc nghiệm Toán tài chính về tỷ số Sharpe",
                Persona = CoPilotPersona.AssessmentArchitect
            };

            // Act
            var res = await service.ProcessPromptAsync(req);

            // Assert
            Assert.NotNull(res);
            Assert.NotEmpty(res.ThoughtTrace);
            Assert.NotEmpty(res.ToolCalls);
            Assert.Contains(res.ToolCalls, t => t.ToolName == "BloomTaxonomyAligner");
            Assert.Contains(res.ToolCalls, t => t.ToolName == "IrtParameterCalibrator");
            Assert.NotNull(res.Artifact);
            Assert.Equal("question", res.Artifact.ArtifactType);
            Assert.True(res.Artifact.CanApplyDirectly);
            Assert.NotEmpty(res.SuggestedActions);
        }

        [Fact]
        public async Task ProcessPrompt_SwarmAuditCommand_Should_Activate_WPSE_And_Return_Consensus()
        {
            // Arrange
            var service = new CoPilotService(
                _fakeAiRouter,
                _fakeSymposiumEngine,
                _fakeSwarmEngine,
                _dbContext,
                NullLogger<CoPilotService>.Instance);

            var req = new CoPilotRequestDto
            {
                Message = "/kiem-dinh câu hỏi trắc nghiệm: Một danh mục đầu tư có E(Rp) = 14%...",
                Persona = CoPilotPersona.GovernanceFinOps
            };

            // Act
            var res = await service.ProcessPromptAsync(req);

            // Assert
            Assert.NotNull(res);
            Assert.NotNull(res.Artifact);
            Assert.Equal("swarm-audit", res.Artifact.ArtifactType);
            Assert.Contains("WPSE Swarm Audit", res.Content);
            Assert.Contains("96%", res.Content);
            Assert.NotEmpty(res.ToolCalls);
            Assert.Contains(res.ToolCalls, t => t.ToolName.StartsWith("WPSE:"));
        }

        [Fact]
        public async Task ProcessPrompt_SymposiumCommand_Should_Convene_Councils_And_Return_Proceedings()
        {
            // Arrange
            var service = new CoPilotService(
                _fakeAiRouter,
                _fakeSymposiumEngine,
                _fakeSwarmEngine,
                _dbContext,
                NullLogger<CoPilotService>.Instance);

            var req = new CoPilotRequestDto
            {
                Message = "/hoithao Đạo đức sử dụng AI trong khảo thí",
                Persona = CoPilotPersona.ResearchScholar
            };

            // Act
            var res = await service.ProcessPromptAsync(req);

            // Assert
            Assert.NotNull(res);
            Assert.NotNull(res.Artifact);
            Assert.Equal("symposium", res.Artifact.ArtifactType);
            Assert.Contains("Kỷ Yếu Tranh Biện Đại Hội Thảo", res.Content);
            Assert.Contains(res.ToolCalls, t => t.ToolName == "SummonEpistemicCouncils");
        }

        [Fact]
        public async Task ProcessPrompt_SocraticTutorCommand_Should_Guide_Without_Direct_Answer()
        {
            // Arrange
            var service = new CoPilotService(
                _fakeAiRouter,
                _fakeSymposiumEngine,
                _fakeSwarmEngine,
                _dbContext,
                NullLogger<CoPilotService>.Instance);

            var req = new CoPilotRequestDto
            {
                Message = "/gia-su giải thích vì sao tôi chọn phương án B lại sai?",
                Persona = CoPilotPersona.SocraticMentor
            };

            // Act
            var res = await service.ProcessPromptAsync(req);

            // Assert
            Assert.NotNull(res);
            Assert.Contains(res.ToolCalls, t => t.ToolName == "SocraticHeuristicEngine");
            Assert.Contains("Socratic", res.ThoughtTrace);
        }

        [Fact]
        public async Task ProcessPrompt_BloomMatrixCommand_Should_Return_6_Levels_Matrix()
        {
            // Arrange
            var service = new CoPilotService(
                _fakeAiRouter,
                _fakeSymposiumEngine,
                _fakeSwarmEngine,
                _dbContext,
                NullLogger<CoPilotService>.Instance);

            var req = new CoPilotRequestDto
            {
                Message = "/ma-tran đề thi 40 câu môn Kinh tế học",
                Persona = CoPilotPersona.AssessmentArchitect
            };

            // Act
            var res = await service.ProcessPromptAsync(req);

            // Assert
            Assert.NotNull(res);
            Assert.NotNull(res.Artifact);
            Assert.Equal("matrix", res.Artifact.ArtifactType);
            Assert.True(res.Artifact.CanApplyDirectly);
        }

        [Fact]
        public async Task ExportToNotebook_Should_Save_Conversation_And_Artifact_Into_Notebook()
        {
            // Arrange
            var testTenantId = Guid.NewGuid();
            var testUserId = Guid.NewGuid();

            var service = new CoPilotService(
                _fakeAiRouter,
                _fakeSymposiumEngine,
                _fakeSwarmEngine,
                _dbContext,
                NullLogger<CoPilotService>.Instance);

            var exportReq = new ExportCoPilotToNotebookRequest
            {
                Title = "Kỷ Yếu Tranh Biện AI Khảo Thí",
                Content = "Nội dung thảo luận sâu sắc giữa các viện hàn lâm về đạo đức AI trong thi cử.",
                ArtifactType = "symposium",
                ArtifactData = new { consensus = 95.5, topic = "Đạo đức AI" },
                UserId = testUserId,
                TenantId = testTenantId
            };

            // Act
            var exportRes = await service.ExportToNotebookAsync(exportReq);

            // Assert
            Assert.NotNull(exportRes);
            Assert.True(exportRes.Success);
            Assert.NotEqual(Guid.Empty, exportRes.NotebookId);
            Assert.NotNull(exportRes.ChapterId);

            // Xác thực dữ liệu được lưu vĩnh viễn trong CSDL
            var savedNotebook = await _dbContext.Notebooks.Include(n => n.Chapters).FirstOrDefaultAsync(n => n.Id == exportRes.NotebookId);
            Assert.NotNull(savedNotebook);
            Assert.Equal("COPILOT_ARCHIVE", savedNotebook.DomainCode);
            Assert.Single(savedNotebook.Chapters);
            Assert.Equal("Kỷ Yếu Tranh Biện AI Khảo Thí", savedNotebook.Chapters[0].Title);
            Assert.Contains("📦 Kỷ Yếu Dữ Liệu Artifact", savedNotebook.Chapters[0].RawContent);
        }

        [Fact]
        public async Task CustomCommandHandler_OCP_Extension_Should_Work_Without_Modifying_CoPilotService()
        {
            // Minh chứng OCP: Tạo một Custom Command Handler độc lập
            var customHandler = new TestCustomCommandHandler();
            var handlers = new List<ICoPilotCommandHandler> { customHandler };

            var service = new CoPilotService(
                _fakeAiRouter,
                _fakeSymposiumEngine,
                _fakeSwarmEngine,
                _dbContext,
                NullLogger<CoPilotService>.Instance,
                handlers);

            // Act
            var availableCommands = await service.GetAvailableCommandsAsync();
            var response = await service.ProcessPromptAsync(new CoPilotRequestDto { Message = "/olympia Đề thi chung kết toán học" });

            // Assert: CoPilotService nhận diện và thực thi lệnh mới mà không cần sửa 1 dòng code lõi!
            Assert.Contains(availableCommands, c => c.Command == "/olympia");
            Assert.Equal("OlympiaHandlerExecuted", response.ThoughtTrace);
        }

        [Fact]
        public async Task GetGroupedModels_Should_Group_By_Providers_And_Recommend_DeepSeek_For_Math()
        {
            // Arrange
            var service = new CoPilotService(
                _fakeAiRouter,
                _fakeSymposiumEngine,
                _fakeSwarmEngine,
                _dbContext,
                NullLogger<CoPilotService>.Instance);

            // Act: Prompt về toán giải tích
            var groups = await service.GetGroupedModelsAsync(prompt: "Soạn câu hỏi Toán giải tích và tỷ số Sharpe");

            // Assert
            Assert.NotEmpty(groups);
            Assert.Contains(groups, g => g.ProviderName == "Anthropic");
            Assert.Contains(groups, g => g.ProviderName == "Google Gemini");
            Assert.Contains(groups, g => g.ProviderName == "OpenAI");
            Assert.Contains(groups, g => g.ProviderName == "DeepSeek & Open-Source");

            // Kiểm tra model được khuyên dùng cho Toán là DeepSeek-R1
            var allModels = new List<CoPilotModelItemDto>();
            foreach (var g in groups) allModels.AddRange(g.Models);
            var recommended = allModels.Find(m => m.IsRecommended);
            Assert.NotNull(recommended);
            Assert.Equal("deepseek-r1-local", recommended.ModelCode);
        }

        [Fact]
        public async Task GetGroupedModels_Waterfall_Should_Prioritize_UserBYOK_Over_Tenant_And_System()
        {
            // Arrange
            var testUserId = Guid.NewGuid();
            var testTenantId = Guid.NewGuid();

            // Thêm UserBYOK cho Anthropic
            _dbContext.UserAiKeys.Add(new UserAiKey
            {
                Id = Guid.NewGuid(),
                UserId = testUserId,
                TenantId = testTenantId,
                KeyLabel = "My Personal Claude Key",
                ProviderCompany = "Anthropic",
                EncryptedApiKey = "encrypted_key",
                Scope = KeyUsageScope.Private,
                IsActive = true
            });

            // Thêm TenantBYOK cho Google
            _dbContext.UserAiKeys.Add(new UserAiKey
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(), // người khác
                TenantId = testTenantId,
                KeyLabel = "University Gemini Key",
                ProviderCompany = "Google",
                EncryptedApiKey = "encrypted_key",
                Scope = KeyUsageScope.Private,
                IsActive = true
            });
            await _dbContext.SaveChangesAsync();

            var service = new CoPilotService(
                _fakeAiRouter,
                _fakeSymposiumEngine,
                _fakeSwarmEngine,
                _dbContext,
                NullLogger<CoPilotService>.Instance);

            // Act
            var groups = await service.GetGroupedModelsAsync(
                prompt: "Tài liệu giáo trình",
                userId: testUserId,
                tenantId: testTenantId);

            // Assert
            var allModels = new List<CoPilotModelItemDto>();
            foreach (var g in groups) allModels.AddRange(g.Models);

            // Claude phải được nhận diện là UserBYOK (Ưu tiên cao nhất)
            var claudeModel = allModels.Find(m => m.ModelCode == "claude-3-5-sonnet");
            Assert.NotNull(claudeModel);
            Assert.Equal("UserBYOK", claudeModel.KeySource);

            // Gemini phải được nhận diện là TenantBYOK (Ưu tiên thứ nhì)
            var geminiModel = allModels.Find(m => m.ModelCode == "gemini-2-5-flash");
            Assert.NotNull(geminiModel);
            Assert.Equal("TenantBYOK", geminiModel.KeySource);

            // OpenAI không có key riêng thì dùng SystemDefault (Ưu tiên cuối)
            var gptModel = allModels.Find(m => m.ModelCode == "gpt-4o");
            Assert.NotNull(gptModel);
            Assert.Equal("SystemDefault", gptModel.KeySource);
        }

        private class TestCustomCommandHandler : ICoPilotCommandHandler
        {
            public string Command => "/olympia";
            public string Description => "Thử thách đỉnh cao toán học Olympic";
            public string Example => "/olympia Bài toán hình học phẳng";
            public CoPilotPersona RecommendedPersona => CoPilotPersona.AssessmentArchitect;
            public string Icon => "Award";

            public bool CanHandle(string message) => message.StartsWith("/olympia");

            public Task<CoPilotResponseDto> HandleAsync(CoPilotRequestDto request, string commandArgs, Stopwatch sw, CancellationToken ct = default)
            {
                return Task.FromResult(new CoPilotResponseDto
                {
                    ThoughtTrace = "OlympiaHandlerExecuted",
                    Content = "Đề thi Olympic đã sẵn sàng."
                });
            }
        }
    }
}
