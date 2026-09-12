# 🛡️ An Ninh Hệ Thống & Cơ Chế Giám Sát Chống Gian Lận (Security & Proctoring)

Tài liệu này mô tả các biện pháp bảo vệ tính toàn vẹn của kỳ thi (Exam Integrity), kiến trúc phòng thi bảo mật **`SecureExamRoom`** và cơ chế xác thực kép trong hệ sinh thái **AegisQuiz**.

---

## 1. Các Mối Đe Dọa Khảo Thí Trực Tuyến & Biện Pháp Phòng Vệ

| Mối Đe Dọa Gian Lận | Hành Vi Của Thí Sinh | Biện Pháp Phòng Vệ Kỹ Thuật (Aegis Shield) |
| :--- | :--- | :--- |
| **Tra cứu tài liệu ngoài** | Mở tab trình duyệt mới, dùng Google Search hoặc ChatGPT. | **Fullscreen Enforcement & Tab-Switch Detection**: Cưỡng bức toàn màn hình. Khi mất focus cửa sổ (`visibilitychange` / `blur`), phát cảnh báo đỏ và ghi nhận lỗi vi phạm. |
| **Sao chép đề bài** | Bôi đen copy câu hỏi gửi cho người khác, mở DevTools. | **Clipboard Disabling & DevTools Trap**: Khóa chuột phải (`contextmenu`), chặn phím tắt `Ctrl+C`, `Ctrl+U`, `F12`, xóa sạch clipboard. |
| **Sử dụng màn hình phụ** | Cắm thêm cổng HDMI/DisplayPort để hiển thị tài liệu. | **Screen Capture Detection**: Quét số lượng màn hình thông qua Web Screen Details API. |
| **Thi hộ / Mạo danh** | Người khác ngồi vào vị trí làm bài thi. | **WebRTC Webcam Monitoring & PKI Auth**: Giám sát khuôn mặt định kỳ qua camera và xác thực khóa công khai định danh. |
| **Sửa đổi điểm client** | Sửa JavaScript hoặc can thiệp gói tin nộp bài. | **Server-Side Verification**: Mọi phép tính điểm, chấm đáp án, thời gian đếm ngược đều được phân xử và kiểm định duy nhất tại máy chủ .NET 10. |

---

## 2. Kiến Trúc Phòng Thi Bảo Mật (`SecureExamRoom`)

Khi một kỳ thi được gắn cờ `isSecureMode = true`, thí sinh bắt buộc phải vào phòng thi bảo mật cao cấp:

```
[Bắt đầu bài thi]
       │
       ▼
[Kiểm tra Camera & Microphone] ──(Không cấp quyền)──► [Từ chối vào phòng]
       │ (Hợp lệ)
       ▼
[Cưỡng bức Toàn Màn Hình Fullscreen] ──(Từ chối)──► [Tự động dừng bài thi]
       │ (Hợp lệ)
       ▼
[Kích hoạt Cảm biến Giám sát Tab & Bàn phím]
       │
       ├─► [Sự kiện rời tab > 3 lần] ──► [Khóa bài thi & Nộp bài cưỡng bức]
       ├─► [Nhấn phím F12 / Chuột phải] ──► [Hủy hành động & Ghi log vi phạm]
       └─► [Nộp bài thi an toàn] ──► [Mã hóa gói tin & Ký số kết quả]
```

### 2.1. Cảm Biến Giám Sát Cửa Sổ Trình Duyệt:
```typescript
useEffect(() => {
  const handleVisibilityChange = () => {
    if (document.hidden) {
      recordViolation('TAB_SWITCH', 'Thí sinh đã chuyển sang ứng dụng hoặc tab khác');
    }
  };

  const handleFullscreenChange = () => {
    if (!document.fullscreenElement) {
      recordViolation('EXIT_FULLSCREEN', 'Thí sinh đã thoát chế độ toàn màn hình');
    }
  };

  document.addEventListener('visibilitychange', handleVisibilityChange);
  document.addEventListener('fullscreenchange', handleFullscreenChange);

  return () => {
    document.removeEventListener('visibilitychange', handleVisibilityChange);
    document.removeEventListener('fullscreenchange', handleFullscreenChange);
  };
}, []);
```

---

## 3. Xác Thực & Phân Quyền (Authentication & Authorization)

Hệ thống sử dụng cơ chế bảo mật nhiều tầng:

### 3.1. Xác Thực Chuẩn JWT & Keycloak OIDC
- Mọi yêu cầu gọi đến API đều yêu cầu Header: `Authorization: Bearer <JWT_TOKEN>`.
- Token chứa các Claim tiêu chuẩn: `sub` (User ID), `role` (`Student`, `Teacher`, `Admin`), `ou` (Đơn vị trường/khoa), `exp` (Thời gian hết hạn).
- Tích hợp giao thức OpenID Connect (OIDC) với máy chủ xác thực Keycloak cho các khách hàng doanh nghiệp và trường đại học.

### 3.2. Xác Thực Khóa Công Khai PKI (`PkiAuthController`)
Dành cho các kỳ thi cấp chứng chỉ quốc tế và khảo thí yêu cầu tính pháp lý cao:
- Mỗi thí sinh sở hữu một cặp khóa bất đối xứng (Public Key / Private Key).
- Trước khi nộp bài, gói tin kết quả thi được ký số (Digital Signature) bằng Private Key của thí sinh trên thiết bị đầu cuối.
- Server giải mã chữ ký bằng Public Key đã đăng ký trước đó để khẳng định chắc chắn 100% bài làm xuất phát từ chính thí sinh đó và không hề bị sửa đổi trên đường truyền mạng.
