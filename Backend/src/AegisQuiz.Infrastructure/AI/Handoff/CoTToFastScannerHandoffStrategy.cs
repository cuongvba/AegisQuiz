using System;
using System.Text.RegularExpressions;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Infrastructure.AI.Handoff
{
    /// <summary>
    /// [AegisAI Sovereign - OCP]
    /// Chiến lược Chuyển giao Nhận thức từ Mô hình Suy Luận Sâu (DeepReasoner)
    /// sang Mô hình Siêu Tốc (UltraFastScanner / MassiveContext).
    /// Giúp mô hình mới không bao giờ bị "ngơ ngẩn" do mất chuỗi CoT,
    /// mà hoạt động như một Trọng Tài Tổng Hợp thừa kế toàn bộ giả thuyết đã được chứng minh.
    /// </summary>
    public class CoTToFastScannerHandoffStrategy : ICognitiveHandoffStrategy
    {
        public int Priority => 100;

        public bool CanHandle(CognitiveArchetype fromArchetype, CognitiveArchetype toArchetype)
        {
            return fromArchetype == CognitiveArchetype.DeepReasoner &&
                   (toArchetype == CognitiveArchetype.UltraFastScanner || 
                    toArchetype == CognitiveArchetype.MassiveContext ||
                    toArchetype == CognitiveArchetype.LinguisticArbiter);
        }

        public (string AdaptedSystemPrompt, string AdaptedUserPrompt) AdaptContext(
            ThinkingTraceContext trace, 
            CognitiveArchetype targetArchetype, 
            string baseSystemPrompt, 
            string baseUserPrompt)
        {
            // Trích xuất các thẻ suy luận trung gian <think>...</think> nếu có trong Scratchpad
            var scratchpad = trace.AccumulatedScratchpad;
            if (string.IsNullOrWhiteSpace(scratchpad))
            {
                return (baseSystemPrompt, baseUserPrompt);
            }

            var cleanThinking = Regex.Replace(scratchpad, @"<\/?think>", "").Trim();
            if (cleanThinking.Length > 3000)
            {
                // Cắt lọc giữ lại phần suy luận then chốt gần nhất
                cleanThinking = "..." + cleanThinking[^3000..];
            }

            var adaptedSystem = $@"{baseSystemPrompt}

=== [GIAO THỨC CHUYỂN GIAO NHẬN THỨC BẢO TOÀN COT (COGNITIVE HANDOFF PROTOCOL)] ===
Bạn đang tiếp quản một phiên làm việc tư duy đa bước. Chuyên gia phân tích suy luận sâu tiền nhiệm ({trace.OriginModelCode}) đã phân tích vấn đề và để lại chuỗi tư duy trung gian sau:
---
[VẾT TƯ DUY TRUNG GIAN ĐÃ XÁC LẬP]:
{cleanThinking}
---
QUY TẮC TIẾP QUẢN TUYỆT ĐỐI:
1. KHÔNG bắt đầu phân tích lại từ con số không (tránh mâu thuẫn nhận thức).
2. Hãy coi các tiền đề và lập luận hợp lý phía trên là căn cứ đã được kiểm chứng.
3. Tập trung giải quyết các bước còn lại và đưa ra KẾT LUẬN / ĐÁP ÁN CUỐI CÙNG theo đúng định dạng được yêu cầu.";

            var adaptedUser = $@"{baseUserPrompt}

[LƯU Ý ĐIỀU ĐỘNG]: Tiếp tục từ chuỗi tư duy đã chuyển giao ở trên và xuất kết quả chính thức.";

            return (adaptedSystem, adaptedUser);
        }
    }
}
