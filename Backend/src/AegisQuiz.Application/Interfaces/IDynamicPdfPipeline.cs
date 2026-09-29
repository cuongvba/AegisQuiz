using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [Clear Dynamic Architecture] Hợp đồng Pipeline bóc tách đề thi PDF đa tầng độc lập.
    /// Hỗ trợ cả PDF Vector có cấu trúc (1 cột hoặc 2 cột), PDF Scan dạng ảnh (Multimodal Image Ingestion),
    /// và Hybrid PDF có đồ thị hàm số, hình học không gian, biển báo và sa hình.
    /// </summary>
    public interface IDynamicPdfPipeline
    {
        /// <summary>
        /// Thực thi đồng bộ toàn bộ pipeline 5 tầng bóc tách đề thi từ luồng PDF.
        /// </summary>
        List<DocxImportPreviewDto> Execute(Stream pdfStream, string? specificRuleCode = null);

        /// <summary>
        /// Thực thi bất đồng bộ pipeline bóc tách đề thi PDF.
        /// </summary>
        Task<List<DocxImportPreviewDto>> ExecuteAsync(Stream pdfStream, string? specificRuleCode = null, CancellationToken ct = default);

        /// <summary>
        /// Trích xuất toàn văn sạch có tái cấu trúc dòng đọc không gian (Spatial Layout Reconstruction).
        /// </summary>
        string ExtractPlainText(Stream pdfStream);
    }
}
