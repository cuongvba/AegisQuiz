using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [AegisUniversal IMS Global LTI 1.3 Advantage Service]
    /// Cung cấp tiêu chuẩn tích hợp giáo dục toàn cầu LTI 1.3 (Learning Tools Interoperability):
    /// - LTI Core 1.3: Khởi động OIDC 3rd-party Launch từ Canvas LMS, Blackboard, Moodle.
    /// - Assignment & Grade Services (AGS): Tự động trả kết quả điểm thi về Sổ điểm LMS.
    /// - Deep Linking: Cho phép giáo viên chọn đề thi trực tiếp trong giao diện LMS.
    /// </summary>
    public interface ILti13Service
    {
        /// <summary>
        /// Khởi tạo bước 1 OIDC Authorization Request tới LMS.
        /// </summary>
        Task<LtiOidcLoginResponseDto> InitiateOidcLoginAsync(LtiOidcLoginRequestDto request, CancellationToken ct = default);

        /// <summary>
        /// Xác thực LTI 1.3 Resource Link Launch JWT token từ LMS gửi sang.
        /// </summary>
        Task<LtiLaunchResultDto> ValidateLtiLaunchAsync(LtiLaunchPayloadDto payload, CancellationToken ct = default);

        /// <summary>
        /// Trả điểm thi tự động về LMS thông qua AGS endpoint (Assignment and Grade Services).
        /// </summary>
        Task<bool> SubmitGradeToLmsAsync(LtiGradeSubmissionDto dto, CancellationToken ct = default);

        /// <summary>
        /// Xuất bản khóa công khai định dạng JWKS (JSON Web Key Set) cho các LMS xác minh chữ ký của AegisQuiz.
        /// </summary>
        Task<object> GetPublicJwksAsync(CancellationToken ct = default);
    }

    public class LtiOidcLoginRequestDto
    {
        public string Iss { get; set; } = string.Empty; // Issuer URL (ví dụ https://canvas.instructure.com)
        public string LoginHint { get; set; } = string.Empty;
        public string TargetLinkUri { get; set; } = string.Empty;
        public string? LtiMessageHint { get; set; }
        public string? ClientId { get; set; }
    }

    public class LtiOidcLoginResponseDto
    {
        public string RedirectUrl { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Nonce { get; set; } = string.Empty;
    }

    public class LtiLaunchPayloadDto
    {
        public string IdToken { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }

    public class LtiLaunchResultDto
    {
        public bool IsValid { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public string CourseId { get; set; } = string.Empty;
        public string ResourceLinkId { get; set; } = string.Empty;
        public string? CustomExamId { get; set; }
        public string? AgsLineItemUrl { get; set; }
        public string RedirectAppUrl { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class LtiGradeSubmissionDto
    {
        public string AgsLineItemUrl { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public double ScoreGiven { get; set; }
        public double ScoreMaximum { get; set; } = 100.0;
        public string Comment { get; set; } = string.Empty;
        public string ActivityProgress { get; set; } = "Completed";
        public string GradingProgress { get; set; } = "FullyGraded";
    }
}
