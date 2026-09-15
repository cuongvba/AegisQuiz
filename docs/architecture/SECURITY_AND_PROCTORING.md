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

### 3.3. Đăng Nhập 1-Giây Bằng Quét Mã QR (Scan-to-Auth Zero-Trust)
Dành cho trải nghiệm người dùng tối ưu trên thiết bị cá nhân:
- **Cơ chế Single-Use Invalidation**: Máy chủ sinh vé ngắn hạn 120s dạng GUID ngẫu nhiên; client hiển thị mã QR SVG kèm đồng hồ đếm ngược.
- **Ủy quyền từ xa**: Thí sinh quét mã bằng camera điện thoại và bấm xác nhận $\rightarrow$ Máy chủ tự động cấp phiên đăng nhập hợp lệ cho máy tính và hủy vé ngay lập tức khỏi RAM để chống tấn công phát lại (Replay Attack).
- **Thử nghiệm 1-Click (Demo Scan)**: Hỗ trợ kiểm thử trực tiếp trên màn hình desktop cho quản trị viên và người kiểm định.

### 3.4. Xác Thực Hai Yếu Tố (2FA TOTP RFC 6238)
- Bảo vệ tài khoản quản trị và thí sinh bằng mật khẩu dùng một lần theo thời gian (Time-Based One-Time Password - RFC 6238).
- Tích hợp chuẩn Google Authenticator / Microsoft Authenticator với thuật toán Base32 và cơ chế bù trừ lệch giờ (Clock Skew Tolerance) $\pm 30$ giây.
- Ngăn chặn hoàn toàn các vụ xâm nhập do lộ mật khẩu tài khoản hoặc tấn công rà quét từ điển.
