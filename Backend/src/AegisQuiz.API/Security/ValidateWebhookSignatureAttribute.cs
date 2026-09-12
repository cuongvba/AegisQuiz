using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AegisQuiz.API.Security
{
    // [Lỗ hổng #5 đã vá] Bảo mật Webhook bằng HMAC-SHA256
    // Attribute này sẽ tự động kiểm tra chữ ký trước khi cho phép request vào Controller
    [AttributeUsage(AttributeTargets.Method)]
    public class ValidateWebhookSignatureAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;
            
            // Đọc Secret Key từ cấu hình (appsettings.json)
            var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            string secretKey = config["Webhook:SecretKey"] ?? "";
            
            // Đọc chữ ký từ Header
            string? receivedSignature = request.Headers["X-Webhook-Signature"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(receivedSignature))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Thiếu chữ ký Webhook (X-Webhook-Signature header)." });
                return;
            }

            // Đọc body của request
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            string bodyContent = await reader.ReadToEndAsync();
            request.Body.Position = 0; // Reset stream để Controller vẫn đọc được

            // Tính toán chữ ký HMAC-SHA256 từ body + secret key
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(bodyContent));
            string expectedSignature = Convert.ToHexString(hashBytes).ToLower();

            // So sánh: Nếu không khớp = Hacker giả mạo!
            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature), 
                Encoding.UTF8.GetBytes(receivedSignature)))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Chữ ký Webhook không hợp lệ. Từ chối truy cập." });
                return;
            }

            // Hợp lệ -> Cho phép đi tiếp vào Controller
            await next();
        }
    }
}
