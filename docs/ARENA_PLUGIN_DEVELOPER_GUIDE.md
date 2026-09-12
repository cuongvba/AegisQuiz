# 🛠️ Tài Liệu Kỹ Thuật Viên: Kiến Trúc Khung Arena Plugin & Hướng Dẫn Phát Triển

Tài liệu này dành cho **Kỹ sư phần mềm (Software Engineers), Lập trình viên Backend (.NET) và Frontend (React/TypeScript)** tham gia bảo trì, phát triển mở rộng hoặc tích hợp các thể thức Gameshow mới vào nền tảng **AegisQuiz Arena**.

---

## 1. Triết Lý Thiết Kế & Tổng Quan Kiến Trúc

Hệ thống Đấu trường AegisQuiz không sử dụng kiến trúc đóng (hard-coded rules). Thay vào đó, toàn bộ quy tắc trò chơi, cách tính điểm, phân chia vòng thi và cơ chế phân xử tương tác được trừu tượng hóa thông qua **Mô hình Kiến trúc Cắm Rút (Plugin Architecture)** kết hợp các mẫu thiết kế **Strategy Pattern** và **Factory Pattern**.

```
                           ┌──────────────────────────────┐
                           │      ArenaRoomManager        │
                           │ (In-Memory State Coordinator)│
                           └──────────────┬───────────────┘
                                          │ Truy vấn Plugin theo GameCode
                           ┌──────────────▼───────────────┐
                           │     IArenaGamePlugin         │
                           │   (Interface Hợp Đồng Chuẩn)  │
                           └──────────────┬───────────────┘
                                          │
       ┌──────────────┬──────────────┼──────────────┬──────────────┬──────────────┐
       │              │              │              │              │              │
┌──────▼──────┐┌──────▼──────┐┌──────▼──────┐┌──────▼──────┐┌──────▼──────┐┌──────▼──────┐
│OlympiaPlugin││GoldenBellPlg││LuckyWheelPlg││UnivChallenPlg││JeopardyPlg ││LightningPlg │
└─────────────┘└─────────────┘└─────────────┘└─────────────┘└─────────────┘└─────────────┘
```

### Tại sao lại sử dụng In-Memory Engine thay vì ghi trực tiếp xuống Database?
- Các sự kiện gameshow như **bấm chuông cướp điểm (Buzzer)** diễn ra ở cấp độ **mili-giây (ms)** và **micro-giây (μs)**. Nếu mỗi lượt bấm chuông đều thực hiện Transaction xuống cơ sở dữ liệu (PostgreSQL/SQL Server), tình trạng khóa dòng (Row lock) và Network I/O Latency sẽ làm sai lệch thứ tự bấm thực tế của thí sinh.
- `ArenaRoomManager` duy trì trạng thái phòng bằng cấu trúc `ConcurrentDictionary<string, ArenaRoomState>` hoàn toàn trong RAM máy chủ, đảm bảo thời gian phản hồi `< 2ms`. Sau khi trận đấu kết thúc, toàn bộ kết quả tổng kết mới được lưu không đồng bộ (asynchronous background flush) vào Database.

---

## 2. Đặc Tả Giao Diện Lõi Backend (`IArenaGamePlugin`)

Mọi Gameshow muốn hoạt động trong hệ thống bắt buộc phải cài đặt interface `IArenaGamePlugin` nằm tại `AegisQuiz.Application/Interfaces/IArenaGamePlugin.cs`:

```csharp
namespace AegisQuiz.Application.Interfaces;

public interface IArenaGamePlugin
{
    // Mã định danh duy nhất (Ví dụ: "OLYMPIA", "GOLDEN_BELL", "LUCKY_WHEEL")
    string GameCode { get; }

    // Metadata mô tả trò chơi (Tên hiển thị, Icon, Số người chơi tối thiểu/tối đa)
    ArenaGameMetadata Metadata { get; }

    // Khởi tạo trạng thái phòng ban đầu khi Host tạo phòng
    ArenaRoomState InitializeRoomState(string roomId, string roomName, string hostUserId);

    // Xử lý các hành động đặc thù (Quay nón, đặt Ngôi sao hy vọng, Thầy cô cứu trợ...)
    ArenaRoomState ProcessPlayerAction(ArenaRoomState currentState, string playerId, string actionType, string payload);

    // Tính điểm số cộng/trừ dựa theo tính đúng sai và thời gian phản xạ
    int CalculateScore(ArenaRoomState currentState, string playerId, bool isCorrect, long elapsedMs);

    // Phân xử lượt bấm chuông cướp quyền trả lời
    ArenaRoomState HandleBuzzer(ArenaRoomState currentState, string playerId, long clientTimestampMs);

    // Chuyển đổi giai đoạn/vòng thi đấu tiếp theo
    ArenaRoomState NextStage(ArenaRoomState currentState, string targetStage);
}
```

### Chi Tiết Cấu Trúc Dữ Liệu `ArenaRoomState`:
```csharp
public class ArenaRoomState
{
    public string RoomId { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    public string GameCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string HostUserId { get; set; } = string.Empty;
    public string CurrentStage { get; set; } = "LOBBY";
    public int CurrentQuestionIndex { get; set; } = 0;
    public List<ArenaQuestionItem> Questions { get; set; } = new();
    public ConcurrentDictionary<string, ArenaPlayer> Players { get; set; } = new();
    public string? BuzzerWinnerPlayerId { get; set; } = null;
    public long BuzzerTimestampMs { get; set; } = 0;
    public DateTime StageStartTimeUtc { get; set; } = DateTime.UtcNow;
    public int StageDurationSeconds { get; set; } = 60;
    public Dictionary<string, object> CustomData { get; set; } = new();
}
```

---

## 3. Cơ Chế Phân Xử Chuông Mili-Giây (Microsecond Buzzer Arbitration)

Để giải quyết triệt để bài toán **tranh chấp bấm chuông (Race Condition)** giữa nhiều thí sinh cùng lúc qua mạng Internet:

```csharp
public (bool Won, ArenaRoomState State) RingBuzzer(string roomId, string playerId, long clientReportedTimestampMs)
{
    var room = GetRoom(roomId);
    if (room == null) return (false, null!);

    lock (room)
    {
        // Nếu đã có người giành được chuông ở câu hỏi hiện tại thì từ chối các người sau
        if (!string.IsNullOrEmpty(room.BuzzerWinnerPlayerId))
        {
            return (false, room);
        }

        // Lấy plugin tương ứng xử lý
        if (_plugins.TryGetValue(room.GameCode, out var plugin))
        {
            var updated = plugin.HandleBuzzer(room, playerId, clientReportedTimestampMs);
            _rooms[roomId] = updated;
            return (true, updated);
        }

        room.BuzzerWinnerPlayerId = playerId;
        room.BuzzerTimestampMs = Stopwatch.GetTimestamp();
        return (true, room);
    }
}
```

- Sử dụng `lock (room)` phạm vi từng phòng đảm bảo an toàn đa luồng tuyệt đối (Thread-Safe).
- Thuật toán kết hợp giữa:
  1. `clientReportedTimestampMs`: Thời điểm thí sinh nhấn nút chuông tại Browser (đã được hiệu chỉnh qua thuật toán đồng bộ thời gian NTP-like).
  2. `Stopwatch.GetTimestamp()`: Đồng hồ xung nhịp phần cứng tại máy chủ để chống gian lận chỉnh giờ phía Client.

---

## 4. Giao Thức WebSocket Real-Time (`ArenaHub`)

SignalR Hub tọa lạc tại `/hubs/arena` đảm nhận vai trò cầu nối dữ liệu hai chiều:

### Client gọi Server:
| Tên Phương Thức (Method) | Tham Số | Chức Năng |
| :--- | :--- | :--- |
| `JoinRoom` | `string roomId, string playerName, string avatar` | Thí sinh tham gia phòng thi đấu. |
| `SubmitAnswer` | `string roomId, string answerRaw, long elapsedMs` | Nộp đáp án cho câu hỏi hiện tại. |
| `RingBuzzer` | `string roomId, long clientTimestampMs` | Phát lệnh bấm chuông cướp quyền trả lời. |
| `TriggerAction` | `string roomId, string actionType, string payload` | Thực hiện hành động đặc biệt của Gameshow. |
| `NextStage` | `string roomId, string nextStage` | Host điều khiển chuyển sang vòng thi tiếp theo. |

### Server phát sóng tới Client (Broadcast Events):
- `PlayerJoined(ArenaPlayer player)`: Có thí sinh mới vào phòng.
- `AnswerSubmitted(string playerId, bool isCorrect, int pointsAwarded)`: Cập nhật kết quả chấm điểm realtime.
- `BuzzerWon(string winnerPlayerId, string winnerName)`: Thông báo thí sinh chiến thắng lượt bấm chuông kèm âm thanh báo động.
- `ActionTriggered(string playerId, string actionType, string payload)`: Phát sóng hành động quay nón, cứu trợ...
- `StageChanged(string newStage, int durationSeconds)`: Chuyển giao diện vòng thi mới cho tất cả người xem.

---

## 5. Web Audio Synthesizer (Bộ Tạo Âm Thanh 0ms Latency)

Nhằm đảm bảo trải nghiệm Studio Gameshow sống động mà không gặp phải độ trễ tải file MP3 trên mạng, AegisQuiz tích hợp bộ tổng hợp âm thanh `Web Audio API` tích hợp sẵn trong trình duyệt (`Frontend/src/services/arena.service.ts`):

- **`playArenaSfx('buzzer')`**: Sóng vuông (Sawtooth Wave) 440Hz ➔ 220Hz mô phỏng tiếng còi báo động Gameshow truyền hình.
- **`playArenaSfx('wheel_tick')`**: Tiếng click nan nón cơ học siêu ngắn (Noise buffer 30ms).
- **`playArenaSfx('drop')`**: Âm thanh trượt dốc giảm tần số từ 350Hz ➔ 80Hz (Nhanh Như Chớp).
- **`playArenaSfx('victory')`**: Hợp âm Arpeggio trưởng (C5 - E5 - G5 - C6) phát sáng chiến thắng.

---

## 6. Hướng Dẫn Từng Bước (Tutorial): Tạo Một Gameshow Mới

Dưới đây là quy trình 5 bước chuẩn hóa để tích hợp Gameshow mới: **"Ai Là Triệu Phú" (Who Wants to Be a Millionaire)**.

### Bước 1: Cài Đặt Backend Plugin
Tạo file `AegisQuiz.Infrastructure/Arena/MillionairePlugin.cs`:

```csharp
using AegisQuiz.Application.Interfaces;

public class MillionairePlugin : IArenaGamePlugin
{
    public string GameCode => "MILLIONAIRE";

    public ArenaGameMetadata Metadata => new()
    {
        GameCode = "MILLIONAIRE",
        DisplayName = "Ai Là Triệu Phú (Hot Seat)",
        Description = "Ghế nóng 15 câu hỏi tiến tới 150 triệu đồng với 4 quyền trợ giúp.",
        Icon = "💰",
        Badge = "Hot Seat",
        MinPlayers = 1,
        MaxPlayers = 10,
        SupportedStages = new[] { "FASTEST_FINGER", "HOT_SEAT", "FINAL_RESULT" }
    };

    public ArenaRoomState InitializeRoomState(string roomId, string roomName, string hostUserId)
    {
        var state = new ArenaRoomState
        {
            RoomId = roomId,
            GameCode = GameCode,
            RoomName = roomName,
            HostUserId = hostUserId,
            CurrentStage = "HOT_SEAT"
        };
        // Cấu hình các mốc tiền thưởng trong CustomData
        state.CustomData["moneyLadder"] = new[] { 200, 400, 600, 1000, 2000, 3000, 6000, 10000, 14000, 22000, 30000, 40000, 60000, 85000, 150000 };
        return state;
    }

    public ArenaRoomState ProcessPlayerAction(ArenaRoomState state, string playerId, string actionType, string payload)
    {
        if (actionType == "USE_LIFELINE")
        {
            // payload: "50_50" | "CALL_PHONE" | "ASK_AUDIENCE" | "ASK_COMPANION"
            state.CustomData[$"lifeline_{payload}_used"] = true;
        }
        return state;
    }

    public int CalculateScore(ArenaRoomState state, string playerId, bool isCorrect, long elapsedMs)
    {
        if (!isCorrect) return 0;
        var ladder = (int[])state.CustomData["moneyLadder"];
        return ladder[Math.Min(state.CurrentQuestionIndex, ladder.Length - 1)];
    }

    public ArenaRoomState HandleBuzzer(ArenaRoomState state, string playerId, long clientTimestampMs)
    {
        state.BuzzerWinnerPlayerId = playerId;
        return state;
    }

    public ArenaRoomState NextStage(ArenaRoomState state, string targetStage)
    {
        state.CurrentStage = targetStage;
        return state;
    }
}
```

### Bước 2: Đăng Ký Dependency Injection
Trong `AegisQuiz.API/Program.cs`:
```csharp
builder.Services.AddSingleton<IArenaGamePlugin, MillionairePlugin>();
```

### Bước 3: Định Nghĩa Type Bên Frontend
Trong `Frontend/src/types/arena.ts`:
```typescript
export type ArenaGameCode = 
  | 'OLYMPIA' 
  | 'GOLDEN_BELL' 
  | 'LUCKY_WHEEL' 
  | 'UNIVERSITY_CHALLENGE' 
  | 'JEOPARDY' 
  | 'LIGHTNING'
  | 'MILLIONAIRE'; // Thêm mã định danh mới
```

### Bước 4: Xây Dựng Component Giao Diện
Tạo `Frontend/src/pages/arena/components/MillionaireArenaView.tsx`:
- Hiển thị Tháp 15 mốc tiền thưởng (Mốc quan trọng số 5 và số 10 được viền vàng).
- Hiển thị 4 nút bấm trợ giúp: **50:50**, **Gọi điện thoại cho người thân**, **Hỏi ý kiến khán giả trường quay**, **Hỏi tổ tư vấn tại chỗ**.
- Hiển thị nút "Dừng cuộc chơi để bảo toàn số tiền".

### Bước 5: Ánh Xạ Vào Đấu Trường
Trong `Frontend/src/pages/arena/ArenaRoomPage.tsx`:
```tsx
{room.gameCode === 'MILLIONAIRE' && (
  <MillionaireArenaView
    state={room}
    onSubmitAnswer={handleSubmitAnswer}
    onAction={handleAction}
    onNextStage={handleNextStage}
  />
)}
```

---

## 7. Khuyến Nghị Về Kiểm Thử & CI/CD

- **Unit Test**: Luôn bổ sung kiểm thử trong `AegisQuiz.UnitTests` để bảo đảm thuật toán cộng trừ điểm, phân xử cướp chuông và chuyển vòng thi thỏa mãn 100% assertions.
- **Frontend Build**: Chạy `npm run build` kiểm tra tính tương thích TypeScript nghiêm ngặt trước khi tạo Pull Request.
- **Độ ổn định kết nối**: Khuyến nghị cấu hình WebSockets transport ưu tiên cao nhất, tự động fallback sang Long Polling nếu Client ở mạng hạn chế proxy/firewall.
