using System.Diagnostics.Metrics;

namespace AegisQuiz.Application.Common
{
    /// <summary>
    /// [Observability Baseline] Đo lường chuẩn hóa hiệu năng, tải và độ trễ theo OpenTelemetry / Prometheus standard.
    /// Không phụ thuộc 3rd-party nặng nề, hoàn toàn native trên .NET BCL.
    /// </summary>
    public static class AegisObservability
    {
        public const string MeterName = "AegisQuiz.Engine";
        public const string MeterVersion = "2.6.0";

        public static readonly Meter Meter = new(MeterName, MeterVersion);

        // Đếm số câu hỏi người dùng đã trả lời (kèm tags: tenantId, result)
        public static readonly Counter<long> QuestionsAnsweredCounter = 
            Meter.CreateCounter<long>("aegis_questions_answered_total", "câu", "Tổng số câu hỏi người học đã làm");

        // Đếm cache hits và misses để tối ưu hóa chi phí I/O
        public static readonly Counter<long> CacheHitCounter = 
            Meter.CreateCounter<long>("aegis_cache_hits_total", "lần", "Số lần truy vấn trúng cache L1/L2");

        public static readonly Counter<long> CacheMissCounter = 
            Meter.CreateCounter<long>("aegis_cache_misses_total", "lần", "Số lần cache miss cần truy vấn DB");

        // Thời gian đáp ứng của AI Multi-Agent Router (ms)
        public static readonly Histogram<double> AiLatencyHistogram = 
            Meter.CreateHistogram<double>("aegis_ai_latency_ms", "ms", "Độ trễ phản hồi từ các provider AI");

        // Số phòng thi / phiên kiểm tra đang diễn ra đồng thời
        public static readonly UpDownCounter<long> ActiveExamSessionsCounter = 
            Meter.CreateUpDownCounter<long>("aegis_active_exam_sessions", "phiên", "Số phiên thi trực tuyến đang mở");
    }
}
