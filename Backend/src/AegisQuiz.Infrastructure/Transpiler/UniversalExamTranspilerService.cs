using System;
using System.Collections.Generic;
using System.Linq;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AegisQuiz.Infrastructure.Transpiler
{
    /// <summary>
    /// [Universal Exam Transpiler Service Engine — DTaaS]
    /// Dịch vụ điều phối chuyển dịch và xuất bản ngân hàng câu hỏi sang các định dạng chuẩn quốc tế.
    /// Tuân thủ nguyên lý OCP: nạp động toàn bộ ITranspilerExportStrategy qua DI.
    /// </summary>
    public class UniversalExamTranspilerService : IUniversalExamTranspilerService
    {
        private readonly IEnumerable<ITranspilerExportStrategy> _strategies;
        private readonly ILogger<UniversalExamTranspilerService> _logger;

        public UniversalExamTranspilerService(
            IEnumerable<ITranspilerExportStrategy> strategies,
            ILogger<UniversalExamTranspilerService> logger)
        {
            _strategies = strategies ?? Enumerable.Empty<ITranspilerExportStrategy>();
            _logger = logger;
        }

        public byte[] Transpile(IReadOnlyList<DocxImportPreviewDto> questions, string formatCode, TranspileOptionsDto? options = null)
        {
            if (string.IsNullOrWhiteSpace(formatCode))
            {
                formatCode = "MOODLE_XML";
            }

            var strategy = _strategies.FirstOrDefault(s => string.Equals(s.FormatCode, formatCode, StringComparison.OrdinalIgnoreCase));
            if (strategy == null)
            {
                _logger.LogWarning("[Transpiler] Định dạng '{Format}' không tìm thấy. Fallback về MOODLE_XML.", formatCode);
                strategy = _strategies.FirstOrDefault(s => string.Equals(s.FormatCode, "MOODLE_XML", StringComparison.OrdinalIgnoreCase));
            }

            if (strategy == null)
            {
                throw new InvalidOperationException($"Không có chiến lược chuyển dịch nào khả dụng cho mã định dạng: '{formatCode}'.");
            }

            options ??= new TranspileOptionsDto();
            _logger.LogInformation("[Transpiler] Đang chuyển dịch {Count} câu hỏi sang định dạng {Format} ({Strategy})...", 
                questions?.Count ?? 0, strategy.FormatCode, strategy.DisplayName);

            return strategy.Transpile(questions ?? new List<DocxImportPreviewDto>(), options);
        }

        public IReadOnlyList<TranspileFormatInfoDto> GetAvailableFormats()
        {
            return _strategies.Select(s => new TranspileFormatInfoDto
            {
                FormatCode = s.FormatCode,
                DisplayName = s.DisplayName,
                Description = s.Description,
                TargetPlatforms = s.TargetPlatforms,
                MimeType = s.MimeType,
                FileExtension = s.FileExtension,
                SupportsSelfContainedMedia = s.SupportsSelfContainedMedia,
                IsPackageZip = s.IsPackageZip
            }).ToList();
        }
    }
}
