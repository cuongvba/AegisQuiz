using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.Infrastructure.Arena
{
    public class ArenaRoomManager
    {
        private readonly ConcurrentDictionary<string, ArenaRoomState> _rooms = new();
        private readonly Dictionary<string, IArenaGamePlugin> _plugins = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, GameshowManifest> _dynamicTemplates = new(StringComparer.OrdinalIgnoreCase);

        public ArenaRoomManager(IEnumerable<IArenaGamePlugin> plugins)
        {
            foreach (var plugin in plugins)
            {
                _plugins[plugin.GameCode] = plugin;
            }

            SeedDefaultDynamicTemplates();
        }

        private void SeedDefaultDynamicTemplates()
        {
            // 1. Ai Là Triệu Phú (Millionaire Challenge)
            var millionaire = new GameshowManifest
            {
                GameCode = "TRIEU_PHU_AI",
                DisplayName = "Ai Là Triệu Phú (Millionaire)",
                Description = "Chinh phục 15 câu hỏi trí tuệ leo thang với 3 mốc an toàn và 4 quyền trợ giúp phao cứu sinh.",
                Icon = "Trophy",
                MinPlayers = 1,
                MaxPlayers = 1,
                LayoutShell = "STEP_LADDER",
                ContentionMode = "TURN_ROUND_ROBIN",
                ThemeColor = "#1e3a8a",
                IsBuiltIn = true,
                CreatedBy = "Aegis Studio",
                ScoringRule = new ScoringRuleConfig
                {
                    BasePointsPerCorrect = 1000,
                    DropToZeroOnWrong = true,
                    LadderPoints = new List<int> { 200, 400, 600, 1000, 2000, 3000, 6000, 10000, 14000, 22000, 30000, 40000, 60000, 85000, 150000 }
                },
                SurvivalRule = new SurvivalRuleConfig
                {
                    Type = "STRIKE_LIMIT",
                    MaxStrikes = 1,
                    SafeMilestones = new List<int> { 5, 10 }
                },
                EnabledLifelines = new List<LifelineConfig>
                {
                    new() { Code = "50_50", Name = "Trợ giúp 50:50", Icon = "Zap", UsageLimit = 1 },
                    new() { Code = "ASK_AI", Name = "Hỏi Gia Sư AI Socrates", Icon = "Brain", UsageLimit = 1 },
                    new() { Code = "POLL_AUDIENCE", Name = "Hỏi Ý Kiến Khán Giả", Icon = "Users", UsageLimit = 1 },
                    new() { Code = "CHANGE_QUESTION", Name = "Đổi Câu Hỏi Khác", Icon = "RefreshCw", UsageLimit = 1 }
                },
                Rounds = new List<RoundDefinition>
                {
                    new() { RoundIndex = 1, RoundName = "Khởi Động Mốc 1 (Câu 1 - 5)", DurationSeconds = 30 },
                    new() { RoundIndex = 2, RoundName = "Vượt Dốc Mốc 2 (Câu 6 - 10)", DurationSeconds = 45 },
                    new() { RoundIndex = 3, RoundName = "Đỉnh Cao Tri Thức (Câu 11 - 15)", DurationSeconds = 60 }
                }
            };
            _dynamicTemplates[millionaire.GameCode] = millionaire;

            // 2. Kẻ Săn Mồi (The Chase Vietnam)
            var theChase = new GameshowManifest
            {
                GameCode = "THE_CHASE",
                DisplayName = "Kẻ Săn Mồi (The Chase)",
                Description = "Cuộc đấu trí nghẹt thở giữa Đội Người Chơi và 'Kẻ Săn Mồi AI' trên bậc thang truy đuổi.",
                Icon = "Flame",
                MinPlayers = 1,
                MaxPlayers = 4,
                LayoutShell = "STEP_LADDER",
                ContentionMode = "BUZZER_FASTEST",
                ThemeColor = "#dc2626",
                IsBuiltIn = true,
                CreatedBy = "Aegis Studio",
                ScoringRule = new ScoringRuleConfig
                {
                    BasePointsPerCorrect = 100,
                    PenaltyPerWrong = 50,
                    SpeedBonus = true
                },
                SurvivalRule = new SurvivalRuleConfig { Type = "SUDDEN_DEATH" },
                EnabledLifelines = new List<LifelineConfig>
                {
                    new() { Code = "TIME_FREEZE", Name = "Đóng Băng Kẻ Săn Mồi 10s", Icon = "Shield", UsageLimit = 1 }
                },
                Rounds = new List<RoundDefinition>
                {
                    new() { RoundIndex = 1, RoundName = "Tích Lũy Điểm Mặt Đối Mặt", DurationSeconds = 60 },
                    new() { RoundIndex = 2, RoundName = "Vượt Qua Bậc Thang Kẻ Săn Mồi", DurationSeconds = 45 },
                    new() { RoundIndex = 3, RoundName = "Chung Kết Đuổi Bắt 120s", DurationSeconds = 120 }
                }
            };
            _dynamicTemplates[theChase.GameCode] = theChase;

            // 3. Đuổi Hình Bắt Chữ (Visual Charades)
            var visualCharades = new GameshowManifest
            {
                GameCode = "DUOI_HINH_BAT_CHU",
                DisplayName = "Đuổi Hình Bắt Chữ",
                Description = "Giải mã các câu đố hình tượng thâm thúy dân gian qua bảng lật tranh ẩn số trung tâm.",
                Icon = "Sparkles",
                MinPlayers = 2,
                MaxPlayers = 8,
                LayoutShell = "HIDDEN_TILES",
                ContentionMode = "BUZZER_FASTEST",
                ThemeColor = "#10b981",
                IsBuiltIn = true,
                CreatedBy = "Aegis Studio",
                ScoringRule = new ScoringRuleConfig { BasePointsPerCorrect = 20, SpeedBonus = true },
                SurvivalRule = new SurvivalRuleConfig { Type = "ACCUMULATIVE" },
                Rounds = new List<RoundDefinition>
                {
                    new() { RoundIndex = 1, RoundName = "Lật Mảnh Ghép Trực Quan", DurationSeconds = 30 }
                }
            };
            _dynamicTemplates[visualCharades.GameCode] = visualCharades;

            // 4. Đấu Trí 1 vs 100 (The Mob Arena)
            var oneVsHundred = new GameshowManifest
            {
                GameCode = "DAU_TRI_100",
                DisplayName = "Đấu Trí 1 vs 100",
                Description = "1 Nhân vật chính đối đầu trực diện 100 người chơi thuộc Đấu Trường Quần Hùng (The Mob).",
                Icon = "Users",
                MinPlayers = 2,
                MaxPlayers = 100,
                LayoutShell = "MEGA_GRID",
                ContentionMode = "SIMULTANEOUS_ALL",
                ThemeColor = "#8b5cf6",
                IsBuiltIn = true,
                CreatedBy = "Aegis Studio",
                ScoringRule = new ScoringRuleConfig { BasePointsPerCorrect = 50, StreakMultiplier = true },
                SurvivalRule = new SurvivalRuleConfig { Type = "SUDDEN_DEATH" },
                EnabledLifelines = new List<LifelineConfig>
                {
                    new() { Code = "ASK_THE_MOB", Name = "Hỏi Thăm Dò 100 Người", Icon = "Users", UsageLimit = 2 }
                },
                Rounds = new List<RoundDefinition>
                {
                    new() { RoundIndex = 1, RoundName = "Sàng Lọc Quần Hùng", DurationSeconds = 20 },
                    new() { RoundIndex = 2, RoundName = "Đỉnh Cao Đơn Đấu", DurationSeconds = 30 }
                }
            };
            _dynamicTemplates[oneVsHundred.GameCode] = oneVsHundred;
        }

        public IEnumerable<IArenaGamePlugin> GetAvailablePlugins()
        {
            var list = _plugins.Values.ToList();
            foreach (var manifest in _dynamicTemplates.Values)
            {
                list.Add(new DynamicArenaGamePlugin(manifest));
            }
            return list;
        }

        public IEnumerable<GameshowManifest> GetAllTemplates()
        {
            var list = _dynamicTemplates.Values.ToList();
            return list;
        }

        public GameshowManifest? GetTemplate(string gameCode) =>
            _dynamicTemplates.TryGetValue(gameCode, out var m) ? m : null;

        public bool SaveTemplate(GameshowManifest manifest)
        {
            if (string.IsNullOrWhiteSpace(manifest.GameCode)) return false;
            manifest.CreatedAtUtc = DateTime.UtcNow;
            _dynamicTemplates[manifest.GameCode] = manifest;
            return true;
        }

        public bool DeleteTemplate(string gameCode)
        {
            if (_dynamicTemplates.TryGetValue(gameCode, out var manifest) && manifest.IsBuiltIn)
            {
                return false; // Không cho xóa preset tích hợp sẵn
            }
            return _dynamicTemplates.TryRemove(gameCode, out _);
        }

        public IArenaGamePlugin? GetPlugin(string gameCode)
        {
            if (_plugins.TryGetValue(gameCode, out var plugin))
            {
                return plugin;
            }

            if (_dynamicTemplates.TryGetValue(gameCode, out var manifest))
            {
                return new DynamicArenaGamePlugin(manifest);
            }

            return null;
        }

        public ArenaRoomState CreateRoom(string gameCode, string roomName, string hostUserId, string hostName)
        {
            var plugin = GetPlugin(gameCode) ?? throw new InvalidOperationException($"Không tìm thấy plugin game: {gameCode}");
            var roomId = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

            var state = new ArenaRoomState
            {
                RoomId = roomId,
                GameCode = plugin.GameCode,
                RoomName = string.IsNullOrWhiteSpace(roomName) ? $"{plugin.DisplayName} #{roomId}" : roomName,
                HostUserId = hostUserId,
                CurrentStage = "LOBBY",
                StageStartTimeUtc = DateTime.UtcNow
            };

            // Khởi tạo trạng thái phòng qua plugin
            plugin.InitializeRoomAsync(state).GetAwaiter().GetResult();

            // Tạo ngân hàng câu hỏi mẫu theo từng format gameshow
            state.Questions = GenerateDefaultArenaQuestions(plugin.GameCode);

            // Thêm Host làm người chơi đầu tiên
            var hostPlayer = new ArenaPlayer
            {
                Id = hostUserId,
                Name = string.IsNullOrWhiteSpace(hostName) ? "Host Player" : hostName,
                Avatar = "👑",
                SeatNumber = 1,
                Status = "ACTIVE",
                Score = 0,
                TeamName = "Team A"
            };
            state.Players[hostUserId] = hostPlayer;

            _rooms[roomId] = state;
            return state;
        }

        public ArenaRoomState? GetRoom(string roomId) =>
            _rooms.TryGetValue(roomId, out var room) ? room : null;

        public IEnumerable<ArenaRoomState> ListActiveRooms() => _rooms.Values.ToList();

        public bool JoinRoom(string roomId, ArenaPlayer player)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) return false;
            var plugin = GetPlugin(room.GameCode);
            if (plugin != null && room.Players.Count >= plugin.MaxPlayers)
            {
                player.Status = "SPECTATOR";
            }
            else
            {
                player.SeatNumber = room.Players.Count + 1;
                player.Status = "ACTIVE";
                player.TeamName = (player.SeatNumber % 2 == 1) ? "Team A" : "Team B";
            }

            room.Players[player.Id] = player;
            return true;
        }

        public bool LeaveRoom(string roomId, string playerId)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) return false;
            room.Players.TryRemove(playerId, out _);
            if (room.Players.IsEmpty)
            {
                _rooms.TryRemove(roomId, out _);
            }
            return true;
        }

        public async Task<bool> PressBuzzerAsync(string roomId, string playerId, long clientTimestampMs)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) return false;
            var plugin = GetPlugin(room.GameCode);
            if (plugin == null) return false;

            return await plugin.HandleBuzzerAsync(room, playerId, clientTimestampMs);
        }

        public async Task SubmitAnswerAsync(string roomId, string playerId, string answerPayload)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) return;
            var plugin = GetPlugin(room.GameCode);
            if (plugin == null) return;

            await plugin.HandleAnswerAsync(room, playerId, answerPayload);
        }

        public async Task ExecuteActionAsync(string roomId, string playerId, string actionType, string payload)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) return;
            var plugin = GetPlugin(room.GameCode);
            if (plugin == null) return;

            await plugin.HandleActionAsync(room, playerId, actionType, payload);
        }

        public async Task TransitionStageAsync(string roomId, string nextStage)
        {
            if (!_rooms.TryGetValue(roomId, out var room)) return;
            var plugin = GetPlugin(room.GameCode);
            if (plugin == null) return;

            await plugin.TransitionStageAsync(room, nextStage);
        }

        private static List<ArenaQuestionItem> GenerateDefaultArenaQuestions(string gameCode)
        {
            var list = new List<ArenaQuestionItem>();

            switch (gameCode.ToUpperInvariant())
            {
                case "OLYMPIA":
                    list.Add(new ArenaQuestionItem
                    {
                        Content = "Ngọn núi cao nhất thế giới Everest nằm trên dãy núi nào?",
                        Options = new List<string> { "Himalaya", "Andes", "Alps", "Rocky" },
                        AnswerRaw = "Himalaya",
                        Points = 10,
                        TimeLimitSeconds = 15,
                        Category = "ĐỊA LÝ"
                    });
                    list.Add(new ArenaQuestionItem
                    {
                        Content = "Ai là vị vua sáng lập ra triều đại nhà Lý năm 1009?",
                        Options = new List<string> { "Lý Công Uẩn", "Lý Thường Kiệt", "Lý Thánh Tông", "Lý Phật Mã" },
                        AnswerRaw = "Lý Công Uẩn",
                        Points = 20,
                        TimeLimitSeconds = 20,
                        Category = "LỊCH SỬ"
                    });
                    list.Add(new ArenaQuestionItem
                    {
                        Content = "Vận tốc của ánh sáng trong chân không xấp xỉ bằng bao nhiêu km/s?",
                        Options = new List<string> { "300.000 km/s", "150.000 km/s", "30.000 km/s", "1.080.000 km/s" },
                        AnswerRaw = "300.000 km/s",
                        Points = 30,
                        TimeLimitSeconds = 30,
                        Category = "VẬT LÝ"
                    });
                    break;

                case "GOLDEN_BELL":
                    for (int i = 1; i <= 10; i++)
                    {
                        list.Add(new ArenaQuestionItem
                        {
                            Content = $"[Câu {i}/50] Thành phố nào sau đây được mệnh danh là 'Thành phố hoa phượng đỏ'?",
                            Options = new List<string> { "Hải Phòng", "Đà Nẵng", "Cần Thơ", "Huế" },
                            AnswerRaw = "Hải Phòng",
                            Points = 10,
                            TimeLimitSeconds = 15,
                            Category = "ĐỊA LÝ"
                        });
                    }
                    break;

                case "LIGHTNING":
                    string[] riddles = {
                        "Cái gì bạn sở hữu nhưng người khác dùng nhiều hơn bạn?",
                        "Càng thâu lại càng dài, càng cắt lại càng ngắn là cái gì?",
                        "Con gì đầu dê mình ốc?",
                        "Có một tàu điện đi về hướng nam, gió thổi hướng bắc. Hỏi khói tàu bay hướng nào?"
                    };
                    string[] answers = { "Tên của bạn", "Cái quần", "Con dốc", "Tàu điện không có khói" };
                    for (int i = 0; i < riddles.Length; i++)
                    {
                        list.Add(new ArenaQuestionItem
                        {
                            Content = riddles[i],
                            AnswerRaw = answers[i],
                            Points = 10,
                            TimeLimitSeconds = 12,
                            Category = "ĐỐ MẸO"
                        });
                    }
                    break;

                default:
                    list.Add(new ArenaQuestionItem
                    {
                        Content = "Đơn vị tiền tệ chính thức của Việt Nam là gì?",
                        Options = new List<string> { "Việt Nam Đồng (VND)", "USD", "EUR", "JPY" },
                        AnswerRaw = "Việt Nam Đồng (VND)",
                        Points = 200,
                        TimeLimitSeconds = 20,
                        Category = "KINH TẾ"
                    });
                    break;
            }

            return list;
        }
    }
}
