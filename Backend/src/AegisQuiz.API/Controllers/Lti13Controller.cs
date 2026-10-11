using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AegisQuiz.Application.Interfaces;

namespace AegisQuiz.API.Controllers
{
    /// <summary>
    /// [AegisUniversal LTI 1.3 Advantage Controller]
    /// Điểm tiếp nhận tích hợp 1-Click Launch từ các LMS hàng đầu thế giới (Canvas, Moodle, Blackboard).
    /// </summary>
    [ApiController]
    [Route("api/v1/lti")]
    public class Lti13Controller : ControllerBase
    {
        private readonly ILti13Service _ltiService;

        public Lti13Controller(ILti13Service ltiService)
        {
            _ltiService = ltiService;
        }

        /// <summary>
        /// Bước 1: LMS khởi phát OIDC Login Initiation
        /// </summary>
        [AllowAnonymous]
        [HttpGet("login_initiations")]
        [HttpPost("login_initiations")]
        public async Task<IActionResult> LoginInitiations(
            [FromQuery] string? iss,
            [FromQuery] string? login_hint,
            [FromQuery] string? target_link_uri,
            [FromQuery] string? lti_message_hint,
            [FromQuery] string? client_id,
            CancellationToken ct = default)
        {
            var request = new LtiOidcLoginRequestDto
            {
                Iss = iss ?? Request.Form["iss"].ToString(),
                LoginHint = login_hint ?? Request.Form["login_hint"].ToString(),
                TargetLinkUri = target_link_uri ?? Request.Form["target_link_uri"].ToString(),
                LtiMessageHint = lti_message_hint ?? Request.Form["lti_message_hint"].ToString(),
                ClientId = client_id ?? Request.Form["client_id"].ToString()
            };

            var result = await _ltiService.InitiateOidcLoginAsync(request, ct);
            return Redirect(result.RedirectUrl);
        }

        /// <summary>
        /// Bước 2: LMS Redirect về kèm IdToken (LTI 1.3 Resource Link Launch)
        /// </summary>
        [AllowAnonymous]
        [HttpPost("launch")]
        public async Task<IActionResult> Launch(
            [FromForm] string id_token,
            [FromForm] string state,
            CancellationToken ct = default)
        {
            var payload = new LtiLaunchPayloadDto
            {
                IdToken = id_token,
                State = state
            };

            var launchResult = await _ltiService.ValidateLtiLaunchAsync(payload, ct);
            if (!launchResult.IsValid)
            {
                return BadRequest(new { error = launchResult.ErrorMessage });
            }

            // Điều hướng Thí sinh vào giao diện làm bài thi tương ứng
            return Redirect(launchResult.RedirectAppUrl);
        }

        /// <summary>
        /// Bước 3: Cung cấp JWKS Endpoint công khai để LMS kiểm tra chữ ký AegisQuiz
        /// </summary>
        [AllowAnonymous]
        [HttpGet(".well-known/jwks.json")]
        [HttpGet("jwks")]
        public async Task<IActionResult> GetJwks(CancellationToken ct = default)
        {
            var jwks = await _ltiService.GetPublicJwksAsync(ct);
            return Ok(jwks);
        }
    }
}
