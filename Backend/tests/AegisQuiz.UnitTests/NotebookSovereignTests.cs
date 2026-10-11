using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Infrastructure.Services;
using AegisQuiz.Infrastructure.AI;

namespace AegisQuiz.UnitTests
{
    public class NotebookSovereignTests
    {
        private AegisQuizDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AegisQuizDbContext>()
                .UseInMemoryDatabase(databaseName: $"AegisNotebookTest_{Guid.NewGuid()}")
                .Options;

            return new AegisQuizDbContext(options);
        }

        [Fact]
        public async Task Notebook_CreateAndRetrieve_ShouldSucceedWithMultiTenancyAndChapters()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var notebook = new Notebook
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = "Giáo Trình Thẩm Định Tín Dụng Agribank",
                DomainCode = "BANKING",
                OriginalFileName = "agribank_credit.pdf",
                TotalPages = 150,
                Status = NotebookStatus.Ready,
                CognitiveStatsJson = "{\"bloom\":{\"remember\":30,\"understand\":40}}"
            };

            var ch1 = new NotebookChapter
            {
                Id = Guid.NewGuid(),
                NotebookId = notebook.Id,
                OrderIndex = 1,
                Title = "Chương 1: Quy trình thẩm định tài sản bảo đảm",
                StartPage = 1,
                EndPage = 25,
                Summary = "Phân tích các bước thẩm định tính pháp lý và giá trị thị trường của bất động sản.",
                KeyPointsJson = "[\"Thẩm định pháp lý sổ đỏ\",\"Định giá theo phương pháp so sánh\"]",
                QuestionCount = 10
            };

            notebook.Chapters.Add(ch1);
            db.Notebooks.Add(notebook);
            await db.SaveChangesAsync();

            // Act
            var retrieved = await db.Notebooks
                .Include(n => n.Chapters)
                .FirstOrDefaultAsync(n => n.Id == notebook.Id);

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(tenantId, retrieved.TenantId);
            Assert.Equal("BANKING", retrieved.DomainCode);
            Assert.Single(retrieved.Chapters);
            Assert.Equal("Chương 1: Quy trình thẩm định tài sản bảo đảm", retrieved.Chapters[0].Title);
            Assert.Equal(10, retrieved.Chapters[0].QuestionCount);
        }

        [Fact]
        public void NotebookDtos_MappingAndJsonSerialization_ShouldBeConsistent()
        {
            // Arrange
            var dto = new NotebookDetailDto
            {
                Id = Guid.NewGuid(),
                Title = "ISO 27001 Information Security Management",
                DomainCode = "IT_SECURITY",
                TotalPages = 85,
                ChapterCount = 4,
                TotalQuestions = 35,
                Status = "Ready",
                SourcePdfUrl = "/uploads/notebooks/sample.pdf",
                Chapters = new List<NotebookChapterDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        OrderIndex = 1,
                        Title = "Phần 1: Chính sách bảo mật ISMS",
                        KeyPoints = new List<string> { "Đánh giá rủi ro CIA", "Kiểm soát truy cập" },
                        QuestionCount = 10
                    }
                }
            };

            // Assert
            Assert.Equal("IT_SECURITY", dto.DomainCode);
            Assert.Single(dto.Chapters);
            Assert.Equal(2, dto.Chapters[0].KeyPoints.Count);
        }

        [Fact]
        public async Task Notebook_MultiSourceAndPersonaRole_ShouldPersistCorrectly()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var notebookId = Guid.NewGuid();
            var notebook = new Notebook
            {
                Id = notebookId,
                Title = "Hồ Sơ Nhận Thức Pháp Chế & Tín Dụng",
                DomainCode = "BANKING",
                PersonaRole = PersonaRoleType.StrictAuditor,
                PersonaName = "Thanh Tra Pháp Chế",
                ConflictReportJson = "[{\"severity\":\"HIGH\"}]"
            };

            var source1 = new NotebookSource
            {
                Id = Guid.NewGuid(),
                NotebookId = notebookId,
                Title = "Luật Các TCTD 2024",
                Authority = AuthorityLevel.SupremeLegal,
                Type = SourceType.Pdf,
                ExtractedText = "Quy định trích lập dự phòng rủi ro tín dụng..."
            };

            var source2 = new NotebookSource
            {
                Id = Guid.NewGuid(),
                NotebookId = notebookId,
                Title = "Sổ Tay Tín Dụng Agribank",
                Authority = AuthorityLevel.InternalSop,
                Type = SourceType.Docx,
                ExtractedText = "Hướng dẫn phân loại nhóm nợ theo sổ tay nội bộ..."
            };

            notebook.Sources.Add(source1);
            notebook.Sources.Add(source2);

            var chatMsg = new NotebookChatMessage
            {
                Id = Guid.NewGuid(),
                NotebookId = notebookId,
                Sender = "Assistant",
                RoleName = notebook.PersonaName,
                Message = "Căn cứ vào Luật Các TCTD 2024, quy trình nội bộ phải điều chỉnh mức dự phòng.",
                CitationsJson = "[{\"sourceId\":\"" + source1.Id + "\"}]"
            };

            notebook.ChatMessages.Add(chatMsg);

            db.Notebooks.Add(notebook);
            await db.SaveChangesAsync();

            // Act
            var retrieved = await db.Notebooks
                .Include(n => n.Sources)
                .Include(n => n.ChatMessages)
                .FirstOrDefaultAsync(n => n.Id == notebookId);

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(2, retrieved.Sources.Count);
            Assert.Equal(PersonaRoleType.StrictAuditor, retrieved.PersonaRole);
            Assert.Equal(AuthorityLevel.SupremeLegal, retrieved.Sources[0].Authority);
            Assert.Equal(AuthorityLevel.InternalSop, retrieved.Sources[1].Authority);
            Assert.Single(retrieved.ChatMessages);
            Assert.Equal("Thanh Tra Pháp Chế", retrieved.ChatMessages[0].RoleName);
        }
    }
}
