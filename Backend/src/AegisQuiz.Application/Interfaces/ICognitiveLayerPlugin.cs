using System.Threading;
using System.Threading.Tasks;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Extensible Epistemic Stack Plugin (OCP)]
    /// Hợp đồng cắm thêm vô hạn các tầng nhận thức mới (L1..Ln) vào hệ thống
    /// mà không cần sửa đổi mã nguồn lõi hay biên dịch lại Engine.
    /// </summary>
    public interface ICognitiveLayerPlugin
    {
        /// <summary>Định danh duy nhất của tầng (vd: "BAYESIAN_BELIEF", "ABDUCTIVE_GENESIS", "FORMAL_FALSIFIABILITY")</summary>
        string LayerKey { get; }

        /// <summary>Thứ tự xếp lớp trong chuỗi Pipeline nhận thức (vd: 10, 20, 30...)</summary>
        int LayerOrder { get; }

        /// <summary>Điều kiện kích hoạt tầng: Kiểm tra xem đề tài hoặc pha hiện tại có cần tầng này không</summary>
        bool IsApplicable(PromptCompositionContext context);

        /// <summary>Thực thi chèn thêm chỉ thị nhận thức, hợp đồng hoặc quy tắc tư duy vào Stack</summary>
        Task<CognitiveLayerContribution> ProcessLayerAsync(PromptCompositionContext context, CancellationToken ct = default);
    }

    public class CognitiveLayerContribution
    {
        public string LayerKey { get; set; } = string.Empty;
        public string HeaderText { get; set; } = string.Empty;
        public string DirectiveText { get; set; } = string.Empty;
        public bool IsMandatory { get; set; } = true;
    }
}
