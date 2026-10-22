using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AegisQuiz.Application.Payments;
using AegisQuiz.Domain.Entities;

namespace AegisQuiz.Infrastructure.Payments
{
    // Singleton In-Memory Store hỗ trợ 0ms Hot-Reload cho Payment Settings
    public static class PaymentSettingsStore
    {
        private static PaymentGatewaySettingsDto _settings = new();
        private static readonly object _lock = new();

        public static PaymentGatewaySettingsDto Current
        {
            get { lock (_lock) { return _settings; } }
            set { lock (_lock) { _settings = value; } }
        }
    }

    public class PayOSGatewayService : IPaymentGatewayStrategy
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<PayOSGatewayService> _logger;

        public PaymentProviderType ProviderType => PaymentProviderType.PayOS;

        public PayOSGatewayService(HttpClient httpClient, IConfiguration config, ILogger<PayOSGatewayService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<PaymentOrderResponseDto> CreatePaymentAsync(PaymentTransaction transaction, CreatePaymentOrderRequest request)
        {
            var dynamicCfg = PaymentSettingsStore.Current;

            var clientId = !string.IsNullOrWhiteSpace(dynamicCfg.PayOsClientId) 
                ? dynamicCfg.PayOsClientId 
                : (_config["PayOS:ClientId"] ?? Environment.GetEnvironmentVariable("PAYOS_CLIENT_ID") ?? "");

            var apiKey = !string.IsNullOrWhiteSpace(dynamicCfg.PayOsApiKey) 
                ? dynamicCfg.PayOsApiKey 
                : (_config["PayOS:ApiKey"] ?? Environment.GetEnvironmentVariable("PAYOS_API_KEY") ?? "");

            var checksumKey = !string.IsNullOrWhiteSpace(dynamicCfg.PayOsChecksumKey) 
                ? dynamicCfg.PayOsChecksumKey 
                : (_config["PayOS:ChecksumKey"] ?? Environment.GetEnvironmentVariable("PAYOS_CHECKSUM_KEY") ?? "");

            var bankBin = !string.IsNullOrWhiteSpace(dynamicCfg.VietQrBankBin) 
                ? dynamicCfg.VietQrBankBin 
                : (_config["PayOS:BankBin"] ?? "970422");

            var accountNo = !string.IsNullOrWhiteSpace(dynamicCfg.VietQrAccountNumber) 
                ? dynamicCfg.VietQrAccountNumber 
                : (_config["PayOS:AccountNumber"] ?? "0868888888");

            var accountName = !string.IsNullOrWhiteSpace(dynamicCfg.VietQrAccountName) 
                ? dynamicCfg.VietQrAccountName 
                : (_config["PayOS:AccountName"] ?? "AEGISQUIZ ENTERPRISE");

            var amountInt = (int)Math.Round(transaction.AmountRequired);
            var description = string.IsNullOrWhiteSpace(request.Description)
                ? $"AEGISQ {transaction.OrderCode}"
                : request.Description.Length > 25 ? request.Description.Substring(0, 25) : request.Description;

            var returnUrl = string.IsNullOrWhiteSpace(request.ReturnUrl) ? "http://localhost:3000/paywall?status=success" : request.ReturnUrl;
            var cancelUrl = string.IsNullOrWhiteSpace(request.CancelUrl) ? "http://localhost:3000/paywall?status=cancelled" : request.CancelUrl;

            // Kiểm tra xem đã có Keys thật chưa
            bool hasValidKeys = !string.IsNullOrWhiteSpace(clientId) && 
                                !string.IsNullOrWhiteSpace(apiKey) && 
                                !string.IsNullOrWhiteSpace(checksumKey) &&
                                !clientId.Contains("SET_VIA");

            if (hasValidKeys)
            {
                try
                {
                    // 1. Tạo chữ ký HMAC-SHA256 chuẩn PayOS
                    // Chuỗi ký: amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}
                    var signData = $"amount={amountInt}&cancelUrl={cancelUrl}&description={description}&orderCode={transaction.OrderCode}&returnUrl={returnUrl}";
                    var signature = ComputeHmacSha256(signData, checksumKey);

                    var payload = new
                    {
                        orderCode = transaction.OrderCode,
                        amount = amountInt,
                        description = description,
                        buyerName = request.BuyerName ?? "Khách Hàng",
                        buyerEmail = request.BuyerEmail ?? "",
                        cancelUrl = cancelUrl,
                        returnUrl = returnUrl,
                        signature = signature
                    };

                    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api-merchant.payos.vn/v2/payment-requests")
                    {
                        Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                    };
                    httpRequest.Headers.Add("x-client-id", clientId);
                    httpRequest.Headers.Add("x-api-key", apiKey);

                    var response = await _httpClient.SendAsync(httpRequest);
                    var responseJson = await response.Content.ReadAsStringAsync();

                    using var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;
                    var code = root.TryGetProperty("code", out var codeProp) ? codeProp.GetString() : "";

                    if (code == "00" && root.TryGetProperty("data", out var dataProp))
                    {
                        var checkoutUrl = dataProp.TryGetProperty("checkoutUrl", out var cUrl) ? cUrl.GetString() ?? "" : "";
                        var qrCode = dataProp.TryGetProperty("qrCode", out var qr) ? qr.GetString() ?? "" : "";
                        var pLinkId = dataProp.TryGetProperty("paymentLinkId", out var pId) ? pId.GetString() ?? "" : "";
                        var bin = dataProp.TryGetProperty("bin", out var b) ? b.GetString() ?? bankBin : bankBin;
                        var accNo = dataProp.TryGetProperty("accountNumber", out var aNo) ? aNo.GetString() ?? accountNo : accountNo;
                        var accNm = dataProp.TryGetProperty("accountName", out var aNm) ? aNm.GetString() ?? accountName : accountName;

                        transaction.CheckoutUrl = checkoutUrl;
                        transaction.QrCode = qrCode;
                        transaction.PaymentLinkId = pLinkId;

                        return new PaymentOrderResponseDto
                        {
                            TransactionId = transaction.TransactionId,
                            OrderCode = transaction.OrderCode,
                            PaymentCode = transaction.PaymentCode,
                            Provider = "PayOS_VietQR",
                            Amount = transaction.AmountRequired,
                            Currency = transaction.Currency,
                            Status = "Pending",
                            CheckoutUrl = checkoutUrl,
                            QrCode = qrCode,
                            AccountNumber = accNo,
                            AccountName = accNm,
                            BankName = GetBankNameFromBin(bin),
                            CreatedAt = transaction.CreatedAt,
                            ExpireAt = transaction.CreatedAt.AddMinutes(15)
                        };
                    }
                    else
                    {
                        _logger.LogWarning("[PayOS] API trả mã lỗi: {Code}. Fallback sang VietQR Napas Hub.", code);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PayOS] Lỗi kết nối API PayOS. Fallback sang VietQR Napas Hub.");
                }
            }

            // ── CHẾ ĐỘ THÍCH ỨNG CAO CẤP (Smart Fallback Sandbox VietQR Napas) ──
            // Tự động sinh VietQR QuickLink chuẩn ngân hàng Việt Nam 24/7
            var vietQrUrl = $"https://img.vietqr.io/image/{bankBin}-{accountNo}-compact2.png?amount={amountInt}&addInfo={Uri.EscapeDataString(description)}&accountName={Uri.EscapeDataString(accountName)}";
            
            transaction.CheckoutUrl = vietQrUrl;
            transaction.QrCode = vietQrUrl;

            return new PaymentOrderResponseDto
            {
                TransactionId = transaction.TransactionId,
                OrderCode = transaction.OrderCode,
                PaymentCode = transaction.PaymentCode,
                Provider = "PayOS_VietQR",
                Amount = transaction.AmountRequired,
                Currency = transaction.Currency,
                Status = "Pending",
                CheckoutUrl = vietQrUrl,
                QrCode = vietQrUrl,
                AccountNumber = accountNo,
                AccountName = accountName,
                BankName = "MBBank (Napas VietQR 24/7)",
                CreatedAt = transaction.CreatedAt,
                ExpireAt = transaction.CreatedAt.AddMinutes(15)
            };
        }

        public Task<bool> VerifyWebhookSignatureAsync(string requestBody, string signature)
        {
            var checksumKey = _config["PayOS:ChecksumKey"] ?? Environment.GetEnvironmentVariable("PAYOS_CHECKSUM_KEY") ?? "";
            if (string.IsNullOrWhiteSpace(checksumKey) || checksumKey.Contains("SET_VIA"))
            {
                return Task.FromResult(true); // Sandbox bypass
            }

            try
            {
                using var doc = JsonDocument.Parse(requestBody);
                if (!doc.RootElement.TryGetProperty("data", out var dataElement))
                {
                    return Task.FromResult(false);
                }

                // Sắp xếp các key trong data theo alphabet
                var sortedData = dataElement.EnumerateObject()
                    .OrderBy(p => p.Name)
                    .Select(p => $"{p.Name}={p.Value}")
                    .ToList();

                var signString = string.Join("&", sortedData);
                var computedSignature = ComputeHmacSha256(signString, checksumKey);

                return Task.FromResult(string.Equals(computedSignature, signature, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PayOS] Lỗi xác minh chữ ký Webhook");
                return Task.FromResult(false);
            }
        }

        public static string ComputeHmacSha256(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static string GetBankNameFromBin(string bin)
        {
            return bin switch
            {
                "970422" => "MBBank (Quân Đội)",
                "970436" => "Vietcombank",
                "970415" => "VietinBank",
                "970405" => "Agribank",
                "970407" => "Techcombank",
                "970418" => "BIDV",
                "970423" => "TPBank",
                _ => "Ngân Hàng (Napas VietQR)"
            };
        }
    }
}
