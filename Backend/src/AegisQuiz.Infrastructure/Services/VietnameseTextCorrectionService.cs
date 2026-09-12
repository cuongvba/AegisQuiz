using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using AegisQuiz.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisQuiz.Infrastructure.Services
{
    public class VietnameseTextCorrectionService : IVietnameseTextCorrectionService
    {
        private readonly ILogger<VietnameseTextCorrectionService> _logger;
        private readonly List<CompiledReplacementRule> _compiledRules = new();

        private static readonly Regex PositionalRefRegex = new Regex(
            @"(cả\s+(\(?\d+\)?|[a-d]|hai|ba|bốn)\s+(và|hoặc)|tất\s+cả\s+(các\s+)?(đáp|phương)\s+án|không\s+(có\s+)?(đáp|phương)\s+án\s+nào\s+đúng|đều\s+(đúng|sai)|phương\s+án\s+trên|đáp\s+án\s+trên|ý\s+(\(?\d+\)?|[a-d])\s+và|\([1-4]\)\s*(và|hoặc|,)\s*\([1-4]\)|\b([1-4]|[a-d]|i{1,4})\s*(,|và|hoặc)\s*([1-4]|[a-d]|i{1,4})\b)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(200));

        private static readonly Regex AnchorKeywordsRegex = new Regex(
            @"^(tất\s+cả|không\s+có\s+(đáp|phương)\s+án|cả\s+(\(?\d+\)?|[a-d]|hai|ba|bốn)|cả\s+\([1-4]\)|\([1-4]\)\s*(và|hoặc|,)\s*\([1-4]\)|các\s+(đáp|phương)\s+án\s+trên\s+đều|\b([1-4]|[a-d]|i{1,4})\s*(,|và|hoặc)\s*([1-4]|[a-d]|i{1,4})\b)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(200));

        public VietnameseTextCorrectionService(
            IConfiguration configuration,
            ILogger<VietnameseTextCorrectionService> logger)
        {
            _logger = logger;
            LoadMapreplDatabase(configuration);
        }

        private void LoadMapreplDatabase(IConfiguration configuration)
        {
            try
            {
                var searchPaths = new List<string?>
                {
                    configuration["Correction:MapreplPath"],
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "maprepl.db"),
                    Path.Combine(Directory.GetCurrentDirectory(), "maprepl.db"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "maprepl.db"),
                    @"D:\Cuong\DuAn\mybank\AegisQuiz\maprepl.db"
                };

                string? foundPath = searchPaths.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p));

                if (foundPath == null)
                {
                    _logger.LogWarning("[VietnameseTextCorrectionService] Không tìm thấy tệp maprepl.db trong các đường dẫn tiêu chuẩn.");
                    return;
                }

                var json = File.ReadAllText(foundPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var rules = JsonSerializer.Deserialize<List<MapreplRuleDto>>(json, options);

                if (rules == null || rules.Count == 0)
                {
                    _logger.LogWarning("[VietnameseTextCorrectionService] Tệp maprepl.db rỗng hoặc không đúng định dạng.");
                    return;
                }

                int compiledCount = 0;
                foreach (var r in rules)
                {
                    if (string.IsNullOrWhiteSpace(r.Pattern)) continue;
                    try
                    {
                        var regexOpts = (r.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None) | RegexOptions.Compiled;
                        var rx = new Regex(r.Pattern, regexOpts, TimeSpan.FromMilliseconds(200));
                        _compiledRules.Add(new CompiledReplacementRule
                        {
                            Regex = rx,
                            Replacement = r.ToStr ?? string.Empty
                        });
                        compiledCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("[VietnameseTextCorrectionService] Bỏ qua quy tắc không hợp lệ '{Pattern}': {Message}", r.Pattern, ex.Message);
                    }
                }

                _logger.LogInformation("[VietnameseTextCorrectionService] Đã nạp và biên dịch thành công {Count} quy tắc từ {Path}", compiledCount, foundPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[VietnameseTextCorrectionService] Lỗi khi nạp cơ sở tri thức maprepl.db");
            }
        }

        public string CorrectText(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            string result = input;
            foreach (var rule in _compiledRules)
            {
                try
                {
                    result = rule.Regex.Replace(result, rule.Replacement);
                }
                catch (RegexMatchTimeoutException)
                {
                    // Tránh nghẽn thread nếu gặp chuỗi quá dài hoặc regex phức tạp
                }
            }

            return result;
        }

        public DocxImportPreviewDto CorrectQuestionDto(DocxImportPreviewDto dto)
        {
            if (dto == null) return dto!;

            dto.Content = CorrectText(dto.Content);

            if (dto.Options != null && dto.Options.Count > 0)
            {
                for (int i = 0; i < dto.Options.Count; i++)
                {
                    var corrected = CorrectText(dto.Options[i]);
                    dto.Options[i] = SmartOptionShufflerService.TranslateAnchorOption(corrected);
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.AiExplanation))
            {
                dto.AiExplanation = CorrectText(dto.AiExplanation);
            }

            if (!string.IsNullOrWhiteSpace(dto.ContextContent))
            {
                dto.ContextContent = CorrectText(dto.ContextContent);
            }

            return dto;
        }

        public List<DocxImportPreviewDto> CorrectQuestionsBatch(List<DocxImportPreviewDto> dtos)
        {
            if (dtos == null || dtos.Count == 0) return dtos ?? new();

            foreach (var item in dtos)
            {
                CorrectQuestionDto(item);
            }

            return dtos;
        }

        public bool ContainsPositionalReference(string optionText)
        {
            if (string.IsNullOrWhiteSpace(optionText)) return false;
            var t = SmartOptionShufflerService.StripOptionPrefix(optionText).Trim();
            try
            {
                return PositionalRefRegex.IsMatch(t) || IsAnchorOption(t);
            }
            catch
            {
                return false;
            }
        }

        public bool IsAnchorOption(string optionText)
        {
            if (string.IsNullOrWhiteSpace(optionText)) return false;
            var t = SmartOptionShufflerService.StripOptionPrefix(optionText).Trim();
            try
            {
                return SmartOptionShufflerService.IsAnchorPhrase(t) ||
                       AnchorKeywordsRegex.IsMatch(t) ||
                       t.StartsWith("Tất cả các phương án", StringComparison.OrdinalIgnoreCase) ||
                       t.StartsWith("Không có phương án nào", StringComparison.OrdinalIgnoreCase) ||
                       t.StartsWith("Cả (", StringComparison.OrdinalIgnoreCase) ||
                       t.StartsWith("Chỉ có (", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private class MapreplRuleDto
        {
            public string Pattern { get; set; } = string.Empty;
            public string ToStr { get; set; } = string.Empty;
            public bool IgnoreCase { get; set; }
        }

        private class CompiledReplacementRule
        {
            public Regex Regex { get; set; } = null!;
            public string Replacement { get; set; } = string.Empty;
        }
    }
}
