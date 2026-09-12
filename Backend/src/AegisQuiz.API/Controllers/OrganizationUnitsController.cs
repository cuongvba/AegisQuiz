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
    /// [Kỳ quan 16] OrganizationUnitsController — Quản lý Cây phân cấp tổ chức đệ quy mềm (5-Tier Hierarchy Governance).
    /// Hỗ trợ cấu trúc đa tầng: Hội sở (HQ) -> Vùng/Miền -> Chi nhánh -> Phòng ban -> Lớp học.
    /// Route base: api/quiz/org-units
    /// </summary>
    [ApiController]
    [Route("api/quiz/org-units")]
    public class OrganizationUnitsController : ControllerBase
    {
        private readonly AegisQuizDbContext _db;
        private readonly ITenantContext _tenantContext;

        public OrganizationUnitsController(AegisQuizDbContext db, ITenantContext tenantContext)
        {
            _db = db;
            _tenantContext = tenantContext;
        }

        // ── GET api/quiz/org-units/tree ────────────────────────────────────────
        /// <summary>
        /// Lấy toàn bộ cây tổ chức phân cấp dạng lồng nhau (Tree Hierarchy)
        /// </summary>
        [HttpGet("tree")]
        public async Task<IActionResult> GetOrganizationTree()
        {
            try
            {
                var tenantId = _tenantContext.CurrentTenantId;
                var allUnits = await _db.OrganizationUnits
                    .AsNoTracking()
                    .Where(o => o.TenantId == tenantId && o.IsActive)
                    .OrderBy(o => o.DisplayOrder)
                    .ThenBy(o => o.Name)
                    .ToListAsync();

                // Dựng cây lồng nhau
                var unitDtos = allUnits.Select(u => new OrgUnitDto
                {
                    Id = u.Id,
                    TenantId = u.TenantId,
                    ParentId = u.ParentId,
                    Code = u.Code,
                    Name = u.Name,
                    UnitType = u.UnitType.ToString(),
                    HierarchyPath = u.HierarchyPath,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Address = u.Address,
                    DisplayOrder = u.DisplayOrder,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                }).ToList();

                var lookup = unitDtos.ToLookup(u => u.ParentId);
                foreach (var unit in unitDtos)
                {
                    unit.Children = lookup[unit.Id].ToList();
                }

                // Root nodes là những node không có cha hoặc cha không thuộc tập này
                var roots = unitDtos.Where(u => u.ParentId == null || !unitDtos.Any(x => x.Id == u.ParentId)).ToList();
                return Ok(roots);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lấy cây tổ chức: {ex.Message}" });
            }
        }

        // ── GET api/quiz/org-units ─────────────────────────────────────────────
        /// <summary>
        /// Lấy danh sách phẳng tất cả đơn vị trực thuộc Tenant (dùng cho Dropdown/Selector)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFlatUnits([FromQuery] OrgUnitType? unitType = null)
        {
            try
            {
                var tenantId = _tenantContext.CurrentTenantId;
                var q = _db.OrganizationUnits
                    .AsNoTracking()
                    .Where(o => o.TenantId == tenantId && o.IsActive);

                if (unitType.HasValue)
                    q = q.Where(o => o.UnitType == unitType.Value);

                var units = await q
                    .OrderBy(o => o.HierarchyPath)
                    .ThenBy(o => o.DisplayOrder)
                    .Select(o => new
                    {
                        o.Id,
                        o.ParentId,
                        o.Code,
                        o.Name,
                        UnitType = o.UnitType.ToString(),
                        o.HierarchyPath,
                        o.Email,
                        o.PhoneNumber
                    })
                    .ToListAsync();

                return Ok(units);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lấy danh sách đơn vị: {ex.Message}" });
            }
        }

        // ── POST api/quiz/org-units ────────────────────────────────────────────
        /// <summary>
        /// Tạo mới một đơn vị tổ chức trong cây phân cấp
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrganizationUnit([FromBody] CreateOrgUnitRequest req)
        {
            try
            {
                var tenantId = _tenantContext.CurrentTenantId;
                if (tenantId == Guid.Empty)
                    return BadRequest(new { message = "Không xác định được Tenant ID." });

                if (string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.Name))
                    return BadRequest(new { message = "Mã đơn vị (Code) và Tên đơn vị (Name) là bắt buộc." });

                var cleanCode = req.Code.Trim().ToUpperInvariant();

                var exists = await _db.OrganizationUnits
                    .AsNoTracking()
                    .AnyAsync(o => o.TenantId == tenantId && o.Code == cleanCode);

                if (exists)
                    return BadRequest(new { message = $"Mã đơn vị '{cleanCode}' đã tồn tại trong tổ chức." });

                // Tính HierarchyPath
                string hierarchyPath = $"/{cleanCode}";
                if (req.ParentId.HasValue && req.ParentId.Value != Guid.Empty)
                {
                    var parent = await _db.OrganizationUnits
                        .AsNoTracking()
                        .FirstOrDefaultAsync(o => o.Id == req.ParentId.Value && o.TenantId == tenantId);

                    if (parent != null)
                    {
                        hierarchyPath = $"{parent.HierarchyPath.TrimEnd('/')}/{cleanCode}";
                    }
                }

                var unit = new OrganizationUnit
                {
                    TenantId = tenantId,
                    ParentId = req.ParentId == Guid.Empty ? null : req.ParentId,
                    Code = cleanCode,
                    Name = req.Name.Trim(),
                    UnitType = req.UnitType,
                    HierarchyPath = hierarchyPath,
                    Email = req.Email?.Trim(),
                    PhoneNumber = req.PhoneNumber?.Trim(),
                    Address = req.Address?.Trim(),
                    DisplayOrder = req.DisplayOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _db.OrganizationUnits.Add(unit);
                await _db.SaveChangesAsync();

                return Ok(new { message = "Tạo đơn vị thành công.", unit });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi tạo đơn vị: {ex.Message}" });
            }
        }

        // ── PUT api/quiz/org-units/{id} ────────────────────────────────────────
        /// <summary>
        /// Cập nhật thông tin đơn vị tổ chức
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateOrganizationUnit(Guid id, [FromBody] UpdateOrgUnitRequest req)
        {
            try
            {
                var tenantId = _tenantContext.CurrentTenantId;
                var unit = await _db.OrganizationUnits
                    .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

                if (unit == null)
                    return NotFound(new { message = "Không tìm thấy đơn vị tổ chức." });

                if (!string.IsNullOrWhiteSpace(req.Name)) unit.Name = req.Name.Trim();
                if (req.UnitType.HasValue) unit.UnitType = req.UnitType.Value;
                if (req.Email != null) unit.Email = req.Email.Trim();
                if (req.PhoneNumber != null) unit.PhoneNumber = req.PhoneNumber.Trim();
                if (req.Address != null) unit.Address = req.Address.Trim();
                unit.DisplayOrder = req.DisplayOrder;
                if (req.IsActive.HasValue) unit.IsActive = req.IsActive.Value;

                await _db.SaveChangesAsync();
                return Ok(new { message = "Cập nhật đơn vị thành công.", unit });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi cập nhật đơn vị: {ex.Message}" });
            }
        }

        // ── DELETE api/quiz/org-units/{id} ─────────────────────────────────────
        /// <summary>
        /// Xóa hoặc vô hiệu hóa đơn vị tổ chức (Bảo vệ tính toàn vẹn cây phân cấp)
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteOrganizationUnit(Guid id)
        {
            try
            {
                var tenantId = _tenantContext.CurrentTenantId;
                var unit = await _db.OrganizationUnits
                    .Include(o => o.Children)
                    .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

                if (unit == null)
                    return NotFound(new { message = "Không tìm thấy đơn vị tổ chức." });

                if (unit.Children.Any(c => c.IsActive))
                {
                    return BadRequest(new { message = "Không thể xóa đơn vị vì còn các đơn vị con trực thuộc. Vui lòng chuyển hoặc xóa các đơn vị con trước." });
                }

                // Soft-delete: tắt kích hoạt
                unit.IsActive = false;
                await _db.SaveChangesAsync();

                return Ok(new { message = "Đã vô hiệu hóa đơn vị tổ chức thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi xóa đơn vị: {ex.Message}" });
            }
        }
    }

    public class OrgUnitDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? ParentId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string UnitType { get; set; } = string.Empty;
        public string HierarchyPath { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrgUnitDto> Children { get; set; } = new();
    }

    public class CreateOrgUnitRequest
    {
        public Guid? ParentId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public OrgUnitType UnitType { get; set; } = OrgUnitType.Branch;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateOrgUnitRequest
    {
        public string? Name { get; set; }
        public OrgUnitType? UnitType { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public int DisplayOrder { get; set; }
        public bool? IsActive { get; set; }
    }
}
