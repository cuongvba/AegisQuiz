# AegisQuiz Backend (C# .NET 10 Clean Architecture)

Hệ thống được thiết kế theo chuẩn Clean Architecture kết hợp CQRS (MediatR) và tích hợp Gemini AI.

## Cấu trúc thư mục:
- `/src/AegisQuiz.Domain`: Chứa các Entities cốt lõi (QuestionBase, UserQuota). Không phụ thuộc vào bất kỳ thư viện nào.
- `/src/AegisQuiz.Application`: Chứa logic nghiệp vụ (Use Cases, CQRS Handlers).
- `/src/AegisQuiz.Infrastructure`: Chứa kết nối Database (PostgreSQL/Spanner), Redis Cache, và Gemini AI Services.
- `/src/AegisQuiz.API`: Chứa các Minimal APIs Endpoint (Cổng giao tiếp).
