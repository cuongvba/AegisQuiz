using System;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.AI.Prompt.Plugins
{
    /// <summary>
    /// [Evolutionary Layer: Formal Falsifiability & Inviolable Truth Verification]
    /// Bánh đà Khẳng định Chân lý: Thắt chặt kỷ luật phản nghiệm Popper và kiểm định logic hình thức,
    /// bảo đảm mọi kết luận được bảo chứng không có lỗ hổng ngụy biện.
    /// </summary>
    public class FormalFalsifiabilityPlugin : ICognitiveLayerPlugin
    {
        public string LayerKey => "FORMAL_FALSIFIABILITY_VERIFICATION";
        public int LayerOrder => 80;

        public bool IsApplicable(PromptCompositionContext context)
        {
            var phase = context.PhaseCode?.ToUpperInvariant();
            // Áp dụng cho Pha Phản biện đối kháng (Critique) và Pha Chấm điểm thẩm định (Score)
            return phase == "CRITIQUE" || phase == "SCORE_ARBITRATION";
        }

        public Task<CognitiveLayerContribution> ProcessLayerAsync(PromptCompositionContext context, CancellationToken ct = default)
        {
            bool isEn = context.Locale?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;

            string header = isEn
                ? "=== [TRUTH VERIFICATION LAYER: FORMAL FALSIFIABILITY & CRITICAL RATIONALE] ==="
                : "=== [TẦNG KHẲNG ĐỊNH CHÂN LÝ: PHẢN NGHIỆM POPPER & KIỂM CHỨNG HÌNH THỨC] ===";

            string directive = isEn
                ? "1. State the exact falsifiability threshold: Under what precise experimental observation, counter-example, or mathematical singularity would your claim be falsified?\n" +
                  "2. Detect Hidden Equivocation & Scope Creep: Highlight any subtle semantic shifts or illicit extensions beyond legitimate empirical boundaries.\n" +
                  "3. Strive for Invariable Truth: Eliminate contingent speculation and converge solely upon demonstrated invariants."
                : "1. Nêu rõ ngưỡng phản nghiệm: Mệnh đề của bạn sẽ bị chứng minh là SAI trong điều kiện quan sát thực nghiệm, phản ví dụ, hoặc điểm kỳ dị toán học chính xác nào?\n" +
                  "2. Phát hiện mập mờ ngữ nghĩa và lạm dụng ngoại suy: Vạch trần mọi sự đánh tráo khái niệm hoặc mở rộng phạm vi tùy tiện vượt qua giới hạn biên thực nghiệm.\n" +
                  "3. Hướng đến chân lý bất biến: Loại bỏ hoàn toàn suy đoán ngẫu nhĩ, chỉ kết tinh các định đề đã vượt qua mọi phép thử phản nghiệm khắt khe nhất.";

            return Task.FromResult(new CognitiveLayerContribution
            {
                LayerKey = LayerKey,
                HeaderText = header,
                DirectiveText = directive,
                IsMandatory = true
            });
        }
    }
}
