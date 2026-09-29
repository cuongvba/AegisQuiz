using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [Universal Dynamic Question Ingestion Engine — CIG Engine / CHIP v3.0]
    /// Động cơ thông dịch Ngữ pháp Nhận thức Toàn cầu (Cognitive Ingestion Grammar):
    /// - Quản lý quy tắc nhận diện câu hỏi nạp từ <c>parsing_rules_registry.json</c> và PostgreSQL (bảng QuestionParsingRules).
    /// - JIT Regex Compiled Cache, Hot-reload 0ms downtime, Live Sandbox thử nghiệm trên Web.
    /// - Từ điển cấu trúc tài liệu (DocumentStructureDictionary) điều khiển 100% nhận diện phân đoạn, lời giải, bảng đáp án.
    /// - Hồ sơ ngôn ngữ 2 lớp (Host / Subject) và Adaptive Rule Pruning — xem <c>DynamicQuestionIngestionEngine.Language.cs</c>.
    ///
    /// Lớp được tách thành các partial theo mối quan tâm:
    ///   • <c>DynamicQuestionIngestionEngine.cs</c>            — vòng đời quy tắc (nạp / hot-reload / CSDL) & khớp điểm neo.
    ///   • <c>DynamicQuestionIngestionEngine.Structure.cs</c>  — cấu trúc tài liệu (đề, chủ đề, phân đoạn, pha lời giải, bài đọc).
    ///   • <c>DynamicQuestionIngestionEngine.Extraction.cs</c> — trích xuất phương án, đáp án inline, lời giải & Sandbox.
    ///   • <c>DynamicQuestionIngestionEngine.Language.cs</c>   — nhận diện ngôn ngữ & lọc quy tắc thích ứng.
    /// </summary>
    public partial class DynamicQuestionIngestionEngine : IDynamicQuestionIngestionEngine
    {
        private const string RegistryFileName = "parsing_rules_registry.json";
        private const string RegistryVersion = "3.0.0";

        private static readonly Lazy<DynamicQuestionIngestionEngine> _instance =
            new(() => new DynamicQuestionIngestionEngine());

        /// <summary>Singleton dùng chung cho toàn hệ thống (Classifier, Controller, Excel/PDF pipelines).</summary>
        public static DynamicQuestionIngestionEngine Instance => _instance.Value;

        private IServiceScopeFactory? _scopeFactory;
        private ILogger<DynamicQuestionIngestionEngine>? _logger;

        private readonly ConcurrentDictionary<string, Regex> _regexCache = new(StringComparer.Ordinal);
        private readonly List<QuestionParsingRuleDto> _rules = new();
        private DocumentStructureDictionaryDto _structureDictionary = new();
        private readonly object _syncLock = new();

        /// <summary>Từ điển cấu trúc tài liệu đang hiệu lực (chỉ đọc).</summary>
        public DocumentStructureDictionaryDto StructureDictionary => _structureDictionary;

        public DynamicQuestionIngestionEngine(IServiceScopeFactory? scopeFactory = null, ILogger<DynamicQuestionIngestionEngine>? logger = null)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            LoadDefaultRules();
        }

        /// <summary>
        /// Gắn hạ tầng (DI ScopeFactory + Logger) vào singleton <see cref="Instance"/> để bật đồng bộ quy tắc với CSDL.
        /// Trả về chính instance để đăng ký DI dạng fluent.
        /// </summary>
        public DynamicQuestionIngestionEngine AttachInfrastructure(IServiceScopeFactory scopeFactory, ILogger<DynamicQuestionIngestionEngine>? logger = null)
        {
            _scopeFactory = scopeFactory;
            _logger ??= logger;
            return this;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 1. Nạp & Hot-reload quy tắc
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Nạp các quy tắc mặc định từ file cấu hình <c>parsing_rules_registry.json</c> (fallback bộ quy tắc lõi nếu thiếu).</summary>
        public void LoadDefaultRules()
        {
            try
            {
                string? registryPath = ResolveRegistryPath();
                if (registryPath != null)
                {
                    string json = File.ReadAllText(registryPath);
                    if (!HotReloadRules(json) || _rules.Count == 0)
                    {
                        LoadHardcodedFallbackRules();
                    }
                    _logger?.LogInformation("[DynamicIngestionEngine] LoadDefaultRules | Path={Path} -> {Count} rules", registryPath, _rules.Count);
                }
                else
                {
                    LoadHardcodedFallbackRules();
                    _logger?.LogWarning("[DynamicIngestionEngine] LoadDefaultRules | RegistryFile=NotFound -> Fallback to {Count} built-in rules", _rules.Count);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[DynamicIngestionEngine] LoadDefaultRules | Error={Error} -> Fallback to built-in rules", ex.Message);
                LoadHardcodedFallbackRules();
            }
        }

        /// <summary>
        /// Hot-reload toàn bộ quy tắc (và từ điển cấu trúc nếu có) từ chuỗi JSON — 0ms downtime.
        /// Trả về true khi có ít nhất một quy tắc được nạp.
        /// </summary>
        public bool HotReloadRules(string jsonConfig)
        {
            lock (_syncLock)
            {
                try
                {
                    using var doc = JsonDocument.Parse(jsonConfig);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    if (doc.RootElement.TryGetProperty("documentStructureDictionary", out var dictElement))
                    {
                        var dictionary = JsonSerializer.Deserialize<DocumentStructureDictionaryDto>(dictElement.GetRawText(), options);
                        if (dictionary != null)
                        {
                            _structureDictionary = dictionary;
                        }
                    }

                    if (doc.RootElement.TryGetProperty("rules", out var rulesArray))
                    {
                        var parsedRules = JsonSerializer.Deserialize<List<QuestionParsingRuleDto>>(rulesArray.GetRawText(), options);
                        if (parsedRules != null && parsedRules.Count > 0)
                        {
                            _rules.Clear();
                            _rules.AddRange(parsedRules.Where(r => r.IsActive).OrderBy(r => r.Priority));
                            _regexCache.Clear();
                            foreach (var rule in _rules)
                            {
                                PrecompileRule(rule);
                            }

                            _logger?.LogInformation("[DynamicIngestionEngine] HotReloadRules | RulesCount={Count} -> Loaded dynamically with 0ms downtime", _rules.Count);
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[DynamicIngestionEngine] HotReloadRules | Error={Error} -> Hot reload aborted", ex.Message);
                }
                return false;
            }
        }

        public IReadOnlyList<QuestionParsingRuleDto> GetAllRules(Guid? tenantId = null) => SnapshotRules();

        public IReadOnlyList<QuestionParsingRuleDto> GetAllRules() => GetAllRules(null);

        /// <summary>Thêm mới hoặc cập nhật một quy tắc vào bộ nhớ + JIT Regex cache (tùy chọn ghi xuống file registry).</summary>
        public void AddOrUpdateRule(QuestionParsingRuleDto newRule, bool persistToFile)
        {
            AddOrUpdateRuleInternal(newRule, persistToFile);
        }

        public bool AddOrUpdateRule(QuestionParsingRuleDto newRule) => AddOrUpdateRuleInternal(newRule, persistToFile: false);

        private bool AddOrUpdateRuleInternal(QuestionParsingRuleDto newRule, bool persistToFile)
        {
            if (newRule == null || string.IsNullOrWhiteSpace(newRule.RuleCode)) return false;

            lock (_syncLock)
            {
                _rules.RemoveAll(r => string.Equals(r.RuleCode, newRule.RuleCode, StringComparison.OrdinalIgnoreCase));
                _rules.Add(newRule);
                _rules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                PrecompileRule(newRule);

                _logger?.LogInformation("[DynamicIngestionEngine] UpsertRuleInMemory | RuleCode={RuleCode} | Name={RuleName} -> Registered and cached", newRule.RuleCode, newRule.RuleName);

                if (persistToFile)
                {
                    PersistRegistryToFile();
                }
                return true;
            }
        }

        /// <summary>Ghi toàn bộ trạng thái hiện tại (quy tắc + từ điển cấu trúc) xuống file registry — bảo toàn từ điển khi lưu.</summary>
        private void PersistRegistryToFile()
        {
            try
            {
                string? registryPath = ResolveRegistryPath();
                if (registryPath == null) return;

                var registryObj = new
                {
                    version = RegistryVersion,
                    lastUpdated = DateTime.UtcNow.ToString("o"),
                    documentStructureDictionary = _structureDictionary,
                    rules = _rules
                };
                var json = JsonSerializer.Serialize(registryObj, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                File.WriteAllText(registryPath, json);
                _logger?.LogInformation("[DynamicIngestionEngine] PersistRuleToFile | FilePath={FilePath} -> Persisted {Count} rules", registryPath, _rules.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[DynamicIngestionEngine] PersistRuleToFile | Warning={Error} -> File persistence skipped", ex.Message);
            }
        }

        /// <summary>
        /// Tìm file registry theo thứ tự: thư mục chạy (Config/), thư mục làm việc, rồi đi ngược cây thư mục
        /// để tìm <c>src/AegisQuiz.Infrastructure/Config/</c> (hỗ trợ chạy test / dotnet run từ bất kỳ thư mục con của repo).
        /// </summary>
        private static string? ResolveRegistryPath()
        {
            var candidates = new List<string>
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", RegistryFileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Config", RegistryFileName)
            };

            foreach (var root in new[] { Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.BaseDirectory })
            {
                var dir = new DirectoryInfo(root);
                for (int depth = 0; dir != null && depth < 8; depth++, dir = dir.Parent)
                {
                    candidates.Add(Path.Combine(dir.FullName, "src", "AegisQuiz.Infrastructure", "Config", RegistryFileName));
                    candidates.Add(Path.Combine(dir.FullName, "Backend", "src", "AegisQuiz.Infrastructure", "Config", RegistryFileName));
                }
            }

            return candidates.FirstOrDefault(File.Exists);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2. Đồng bộ quy tắc với PostgreSQL (bảng QuestionParsingRules)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Đồng bộ quy tắc từ CSDL vào bộ nhớ. Nếu bảng trống → seed bộ quy tắc hiện tại xuống CSDL.
        /// Quy tắc CSDL trùng RuleCode sẽ ghi đè các trường biên tập (tên, mô tả, ưu tiên, 4 spec) nhưng giữ nguyên
        /// ngôn ngữ / lifecycle của quy tắc gốc trong file để không phá vỡ Adaptive Rule Pruning.
        /// </summary>
        public async Task<int> SyncRulesFromDatabaseAsync(CancellationToken ct = default)
        {
            if (_scopeFactory == null) return 0;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();

                if (db.Database.IsRelational())
                {
                    await db.Database.ExecuteSqlRawAsync(EnsureRulesTableSql, ct);
                }

                var dbRules = await db.QuestionParsingRules.AsNoTracking()
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.Priority)
                    .ToListAsync(ct);

                if (dbRules.Count == 0)
                {
                    await SeedDefaultRulesToDatabaseAsync(db, ct);
                    return _rules.Count;
                }

                lock (_syncLock)
                {
                    int synced = 0;
                    foreach (var entity in dbRules)
                    {
                        var dto = MapEntityToDto(entity, _rules.FirstOrDefault(r => string.Equals(r.RuleCode, entity.RuleCode, StringComparison.OrdinalIgnoreCase)));
                        _rules.RemoveAll(r => string.Equals(r.RuleCode, dto.RuleCode, StringComparison.OrdinalIgnoreCase));
                        _rules.Add(dto);
                        PrecompileRule(dto);
                        synced++;
                    }
                    _rules.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                    _logger?.LogInformation("[DynamicIngestionEngine] SyncRulesFromDatabase | Count={Count} -> Synced from PostgreSQL", synced);
                    return synced;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[DynamicIngestionEngine] SyncRulesFromDatabase | Warning={Message} -> Keeping in-memory rules", ex.Message);
                return 0;
            }
        }

        private async Task SeedDefaultRulesToDatabaseAsync(AegisQuizDbContext db, CancellationToken ct)
        {
            try
            {
                List<QuestionParsingRule> entities;
                lock (_syncLock)
                {
                    entities = _rules.Select(rule => MapDtoToEntity(rule, new QuestionParsingRule { TenantId = Guid.Empty })).ToList();
                }

                await db.QuestionParsingRules.AddRangeAsync(entities, ct);
                await db.SaveChangesAsync(ct);
                _logger?.LogInformation("[DynamicIngestionEngine] SeedDefaultRules | Count={Count} -> Seeded into PostgreSQL", entities.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[DynamicIngestionEngine] SeedDefaultRules | Warning={Message}", ex.Message);
            }
        }

        /// <summary>Lưu quy tắc vào bộ nhớ + file, đồng thời upsert xuống CSDL (nếu đã gắn hạ tầng).</summary>
        public async Task<bool> SaveRuleToDatabaseAsync(QuestionParsingRuleDto ruleDto, Guid? tenantId = null, CancellationToken ct = default)
        {
            AddOrUpdateRule(ruleDto, persistToFile: true);
            if (_scopeFactory == null) return true;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AegisQuizDbContext>();
                var targetTenantId = tenantId ?? Guid.Empty;

                var existing = await db.QuestionParsingRules
                    .FirstOrDefaultAsync(r => r.RuleCode == ruleDto.RuleCode && (r.TenantId == targetTenantId || r.TenantId == Guid.Empty), ct);

                if (existing != null)
                {
                    MapDtoToEntity(ruleDto, existing);
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    db.QuestionParsingRules.Add(MapDtoToEntity(ruleDto, new QuestionParsingRule { TenantId = targetTenantId }));
                }

                await db.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[DynamicIngestionEngine] SaveRuleToDatabase | RuleCode={RuleCode} | Warning={Message}", ruleDto.RuleCode, ex.Message);
                return false;
            }
        }

        private static QuestionParsingRule MapDtoToEntity(QuestionParsingRuleDto dto, QuestionParsingRule entity)
        {
            entity.RuleCode = dto.RuleCode;
            entity.RuleName = dto.RuleName;
            entity.Description = dto.Description;
            entity.Priority = dto.Priority;
            entity.IsActive = dto.IsActive;
            entity.Scope = string.IsNullOrWhiteSpace(dto.TargetExamType) ? "GLOBAL" : dto.TargetExamType;
            entity.AnchorSpecJson = JsonSerializer.Serialize(dto.AnchorSpec);
            entity.InteractionSpecJson = JsonSerializer.Serialize(dto.InteractionSpec);
            entity.EvaluationSpecJson = JsonSerializer.Serialize(dto.EvaluationSpec);
            entity.SemanticsSpecJson = JsonSerializer.Serialize(dto.StimulusSpec);
            return entity;
        }

        private static QuestionParsingRuleDto MapEntityToDto(QuestionParsingRule entity, QuestionParsingRuleDto? baseRule)
        {
            var dto = baseRule ?? new QuestionParsingRuleDto
            {
                TargetExamType = string.IsNullOrWhiteSpace(entity.Scope) ? "GENERAL" : entity.Scope
            };

            dto.RuleCode = entity.RuleCode;
            dto.RuleName = entity.RuleName;
            dto.Description = entity.Description;
            dto.Priority = entity.Priority;
            dto.IsActive = entity.IsActive;
            dto.AnchorSpec = SafeDeserialize<AnchorSpecDto>(entity.AnchorSpecJson) ?? dto.AnchorSpec;
            dto.InteractionSpec = SafeDeserialize<InteractionSpecDto>(entity.InteractionSpecJson) ?? dto.InteractionSpec;
            dto.EvaluationSpec = SafeDeserialize<EvaluationSpecDto>(entity.EvaluationSpecJson) ?? dto.EvaluationSpec;
            dto.StimulusSpec = SafeDeserialize<StimulusSpecDto>(entity.SemanticsSpecJson) ?? dto.StimulusSpec;
            return dto;
        }

        private static T? SafeDeserialize<T>(string? json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json) || json == "{}") return null;
            try
            {
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return null;
            }
        }

        private const string EnsureRulesTableSql = @"
            CREATE TABLE IF NOT EXISTS ""QuestionParsingRules"" (
                ""Id"" uuid NOT NULL PRIMARY KEY,
                ""TenantId"" uuid NOT NULL,
                ""RuleCode"" character varying(100) NOT NULL,
                ""RuleName"" character varying(250) NOT NULL,
                ""Description"" character varying(1000) NULL,
                ""Priority"" integer NOT NULL DEFAULT 100,
                ""IsActive"" boolean NOT NULL DEFAULT true,
                ""Scope"" text NOT NULL DEFAULT 'GLOBAL',
                ""AnchorSpecJson"" text NOT NULL,
                ""InteractionSpecJson"" text NOT NULL,
                ""EvaluationSpecJson"" text NOT NULL,
                ""SemanticsSpecJson"" text NOT NULL,
                ""HitCount"" bigint NOT NULL DEFAULT 0,
                ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                ""UpdatedAt"" timestamp with time zone NULL
            );
            ALTER TABLE ""QuestionParsingRules"" ADD COLUMN IF NOT EXISTS ""Scope"" text NOT NULL DEFAULT 'GLOBAL';
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_QuestionParsingRules_TenantId_RuleCode"" ON ""QuestionParsingRules"" (""TenantId"", ""RuleCode"");";

        // ─────────────────────────────────────────────────────────────────────
        // 3. Khớp điểm neo câu hỏi, phương án, đáp án, lời giải, điểm liệt
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Khớp điểm neo nhận diện câu hỏi từ một dòng văn bản.
        /// Duyệt theo <paramref name="activeRules"/> (tập đã lọc theo hồ sơ ngôn ngữ) hoặc toàn bộ quy tắc theo Priority tăng dần.
        /// Quy tắc mang ngôn ngữ yêu cầu bằng chứng (Hangul, Hanzi, Kana, dấu Pháp/Đức...) sẽ bị bỏ qua nếu dòng không có bằng chứng đó.
        /// </summary>
        public bool TryMatchQuestionAnchor(string line, out int questionNumber, out QuestionParsingRuleDto? matchedRule, IEnumerable<QuestionParsingRuleDto>? activeRules = null)
        {
            questionNumber = 0;
            matchedRule = null;
            if (string.IsNullOrWhiteSpace(line)) return false;

            var candidates = activeRules != null ? activeRules.ToList() : SnapshotRules();
            foreach (var rule in candidates)
            {
                if (!HasLanguageEvidenceForAnchor(rule.Language, line)) continue;

                var match = GetOrAddRegex(rule.AnchorSpec.RegexPattern).Match(line);
                if (!match.Success) continue;

                matchedRule = rule;
                if (rule.AnchorSpec.QuestionNumberGroupIndex <= match.Groups.Count - 1)
                {
                    int.TryParse(match.Groups[rule.AnchorSpec.QuestionNumberGroupIndex].Value, out questionNumber);
                }
                return true;
            }
            return false;
        }

        /// <summary>Khớp một phương án trả lời (Option) theo quy tắc.</summary>
        public bool TryMatchOption(string line, QuestionParsingRuleDto rule, out string label, out string content)
        {
            label = string.Empty;
            content = string.Empty;

            var match = GetOrAddRegex(rule.InteractionSpec.OptionRegexPattern).Match(line);
            if (!match.Success) return false;

            int labelIdx = rule.InteractionSpec.OptionLabelGroupIndex;
            if (labelIdx <= match.Groups.Count - 1 && !string.IsNullOrEmpty(match.Groups[labelIdx].Value))
            {
                label = match.Groups[labelIdx].Value.Trim();
            }
            else
            {
                for (int i = 1; i <= Math.Min(3, match.Groups.Count - 1); i++)
                {
                    if (!string.IsNullOrEmpty(match.Groups[i].Value))
                    {
                        label = match.Groups[i].Value.Trim();
                        break;
                    }
                }
            }

            int contentIdx = rule.InteractionSpec.OptionContentGroupIndex;
            content = contentIdx <= match.Groups.Count - 1 && !string.IsNullOrEmpty(match.Groups[contentIdx].Value)
                ? match.Groups[contentIdx].Value.Trim()
                : line.Substring(match.Length).Trim();
            return true;
        }

        /// <summary>Khớp dòng đáp án đúng (Answer Key) theo quy tắc.</summary>
        public bool TryMatchAnswer(string line, QuestionParsingRuleDto rule, out string answer)
        {
            answer = string.Empty;
            var match = GetOrAddRegex(rule.EvaluationSpec.AnswerRegexPattern).Match(line);
            if (!match.Success) return false;

            int groupIdx = rule.EvaluationSpec.AnswerGroupIndex;
            answer = groupIdx <= match.Groups.Count - 1 && !string.IsNullOrEmpty(match.Groups[groupIdx].Value)
                ? match.Groups[groupIdx].Value.Trim()
                : match.Value.Trim();
            return true;
        }

        /// <summary>Nhận diện dòng bắt đầu phần Lời giải / Hướng dẫn giải theo quy tắc.</summary>
        public bool TryMatchExplanation(string line, QuestionParsingRuleDto rule)
        {
            if (string.IsNullOrEmpty(rule.EvaluationSpec.ExplanationRegexPattern)) return false;
            return GetOrAddRegex(rule.EvaluationSpec.ExplanationRegexPattern).IsMatch(line);
        }

        /// <summary>
        /// Nhận diện câu điểm liệt / câu bắt buộc đúng: theo quy tắc khớp → toàn bộ quy tắc → từ điển cấu trúc → mẫu lõi dự phòng.
        /// </summary>
        public bool IsCriticalQuestion(string text, QuestionParsingRuleDto? rule = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            if (rule != null && MatchesAnyCriticalMarker(text, rule)) return true;

            foreach (var r in SnapshotRules())
            {
                if (MatchesAnyCriticalMarker(text, r)) return true;
            }

            if (MatchesAny(text, _structureDictionary.CriticalQuestionMarkers)) return true;

            return GetOrAddRegex(BuiltInCriticalMarkerPattern).IsMatch(text);
        }

        private bool MatchesAnyCriticalMarker(string text, QuestionParsingRuleDto rule)
        {
            var spec = rule.EvaluationSpec;
            if (spec == null) return false;
            if (!string.IsNullOrEmpty(spec.CriticalQuestionMarkerRegex) && GetOrAddRegex(spec.CriticalQuestionMarkerRegex).IsMatch(text)) return true;
            return MatchesAny(text, spec.CriticalMarkers);
        }

        private const string BuiltInCriticalMarkerPattern =
            @"(?:\bDIEM[_\s]*LIET\b|\bĐIỂM[_\s]*LIỆT\b|mất\s*an\s*toàn\s*giao\s*thông\s*nghiêm\s*trọng|bị\s*nghiêm\s*cấm|\bnghiêm\s*cấm\b|nồng\s*độ\s*cồn|sử\s*dụng\s*ma\s*túy|đua\s*xe\s*trái\s*phép|\bFATAL\b|\bCRITICAL\b)";

        // ─────────────────────────────────────────────────────────────────────
        // 4. Hạ tầng dùng chung: snapshot, regex cache, tiện ích khớp
        // ─────────────────────────────────────────────────────────────────────

        private List<QuestionParsingRuleDto> SnapshotRules()
        {
            lock (_syncLock)
            {
                return _rules.ToList();
            }
        }

        private Regex GetOrAddRegex(string pattern)
            => _regexCache.GetOrAdd(pattern, p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant));

        private void CompileAndCache(string? pattern)
        {
            if (!string.IsNullOrWhiteSpace(pattern))
            {
                _ = GetOrAddRegex(pattern);
            }
        }

        private void PrecompileRule(QuestionParsingRuleDto rule)
        {
            CompileAndCache(rule.AnchorSpec?.RegexPattern);
            CompileAndCache(rule.InteractionSpec?.OptionRegexPattern);
            CompileAndCache(rule.EvaluationSpec?.AnswerRegexPattern);
        }

        /// <summary>True nếu văn bản khớp bất kỳ mẫu regex nào trong danh sách (bỏ qua mẫu rỗng / lỗi cú pháp).</summary>
        private bool MatchesAny(string text, IEnumerable<string>? patterns)
        {
            if (patterns == null) return false;
            foreach (var pattern in patterns)
            {
                if (string.IsNullOrWhiteSpace(pattern)) continue;
                try
                {
                    if (GetOrAddRegex(pattern).IsMatch(text)) return true;
                }
                catch (ArgumentException ex)
                {
                    _logger?.LogWarning("[DynamicIngestionEngine] InvalidPattern | Pattern={Pattern} -> Skipped: {Error}", pattern, ex.Message);
                }
            }
            return false;
        }

        /// <summary>Trả về giá trị Group 1 (hoặc toàn bộ match) của mẫu đầu tiên khớp; null nếu không mẫu nào khớp.</summary>
        private string? FirstCapture(string text, IEnumerable<string>? patterns)
        {
            if (patterns == null) return null;
            foreach (var pattern in patterns)
            {
                if (string.IsNullOrWhiteSpace(pattern)) continue;
                try
                {
                    var match = GetOrAddRegex(pattern).Match(text);
                    if (match.Success)
                    {
                        return match.Groups.Count > 1 && match.Groups[1].Success ? match.Groups[1].Value.Trim() : match.Value.Trim();
                    }
                }
                catch (ArgumentException ex)
                {
                    _logger?.LogWarning("[DynamicIngestionEngine] InvalidPattern | Pattern={Pattern} -> Skipped: {Error}", pattern, ex.Message);
                }
            }
            return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 5. Bộ quy tắc lõi dự phòng (chỉ dùng khi không có file registry)
        // ─────────────────────────────────────────────────────────────────────

        private void LoadHardcodedFallbackRules()
        {
            _rules.Clear();
            _rules.AddRange(GetDefaultFallbackRules());
            _regexCache.Clear();
            foreach (var rule in _rules)
            {
                PrecompileRule(rule);
            }
        }

        private static List<QuestionParsingRuleDto> GetDefaultFallbackRules()
        {
            return new List<QuestionParsingRuleDto>
            {
                new()
                {
                    RuleCode = "VN_STANDARD_MULTIPLE_CHOICE",
                    RuleName = "Trắc Nghiệm Đơn Tiêu Chuẩn (A/B/C/D)",
                    TargetExamType = "GENERAL",
                    Language = "vi",
                    Priority = 10,
                    AnchorSpec = new AnchorSpecDto { RegexPattern = @"^(?:Câu|Question|Item|Bài|Q)\s*(\d+)", QuestionNumberGroupIndex = 1 },
                    InteractionSpec = new InteractionSpecDto
                    {
                        DefaultQuestionType = "SINGLE",
                        OptionRegexPattern = @"^(?:([A-D])|[(]([A-D])[)])[\.\:\)\s\-]+(.*)$",
                        OptionLabelGroupIndex = 1,
                        OptionContentGroupIndex = 3
                    },
                    EvaluationSpec = new EvaluationSpecDto
                    {
                        AnswerRegexPattern = @"^(?:ĐÁP\s*ÁN|ANSWER|KEY|CHỌN)[\.\:\s\-]+([A-D0-9a-d\,\s]+)",
                        AnswerGroupIndex = 1,
                        ExplanationRegexPattern = @"^(?:Lời\s*giải|Hướng\s*dẫn|HD|Giải\s*thích|Explanation|Căn\s*cứ)\b",
                        CriticalQuestionMarkerRegex = @"(?:\bĐIỂM\s*LIỆT\b|⚠️|TỬ\s*THẦN)"
                    }
                },
                new()
                {
                    RuleCode = "VN_GDPT_2025_TRUE_FALSE_MATRIX",
                    RuleName = "Trắc Nghiệm Đúng / Sai Đa Mệnh Đề (GDPT 2025)",
                    TargetExamType = "GDPT_2025",
                    Language = "vi",
                    Priority = 20,
                    AnchorSpec = new AnchorSpecDto { RegexPattern = @"^(?:Câu|Question)\s*(\d+)", QuestionNumberGroupIndex = 1 },
                    InteractionSpec = new InteractionSpecDto
                    {
                        DefaultQuestionType = "TRUE_FALSE",
                        OptionRegexPattern = @"^(?:([a-d])|[(]([a-d])[)])[\.\:\)\s\-]+(.*)$",
                        OptionLabelGroupIndex = 1,
                        OptionContentGroupIndex = 3
                    },
                    EvaluationSpec = new EvaluationSpecDto
                    {
                        AnswerRegexPattern = @"^(?:ĐÁP\s*ÁN|ANSWER|KEY)[\.\:\s\-]+([a-d\:\sĐSđsTFtf\,\;]+)",
                        AnswerGroupIndex = 1,
                        ExplanationRegexPattern = @"^(?:Lời\s*giải|Hướng\s*dẫn|HD|Giải\s*thích)\b"
                    }
                },
                new()
                {
                    RuleCode = "VN_GDPT_2025_SHORT_ANSWER",
                    RuleName = "Trắc Nghiệm Trả Lời Ngắn / Điền Số (GDPT 2025)",
                    TargetExamType = "GDPT_2025",
                    Language = "vi",
                    Priority = 30,
                    AnchorSpec = new AnchorSpecDto { RegexPattern = @"^(?:Câu|Question)\s*(\d+)", QuestionNumberGroupIndex = 1 },
                    InteractionSpec = new InteractionSpecDto
                    {
                        DefaultQuestionType = "SHORT_ANSWER",
                        OptionRegexPattern = @"^(?:([A-D])|[(]([A-D])[)])[\.\:\s\-]+(.*)$"
                    },
                    EvaluationSpec = new EvaluationSpecDto
                    {
                        AnswerRegexPattern = @"^(?:ĐÁP\s*ÁN|ANSWER|KEY|KẾT\s*QUẢ)[\.\:\s\-]+([\-\+]?\d+[\,\.]?\d*)",
                        AnswerGroupIndex = 1
                    }
                },
                new()
                {
                    RuleCode = "GPLX_NATIONAL_DRIVING_TEST",
                    RuleName = "Sát Hạch Lái Xe Quốc Gia (GPLX 600 Câu & Điểm Liệt)",
                    TargetExamType = "GPLX",
                    Language = "vi",
                    Priority = 15,
                    AnchorSpec = new AnchorSpecDto { RegexPattern = @"^(?:Câu|Question)\s*(\d+)", QuestionNumberGroupIndex = 1 },
                    InteractionSpec = new InteractionSpecDto
                    {
                        DefaultQuestionType = "SINGLE",
                        OptionRegexPattern = @"^(?:([1-4])|([A-D]))[\.\:\s\-]+(.*)$",
                        OptionLabelGroupIndex = 1,
                        OptionContentGroupIndex = 3
                    },
                    EvaluationSpec = new EvaluationSpecDto
                    {
                        AnswerRegexPattern = @"^(?:ĐÁP\s*ÁN|CHỌN)[\.\:\s\-]+([1-4A-D])",
                        AnswerGroupIndex = 1,
                        CriticalQuestionMarkerRegex = @"(?:\bĐIỂM\s*LIỆT\b|⚠️|TỬ\s*THẦN)"
                    }
                },
                new()
                {
                    RuleCode = "INTERNATIONAL_SAT_LOGIC",
                    RuleName = "Khảo Thí Quốc Tế SAT / GRE Logic (Format Không Dấu Chấm)",
                    TargetExamType = "SAT",
                    Language = "en",
                    Priority = 5,
                    AnchorSpec = new AnchorSpecDto { RegexPattern = @"^(?:Task|Problem)\s*#?(\d+)", QuestionNumberGroupIndex = 1 },
                    InteractionSpec = new InteractionSpecDto
                    {
                        DefaultQuestionType = "SINGLE",
                        OptionRegexPattern = @"^\[([A-D])\]\s*(.*)$",
                        OptionLabelGroupIndex = 1,
                        OptionContentGroupIndex = 2
                    },
                    EvaluationSpec = new EvaluationSpecDto
                    {
                        AnswerRegexPattern = @"^\[(?:Key|Truth|Answer)\s*\:\s*([A-D])\]",
                        AnswerGroupIndex = 1,
                        ExplanationRegexPattern = @"^\[(?:Rationale|Explanation)\]"
                    }
                }
            };
        }
    }

    /// <summary>Một phương án được bóc tách từ dòng chứa nhiều phương án liền nhau (A. ... B. ...).</summary>
    public class InlineOptionMatch
    {
        public char Letter { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsMarkedCorrect { get; set; }
        public int StartIndex { get; set; }
    }
}
