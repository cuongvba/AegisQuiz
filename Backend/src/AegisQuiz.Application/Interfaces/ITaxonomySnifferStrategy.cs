namespace AegisQuiz.Application.Interfaces
{
    public record TaxonomyAlignmentResult(
        string DomainCode,
        string DomainName,
        string TargetLevel,
        string CategoryCode,
        string CategoryName,
        string BenchmarkStandard,
        string CognitiveLevel,
        double ConfidenceScore
    );

    /// <summary>
    /// [P1 OCP Taxonomy Sniffer] Chiến lược phân loại ma trận tri thức tự động cho từng ngành/môn học.
    /// Cho phép mở rộng ngành/môn học mới vô hạn mà không sửa mã nguồn lõi.
    /// </summary>
    public interface ITaxonomySnifferStrategy
    {
        int Priority { get; }
        bool CanHandle(string contentPreview);
        TaxonomyAlignmentResult Sniff(string contentPreview);
    }

    /// <summary>
    /// Dispatcher điều phối Strategy nhận diện ma trận tri thức phù hợp nhất.
    /// </summary>
    public interface ITaxonomySnifferDispatcher
    {
        TaxonomyAlignmentResult SniffTaxonomy(string contentPreview);
    }
}
