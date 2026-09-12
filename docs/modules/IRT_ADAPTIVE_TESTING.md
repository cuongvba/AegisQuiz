# 🧠 Khảo Thí Thích Ứng Máy Tính & Mô Hình IRT (Item Response Theory)

Tài liệu này mô tả cơ sở toán học xác suất và kiến trúc dịch vụ vi mô Python (**IRT/CAT Service**) phục vụ bài toán thi thích ứng thích nghi theo trình độ thí sinh trong **AegisQuiz**.

---

## 1. Khảo Thí Thích Ứng Máy Tính (CAT - Computerized Adaptive Testing) Là Gì?

Trong các kỳ thi truyền thống:
- Mọi thí sinh đều làm cùng một đề thi cố định (ví dụ 50 câu).
- Thí sinh giỏi cảm thấy đề quá dễ (mất thời gian làm các câu cơ bản).
- Thí sinh yếu cảm thấy đề quá khó (chán nản khi liên tục gặp câu hóc búa).

**Khảo Thí Thích Ứng (CAT)** giải quyết vấn đề này bằng cách:
- Hệ thống chọn câu hỏi tiếp theo dựa trên **kết quả làm bài của các câu trước đó**.
- Nếu thí sinh trả lời **đúng**, câu tiếp theo sẽ **khó hơn**.
- Nếu thí sinh trả lời **sai**, câu tiếp theo sẽ **dễ hơn**.
- Sau khoảng 15 đến 20 câu, thuật toán hội tụ và xác định chính xác năng lực thực sự của thí sinh với sai số chuẩn (Standard Error) cực nhỏ.

---

## 2. Cơ Sở Toán Học: Mô Hình 3 Tham Số Logistic (3PL IRT Model)

Mô hình tính xác suất một thí sinh có năng lực $\theta$ trả lời đúng câu hỏi thứ $i$:

$$P_i(\theta) = c_i + \frac{1 - c_i}{1 + e^{-1.7 a_i (\theta - b_i)}}$$

Trong đó:
- $\theta \in [-4.0, +4.0]$: Năng lực tiềm ẩn của thí sinh (Theta). $\theta = 0$ là mức trung bình chuẩn, $\theta > 0$ là khá/giỏi, $\theta < 0$ là yếu.
- $b_i \in [-3.0, +3.0]$: **Độ khó (Item Difficulty)** của câu hỏi.
- $a_i \in [0.5, 2.5]$: **Độ phân biệt (Item Discrimination)** — độ dốc của đường cong đặc trưng, câu hỏi phân biệt tốt sẽ tách rõ người giỏi và người yếu.
- $c_i \in [0.0, 0.35]$: **Hệ số đoán mò (Pseudo-Guessing)** — xác suất một người hoàn toàn không biết gì vẫn chọn đúng (trắc nghiệm 4 đáp án thì $c \approx 0.25$).

---

## 3. Thuật Toán Ước Lượng Năng Lực $\theta$

Dịch vụ Python `irt-service/main.py` triển khai hai phương pháp ước lượng chuẩn quốc tế:
1. **Maximum Likelihood Estimation (MLE)**: Tìm giá trị $\hat{\theta}$ tối đa hóa hàm hợp lý của chuỗi câu trả lời.
2. **Expected A Posteriori (EAP - Bayes)**: Sử dụng phân phối tiên nghiệm chuẩn $N(0, 1)$ giúp hội tụ ngay từ những câu đầu tiên khi chưa có đủ dữ liệu để tính MLE.

### Hàm Thông Tin Câu Hỏi (Fisher Information Function):
$$I_i(\theta) = \frac{(P'_i(\theta))^2}{P_i(\theta)(1 - P_i(\theta))}$$

Để chọn câu hỏi tối ưu kế tiếp, hệ thống tìm trong ngân hàng câu hỏi chưa làm câu nào có giá trị $I_i(\hat{\theta})$ lớn nhất.

---

## 4. Giao Tiếp Microservice Với Backend .NET

Khi thí sinh đang làm bài luyện thi phân hóa tại màn hình `/practice`:
1. .NET Backend gửi POST request tới `http://irt-service:8001/api/v1/cat/next-item`:
   ```json
   {
     "candidate_history": [
       { "item_id": "q1", "is_correct": true, "a": 1.2, "b": -0.5, "c": 0.25 },
       { "item_id": "q2", "is_correct": true, "a": 1.5, "b": 0.8, "c": 0.25 }
     ],
     "candidate_current_theta": 0.65
   }
   ```
2. Python IRT Service tính toán lại $\hat{\theta}$ mới và trả về ID câu hỏi tiếp theo phù hợp nhất với trình độ hiện tại của thí sinh.
