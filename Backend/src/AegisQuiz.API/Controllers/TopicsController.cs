using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [World-Class Refactor] TopicsController — Quản lý Chủ đề câu hỏi.
    /// Route base: api/quiz/topics
    /// </summary>
    [ApiController]
    [Route("api/quiz/topics")]
    public class TopicsController : ControllerBase
    {
        private readonly AegisQuizDbContext _db;

        public TopicsController(AegisQuizDbContext db) => _db = db;

        // ── GET api/quiz/topics ────────────────────────────────────────────────
        [HttpGet]
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
                        dt.Description = $"{dt.Name} | Đường dẫn: 2026-DOT2 | Số lượng: {qCount} câu hỏi";
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
                        foreach (var child in allTopics.Where(t => t.ParentId == parentId))
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
            try
            {
                var topic = await _db.BankTopics.FindAsync(id);
                if (topic == null)
                    return NotFound(new { message = "Không tìm thấy chủ đề" });

                topic.Code            = dto.Code.Trim().ToUpper();
                topic.Name            = dto.Name;
                topic.Description     = dto.Description;
                topic.CategoryCode    = dto.CategoryCode;
                topic.ParentId        = dto.ParentId;
                topic.Enabled         = dto.Enabled;
                topic.VisibilityScope = dto.VisibilityScope;
                if (!string.IsNullOrWhiteSpace(dto.DomainCode)) topic.DomainCode = dto.DomainCode;
                if (!string.IsNullOrWhiteSpace(dto.Scope)) topic.Scope = dto.Scope;
                topic.UpdatedAt       = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return Ok(topic);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi cập nhật chủ đề: {ex.Message}" });
            }
        }

        // ── DELETE api/quiz/topics/{id} ────────────────────────────────────────
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTopic(Guid id, [FromQuery] bool deleteQuestions = false)
        {
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
            if (_db.Database.IsRelational())
            {
                transaction = await _db.Database.BeginTransactionAsync();
            }

            try
            {
                // Sử dụng IgnoreQueryFilters để quét toàn bộ cây phân cấp, tránh bị sót nhánh con do multi-tenancy filter
                var topic = await _db.BankTopics
                    .IgnoreQueryFilters()
                    .Include(t => t.Children)
                    .Include(t => t.Parent)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (topic == null)
                    return NotFound(new { message = "Không tìm thấy chủ đề" });

                var allTopics = await _db.BankTopics.IgnoreQueryFilters().ToListAsync();

                if (deleteQuestions)
                {
                    // ── KỊCH BẢN 1: XÓA TOÀN BỘ NHÁNH & CÂU HỎI (CASCADE) ──
                    var descendantTopics = GetDescendantTopics(topic.Id, allTopics);
                    var allBranchTopics = new List<BankTopic>(descendantTopics) { topic };

                    // Thu thập tất cả các mã chủ đề (cả Code và CategoryCode)
                    var allBranchCodes = allBranchTopics
                        .SelectMany(t => new[] { t.Code, t.CategoryCode })
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    // 1. Xóa toàn bộ câu hỏi liên quan thuộc tất cả các chủ đề trong nhánh
                    var questionsToDelete = await _db.Questions
                        .IgnoreQueryFilters()
                        .Where(q => allBranchCodes.Contains(q.CategoryCode))
                        .ToListAsync();
                    if (questionsToDelete.Count > 0)
                    {
                        _db.Questions.RemoveRange(questionsToDelete);
                    }

                    // 2. Gỡ bỏ hoàn toàn ràng buộc tự tham chiếu ParentId và navigation properties
                    foreach (var t in allBranchTopics)
                    {
                        t.ParentId = null;
                        t.Parent = null;
                        t.Children.Clear();
                    }
                    await _db.SaveChangesAsync();

                    // 3. Xóa các chủ đề theo thứ tự ngược: nhánh sâu nhất trước rồi mới đến gốc (Leaf-to-Root)
                    allBranchTopics.Reverse();
                    _db.BankTopics.RemoveRange(allBranchTopics);
                    await _db.SaveChangesAsync();
                    if (transaction != null) await transaction.CommitAsync();

                    return Ok(new
                    {
                        success = true,
                        message = $"Đã xóa thành công chủ đề '{topic.Name}', {descendantTopics.Count} chủ đề con và {questionsToDelete.Count} câu hỏi liên quan.",
                        deletedTopicCount = allBranchTopics.Count,
                        deletedQuestionCount = questionsToDelete.Count
                    });
                }
                else
                {
                    // ── KỊCH BẢN 2: CHỈ XÓA CHỦ ĐỀ HIỆN TẠI (BẢO TOÀN CON VÀ CÂU HỎI) ──
                    // 1. Bàn giao các chủ đề con trực tiếp: chuyển ParentId lên cấp cha của topic (hoặc thành root nếu topic là root)
                    var directChildren = allTopics.Where(t => t.ParentId == topic.Id).ToList();
                    foreach (var child in directChildren)
                    {
                        child.ParentId = topic.ParentId;
                        child.Parent = topic.Parent;
                    }

                    // 2. Bàn giao các câu hỏi của chủ đề này: chuyển sang chủ đề cha hoặc GENERAL để không bị mồ côi
                    string fallbackCode = !string.IsNullOrWhiteSpace(topic.Parent?.Code) ? topic.Parent.Code : "GENERAL";

                    // Nếu chuyển về GENERAL, đảm bảo hệ thống luôn có chủ đề GENERAL trong DB
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

                    // Tìm các câu hỏi thuộc chủ đề bị xóa
                    var orphanQuestions = await _db.Questions
                        .IgnoreQueryFilters()
                        .Where(q => q.CategoryCode == topic.Code || (!string.IsNullOrEmpty(topic.CategoryCode) && q.CategoryCode == topic.CategoryCode && !_db.BankTopics.Any(other => other.Id != topic.Id && other.Code == topic.CategoryCode)))
                        .ToListAsync();
                    foreach (var q in orphanQuestions)
                    {
                        q.CategoryCode = fallbackCode;
                    }

                    // 3. Ngắt liên kết hoàn toàn trên entity bị xóa để EF ChangeTracker không chặn DeleteBehavior.Restrict
                    topic.Children.Clear();
                    topic.Parent = null;
                    topic.ParentId = null;
                    await _db.SaveChangesAsync();

                    _db.BankTopics.Remove(topic);
                    await _db.SaveChangesAsync();
                    if (transaction != null) await transaction.CommitAsync();

                    return Ok(new
                    {
                        success = true,
                        message = $"Đã xóa chủ đề '{topic.Name}'. {directChildren.Count} chủ đề con đã được chuyển lên cấp trên và {orphanQuestions.Count} câu hỏi được chuyển về nhóm '{fallbackCode}'.",
                        promotedChildrenCount = directChildren.Count,
                        reassignedQuestionsCount = orphanQuestions.Count
                    });
                }
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
