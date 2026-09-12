using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AegisQuiz.Application.Interfaces
{
    public class ArenaPlayer
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public int Score { get; set; } = 0;
        public bool IsConnected { get; set; } = true;
        public int SeatNumber { get; set; } = 1;
        public string Status { get; set; } = "ACTIVE"; // "ACTIVE", "ELIMINATED", "BUZZED", "SPECTATOR"
        public string TeamName { get; set; } = string.Empty; // For team-based games like University Challenge
        public int Streak { get; set; } = 0;
        public int StepPosition { get; set; } = 0; // For Nhanh Nhu Chop
        public bool StarOfHopeUsed { get; set; } = false; // For Olympia
    }

    public class ArenaQuestionItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "SINGLE";
        public List<string> Options { get; set; } = new();
        public string AnswerRaw { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public int Points { get; set; } = 10;
        public string? MediaUrl { get; set; }
        public int TimeLimitSeconds { get; set; } = 20;
        public string Category { get; set; } = "GENERAL";
        public string? Hint { get; set; }
    }

    public class ArenaRoomState
    {
        public string RoomId { get; set; } = string.Empty;
        public string GameCode { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string HostUserId { get; set; } = string.Empty;
        public string CurrentStage { get; set; } = "LOBBY"; // "LOBBY", "ROUND_1", "ROUND_2", "ROUND_3", "ROUND_4", "FINISHED"
        public int CurrentQuestionIndex { get; set; } = 0;
        public List<ArenaQuestionItem> Questions { get; set; } = new();
        public ConcurrentDictionary<string, ArenaPlayer> Players { get; set; } = new();
        public string? BuzzerWinnerPlayerId { get; set; }
        public long BuzzerTimestampMs { get; set; } = 0;
        public DateTime StageStartTimeUtc { get; set; } = DateTime.UtcNow;
        public int StageDurationSeconds { get; set; } = 30;
        public Dictionary<string, object> CustomData { get; set; } = new();
    }

    public interface IArenaGamePlugin
    {
        string GameCode { get; }
        string DisplayName { get; }
        string Description { get; }
        string Icon { get; }
        int MinPlayers { get; }
        int MaxPlayers { get; }

        Task InitializeRoomAsync(ArenaRoomState state);
        Task<bool> HandleBuzzerAsync(ArenaRoomState state, string playerId, long clientTimestampMs);
        Task HandleAnswerAsync(ArenaRoomState state, string playerId, string answerPayload);
        Task HandleActionAsync(ArenaRoomState state, string playerId, string actionType, string payload);
        Task TransitionStageAsync(ArenaRoomState state, string nextStage);
    }

    public class ScoringRuleConfig
    {
        public int BasePointsPerCorrect { get; set; } = 10;
        public int PenaltyPerWrong { get; set; } = 0;
        public bool SpeedBonus { get; set; } = false;
        public bool StreakMultiplier { get; set; } = false;
        public bool DropToZeroOnWrong { get; set; } = false;
        public List<int> LadderPoints { get; set; } = new();
    }

    public class SurvivalRuleConfig
    {
        public string Type { get; set; } = "ACCUMULATIVE"; // "ACCUMULATIVE", "SUDDEN_DEATH", "STRIKE_LIMIT"
        public int MaxStrikes { get; set; } = 3;
        public List<int> SafeMilestones { get; set; } = new();
    }

    public class LifelineConfig
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "Sparkles";
        public int UsageLimit { get; set; } = 1;
        public string Description { get; set; } = string.Empty;
    }

    public class RoundDefinition
    {
        public int RoundIndex { get; set; } = 1;
        public string RoundName { get; set; } = "Vòng 1";
        public int DurationSeconds { get; set; } = 30;
        public string QuestionCategory { get; set; } = "GENERAL";
        public double PointsMultiplier { get; set; } = 1.0;
    }

    public class GameshowManifest
    {
        public string GameCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "Trophy";
        public int MinPlayers { get; set; } = 1;
        public int MaxPlayers { get; set; } = 100;
        public string LayoutShell { get; set; } = "PODIUM"; // "PODIUM", "MEGA_GRID", "STEP_LADDER", "RADIAL_WHEEL", "TOPIC_MATRIX", "HIDDEN_TILES"
        public string ContentionMode { get; set; } = "BUZZER_FASTEST"; // "BUZZER_FASTEST", "SIMULTANEOUS_ALL", "TURN_ROUND_ROBIN", "SELECTIVE_PICK"
        public ScoringRuleConfig ScoringRule { get; set; } = new();
        public SurvivalRuleConfig SurvivalRule { get; set; } = new();
        public List<LifelineConfig> EnabledLifelines { get; set; } = new();
        public List<RoundDefinition> Rounds { get; set; } = new();
        public string ThemeColor { get; set; } = "#f59e0b";
        public bool IsBuiltIn { get; set; } = false;
        public string CreatedBy { get; set; } = "System";
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}

