using System;
using System.Collections.Generic;
using System.Linq;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Excel.Strategies;

namespace AegisQuiz.Infrastructure.Excel
{
    /// <summary>
    /// [Universal Excel Export Engine — DTaaS Coordinator]
    /// Dịch vụ điều phối xuất bản bảng tính Excel đa mẫu theo chuẩn OCP:
    /// Tự động định tuyến sang chiến lược xuất bản tương ứng (Native Isomorphic, GDPT 2025, Azota, Custom BYOT...).
    /// </summary>
    public class UniversalExcelExportService : IUniversalExcelExportService
    {
        private readonly Dictionary<string, IExcelExportStrategy> _strategies;

        public UniversalExcelExportService(IEnumerable<IExcelExportStrategy>? strategies = null)
        {
            _strategies = new Dictionary<string, IExcelExportStrategy>(StringComparer.OrdinalIgnoreCase);

            if (strategies != null)
            {
                foreach (var s in strategies)
                {
                    _strategies[s.TemplateCode] = s;
                }
            }

            // Đảm bảo luôn có chiến lược Native Isomorphic cốt lõi làm mỏ neo dự phòng
            if (!_strategies.ContainsKey("NATIVE_AEGIS"))
            {
                var fallback = new NativeIsomorphicExportStrategy();
                _strategies[fallback.TemplateCode] = fallback;
            }

            if (!_strategies.ContainsKey("CUSTOM_BYOT"))
            {
                var custom = new CustomTemplateExportStrategy();
                _strategies[custom.TemplateCode] = custom;
            }
        }

        public byte[] Export(IReadOnlyList<DocxImportPreviewDto> questions, string templateCode = "NATIVE_AEGIS", ExcelExportOptionsDto? options = null)
        {
            if (questions == null || questions.Count == 0)
            {
                throw new ArgumentException("Danh sách câu hỏi xuất bản không được rỗng.", nameof(questions));
            }

            options ??= new ExcelExportOptionsDto();

            if (!_strategies.TryGetValue(templateCode, out var strategy))
            {
                // Fallback về Native Isomorphic nếu mã template không tồn tại
                strategy = _strategies["NATIVE_AEGIS"];
            }

            return strategy.Export(questions, options);
        }

        public IReadOnlyList<ExcelExportTemplateInfoDto> GetAvailableTemplates()
        {
            return _strategies.Values.Select(s => new ExcelExportTemplateInfoDto
            {
                TemplateCode = s.TemplateCode,
                DisplayName = s.DisplayName,
                Description = s.Description,
                SupportsMultiSheet = s.SupportsMultiSheet,
                IsNativeIsomorphic = s.IsNativeIsomorphic
            }).ToList();
        }
    }
}
