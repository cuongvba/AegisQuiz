# 🎓 Cẩm Nang Người Học (Learner User Manual)

Chào mừng các bạn học sinh và sinh viên đến với nền tảng khảo thí thông minh **AegisQuiz**! Tài liệu này sẽ hướng dẫn bạn tận dụng tối đa các tính năng của nền tảng để bứt phá kết quả học tập.

---

## 1. Bảng Điều Khiển Học Tập (Dashboard)

Khi đăng nhập vào hệ thống tại trang chủ (`/`), bạn sẽ thấy:
- **Chuỗi ngày học liên tục (Streaks)**: Đếm số ngày bạn vào luyện tập không gián đoạn để nhận huy hiệu chăm chỉ.
- **Biểu đồ năng lực $\theta$ (Mastery Level)**: Đánh giá trình độ hiện tại theo từng chuyên đề kiến thức (Đại số, Hình học, Tiếng Anh, Vật Lý...).
- **Đề thi gợi ý theo AI**: Hệ thống tự động đề xuất bài kiểm tra phù hợp với điểm yếu cần cải thiện nhất của bạn.

---

## 2. Các Chế Độ Luyện Thi

### 2.1. Luyện Tập Thích Ứng (Adaptive Practice - `/practice`)
- Hệ thống sẽ đưa ra các câu hỏi thích nghi theo trình độ của bạn:
  - Trả lời đúng: Câu hỏi tiếp theo sẽ thử thách hơn.
  - Trả lời sai: Hệ thống giảm độ khó và hiển thị lời giải chi tiết của trợ lý AI để bạn nắm vững kiến thức nền tảng.
- Bạn có thể làm bài theo từng chủ đề hoặc kết hợp đa dạng nhiều môn học.

### 2.2. Thi Thử Chuẩn Hóa (Mock Exam - `/quiz/attempt/:id`)
- Giả lập phòng thi thực tế với đồng hồ đếm ngược thời gian nghiêm ngặt.
- Bảng câu hỏi bên tay phải giúp bạn đánh dấu các câu chưa chắc chắn để quay lại kiểm tra trước khi nộp bài.

### 2.3. Phòng Thi Bảo Mật Cao Cấp (Secure Exam Room)
- Dành cho các kỳ thi học kỳ, thi thử tốt nghiệp hoặc thi xếp lớp:
  - Bắt buộc kích hoạt chế độ **Toàn màn hình (Fullscreen)**.
  - Không được mở tab mới hoặc chuyển đổi ứng dụng. Nếu vi phạm quá số lần quy định, bài thi sẽ bị tự động nộp và lưu bằng chứng vi phạm.

### 2.4. Phân Hệ Ôn Thi Giấy Phép Lái Xe Quốc Gia (GPLX A1 - C)
- **Chuẩn cấu trúc thi sát hạch thực tế**:
  - 🏍️ **Hạng A1**: 25 câu làm trong 19 phút.
  - 🚗 **Hạng B1**: 30 câu làm trong 20 phút.
  - 🚙 **Hạng B2**: 35 câu làm trong 22 phút.
  - 🚛 **Hạng C**: 40 câu làm trong 24 phút.
- **Chế độ Luyện 60 Câu Điểm Liệt Tử Thần (`gplx_fatal_only`)**:
  - Tập hợp toàn bộ 60 câu hỏi tình huống mất an toàn giao thông nghiêm trọng (vượt đèn đỏ, qua đường sắt, nồng độ cồn...).
  - Giúp học viên rèn luyện phản xạ không bao giờ mắc phải lỗi "sai 1 câu liệt là trượt toàn bài".
- **Học Sa Hình & Biển Báo Thông Minh**:
  - AI phân tích thế xe nhường đường theo 4 quy tắc vàng: *Nhất chớm ➔ Nhì ưu (Hỏa - Sự - Công - Thương) ➔ Tam đường ➔ Tứ hướng*.
  - Chi tiết kỹ thuật và cấu trúc đề thi xem tại: [NATIONAL_DRIVING_LICENSE_MODULE.md](../modules/NATIONAL_DRIVING_LICENSE_MODULE.md).

---

## 3. Hệ Thống Gamification: Danh Hiệu & Bảng Xếp Hạng

- **Bảng Vinh Danh (`/leaderboard`)**: Xếp hạng những học sinh có điểm rèn luyện cao nhất tuần và tháng.
- **Kho Danh Hiệu & Huy Hiệu (`/achievements`)**: Mở khóa các danh hiệu độc quyền như:
  - ⚡ *Tia Chớp Phản Xạ*: Trả lời đúng 5 câu hỏi dưới 5 giây mỗi câu.
  - 🏹 *Bách Phát Bách Trúng*: Đạt điểm tối đa 100/100 trong bài thi khó.
  - 🏛️ *Nhà Leo Núi*: Giành chiến thắng trong Đấu trường Đường Lên Đỉnh Olympia.
