using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using AegisQuiz.API.Controllers;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Services;
using Xunit;

namespace AegisQuiz.UnitTests
{
    [Collection("MathDictionaryCollection")]
    public class MathDictionaryControllerTests
    {
        [Fact]
        public void GetDictionary_ReturnsAllCategoriesAndStructuralRules()
        {
            // Arrange
            var service = new DynamicMathDictionaryService();
            var controller = new MathDictionaryController(service);

            // Act
            var result = controller.GetDictionary() as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);

            dynamic? val = result.Value;
            var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
            Assert.True(doc.RootElement.GetProperty("totalStructuralRules").GetInt32() >= 0);
        }

        [Fact]
        public void UpsertStructuralRule_AddsOrUpdatesRuleSuccessfully()
        {
            // Arrange
            var service = new DynamicMathDictionaryService();
            var controller = new MathDictionaryController(service);
            string testRuleId = "TEST_API_RULE_" + Guid.NewGuid().ToString("N")[..8];

            var rule = new MathStructuralRuleDto
            {
                Id = testRuleId,
                Name = "API Test Structural Rule",
                Priority = 77,
                Enabled = true,
                Anchor = new MathAnchorSpecDto { Type = "GEOMETRIC_LINE" },
                Slots = new List<SpatialSlotDto>
                {
                    new() { Name = "top", RelativePosition = "ABOVE_ANCHOR" },
                    new() { Name = "bottom", RelativePosition = "BELOW_ANCHOR" }
                },
                Template = "\\frac{{{top}}}{{{bottom}}}"
            };

            try
            {
                // Act: Upsert
                var upsertResult = controller.UpsertStructuralRule(rule) as OkObjectResult;
                Assert.NotNull(upsertResult);

                // Verify rule exists in registry
                var registry = service.GetCurrentRegistry();
                var found = registry.StructuralSimplificationRules.Find(r => r.Id == testRuleId);
                Assert.NotNull(found);
                Assert.Equal("API Test Structural Rule", found.Name);

                // Act: Toggle
                var toggleResult = controller.ToggleStructuralRule(testRuleId) as OkObjectResult;
                Assert.NotNull(toggleResult);
                var toggled = service.GetCurrentRegistry().StructuralSimplificationRules.Find(r => r.Id == testRuleId);
                Assert.NotNull(toggled);
                Assert.False(toggled.Enabled);

                // Act: Delete
                var deleteResult = controller.DeleteStructuralRule(testRuleId) as OkObjectResult;
                Assert.NotNull(deleteResult);
                var deleted = service.GetCurrentRegistry().StructuralSimplificationRules.Find(r => r.Id == testRuleId);
                Assert.Null(deleted);
            }
            finally
            {
                service.DeleteStructuralRule(testRuleId);
            }
        }
    }
}
