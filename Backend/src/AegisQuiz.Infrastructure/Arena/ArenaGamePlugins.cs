using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Arena
{
    // ══════════════════════════════════════════════════════════════════════════════
    // 1. OLYMPIA SUPREME PLUGIN (ĐƯỜNG LÊN ĐỈNH TRI THỨC)
    // ══════════════════════════════════════════════════════════════════════════════
    public class OlympiaPlugin : IArenaGamePlugin
    {
        public string GameCode => "OLYMPIA";
        public string DisplayName => "Olympia Supreme (Đường Lên Đỉnh Tri Thức)";
        public string Description => "Format 4 vòng thi huyền thoại: Khởi Động 60s, Vượt Chướng Ngại Vật ma trận ảnh, Tăng Tốc mili-giây, Về Đích & Ngôi Sao Hy Vọng cướp điểm!";
        public string Icon => "🏛️";
        public int MinPlayers => 1;
        public int MaxPlayers => 4;

        public Task InitializeRoomAsync(ArenaRoomState state)
        {
            state.CurrentStage = "KHOI_DONG";
            state.CurrentQuestionIndex = 0;
            state.CustomData["cnvKeyword"] = "TRÍ TUỆ NHÂN TẠO";
            state.CustomData["cnvRevealedRows"] = new List<int>();
            state.CustomData["starOfHopeActive"] = false;
            return Task.CompletedTask;
        }

        public Task<bool> HandleBuzzerAsync(ArenaRoomState state, string playerId, long clientTimestampMs)
        {
            if (string.IsNullOrEmpty(state.BuzzerWinnerPlayerId))
            {
                state.BuzzerWinnerPlayerId = playerId;
                state.BuzzerTimestampMs = clientTimestampMs;
                if (state.Players.TryGetValue(playerId, out var p))
                {
                    p.Status = "BUZZED";
                }
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task HandleAnswerAsync(ArenaRoomState state, string playerId, string answerPayload)
        {
            if (!state.Players.TryGetValue(playerId, out var player)) return Task.CompletedTask;

            var q = state.Questions.ElementAtOrDefault(state.CurrentQuestionIndex);
            if (q == null) return Task.CompletedTask;

            bool isCorrect = string.Equals(answerPayload.Trim(), q.AnswerRaw.Trim(), StringComparison.OrdinalIgnoreCase);
            bool isStar = state.CustomData.TryGetValue("starOfHopeActive", out var starVal) && (bool)starVal;

            int points = q.Points > 0 ? q.Points : 10;
            if (state.CurrentStage == "TANG_TOC")
            {
                // Tăng tốc phân hạng điểm: Người nhanh nhất 40đ, tiếp 30đ, 20đ, 10đ
                points = 40;
            }

            if (isCorrect)
            {
                player.Score += isStar ? points * 2 : points;
                player.Streak++;
            }
            else
            {
                if (isStar)
                {
                    player.Score = Math.Max(0, player.Score - points);
                }
                player.Streak = 0;
            }

            state.CustomData["starOfHopeActive"] = false;
            state.BuzzerWinnerPlayerId = null;
            return Task.CompletedTask;
        }

        public Task HandleActionAsync(ArenaRoomState state, string playerId, string actionType, string payload)
        {
            if (actionType == "USE_STAR_OF_HOPE" && state.Players.TryGetValue(playerId, out var player))
            {
                if (!player.StarOfHopeUsed)
                {
                    player.StarOfHopeUsed = true;
                    state.CustomData["starOfHopeActive"] = true;
                }
            }
            else if (actionType == "GUESS_CNV" && state.Players.TryGetValue(playerId, out var p))
            {
                var keyword = state.CustomData.GetValueOrDefault("cnvKeyword")?.ToString() ?? "";
                if (string.Equals(payload.Trim(), keyword.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    p.Score += 80;
                    state.CustomData["cnvSolved"] = true;
                }
            }
            return Task.CompletedTask;
        }

        public Task TransitionStageAsync(ArenaRoomState state, string nextStage)
        {
            state.CurrentStage = nextStage;
            state.CurrentQuestionIndex = 0;
            state.BuzzerWinnerPlayerId = null;
            state.StageStartTimeUtc = DateTime.UtcNow;
            return Task.CompletedTask;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // 2. RUNG CHUÔNG VÀNG (GOLDEN BELL MEGA-GRID BATTLE ROYALE)
    // ══════════════════════════════════════════════════════════════════════════════
    public class GoldenBellPlugin : IArenaGamePlugin
    {
        public string GameCode => "GOLDEN_BELL";
        public string DisplayName => "Rung Chuông Vàng (Mega-Grid Battle Royale)";
        public string Description => "Đấu trường 100 đến 10,000 thí sinh trên sàn đấu số trực tiếp. Sai 1 câu là rời sàn! Minigame Cứu Trợ của Thầy Cô hồi sinh cả trường.";
        public string Icon => "🔔";
        public int MinPlayers => 1;
        public int MaxPlayers => 10000;

        public Task InitializeRoomAsync(ArenaRoomState state)
        {
            state.CurrentStage = "FLOOR_BATTLE";
            state.CurrentQuestionIndex = 0;
            int seat = 1;
            foreach (var p in state.Players.Values)
            {
                p.SeatNumber = seat++;
                p.Status = "ACTIVE";
            }
            return Task.CompletedTask;
        }

        public Task<bool> HandleBuzzerAsync(ArenaRoomState state, string playerId, long clientTimestampMs)
        {
            // Rung chuông vàng dùng cơ chế nộp bảng cùng lúc, không cướp chuông ngoại trừ câu 50
            return Task.FromResult(true);
        }

        public Task HandleAnswerAsync(ArenaRoomState state, string playerId, string answerPayload)
        {
            if (!state.Players.TryGetValue(playerId, out var player)) return Task.CompletedTask;
            if (player.Status != "ACTIVE") return Task.CompletedTask;

            var q = state.Questions.ElementAtOrDefault(state.CurrentQuestionIndex);
            if (q == null) return Task.CompletedTask;

            bool isCorrect = string.Equals(answerPayload.Trim(), q.AnswerRaw.Trim(), StringComparison.OrdinalIgnoreCase);
            if (isCorrect)
            {
                player.Score += 10;
                player.Streak++;
            }
            else
            {
                player.Status = "ELIMINATED"; // Rời sàn đấu
            }
            return Task.CompletedTask;
        }

        public Task HandleActionAsync(ArenaRoomState state, string playerId, string actionType, string payload)
        {
            if (actionType == "TEACHER_RESCUE")
            {
                // Hồi sinh tất cả thí sinh hoặc 50% thí sinh bị loại
                var eliminated = state.Players.Values.Where(p => p.Status == "ELIMINATED").ToList();
                foreach (var p in eliminated)
                {
                    p.Status = "ACTIVE";
                }
                state.CustomData["lastRescueMessage"] = $"Thầy cô đã cứu trợ thành công {eliminated.Count} thí sinh quay lại sàn đấu!";
            }
            return Task.CompletedTask;
        }

        public Task TransitionStageAsync(ArenaRoomState state, string nextStage)
        {
            state.CurrentStage = nextStage;
            state.CurrentQuestionIndex++;
            state.StageStartTimeUtc = DateTime.UtcNow;
            return Task.CompletedTask;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // 3. CHIẾC NÓN KỲ DIỆU (QUANTUM LUCKY WHEEL)
    // ══════════════════════════════════════════════════════════════════════════════
    public class LuckyWheelPlugin : IArenaGamePlugin
    {
        public string GameCode => "LUCKY_WHEEL";
        public string DisplayName => "Chiếc Nón Kỳ Diệu (Quantum Wheel)";
        public string Description => "Vòng quay vật lý Canvas 60fps chân thực, bảng lật mở ô chữ realtime, ô Nhân Đôi, Mất Lượt, Phần Thưởng Bí Ẩn!";
        public string Icon => "🎡";
        public int MinPlayers => 1;
        public int MaxPlayers => 4;

        public Task InitializeRoomAsync(ArenaRoomState state)
        {
            state.CurrentStage = "SPINNING";
            state.CustomData["targetWord"] = "TRI THỨC LÀ SỨC MẠNH";
            state.CustomData["revealedLetters"] = new List<string> { " " };
            state.CustomData["currentSectorPoints"] = 500;
            state.CustomData["activePlayerIndex"] = 0;
            return Task.CompletedTask;
        }

        public Task<bool> HandleBuzzerAsync(ArenaRoomState state, string playerId, long clientTimestampMs) => Task.FromResult(true);

        public Task HandleAnswerAsync(ArenaRoomState state, string playerId, string answerPayload)
        {
            if (!state.Players.TryGetValue(playerId, out var player)) return Task.CompletedTask;

            var target = state.CustomData.GetValueOrDefault("targetWord")?.ToString() ?? "";
            var revealed = state.CustomData.GetValueOrDefault("revealedLetters") as List<string> ?? new List<string>();

            // Đoán chữ cái
            if (answerPayload.Length == 1)
            {
                var letter = answerPayload.ToUpperInvariant();
                int occurrences = target.Count(c => char.ToUpperInvariant(c) == letter[0]);
                if (occurrences > 0 && !revealed.Contains(letter))
                {
                    revealed.Add(letter);
                    int sectorPts = Convert.ToInt32(state.CustomData.GetValueOrDefault("currentSectorPoints") ?? 500);
                    player.Score += sectorPts * occurrences;
                }
            }
            // Đoán toàn bộ cụm từ
            else if (string.Equals(answerPayload.Trim(), target.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                player.Score += 2000;
                state.CurrentStage = "FINISHED";
            }
            return Task.CompletedTask;
        }

        public Task HandleActionAsync(ArenaRoomState state, string playerId, string actionType, string payload)
        {
            if (actionType == "SPIN_RESULT")
            {
                // Cập nhật kết quả ô quay được (ví dụ: 1000, "MAT_LUOT", "NHAN_DOI")
                if (int.TryParse(payload, out int pts))
                {
                    state.CustomData["currentSectorPoints"] = pts;
                }
                else if (payload == "MAT_LUOT" && state.Players.TryGetValue(playerId, out var p))
                {
                    p.Status = "SKIPPED";
                }
            }
            return Task.CompletedTask;
        }

        public Task TransitionStageAsync(ArenaRoomState state, string nextStage)
        {
            state.CurrentStage = nextStage;
            return Task.CompletedTask;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // 4. UNIVERSITY CHALLENGE (ĐẤU TRƯỜNG ĐẠI HỌC)
    // ══════════════════════════════════════════════════════════════════════════════
    public class UniversityChallengePlugin : IArenaGamePlugin
    {
        public string GameCode => "UNIVERSITY_CHALLENGE";
        public string DisplayName => "University Challenge (Đấu Trường Đại Học)";
        public string Description => "Đối kháng học thuật đỉnh cao giữa các trường đại học (4 vs 4). Starter Buzzer 10đ và chuỗi câu hỏi Bonus 15đ với kênh thảo luận kín.";
        public string Icon => "🎓";
        public int MinPlayers => 2;
        public int MaxPlayers => 8;

        public Task InitializeRoomAsync(ArenaRoomState state)
        {
            state.CurrentStage = "STARTER_QUESTION";
            state.CustomData["teamAScore"] = 0;
            state.CustomData["teamBScore"] = 0;
            state.CustomData["bonusQuestionsRemaining"] = 0;
            return Task.CompletedTask;
        }

        public Task<bool> HandleBuzzerAsync(ArenaRoomState state, string playerId, long clientTimestampMs)
        {
            if (string.IsNullOrEmpty(state.BuzzerWinnerPlayerId))
            {
                state.BuzzerWinnerPlayerId = playerId;
                state.BuzzerTimestampMs = clientTimestampMs;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task HandleAnswerAsync(ArenaRoomState state, string playerId, string answerPayload)
        {
            if (!state.Players.TryGetValue(playerId, out var player)) return Task.CompletedTask;
            var q = state.Questions.ElementAtOrDefault(state.CurrentQuestionIndex);
            if (q == null) return Task.CompletedTask;

            bool isCorrect = string.Equals(answerPayload.Trim(), q.AnswerRaw.Trim(), StringComparison.OrdinalIgnoreCase);

            if (state.CurrentStage == "STARTER_QUESTION")
            {
                if (isCorrect)
                {
                    player.Score += 10;
                    state.CurrentStage = "BONUS_SET";
                    state.CustomData["bonusTeam"] = player.TeamName;
                    state.CustomData["bonusQuestionsRemaining"] = 3;
                }
                else
                {
                    player.Score = Math.Max(0, player.Score - 5); // Penalty ngắt lời MC
                    state.BuzzerWinnerPlayerId = null;
                }
            }
            else if (state.CurrentStage == "BONUS_SET")
            {
                if (isCorrect) player.Score += 5;
                int remaining = Convert.ToInt32(state.CustomData.GetValueOrDefault("bonusQuestionsRemaining") ?? 0) - 1;
                state.CustomData["bonusQuestionsRemaining"] = remaining;
                if (remaining <= 0)
                {
                    state.CurrentStage = "STARTER_QUESTION";
                    state.BuzzerWinnerPlayerId = null;
                }
            }
            return Task.CompletedTask;
        }

        public Task HandleActionAsync(ArenaRoomState state, string playerId, string actionType, string payload) => Task.CompletedTask;
        public Task TransitionStageAsync(ArenaRoomState state, string nextStage)
        {
            state.CurrentStage = nextStage;
            return Task.CompletedTask;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // 5. JEOPARDY! AMERICAN MATRIX
    // ══════════════════════════════════════════════════════════════════════════════
    public class JeopardyPlugin : IArenaGamePlugin
    {
        public string GameCode => "JEOPARDY";
        public string DisplayName => "Jeopardy! American Matrix";
        public string Description => "Bảng ma trận 30 ô chủ đề rực rỡ, format câu hỏi ngược ('Ai là...? / Là gì...?'), Daily Double và Final Jeopardy!";
        public string Icon => "🇺🇸";
        public int MinPlayers => 1;
        public int MaxPlayers => 3;

        public Task InitializeRoomAsync(ArenaRoomState state)
        {
            state.CurrentStage = "SELECT_CLUE";
            state.CustomData["categories"] = new List<string> { "LỊCH SỬ", "KHOA HỌC", "TOÁN HỌC", "VĂN HỌC", "CÔNG NGHỆ", "ĐỊA LÝ" };
            state.CustomData["solvedClues"] = new List<string>();
            return Task.CompletedTask;
        }

        public Task<bool> HandleBuzzerAsync(ArenaRoomState state, string playerId, long clientTimestampMs)
        {
            if (string.IsNullOrEmpty(state.BuzzerWinnerPlayerId))
            {
                state.BuzzerWinnerPlayerId = playerId;
                state.BuzzerTimestampMs = clientTimestampMs;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task HandleAnswerAsync(ArenaRoomState state, string playerId, string answerPayload)
        {
            if (!state.Players.TryGetValue(playerId, out var player)) return Task.CompletedTask;
            var q = state.Questions.ElementAtOrDefault(state.CurrentQuestionIndex);
            if (q == null) return Task.CompletedTask;

            int cluePoints = q.Points > 0 ? q.Points : 200;
            bool isCorrect = string.Equals(answerPayload.Trim(), q.AnswerRaw.Trim(), StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
            {
                player.Score += cluePoints;
            }
            else
            {
                player.Score -= cluePoints;
            }

            state.BuzzerWinnerPlayerId = null;
            state.CurrentStage = "SELECT_CLUE";
            return Task.CompletedTask;
        }

        public Task HandleActionAsync(ArenaRoomState state, string playerId, string actionType, string payload)
        {
            if (actionType == "SELECT_CLUE")
            {
                state.CurrentStage = "CLUE_ACTIVE";
                state.BuzzerWinnerPlayerId = null;
            }
            return Task.CompletedTask;
        }

        public Task TransitionStageAsync(ArenaRoomState state, string nextStage)
        {
            state.CurrentStage = nextStage;
            return Task.CompletedTask;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // 6. NHANH NHƯ CHỚP (LIGHTNING INCLINE)
    // ══════════════════════════════════════════════════════════════════════════════
    public class LightningQuizPlugin : IArenaGamePlugin
    {
        public string GameCode => "LIGHTNING";
        public string DisplayName => "Nhanh Như Chớp (Lightning Incline)";
        public string Description => "Cỗ máy leo dốc 10 bậc đứng! Đúng leo 1 bậc, sai tụt ngay về vạch số 0! 2 phút chinh phục đỉnh cao 10 câu liên tiếp.";
        public string Icon => "⚡";
        public int MinPlayers => 1;
        public int MaxPlayers => 4;

        public Task InitializeRoomAsync(ArenaRoomState state)
        {
            state.CurrentStage = "CLIMBING";
            state.CurrentQuestionIndex = 0;
            foreach (var p in state.Players.Values)
            {
                p.StepPosition = 0;
            }
            return Task.CompletedTask;
        }

        public Task<bool> HandleBuzzerAsync(ArenaRoomState state, string playerId, long clientTimestampMs) => Task.FromResult(true);

        public Task HandleAnswerAsync(ArenaRoomState state, string playerId, string answerPayload)
        {
            if (!state.Players.TryGetValue(playerId, out var player)) return Task.CompletedTask;
            var q = state.Questions.ElementAtOrDefault(state.CurrentQuestionIndex);
            if (q == null) return Task.CompletedTask;

            bool isCorrect = string.Equals(answerPayload.Trim(), q.AnswerRaw.Trim(), StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
            {
                player.StepPosition = Math.Min(10, player.StepPosition + 1);
                player.Score += player.StepPosition * 10;
                if (player.StepPosition == 10)
                {
                    player.Score += 1000; // Giải thưởng đặc biệt 10 bậc đỉnh cao!
                    state.CurrentStage = "FINISHED";
                }
            }
            else
            {
                player.StepPosition = 0; // TỤT VỀ VẠCH SỐ 0!
            }

            state.CurrentQuestionIndex++;
            return Task.CompletedTask;
        }

        public Task HandleActionAsync(ArenaRoomState state, string playerId, string actionType, string payload) => Task.CompletedTask;
        public Task TransitionStageAsync(ArenaRoomState state, string nextStage)
        {
            state.CurrentStage = nextStage;
            return Task.CompletedTask;
        }
    }
}
