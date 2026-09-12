using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Arena
{
    public class DynamicArenaGamePlugin : IArenaGamePlugin
    {
        private readonly GameshowManifest _manifest;

        public DynamicArenaGamePlugin(GameshowManifest manifest)
        {
            _manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        }

        public string GameCode => _manifest.GameCode;
        public string DisplayName => _manifest.DisplayName;
        public string Description => _manifest.Description;
        public string Icon => _manifest.Icon;
        public int MinPlayers => _manifest.MinPlayers;
        public int MaxPlayers => _manifest.MaxPlayers;
        public GameshowManifest Manifest => _manifest;

        public Task InitializeRoomAsync(ArenaRoomState state)
        {
            state.CustomData["Manifest"] = _manifest;
            state.CustomData["LayoutShell"] = _manifest.LayoutShell;
            state.CustomData["ContentionMode"] = _manifest.ContentionMode;
            state.CustomData["Lifelines"] = _manifest.EnabledLifelines;
            state.CustomData["UsedLifelines"] = new ConcurrentDictionary<string, int>();

            var firstRound = _manifest.Rounds.FirstOrDefault();
            state.StageDurationSeconds = firstRound?.DurationSeconds ?? 30;
            state.CurrentStage = firstRound != null ? $"ROUND_{firstRound.RoundIndex}" : "ROUND_1";
            state.StageStartTimeUtc = DateTime.UtcNow;

            return Task.CompletedTask;
        }

        public Task<bool> HandleBuzzerAsync(ArenaRoomState state, string playerId, long clientTimestampMs)
        {
            // Chỉ áp dụng khi luật là cướp chuông
            if (_manifest.ContentionMode != "BUZZER_FASTEST" && _manifest.ContentionMode != "SELECTIVE_PICK")
            {
                return Task.FromResult(false);
            }

            lock (state)
            {
                if (!string.IsNullOrEmpty(state.BuzzerWinnerPlayerId))
                {
                    return Task.FromResult(false);
                }

                if (state.Players.TryGetValue(playerId, out var player) && player.Status == "ACTIVE")
                {
                    state.BuzzerWinnerPlayerId = playerId;
                    state.BuzzerTimestampMs = Stopwatch.GetTimestamp();
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        public Task HandleAnswerAsync(ArenaRoomState state, string playerId, string answerPayload)
        {
            if (!state.Players.TryGetValue(playerId, out var player) || player.Status == "ELIMINATED")
            {
                return Task.CompletedTask;
            }

            var q = (state.Questions.Count > state.CurrentQuestionIndex && state.CurrentQuestionIndex >= 0)
                ? state.Questions[state.CurrentQuestionIndex]
                : null;

            bool isCorrect = false;
            if (q != null && !string.IsNullOrEmpty(answerPayload))
            {
                isCorrect = answerPayload.Trim().Equals(q.AnswerRaw.Trim(), StringComparison.OrdinalIgnoreCase);
            }

            // 1. Áp dụng luật Điểm & Vị trí
            if (isCorrect)
            {
                int earned = q?.Points ?? _manifest.ScoringRule.BasePointsPerCorrect;
                if (_manifest.ScoringRule.StreakMultiplier && player.Streak > 0)
                {
                    earned = (int)(earned * (1 + player.Streak * 0.2));
                }

                player.Score += earned;
                player.Streak++;

                if (_manifest.LayoutShell == "STEP_LADDER")
                {
                    player.StepPosition = Math.Min(15, player.StepPosition + 1);
                }
            }
            else
            {
                player.Streak = 0;
                player.Score = Math.Max(0, player.Score - _manifest.ScoringRule.PenaltyPerWrong);

                if (_manifest.ScoringRule.DropToZeroOnWrong || _manifest.LayoutShell == "STEP_LADDER")
                {
                    // Kiểm tra mốc an toàn
                    int fallback = 0;
                    if (_manifest.SurvivalRule.SafeMilestones != null && _manifest.SurvivalRule.SafeMilestones.Any())
                    {
                        var passedMilestones = _manifest.SurvivalRule.SafeMilestones.Where(m => m <= player.StepPosition);
                        fallback = passedMilestones.Any() ? passedMilestones.Max() : 0;
                    }
                    player.StepPosition = fallback;
                }

                // 2. Áp dụng luật Sinh tử
                if (_manifest.SurvivalRule.Type == "SUDDEN_DEATH")
                {
                    player.Status = "ELIMINATED";
                }
                else if (_manifest.SurvivalRule.Type == "STRIKE_LIMIT")
                {
                    int strikes = 1;
                    if (state.CustomData.TryGetValue($"Strikes_{playerId}", out var sObj) && sObj is int currentStrikes)
                    {
                        strikes = currentStrikes + 1;
                    }
                    state.CustomData[$"Strikes_{playerId}"] = strikes;

                    if (strikes >= _manifest.SurvivalRule.MaxStrikes)
                    {
                        player.Status = "ELIMINATED";
                    }
                }
            }

            // Reset chuông sau khi trả lời
            lock (state)
            {
                state.BuzzerWinnerPlayerId = null;
                state.BuzzerTimestampMs = 0;
            }

            return Task.CompletedTask;
        }

        public Task HandleActionAsync(ArenaRoomState state, string playerId, string actionType, string payload)
        {
            if (actionType == "USE_LIFELINE")
            {
                // Sử dụng phao cứu sinh: 50_50, ASK_AI, POLL_AUDIENCE, STAR_OF_HOPE, etc.
                state.CustomData[$"UsedLifeline_{playerId}_{payload}"] = true;
            }
            else if (actionType == "REVIVE_PLAYER" && state.Players.TryGetValue(payload, out var p))
            {
                p.Status = "ACTIVE";
            }
            else if (actionType == "RESET_BUZZER")
            {
                lock (state)
                {
                    state.BuzzerWinnerPlayerId = null;
                    state.BuzzerTimestampMs = 0;
                }
            }

            return Task.CompletedTask;
        }

        public Task TransitionStageAsync(ArenaRoomState state, string nextStage)
        {
            state.CurrentStage = nextStage;
            state.StageStartTimeUtc = DateTime.UtcNow;

            lock (state)
            {
                state.BuzzerWinnerPlayerId = null;
                state.BuzzerTimestampMs = 0;
            }

            // Nếu nextStage khớp với 1 Round trong Manifest, cập nhật thời lượng vòng đó
            var round = _manifest.Rounds.FirstOrDefault(r => $"ROUND_{r.RoundIndex}".Equals(nextStage, StringComparison.OrdinalIgnoreCase));
            if (round != null)
            {
                state.StageDurationSeconds = round.DurationSeconds;
            }

            return Task.CompletedTask;
        }
    }
}
