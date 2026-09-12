# ⚡ Đặc Tả Giao Thức SignalR WebSockets (Real-time Protocols)

Tài liệu này mô tả chi tiết giao thức truyền tin thời gian thực hai chiều giữa Client và Server thông qua **ASP.NET Core SignalR**.

---

## 1. Kết Nối & Khởi Tạo (Connection Setup)

- **Endpoint Hub**: `wss://<domain>/hubs/arena` (hoặc `http://localhost:5000/hubs/arena`)
- **Transport**: WebSockets (ưu tiên số 1), Server-Sent Events (fallback), Long Polling (dự phòng).
- **Khởi tạo kết nối phía Frontend (TypeScript)**:
  ```typescript
  import * as signalR from '@microsoft/signalr';

  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/arena', {
      transport: signalR.HttpTransportType.WebSockets,
      skipNegotiation: true
    })
    .withAutomaticReconnect([0, 1000, 5000, 10000])
    .build();

  await connection.start();
  ```

---

## 2. Các Lệnh Client Gửi Tới Server (Client-to-Server Invocations)

| Tên Phương Thức | Tham Số | Mô Tả Nghiệp Vụ |
| :--- | :--- | :--- |
| `JoinRoom` | `string roomId, string playerName, string avatar` | Thí sinh tham gia vào phòng thi đấu. Server sẽ đưa connection vào Group SignalR tương ứng. |
| `SubmitAnswer` | `string roomId, string answerRaw, long elapsedMs` | Gửi đáp án cho câu hỏi hiện tại kèm thời gian phản xạ tính bằng mili-giây. |
| `RingBuzzer` | `string roomId, long clientTimestampMs` | Bắn lệnh bấm chuông cướp quyền trả lời (kèm timestamp mili-giây của trình duyệt). |
| `TriggerAction` | `string roomId, string actionType, string payload` | Kích hoạt hành động đặc thù (VD: `SPIN_WHEEL`, `GUESS_CNV`, `TEACHER_RESCUE`). |
| `NextStage` | `string roomId, string nextStage` | Host điều khiển chuyển sang vòng thi đấu tiếp theo (VD: `VUOT_CNV`, `TANG_TOC`). |

---

## 3. Các Sự Kiện Server Phát Sóng (Server-to-Client Broadcasts)

Tất cả các thành viên trong cùng một phòng đấu trường sẽ lắng nghe các sự kiện sau:

### 3.1. `PlayerJoined`
- **Thời điểm**: Khi có một thí sinh mới vào phòng.
- **Payload**: Đối tượng `ArenaPlayer` (Id, Tên, Avatar, Vị trí ghế).

### 3.2. `BuzzerWon`
- **Thời điểm**: Khi hệ thống phân xử thành công thí sinh bấm chuông nhanh nhất.
- **Payload**:
  ```json
  {
    "winnerPlayerId": "user-abc-123",
    "winnerName": "Kiện Tướng Toán Học",
    "timestampMs": 1788482062
  }
  ```
- **Hành vi Client**: Phát âm thanh `buzzer` bằng Web Audio API, làm sáng rực viền bục của thí sinh chiến thắng.

### 3.3. `AnswerSubmitted`
- **Thời điểm**: Khi một thí sinh nộp câu trả lời và hệ thống hoàn tất chấm điểm.
- **Payload**:
  ```json
  {
    "playerId": "user-abc-123",
    "isCorrect": true,
    "pointsAwarded": 40,
    "currentTotalScore": 180,
    "streak": 3
  }
  ```

### 3.4. `StageChanged`
- **Thời điểm**: Khi Host chuyển sang vòng thi mới.
- **Payload**: `{ "newStage": "VUOT_CNV", "stageDurationSeconds": 180 }`.
- **Hành vi Client**: Tự động chuyển đổi giao diện sang màn hình của vòng thi tương ứng mà người dùng không cần tải lại trang.
