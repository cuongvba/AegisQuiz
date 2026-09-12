using System.Collections.Generic;

namespace AegisQuiz.Application.Interfaces
{
    public class ShuffledOptionsResult
    {
        public List<string> Options { get; set; } = new();
        public string? CorrectAnswer { get; set; }
        public string? CorrectOptionText { get; set; }
        public Dictionary<int, int> IndexMapping { get; set; } = new(); // 1-based index: oldIndex -> newIndex
        public bool HasAnchorPinned { get; set; }
        public bool WasShuffled { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// [AegisQuiz Smart Shuffler] Dịch vụ đảo vị trí phương án thông minh,
    /// giải quyết triệt để vấn đề tham chiếu tương đối (Anchor Pinning, Dynamic Re-mapping, Safeguard).
    /// </summary>
    public interface ISmartOptionShufflerService
    {
        /// <summary>
        /// Đảo các phương án cho một câu hỏi với cơ chế bảo vệ phương án neo và ánh xạ lại đáp án đúng chuẩn xác.
        /// </summary>
        ShuffledOptionsResult ShuffleOptions(
            List<string> options, 
            string? currentCorrectAnswer, 
            string? currentCorrectOptionText = null);
    }
}
