using System;
using System.Collections.Generic;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Models.AI
{
    /// <summary>
    /// [AegisAI Sovereign] Ngữ cảnh Vết Suy Luận (Thinking-Trace Context)
    /// Đóng vai trò là Ephemeral Working Memory bảo toàn chuỗi nhận thức (Chain of Thought / CoT)
    /// khi xảy ra xoay khóa (Key Rotation) hoặc chuyển vùng mô hình (Cross-Archetype Fallback).
    /// </summary>
    public class ThinkingTraceContext
    {
        public string TraceId { get; set; } = Guid.NewGuid().ToString("N");
        public string SessionId { get; set; } = string.Empty;
        public string OriginalTaskPrompt { get; set; } = string.Empty;
        public string OriginalSystemPrompt { get; set; } = string.Empty;
        public CognitiveArchetype OriginArchetype { get; set; } = CognitiveArchetype.DeepReasoner;
        public string OriginModelCode { get; set; } = string.Empty;

        /// <summary>
        /// Toàn bộ dòng tư duy trung gian (CoT / Scratchpad) đã tích lũy trước thời điểm xoay khóa
        /// </summary>
        public string AccumulatedScratchpad { get; set; } = string.Empty;

        /// <summary>
        /// Các giả thuyết và tiền đề đã được chứng minh hoặc bác bỏ
        /// </summary>
        public List<string> VerifiedHypotheses { get; set; } = new();

        /// <summary>
        /// Danh sách tài liệu / trích dẫn đã được kiểm tra tính hợp lệ
        /// </summary>
        public List<string> ExaminedCitations { get; set; } = new();

        /// <summary>
        /// Lịch sử các lần xoay khóa trong phiên làm việc để phục vụ FinOps Audit
        /// </summary>
        public List<KeyFailoverEvent> RotationHistory { get; set; } = new();

        public int CompletedSteps { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Ghi nhận sự kiện xoay khóa trong quá trình thực thi nhận thức
    /// </summary>
    public class KeyFailoverEvent
    {
        public string FailedKeySource { get; set; } = string.Empty;
        public string FailedProviderOrModel { get; set; } = string.Empty;
        public string ResolvedKeySource { get; set; } = string.Empty;
        public string ResolvedProviderOrModel { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty; // 429, QuotaExhausted, ConnectionTimeout, etc.
        public bool PreservedThinkingTrace { get; set; } = true;
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Ứng viên khóa được phân giải trong chuỗi Thác lũ (Waterfall)
    /// </summary>
    public class ResolvedKeyCandidate
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string KeySource { get; set; } = string.Empty; // 'UserBYOK', 'TenantBYOK', 'CommunityPool', 'SystemDefault'
        public Guid? KeyEntityId { get; set; }
        public int Priority { get; set; }
    }
}
