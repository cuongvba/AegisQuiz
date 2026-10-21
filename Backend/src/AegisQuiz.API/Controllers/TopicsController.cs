using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AegisQuiz.API.Security;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Infrastructure.Services;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [World-Class Refactor] TopicsController — Quản lý Chủ đề câu hỏi.
    /// Route base: api/quiz/topics
    /// [CRITICAL SECURITY FIX] Trước đây controller này KHÔNG có [Authorize] nào — chỉ
    /// được bảo vệ "tình cờ" bởi FallbackPolicy toàn cục (yêu cầu đăng nhập), nhưng Learner/
    /// GuestViewer vẫn tạo/sửa/xóa được Topic. Nay yêu cầu Role-Weight ≥ TeamLeader, khớp
    /// với QuestionsController.
    /// </summary>
    [ApiController]
    [Route("api/quiz/topics")]
    [Authorize(Policy = RoleWeightPolicies.TeamLeader)]
    public class TopicsController : ControllerBase
    {
        private readonly AegisQuizDbContext          _db;
        private readonly IContentOwnershipGuard      _ownershipGuard;
        private readonly ITenantContext              _tenant;
        private readonly IExamDeduplicationEngine    _deduplicationEngine;

        public TopicsController(
            AegisQuizDbContext              db, 
            IContentOwnershipGuard          ownershipGuard, 
            ITenantContext                  tenant,
            IExamDeduplicationEngine?       deduplicationEngine = null)
        {
            _db                  = db;
            _ownershipGuard      = ownershipGuard;
            _tenant              = tenant;
            _deduplicationEngine = deduplicationEngine ?? new ExamDeduplicationEngine(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<ExamDeduplicationEngine>.Instance);
        }

        // ── GET api/quiz/topics ────────────────────────────────────────────────
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetTopics([FromQuery] bool enabledOnly = false)
        {
            try
            {
                await AutoHealHierarchicalTopicsAsync();

                var q = _db.BankTopics.AsNoTracking().AsQueryable();
                if (enabledOnly) q = q.Where(t => t.Enabled);

                var topics = await q.OrderBy(t => t.Code).ToListAsync();
                return Ok(topics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lấy danh sách chủ đề: {ex.Message}" });
            }
        }

        private async Task AutoHealHierarchicalTopicsAsync()
        {
            try
            {
                var dot2Topics = await _db.BankTopics
                    .Where(t => t.Code.StartsWith("2026_DOT2_") && (!t.ParentId.HasValue || t.Name == t.Code || t.Description == "Tự động khởi tạo từ dữ liệu nhập file"))
                    .ToListAsync();

                if (dot2Topics.Count == 0) return;

                // 1. Bảo đảm chủ đề cha 2026-DOT2 tồn tại
                var parent = await _db.BankTopics.FirstOrDefaultAsync(t => t.Code == "2026_DOT2");
                if (parent == null)
                {
                    parent = new Domain.Entities.BankTopic
                    {
                        Id = Guid.NewGuid(),
                        Code = "2026_DOT2",
                        Name = "2026-DOT2",
                        CategoryCode = "BANKING",
                        DomainCode = "BANKING",
                        Scope = "COMMUNITY",
                        MaterializedPath = "/BANKING/2026_DOT2/",
                        DepthLevel = 1,
                        Urn = "urn:aegis:topic:banking:2026-dot2",
                        Description = "Thư mục chuyên đề: 2026-DOT2",
                        VisibilityScope = Domain.Entities.TopicVisibility.Public,
                        Enabled = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.BankTopics.Add(parent);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    if (parent.DomainCode != "BANKING" || string.IsNullOrWhiteSpace(parent.MaterializedPath))
                    {
                        parent.DomainCode = "BANKING";
                        parent.Scope = "COMMUNITY";
                        parent.MaterializedPath = "/BANKING/2026_DOT2/";
                        parent.DepthLevel = 1;
                        parent.Urn = "urn:aegis:topic:banking:2026-dot2";
                    }
                }

                var knownTitles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["2026_DOT2_1_TIN_DUNG_KHDN_240_CAU"] = "1. Tín dụng KHDN - 240 câu",
                    ["2026_DOT2_2_TIN_DUNG_KHCN_240_CAU"] = "2. Tín dụng KHCN - 240 câu",
                    ["2026_DOT2_3_KE_TOAN_TIEN_GUI_TIEN_VAY"] = "3. Kế toán tiền gửi, tiền vay",
                    ["2026_DOT2_4_KE_TOAN_THANH_TOAN"] = "4. Kế toán thanh toán",
                    ["2026_DOT2_5_KE_TOAN_TIEN_MAT"] = "5. Kế toán tiền mặt",
                    ["2026_DOT2_6_KE_TOAN_TAI_CHINH_TAI_SAN"] = "6. Kế toán tài chính, tài sản",
                    ["2026_DOT2_7_KE_TOAN_GIAO_DICH_NOI_BO"] = "7. Kế toán giao dịch nội bộ",
                    ["2026_DOT2_8_THANH_TOAN_QUOC_TE"] = "8. Thanh toán quốc tế",
                    ["2026_DOT2_9_QUAN_TRI_RUI_RO"] = "9. Quản trị rủi ro",
                    ["2026_DOT2_10_PHAP_CHE"] = "10. Pháp chế",
                    ["2026_DOT2_11_KIEM_TRA_KIEM_SOAT_NOI_BO"] = "11. Kiểm tra kiểm soát nội bộ",
                    ["2026_DOT2_12_XU_LY_NO"] = "12. Xử lý nợ",
                    ["2026_DOT2_13_DICH_VU_KHACH_HANG"] = "13. Dịch vụ khách hàng",
                    ["2026_DOT2_14_KHO_QUY"] = "14. Kho quỹ",
                    ["2026_DOT2_15_KE_HOACH_NGUON_VON"] = "15. Kế hoạch nguồn vốn",
                    ["2026_DOT2_16_TO_CHUC_CAN_BO"] = "16. Tổ chức cán bộ",
                    ["2026_DOT2_17_VAN_PHONG_TIEN_ICH"] = "17. Văn phòng tiện ích",
                    ["2026_DOT2_18_CONG_NGHE_THONG_TIN"] = "18. Công nghệ thông tin",
                    ["2026_DOT2_19_AN_TOAN_BAO_MAT"] = "19. An toàn bảo mật"
                };

                bool changed = false;
                foreach (var dt in dot2Topics)
                {
                    if (dt.Id == parent.Id) continue;

                    if (!dt.ParentId.HasValue)
                    {
                        dt.ParentId = parent.Id;
                        changed = true;
                    }

                    if (dt.Name == dt.Code && knownTitles.TryGetValue(dt.Code, out var knownName))
                    {
                        dt.Name = knownName;
                        changed = true;
                    }

                    dt.DomainCode = "BANKING";
                    dt.Scope = "COMMUNITY";
                    dt.MaterializedPath = $"/BANKING/2026_DOT2/{dt.Code}/";
                    dt.DepthLevel = 2;
                    dt.Urn = $"urn:aegis:topic:banking:2026-dot2:{dt.Code.ToLowerInvariant()}";

                    if (string.IsNullOrWhiteSpace(dt.Description) || dt.Description == "Tự động khởi tạo từ dữ liệu nhập file")
                    {
                        var qCount = await _db.Questions.CountAsync(q => q.CategoryCode == dt.Code);
                        dt.Description = $"{dt.Name} | Đường dẫn: 2026-DOT2";
                        dt.QuestionCountCached = qCount;
                        changed = true;
                    }
                }

                // Cập nhật các chủ đề khác: TNPT_TIENG_ANH
                var tnpt = await _db.BankTopics.FirstOrDefaultAsync(t => t.Code == "TNPT_TIENG_ANH");
                if (tnpt != null && (tnpt.DomainCode != "EDUCATION" || string.IsNullOrWhiteSpace(tnpt.MaterializedPath)))
                {
                    tnpt.DomainCode = "EDUCATION";
                    tnpt.Scope = "COMMUNITY";
                    tnpt.MaterializedPath = "/EDUCATION/TNPT_TIENG_ANH/";
                    tnpt.DepthLevel = 1;
                    tnpt.Urn = "urn:aegis:topic:education:tnpt-tieng-anh";
                    changed = true;
                }

                if (changed)
                {
                    await _db.SaveChangesAsync();
                }
            }
            catch
            {
                // Bỏ qua nếu có xung đột để không làm gián đoạn API get
            }
        }

        // ── GET api/quiz/topics/tree ───────────────────────────────────────────
        /// <summary>
        /// [Omni-Century] Trả về cây tri thức đa phân cấp gom nhóm theo Domain ➔ Parent Folder ➔ Child Topics.
        /// </summary>
        [HttpGet("tree")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCognitiveTopicTree(
            [FromQuery] string? domainCode = null,
            [FromQuery] string? scope = null,
            [FromQuery] string? search = null)
        {
            try
            {
                await AutoHealHierarchicalTopicsAsync();

                var domains = await _db.DynamicDomains.AsNoTracking().OrderBy(d => d.DisplayOrder).ToListAsync();
                var topics = await _db.BankTopics.AsNoTracking().Where(t => t.Enabled).ToListAsync();

                // Lấy thống kê số câu hỏi theo CategoryCode
                var questionCounts = await _db.Questions.AsNoTracking()
                    .GroupBy(q => q.CategoryCode)
                    .Select(g => new { Code = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Code, x => x.Count, StringComparer.OrdinalIgnoreCase);

                // Áp dụng bộ lọc
                if (!string.IsNullOrWhiteSpace(scope))
                {
                    topics = topics.Where(t => string.Equals(t.Scope, scope, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                if (!string.IsNullOrWhiteSpace(domainCode))
                {
                    topics = topics.Where(t => string.Equals(t.DomainCode, domainCode, StringComparison.OrdinalIgnoreCase)).ToList();
                    domains = domains.Where(d => string.Equals(d.Code, domainCode, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim().ToLowerInvariant();
                    topics = topics.Where(t => t.Name.ToLowerInvariant().Contains(s) || t.Code.ToLowerInvariant().Contains(s)).ToList();
                }

                var treeResult = new List<CognitiveDomainGroupDto>();

                // Nhóm theo Domain
                var topicsByDomain = topics.GroupBy(t => string.IsNullOrWhiteSpace(t.DomainCode) ? "GENERAL" : t.DomainCode);

                foreach (var group in topicsByDomain)
                {
                    var domain = domains.FirstOrDefault(d => string.Equals(d.Code, group.Key, StringComparison.OrdinalIgnoreCase))
                                 ?? new DynamicDomain { Code = group.Key, Name = group.Key, Icon = "Folder", ColorBadge = "#64748b" };

                    var domainTopics = group.ToList();
                    var allNodes = domainTopics.Select(t => new CognitiveTopicNodeDto
                    {
                        Id = t.Id,
                        Code = t.Code,
                        Name = t.Name,
                        Description = t.Description,
                        DomainCode = t.DomainCode,
                        Scope = t.Scope,
                        MaterializedPath = t.MaterializedPath ?? "",
                        DepthLevel = t.DepthLevel,
                        ParentId = t.ParentId,
                        Enabled = t.Enabled,
                        QuestionCount = questionCounts.TryGetValue(t.Code, out var c) ? c : t.QuestionCountCached
                    }).ToList();

                    // Xây dựng cây quan hệ cha-con
                    var nodeLookup = allNodes.ToDictionary(n => n.Id);
                    var rootNodes = new List<CognitiveTopicNodeDto>();

                    foreach (var node in allNodes)
                    {
                        if (node.ParentId.HasValue && nodeLookup.TryGetValue(node.ParentId.Value, out var parentNode))
                        {
                            parentNode.Children.Add(node);
                        }
                        else
                        {
                            rootNodes.Add(node);
                        }
                    }

                    // Sắp xếp các con
                    foreach (var r in rootNodes)
                    {
                        r.Children = r.Children.OrderBy(c => c.Name).ToList();
                    }

                    // Tính tổng số câu hỏi của Domain
                    int domainTotalQuestions = allNodes.Sum(n => n.QuestionCount);

                    // Sắp xếp
                    rootNodes = rootNodes.OrderBy(n => n.Name).ToList();

                    treeResult.Add(new CognitiveDomainGroupDto
                    {
                        DomainCode = domain.Code,
                        DomainName = domain.Name,
                        Icon = domain.Icon,
                        ColorBadge = domain.ColorBadge,
                        TotalQuestionCount = domainTotalQuestions,
                        RootTopics = rootNodes
                    });
                }

                // Sắp xếp danh sách Domain theo thứ tự chuẩn
                treeResult = treeResult.OrderBy(g => g.DomainCode == "BANKING" ? 1 : g.DomainCode == "EDUCATION" ? 2 : g.DomainCode == "GOV_DRIVING" ? 3 : 4).ToList();

                return Ok(treeResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi truy vấn cây tri thức: {ex.Message}" });
            }
        }

        // ── GET api/quiz/topics/counts ─────────────────────────────────────────
        [HttpGet("counts")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTopicCounts()
        {
            try
            {
                // Tổng số câu hỏi theo CategoryCode
                var rawCounts = await _db.Questions.AsNoTracking()
                    .GroupBy(q => q.CategoryCode)
                    .Select(g => new { TopicCode = g.Key, Count = g.Count() })
                    .ToListAsync();

                var countMap   = rawCounts.ToDictionary(x => x.TopicCode, x => x.Count);
                var allTopics  = await _db.BankTopics.AsNoTracking().ToListAsync();
                var result     = new List<object>();

                // [P2 Perf Fix] Trước đây BFS lại toàn bộ allTopics cho TỪNG topic
                // (allTopics.Where(t => t.ParentId == parentId) lồng trong loop) → O(n²)
                // trong bộ nhớ. Nay dựng sẵn 1 lần Dictionary<ParentId, List<Children>>,
                // tra cứu O(1) mỗi bước BFS → tổng chi phí còn O(n).
                var childrenByParentId = allTopics
                    .Where(t => t.ParentId.HasValue)
                    .GroupBy(t => t.ParentId!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var topic in allTopics)
                {
                    // BFS để lấy tất cả mã con (chống cycle lặp vô hạn)
                    var descendants = new List<string> { topic.Code };
                    var queue       = new Queue<Guid>();
                    var visited     = new HashSet<Guid> { topic.Id };
                    queue.Enqueue(topic.Id);

                    while (queue.Count > 0)
                    {
                        var parentId = queue.Dequeue();
                        if (!childrenByParentId.TryGetValue(parentId, out var children))
                            continue;

                        foreach (var child in children)
                        {
                            if (visited.Add(child.Id))
                            {
                                descendants.Add(child.Code);
                                queue.Enqueue(child.Id);
                            }
                        }
                    }

                    result.Add(new
                    {
                        TopicCode = topic.Code,
                        Count     = descendants.Sum(code => countMap.GetValueOrDefault(code, 0))
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi lấy thống kê chủ đề: {ex.Message}" });
            }
        }

        // ── POST api/quiz/topics ───────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> CreateTopic([FromBody] BankTopicUpsertDto dto)
        {
            try
            {
                // Kiểm tra Code trùng lặp
                if (await _db.BankTopics.AnyAsync(t => t.Code == dto.Code))
                    return Conflict(new { message = $"Mã chủ đề '{dto.Code}' đã tồn tại." });

                var topic = new BankTopic
                {
                    Id              = Guid.NewGuid(),
                    Code            = dto.Code.Trim().ToUpper(),
                    Name            = dto.Name,
                    Description     = dto.Description,
                    CategoryCode    = dto.CategoryCode,
                    ParentId        = dto.ParentId,
                    Enabled         = dto.Enabled,
                    VisibilityScope = dto.VisibilityScope,
                    DomainCode      = !string.IsNullOrWhiteSpace(dto.DomainCode) ? dto.DomainCode : "GENERAL",
                    Scope           = !string.IsNullOrWhiteSpace(dto.Scope) ? dto.Scope : "COMMUNITY",
                    CreatedAt       = DateTime.UtcNow
                };

                _db.BankTopics.Add(topic);
                await _db.SaveChangesAsync();
                return Created($"api/quiz/topics/{topic.Id}", topic);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi tạo chủ đề: {ex.Message}" });
            }
        }

        // ── PUT api/quiz/topics/{id} ───────────────────────────────────────────
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateTopic(Guid id, [FromBody] BankTopicUpsertDto dto)
        {
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
            if (_db.Database.IsRelational()) transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var topic = await _db.BankTopics.FindAsync(id);
                if (topic == null)
                    return NotFound(new { message = "Không tìm thấy chủ đề" });

                if (!await _ownershipGuard.CanManageAsync(User, topic.CreatedByUserId?.ToString(), null))
                    return Forbid();

                var oldCode = topic.Code;
                var newCode = dto.Code.Trim().ToUpper();
                bool codeChanged = !string.Equals(oldCode, newCode, StringComparison.OrdinalIgnoreCase);

                if (codeChanged)
                {
                    if (await _db.BankTopics.AnyAsync(t => t.Id != id && t.Code == newCode))
                        return Conflict(new { message = $"Mã chủ đề '{newCode}' đã được sử dụng bởi một chủ đề khác." });
                }

                topic.Code            = newCode;
                topic.Name            = dto.Name;
                topic.Description     = dto.Description;
                topic.CategoryCode    = dto.CategoryCode;
                topic.ParentId        = dto.ParentId;
                topic.Enabled         = dto.Enabled;
                topic.VisibilityScope = dto.VisibilityScope;
                if (!string.IsNullOrWhiteSpace(dto.DomainCode)) topic.DomainCode = dto.DomainCode;
                if (!string.IsNullOrWhiteSpace(dto.Scope)) topic.Scope = dto.Scope;
                topic.UpdatedAt       = DateTime.UtcNow;

                if (codeChanged)
                {
                    // 1. Cascade update MaterializedPath của các chủ đề con
                    var oldPathSegment = $"/{oldCode}/";
                    var newPathSegment = $"/{newCode}/";
                    var descendants = await _db.BankTopics
                        .Where(t => t.MaterializedPath != null && t.MaterializedPath.Contains(oldPathSegment))
                        .ToListAsync();
                    foreach (var d in descendants)
                    {
                        d.MaterializedPath = d.MaterializedPath!.Replace(oldPathSegment, newPathSegment);
                    }

                    // 2. Cascade update các câu hỏi có CategoryCode == oldCode
                    var directQuestions = await _db.Questions
                        .Where(q => q.CategoryCode == oldCode)
                        .ToListAsync();
                    foreach (var q in directQuestions)
                    {
                        q.CategoryCode = newCode;
                    }

                    // 3. Cascade update các câu hỏi liên kết qua Tags / linkedTopicCodes
                    var oldTag = "#" + oldCode;
                    var taggedQuestions = await _db.Questions
                        .Where(q => q.Tags.Contains(oldCode) || q.Tags.Contains(oldTag))
                        .ToListAsync();
                    foreach (var q in taggedQuestions)
                    {
                        _deduplicationEngine.DisassociateTopicFromQuestion(q, oldCode, newCode);
                        _deduplicationEngine.AssociateTopicToQuestion(q, newCode, dto.Name);
                    }
                }

                await _db.SaveChangesAsync();
                if (transaction != null) await transaction.CommitAsync();
                return Ok(topic);
            }
            catch (Exception ex)
            {
                if (transaction != null) await transaction.RollbackAsync();
                return StatusCode(500, new { message = $"Lỗi cập nhật chủ đề: {ex.Message}" });
            }
            finally
            {
                if (transaction != null) await transaction.DisposeAsync();
            }
        }

        // ── DELETE api/quiz/topics/{id} ────────────────────────────────────────
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTopic(
            Guid id, 
            [FromQuery] bool deleteQuestions = false,
            [FromQuery] TopicDeleteMode mode = TopicDeleteMode.UnlinkOnly)
        {
            // Tương thích ngược: nếu client cũ truyền deleteQuestions = true
            if (deleteQuestions && mode == TopicDeleteMode.UnlinkOnly)
            {
                mode = TopicDeleteMode.SafeCascade;
            }

            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
            if (_db.Database.IsRelational())
            {
                transaction = await _db.Database.BeginTransactionAsync();
            }

            try
            {
                var topic = await _db.BankTopics
                    .IgnoreQueryFilters()
                    .Include(t => t.Children)
                    .Include(t => t.Parent)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (topic == null)
                    return NotFound(new { message = "Không tìm thấy chủ đề" });

                if (!_tenant.IsSystemAdmin && topic.TenantId != Guid.Empty && topic.TenantId != _tenant.CurrentTenantId)
                    return NotFound(new { message = "Không tìm thấy chủ đề" });

                if (!await _ownershipGuard.CanManageAsync(User, topic.CreatedByUserId?.ToString(), null))
                    return Forbid();

                // ── KỊCH BẢN 3: LƯU TRỮ MỀM (ARCHIVE SOFT) ──
                if (mode == TopicDeleteMode.ArchiveSoft)
                {
                    topic.Enabled = false;
                    topic.VisibilityScope = TopicVisibility.Private;
                    topic.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    if (transaction != null) await transaction.CommitAsync();

                    return Ok(new
                    {
                        success = true,
                        message = $"Đã lưu trữ thành công chủ đề '{topic.Name}' mà không xóa câu hỏi.",
                        archivedTopicId = topic.Id
                    });
                }

                var allTopics = await _db.BankTopics
                    .IgnoreQueryFilters()
                    .Where(t => _tenant.IsSystemAdmin || t.TenantId == Guid.Empty || t.TenantId == _tenant.CurrentTenantId)
                    .ToListAsync();

                // Thu thập các chủ đề bị ảnh hưởng (cả topic hiện tại và các con cháu nếu cascade)
                var affectedTopics = (mode == TopicDeleteMode.SafeCascade)
                    ? new List<BankTopic>(GetDescendantTopics(topic.Id, allTopics)) { topic }
                    : new List<BankTopic> { topic };

                var baseCodes = affectedTopics
                    .SelectMany(t => new[] { t.Code, t.CategoryCode })
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var expandedCodes = new HashSet<string>(baseCodes, StringComparer.OrdinalIgnoreCase);
                foreach (var c in baseCodes)
                {
                    expandedCodes.Add(c.Replace("_", "-"));
                    expandedCodes.Add(c.Replace("-", "_"));
                }
                var affectedCodes = expandedCodes.ToList();

                var affectedTags = affectedCodes.Select(c => "#" + c).Concat(affectedCodes).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                // Tìm tất cả câu hỏi liên quan trực tiếp qua CategoryCode (100% provider-agnostic & indexed)
                var candidateQuestions = await _db.Questions
                    .IgnoreQueryFilters()
                    .Where(q => (_tenant.IsSystemAdmin || q.TenantId == Guid.Empty || q.TenantId == _tenant.CurrentTenantId)
                             && affectedCodes.Contains(q.CategoryCode))
                    .ToListAsync();

                // Bổ sung các câu hỏi liên kết chéo qua Tags (hỗ trợ N-M, an toàn với mọi provider)
                try
                {
                    foreach (var code in affectedCodes)
                    {
                        var tag1 = code;
                        var tag2 = "#" + code;
                        var tag3 = "#topic:" + code;
                        var tagQuestions = await _db.Questions
                            .IgnoreQueryFilters()
                            .Where(q => (_tenant.IsSystemAdmin || q.TenantId == Guid.Empty || q.TenantId == _tenant.CurrentTenantId)
                                     && (q.Tags.Contains(tag1) || q.Tags.Contains(tag2) || q.Tags.Contains(tag3)))
                            .ToListAsync();

                        foreach (var tq in tagQuestions)
                        {
                            if (!candidateQuestions.Any(cq => cq.Id == tq.Id))
                            {
                                candidateQuestions.Add(tq);
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback mềm cho các database provider kiểm thử in-memory không dịch được array contains
                }

                int unlinkedCount = 0;
                int deletedQuestionsCount = 0;
                int reassignedCount = 0;

                string fallbackCode = !string.IsNullOrWhiteSpace(topic.Parent?.Code) ? topic.Parent.Code : "GENERAL";
                if (fallbackCode == "GENERAL" && !allTopics.Any(t => t.Code.Equals("GENERAL", StringComparison.OrdinalIgnoreCase)))
                {
                    var generalTopic = new BankTopic
                    {
                        Id = Guid.NewGuid(),
                        Code = "GENERAL",
                        Name = "Chủ đề chung",
                        Description = "Chủ đề mặc định cho các câu hỏi chưa phân nhóm",
                        CategoryCode = "GENERAL",
                        Enabled = true,
                        VisibilityScope = "PUBLIC",
                        TenantId = topic.TenantId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.BankTopics.Add(generalTopic);
                    allTopics.Add(generalTopic);
                    await _db.SaveChangesAsync();
                }

                var questionsToDelete = new List<QuestionBase>();

                foreach (var q in candidateQuestions)
                {
                    // Kiểm tra xem câu hỏi này có liên kết tới các Topic BÊN NGOÀI nhánh bị xóa hay không
                    var outsideTopics = q.LinkedTopicCodes
                        .Where(tc => !affectedCodes.Contains(tc, StringComparer.OrdinalIgnoreCase))
                        .ToList();

                    bool isShared = outsideTopics.Count > 0;

                    if (isShared)
                    {
                        // [BẢO VỆ TUYỆT ĐỐI CÂU HỎI DÙNG CHUNG]:
                        // Chỉ gỡ bỏ các mã topic bị xóa, TUYỆT ĐỐI KHÔNG XÓA THỰC THỂ CÂU HỎI!
                        foreach (var code in affectedCodes)
                        {
                            _deduplicationEngine.DisassociateTopicFromQuestion(q, code, outsideTopics.FirstOrDefault() ?? fallbackCode);
                        }
                        unlinkedCount++;
                    }
                    else
                    {
                        // Câu hỏi ĐỘC QUYỀN (chỉ thuộc riêng nhánh này)
                        if (mode == TopicDeleteMode.SafeCascade)
                        {
                            questionsToDelete.Add(q);
                            deletedQuestionsCount++;
                        }
                        else
                        {
                            // UnlinkOnly: Bàn giao câu hỏi về fallbackCode
                            foreach (var code in affectedCodes)
                            {
                                _deduplicationEngine.DisassociateTopicFromQuestion(q, code, fallbackCode);
                            }
                            q.CategoryCode = fallbackCode;
                            reassignedCount++;
                        }
                    }
                }

                if (questionsToDelete.Count > 0)
                {
                    _db.Questions.RemoveRange(questionsToDelete);
                }

                // Bàn giao các topic con trực tiếp nếu không cascade
                var directChildren = allTopics.Where(t => t.ParentId == topic.Id).ToList();
                if (mode != TopicDeleteMode.SafeCascade)
                {
                    foreach (var child in directChildren)
                    {
                        child.ParentId = topic.ParentId;
                        child.Parent = topic.Parent;
                    }
                }

                // Xóa ràng buộc tự tham chiếu
                foreach (var t in affectedTopics)
                {
                    t.ParentId = null;
                    t.Parent = null;
                    t.Children.Clear();
                }
                await _db.SaveChangesAsync();

                // Xóa topics (leaf-to-root)
                affectedTopics.Reverse();
                _db.BankTopics.RemoveRange(affectedTopics);
                await _db.SaveChangesAsync();

                if (transaction != null) await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Đã xử lý xóa chủ đề '{topic.Name}' thành công (Chế độ: {mode}).",
                    deletedTopicCount = affectedTopics.Count,
                    unlinkedSharedQuestionCount = unlinkedCount,
                    deletedExclusiveQuestionCount = deletedQuestionsCount,
                    reassignedQuestionCount = reassignedCount
                });
            }
            catch (Exception ex)
            {
                if (transaction != null) await transaction.RollbackAsync();
                var innerMsg = ex.InnerException?.Message;
                var detailedMsg = string.IsNullOrWhiteSpace(innerMsg) ? ex.Message : $"{ex.Message} -> {innerMsg}";
                return StatusCode(500, new { message = $"Lỗi xóa chủ đề: {detailedMsg}" });
            }
            finally
            {
                if (transaction != null) await transaction.DisposeAsync();
            }
        }

        // ── Private helpers ────────────────────────────────────────────────────
        private static List<BankTopic> GetDescendantTopics(Guid rootId, List<BankTopic> allTopics)
        {
            var result = new List<BankTopic>();
            var queue = new Queue<Guid>();
            var visited = new HashSet<Guid> { rootId };
            queue.Enqueue(rootId);

            while (queue.Count > 0)
            {
                var currentParentId = queue.Dequeue();
                var children = allTopics.Where(t => t.ParentId == currentParentId).ToList();
                foreach (var child in children)
                {
                    if (visited.Add(child.Id))
                    {
                        result.Add(child);
                        queue.Enqueue(child.Id);
                    }
                }
            }

            return result;
        }
    }
}
