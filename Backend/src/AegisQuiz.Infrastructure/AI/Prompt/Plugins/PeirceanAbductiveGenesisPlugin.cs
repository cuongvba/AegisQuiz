using System;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.AI.Prompt.Plugins
{
    /// <summary>
    /// [Evolutionary Layer: Peircean Abductive Leap & Conceptual Blending]
    /// Bánh đà Phát kiến Tri thức mới: Hướng dẫn mô hình không chỉ diễn dịch an toàn,
    /// mà thực hiện phép quy nạp giả định (Abduction) để sinh ra tiên đề chưa từng có trong lịch sử nhân loại.
    /// </summary>
    public class PeirceanAbductiveGenesisPlugin : ICognitiveLayerPlugin
    {
        public string LayerKey => "PEIRCEAN_ABDUCTIVE_GENESIS";
        public int LayerOrder => 70;

        public bool IsApplicable(PromptCompositionContext context)
        {
            var phase = context.PhaseCode?.ToUpperInvariant();
            // Áp dụng chủ yếu cho Pha Đặt vấn đề (Formulation) và Pha Hợp nhất Siêu lý thuyết (Synthesis)
            return phase == "FORMULATION" || phase == "SYNTHESIS";
        }

        public Task<CognitiveLayerContribution> ProcessLayerAsync(PromptCompositionContext context, CancellationToken ct = default)
        {
            bool isEn = context.Locale?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true;

            string header = isEn
                ? "=== [EVOLUTIONARY LAYER: PEIRCEAN ABDUCTIVE LEAP & CONCEPTUAL BLENDING] ==="
                : "=== [TẦNG TIẾN HÓA NHẬN THỨC: NHẢY VỌT QUY NẠP GIẢ ĐỊNH & HỢP NHẤT KHÁI NIỆM MỚI] ===";

            string directive = isEn
                ? "1. Transcend mere deductive rephrasing: If existing paradigms lead to an unresolved antinomy, perform an ABDUCTIVE LEAP to propose an entirely novel axiom.\n" +
                  "2. Practice Cross-Disciplinary Conceptual Blending: Merge structural topologies from disparate fields (e.g. quantum topology with biological morphogenesis) to unlock breakthrough insights.\n" +
                  "3. Formulate any newly discovered principle as a testable invariant: State clearly what novel phenomenon your new axiom predicts."
                : "1. Vượt thoát khỏi diễn dịch lặp lại an toàn: Nếu hệ hình hiện tại dẫn đến bế tắc bất khả hòa giải, hãy thực hiện PHÉP NHẢY VỌT QUY NẠP GIẢ ĐỊNH (Peircean Abduction) để đề xuất một tiên đề hoàn toàn mới.\n" +
                  "2. Hợp nhất khái niệm xuyên ngành (Conceptual Blending): Kết hợp các hình thái cấu trúc từ các miền tri thức đối nghịch (ví dụ: topo lượng tử với hình thái sinh học) để mở khóa phát kiến đột phá.\n" +
                  "3. Đúc kết nguyên lý mới thành một định đề kiểm chứng được: Nêu rõ tiên đề mới của bạn dự đoán hiện tượng mới nào chưa từng được giải thích.";

            return Task.FromResult(new CognitiveLayerContribution
            {
                LayerKey = LayerKey,
                HeaderText = header,
                DirectiveText = directive,
                IsMandatory = false
            });
        }
    }
}
