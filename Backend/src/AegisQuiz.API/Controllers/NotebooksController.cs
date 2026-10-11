using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AegisQuiz.API.Security;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [AegisNotebook Sovereign API V2.0]
    /// Quản lý phân hệ Không Gian Tri Thức Hub & Ngân hàng Đề thi Thích ứng Đa Nguồn:
    /// - Khởi tạo Hub và tiếp nhận đa định dạng: PDF, Word DOCX, Excel XLSX, Web URLs.
    /// - Quản lý Cấp thẩm quyền tài liệu & Cảm biến Xung đột văn bản.
    /// - Nạp Vai Trò Đối Thoại Nhận Thức (Persona Role Injection & Socratic Interlocutor).
    /// - Sinh câu hỏi Đơn nguồn & Tình huống Phức hợp Liên Nguồn (Bloom 5-6) theo tỷ lệ %.
    /// - Đồng bộ O(1) vào Ngân hàng Câu hỏi chính thức (BankTopics).
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class NotebooksController : ControllerBase
    {
        private readonly INotebookService _notebookService;
        private readonly ITenantContext _tenantContext;

        public NotebooksController(
            INotebookService notebookService, 
            ITenantContext tenantContext)
        {
            _notebookService = notebookService;
            _tenantContext = tenantContext;
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var uid) ? uid : Guid.Empty;
        }

        private Guid GetCurrentTenantId()
        {
            return _tenantContext.CurrentTenantId != Guid.Empty 
                ? _tenantContext.CurrentTenantId 
                : Guid.Empty;
        }

        /// <summary>
        /// Khởi tạo Không Gian Tri Thức Hub mới (chưa cần tệp ngay).
        /// </summary>
        [AllowAnonymous]
        [HttpPost("hub")]
        public async Task<IActionResult> CreateHub(
            [FromBody] NotebookCreateHubDto request,
            CancellationToken ct = default)
        {
            var summary = await _notebookService.CreateHubAsync(
                request ?? new NotebookCreateHubDto(),
                GetCurrentUserId(),
                GetCurrentTenantId(),
                ct);

            return Ok(summary);
        }

        /// <summary>
        /// Tải lên nhanh tệp PDF để tạo Hub và nạp ngay làm nguồn đầu tiên.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadNotebook(
            IFormFile file,
            [FromForm] string? title = null,
            [FromForm] string? description = null,
            [FromForm] string? domainCode = null,
            [FromForm] Guid? bankTopicId = null,
            [FromForm] int authorityLevel = 2,
            CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Vui lòng chọn tệp tin hợp lệ." });

            if (file.Length > 150 * 1024 * 1024) // 150MB max
                return BadRequest(new { message = "Dung lượng tệp vượt quá giới hạn 150MB." });

            var request = new NotebookUploadRequestDto
            {
                Title = title,
                Description = description,
                DomainCode = domainCode,
                BankTopicId = bankTopicId,
                AuthorityLevel = authorityLevel
            };

            using var stream = file.OpenReadStream();
            var summary = await _notebookService.CreateFromUploadAsync(
                stream, 
                file.FileName, 
                file.Length, 
                request, 
                GetCurrentUserId(), 
                GetCurrentTenantId(), 
                ct);

            return Ok(summary);
        }

        /// <summary>
        /// Tải thêm tài liệu nguồn (PDF, Word, Excel) vào Không Gian Tri Thức đang có.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("{id:guid}/sources")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddSourceFile(
            Guid id,
            IFormFile file,
            [FromForm] int authorityLevel = 2,
            CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Vui lòng chọn tệp tin hợp lệ." });

            using var stream = file.OpenReadStream();
            var sourceDto = await _notebookService.AddSourceFileAsync(
                id,
                stream,
                file.FileName,
                file.Length,
                authorityLevel,
                GetCurrentUserId(),
                GetCurrentTenantId(),
                ct);

            return Ok(sourceDto);
        }

        /// <summary>
        /// Nạp thêm tài liệu Web URL vào Không Gian Tri Thức.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("{id:guid}/sources/url")]
        public async Task<IActionResult> AddSourceUrl(
            Guid id,
            [FromBody] AddSourceUrlDto request,
            CancellationToken ct = default)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url))
                return BadRequest(new { message = "Vui lòng nhập đường dẫn URL hợp lệ." });

            var sourceDto = await _notebookService.AddSourceUrlAsync(
                id,
                request,
                GetCurrentUserId(),
                GetCurrentTenantId(),
                ct);

            return Ok(sourceDto);
        }

        /// <summary>
        /// Lấy danh sách toàn bộ tài liệu nguồn trong Không Gian Tri Thức.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id:guid}/sources")]
        public async Task<IActionResult> GetSources(Guid id, CancellationToken ct = default)
        {
            var list = await _notebookService.GetSourcesAsync(id, GetCurrentTenantId(), ct);
            return Ok(list);
        }

        /// <summary>
        /// Gỡ bỏ một tài liệu nguồn khỏi Không Gian Tri Thức.
        /// </summary>
        [AllowAnonymous]
        [HttpDelete("{id:guid}/sources/{sourceId:guid}")]
        public async Task<IActionResult> RemoveSource(Guid id, Guid sourceId, CancellationToken ct = default)
        {
            var ok = await _notebookService.RemoveSourceAsync(id, sourceId, GetCurrentTenantId(), ct);
            if (!ok) return NotFound(new { message = "Không tìm thấy tài liệu nguồn để xóa." });
            return Ok(new { success = true, message = "Đã gỡ bỏ tài liệu nguồn thành công." });
        }

        /// <summary>
        /// Lấy danh sách toàn bộ không gian tri thức của tổ chức / cá nhân.
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetNotebooks(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 50, 
            CancellationToken ct = default)
        {
            var list = await _notebookService.GetNotebooksAsync(GetCurrentTenantId(), page, pageSize, ct);
            return Ok(list);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một Không Gian Tri Thức kèm các nguồn, chương mục và lịch sử chat.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetNotebookById(Guid id, CancellationToken ct = default)
        {
            var detail = await _notebookService.GetNotebookByIdAsync(id, GetCurrentTenantId(), ct);
            if (detail == null)
                return NotFound(new { message = "Không tìm thấy Không Gian Tri Thức được yêu cầu." });

            return Ok(detail);
        }

        /// <summary>
        /// Xóa một Không Gian Tri Thức khỏi hệ thống.
        /// </summary>
        [AllowAnonymous]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteNotebook(Guid id, CancellationToken ct = default)
        {
            var success = await _notebookService.DeleteNotebookAsync(id, GetCurrentTenantId(), ct);
            if (!success)
                return NotFound(new { message = "Không tìm thấy Không Gian Tri Thức để xóa." });

            return Ok(new { success = true, message = "Đã xóa Không Gian Tri Thức thành công." });
        }

        /// <summary>
        /// Cập nhật nạp vai trò đối thoại (Persona Role Injection).
        /// </summary>
        [AllowAnonymous]
        [HttpPut("{id:guid}/persona")]
        public async Task<IActionResult> UpdatePersona(
            Guid id,
            [FromBody] UpdatePersonaDto request,
            CancellationToken ct = default)
        {
            if (request == null)
                return BadRequest(new { message = "Dữ liệu cấu hình vai trò không hợp lệ." });

            var ok = await _notebookService.UpdatePersonaAsync(id, request, GetCurrentTenantId(), ct);
            if (!ok) return NotFound(new { message = "Không tìm thấy Không Gian Tri Thức để cấu hình vai trò." });

            return Ok(new { success = true, message = "Đã cập nhật vai trò Persona thành công." });
        }

        /// <summary>
        /// Gửi câu hỏi / tình huống để Persona đóng vai phản hồi trực tiếp với Grounding Citations.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("{id:guid}/chat")]
        public async Task<IActionResult> SendChatMessage(
            Guid id,
            [FromBody] SendChatMessageDto request,
            CancellationToken ct = default)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { message = "Nội dung tin nhắn không được để trống." });

            var reply = await _notebookService.SendChatMessageAsync(
                id,
                request,
                GetCurrentUserId(),
                GetCurrentTenantId(),
                ct);

            return Ok(reply);
        }

        /// <summary>
        /// Lấy toàn bộ lịch sử đàm thoại nhập vai của Notebook.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id:guid}/chat")]
        public async Task<IActionResult> GetChatHistory(Guid id, CancellationToken ct = default)
        {
            var history = await _notebookService.GetChatHistoryAsync(id, GetCurrentTenantId(), ct);
            return Ok(history);
        }

        /// <summary>
        /// Đặt lại toàn bộ lịch sử đàm thoại.
        /// </summary>
        [AllowAnonymous]
        [HttpDelete("{id:guid}/chat")]
        public async Task<IActionResult> ClearChatHistory(Guid id, CancellationToken ct = default)
        {
            var ok = await _notebookService.ClearChatHistoryAsync(id, GetCurrentTenantId(), ct);
            return Ok(new { success = ok, message = "Đã làm sạch lịch sử đối thoại." });
        }

        /// <summary>
        /// Sinh bộ câu hỏi trắc nghiệm / tự luận khảo thí tự động cho một chương cụ thể.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("{id:guid}/chapters/{chapterId:guid}/generate-quiz")]
        public async Task<IActionResult> GenerateChapterQuiz(
            Guid id, 
            Guid chapterId, 
            [FromBody] GenerateChapterQuizRequestDto request, 
            CancellationToken ct = default)
        {
            var questions = await _notebookService.GenerateQuizForChapterAsync(
                id, 
                chapterId, 
                request ?? new GenerateChapterQuizRequestDto(), 
                GetCurrentUserId(), 
                GetCurrentTenantId(), 
                ct);

            return Ok(questions);
        }

        /// <summary>
        /// Sinh đề thi khảo thí đa chiều: điều chỉnh tỷ lệ % câu hỏi đơn nguồn vs câu hỏi tình huống liên nguồn (Bloom 5-6).
        /// </summary>
        [AllowAnonymous]
        [HttpPost("{id:guid}/generate-cross-quiz")]
        public async Task<IActionResult> GenerateCrossQuiz(
            Guid id,
            [FromBody] GenerateCrossQuizRequestDto request,
            CancellationToken ct = default)
        {
            var questions = await _notebookService.GenerateCrossQuizAsync(
                id,
                request ?? new GenerateCrossQuizRequestDto(),
                GetCurrentUserId(),
                GetCurrentTenantId(),
                ct);

            return Ok(questions);
        }

        /// <summary>
        /// Xuất bản toàn bộ hoặc từng phần câu hỏi của giáo án vào Ngân hàng câu hỏi chính thức (BankTopics).
        /// </summary>
        [AllowAnonymous]
        [HttpPost("{id:guid}/publish-to-bank")]
        public async Task<IActionResult> PublishToBank(
            Guid id, 
            [FromBody] PublishNotebookQuestionsDto request, 
            CancellationToken ct = default)
        {
            if (request == null || request.TargetBankTopicId == Guid.Empty)
                return BadRequest(new { message = "Vui lòng chọn Chủ đề Ngân hàng (BankTopic) đích hợp lệ." });

            var count = await _notebookService.PublishQuestionsToBankAsync(
                id, 
                request, 
                GetCurrentUserId(), 
                GetCurrentTenantId(), 
                ct);

            return Ok(new 
            { 
                success = true, 
                publishedCount = count, 
                message = $"Đã xuất bản thành công {count} câu hỏi vào Ngân hàng tri thức." 
            });
        }
    }
}
