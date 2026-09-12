using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [Kỳ quan 16] DomainsController — Quản lý Danh mục Ngành động & Bộ tọa độ tri thức đặc thù theo Tenant.
    /// Route base: api/quiz/domains
    /// </summary>
    [ApiController]
    [Route("api/quiz/domains")]
    public class DomainsController : ControllerBase
    {
        private readonly AegisQuizDbContext _db;
        private readonly ITenantContext _tenantContext;

        public DomainsController(AegisQuizDbContext db, ITenantContext tenantContext)
        {
            _db = db;
            _tenantContext = tenantContext;
        }

        // ── GET api/quiz/domains ───────────────────────────────────────────────
        /// <summary>
        /// Lấy danh sách ngành nghề có hiệu lực cho Tenant hiện tại.
        /// Tự động fallback về 7 ngành chuẩn nếu Tenant chưa tùy biến.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetActiveDomains()
        {
            try
            {
                var tenantId = _tenantContext.CurrentTenantId;
                
                // 1. Kiểm tra cấu hình riêng của Tenant
                var tenantConfigs = await _db.TenantDomainConfigs
                    .AsNoTracking()
                    .Where(c => c.TenantId == tenantId && c.IsEnabled)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync();

                // Lấy danh sách domain có thể dùng (chuẩn hệ thống + domain riêng của tenant)
                var availableDomains = await _db.DynamicDomains
                    .AsNoTracking()
                    .Where(d => d.IsActive && (d.IsSystemStandard || d.TenantId == tenantId))
                    .OrderBy(d => d.DisplayOrder)
                    .ToListAsync();

                if (tenantConfigs.Count > 0)
                {
                    // Map theo cấu hình đã bật của tenant
                    var configDict = tenantConfigs.ToDictionary(c => c.DomainCode, StringComparer.OrdinalIgnoreCase);
                    var result = availableDomains
                        .Where(d => configDict.ContainsKey(d.Code))
                        .Select(d =>
                        {
                            var cfg = configDict[d.Code];
                            return new
                            {
                                code = d.Code,
                                name = !string.IsNullOrWhiteSpace(cfg.CustomDisplayName) ? cfg.CustomDisplayName : d.Name,
                                description = d.Description,
                                icon = d.Icon,
                                colorBadge = d.ColorBadge,
                                isSystemStandard = d.IsSystemStandard,
                                parentDomainCode = d.ParentDomainCode,
                                displayOrder = cfg.DisplayOrder
                            };
                        })
                        .OrderBy(x => x.displayOrder)
                        .ToList();

                    if (result.Count > 0) return Ok(result);
                }

                // Fallback: Trả về danh sách mặc định hệ thống
                var fallback = availableDomains
                    .Where(d => d.IsSystemStandard)
                    .Select(d => new
                    {
                        code = d.Code,
                        name = d.Name,
                        description = d.Description,
                        icon = d.Icon,
                        colorBadge = d.ColorBadge,
                        isSystemStandard = d.IsSystemStandard,
                        parentDomainCode = d.ParentDomainCode,
                        displayOrder = d.DisplayOrder
                    })
                    .OrderBy(x => x.displayOrder)
                    .ToList();

                return Ok(fallback);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lấy danh mục ngành: {ex.Message}" });
            }
        }

        // ── GET api/quiz/domains/catalog ───────────────────────────────────────
        /// <summary>
        /// Lấy toàn bộ danh mục ngành để Quản trị viên Tenant cấu hình bật/tắt
        /// </summary>
        [HttpGet("catalog")]
        public async Task<IActionResult> GetDomainCatalog()
        {
            try
            {
                var tenantId = _tenantContext.CurrentTenantId;
                var allDomains = await _db.DynamicDomains
                    .AsNoTracking()
                    .Where(d => d.IsSystemStandard || d.TenantId == tenantId)
                    .OrderBy(d => d.DisplayOrder)
                    .ToListAsync();

                var tenantConfigs = await _db.TenantDomainConfigs
                    .AsNoTracking()
                    .Where(c => c.TenantId == tenantId)
                    .ToDictionaryAsync(c => c.DomainCode, StringComparer.OrdinalIgnoreCase);

                var catalog = allDomains.Select(d =>
                {
                    tenantConfigs.TryGetValue(d.Code, out var cfg);
                    return new
                    {
                        code = d.Code,
                        name = d.Name,
                        description = d.Description,
                        icon = d.Icon,
                        colorBadge = d.ColorBadge,
                        isSystemStandard = d.IsSystemStandard,
                        parentDomainCode = d.ParentDomainCode,
                        displayOrder = cfg?.DisplayOrder ?? d.DisplayOrder,
                        isEnabled = cfg != null ? cfg.IsEnabled : true, // Mặc định bật nếu chưa tắt
                        customDisplayName = cfg?.CustomDisplayName ?? string.Empty
                    };
                }).OrderBy(x => x.displayOrder).ToList();

                return Ok(catalog);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lấy catalog ngành: {ex.Message}" });
            }
        }

        // ── POST api/quiz/domains ──────────────────────────────────────────────
        /// <summary>
        /// Tạo mới một ngành hoặc tiểu ngành riêng cho Tenant
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCustomDomain([FromBody] CreateDomainRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.Name))
                    return BadRequest(new { message = "Mã ngành (Code) và Tên ngành (Name) là bắt buộc." });

                var cleanCode = req.Code.Trim().ToUpperInvariant();
                var tenantId = _tenantContext.CurrentTenantId;

                var existing = await _db.DynamicDomains
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Code == cleanCode);

                if (existing != null)
                    return BadRequest(new { message = $"Mã ngành '{cleanCode}' đã tồn tại trong hệ thống." });

                var newDomain = new DynamicDomain
                {
                    Code = cleanCode,
                    Name = req.Name.Trim(),
                    Description = req.Description,
                    Icon = string.IsNullOrWhiteSpace(req.Icon) ? "Layers" : req.Icon.Trim(),
                    ColorBadge = string.IsNullOrWhiteSpace(req.ColorBadge) ? "#2563eb" : req.ColorBadge.Trim(),
                    IsSystemStandard = _tenantContext.IsSystemAdmin && req.IsSystemStandard,
                    TenantId = _tenantContext.IsSystemAdmin && req.IsSystemStandard ? null : tenantId,
                    ParentDomainCode = !string.IsNullOrWhiteSpace(req.ParentDomainCode) ? req.ParentDomainCode.Trim().ToUpperInvariant() : null,
                    DisplayOrder = req.DisplayOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _db.DynamicDomains.Add(newDomain);

                // Tự động bật ngành này cho tenant
                if (tenantId != Guid.Empty)
                {
                    _db.TenantDomainConfigs.Add(new TenantDomainConfig
                    {
                        TenantId = tenantId,
                        DomainCode = cleanCode,
                        IsEnabled = true,
                        DisplayOrder = req.DisplayOrder
                    });
                }

                await _db.SaveChangesAsync();
                return Ok(new { message = "Tạo ngành thành công", domain = newDomain });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi tạo ngành mới: {ex.Message}" });
            }
        }

        // ── PUT api/quiz/domains/configs ───────────────────────────────────────
        /// <summary>
        /// Cập nhật cấu hình bật/tắt và sắp xếp danh mục ngành của Tenant
        /// </summary>
        [HttpPut("configs")]
        public async Task<IActionResult> UpdateTenantDomainConfigs([FromBody] List<UpdateDomainConfigRequest> configs)
        {
            try
            {
                var tenantId = _tenantContext.CurrentTenantId;
                if (tenantId == Guid.Empty)
                    return BadRequest(new { message = "Không xác định được Tenant ID." });

                var existingConfigs = await _db.TenantDomainConfigs
                    .Where(c => c.TenantId == tenantId)
                    .ToListAsync();

                var configMap = existingConfigs.ToDictionary(c => c.DomainCode, StringComparer.OrdinalIgnoreCase);

                foreach (var item in configs)
                {
                    var cleanCode = item.DomainCode.Trim().ToUpperInvariant();
                    if (configMap.TryGetValue(cleanCode, out var existing))
                    {
                        existing.IsEnabled = item.IsEnabled;
                        existing.CustomDisplayName = item.CustomDisplayName;
                        existing.DisplayOrder = item.DisplayOrder;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _db.TenantDomainConfigs.Add(new TenantDomainConfig
                        {
                            TenantId = tenantId,
                            DomainCode = cleanCode,
                            IsEnabled = item.IsEnabled,
                            CustomDisplayName = item.CustomDisplayName,
                            DisplayOrder = item.DisplayOrder,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _db.SaveChangesAsync();
                return Ok(new { message = "Cập nhật cấu hình ngành thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lưu cấu hình ngành: {ex.Message}" });
            }
        }

        // ── GET api/quiz/domains/presets ───────────────────────────────────────
        /// <summary>
        /// Lấy danh sách Preset tọa độ tri thức đặc thù của Tenant
        /// </summary>
        [HttpGet("presets")]
        public async Task<IActionResult> GetCoordinatePresets(
            [FromQuery] string? domainCode = null,
            [FromQuery] string? coordinateType = null)
        {
            try
            {
                var tenantId = _tenantContext.CurrentTenantId;
                var query = _db.TenantCoordinatePresets
                    .AsNoTracking()
                    .Where(p => p.TenantId == tenantId);

                if (!string.IsNullOrWhiteSpace(domainCode))
                    query = query.Where(p => p.DomainCode == domainCode.Trim().ToUpperInvariant());

                if (!string.IsNullOrWhiteSpace(coordinateType))
                    query = query.Where(p => p.CoordinateType == coordinateType.Trim().ToUpperInvariant());

                var presets = await query
                    .OrderBy(p => p.DomainCode)
                    .ThenBy(p => p.DisplayOrder)
                    .ToListAsync();

                return Ok(presets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lấy danh sách presets: {ex.Message}" });
            }
        }

        // ── POST api/quiz/domains/presets ──────────────────────────────────────
        /// <summary>
        /// Thêm một Preset tọa độ tri thức cho Tenant
        /// </summary>
        [HttpPost("presets")]
        public async Task<IActionResult> CreateCoordinatePreset([FromBody] CreatePresetRequest req)
        {
            try
            {
                var tenantId = _tenantContext.CurrentTenantId;
                if (tenantId == Guid.Empty)
                    return BadRequest(new { message = "Không xác định được Tenant ID." });

                if (string.IsNullOrWhiteSpace(req.DomainCode) || string.IsNullOrWhiteSpace(req.CoordinateType) || string.IsNullOrWhiteSpace(req.PresetLabel))
                    return BadRequest(new { message = "DomainCode, CoordinateType và PresetLabel là bắt buộc." });

                var preset = new TenantCoordinatePreset
                {
                    TenantId = tenantId,
                    DomainCode = req.DomainCode.Trim().ToUpperInvariant(),
                    CoordinateType = req.CoordinateType.Trim().ToUpperInvariant(),
                    PresetCode = string.IsNullOrWhiteSpace(req.PresetCode) ? req.PresetLabel.Trim().ToUpperInvariant().Replace(" ", "_") : req.PresetCode.Trim().ToUpperInvariant(),
                    PresetLabel = req.PresetLabel.Trim(),
                    SynonymsJson = req.SynonymsJson,
                    IsDefault = req.IsDefault,
                    DisplayOrder = req.DisplayOrder,
                    CreatedAt = DateTime.UtcNow
                };

                _db.TenantCoordinatePresets.Add(preset);
                await _db.SaveChangesAsync();

                return Ok(new { message = "Tạo preset tọa độ thành công", preset });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi tạo preset: {ex.Message}" });
            }
        }

        // ── DELETE api/quiz/domains/presets/{id} ───────────────────────────────
        [HttpDelete("presets/{id:guid}")]
        public async Task<IActionResult> DeleteCoordinatePreset(Guid id)
        {
            try
            {
                var tenantId = _tenantContext.CurrentTenantId;
                var preset = await _db.TenantCoordinatePresets
                    .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

                if (preset == null) return NotFound(new { message = "Không tìm thấy preset tọa độ." });

                _db.TenantCoordinatePresets.Remove(preset);
                await _db.SaveChangesAsync();

                return Ok(new { message = "Xóa preset tọa độ thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi xóa preset: {ex.Message}" });
            }
        }
    }

    public class CreateDomainRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? ColorBadge { get; set; }
        public bool IsSystemStandard { get; set; } = false;
        public string? ParentDomainCode { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateDomainConfigRequest
    {
        public string DomainCode { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string? CustomDisplayName { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }

    public class CreatePresetRequest
    {
        public string DomainCode { get; set; } = string.Empty;
        public string CoordinateType { get; set; } = string.Empty;
        public string? PresetCode { get; set; }
        public string PresetLabel { get; set; } = string.Empty;
        public string? SynonymsJson { get; set; }
        public bool IsDefault { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
    }
}
