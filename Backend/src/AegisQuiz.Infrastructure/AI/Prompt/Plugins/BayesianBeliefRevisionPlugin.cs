using System;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.AI.Prompt.Plugins
{
    /// <summary>
    /// [Evolutionary Layer: Bayesian Belief Dynamics]
    /// Tầng điều phối động học niềm tin: Ép buộc các mô hình AI phải cập nhật phân phối xác suất
    /// niềm tin khi đối diện với phản chứng hoặc dữ liệu thực nghiệm mới P(Axiom|Evidence).
    /// </summary>
    public class BayesianBeliefRevisionPlugin : ICognitiveLayerPlugin
    {
        public string LayerKey => "BAYESIAN_BELIEF_DYNAMICS";
        public int LayerOrder => 60; // Nằm sau OutputContract chuẩn

        public bool IsApplicable(PromptCompositionContext context)
        {
            var phase = context.PhaseCode?.ToUpperInvariant();
            // Áp dụng cho các pha đối chất, phản biện và hợp nhất
            return phase == "CROSS_EXAMINATION" || phase == "CRITIQUE" || phase == "SYNTHESIS";
        }

        public Task<CognitiveLayerContribution> ProcessLayerAsync(PromptCompositionContext context, CancellationToken ct = default)
        {
            bool isEn = context.Locale?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;

            string header = isEn
                ? "=== [EPISTEMIC LAYER: BAYESIAN BELIEF UPDATING & COGNITIVE FLEXIBILITY] ==="
                : "=== [TẦNG TIẾN HÓA NHẬN THỨC: ĐỘNG HỌC NIỀM TIN BAYESIAN & SỰ THÍCH ỨNG CHÂN LÝ] ===";

            string directive = isEn
                ? "1. You are strictly forbidden from maintaining unyielding dogmatism if confronted with credible empirical evidence [E#].\n" +
                  "2. Quantify your subjective confidence P(Hypothesis | Evidence) from 0.0 to 1.0 when asserting or conceding a point.\n" +
                  "3. If an adversary exposes a fatal vulnerability in your premise, you MUST explicitly acknowledge the cognitive dissonance and shift your probability distribution toward the emerging truth."
                : "1. Nghiêm cấm bảo thủ giáo điều nếu đối phương đưa ra bằng chứng thực nghiệm hợp lệ [E#].\n" +
                  "2. Lượng hóa độ tin cậy chủ quan P(Giả thuyết | Thực chứng) từ 0.0 đến 1.0 trong lập luận của bạn.\n" +
                  "3. Nếu đối phương vạch trần lỗ hổng tử huyệt trong tiền đề của bạn, bạn BẮT BUỘC phải công khai thừa nhận sự khủng hoảng nhận thức và chuyển dịch phân phối xác suất niềm tin hướng về chân lý.";

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
