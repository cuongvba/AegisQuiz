using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.Models.AI;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Infrastructure.Services;
using Xunit;

namespace AegisQuiz.UnitTests
{
    public class VisionMathServiceTests
    {
        private class FakeCacheService : ICacheService
        {
            public readonly Dictionary<string, string> Store = new();
            public int SetCallCount = 0;

            public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
            {
                if (Store.TryGetValue(key, out var val) && typeof(T) == typeof(string))
                {
                    return Task.FromResult((T?)(object)val);
                }
                return Task.FromResult(default(T));
            }

            public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
            {
                SetCallCount++;
                if (value is string s)
                {
                    Store[key] = s;
                }
                return Task.CompletedTask;
            }

            public Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken ct = default)
            {
                if (Store.TryGetValue(key, out var val) && typeof(T) == typeof(string))
                {
                    return Task.FromResult((T)(object)val);
                }
                return factory();
            }

            public Task RemoveAsync(string key, CancellationToken ct = default)
            {
                Store.Remove(key);
                return Task.CompletedTask;
            }

            public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
            {
                return Task.CompletedTask;
            }
        }

        private class FakeAiRouter : IUniversalAiRouter
        {
            public AiExecutionResponse ResponseToReturn { get; set; } = new() { Success = true, Content = "" };
            public int ExecuteCallCount = 0;

            public Task<AiExecutionResponse> ExecuteAsync(AiExecutionRequest request)
            {
                ExecuteCallCount++;
                return Task.FromResult(ResponseToReturn);
            }

            public Task<AiExecutionResponse> ExecuteModelDirectAsync(string modelSlug, AiExecutionRequest request, Guid? tenantId = null, Guid? userId = null, CancellationToken ct = default) => ExecuteAsync(request);
            public Task<IReadOnlyList<GatewayModelPresetDto>> GetDynamicGatewayPresetsAsync(string? taskCode = null, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<GatewayModelPresetDto>>(new List<GatewayModelPresetDto>());
            public Task<PedagogicalWalkthroughDto> GenerateWalkthroughAsync(string questionContent, List<string> options, string correctAnswer, string domainCode, AgentPersonaType persona = AgentPersonaType.SupremeArbiter, string? tenantId = null) => throw new NotImplementedException();
            public AiSystemConfigDto GetSystemConfig() => throw new NotImplementedException();
            public void UpdateSystemConfig(AiSystemConfigDto config) => throw new NotImplementedException();
            public void RegisterTenantByok(TenantByokConfigDto byokConfig) => throw new NotImplementedException();
            public Task<Dictionary<string, (bool Success, long LatencyMs, string Message)>> TestProvidersAsync() => throw new NotImplementedException();
            public Task RefreshRegistryFromDatabaseAsync() => Task.CompletedTask;
            public Task<IReadOnlyList<AiModelRegistryDto>> GetActiveNormalizedModelsAsync(string? taskCode = null) => Task.FromResult<IReadOnlyList<AiModelRegistryDto>>(new List<AiModelRegistryDto>());
            public Task<IReadOnlyList<AiModelRegistryDto>> GetActiveNormalizedModelsAsync(CognitiveArchetype? archetype = null, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AiModelRegistryDto>>(new List<AiModelRegistryDto>());
        }

        [Fact]
        public async Task ConvertImageToLatexAsync_WhenCacheHit_ShouldReturnCachedValueWithoutCallingAiRouter()
        {
            // Arrange
            var fakeCache = new FakeCacheService();
            var fakeRouter = new FakeAiRouter();
            var service = new VisionMathService(fakeRouter, fakeCache, NullLogger<VisionMathService>.Instance);

            var imageBytes = Encoding.UTF8.GetBytes("fake_image_bytes_content_for_math_testing_12345");
            string hashKey;
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                hashKey = Convert.ToHexString(sha.ComputeHash(imageBytes)).ToLowerInvariant();
            }
            fakeCache.Store[$"math:vision:{hashKey}"] = "$\\frac{x^2 - 1}{x + 1} = x - 1$";

            // Act
            var result = await service.ConvertImageToLatexAsync(imageBytes, "image/png");

            // Assert
            Assert.Equal("$\\frac{x^2 - 1}{x + 1} = x - 1$", result);
            Assert.Equal(0, fakeRouter.ExecuteCallCount);
        }

        [Fact]
        public async Task ConvertImageToLatexAsync_WhenCacheMiss_ShouldCallAiRouterAndCacheResult()
        {
            // Arrange
            var fakeCache = new FakeCacheService();
            var fakeRouter = new FakeAiRouter
            {
                ResponseToReturn = new AiExecutionResponse
                {
                    Success = true,
                    Content = "```latex\n\\sqrt{x^2 + 2x + 1} = |x + 1|\n```"
                }
            };
            var service = new VisionMathService(fakeRouter, fakeCache, NullLogger<VisionMathService>.Instance);

            var imageBytes = Encoding.UTF8.GetBytes("fake_image_bytes_content_for_math_testing_67890");

            // Act
            var result = await service.ConvertImageToLatexAsync(imageBytes, "image/png");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("$\\sqrt{x^2 + 2x + 1} = |x + 1|$", result);
            Assert.Equal(1, fakeRouter.ExecuteCallCount);
            Assert.Equal(1, fakeCache.SetCallCount);
        }

        [Fact]
        public async Task ConvertImageToLatexAsync_WhenNullOrEmptyBytes_ShouldReturnNull()
        {
            var fakeCache = new FakeCacheService();
            var fakeRouter = new FakeAiRouter();
            var service = new VisionMathService(fakeRouter, fakeCache, NullLogger<VisionMathService>.Instance);

            var result = await service.ConvertImageToLatexAsync(Array.Empty<byte>());
            Assert.Null(result);
        }
    }
}
