using System;
using System.Collections.Generic;

namespace AegisQuiz.Application.Models.AI
{
    /// <summary>
    /// Bản ghi đơn lẻ trong Sổ cái Bằng chứng Thực nghiệm (Evidence Ledger).
    /// Được đánh mã định danh [E1], [E2]... làm căn cứ truy vết bắt buộc cho các Viện tranh biện và Trọng tài.
    /// </summary>
    public class EvidenceLedgerItemDto
    {
        /// <summary>Mã định danh bằng chứng (vd: "E1", "E2", "E3")</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Luận điểm / Dữ kiện thực chứng cốt lõi</summary>
        public string ClaimOrFact { get; set; } = string.Empty;

        /// <summary>Chỉ số định lượng hoặc số liệu đo đạc (nếu có)</summary>
        public string? QuantitativeValue { get; set; }

        /// <summary>Tên công trình / Tài liệu nguồn (Tác giả, Tạp chí, Năm công bố)</summary>
        public string SourceTitle { get; set; } = string.Empty;

        /// <summary>Định danh DOI hoặc liên kết truy cập chính thức</summary>
        public string? DoiOrUrl { get; set; }

        /// <summary>Xuất xứ nguồn: CuratedOfflineLibrary | ScholarlyApi | NotebookRag | ModelRecall</summary>
        public string Provenance { get; set; } = "CuratedOfflineLibrary";

        /// <summary>Đã được xác minh tồn tại trực tuyến (qua DOI hoặc Web API)</summary>
        public bool IsOnlineVerified { get; set; }

        /// <summary>Trọng số tin cậy khoa học từ 0.0 đến 1.0</summary>
        public double ConfidenceWeight { get; set; } = 0.95;
    }

    /// <summary>
    /// [AegisAI Deep Research Scout - Empirical Dossier Contract]
    /// Hồ sơ bằng chứng thực nghiệm do Phân hệ Deep Research tổng hợp làm đầu vào tối cao cho Đại Hội Thảo.
    /// Neo giữ mọi lập luận vào dữ liệu thực chứng thay vì giả định trừu tượng.
    /// </summary>
    public class EmpiricalEvidenceDossierDto
    {
        public string TopicTitle { get; set; } = string.Empty;
        public string DomainArea { get; set; } = string.Empty;
        
        /// <summary>Sổ cái Bằng chứng cấu trúc hóa [E1..En] bắt buộc trích dẫn</summary>
        public List<EvidenceLedgerItemDto> EvidenceLedger { get; set; } = new();

        /// <summary>Các tiên đề và sự kiện thực chứng đã được kiểm chứng (Empirical Facts)</summary>
        public List<string> VerifiedFacts { get; set; } = new();

        /// <summary>Các chỉ số đo đạc, dữ liệu định lượng, tỷ lệ % và sai số quan trắc</summary>
        public List<string> QuantitativeDataPoints { get; set; } = new();

        /// <summary>Nguồn trích dẫn học thuật uy tín (DOI, PubMed, ArXiv, văn bản pháp luật, tiêu chuẩn ISO)</summary>
        public List<string> AcademicCitations { get; set; } = new();

        /// <summary>Các điểm nghẽn tranh cãi thực địa chưa có sự đồng thuận khoa học (Controversies)</summary>
        public List<string> UnresolvedControversies { get; set; } = new();

        /// <summary>Bản định dạng Markdown hoàn chỉnh để nội suy trực tiếp vào System Prompt</summary>
        public string FormattedDossierMarkdown { get; set; } = string.Empty;

        public DateTime GatheredAt { get; set; } = DateTime.UtcNow;
        public int TotalEvidenceUnits { get; set; }

        /// <summary>Xuất xứ: ModelRecall | CuratedOfflineLibrary | ScholarlyApi | NotebookRag | GenericFallback.</summary>
        public string Provenance { get; set; } = string.Empty;

        /// <summary>true chỉ khi từng nguồn đã được xác minh trực tuyến (DOI/URL).</summary>
        public bool IsOnlineVerified { get; set; }

        /// <summary>Ghi chú trung thực về giới hạn của hồ sơ, hiển thị cho người dùng.</summary>
        public string? ProvenanceNote { get; set; }
    }
}
