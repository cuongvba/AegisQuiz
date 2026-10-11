using System;
using System.Collections.Generic;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Models.AI
{
    // [AegisAI Swarm] Yêu cầu thẩm định bầy sói đa mô hình
    public class WolfPackAuditRequest
    {
        public string Prompt { get; set; } = string.Empty;
        public string? Context { get; set; }
        public string? QuestionContent { get; set; }
        public string? ProposedAnswer { get; set; }
        public List<CognitiveArchetype>? TargetArchetypes { get; set; }
        public Guid? UserId { get; set; }
        public Guid? TenantId { get; set; }
        public int TimeoutSeconds { get; set; } = 15;
    }

    // [AegisAI Swarm] Phán quyết của từng thành viên trong bầy sói
    public class WolfPackMemberVerdict
    {
        public string ModelCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ProviderCompany { get; set; } = string.Empty;
        public CognitiveArchetype Archetype { get; set; }
        public string RawResponse { get; set; } = string.Empty;
        public string NormalizedAnswer { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; } = 0.9;
        public string PedagogicalRationale { get; set; } = string.Empty;
        public List<string> IdentifiedFlaws { get; set; } = new();
        public long LatencyMs { get; set; } = 0;
        public double TokenCostUsd { get; set; } = 0.0;
        public string KeySource { get; set; } = "SystemDefault"; // 'UserBYOK', 'TenantBYOK', 'CommunityPool', 'SystemDefault'
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }

    // [AegisAI Swarm] Kết quả tổng hợp hội chẩn bầy sói
    public class WolfPackSwarmAuditResult
    {
        public double ConsensusIndex { get; set; } = 1.0; // SCI ∈ [0.0, 1.0]
        public string ConsensusLevel { get; set; } = "UNANIMOUS"; // UNANIMOUS, HIGH_CONSENSUS, MODERATE_DIVERGENCE, CONTESTED_FLAG
        public string AlphaModelCode { get; set; } = string.Empty;
        public string AlphaModelDisplayName { get; set; } = string.Empty;
        public string WinningAnswer { get; set; } = string.Empty;
        public string SynthesisSummary { get; set; } = string.Empty;
        public long TotalLatencyMs { get; set; } = 0;
        public double TotalCostUsd { get; set; } = 0.0;
        public List<WolfPackMemberVerdict> MemberVerdicts { get; set; } = new();
        public bool HasContradictions => ConsensusIndex < 0.65;
    }

    // [AegisAI Registry] DTO quản trị mô hình động 100% CSDL
    public class AiModelRegistryDto
    {
        public Guid Id { get; set; }
        public string ModelCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ProviderCompany { get; set; } = string.Empty;
        public ModelProtocolType ProtocolType { get; set; }
        public CognitiveArchetype CognitiveArchetype { get; set; }
        public string EndpointUrl { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public int MaxContextTokens { get; set; }
        public double CostInputPerMillion { get; set; }
        public double CostOutputPerMillion { get; set; }
        public int BenchmarkElo { get; set; }
        public int AvgLatencyMs { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsDefaultInArchetype { get; set; }
        public Guid TenantId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpsertAiModelRequest
    {
        public Guid? Id { get; set; }
        public string ModelCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ProviderCompany { get; set; } = "DeepSeek";
        public ModelProtocolType ProtocolType { get; set; } = ModelProtocolType.OpenAiCompatible;
        public CognitiveArchetype CognitiveArchetype { get; set; } = CognitiveArchetype.DeepReasoner;
        public string EndpointUrl { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public int MaxContextTokens { get; set; } = 128000;
        public double CostInputPerMillion { get; set; } = 0.14;
        public double CostOutputPerMillion { get; set; } = 0.28;
        public int BenchmarkElo { get; set; } = 1250;
        public int AvgLatencyMs { get; set; } = 400;
        public bool IsEnabled { get; set; } = true;
        public bool IsDefaultInArchetype { get; set; } = false;
    }

    // [AegisAI BYOK] DTO Quản lý Khóa cá nhân & Bể chung
    public class UserAiKeyDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string KeyLabel { get; set; } = string.Empty;
        public string ProviderCompany { get; set; } = string.Empty;
        public ModelProtocolType ProtocolType { get; set; }
        public string KeyFingerprint { get; set; } = string.Empty;
        public string? CustomEndpoint { get; set; }
        public string? CustomModelName { get; set; }
        public KeyUsageScope Scope { get; set; }
        public bool IsActive { get; set; }
        public long MonthlyTokenQuota { get; set; }
        public long TokensUsedThisMonth { get; set; }
        public long TotalCallsCount { get; set; }
        public long SuccessCallsCount { get; set; }
        public long FailedCallsCount { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SaveUserAiKeyRequest
    {
        public Guid? Id { get; set; }
        public string KeyLabel { get; set; } = string.Empty;
        public string ProviderCompany { get; set; } = "DeepSeek";
        public ModelProtocolType ProtocolType { get; set; } = ModelProtocolType.OpenAiCompatible;
        public string RawApiKey { get; set; } = string.Empty;
        public string? CustomEndpoint { get; set; }
        public string? CustomModelName { get; set; }
        public KeyUsageScope Scope { get; set; } = KeyUsageScope.Private;
        public long MonthlyTokenQuota { get; set; } = 0;
    }

    public class TestAiKeyRequest
    {
        public string ProviderCompany { get; set; } = "DeepSeek";
        public ModelProtocolType ProtocolType { get; set; } = ModelProtocolType.OpenAiCompatible;
        public string RawApiKey { get; set; } = string.Empty;
        public string? CustomEndpoint { get; set; }
        public string? CustomModelName { get; set; }
    }

    public class TestAiKeyResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long LatencyMs { get; set; }
        public string SampleOutput { get; set; } = string.Empty;
    }

    public class CommunityContributorHonorDto
    {
        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string ProviderCompany { get; set; } = string.Empty;
        public long TotalContributionsServed { get; set; }
        public long AegisCoinsAwarded { get; set; }
        public string HonorTier { get; set; } = "Silver"; // Bronze, Silver, Gold, Sovereign Diamond
    }

    /// <summary>
    /// [AegisAI Gateway Preset] Mẫu hình mô hình được phân tích ma trận năng lực & đề xuất Viện Counter
    /// </summary>
    public class GatewayModelPresetDto
    {
        public string ModelCode { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ProviderCompany { get; set; } = string.Empty;
        public string EndpointUrl { get; set; } = string.Empty;
        public ModelProtocolType ProtocolType { get; set; } = ModelProtocolType.OpenAiCompatible;
        public CognitiveArchetype CognitiveArchetype { get; set; }
        
        /// <summary>Thế mạnh nhận thức đặc trưng</summary>
        public string ContemporaryStrength { get; set; } = string.Empty;
        
        /// <summary>Vai trò đề xuất tối ưu trong Viện Hàn Lâm / Hội Đồng Tranh Biện</summary>
        public CouncilSeatRole RecommendedCouncilRole { get; set; } = CouncilSeatRole.AdversarialCritic;
        
        /// <summary>Mã các Viện Hàn Lâm đề xuất gán vào (vd: SYS_ARBITRATION, SYS_SECRETARIAT, COSMOLOGY)</summary>
        public List<string> RecommendedCouncilCodes { get; set; } = new();

        public int BenchmarkElo { get; set; } = 1300;
        public int AvgLatencyMs { get; set; } = 300;
        public int MaxContextTokens { get; set; } = 128000;
        public double CostInputPerMillion { get; set; } = 0.0;
        public double CostOutputPerMillion { get; set; } = 0.0;
        
        public bool IsFree { get; set; } = false;
        public string BadgeTag { get; set; } = "STANDARD"; // FREE, REASONING, COUNTER, FLAGSHIP
        public string Description { get; set; } = string.Empty;
    }

    public class ImportGatewayModelsRequest
    {
        public string GatewayProvider { get; set; } = "openrouter"; // "openrouter" hoặc "github"
        public List<string> SelectedModelCodes { get; set; } = new();
        public bool AutoAssignToCounterCouncils { get; set; } = true;
    }

    public class ImportGatewayModelsResult
    {
        public int ImportedModelsCount { get; set; }
        public int UpdatedModelsCount { get; set; }
        public int AssignedSeatsCount { get; set; }
        public List<string> ImportedModelNames { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}
