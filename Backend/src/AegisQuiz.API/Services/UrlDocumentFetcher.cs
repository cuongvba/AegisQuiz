using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AegisQuiz.API.Services
{
    public interface IUrlDocumentFetcher
    {
        Task<FetchedDocumentResult> FetchDocumentAsync(string rawUrl, CancellationToken cancellationToken = default);
    }

    public class FetchedDocumentResult
    {
        public bool Success { get; set; }
        public string DocumentType { get; set; } = "UNKNOWN"; // DOCX, EXCEL, PDF
        public string FileName { get; set; } = string.Empty;
        public MemoryStream? Stream { get; set; }
        public long FileSizeBytes { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsRestricted { get; set; }
        public string? SuggestedAction { get; set; }
        public string OriginalUrl { get; set; } = string.Empty;
        public string FinalDownloadUrl { get; set; } = string.Empty;
    }

    public class UrlDocumentFetcher : IUrlDocumentFetcher
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<UrlDocumentFetcher> _logger;

        private const long MaxFileSizeLimit = 50 * 1024 * 1024; // 50 MB limit

        public UrlDocumentFetcher(IHttpClientFactory httpClientFactory, ILogger<UrlDocumentFetcher> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<FetchedDocumentResult> FetchDocumentAsync(string rawUrl, CancellationToken cancellationToken = default)
        {
            var result = new FetchedDocumentResult
            {
                OriginalUrl = rawUrl?.Trim() ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(rawUrl))
            {
                result.ErrorMessage = "Đường dẫn URL không được để trống.";
                return result;
            }

            var cleanUrl = rawUrl.Trim();
            if (!Uri.TryCreate(cleanUrl, UriKind.Absolute, out var uri) || 
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                result.ErrorMessage = "Định dạng URL không hợp lệ. Vui lòng nhập link bắt đầu bằng http:// hoặc https://";
                return result;
            }

            // 1. SSRF Guard: Ngăn chặn truy cập vào dải mạng nội bộ hoặc localhost
            if (IsInternalOrPrivateHost(uri.Host))
            {
                _logger.LogWarning("[Security SSRF] Chặn yêu cầu tải tài liệu từ IP/Host nội bộ: {Host}", uri.Host);
                result.ErrorMessage = "Không được phép truy cập vào địa chỉ IP nội bộ hoặc mạng bảo vệ (SSRF Protection).";
                return result;
            }

            // 2. Phân tích loại dịch vụ & chuyển đổi sang URL tải trực tiếp
            var (downloadUrl, expectedDocType, suggestedFileName) = TransformToDownloadUrl(uri);
            result.FinalDownloadUrl = downloadUrl;
            result.DocumentType = expectedDocType;
            result.FileName = suggestedFileName;

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(35);
                client.DefaultRequestHeaders.UserAgent.Clear();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36 AegisQuiz/2.0");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

                using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                // Kiểm tra mã trạng thái HTTP
                if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                {
                    result.IsRestricted = true;
                    result.ErrorMessage = "Tài liệu này được cài đặt chế độ riêng tư hoặc yêu cầu đăng nhập tài khoản sở hữu.";
                    result.SuggestedAction = "Vui lòng mở tệp trên Google Drive / Docs, vào mục 'Chia sẻ' -> chuyển quyền truy cập chung thành 'Bất kỳ ai có đường liên kết (Người xem)' và đảm bảo không chặn tải xuống.";
                    return result;
                }

                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"Máy chủ lưu trữ tài liệu phản hồi mã lỗi HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).";
                    return result;
                }

                // Kiểm tra dung lượng Content-Length nếu có
                if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value > MaxFileSizeLimit)
                {
                    result.ErrorMessage = $"Dung lượng tệp ({response.Content.Headers.ContentLength.Value / (1024 * 1024)} MB) vượt quá giới hạn cho phép tối đa 50 MB.";
                    return result;
                }

                // Đọc nội dung vào MemoryStream
                var memoryStream = new MemoryStream();
                await response.Content.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;

                result.FileSizeBytes = memoryStream.Length;

                // Kiểm tra nếu nội dung trả về là trang HTML báo lỗi của Google Drive thay vì file nhị phân
                if (memoryStream.Length < 100000)
                {
                    memoryStream.Position = 0;
                    using var reader = new StreamReader(memoryStream, System.Text.Encoding.UTF8, leaveOpen: true);
                    var headerSample = await reader.ReadToEndAsync(cancellationToken);
                    memoryStream.Position = 0;

                    if (headerSample.Contains("<title>Google Drive - Can&#39;t download file</title>") ||
                        headerSample.Contains("Sorry, the owner hasn&#39;t given you permission to download this file") ||
                        headerSample.Contains("Bạn phải đăng nhập để truy cập nội dung này") ||
                        headerSample.Contains("Too Many Requests") ||
                        (headerSample.Contains("<!DOCTYPE html>") && headerSample.Contains("docs.google.com") && !expectedDocType.Equals("HTML", StringComparison.OrdinalIgnoreCase)))
                    {
                        result.IsRestricted = true;
                        result.ErrorMessage = "Chủ sở hữu tài liệu Google Docs/Drive đã khóa quyền tải tệp đối với người xem công khai.";
                        result.SuggestedAction = "Để nạp tự động, chủ tài liệu cần vào Cài đặt chia sẻ trên Google Drive và bỏ chọn mục 'Người xem và người nhận xét có thể thấy tùy chọn tải xuống'. Hoặc bạn có thể tải file .docx về máy tính rồi kéo thả vào đây.";
                        return result;
                    }
                }

                // Bóc tách tên file thực tế từ Content-Disposition header nếu có
                var contentDisposition = response.Content.Headers.ContentDisposition;
                if (!string.IsNullOrWhiteSpace(contentDisposition?.FileName))
                {
                    result.FileName = CleanFileName(contentDisposition.FileName);
                }
                else if (!string.IsNullOrWhiteSpace(contentDisposition?.FileNameStar))
                {
                    result.FileName = CleanFileName(contentDisposition.FileNameStar);
                }

                // Xác định chính xác định dạng qua Magic Bytes nếu chưa rõ
                result.DocumentType = RefineDocumentType(memoryStream, result.FileName, result.DocumentType);

                result.Stream = memoryStream;
                result.Success = true;
                return result;
            }
            catch (TaskCanceledException)
            {
                result.ErrorMessage = "Quá thời gian chờ phản hồi từ máy chủ tài liệu (Timeout 35s). Vui lòng thử lại.";
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UrlDocumentFetcher] Lỗi khi tải tài liệu từ {Url}: {Msg}", cleanUrl, ex.Message);
                result.ErrorMessage = $"Không thể tải tệp từ liên kết đã cung cấp: {ex.Message}";
                return result;
            }
        }

        private static (string downloadUrl, string expectedDocType, string suggestedFileName) TransformToDownloadUrl(Uri uri)
        {
            var host = uri.Host.ToLowerInvariant();
            var path = uri.AbsolutePath;

            // 1. Google Docs (document/d/{id})
            var gdocMatch = Regex.Match(path, @"/document/d/([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase);
            if (host.Contains("docs.google.com") && gdocMatch.Success)
            {
                var docId = gdocMatch.Groups[1].Value;
                var exportUrl = $"https://docs.google.com/document/d/{docId}/export?format=docx";
                return (exportUrl, "DOCX", $"GoogleDoc_{docId.Substring(0, Math.Min(8, docId.Length))}.docx");
            }

            // 2. Google Sheets (spreadsheets/d/{id})
            var gsheetMatch = Regex.Match(path, @"/spreadsheets/d/([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase);
            if (host.Contains("docs.google.com") && gsheetMatch.Success)
            {
                var sheetId = gsheetMatch.Groups[1].Value;
                var gidMatch = Regex.Match(uri.Fragment + uri.Query, @"gid=([0-9]+)", RegexOptions.IgnoreCase);
                var gidParam = gidMatch.Success ? $"&gid={gidMatch.Groups[1].Value}" : "";
                var exportUrl = $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=xlsx{gidParam}";
                return (exportUrl, "EXCEL", $"GoogleSheet_{sheetId.Substring(0, Math.Min(8, sheetId.Length))}.xlsx");
            }

            // 3. Google Drive File (drive.google.com/file/d/{id})
            var gdriveMatch = Regex.Match(path, @"/file/d/([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase);
            if (host.Contains("drive.google.com") && gdriveMatch.Success)
            {
                var fileId = gdriveMatch.Groups[1].Value;
                var downloadUrl = $"https://drive.usercontent.google.com/download?id={fileId}&export=download";
                return (downloadUrl, "DOCX", $"DriveFile_{fileId.Substring(0, Math.Min(8, fileId.Length))}.docx");
            }

            // 4. Dropbox Share link
            if (host.Contains("dropbox.com"))
            {
                var builder = new UriBuilder(uri);
                builder.Query = builder.Query.Replace("dl=0", "dl=1");
                if (!builder.Query.Contains("dl=1"))
                {
                    builder.Query = string.IsNullOrEmpty(builder.Query) ? "dl=1" : builder.Query.TrimStart('?') + "&dl=1";
                }
                var docType = path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ? "EXCEL" : "DOCX";
                return (builder.Uri.ToString(), docType, Path.GetFileName(path));
            }

            // 5. OneDrive / SharePoint
            if (host.Contains("1drv.ms") || host.Contains("sharepoint.com"))
            {
                var builder = new UriBuilder(uri);
                if (!builder.Query.Contains("download=1"))
                {
                    builder.Query = string.IsNullOrEmpty(builder.Query) ? "download=1" : builder.Query.TrimStart('?') + "&download=1";
                }
                var docType = path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ? "EXCEL" : "DOCX";
                return (builder.Uri.ToString(), docType, Path.GetFileName(path));
            }

            // 6. Direct File Link (suy đoán theo đuôi mở rộng)
            var lowerPath = path.ToLowerInvariant();
            if (lowerPath.EndsWith(".xlsx") || lowerPath.EndsWith(".xls"))
            {
                return (uri.ToString(), "EXCEL", Path.GetFileName(path));
            }
            if (lowerPath.EndsWith(".pdf"))
            {
                return (uri.ToString(), "PDF", Path.GetFileName(path));
            }

            // Mặc định xem như Word docx
            var defaultName = Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(defaultName) || !defaultName.Contains('.'))
            {
                defaultName = "Document_Import.docx";
            }
            return (uri.ToString(), "DOCX", defaultName);
        }

        private static string RefineDocumentType(MemoryStream stream, string fileName, string currentType)
        {
            if (stream.Length < 4) return currentType;

            stream.Position = 0;
            var header = new byte[4];
            stream.Read(header, 0, 4);
            stream.Position = 0;

            // PDF: %PDF (0x25, 0x50, 0x44, 0x46)
            if (header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46)
            {
                return "PDF";
            }

            // ZIP / OpenXML (DOCX, XLSX): PK.. (0x50, 0x4B, 0x03, 0x04)
            if (header[0] == 0x50 && header[1] == 0x4B)
            {
                if (fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || currentType == "EXCEL")
                {
                    return "EXCEL";
                }
                return "DOCX";
            }

            return currentType;
        }

        private static bool IsInternalOrPrivateHost(string host)
        {
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(host, "127.0.0.1")) return true;
            if (string.Equals(host, "::1")) return true;
            if (string.Equals(host, "0.0.0.0")) return true;

            // Phân giải IP nếu host là địa chỉ số
            if (IPAddress.TryParse(host, out var ip))
            {
                if (IPAddress.IsLoopback(ip)) return true;
                var bytes = ip.GetAddressBytes();
                if (bytes.Length == 4)
                {
                    // 10.0.0.0/8
                    if (bytes[0] == 10) return true;
                    // 172.16.0.0/12
                    if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                    // 192.168.0.0/16
                    if (bytes[0] == 192 && bytes[1] == 168) return true;
                    // 169.254.0.0/16 (Link Local / Cloud Metadata)
                    if (bytes[0] == 169 && bytes[1] == 254) return true;
                }
            }

            return false;
        }

        private static string CleanFileName(string rawName)
        {
            var cleaned = rawName.Trim('"', '\'', ' ');
            return string.IsNullOrWhiteSpace(cleaned) ? "document.docx" : cleaned;
        }
    }
}
