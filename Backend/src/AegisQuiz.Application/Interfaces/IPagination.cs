namespace AegisQuiz.Application.Interfaces
{
    // ── Pagination Model ───────────────────────────────────────────────────────
    // Chuẩn PagedResult dùng chung cho toàn bộ API — World-Class pattern
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public int Total       { get; set; }
        public int Page        { get; set; }
        public int PageSize    { get; set; }
        public int TotalPages  => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 1;
        public bool HasNext    => Page < TotalPages;
        public bool HasPrev    => Page > 1;
    }

    // ── Sort Direction ─────────────────────────────────────────────────────────
    public enum SortDirection { Asc, Desc }

    // ── Base Paged Query ───────────────────────────────────────────────────────
    public class PagedQuery
    {
        private int _page = 1;
        private int _pageSize = 20;

        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value <= 0 ? 0 : value > 100000 ? 100000 : value;
        }

        /// <summary>
        /// Khi All = true hoặc PageSize = 0, hệ thống sẽ trả về toàn bộ dữ liệu không áp dụng phân trang cắt lát.
        /// </summary>
        public bool All { get; set; } = false;

        public string? SortBy      { get; set; }
        public SortDirection Sort  { get; set; } = SortDirection.Asc;
        public string? Search      { get; set; }
    }

    // ── Question Query ─────────────────────────────────────────────────────────
    public class QuestionPagedQuery : PagedQuery
    {
        public string? CategoryCode  { get; set; }
        public string? QuestionType  { get; set; }
        public int?    Difficulty    { get; set; }
        public bool?   Enabled       { get; set; }
        public bool    ShuffleAnswers { get; set; } = false;

        // [Universal Multi-Industry Taxonomy & Smart Tags]
        public string? DomainCode    { get; set; }
        public string? Tag           { get; set; }
        public int?    Year          { get; set; }
        public string? TargetLevel   { get; set; }
        public string? AssessmentPurpose { get; set; }
        public string? IssuingOrg    { get; set; }
    }
}
