# 🧩 Phân Hệ Đấu Trường Gameshow (Arena Plugin Engine)

Tài liệu này cung cấp cái nhìn toàn cảnh về phân hệ **Đấu Trường Trí Tuệ (AegisQuiz Arena)**, cơ chế cắm rút Plugin, phân luồng SignalR và bộ tổng hợp âm thanh Web Audio SFX.

---

## 1. Giới Thiệu Phân Hệ

Phân hệ Đấu trường được thiết kế nhằm mô phỏng và số hóa các gameshow truyền hình trí tuệ đỉnh cao cho học sinh, sinh viên và doanh nghiệp. Hệ thống giải quyết 3 thách thức lớn nhất của các ứng dụng gameshow online:
1. **Khả năng mở rộng không giới hạn (Extensibility)**: Không cần sửa đổi kiến trúc lõi khi thêm một gameshow mới (sử dụng `IArenaGamePlugin`).
2. **Độ trễ cướp chuông mili-giây (Microsecond Buzzer Latency)**: Xử lý tranh chấp bằng bộ nhớ RAM đa luồng `ConcurrentDictionary` kết hợp đồng hồ xung nhịp phần cứng `Stopwatch.GetTimestamp()`.
3. **Trải nghiệm nghe nhìn Studio sống động**: Tích hợp công nghệ Canvas 60fps mô phỏng nón quay vật lý và bộ tổng hợp âm thanh Web Audio API không có độ trễ tải mạng.

---

## 2. Danh Sách 6 Gameshow Đã Tích Hợp Sẵn

```
[Sảnh Đấu Trường: ArenaHubPage]
  ├── 🏛️ Olympia Supreme (4 Vòng, 4 Bục 3D, VCNV 80đ, Ngôi Sao Hy Vọng x2)
  ├── 🔔 Rung Chuông Vàng (Mega-Grid 100 Ghế Ma Trận, Bảng Mica, Thầy Cô Cứu Trợ)
  ├── 🎡 Chiếc Nón Kỳ Diệu (Vòng Quay Canvas 60fps Quán Tính, Lật Ô Chữ 3D)
  ├── 🎓 University Challenge (Đối Kháng 4v4 Liên Trường, Starter 10đ, Bonus 15đ)
  ├── 🇺🇸 Jeopardy! American Matrix (30 Ô Ma Trận Điểm, Quy Tắc Câu Hỏi Ngược)
  └── ⚡ Nhanh Như Chớp (Thang Leo Dốc 10 Bậc, Đúng Lên 1, Sai Tụt Về 0)
```

Chi tiết mã nguồn của các plugin được đặt tại:
- Backend: [`ArenaGamePlugins.cs`](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Arena/ArenaGamePlugins.cs)
- Frontend: [`Frontend/src/pages/arena/components/`](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Frontend/src/pages/arena/components/)

---

## 3. Kiến Trúc Bộ Điều Phối Phòng (`ArenaRoomManager`)

`ArenaRoomManager` là Singleton Service duy trì trạng thái của tất cả các phòng thi đấu đang diễn ra:

```csharp
public class ArenaRoomManager
{
    private readonly ConcurrentDictionary<string, ArenaRoomState> _rooms = new();
    private readonly Dictionary<string, IArenaGamePlugin> _plugins = new();

    public ArenaRoomManager(IEnumerable<IArenaGamePlugin> plugins)
    {
        foreach (var p in plugins)
        {
            _plugins[p.GameCode] = p;
        }
    }

    public ArenaRoomState CreateRoom(string gameCode, string roomName, string hostUserId)
    {
        if (!_plugins.TryGetValue(gameCode, out var plugin))
            throw new ArgumentException($"Game plugin '{gameCode}' không tồn tại.");

        var roomId = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var state = plugin.InitializeRoomState(roomId, roomName, hostUserId);
        _rooms[roomId] = state;
        return state;
    }
}
```

---

## 4. Hướng Dẫn Phát Triển Chi Tiết

Để tìm hiểu chi tiết các bước tạo mới một gameshow từ A đến Z, xem hướng dẫn kỹ thuật viên đầy đủ tại:
👉 **[Tài Liệu Kỹ Thuật Viên: Arena Plugin Developer Guide](../ARENA_PLUGIN_DEVELOPER_GUIDE.md)**
