using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.Models.AI;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisAI Deep Research Scout - Evidence Source Plugin]
    /// Giao diện mở rộng (OCP) cho các nguồn thu thập bằng chứng thực nghiệm:
    /// Thư viện ngoại tuyến đã kiểm định, RAG từ Notebook, API học thuật mở (OpenAlex, Crossref),
    /// hoặc truy vấn thời gian thực từ mạng Internet.
    /// </summary>
    public interface IEvidenceSource
    {
        /// <summary>Tên định danh nguồn bằng chứng</summary>
        string SourceName { get; }

        /// <summary>Thứ tự ưu tiên nguồn (số càng nhỏ ưu tiên chạy trước)</summary>
        int Priority { get; }

        /// <summary>Kiểm tra nguồn có hỗ trợ đề tài hoặc điều kiện hiện tại không</summary>
        bool CanHandle(string topicTitle, string coreProblemStatement);

        /// <summary>Thu thập danh sách bằng chứng thực nghiệm cấu trúc</summary>
        Task<List<EvidenceLedgerItemDto>> GatherEvidenceAsync(
            string topicTitle,
            string coreProblemStatement,
            CancellationToken ct = default);
    }
}
