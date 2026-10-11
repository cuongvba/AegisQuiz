using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.Infrastructure.Services
{
    public class DynamicDomainContextService : IDynamicDomainContextService
    {
        private readonly AegisQuizDbContext _db;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<DynamicDomainContextService>? _logger;

        public DynamicDomainContextService(
            AegisQuizDbContext db,
            ITenantContext tenantContext,
            ILogger<DynamicDomainContextService>? logger = null)
        {
            _db = db;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<DynamicDomainContext> BuildContextAsync(Guid? tenantId = null, CancellationToken ct = default)
        {
            var effectiveTenantId = tenantId ?? _tenantContext.CurrentTenantId;
            var context = DynamicDomainContext.CreateDefault();
            context.TenantId = effectiveTenantId;

            try
            {
                // 1. Nạp danh mục Domains (ưu tiên domain của tenant + system standard)
                var availableDomains = await _db.DynamicDomains
                    .AsNoTracking()
                    .Where(d => d.IsActive && (d.IsSystemStandard || d.TenantId == effectiveTenantId))
                    .OrderBy(d => d.DisplayOrder)
                    .ToListAsync(ct);

                if (availableDomains.Count > 0)
                {
                    // Lấy cấu hình bật/tắt của tenant
                    var tenantConfigs = await _db.TenantDomainConfigs
                        .AsNoTracking()
                        .Where(c => c.TenantId == effectiveTenantId)
                        .ToDictionaryAsync(c => c.DomainCode, StringComparer.OrdinalIgnoreCase, ct);

                    var domainDtos = new List<DynamicDomainSummaryDto>();
                    foreach (var d in availableDomains)
                    {
                        if (tenantConfigs.TryGetValue(d.Code, out var cfg) && !cfg.IsEnabled)
                        {
                            // Đã bị tắt bởi tenant
                            continue;
                        }

                        var displayName = (tenantConfigs.TryGetValue(d.Code, out var c) && !string.IsNullOrWhiteSpace(c.CustomDisplayName))
                            ? c.CustomDisplayName
                            : d.Name;

                        // Tìm từ khóa / từ đồng nghĩa mặc định nếu có trong fallback
                        var defaultFallback = context.Domains.FirstOrDefault(fd => fd.Code.Equals(d.Code, StringComparison.OrdinalIgnoreCase));
                        var synonyms = defaultFallback?.Synonyms != null ? new List<string>(defaultFallback.Synonyms) : new List<string>();
                        if (!synonyms.Any(s => s.Equals(displayName, StringComparison.OrdinalIgnoreCase)))
                        {
                            synonyms.Add(displayName.ToLowerInvariant());
                        }

                        domainDtos.Add(new DynamicDomainSummaryDto
                        {
                            Code = d.Code,
                            Name = displayName,
                            Description = d.Description,
                            Icon = d.Icon,
                            ColorBadge = d.ColorBadge,
                            IsSystemStandard = d.IsSystemStandard,
                            ParentDomainCode = d.ParentDomainCode,
                            Synonyms = synonyms
                        });
                    }

                    if (domainDtos.Count > 0)
                    {
                        context.Domains = domainDtos;
                    }
                }

                // 2. Nạp TenantCoordinatePresets từ DB
                var dbPresets = await _db.TenantCoordinatePresets
                    .AsNoTracking()
                    .Where(p => p.TenantId == effectiveTenantId)
                    .OrderBy(p => p.DisplayOrder)
                    .ToListAsync(ct);

                if (dbPresets.Count > 0)
                {
                    var presetDtos = new List<TenantCoordinatePresetDto>();
                    foreach (var p in dbPresets)
                    {
                        var syns = new List<string>();
                        if (!string.IsNullOrWhiteSpace(p.SynonymsJson))
                        {
                            try
                            {
                                var parsed = JsonSerializer.Deserialize<List<string>>(p.SynonymsJson);
                                if (parsed != null) syns.AddRange(parsed);
                            }
                            catch
                            {
                                // Bỏ qua nếu json lỗi
                            }
                        }
                        if (!syns.Contains(p.PresetLabel, StringComparer.OrdinalIgnoreCase))
                        {
                            syns.Add(p.PresetLabel);
                        }

                        presetDtos.Add(new TenantCoordinatePresetDto
                        {
                            Id = p.Id,
                            DomainCode = p.DomainCode,
                            CoordinateType = p.CoordinateType,
                            PresetCode = p.PresetCode,
                            PresetLabel = p.PresetLabel,
                            Synonyms = syns,
                            IsDefault = p.IsDefault,
                            DisplayOrder = p.DisplayOrder
                        });
                    }

                    // Hợp nhất preset của tenant với default fallback (tenant preset ghi đè fallback)
                    context.CoordinatePresets.InsertRange(0, presetDtos);
                }

                // 3. Nạp BankTopics từ DB
                var topics = await _db.BankTopics
                    .AsNoTracking()
                    .Where(t => t.TenantId == effectiveTenantId || t.TenantId == Guid.Empty)
                    .OrderBy(t => t.Name)
                    .ToListAsync(ct);

                if (topics.Count > 0)
                {
                    context.Topics = topics.Select(t =>
                    {
                        var kw = new List<string> { t.Name.ToLowerInvariant(), t.Code.ToLowerInvariant() };
                        if (!string.IsNullOrWhiteSpace(t.Description))
                        {
                            var parts = t.Description.Split(new[] { ',', ';', '\n', '|' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var p in parts)
                            {
                                var clean = p.Trim().ToLowerInvariant();
                                if (clean.Length > 2 && !kw.Contains(clean)) kw.Add(clean);
                            }
                        }

                        return new BankTopicSummaryDto
                        {
                            Code = t.Code,
                            Name = t.Name,
                            Description = t.Description,
                            Keywords = kw
                        };
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[DynamicDomainContextService] Lỗi nạp ngữ cảnh động từ DB cho Tenant {TenantId}. Dùng context mặc định.", effectiveTenantId);
            }

            return context;
        }
    }
}
