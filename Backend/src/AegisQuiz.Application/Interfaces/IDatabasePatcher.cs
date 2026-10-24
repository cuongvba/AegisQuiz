namespace AegisQuiz.Application.Interfaces
{
    /// <summary>
    /// [Zero Hard-code Architecture] Quản lý tự động vá và đồng bộ cấu trúc cơ sở dữ liệu
    /// từ các tệp cấu hình bên ngoài (*.sql) và tệp nhúng dự phòng (Embedded Resources).
    /// </summary>
    public interface IDatabasePatcher
    {
        /// <summary>
        /// Quét và áp dụng các tệp bản vá SQL đang chờ (Pending Patches).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Số lượng tệp bản vá hoặc câu lệnh đã được thực thi thành công</returns>
        Task<int> ApplyPendingPatchesAsync(CancellationToken cancellationToken = default);
    }
}
