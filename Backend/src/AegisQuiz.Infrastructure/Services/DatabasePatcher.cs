using System.Reflection;
using System.Text.RegularExpressions;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [Zero Hard-code & Resilience] Bộ điều phối tự động vá và đồng bộ cấu trúc cơ sở dữ liệu.
    /// Tự động phát hiện các file *.sql từ thư mục cấu hình ngoài (Config/DbPatches)
    /// hoặc fallback qua Embedded Resources trong Assembly.
    /// </summary>
    public class DatabasePatcher : IDatabasePatcher
    {
        private readonly AegisQuizDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<DatabasePatcher> _logger;

        public DatabasePatcher(
            AegisQuizDbContext db,
            IConfiguration config,
            ILogger<DatabasePatcher> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        public async Task<int> ApplyPendingPatchesAsync(CancellationToken cancellationToken = default)
        {
            if (!_db.Database.IsNpgsql())
            {
                _logger.LogInformation("[DatabasePatcher] Non-Postgres database detected, skipping SQL patches.");
                return 0;
            }

            int executedStatements = 0;

            try
            {
                // 1. Đảm bảo bảng lịch sử bản vá luôn tồn tại
                await EnsurePatchHistoryTableExistsAsync(cancellationToken);

                // 2. Thu thập danh sách các bản vá (ưu tiên thư mục Config ngoài -> fallback Embedded)
                var patchSources = GetPatchSources();
                _logger.LogInformation("[DatabasePatcher] Found {Count} patch sources to evaluate.", patchSources.Count);

                var appliedPatches = await GetAppliedPatchesAsync(cancellationToken);

                foreach (var patch in patchSources.OrderBy(p => p.Key))
                {
                    string patchName = patch.Key;
                    string sqlContent = patch.Value;

                    // Bản vá 01_Emergency luôn được chạy để bảo đảm kiểu dữ liệu, các bản vá khác chỉ chạy nếu chưa thành công
                    bool isEmergency = patchName.Contains("Emergency", StringComparison.OrdinalIgnoreCase);
                    if (!isEmergency && appliedPatches.Contains(patchName))
                    {
                        _logger.LogDebug("[DatabasePatcher] Patch '{PatchName}' already applied. Skipping.", patchName);
                        continue;
                    }

                    _logger.LogInformation("[DatabasePatcher] Applying patch '{PatchName}'...", patchName);
                    var statements = SplitSqlStatements(sqlContent);
                    int patchSuccessCount = 0;
                    int patchFailCount = 0;
                    var errorLogs = new List<string>();

                    foreach (var stmt in statements)
                    {
                        if (string.IsNullOrWhiteSpace(stmt)) continue;

                        try
                        {
                            await _db.Database.ExecuteSqlRawAsync(stmt, cancellationToken);
                            patchSuccessCount++;
                            executedStatements++;
                        }
                        catch (Exception ex)
                        {
                            patchFailCount++;
                            string err = $"Failed: {stmt.Trim().Substring(0, Math.Min(60, stmt.Trim().Length))}... -> {ex.Message}";
                            errorLogs.Add(err);
                            _logger.LogWarning("[DatabasePatcher] Statement notice in '{PatchName}': {Message}", patchName, ex.Message);
                        }
                    }

                    bool overallSuccess = patchFailCount == 0 || patchSuccessCount > 0;
                    string logSummary = $"Success: {patchSuccessCount}, Notices: {patchFailCount}. " + string.Join(" | ", errorLogs.Take(3));

                    await RecordPatchExecutionAsync(patchName, overallSuccess, logSummary, cancellationToken);
                    _logger.LogInformation("[DatabasePatcher] Patch '{PatchName}' completed ({SuccessCount} statements executed).", patchName, patchSuccessCount);
                }
            }
            catch (Exception exGlobal)
            {
                _logger.LogError(exGlobal, "[DatabasePatcher] Critical error during database patching: {Message}", exGlobal.Message);
            }

            return executedStatements;
        }

        private async Task EnsurePatchHistoryTableExistsAsync(CancellationToken ct)
        {
            const string createTableSql = @"
                CREATE TABLE IF NOT EXISTS ""__AegisPatchHistory"" (
                    ""PatchFileName"" text PRIMARY KEY,
                    ""AppliedAt"" timestamp with time zone NOT NULL DEFAULT now(),
                    ""Success"" boolean NOT NULL DEFAULT true,
                    ""ExecutionLog"" text
                );";
            await _db.Database.ExecuteSqlRawAsync(createTableSql, ct);
        }

        private async Task<HashSet<string>> GetAppliedPatchesAsync(CancellationToken ct)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var cmd = _db.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = @"SELECT ""PatchFileName"" FROM ""__AegisPatchHistory"" WHERE ""Success"" = true;";
                if (cmd.Connection!.State != System.Data.ConnectionState.Open)
                {
                    await cmd.Connection.OpenAsync(ct);
                }
                using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    set.Add(reader.GetString(0));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[DatabasePatcher] Could not read __AegisPatchHistory: {Message}", ex.Message);
            }
            return set;
        }

        private async Task RecordPatchExecutionAsync(string patchName, bool success, string log, CancellationToken ct)
        {
            const string upsertSql = @"
                INSERT INTO ""__AegisPatchHistory"" (""PatchFileName"", ""AppliedAt"", ""Success"", ""ExecutionLog"")
                VALUES ({0}, now(), {1}, {2})
                ON CONFLICT (""PatchFileName"") DO UPDATE 
                SET ""AppliedAt"" = now(), ""Success"" = EXCLUDED.""Success"", ""ExecutionLog"" = EXCLUDED.""ExecutionLog"";";

            try
            {
                await _db.Database.ExecuteSqlRawAsync(upsertSql, new object[] { patchName, success, log }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[DatabasePatcher] Could not record patch '{PatchName}': {Message}", patchName, ex.Message);
            }
        }

        private Dictionary<string, string> GetPatchSources()
        {
            var sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 1. Kiểm tra thư mục cấu hình trên đĩa
            string configDir = _config["DatabasePatcher:ScriptsDirectory"] ?? "Config/DbPatches";
            string absolutePath = Path.IsPathRooted(configDir) 
                ? configDir 
                : Path.Combine(AppContext.BaseDirectory, configDir);

            if (Directory.Exists(absolutePath))
            {
                var files = Directory.GetFiles(absolutePath, "*.sql", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    try
                    {
                        sources[fileName] = File.ReadAllText(file);
                        _logger.LogInformation("[DatabasePatcher] Loaded disk patch file: {FileName}", fileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[DatabasePatcher] Failed to read disk patch file '{FileName}': {Message}", fileName, ex.Message);
                    }
                }
            }

            // 2. Fallback sang Embedded Resources nếu chưa có file đĩa tương ứng
            var assembly = typeof(AegisQuizDbContext).Assembly;
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(n => n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase));

            foreach (var rName in resourceNames)
            {
                string shortName = rName.Split('.').Reverse().Skip(1).First() + ".sql";
                if (!sources.ContainsKey(shortName))
                {
                    try
                    {
                        using var stream = assembly.GetManifestResourceStream(rName);
                        if (stream != null)
                        {
                            using var reader = new StreamReader(stream);
                            sources[shortName] = reader.ReadToEnd();
                            _logger.LogInformation("[DatabasePatcher] Loaded embedded patch resource: {ResourceName} as {ShortName}", rName, shortName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[DatabasePatcher] Failed to read embedded resource '{ResourceName}': {Message}", rName, ex.Message);
                    }
                }
            }

            return sources;
        }

        private static List<string> SplitSqlStatements(string sql)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(sql)) return list;

            // Loại bỏ comment block /* ... */
            var withoutBlockComments = Regex.Replace(sql, @"/\*[\s\S]*?\*/", "", RegexOptions.Compiled);

            // Tách theo dòng
            var lines = withoutBlockComments.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var sb = new System.Text.StringBuilder();

            foreach (var rawLine in lines)
            {
                var trimmed = rawLine.Trim();
                // Bỏ qua dòng comment đơn --
                if (trimmed.StartsWith("--")) continue;

                sb.AppendLine(rawLine);

                // Nếu kết thúc bằng dấu chấm phẩy ;
                if (trimmed.EndsWith(";"))
                {
                    var stmt = sb.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(stmt))
                    {
                        list.Add(stmt);
                    }
                    sb.Clear();
                }
            }

            var remaining = sb.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(remaining))
            {
                list.Add(remaining);
            }

            return list;
        }
    }
}
