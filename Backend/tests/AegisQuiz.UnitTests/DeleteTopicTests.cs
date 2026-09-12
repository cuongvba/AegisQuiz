using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AegisQuiz.API.Controllers;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AegisQuiz.UnitTests
{
    public class DeleteTopicTests : IDisposable
    {
        private readonly SqliteConnection _connection;

        public DeleteTopicTests()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            // Bật foreign key constraint trong SQLite
            using var command = _connection.CreateCommand();
            command.CommandText = "PRAGMA foreign_keys = ON;";
            command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        private AegisQuizDbContext CreateSqliteDbContext(ITenantContext? tenantContext = null)
        {
            var options = new DbContextOptionsBuilder<AegisQuizDbContext>()
                .UseSqlite(_connection)
                .Options;

            var context = new AegisQuizDbContext(options, tenantContext);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task DeleteTopic_WithQuestions_DeleteQuestionsTrue_ShouldDeleteQuestionsAndTopic()
        {
            using var db = CreateSqliteDbContext();
            var topic = new BankTopic
            {
                Id = Guid.NewGuid(),
                Code = "PHYSICS",
                Name = "Physics",
                CategoryCode = "PHYSICS"
            };
            db.BankTopics.Add(topic);

            var q = new SingleChoiceQuestion
            {
                Id = Guid.NewGuid(),
                Content = "What is gravity?",
                CategoryCode = "PHYSICS",
                Options = new List<string> { "A", "B", "C", "D" },
                CorrectOption = "A"
            };
            db.Questions.Add(q);
            await db.SaveChangesAsync();

            var controller = new TopicsController(db);
            var result = await controller.DeleteTopic(topic.Id, true);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(await db.BankTopics.FindAsync(topic.Id));
            Assert.Null(await db.Questions.FindAsync(q.Id));
        }

        [Fact]
        public async Task DeleteTopic_WithQuestions_DeleteQuestionsFalse_ShouldReassignQuestions()
        {
            using var db = CreateSqliteDbContext();
            var topic = new BankTopic
            {
                Id = Guid.NewGuid(),
                Code = "CHEMISTRY",
                Name = "Chemistry",
                CategoryCode = "CHEMISTRY"
            };
            db.BankTopics.Add(topic);

            var q = new SingleChoiceQuestion
            {
                Id = Guid.NewGuid(),
                Content = "What is H2O?",
                CategoryCode = "CHEMISTRY",
                Options = new List<string> { "Water", "Gas", "Metal", "Plastic" },
                CorrectOption = "Water"
            };
            db.Questions.Add(q);
            await db.SaveChangesAsync();

            var controller = new TopicsController(db);
            var result = await controller.DeleteTopic(topic.Id, false);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Null(await db.BankTopics.FindAsync(topic.Id));
            var refreshedQ = await db.Questions.FindAsync(q.Id);
            Assert.NotNull(refreshedQ);
            Assert.Equal("GENERAL", refreshedQ.CategoryCode);
        }

        [Fact]
        public async Task DeleteTopic_TenantFilter_ShouldNotFailWhenChildHasAnotherTenant()
        {
            var tenantA = Guid.NewGuid();
            var tenantB = Guid.NewGuid();

            var tenantContext = new TenantContext();
            tenantContext.SetTenant(tenantA, "tenantA", isSystemAdmin: false);

            using (var dbSetup = CreateSqliteDbContext())
            {
                var parent = new BankTopic
                {
                    Id = Guid.NewGuid(),
                    Code = "SHARED_ROOT",
                    Name = "Shared Root",
                    CategoryCode = "SHARED_ROOT",
                    TenantId = Guid.Empty // Shared topic
                };
                var childOtherTenant = new BankTopic
                {
                    Id = Guid.NewGuid(),
                    Code = "CHILD_TENANT_B",
                    Name = "Child Tenant B",
                    CategoryCode = "CHILD_TENANT_B",
                    ParentId = parent.Id,
                    TenantId = tenantB // Belong to tenant B
                };
                dbSetup.BankTopics.AddRange(parent, childOtherTenant);
                await dbSetup.SaveChangesAsync();
            }

            // User from Tenant A deletes SHARED_ROOT
            using (var dbTenantA = CreateSqliteDbContext(tenantContext))
            {
                var controller = new TopicsController(dbTenantA);
                var sharedRoot = await dbTenantA.BankTopics.FirstOrDefaultAsync(t => t.Code == "SHARED_ROOT");
                Assert.NotNull(sharedRoot);

                // This should handle the child that is hidden by tenant isolation without crashing with 500 FK constraint
                var result = await controller.DeleteTopic(sharedRoot.Id, false);
                // Let's see what result we get!
                var statusCode = (result as ObjectResult)?.StatusCode;
                Assert.True(statusCode == 200 || statusCode == null, $"Failed with status {statusCode}: {((result as ObjectResult)?.Value)}");
            }
        }
    }
}
