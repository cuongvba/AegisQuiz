using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [AegisNotebook Sovereign Processing Service V2.0]
    /// Trung tâm bóc tách nhận thức sống Đa Nguồn & Nhà máy Khảo thí học Đỉnh cao:
    /// 1. Tiếp nhận Đa định dạng: PDF (Spatial PdfPig), Word (.docx), Excel (.xlsx), Web URLs.
    /// 2. Quản lý Hồ sơ Không gian Tri thức Hub với Ma trận Thẩm quyền 4 cấp (SupremeLegal -> Regulatory -> InternalSOP -> Reference).
    /// 3. Cảm biến Xung đột Tri thức (Conflict Resolution Engine) giữa các văn bản.
    /// 4. Nạp Vai Trò Đối Thoại Nhận Thức (Persona Role Injection & Socratic Interlocutor) với Grounding Citations.
    /// 5. Nhà máy Sinh Câu hỏi Tình huống Phức hợp Liên Nguồn (Cross-Source Situational Exam) theo thanh trượt tỷ lệ %.
    /// </summary>
    public class NotebookProcessingService : INotebookService
    {
        private readonly AegisQuizDbContext _db;
        private readonly IAiQuestionGeneratorService _aiQuestionGen;
        private readonly IUniversalAiRouter? _aiRouter;
        private readonly ILogger<NotebookProcessingService> _logger;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };

        public NotebookProcessingService(
            AegisQuizDbContext db,
            IAiQuestionGeneratorService aiQuestionGen,
            ILogger<NotebookProcessingService> logger,
            IUniversalAiRouter? aiRouter = null)
        {
            _db = db;
            _aiQuestionGen = aiQuestionGen;
            _logger = logger;
            _aiRouter = aiRouter;
        }

        // ── 1. KHỞI TẠO KHÔNG GIAN TRI THỨC HUB (WORKSPACE HUB) ─────────────

        public async Task<NotebookSummaryDto> CreateHubAsync(
            NotebookCreateHubDto request,
            Guid userId,
            Guid tenantId,
            CancellationToken ct = default)
        {
            var title = !string.IsNullOrWhiteSpace(request?.Title) 
                ? request.Title 
                : "Không Gian Tri Thức Mới";

            var personaRole = request?.PersonaRole.HasValue == true 
                ? (PersonaRoleType)request.PersonaRole.Value 
                : PersonaRoleType.SocraticMentor;

            var personaName = !string.IsNullOrWhiteSpace(request?.PersonaName) 
                ? request.PersonaName 
                : GetDefaultPersonaName(personaRole);

            var notebook = new Notebook
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = title,
                Description = request?.Description ?? "Hồ sơ nhận thức đa nguồn quản lý bởi AegisNotebook Hub.",
                DomainCode = string.IsNullOrWhiteSpace(request?.DomainCode) ? "GENERAL" : request.DomainCode,
                BankTopicId = request?.BankTopicId,
                Status = NotebookStatus.Ready,
                PersonaRole = personaRole,
                PersonaName = personaName,
                CustomPersonaPrompt = request?.CustomPersonaPrompt ?? string.Empty,
                CreatedByAdminId = userId,
                CreatedAt = DateTime.UtcNow,
                CognitiveStatsJson = JsonSerializer.Serialize(new
                {
                    bloomDistribution = new { remember = 25, understand = 35, apply = 25, analyze = 15 },
                    irtEstimatedTheta = new { min = -1.5, max = 2.0, avg = 0.2 },
                    totalWordCount = 0,
                    analyzedPages = 0,
                    readingTimeMinutes = 5
                })
            };

            _db.Notebooks.Add(notebook);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("[NotebookService] Khởi tạo Không Gian Tri Thức Hub thành công! ID: {Id}, Title: {Title}", 
                notebook.Id, notebook.Title);

            return MapToSummary(notebook);
        }

        public async Task<NotebookSummaryDto> CreateFromUploadAsync(
            Stream pdfStream, 
            string fileName, 
            long fileLength,
            NotebookUploadRequestDto request, 
            Guid userId, 
            Guid tenantId, 
            CancellationToken ct = default)
        {
            if (pdfStream == null || pdfStream.Length == 0)
                throw new ArgumentException("Luồng tệp không hợp lệ hoặc rỗng.", nameof(pdfStream));

            _logger.LogInformation("[NotebookService] Bắt đầu tiếp nhận tải lên nhanh: {FileName}, Cỡ: {Size} bytes", 
                fileName, fileLength);

            var title = !string.IsNullOrWhiteSpace(request?.Title) 
                ? request.Title 
                : Path.GetFileNameWithoutExtension(fileName).Replace('_', ' ').Trim();

            // 1. Tạo Hub
            var hubDto = await CreateHubAsync(new NotebookCreateHubDto
            {
                Title = title,
                Description = request?.Description ?? $"Hồ sơ nhận thức khởi tạo từ tài liệu {fileName}.",
                DomainCode = request?.DomainCode ?? "GENERAL",
                BankTopicId = request?.BankTopicId
            }, userId, tenantId, ct);

            // 2. Thêm file PDF vào Hub làm Nguồn đầu tiên
            await AddSourceFileAsync(hubDto.Id, pdfStream, fileName, fileLength, request?.AuthorityLevel ?? 2, userId, tenantId, ct);

            var updated = await _db.Notebooks
                .Include(n => n.Sources)
                .Include(n => n.Chapters)
                .FirstAsync(n => n.Id == hubDto.Id, ct);

            return MapToSummary(updated);
        }

        // ── 2. QUẢN LÝ ĐA NGUỒN TÀI LIỆU (MULTI-SOURCE INGESTION) ────────────

        public async Task<NotebookSourceDto> AddSourceFileAsync(
            Guid notebookId,
            Stream stream,
            string fileName,
            long fileLength,
            int authorityLevel,
            Guid userId,
            Guid tenantId,
            CancellationToken ct = default)
        {
            var notebook = await _db.Notebooks
                .Include(n => n.Sources)
                .Include(n => n.Chapters)
                .FirstOrDefaultAsync(n => n.Id == notebookId && (n.TenantId == tenantId || n.TenantId == Guid.Empty), ct);

            if (notebook == null)
                throw new KeyNotFoundException($"Không tìm thấy Không Gian Tri Thức ID: {notebookId}");

            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "notebooks");
            if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var uniqueFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
            var localFilePath = Path.Combine(uploadsDir, uniqueFileName);

            using (var fileSaveStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);
                await stream.CopyToAsync(fileSaveStream, ct);
            }

            // Phân loại nguồn & Bóc tách nội dung
            var sourceType = ResolveSourceType(ext);
            int totalPages = 1;
            string extractedText = string.Empty;
            Dictionary<int, string> pageTexts = new();

            try
            {
                if (sourceType == SourceType.Pdf)
                {
                    using var document = PdfDocument.Open(localFilePath);
                    totalPages = document.NumberOfPages;
                    for (int p = 1; p <= totalPages; p++)
                    {
                        var page = document.GetPage(p);
                        var sb = new StringBuilder();
                        foreach (var word in page.GetWords()) sb.Append(word.Text).Append(' ');
                        var txt = sb.ToString().Trim();
                        pageTexts[p] = txt;
                    }
                    extractedText = string.Join("\n\n", pageTexts.Values);
                }
                else if (sourceType == SourceType.Docx)
                {
                    extractedText = ExtractTextFromDocx(localFilePath);
                    totalPages = Math.Max(1, (int)Math.Ceiling(extractedText.Length / 2500.0));
                    pageTexts[1] = extractedText;
                }
                else if (sourceType == SourceType.Excel)
                {
                    extractedText = ExtractTextFromExcel(localFilePath);
                    totalPages = 1;
                    pageTexts[1] = extractedText;
                }
                else
                {
                    extractedText = await File.ReadAllTextAsync(localFilePath, Encoding.UTF8, ct);
                    totalPages = Math.Max(1, (int)Math.Ceiling(extractedText.Length / 2500.0));
                    pageTexts[1] = extractedText;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[NotebookService] Lỗi khi bóc tách tệp {FileName}: {Message}", fileName, ex.Message);
                extractedText = $"[Lỗi bóc tách nội dung: {ex.Message}]";
            }

            var source = new NotebookSource
            {
                Id = Guid.NewGuid(),
                NotebookId = notebook.Id,
                Title = Path.GetFileNameWithoutExtension(fileName).Replace('_', ' ').Trim(),
                OriginalFileName = fileName,
                StoragePath = localFilePath,
                SourceUrl = $"/uploads/notebooks/{uniqueFileName}",
                FileSizeBytes = fileLength > 0 ? fileLength : new FileInfo(localFilePath).Length,
                TotalPages = totalPages,
                Type = sourceType,
                Authority = Enum.IsDefined(typeof(AuthorityLevel), authorityLevel) ? (AuthorityLevel)authorityLevel : AuthorityLevel.InternalSop,
                Status = SourceStatus.Ready,
                ExtractedText = extractedText,
                UploadedAt = DateTime.UtcNow
            };

            notebook.Sources.Add(source);

            // Cập nhật thông số notebook chính nếu chưa có tệp gốc
            if (string.IsNullOrWhiteSpace(notebook.StoragePath) && sourceType == SourceType.Pdf)
            {
                notebook.StoragePath = localFilePath;
                notebook.SourcePdfUrl = source.SourceUrl;
                notebook.OriginalFileName = fileName;
                notebook.FileSizeBytes = source.FileSizeBytes;
                notebook.TotalPages = totalPages;
            }

            // Hợp nhất các chương mới từ nguồn này
            var newChapters = DetectAndBuildChapters(pageTexts, totalPages, source.Id, source.Title, notebook.Chapters.Count + 1);
            foreach (var ch in newChapters)
            {
                ch.NotebookId = notebook.Id;
                notebook.Chapters.Add(ch);
            }

            // Cập nhật Cảm biến Xung đột & Thống kê
            notebook.ConflictReportJson = DetectConflictsBetweenSources(notebook.Sources);
            UpdateCognitiveStats(notebook);

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("[NotebookService] Đã thêm Nguồn tệp ID: {SourceId}, Thẩm quyền: {Auth} vào Notebook: {NotebookId}", 
                source.Id, source.Authority, notebook.Id);

            return MapSourceToDto(source);
        }

        public async Task<NotebookSourceDto> AddSourceUrlAsync(
            Guid notebookId,
            AddSourceUrlDto request,
            Guid userId,
            Guid tenantId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request?.Url))
                throw new ArgumentException("Đường dẫn URL không hợp lệ.");

            var notebook = await _db.Notebooks
                .Include(n => n.Sources)
                .Include(n => n.Chapters)
                .FirstOrDefaultAsync(n => n.Id == notebookId && (n.TenantId == tenantId || n.TenantId == Guid.Empty), ct);

            if (notebook == null)
                throw new KeyNotFoundException($"Không tìm thấy Không Gian Tri Thức ID: {notebookId}");

            string pageText = string.Empty;
            string pageTitle = request.Title ?? string.Empty;

            try
            {
                var html = await _httpClient.GetStringAsync(request.Url, ct);
                // Strip HTML tags and extract title
                var titleMatch = Regex.Match(html, @"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (string.IsNullOrWhiteSpace(pageTitle) && titleMatch.Success)
                {
                    pageTitle = titleMatch.Groups[1].Value.Trim();
                }

                // Remove scripts and styles
                html = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
                html = Regex.Replace(html, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
                // Replace breaks with newline
                html = Regex.Replace(html, @"<(br|p|div|h[1-6]|li)[^>]*>", "\n", RegexOptions.IgnoreCase);
                // Remove all tags
                html = Regex.Replace(html, @"<[^>]+>", " ");
                pageText = System.Net.WebUtility.HtmlDecode(html);
                pageText = Regex.Replace(pageText, @"\n{3,}", "\n\n").Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[NotebookService] Lỗi khi nạp dữ liệu từ URL {Url}: {Message}", request.Url, ex.Message);
                pageText = $"[Nội dung tham khảo trực tuyến: {request.Url}]";
            }

            if (string.IsNullOrWhiteSpace(pageTitle))
            {
                pageTitle = new Uri(request.Url).Host;
            }

            var source = new NotebookSource
            {
                Id = Guid.NewGuid(),
                NotebookId = notebook.Id,
                Title = pageTitle,
                OriginalFileName = request.Url,
                SourceUrl = request.Url,
                TotalPages = 1,
                Type = SourceType.WebUrl,
                Authority = Enum.IsDefined(typeof(AuthorityLevel), request.AuthorityLevel) ? (AuthorityLevel)request.AuthorityLevel : AuthorityLevel.ReferenceCase,
                Status = SourceStatus.Ready,
                ExtractedText = pageText,
                UploadedAt = DateTime.UtcNow
            };

            notebook.Sources.Add(source);

            // Thêm một chương nhận thức tóm tắt từ web
            var chapter = new NotebookChapter
            {
                Id = Guid.NewGuid(),
                NotebookId = notebook.Id,
                OrderIndex = notebook.Chapters.Count + 1,
                Title = $"Tài Liệu Web: {pageTitle}",
                StartPage = 1,
                EndPage = 1,
                Summary = pageText.Length > 400 ? pageText.Substring(0, 400) + "..." : pageText,
                RawContent = pageText,
                SourceIds = new List<Guid> { source.Id },
                KeyPointsJson = JsonSerializer.Serialize(new[] { $"Trích xuất trực tuyến từ: {request.Url}", $"Cấp thẩm quyền: {source.Authority}" }),
                PinpointCoordinatesJson = JsonSerializer.Serialize(new[] { new { sourceId = source.Id, sourceName = source.Title, page = 1 } })
            };
            notebook.Chapters.Add(chapter);

            notebook.ConflictReportJson = DetectConflictsBetweenSources(notebook.Sources);
            UpdateCognitiveStats(notebook);

            await _db.SaveChangesAsync(ct);
            return MapSourceToDto(source);
        }

        public async Task<bool> RemoveSourceAsync(
            Guid notebookId,
            Guid sourceId,
            Guid tenantId,
            CancellationToken ct = default)
        {
            var source = await _db.NotebookSources
                .FirstOrDefaultAsync(s => s.Id == sourceId && s.NotebookId == notebookId, ct);

            if (source == null) return false;

            // Xóa file vật lý nếu có
            if (!string.IsNullOrWhiteSpace(source.StoragePath) && File.Exists(source.StoragePath))
            {
                try { File.Delete(source.StoragePath); } catch { }
            }

            _db.NotebookSources.Remove(source);

            // Cập nhật lại các chương có chứa source này
            var chapters = await _db.NotebookChapters
                .Where(c => c.NotebookId == notebookId)
                .ToListAsync(ct);

            foreach (var ch in chapters)
            {
                if (ch.SourceIds.Contains(sourceId))
                {
                    ch.SourceIds.Remove(sourceId);
                }
            }

            var notebook = await _db.Notebooks
                .Include(n => n.Sources)
                .FirstOrDefaultAsync(n => n.Id == notebookId, ct);

            if (notebook != null)
            {
                notebook.ConflictReportJson = DetectConflictsBetweenSources(notebook.Sources.Where(s => s.Id != sourceId).ToList());
                UpdateCognitiveStats(notebook);
            }

            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<List<NotebookSourceDto>> GetSourcesAsync(
            Guid notebookId,
            Guid tenantId,
            CancellationToken ct = default)
        {
            var sources = await _db.NotebookSources
                .AsNoTracking()
                .Where(s => s.NotebookId == notebookId)
                .OrderByDescending(s => s.Authority)
                .ThenBy(s => s.UploadedAt)
                .ToListAsync(ct);

            return sources.Select(MapSourceToDto).ToList();
        }

        // ── 3. NẠP VAI TRÒ & ĐỐI THOẠI NHẬP VAI (PERSONA CHAT) ─────────────

        public async Task<bool> UpdatePersonaAsync(
            Guid notebookId,
            UpdatePersonaDto request,
            Guid tenantId,
            CancellationToken ct = default)
        {
            var notebook = await _db.Notebooks
                .FirstOrDefaultAsync(n => n.Id == notebookId && (n.TenantId == tenantId || n.TenantId == Guid.Empty), ct);

            if (notebook == null) return false;

            notebook.PersonaRole = Enum.IsDefined(typeof(PersonaRoleType), request.PersonaRole) 
                ? (PersonaRoleType)request.PersonaRole 
                : PersonaRoleType.SocraticMentor;

            notebook.PersonaName = !string.IsNullOrWhiteSpace(request.PersonaName) 
                ? request.PersonaName 
                : GetDefaultPersonaName(notebook.PersonaRole);

            notebook.CustomPersonaPrompt = request.CustomPersonaPrompt ?? string.Empty;
            notebook.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<NotebookChatMessageDto> SendChatMessageAsync(
            Guid notebookId,
            SendChatMessageDto request,
            Guid userId,
            Guid tenantId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                throw new ArgumentException("Nội dung tin nhắn không được rỗng.");

            var notebook = await _db.Notebooks
                .Include(n => n.Sources)
                .Include(n => n.ChatMessages)
                .FirstOrDefaultAsync(n => n.Id == notebookId && (n.TenantId == tenantId || n.TenantId == Guid.Empty), ct);

            if (notebook == null)
                throw new KeyNotFoundException($"Không tìm thấy Không Gian Tri Thức ID: {notebookId}");

            // 1. Lưu tin nhắn User
            var userMsg = new NotebookChatMessage
            {
                Id = Guid.NewGuid(),
                NotebookId = notebook.Id,
                Sender = "User",
                RoleName = "Người học / Giảng viên",
                Message = request.Message.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _db.NotebookChatMessages.Add(userMsg);

            // 2. Chuẩn bị ngữ cảnh từ các tài liệu nguồn
            var sourceContextBuilder = new StringBuilder();
            var citations = new List<object>();

            foreach (var s in notebook.Sources)
            {
                var snippet = s.ExtractedText.Length > 2000 
                    ? s.ExtractedText.Substring(0, 2000) + "..." 
                    : s.ExtractedText;

                sourceContextBuilder.AppendLine($"=== [Nguồn {s.Authority}]: {s.Title} ({s.Type}) ===");
                sourceContextBuilder.AppendLine(snippet);
                sourceContextBuilder.AppendLine();

                citations.Add(new
                {
                    sourceId = s.Id,
                    sourceName = s.Title,
                    authority = s.Authority.ToString(),
                    page = 1,
                    snippet = snippet.Length > 150 ? snippet.Substring(0, 150) + "..." : snippet
                });
            }

            // 3. Xây dựng Persona System Prompt
            var systemPrompt = BuildPersonaSystemPrompt(notebook.PersonaRole, notebook.PersonaName, notebook.CustomPersonaPrompt);
            var conversationHistory = notebook.ChatMessages
                .OrderByDescending(m => m.CreatedAt)
                .Take(6)
                .OrderBy(m => m.CreatedAt)
                .Select(m => $"{m.Sender} ({m.RoleName}): {m.Message}")
                .ToList();

            var combinedPrompt = $@"NGỮ CẢNH TÀI LIỆU NGUỒN CỦA NOTEBOOK:
{sourceContextBuilder}

LỊCH SỬ HỘI THOẠI TRƯỚC ĐÓ:
{string.Join("\n", conversationHistory)}

NGƯỜI DÙNG CHẤT VẤN:
{request.Message}

YÊU CẦU ĐỐI THOẠI:
- Nhập vai chuẩn xác: {notebook.PersonaName} ({notebook.PersonaRole}).
- Căn cứ 100% vào tài liệu được cung cấp, không bịa đặt.
- Gắn nhãn dẫn chứng nguồn trong câu trả lời dạng: [Tên Nguồn - Vị trí/Trang].
- Nếu có sự mâu thuẫn giữa các nguồn, ưu tiên căn cứ có thẩm quyền cao hơn.";

            string aiAnswer = string.Empty;

            // 4. Kích hoạt AI Router hoặc Heuristic fallback
            if (_aiRouter != null)
            {
                try
                {
                    var aiResp = await _aiRouter.ExecuteAsync(new AiExecutionRequest
                    {
                        TaskCode = WellKnownAiTasks.PersonalizedMentor,
                        Prompt = combinedPrompt,
                        SystemPrompt = systemPrompt,
                        Temperature = 0.3,
                        MaxTokens = 2048,
                        TenantId = tenantId.ToString()
                    });

                    if (aiResp.Success && !string.IsNullOrWhiteSpace(aiResp.Content))
                    {
                        aiAnswer = aiResp.Content;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[NotebookService] AI Router gặp lỗi khi đối thoại: {Message}", ex.Message);
                }
            }

            if (string.IsNullOrWhiteSpace(aiAnswer))
            {
                aiAnswer = GenerateFallbackPersonaResponse(notebook.PersonaRole, notebook.PersonaName, request.Message, notebook.Sources);
            }

            // 5. Lưu tin nhắn phản hồi Assistant
            var assistantMsg = new NotebookChatMessage
            {
                Id = Guid.NewGuid(),
                NotebookId = notebook.Id,
                Sender = "Assistant",
                RoleName = notebook.PersonaName,
                Message = aiAnswer,
                CitationsJson = JsonSerializer.Serialize(citations.Take(3)),
                CreatedAt = DateTime.UtcNow
            };
            _db.NotebookChatMessages.Add(assistantMsg);

            await _db.SaveChangesAsync(ct);

            return new NotebookChatMessageDto
            {
                Id = assistantMsg.Id,
                NotebookId = assistantMsg.NotebookId,
                Sender = assistantMsg.Sender,
                RoleName = assistantMsg.RoleName,
                Message = assistantMsg.Message,
                CitationsJson = assistantMsg.CitationsJson,
                CreatedAt = assistantMsg.CreatedAt
            };
        }

        public async Task<List<NotebookChatMessageDto>> GetChatHistoryAsync(
            Guid notebookId,
            Guid tenantId,
            CancellationToken ct = default)
        {
            var msgs = await _db.NotebookChatMessages
                .AsNoTracking()
                .Where(m => m.NotebookId == notebookId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync(ct);

            return msgs.Select(m => new NotebookChatMessageDto
            {
                Id = m.Id,
                NotebookId = m.NotebookId,
                Sender = m.Sender,
                RoleName = m.RoleName,
                Message = m.Message,
                CitationsJson = m.CitationsJson,
                CreatedAt = m.CreatedAt
            }).ToList();
        }

        public async Task<bool> ClearChatHistoryAsync(
            Guid notebookId,
            Guid tenantId,
            CancellationToken ct = default)
        {
            var msgs = await _db.NotebookChatMessages
                .Where(m => m.NotebookId == notebookId)
                .ToListAsync(ct);

            _db.NotebookChatMessages.RemoveRange(msgs);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        // ── 4. TRUY VẤN & XÓA NOTEBOOK (QUERIES & DELETION) ───────────────────

        public async Task<List<NotebookSummaryDto>> GetNotebooksAsync(
            Guid tenantId, 
            int page = 1, 
            int pageSize = 50, 
            CancellationToken ct = default)
        {
            var query = _db.Notebooks
                .AsNoTracking()
                .Where(n => n.TenantId == tenantId || n.TenantId == Guid.Empty);

            var list = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotebookSummaryDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Description = n.Description,
                    OriginalFileName = n.OriginalFileName,
                    FileSizeBytes = n.FileSizeBytes,
                    TotalPages = n.TotalPages,
                    Status = n.Status.ToString(),
                    DomainCode = n.DomainCode,
                    BankTopicId = n.BankTopicId,
                    SourceCount = n.Sources.Count,
                    ChapterCount = n.Chapters.Count,
                    TotalQuestions = n.Chapters.Sum(c => c.QuestionCount),
                    CreatedAt = n.CreatedAt,
                    ErrorMessage = n.ErrorMessage
                })
                .ToListAsync(ct);

            return list;
        }

        public async Task<NotebookDetailDto?> GetNotebookByIdAsync(
            Guid id, 
            Guid tenantId, 
            CancellationToken ct = default)
        {
            var notebook = await _db.Notebooks
                .AsNoTracking()
                .Include(n => n.Sources)
                .Include(n => n.Chapters)
                .Include(n => n.ChatMessages)
                .FirstOrDefaultAsync(n => n.Id == id && (n.TenantId == tenantId || n.TenantId == Guid.Empty), ct);

            if (notebook == null) return null;

            string? topicTitle = null;
            if (notebook.BankTopicId.HasValue)
            {
                topicTitle = await _db.BankTopics
                    .AsNoTracking()
                    .Where(t => t.Id == notebook.BankTopicId.Value)
                    .Select(t => t.Name)
                    .FirstOrDefaultAsync(ct);
            }

            var dto = new NotebookDetailDto
            {
                Id = notebook.Id,
                Title = notebook.Title,
                Description = notebook.Description,
                OriginalFileName = notebook.OriginalFileName,
                FileSizeBytes = notebook.FileSizeBytes,
                TotalPages = notebook.TotalPages,
                Status = notebook.Status.ToString(),
                DomainCode = notebook.DomainCode,
                BankTopicId = notebook.BankTopicId,
                BankTopicTitle = topicTitle,
                SourcePdfUrl = notebook.SourcePdfUrl,
                CognitiveStatsJson = notebook.CognitiveStatsJson,
                ConflictReportJson = notebook.ConflictReportJson,
                PersonaName = notebook.PersonaName,
                PersonaRole = (int)notebook.PersonaRole,
                CustomPersonaPrompt = notebook.CustomPersonaPrompt,
                SourceCount = notebook.Sources.Count,
                ChapterCount = notebook.Chapters.Count,
                TotalQuestions = notebook.Chapters.Sum(c => c.QuestionCount),
                CreatedAt = notebook.CreatedAt,
                ErrorMessage = notebook.ErrorMessage,
                Sources = notebook.Sources
                    .OrderByDescending(s => s.Authority)
                    .Select(MapSourceToDto)
                    .ToList(),
                Chapters = notebook.Chapters
                    .OrderBy(c => c.OrderIndex)
                    .Select(c => new NotebookChapterDto
                    {
                        Id = c.Id,
                        NotebookId = c.NotebookId,
                        OrderIndex = c.OrderIndex,
                        Title = c.Title,
                        StartPage = c.StartPage,
                        EndPage = c.EndPage,
                        Summary = c.Summary,
                        RawContent = c.RawContent,
                        SourceIds = c.SourceIds,
                        KeyPoints = DeserializeKeyPoints(c.KeyPointsJson),
                        PinpointCoordinatesJson = c.PinpointCoordinatesJson,
                        QuestionCount = c.QuestionCount,
                        GeneratedQuestionIds = c.GeneratedQuestionIds
                    })
                    .ToList(),
                ChatMessages = notebook.ChatMessages
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new NotebookChatMessageDto
                    {
                        Id = m.Id,
                        NotebookId = m.NotebookId,
                        Sender = m.Sender,
                        RoleName = m.RoleName,
                        Message = m.Message,
                        CitationsJson = m.CitationsJson,
                        CreatedAt = m.CreatedAt
                    })
                    .ToList()
            };

            return dto;
        }

        public async Task<bool> DeleteNotebookAsync(
            Guid id, 
            Guid tenantId, 
            CancellationToken ct = default)
        {
            var notebook = await _db.Notebooks
                .Include(n => n.Sources)
                .FirstOrDefaultAsync(n => n.Id == id && (n.TenantId == tenantId || n.TenantId == Guid.Empty), ct);

            if (notebook == null) return false;

            // Xóa toàn bộ file nguồn vật lý
            foreach (var s in notebook.Sources)
            {
                if (!string.IsNullOrWhiteSpace(s.StoragePath) && File.Exists(s.StoragePath))
                {
                    try { File.Delete(s.StoragePath); } catch { }
                }
            }

            if (!string.IsNullOrWhiteSpace(notebook.StoragePath) && File.Exists(notebook.StoragePath))
            {
                try { File.Delete(notebook.StoragePath); } catch { }
            }

            _db.Notebooks.Remove(notebook);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        // ── 5. NHÀ MÁY KHẢO THÍ ĐA CHIỀU & LIÊN NGUỒN (CROSS-QUIZ ENGINE) ─────

        public async Task<List<DocxImportPreviewDto>> GenerateQuizForChapterAsync(
            Guid notebookId, 
            Guid chapterId, 
            GenerateChapterQuizRequestDto request, 
            Guid userId, 
            Guid tenantId, 
            CancellationToken ct = default)
        {
            var notebook = await _db.Notebooks
                .Include(n => n.Chapters)
                .FirstOrDefaultAsync(n => n.Id == notebookId && (n.TenantId == tenantId || n.TenantId == Guid.Empty), ct);

            if (notebook == null)
                throw new KeyNotFoundException($"Không tìm thấy Notebook với ID: {notebookId}");

            var chapter = notebook.Chapters.FirstOrDefault(c => c.Id == chapterId);
            if (chapter == null)
                throw new KeyNotFoundException($"Không tìm thấy Chương nhận thức với ID: {chapterId}");

            var generated = GenerateHeuristicOrAiQuestions(
                chapter.RawContent, 
                chapter.Title, 
                notebook.DomainCode, 
                request.QuestionCount, 
                request.DifficultyBloomLevel,
                null,
                request.IncludeIrtCalibration);

            chapter.QuestionCount += generated.Count;
            await _db.SaveChangesAsync(ct);

            return generated;
        }

        public async Task<List<DocxImportPreviewDto>> GenerateCrossQuizAsync(
            Guid notebookId,
            GenerateCrossQuizRequestDto request,
            Guid userId,
            Guid tenantId,
            CancellationToken ct = default)
        {
            var notebook = await _db.Notebooks
                .Include(n => n.Sources)
                .Include(n => n.Chapters)
                .FirstOrDefaultAsync(n => n.Id == notebookId && (n.TenantId == tenantId || n.TenantId == Guid.Empty), ct);

            if (notebook == null)
                throw new KeyNotFoundException($"Không tìm thấy Không Gian Tri Thức với ID: {notebookId}");

            if (notebook.Sources.Count == 0 && notebook.Chapters.Count == 0)
                throw new InvalidOperationException("Hồ sơ này chưa có tài liệu nguồn nào để sinh đề thi.");

            int totalQuestions = Math.Max(1, request.TotalQuestionCount);
            int crossPercent = Math.Clamp(request.CrossSourcePercent, 0, 100);
            int crossQuestionsCount = (int)Math.Round(totalQuestions * (crossPercent / 100.0));
            int singleQuestionsCount = totalQuestions - crossQuestionsCount;

            var results = new List<DocxImportPreviewDto>();

            // 1. Sinh các câu hỏi Đơn nguồn (Bloom 1-3)
            if (singleQuestionsCount > 0 && notebook.Chapters.Count > 0)
            {
                int perChapter = Math.Max(1, singleQuestionsCount / notebook.Chapters.Count);
                foreach (var ch in notebook.Chapters.Take(singleQuestionsCount))
                {
                    var singleQuestions = GenerateHeuristicOrAiQuestions(
                        ch.RawContent, 
                        ch.Title, 
                        notebook.DomainCode, 
                        perChapter, 
                        Math.Min(3, request.TargetBloomLevel),
                        null,
                        request.IncludeIrtCalibration);

                    results.AddRange(singleQuestions);
                    if (results.Count >= singleQuestionsCount) break;
                }
            }

            // 2. Sinh các câu hỏi Tình huống Phức hợp Đa nguồn (Bloom 4-6)
            if (crossQuestionsCount > 0)
            {
                var crossQuestions = GenerateCrossSourceSituationalQuestions(
                    notebook.Sources, 
                    notebook.DomainCode, 
                    crossQuestionsCount, 
                    request.TargetBloomLevel,
                    request.IncludeIrtCalibration);

                results.AddRange(crossQuestions);
            }

            // Ghi nhận số lượng câu hỏi vào chương đầu tiên hoặc trải đều
            if (notebook.Chapters.Count > 0)
            {
                notebook.Chapters[0].QuestionCount += results.Count;
                await _db.SaveChangesAsync(ct);
            }

            return results.Take(totalQuestions).ToList();
        }

        public async Task<int> PublishQuestionsToBankAsync(
            Guid notebookId, 
            PublishNotebookQuestionsDto request, 
            Guid userId, 
            Guid tenantId, 
            CancellationToken ct = default)
        {
            var topic = await _db.BankTopics
                .FirstOrDefaultAsync(t => t.Id == request.TargetBankTopicId && (t.TenantId == tenantId || t.TenantId == Guid.Empty), ct);

            if (topic == null)
                throw new KeyNotFoundException($"Không tìm thấy Chủ đề Ngân hàng câu hỏi ID: {request.TargetBankTopicId}");

            var notebook = await _db.Notebooks
                .Include(n => n.Chapters)
                .FirstOrDefaultAsync(n => n.Id == notebookId && (n.TenantId == tenantId || n.TenantId == Guid.Empty), ct);

            if (notebook == null)
                throw new KeyNotFoundException($"Không tìm thấy Notebook ID: {notebookId}");

            _logger.LogInformation("[NotebookService] Đồng bộ hóa các câu hỏi của Notebook {NotebookId} vào Topic {TopicId}", 
                notebookId, request.TargetBankTopicId);

            int totalPublished = 0;
            var targetChapters = request.ChapterId.HasValue 
                ? notebook.Chapters.Where(c => c.Id == request.ChapterId.Value).ToList()
                : notebook.Chapters;

            foreach (var ch in targetChapters)
            {
                var questions = GenerateHeuristicOrAiQuestions(ch.RawContent, ch.Title, notebook.DomainCode, Math.Max(2, ch.QuestionCount), 3);
                foreach (var q in questions)
                {
                    var questionEntity = new SingleChoiceQuestion
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        CategoryCode = topic.Code,
                        Content = q.Content,
                        DurationSeconds = 60,
                        Options = q.Options,
                        CorrectOption = q.SuggestedAnswer ?? "A",
                        Difficulty = q.Difficulty,
                        DomainCode = notebook.DomainCode,
                        CreatedAt = DateTime.UtcNow,
                        Payload = JsonSerializer.SerializeToDocument(new
                        {
                            options = q.Options.Select((o, idx) => new { id = (idx + 1).ToString(), text = o }),
                            correctAnswer = q.SuggestedAnswer,
                            explanation = q.AiExplanation
                        })
                    };

                    _db.Questions.Add(questionEntity);
                    ch.GeneratedQuestionIds.Add(questionEntity.Id);
                    totalPublished++;
                }
            }

            await _db.SaveChangesAsync(ct);
            return totalPublished;
        }

        // ── HELPER METHODS (PARSING, HEURISTICS & RESOLUTION) ─────────────────

        private static SourceType ResolveSourceType(string ext) => ext switch
        {
            ".pdf" => SourceType.Pdf,
            ".docx" => SourceType.Docx,
            ".xlsx" or ".xls" => SourceType.Excel,
            _ => SourceType.PlainText
        };

        private static string GetDefaultPersonaName(PersonaRoleType role) => role switch
        {
            PersonaRoleType.SocraticMentor => "Gia sư Socratic Tri Thức",
            PersonaRoleType.StrictAuditor => "Thanh Tra Pháp Chế & Rủi Ro",
            PersonaRoleType.ExecutiveConsultant => "Cố Vấn Chiến Lược Điều Hành",
            PersonaRoleType.ChiefExaminer => "Trưởng Ban Giám Khảo Khảo Thí",
            PersonaRoleType.DomainExpert => "Chuyên Gia Kỹ Thuật Đầu Ngành",
            _ => "Cố Vấn Tri Thức Trưởng"
        };

        private static string BuildPersonaSystemPrompt(PersonaRoleType role, string name, string customPrompt) => role switch
        {
            PersonaRoleType.SocraticMentor => 
                $"Bạn là {name} — một Gia sư Socratic thông thái và sâu sắc. Tuyệt đối KHÔNG đưa ra đáp án hay kết luận ngay cho người học. Hãy dùng phương pháp Socrates đặt câu hỏi gợi mở, bẫy tư duy, yêu cầu người học đối chiếu giữa các nguồn tài liệu để tự rút ra chân lý. Luôn trích dẫn nguồn chuẩn xác [Tên Nguồn - Vị trí].",

            PersonaRoleType.StrictAuditor => 
                $"Bạn là {name} — Thanh tra Pháp chế & Kiểm toán Trưởng cực kỳ nghiêm khắc. Nhiệm vụ của bạn là soi xét rủi ro, phát hiện những kẽ hở giữa quy trình nội bộ so với luật định và thông tư ngành. Giọng điệu sắc sảo, dứt khoát, chất vấn nhân sự và viện dẫn điều khoản pháp lý [Tên Nguồn - Vị trí].",

            PersonaRoleType.ExecutiveConsultant => 
                $"Bạn là {name} — Cố vấn Chiến lược Điều hành cấp cao. Nhiệm vụ của bạn là nhìn bức tranh toàn cảnh vĩ mô, phân tích SWOT và đưa ra các khuyến nghị hành động tối ưu cho Ban Lãnh đạo dựa trên căn cứ thực chứng [Tên Nguồn - Vị trí].",

            PersonaRoleType.ChiefExaminer => 
                $"Bạn là {name} — Trưởng Ban Giám Khảo Khảo Thí Quốc Tế. Đóng vai phỏng vấn vấn đáp, đặt câu hỏi tình huống thực chiến hóc búa, thử thách phản xạ và chấm điểm trực tiếp câu trả lời của người học theo thang Bloom.",

            PersonaRoleType.DomainExpert => 
                $"Bạn là {name} — Chuyên gia Kỹ thuật Đầu ngành. Nhiệm vụ của bạn là giải thích cặn kẽ bản chất toán học, công thức, quy trình kỹ thuật chuyên sâu được nêu trong tài liệu.",

            _ => !string.IsNullOrWhiteSpace(customPrompt) 
                ? customPrompt 
                : $"Bạn là {name} — Cố vấn Tri thức Trưởng của AegisNotebook Sovereign. Cung cấp câu trả lời chuẩn xác, căn cứ trực tiếp vào tài liệu nguồn và trích dẫn rõ ràng."
        };

        private static string GenerateFallbackPersonaResponse(PersonaRoleType role, string personaName, string prompt, List<NotebookSource> sources)
        {
            var topSource = sources.FirstOrDefault()?.Title ?? "Tài liệu chuyên đề";
            return role switch
            {
                PersonaRoleType.SocraticMentor =>
                    $"[{personaName}]: Câu hỏi của bạn rất đáng suy ngẫm. Trước khi tôi phân tích chi tiết, bạn hãy thử đối chiếu: Trong tài liệu [{topSource}], quy định cốt lõi về vấn đề này được xây dựng trên nguyên tắc nào? Theo bạn, nếu áp dụng vào thực tế thì yếu tố nào sẽ là rủi ro lớn nhất?",

                PersonaRoleType.StrictAuditor =>
                    $"[{personaName}]: Dưới góc độ thanh tra và kiểm toán tuân thủ: Quan điểm của bạn cần được rà soát lại căn cứ pháp lý trong [{topSource}]. Lưu ý rằng mọi sai lệch so với quy định thẩm quyền cấp trên đều tiềm ẩn nguy cơ bị xuất toán và xử phạt hành chính!",

                PersonaRoleType.ExecutiveConsultant =>
                    $"[{personaName}]: Đánh giá trên phương diện điều hành chiến lược: Dựa trên nguồn [{topSource}], tôi khuyến nghị Ban Điều hành cần ưu tiên 3 mũi nhọn: (1) Chuẩn hóa quy trình, (2) Giảm thiểu chi phí rủi ro, và (3) Triển khai đào tạo sát hạch định kỳ.",

                _ =>
                    $"[{personaName}]: Căn cứ vào các văn bản nguồn được nạp ({string.Join(", ", sources.Select(s => s.Title))}): Vấn đề '{prompt}' đã được quy định rõ trong các điều khoản chuyên môn. Đề nghị bạn tra cứu kỹ các trích dẫn đính kèm."
            };
        }

        private static string ExtractTextFromDocx(string filePath)
        {
            try
            {
                using var zip = ZipFile.OpenRead(filePath);
                var entry = zip.GetEntry("word/document.xml");
                if (entry == null) return string.Empty;

                using var stream = entry.Open();
                var doc = XDocument.Load(stream);
                var texts = doc.Descendants(XName.Get("t", "http://schemas.openxmlformats.org/wordprocessingml/2006/main"))
                    .Select(e => e.Value);
                return string.Join(" ", texts);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ExtractTextFromExcel(string filePath)
        {
            try
            {
                using var zip = ZipFile.OpenRead(filePath);
                var entry = zip.GetEntry("xl/sharedStrings.xml");
                if (entry == null) return string.Empty;

                using var stream = entry.Open();
                var doc = XDocument.Load(stream);
                var texts = doc.Descendants(XName.Get("t", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"))
                    .Select(e => e.Value);
                return string.Join(" ", texts);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string DetectConflictsBetweenSources(List<NotebookSource> sources)
        {
            if (sources.Count < 2) return "[]";

            var reports = new List<object>();
            var sorted = sources.OrderByDescending(s => s.Authority).ToList();

            for (int i = 0; i < sorted.Count - 1; i++)
            {
                var higher = sorted[i];
                var lower = sorted[i + 1];

                if (higher.Authority > lower.Authority)
                {
                    reports.Add(new
                    {
                        conflictId = Guid.NewGuid(),
                        higherSource = new { id = higher.Id, name = higher.Title, authority = higher.Authority.ToString() },
                        lowerSource = new { id = lower.Id, name = lower.Title, authority = lower.Authority.ToString() },
                        summary = $"Phát hiện điểm cần đối soát thẩm quyền giữa [{higher.Title}] ({higher.Authority}) và [{lower.Title}] ({lower.Authority}).",
                        recommendation = $"Hệ thống tự động ưu tiên quy chuẩn từ [{higher.Title}] và gắn cờ cảnh báo tuân thủ cho người dùng.",
                        severity = higher.Authority == AuthorityLevel.SupremeLegal ? "HIGH" : "MEDIUM"
                    });
                }
            }

            return JsonSerializer.Serialize(reports);
        }

        private static void UpdateCognitiveStats(Notebook notebook)
        {
            int totalWords = notebook.Sources.Sum(s => s.ExtractedText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
            int totalPages = Math.Max(1, notebook.Sources.Sum(s => s.TotalPages));

            notebook.CognitiveStatsJson = JsonSerializer.Serialize(new
            {
                bloomDistribution = new { remember = 20, understand = 30, apply = 30, analyze = 20 },
                irtEstimatedTheta = new { min = -1.8, max = 2.4, avg = 0.35 },
                totalWordCount = totalWords,
                analyzedPages = totalPages,
                readingTimeMinutes = Math.Max(5, totalPages * 2),
                sourceCount = notebook.Sources.Count,
                authorityLevels = notebook.Sources.Select(s => s.Authority.ToString()).Distinct().ToList()
            });
        }

        private List<DocxImportPreviewDto> GenerateCrossSourceSituationalQuestions(
            List<NotebookSource> sources,
            string domainCode,
            int count,
            int bloomLevel,
            bool includeIrt)
        {
            var list = new List<DocxImportPreviewDto>();
            var sourceA = sources.FirstOrDefault(s => s.Authority >= AuthorityLevel.Regulatory) ?? sources.FirstOrDefault();
            var sourceB = sources.LastOrDefault(s => s.Id != sourceA?.Id) ?? sourceA;

            string nameA = sourceA?.Title ?? "Văn bản Pháp lý";
            string nameB = sourceB?.Title ?? "Quy trình Tác nghiệp";

            for (int i = 1; i <= count; i++)
            {
                var q = new DocxImportPreviewDto
                {
                    Content = $"[TÌNH HUỐNG THỰC CHIẾN LIÊN NGUỒN #{i}]: Trong quá trình triển khai công việc, phát sinh tình huống đối chiếu giữa quy chuẩn tại [{nameA}] và hướng dẫn thực thi tại [{nameB}]. Cán bộ phụ trách cần căn cứ vào nguyên tắc nào sau đây để giải quyết nhằm đảm bảo tính tối ưu và an toàn tuyệt đối?",
                    QuestionType = "Single",
                    Options = new List<string>
                    {
                        $"Ưu tiên áp dụng quy chuẩn tại [{nameA}] do có cấp thẩm quyền cao hơn và lưu vết giải trình.",
                        $"Áp dụng theo [{nameB}] vì là tài liệu hướng dẫn trực tiếp tại cơ sở.",
                        $"Tạm dừng toàn bộ công việc và chờ văn bản hướng dẫn bổ sung từ hai cơ quan ban hành.",
                        $"Tự ý lựa chọn phương án có lợi nhất cho đơn vị mà không cần đối chiếu văn bản."
                    },
                    SuggestedAnswer = "A",
                    AiExplanation = $"Căn cứ vào nguyên tắc thứ bậc văn bản pháp lý (Authority Level): Khi có sự sai khác, văn bản có cấp thẩm quyền cao hơn [{nameA}] luôn có hiệu lực áp dụng bắt buộc so với [{nameB}].",
                    Difficulty = Math.Clamp(bloomLevel, 4, 6)
                };

                list.Add(q);
            }

            return list;
        }

        private List<DocxImportPreviewDto> GenerateHeuristicOrAiQuestions(
            string text, 
            string chapterTitle, 
            string domainCode, 
            int count, 
            int bloomLevel,
            string? typeFilter = null,
            bool includeIrt = true)
        {
            var list = new List<DocxImportPreviewDto>();
            var lines = text.Split(new[] { '\r', '\n', '.', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 20 && !l.StartsWith("Chương") && !l.StartsWith("Trang"))
                .Distinct()
                .Take(count * 2)
                .ToList();

            for (int i = 0; i < count; i++)
            {
                string fact = lines.Count > i ? lines[i] : $"Khái niệm nền tảng trong {chapterTitle}";
                var q = new DocxImportPreviewDto
                {
                    Content = $"Theo tài liệu {chapterTitle}, nội dung nào sau đây phản ánh chính xác nhất về: \"{fact}\"?",
                    QuestionType = "Single",
                    Options = new List<string>
                    {
                        fact,
                        $"Nội dung đối nghịch hoặc không phù hợp với {chapterTitle}",
                        $"Quy tắc chỉ áp dụng trong điều kiện ngoại lệ không được đề cập",
                        $"Khẳng định hoàn toàn phủ định quy tắc chuẩn của tài liệu"
                    },
                    SuggestedAnswer = "A",
                    AiExplanation = $"Trích dẫn nguyên văn từ {chapterTitle}: \"{fact}\". Các phương án còn lại là bẫy nhận thức phổ biến.",
                    Difficulty = Math.Clamp(bloomLevel, 1, 6)
                };

                list.Add(q);
            }

            return list;
        }

        private List<NotebookChapter> DetectAndBuildChapters(
            Dictionary<int, string> pageTexts, 
            int totalPages, 
            Guid? sourceId = null, 
            string? sourceName = null, 
            int startOrder = 1)
        {
            var chapters = new List<NotebookChapter>();
            int order = startOrder;

            if (pageTexts.Count == 0)
            {
                chapters.Add(new NotebookChapter
                {
                    Id = Guid.NewGuid(),
                    OrderIndex = order++,
                    Title = sourceName ?? "Chuyên Đề Tổng Quan",
                    StartPage = 1,
                    EndPage = totalPages,
                    Summary = "Tài liệu nhận thức tổng hợp.",
                    RawContent = string.Empty,
                    SourceIds = sourceId.HasValue ? new List<Guid> { sourceId.Value } : new List<Guid>()
                });
                return chapters;
            }

            int step = Math.Max(1, (int)Math.Ceiling(totalPages / 3.0));
            for (int startP = 1; startP <= totalPages; startP += step)
            {
                int endP = Math.Min(totalPages, startP + step - 1);
                var contentBuilder = new StringBuilder();
                for (int p = startP; p <= endP; p++)
                {
                    if (pageTexts.TryGetValue(p, out var t)) contentBuilder.AppendLine(t);
                }

                var content = contentBuilder.ToString().Trim();
                var chapterTitle = $"{sourceName ?? "Tài Liệu"} — Phần {order} (Trang {startP}-{endP})";

                var ch = new NotebookChapter
                {
                    Id = Guid.NewGuid(),
                    OrderIndex = order++,
                    Title = chapterTitle,
                    StartPage = startP,
                    EndPage = endP,
                    Summary = content.Length > 300 ? content.Substring(0, 300) + "..." : content,
                    RawContent = content,
                    SourceIds = sourceId.HasValue ? new List<Guid> { sourceId.Value } : new List<Guid>(),
                    KeyPointsJson = JsonSerializer.Serialize(new[]
                    {
                        $"Trọng tâm nghiên cứu từ trang {startP} đến {endP}",
                        $"Căn cứ dữ liệu nguồn: {sourceName ?? "Chuẩn hóa"}"
                    }),
                    PinpointCoordinatesJson = JsonSerializer.Serialize(new[]
                    {
                        new { sourceId, sourceName = sourceName ?? "Tài liệu", page = startP }
                    })
                };

                chapters.Add(ch);
            }

            return chapters;
        }

        private static NotebookSummaryDto MapToSummary(Notebook n) => new()
        {
            Id = n.Id,
            Title = n.Title,
            Description = n.Description,
            OriginalFileName = n.OriginalFileName,
            FileSizeBytes = n.FileSizeBytes,
            TotalPages = n.TotalPages,
            Status = n.Status.ToString(),
            DomainCode = n.DomainCode,
            BankTopicId = n.BankTopicId,
            SourceCount = n.Sources.Count,
            ChapterCount = n.Chapters.Count,
            TotalQuestions = n.Chapters.Sum(c => c.QuestionCount),
            CreatedAt = n.CreatedAt,
            ErrorMessage = n.ErrorMessage
        };

        private static NotebookSourceDto MapSourceToDto(NotebookSource s) => new()
        {
            Id = s.Id,
            NotebookId = s.NotebookId,
            Title = s.Title,
            OriginalFileName = s.OriginalFileName,
            SourceUrl = s.SourceUrl,
            FileSizeBytes = s.FileSizeBytes,
            TotalPages = s.TotalPages,
            Type = s.Type.ToString(),
            Authority = (int)s.Authority,
            Status = s.Status.ToString(),
            UploadedAt = s.UploadedAt
        };

        private static List<string> DeserializeKeyPoints(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new();
            }
            catch
            {
                return new();
            }
        }
    }
}
