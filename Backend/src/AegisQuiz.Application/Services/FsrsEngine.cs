using System;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Application.Services
{
    /// <summary>
    /// [Epoch 2: Cognitive Neuroscience Engine]
    /// Triển khai chuẩn xác mô hình FSRS v4.5/v5 (Free Spaced Repetition Scheduler).
    /// </summary>
    public class FsrsEngine : IFsrsEngine
    {
        // 17 trọng số tham số chuẩn của FSRS-4.5 / v5
        private static readonly double[] W =
        {
            0.40255, 1.18385, 3.173, 15.69105,   // w0..w3: Initial stability for Again, Hard, Good, Easy
            7.1949,  0.5345,  1.4604, 0.0046,     // w4..w7: Difficulty parameters
            1.54575, 0.1192,  1.01925,            // w8..w10: Stability update for recall
            1.9395,  0.11,    0.29605, 0.22695,   // w11..w14: Stability update for lapse
            0.5668,  4.7941                       // w15..w16: Hard & Easy multiplier
        };

        public double CalculateRetrievability(double stability, TimeSpan elapsed)
        {
            if (stability <= 0.0001) return 0.0;
            var days = Math.Max(0.0, elapsed.TotalDays);
            // Chuẩn FSRS power law: R(t, S) = (1 + 19 * t / S)^-0.5
            return Math.Clamp(Math.Pow(1.0 + 19.0 * (days / stability), -0.5), 0.0, 1.0);
        }

        public FsrsScheduleResult Schedule(
            QuestionNeuralState state,
            FsrsRating rating,
            DateTime reviewTime,
            double requestRetention = 0.9)
        {
            var g = (int)rating;
            var elapsed = state.LastReviewAt.HasValue
                ? reviewTime - state.LastReviewAt.Value
                : TimeSpan.Zero;
            var elapsedDays = Math.Max(0, (int)elapsed.TotalDays);

            var currentR = state.Reps > 0
                ? CalculateRetrievability(state.Stability, elapsed)
                : 1.0;

            double nextD;
            double nextS;
            int nextState;

            if (state.State == 0 || state.Reps == 0) // New Card
            {
                // Khởi tạo D0 và S0
                nextD = InitDifficulty(g);
                nextS = InitStability(g);
                nextState = (g == 1) ? 1 : 2; // Again -> Learning, Good/Easy -> Review
            }
            else // Review / Relearning Card
            {
                nextD = NextDifficulty(state.Difficulty, g);

                if (g == 1) // Lapse (Again)
                {
                    nextS = NextLapseStability(nextD, state.Stability, currentR);
                    nextState = 3; // Relearning
                }
                else // Recall success (Hard, Good, Easy)
                {
                    nextS = NextRecallStability(nextD, state.Stability, currentR, g);
                    nextState = 2; // Review
                }
            }

            // Tính số ngày cho chu kỳ kế tiếp dựa trên retention mong muốn (mặc định 90%)
            var intervalDays = NextInterval(nextS, requestRetention);

            // Xử lý bước ngắn nếu là Again (< 1 ngày)
            if (g == 1 && nextS < 1.0)
            {
                intervalDays = 1; // Học lại trong ngày / ngày mai
            }

            var result = new FsrsScheduleResult
            {
                NewDifficulty = Math.Round(nextD, 2),
                NewStability = Math.Round(nextS, 2),
                Retrievability = Math.Round(currentR, 4),
                ScheduledDays = intervalDays,
                NewState = nextState,
                DueAt = reviewTime.AddDays(intervalDays)
            };

            // Sinh preview cho lần tới
            result.NextIntervals = PreviewNextIntervals(state, reviewTime, requestRetention);

            return result;
        }

        public FsrsIntervalPreview PreviewNextIntervals(
            QuestionNeuralState state,
            DateTime reviewTime,
            double requestRetention = 0.9)
        {
            var preview = new FsrsIntervalPreview();

            // Tính toán trước cho 4 trường hợp
            var againResult = ComputeNextStabilityOnly(state, FsrsRating.Again, reviewTime);
            var hardResult  = ComputeNextStabilityOnly(state, FsrsRating.Hard,  reviewTime);
            var goodResult  = ComputeNextStabilityOnly(state, FsrsRating.Good,  reviewTime);
            var easyResult  = ComputeNextStabilityOnly(state, FsrsRating.Easy,  reviewTime);

            preview.AgainInterval = TimeSpan.FromMinutes(10); // Ngay trong phiên / 10 phút
            preview.HardInterval  = TimeSpan.FromDays(Math.Max(1, NextInterval(hardResult, requestRetention)));
            preview.GoodInterval  = TimeSpan.FromDays(Math.Max(1, NextInterval(goodResult, requestRetention)));
            preview.EasyInterval  = TimeSpan.FromDays(Math.Max(2, NextInterval(easyResult, requestRetention)));

            preview.AgainLabel = "< 10 phút";
            preview.HardLabel  = FormatDays((int)preview.HardInterval.TotalDays);
            preview.GoodLabel  = FormatDays((int)preview.GoodInterval.TotalDays);
            preview.EasyLabel  = FormatDays((int)preview.EasyInterval.TotalDays);

            return preview;
        }

        public QuestionNeuralState InitializeState(Guid userId, Guid questionId, Guid tenantId, int questionBaseDifficulty = 1)
        {
            // Ánh xạ độ khó câu hỏi (1-5) sang độ khó khởi điểm FSRS (3.0 - 7.0)
            var baseD = Math.Clamp(3.0 + (questionBaseDifficulty * 0.8), 1.0, 10.0);

            return new QuestionNeuralState
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QuestionId = questionId,
                TenantId = tenantId,
                Difficulty = baseD,
                Stability = 0.5,
                Retrievability = 1.0,
                ElapsedDays = 0,
                ScheduledDays = 1,
                Reps = 0,
                Lapses = 0,
                State = 0, // New
                LastReviewAt = null,
                DueAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        #region Thuật Toán FSRS Cốt Lõi

        private static double InitStability(int g)
        {
            return Math.Max(0.1, W[g - 1]);
        }

        private static double InitDifficulty(int g)
        {
            var d0 = W[4] - Math.Exp(W[5] * (g - 1)) + 1.0;
            return Math.Clamp(d0, 1.0, 10.0);
        }

        private static double NextDifficulty(double d, int g)
        {
            var delta = -W[6] * (g - 3);
            var nextD = W[7] * InitDifficulty(3) + (1.0 - W[7]) * (d + delta);
            return Math.Clamp(nextD, 1.0, 10.0);
        }

        private static double NextRecallStability(double d, double s, double r, int g)
        {
            var hardPenalty = (g == 2) ? W[15] : 1.0;
            var easyBonus = (g == 4) ? W[16] : 1.0;
            var factor = Math.Exp(W[8]) *
                         (11.0 - d) *
                         Math.Pow(Math.Max(0.01, s), -W[9]) *
                         (Math.Exp(W[10] * (1.0 - r)) - 1.0) *
                         hardPenalty * easyBonus;

            return Math.Max(0.1, s * (1.0 + factor));
        }

        private static double NextLapseStability(double d, double s, double r)
        {
            var factor = W[11] *
                         Math.Pow(Math.Max(0.1, d), -W[12]) *
                         (Math.Pow(s + 1.0, W[13]) - 1.0) *
                         Math.Exp(W[14] * (1.0 - r));

            return Math.Clamp(factor, 0.1, s);
        }

        private static int NextInterval(double stability, double requestRetention)
        {
            // I(r, S) = (S / 19) * (r^-2 - 1)
            var factor = (1.0 / Math.Pow(requestRetention, 2.0)) - 1.0;
            var interval = (stability / 19.0) * factor * 19.0; // Rút gọn = stability * factor
            var days = (int)Math.Round(interval);
            return Math.Max(1, days);
        }

        private double ComputeNextStabilityOnly(QuestionNeuralState state, FsrsRating rating, DateTime reviewTime)
        {
            var g = (int)rating;
            if (state.State == 0 || state.Reps == 0)
                return InitStability(g);

            var elapsed = state.LastReviewAt.HasValue ? reviewTime - state.LastReviewAt.Value : TimeSpan.Zero;
            var currentR = CalculateRetrievability(state.Stability, elapsed);
            var nextD = NextDifficulty(state.Difficulty, g);

            return (g == 1)
                ? NextLapseStability(nextD, state.Stability, currentR)
                : NextRecallStability(nextD, state.Stability, currentR, g);
        }

        private static string FormatDays(int days)
        {
            if (days <= 1) return "1 ngày";
            if (days < 30) return $"{days} ngày";
            var months = days / 30;
            return $"{months} tháng";
        }

        #endregion
    }
}
