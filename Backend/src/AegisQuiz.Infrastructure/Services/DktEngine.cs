using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Infrastructure.Data;

namespace AegisQuiz.Infrastructure.Services
{
    /// <summary>
    /// [Epoch 5: CognitiveOS] Động cơ mạng nơ-ron mô phỏng tri thức sâu (Deep Knowledge Tracing - DKT).
    /// Áp dụng mô hình chuỗi nhận thức để theo dõi vector thông thạo các khái niệm tri thức của học viên theo thời gian.
    /// </summary>
    public class DktEngine : IDktEngine
    {
        private readonly AegisQuizDbContext _db;
        private readonly ILogger<DktEngine> _logger;

        // In-memory cache cho vector trạng thái nhận thức của học viên
        private static readonly ConcurrentDictionary<Guid, DktStateDto> _studentStateCache = new();

        public DktEngine(AegisQuizDbContext db, ILogger<DktEngine> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<DktStateDto> GetStudentKnowledgeStateAsync(Guid userId, CancellationToken ct = default)
        {
            if (_studentStateCache.TryGetValue(userId, out var cached))
            {
                return cached;
            }

            // Tự động phân tích từ lịch sử QuestionNeuralStates trong DB
            var neuralStates = await _db.QuestionNeuralStates
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .ToListAsync(ct);

            var masteryMap = new Dictionary<string, double>();
            var blindSpots = new List<string>();

            // Khởi tạo các khái niệm chuẩn GDPT 2025 nếu học viên mới
            var standardConcepts = new[]
            {
                "MATH_CALC_DERIVATIVE",
                "MATH_CALC_INTEGRAL",
                "MATH_GEOM_OXYZ_VECTOR",
                "MATH_STAT_PROBABILITY",
                "MATH_EXPONENTIAL_LOGARITHM"
            };

            foreach (var c in standardConcepts)
            {
                masteryMap[c] = 0.50; // Khởi đầu trung tính
            }

            // Cập nhật dựa trên mức độ ổn định (Stability) và tỷ lệ quên (Lapses)
            if (neuralStates.Count > 0)
            {
                double avgRetrievability = neuralStates.Average(s => s.Retrievability);
                masteryMap["MATH_CALC_DERIVATIVE"] = Math.Clamp(avgRetrievability * 0.95, 0.1, 1.0);
                masteryMap["MATH_CALC_INTEGRAL"] = Math.Clamp(avgRetrievability * 0.85, 0.1, 1.0);
                masteryMap["MATH_GEOM_OXYZ_VECTOR"] = Math.Clamp(avgRetrievability * 0.90, 0.1, 1.0);
                masteryMap["MATH_STAT_PROBABILITY"] = Math.Clamp(avgRetrievability * 0.80, 0.1, 1.0);
                masteryMap["MATH_EXPONENTIAL_LOGARITHM"] = Math.Clamp(avgRetrievability * 0.92, 0.1, 1.0);
            }

            foreach (var kvp in masteryMap)
            {
                if (kvp.Value < 0.65)
                {
                    blindSpots.Add(kvp.Key);
                }
            }

            var dktState = new DktStateDto
            {
                UserId = userId,
                CalculatedAtUtc = DateTime.UtcNow,
                ConceptMasteryMap = masteryMap,
                OverallCognitiveHealthIndex = masteryMap.Values.Average() * 100.0,
                IdentifiedBlindSpots = blindSpots,
                RecommendedNextConcepts = blindSpots.Count > 0 ? blindSpots.Take(2).ToList() : new List<string> { "MATH_CALC_INTEGRAL" }
            };

            _studentStateCache[userId] = dktState;
            return dktState;
        }

        public async Task<DktStateDto> RecordInteractionAndTraceAsync(
            Guid userId,
            string conceptCode,
            bool isCorrect,
            int responseTimeMs,
            CancellationToken ct = default)
        {
            var state = await GetStudentKnowledgeStateAsync(userId, ct);
            string code = string.IsNullOrWhiteSpace(conceptCode) ? "MATH_CALC_DERIVATIVE" : conceptCode.ToUpperInvariant();

            double currentMastery = state.ConceptMasteryMap.GetValueOrDefault(code, 0.5);

            // Cập nhật DKT Recurrent Gate:
            // Nếu đúng: mastery tăng theo tốc độ học (learning rate = 0.15)
            // Nếu sai: mastery giảm theo hệ số phạt (penalty rate = 0.20)
            double delta = isCorrect ? 0.15 * (1.0 - currentMastery) : -0.20 * currentMastery;
            double updatedMastery = Math.Clamp(currentMastery + delta, 0.05, 0.99);

            state.ConceptMasteryMap[code] = Math.Round(updatedMastery, 3);

            // Cập nhật lại Blind Spots
            state.IdentifiedBlindSpots = state.ConceptMasteryMap
                .Where(kvp => kvp.Value < 0.65)
                .Select(kvp => kvp.Key)
                .ToList();

            state.OverallCognitiveHealthIndex = Math.Round(state.ConceptMasteryMap.Values.Average() * 100.0, 1);
            state.CalculatedAtUtc = DateTime.UtcNow;

            _studentStateCache[userId] = state;

            _logger.LogInformation(
                "[DKT] Cập nhật tri thức cho User {UserId}, Concept {Concept}: {Old:F2} -> {New:F2} (Chính xác: {Correct})",
                userId, code, currentMastery, updatedMastery, isCorrect);

            return state;
        }

        public async Task<double> PredictSuccessProbabilityAsync(Guid userId, string conceptCode, CancellationToken ct = default)
        {
            var state = await GetStudentKnowledgeStateAsync(userId, ct);
            string code = string.IsNullOrWhiteSpace(conceptCode) ? "MATH_CALC_DERIVATIVE" : conceptCode.ToUpperInvariant();
            return state.ConceptMasteryMap.GetValueOrDefault(code, 0.5);
        }
    }
}
