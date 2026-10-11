using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AegisQuiz.Application.DTOs;

namespace AegisQuiz.Application.Interfaces
{
    // [AegisNotebook Sovereign Interface V2.0]
    public interface INotebookService
    {
        Task<NotebookSummaryDto> CreateHubAsync(
            NotebookCreateHubDto request,
            Guid userId,
            Guid tenantId,
            CancellationToken ct = default);

        Task<NotebookSummaryDto> CreateFromUploadAsync(
            Stream pdfStream, 
            string fileName, 
            long fileLength,
            NotebookUploadRequestDto request, 
            Guid userId, 
            Guid tenantId, 
            CancellationToken ct = default);

        Task<NotebookSourceDto> AddSourceFileAsync(
            Guid notebookId,
            Stream stream,
            string fileName,
            long fileLength,
            int authorityLevel,
            Guid userId,
            Guid tenantId,
            CancellationToken ct = default);

        Task<NotebookSourceDto> AddSourceUrlAsync(
            Guid notebookId,
            AddSourceUrlDto request,
            Guid userId,
            Guid tenantId,
            CancellationToken ct = default);

        Task<bool> RemoveSourceAsync(
            Guid notebookId,
            Guid sourceId,
            Guid tenantId,
            CancellationToken ct = default);

        Task<List<NotebookSourceDto>> GetSourcesAsync(
            Guid notebookId,
            Guid tenantId,
            CancellationToken ct = default);

        Task<List<NotebookSummaryDto>> GetNotebooksAsync(
            Guid tenantId, 
            int page = 1, 
            int pageSize = 50, 
            CancellationToken ct = default);

        Task<NotebookDetailDto?> GetNotebookByIdAsync(
            Guid id, 
            Guid tenantId, 
            CancellationToken ct = default);

        Task<bool> DeleteNotebookAsync(
            Guid id, 
            Guid tenantId, 
            CancellationToken ct = default);

        Task<bool> UpdatePersonaAsync(
            Guid notebookId,
            UpdatePersonaDto request,
            Guid tenantId,
            CancellationToken ct = default);

        Task<NotebookChatMessageDto> SendChatMessageAsync(
            Guid notebookId,
            SendChatMessageDto request,
            Guid userId,
            Guid tenantId,
            CancellationToken ct = default);

        Task<List<NotebookChatMessageDto>> GetChatHistoryAsync(
            Guid notebookId,
            Guid tenantId,
            CancellationToken ct = default);

        Task<bool> ClearChatHistoryAsync(
            Guid notebookId,
            Guid tenantId,
            CancellationToken ct = default);

        Task<List<DocxImportPreviewDto>> GenerateQuizForChapterAsync(
            Guid notebookId, 
            Guid chapterId, 
            GenerateChapterQuizRequestDto request, 
            Guid userId, 
            Guid tenantId, 
            CancellationToken ct = default);

        Task<List<DocxImportPreviewDto>> GenerateCrossQuizAsync(
            Guid notebookId,
            GenerateCrossQuizRequestDto request,
            Guid userId,
            Guid tenantId,
            CancellationToken ct = default);

        Task<int> PublishQuestionsToBankAsync(
            Guid notebookId, 
            PublishNotebookQuestionsDto request, 
            Guid userId, 
            Guid tenantId, 
            CancellationToken ct = default);
    }
}
