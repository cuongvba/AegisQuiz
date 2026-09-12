using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using AegisQuiz.Domain.Entities;
using AegisQuiz.Domain.Factories;
using AegisQuiz.Application.Interfaces;
using AegisQuiz.Application.DTOs;
using AegisQuiz.Infrastructure.Data;
using AegisQuiz.Infrastructure.Services;

namespace AegisQuiz.Infrastructure.Excel
{
    /// <summary>
    /// [AegisQuiz Transcendent Dynamic Excel Ingestion Engine]
    /// Nâng cấp toàn diện bộ bóc tách Excel lên cảnh giới tối thượng của hệ thống:
    /// 1. Dynamic Cognitive Header & Column Sniffer: Tự động nhận thức cột linh hoạt qua từ vựng ngữ nghĩa đa ngôn ngữ.
    /// 2. Polymorphic Cell Ingestion: Tự động giải mã câu hỏi nằm trọn trong 1 ô / 1 cột thông qua DynamicQuestionIngestionEngine.
    /// 3. Toàn diện 8 Siêu hình thái tương tác: SINGLE, MULTI, TRUE_FALSE (đa mệnh đề GDPT 2025), FILL_BLANK, MATCHING, ORDERING, SHORT_ANSWER, ESSAY.
    /// 4. Polymorphic Answer Resolver: Giải mã đáp án chữ cái, số, boolean, ma trận mệnh đề, ghép cặp, thứ tự, và dấu hoa thị (*) trong ô phương án.
    /// 5. Critical / Fatal Fail Recognition: Tự động nhận diện câu điểm liệt tử thần qua Dynamic Rules & từ khóa cảnh báo.
    /// 6. Section Hierarchy & Shared Context: Tự động bóc tách phân đoạn (Phần/Chương) và neo đoạn văn bản đọc hiểu dùng chung (Reading Passage).
    /// 7. Vietnamese Text Healing & Maprepl Integration: Chuẩn hóa NFC, khử rác Excel (_x000D_, non-breaking space) và sửa lỗi hiển thị.
    /// 8. Đồng bộ hoàn hảo: Cả ParseExcelFile (lưu CSDL) và ParseExcelFileToPreview (hiển thị UI) đều dùng chung một lõi động cơ nhận thức cao cấp.
    /// </summary>
    public class ExcelParserService : IExcelParserService
    {
        private readonly AegisQuizDbContext _dbContext;
        private readonly IVietnameseTextCorrectionService? _textCorrectionService;
        private readonly ISmartOptionShufflerService? _smartOptionShufflerService;

        public ExcelParserService(
            AegisQuizDbContext dbContext,
            IVietnameseTextCorrectionService? textCorrectionService = null,
            ISmartOptionShufflerService? smartOptionShufflerService = null)
        {
            _dbContext = dbContext;
            _textCorrectionService = textCorrectionService;
            _smartOptionShufflerService = smartOptionShufflerService ?? (textCorrectionService != null ? new SmartOptionShufflerService(textCorrectionService) : null);
        }

        // ── Regex Cognitive cho Header Detection (chuẩn hóa tiếng Việt không dấu) ──
        private static readonly Regex _rxQuestionCognitive = new(
            @"(cau\s*hoi|noi\s*dung|de\s*bai|yeu\s*cau|tinh\s*huong|van\s*de|menh\s*de|khang\s*dinh|question|content|prompt|stem|problem|statement|task|title)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _rxOptNCognitive = new(
            @"((?:da[pn]\s*an|phuong\s*an|lua\s*chon|option|pa|choice|tra\s*loi))\s*([0-9]+|[a-hA-H])\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _rxSingleLetterOpt = new(
            @"^(?:\[?|\(?)([a-hA-H])(?:\]?|\)?|\.?)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _rxAnswerCognitive = new(
            @"(?:da[pn]\s*an\s*(?:dung|d\b|chinh\s*xac|nhat|chon|chuan|cau\s*hoi|1\s*2\s*3\s*4|a\s*b\s*c\s*d)|phuong\s*an\s*(?:dung|d\b|chinh\s*xac|nhat|chon|chuan|1\s*2\s*3\s*4|a\s*b\s*c\s*d)|pa\s*(?:dung|d\b)|d\s*a\s*(?:dung|d\b)|cau\s*tra\s*loi\s*(?:dung|chinh\s*xac)|correct\s*(?:answer|option)?|ket\s*qua|key|dap\s*so|nghiem|^da[pn]\s*an$|^phuong\s*an$|^tra\s*loi$)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _rxRefCognitive = new(
            @"(trich\s*dan|nguon|giai\s*thich|can\s*cu|can\s*cu\s*phap\s*ly|huong\s*dan|reference|citation|explanation|rationale|ghi\s*chu|luu\s*y|barem|rubric|dieu\s*khoan)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _rxTopicCodeCognitive = new(
            @"(ma\s*cd|ma\s*chu\s*de|topic\s*code|topiccode|mcd|chu\s*de|chuyen\s*de|ma\s*chuyen\s*de|category|domain|nghiep\s*vu|linh\s*vuc|phan\b)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _rxTopicNameCognitive = new(
            @"(ten\s*chu\s*de|ten\s*cd|topic\s*name|ten\s*chuyen\s*de)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _rxTypeCognitive = new(
            @"(loai|loai\s*cau\s*hoi|hinh\s*thuc|type|question\s*type|kieu)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _rxDifficultyCognitive = new(
            @"(do\s*kho|muc\s*do|bac\s*nhan\s*thuc|bloom|difficulty|hardness|level)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _rxDurationCognitive = new(
            @"(thoi\s*gian|duration|time|so\s*giay)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _rxCriticalCognitive = new(
            @"(diem\s*liet|liet|tu\s*than|fatal|critical|bat\s*buoc\s*dung|cau\s*cot\s*loi)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _rxSubCategoryCognitive = new(
            @"(phan\s*loai|nhom|sub\s*category|subcategory|chuyen\s*de\s*phu|sa\s*hinh|bien\s*bao|luat)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _rxContextCognitive = new(
            @"(bai\s*doc|ngu\s*canh|doan\s*van|reading\s*passage|passage|context|tinh\s*huong\s*chung)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _rxSectionMarker = new(
            @"^(?:PHẦN|CHƯƠNG|MỤC|BÀI|PHAN|CHUONG|PART|SECTION|CHAPTER)\s+([0-9IVXLCDM]+|[A-Z])[\.\:\s\-]+(.*)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _rxPassageMarker = new(
            @"^(?:Đọc\s+đoạn\s+văn|Cho\s+tình\s+huống|Bối\s+cảnh|Căn\s+cứ\s+hồ\s+sơ|Read\s+the\s+following|Passage|Context)[\.\:\s\-]+",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // ═════════════════════════════════════════════════════════════════════
        // 1. DIRECT PARSE TO ENTITIES (Lưu thẳng vào CSDL thông qua lõi thông minh)
        // ═════════════════════════════════════════════════════════════════════
        public List<QuestionBase> ParseExcelFile(Stream fileStream)
        {
            // Tận dụng 100% động cơ preview đa tầng để bóc tách chính xác tuyệt đối
            var previewResult = ParseExcelFileToPreview(fileStream, "DirectExcelImport.xlsx");
            var result = new List<QuestionBase>();

            // Map quản lý ngân hàng chủ đề (BankTopics)
            var topicCache = new Dictionary<string, BankTopic>(StringComparer.OrdinalIgnoreCase);

            foreach (var sheet in previewResult.Sheets)
            {
                var topicCode = sheet.DetectedTopicCode;
                var topicName = sheet.DetectedTopicName;

                // Bảo đảm Topic tồn tại trong DB
                if (!topicCache.TryGetValue(topicCode, out var bankTopic))
                {
                    bankTopic = _dbContext.BankTopics.FirstOrDefault(t => t.Code == topicCode)
                             ?? _dbContext.BankTopics.Local.FirstOrDefault(t => t.Code == topicCode);

                    if (bankTopic == null)
                    {
                        bankTopic = new BankTopic
                        {
                            Id = Guid.NewGuid(),
                            Code = topicCode,
                            Name = topicName,
                            Description = $"Chủ đề được tạo tự động từ sheet {sheet.SheetName}",
                            CategoryCode = "GENERAL",
                            VisibilityScope = "PUBLIC",
                            Enabled = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        _dbContext.BankTopics.Add(bankTopic);
                    }
                    topicCache[topicCode] = bankTopic;
                }

                // Chuyển hóa từng câu hỏi preview thành thực thể QuestionBase
                foreach (var qDto in sheet.Questions)
                {
                    var qType = QuestionFactory.NormalizeType(qDto.QuestionType);
                    var difficulty = Math.Clamp(qDto.Difficulty, 1, 5);
                    var duration = 45;

                    var cleanContent = qDto.Content;
                    var activeOptions = qDto.Options ?? new List<string>();
                    var answerRaw = qDto.SuggestedAnswer ?? string.Empty;
                    var reference = qDto.AiExplanation ?? string.Empty;

                    // Xây dựng JSONB Payload chuẩn 8 hình thái tương tác
                    var payload = BuildPayload(qType, activeOptions, answerRaw, reference, qDto);
                    if (payload == null) continue;

                    try
                    {
                        var question = QuestionFactory.CreateQuestion(qType, cleanContent);
                        question.Difficulty = difficulty;
                        question.DurationSeconds = duration;
                        question.CategoryCode = topicCode;
                        question.Payload = payload;
                        question.CreatedAt = DateTime.UtcNow;
                        question.IsCritical = qDto.IsCritical;
                        question.SubCategory = qDto.SubCategory;
                        question.DomainCode = qDto.DomainCode ?? "GENERAL";
                        question.Tags = qDto.Tags ?? new List<string>();

                        // Gán Options và CorrectOption cho thực thể (bảo toàn chỉ mục số 1, 2, 3...)
                        question.Options = activeOptions;
                        question.CorrectOption = !string.IsNullOrWhiteSpace(answerRaw) ? answerRaw : "1";

                        // Tự động nhận diện media type
                        question.ContentType = NormalizeFormatType(qDto.ContentType, DetectMediaType(cleanContent));
                        var nonTextOptTypes = activeOptions.Select(DetectMediaType).Where(t => t != "text").Distinct().ToList();
                        question.OptionType = NormalizeFormatType(qDto.OptionType, nonTextOptTypes.FirstOrDefault() ?? "text");

                        result.Add(question);
                    }
                    catch
                    {
                        // Bỏ qua câu hỏi nếu có lỗi cấu trúc đặc thù
                        continue;
                    }
                }
            }

            return result;
        }

        // ═════════════════════════════════════════════════════════════════════
        // 2. PARSE TO PREVIEW — DOT-2026 & UNIVERSAL MULTI-SHEET SMART PARSER
        // ═════════════════════════════════════════════════════════════════════
        public ExcelImportPreviewResult ParseExcelFileToPreview(Stream fileStream, string? fileName = null)
        {
            var result = new ExcelImportPreviewResult
            {
                FileName = fileName ?? "Import.xlsx"
            };

            using var workbook = OpenWorkbook(fileStream);
            result.TotalSheets = workbook.NumberOfSheets;

            var fileTopicBase = ExtractTopicFromFileName(fileName ?? "");

            for (int s = 0; s < workbook.NumberOfSheets; s++)
            {
                var sheet = workbook.GetSheetAt(s);
                if (sheet == null) continue;

                var sheetName = sheet.SheetName.Trim();

                var sampleRows = ExtractSampleRows(sheet, 10);

                // 1. Tự động phân loại và xử lý các sheet phụ trợ (văn bản, danh mục, barem, thống kê)
                if (IsSkippableSheet(sheetName))
                {
                    result.SkippedSheets.Add(new ExcelSkippedSheetPreview
                    {
                        SheetIndex = s,
                        SheetName = sheetName,
                        Reason = "Được nhận diện là tài liệu văn bản / danh mục tham khảo phi câu hỏi",
                        SuggestedRole = "REFERENCE_DOCS",
                        RowCount = sheet.LastRowNum + 1,
                        SampleRows = sampleRows
                    });
                    continue;
                }

                // 2. Nhận diện dòng Header bằng Cognitive Sniffer
                var colMap = DetectCognitiveHeader(sheet);
                if (colMap == null)
                {
                    result.SkippedSheets.Add(new ExcelSkippedSheetPreview
                    {
                        SheetIndex = s,
                        SheetName = sheetName,
                        Reason = "Không tìm thấy cấu trúc dòng tiêu đề nhận thức hợp lệ",
                        SuggestedRole = "METADATA_COVER",
                        RowCount = sheet.LastRowNum + 1,
                        SampleRows = sampleRows
                    });
                    result.Warnings.Add($"Sheet '{sheetName}': Không tìm thấy cấu trúc dòng tiêu đề nhận thức hợp lệ.");
                    continue;
                }

                // 3. Nhận diện chủ đề của sheet
                var (topicCode, topicName, parentTopicCode, parentTopicName, topicDesc) = DetectTopicFromSheetContext(
                    sheetName, fileTopicBase, fileName ?? "", sheet.LastRowNum);

                var sheetPreview = new ExcelSheetPreview
                {
                    SheetIndex = s,
                    SheetName = sheetName,
                    SheetRole = "EXAM_QUIZ",
                    ConfidenceScore = 0.98,
                    IsEnabledForImport = true,
                    RowCount = sheet.LastRowNum + 1,
                    ColumnCount = colMap != null ? Math.Max(colMap.Answer + 1, colMap.OptionCols.Values.DefaultIfEmpty(0).Max() + 1) : 0,
                    HeaderRowIndex = colMap.HeaderRowIndex,
                    DetectedTopicCode = topicCode,
                    DetectedTopicName = topicName,
                    DetectedParentTopicCode = parentTopicCode,
                    DetectedParentTopicName = parentTopicName,
                    DetectedTopicDescription = topicDesc,
                    SampleRows = sampleRows
                };

                // Trích xuất ánh xạ cột
                var colDict = new Dictionary<string, int>();
                if (colMap.Stt >= 0) colDict["stt"] = colMap.Stt;
                if (colMap.Content >= 0) colDict["content"] = colMap.Content;
                foreach (var opt in colMap.OptionCols)
                {
                    colDict[$"option_{opt.Key}"] = opt.Value;
                }
                if (colMap.Answer >= 0) colDict["answer"] = colMap.Answer;
                if (colMap.Reference >= 0) colDict["reference"] = colMap.Reference;
                if (colMap.Critical >= 0) colDict["critical"] = colMap.Critical;
                if (colMap.Difficulty >= 0) colDict["difficulty"] = colMap.Difficulty;
                if (colMap.TopicCode >= 0) colDict["topicCode"] = colMap.TopicCode;
                if (colMap.TopicName >= 0) colDict["topicName"] = colMap.TopicName;
                if (colMap.Type >= 0) colDict["type"] = colMap.Type;
                if (colMap.Context >= 0) colDict["context"] = colMap.Context;
                if (colMap.ContextTitle >= 0) colDict["contextTitle"] = colMap.ContextTitle;
                if (colMap.Rubric >= 0) colDict["rubric"] = colMap.Rubric;
                if (colMap.Duration >= 0) colDict["duration"] = colMap.Duration;
                if (colMap.SubCategory >= 0) colDict["subCategory"] = colMap.SubCategory;
                if (colMap.Tags >= 0) colDict["tags"] = colMap.Tags;
                if (colMap.MediaUrl >= 0) colDict["mediaUrl"] = colMap.MediaUrl;
                if (colMap.TargetLevel >= 0) colDict["targetLevel"] = colMap.TargetLevel;
                if (colMap.DomainCode >= 0) colDict["domainCode"] = colMap.DomainCode;
                sheetPreview.ColumnMapping = colDict;

                // Thu thập các dòng tiêu đề / banner phía trên dòng Header (từ hàng 0 đến colMap.HeaderRowIndex - 1)
                var headerBannerLines = new List<string>();
                for (int hr = 0; hr < colMap.HeaderRowIndex; hr++)
                {
                    var hRow = sheet.GetRow(hr);
                    if (hRow == null) continue;
                    var lineParts = new List<string>();
                    int maxC = Math.Min((int)hRow.LastCellNum, 25);
                    for (int c = hRow.FirstCellNum; c < maxC; c++)
                    {
                        var cellVal = GetCellValue(hRow.GetCell(c)).Trim();
                        if (!string.IsNullOrWhiteSpace(cellVal))
                        {
                            lineParts.Add(cellVal);
                        }
                    }
                    if (lineParts.Count > 0)
                    {
                        headerBannerLines.Add(string.Join(" | ", lineParts));
                    }
                }

                // Trạng thái ngữ cảnh theo dõi xuyên suốt sheet
                string currentSectionTitle = string.Empty;
                string currentSubCategory = string.Empty;
                string currentContextTitle = string.Empty;
                string currentContextContent = string.Empty;

                int startRow = colMap.DataStartRowIndex >= 0 ? colMap.DataStartRowIndex : colMap.HeaderRowIndex + 1;

                for (int r = startRow; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row == null) continue;

                    // Lấy ô số thứ tự và nội dung
                    var sttVal = GetCellValue(row, colMap.Stt).Trim();
                    var contentVal = colMap.Content >= 0 ? GetCellValue(row, colMap.Content).Trim() : string.Empty;

                    // Nếu cột nội dung trống, quét xem ô nào dài nhất trong hàng
                    if (string.IsNullOrWhiteSpace(contentVal))
                    {
                        contentVal = FindLongestCellInRow(row);
                    }

                    if (string.IsNullOrWhiteSpace(contentVal)) continue;

                    // A. Phát hiện dòng phân đoạn (Section Header: Phần I, Chương II, hoặc I. KIẾN THỨC CHUNG...)
                    var sectionMatch = _rxSectionMarker.Match(contentVal);
                    bool isExplicitSection = sectionMatch.Success ||
                        Regex.IsMatch(contentVal, @"^(?:[IVXLCDM0-9]+[\.\:\)\s\-]+)?(?:PHẦN|CHƯƠNG|MỤC|BÀI|CHUYÊN ĐỀ|KIẾN THỨC)\b", RegexOptions.IgnoreCase);

                    bool hasOptionsInRow = colMap.OptionCols.Values.Any(c => !string.IsNullOrWhiteSpace(GetCellValue(row, c)));
                    bool hasAnswerInRow = colMap.Answer >= 0 && !string.IsNullOrWhiteSpace(GetCellValue(row, colMap.Answer));

                    if ((isExplicitSection || (!hasOptionsInRow && !hasAnswerInRow && !HasInlineQuestionFormat(contentVal)))
                        && (colMap.OptionCols.Count >= 2 || isExplicitSection))
                    {
                        currentSectionTitle = contentVal;
                        currentSubCategory = GenerateVietnameseSlug(contentVal);
                        continue;
                    }

                    // B. Phát hiện dòng Ngữ cảnh / Đọc hiểu dùng chung (Reading Passage / Scenario)
                    var passageMatch = _rxPassageMarker.Match(contentVal);
                    if (passageMatch.Success && contentVal.Length > 80 && !HasAnswersInRow(row, colMap))
                    {
                        currentContextTitle = "Tình huống nghiệp vụ / Đoạn văn đọc hiểu";
                        currentContextContent = contentVal;
                        continue;
                    }

                    if (!IsValidDataRow(sttVal, contentVal)) continue;

                    // C. Kiểm tra xem ô Content có phải dạng Unstructured (Gộp cả phương án vào trong 1 ô) hay không
                    if (colMap.IsUnstructuredSingleCell || HasInlineQuestionFormat(contentVal))
                    {
                        var snippetResult = DynamicQuestionIngestionEngine.Instance.ParseSnippet(contentVal);
                        if (snippetResult.Success && snippetResult.Questions.Count > 0)
                        {
                            foreach (var parsedQ in snippetResult.Questions)
                            {
                                parsedQ.SuggestedAnswer = NormalizeCognitiveAnswer(parsedQ.SuggestedAnswer, parsedQ.Options, parsedQ.QuestionType);

                                // Dịch và chuẩn hóa cụm từ neo (Anchor Phrases) theo luật VN Correct
                                if (parsedQ.Options != null && parsedQ.Options.Count >= 2)
                                {
                                    for (int i = 0; i < parsedQ.Options.Count; i++)
                                    {
                                        var optText = CleanText(parsedQ.Options[i]);
                                        if (_textCorrectionService != null)
                                        {
                                            optText = _textCorrectionService.CorrectText(optText);
                                        }
                                        parsedQ.Options[i] = SmartOptionShufflerService.TranslateAnchorOption(optText);
                                    }

                                    // Lưu ý: Không áp dụng SmartOptionShuffler tại bước nạp ngân hàng câu hỏi (quản trị),
                                    // để bảo toàn nguyên vẹn thứ tự A, B, C, D gốc của tác giả đề thi.
                                    // SmartOptionShuffler chỉ được áp dụng động khi học viên vào thi/luyện tập.
                                }

                                EnrichAndFinalizeDto(parsedQ, topicCode, currentSubCategory, currentContextTitle, currentContextContent);
                                sheetPreview.Questions.Add(parsedQ);
                            }
                            continue;
                        }
                    }

                    // D. Xử lý câu hỏi có cấu trúc cột (Structured Question)
                    var options = new List<string>();
                    int markedCorrectOptionIndex = -1;

                    foreach (var pair in colMap.OptionCols)
                    {
                        var optRaw = GetCellValue(row, pair.Value).Trim();
                        if (string.IsNullOrWhiteSpace(optRaw)) continue;

                        // Kiểm tra xem phương án có dấu sao (*) hoặc (Đúng) đánh dấu đáp án đúng hay không
                        string rawTextToClean;
                        if (IsOptionMarkedCorrect(optRaw, out string cleanedOpt))
                        {
                            markedCorrectOptionIndex = pair.Key; // Chỉ mục 1-based của phương án
                            rawTextToClean = cleanedOpt;
                        }
                        else
                        {
                            rawTextToClean = optRaw;
                        }

                        var optCleaned = CleanText(rawTextToClean);
                        if (_textCorrectionService != null)
                        {
                            optCleaned = _textCorrectionService.CorrectText(optCleaned);
                        }
                        optCleaned = SmartOptionShufflerService.TranslateAnchorOption(optCleaned);
                        options.Add(optCleaned);
                    }

                    // Nếu không có cột option riêng, kiểm tra tách inline options từ contentVal
                    if (options.Count == 0)
                    {
                        var inlineMatches = DynamicQuestionIngestionEngine.ExtractInlineOptions(contentVal);
                        if (inlineMatches.Count >= 2)
                        {
                            var first = inlineMatches[0];
                            var prompt = contentVal.Substring(0, first.StartIndex).Trim();
                            contentVal = prompt;
                            foreach (var im in inlineMatches)
                            {
                                var optCleaned = CleanText(im.Content);
                                if (_textCorrectionService != null)
                                {
                                    optCleaned = _textCorrectionService.CorrectText(optCleaned);
                                }
                                optCleaned = SmartOptionShufflerService.TranslateAnchorOption(optCleaned);
                                options.Add(optCleaned);
                                if (im.IsMarkedCorrect) markedCorrectOptionIndex = (im.Letter - 'A' + 1);
                            }
                        }
                    }

                    // Bỏ qua nếu vẫn không có phương án và không phải dạng tự luận/điền từ
                    var typeRaw = colMap.Type >= 0 ? GetCellValue(row, colMap.Type).Trim() : string.Empty;
                    var answerRaw = colMap.Answer >= 0 ? GetCellValue(row, colMap.Answer).Trim() : string.Empty;
                    var reference = colMap.Reference >= 0 ? GetCellValue(row, colMap.Reference).Trim() : string.Empty;
                    var diffRaw = colMap.Difficulty >= 0 ? GetCellValue(row, colMap.Difficulty).Trim() : string.Empty;
                    var criticalRaw = colMap.Critical >= 0 ? GetCellValue(row, colMap.Critical).Trim() : string.Empty;
                    var subCatRaw = colMap.SubCategory >= 0 ? GetCellValue(row, colMap.SubCategory).Trim() : string.Empty;

                    // Mở rộng các trường Universal Cognitive:
                    var contextRaw = colMap.Context >= 0 ? GetCellValue(row, colMap.Context).Trim() : string.Empty;
                    var contextTitleRaw = colMap.ContextTitle >= 0 ? GetCellValue(row, colMap.ContextTitle).Trim() : string.Empty;
                    var rubricRaw = colMap.Rubric >= 0 ? GetCellValue(row, colMap.Rubric).Trim() : string.Empty;
                    var durationRaw = colMap.Duration >= 0 ? GetCellValue(row, colMap.Duration).Trim() : string.Empty;
                    var tagsRaw = colMap.Tags >= 0 ? GetCellValue(row, colMap.Tags).Trim() : string.Empty;
                    var mediaRaw = colMap.MediaUrl >= 0 ? GetCellValue(row, colMap.MediaUrl).Trim() : string.Empty;
                    var targetLevelRaw = colMap.TargetLevel >= 0 ? GetCellValue(row, colMap.TargetLevel).Trim() : string.Empty;
                    var domainRaw = colMap.DomainCode >= 0 ? GetCellValue(row, colMap.DomainCode).Trim() : string.Empty;

                    // Cập nhật ngữ cảnh dùng chung nếu có trên dòng này
                    if (!string.IsNullOrWhiteSpace(contextRaw))
                    {
                        currentContextContent = contextRaw;
                    }
                    if (!string.IsNullOrWhiteSpace(contextTitleRaw))
                    {
                        currentContextTitle = contextTitleRaw;
                    }

                    // Tính thời gian làm bài chuẩn (DurationSeconds)
                    int durationSeconds = 60;
                    if (!string.IsNullOrWhiteSpace(durationRaw))
                    {
                        durationSeconds = ParseCognitiveDuration(durationRaw);
                    }

                    // Nếu đáp án trống nhưng có phương án đánh dấu (*)
                    if (string.IsNullOrWhiteSpace(answerRaw) && markedCorrectOptionIndex > 0)
                    {
                        answerRaw = markedCorrectOptionIndex.ToString();
                    }

                    // Suy luận 8 Siêu hình thái tương tác (Universal 8 Interaction Primitives)
                    string questionType = !string.IsNullOrWhiteSpace(typeRaw)
                        ? QuestionFactory.NormalizeType(typeRaw)
                        : InferCognitiveQuestionType(contentVal, options, answerRaw);

                    // Giải mã đáp án đa hình (Polymorphic Answer Decoding)
                    string normalizedAnswer = NormalizeCognitiveAnswer(answerRaw, options, questionType);

                    // Lưu ý: Không áp dụng SmartOptionShuffler tại bước nạp ngân hàng câu hỏi (quản trị),
                    // để bảo toàn nguyên vẹn thứ tự A, B, C, D gốc của tác giả đề thi.
                    // SmartOptionShuffler chỉ được áp dụng động khi học viên vào thi/luyện tập.

                    // Nhận diện điểm liệt tử thần (Fatal Question Recognition)
                    bool isCritical = DetermineIsCritical(criticalRaw, contentVal);

                    // Tính toán độ khó nhận thức Bloom (1 - 4)
                    int difficultyLevel = ParseCognitiveDifficulty(diffRaw, contentVal);

                    var subCategory = !string.IsNullOrWhiteSpace(subCatRaw) ? GenerateVietnameseSlug(subCatRaw) : currentSubCategory;

                    // Tách thẻ tags từ cột riêng nếu có
                    var rowTags = GenerateSmartTags(topicCode, subCategory, isCritical, difficultyLevel);
                    if (!string.IsNullOrWhiteSpace(tagsRaw))
                    {
                        var customTags = tagsRaw.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim().StartsWith("#") ? t.Trim() : "#" + t.Trim())
                            .Where(t => t.Length > 1);
                        rowTags = rowTags.Concat(customTags).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    }

                    var dto = new DocxImportPreviewDto
                    {
                        TempId = Guid.NewGuid().ToString(),
                        Content = CleanText(contentVal),
                        QuestionType = questionType,
                        Options = options,
                        SuggestedAnswer = normalizedAnswer,
                        AiExplanation = CleanText(reference),
                        TopicCode = topicCode,
                        TopicName = topicName,
                        ParentTopicCode = parentTopicCode,
                        ParentTopicName = parentTopicName,
                        TopicDescription = topicDesc,
                        Difficulty = difficultyLevel,
                        IsCritical = isCritical,
                        SubCategory = string.IsNullOrWhiteSpace(subCategory) ? null : subCategory,
                        DomainCode = !string.IsNullOrWhiteSpace(domainRaw) ? domainRaw : "GENERAL",
                        Tags = rowTags,
                        TargetLevel = !string.IsNullOrWhiteSpace(targetLevelRaw) ? targetLevelRaw : null,
                        GradingRubric = !string.IsNullOrWhiteSpace(rubricRaw) ? rubricRaw : null,
                        DurationSeconds = durationSeconds,
                        MediaUrl = !string.IsNullOrWhiteSpace(mediaRaw) ? mediaRaw : null,
                        ContentType = !string.IsNullOrWhiteSpace(mediaRaw) ? "image" : "text",
                        OptionType = options.Any(o => o.Contains("data:image/") || DetectMediaType(o) != "text") ? "image" : "text",
                        ContextTitle = string.IsNullOrWhiteSpace(currentContextContent) ? null : currentContextTitle,
                        ContextContent = string.IsNullOrWhiteSpace(currentContextContent) ? null : currentContextContent
                    };

                    sheetPreview.Questions.Add(dto);
                }

                sheetPreview.QuestionCount = sheetPreview.Questions.Count;
                if (sheetPreview.QuestionCount > 0)
                {
                    var finalDesc = $"{topicName} | Đường dẫn: {parentTopicName} | Số lượng: {sheetPreview.QuestionCount} câu hỏi";
                    sheetPreview.DetectedTopicDescription = finalDesc;
                    foreach (var q in sheetPreview.Questions)
                    {
                        q.TopicDescription = finalDesc;
                    }
                    // [Universal Cognitive Coordinate & Taxonomy Sniffer]
                    // Tự động nhận diện Hệ 5 Trục Tọa Độ và Thẻ Thông Minh từ tiêu đề file, header banner và nội dung câu hỏi
                    var taxonomy = UniversalCoordinateTaxonomyEngine.SniffCoordinates(
                        fileName,
                        sheetName,
                        headerBannerLines,
                        sheetPreview.Questions.Select(q => q.Content),
                        sheetPreview.Questions.Select(q => q.AiExplanation));

                    sheetPreview.DetectedDomainCode = taxonomy.DomainCode;
                    sheetPreview.DetectedTargetLevel = taxonomy.TargetLevel;
                    sheetPreview.DetectedAssessmentPurpose = taxonomy.AssessmentPurpose;
                    sheetPreview.DetectedIssuingOrg = taxonomy.IssuingOrg;
                    sheetPreview.DetectedBenchmarkYear = taxonomy.BenchmarkYear;
                    sheetPreview.DetectedBenchmarkStandard = taxonomy.BenchmarkStandard;
                    sheetPreview.DetectedTags = taxonomy.Tags;

                    foreach (var q in sheetPreview.Questions)
                    {
                        if (string.IsNullOrWhiteSpace(q.DomainCode) || q.DomainCode == "GENERAL")
                        {
                            q.DomainCode = taxonomy.DomainCode;
                        }
                        if (string.IsNullOrWhiteSpace(q.TargetLevel))
                        {
                            q.TargetLevel = taxonomy.TargetLevel;
                        }
                        q.AssessmentPurpose ??= taxonomy.AssessmentPurpose;
                        q.IssuingOrg ??= taxonomy.IssuingOrg;
                        q.BenchmarkYear ??= taxonomy.BenchmarkYear;
                        q.BenchmarkStandard ??= taxonomy.BenchmarkStandard;
                        q.Tags = taxonomy.Tags.Concat(q.Tags).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    }

                    sheetPreview.HealthStats = new ExcelCognitiveHealthStats
                    {
                        ValidQuestionCount = sheetPreview.Questions.Count(q => !string.IsNullOrWhiteSpace(q.Content) && !string.IsNullOrWhiteSpace(q.SuggestedAnswer)),
                        MissingAnswerCount = sheetPreview.Questions.Count(q => string.IsNullOrWhiteSpace(q.SuggestedAnswer)),
                        FatalQuestionCount = sheetPreview.Questions.Count(q => q.IsCritical),
                        QuestionTypeBreakdown = sheetPreview.Questions
                            .GroupBy(q => q.QuestionType ?? "SINGLE")
                            .ToDictionary(g => g.Key, g => g.Count()),
                        BloomDistribution = sheetPreview.Questions
                            .GroupBy(q => q.Difficulty > 0 ? q.Difficulty : 3)
                            .ToDictionary(g => g.Key, g => g.Count())
                    };

                    result.Sheets.Add(sheetPreview);
                    result.TotalQuestions += sheetPreview.QuestionCount;
                }
                else
                {
                    result.Warnings.Add($"Sheet '{sheetName}': Không tìm thấy câu hỏi hợp lệ nào.");
                }
            }

            var firstSheet = result.Sheets.FirstOrDefault();
            if (firstSheet != null)
            {
                result.DetectedDomainCode = firstSheet.DetectedDomainCode;
                result.DetectedTargetLevel = firstSheet.DetectedTargetLevel;
                result.DetectedAssessmentPurpose = firstSheet.DetectedAssessmentPurpose;
                result.DetectedIssuingOrg = firstSheet.DetectedIssuingOrg;
                result.DetectedBenchmarkYear = firstSheet.DetectedBenchmarkYear;
                result.DetectedBenchmarkStandard = firstSheet.DetectedBenchmarkStandard;
                result.DetectedTags = firstSheet.DetectedTags;
            }

            return result;
        }

        private static List<List<string>> ExtractSampleRows(ISheet sheet, int maxRows = 10)
        {
            var sampleRows = new List<List<string>>();
            int totalRows = Math.Min(sheet.LastRowNum + 1, maxRows);
            for (int r = 0; r < totalRows; r++)
            {
                var row = sheet.GetRow(r);
                var rowList = new List<string>();
                if (row != null)
                {
                    int maxCols = Math.Min((int)row.LastCellNum, 20);
                    for (int c = 0; c < maxCols; c++)
                    {
                        rowList.Add(GetCellValue(row.GetCell(c)).Trim());
                    }
                }
                sampleRows.Add(rowList);
            }
            return sampleRows;
        }

        // ═════════════════════════════════════════════════════════════════════
        // 3. DYNAMIC COGNITIVE HEADER & COLUMN SNIFFER
        // ═════════════════════════════════════════════════════════════════════
        public class CognitiveColumnMap
        {
            public int HeaderRowIndex { get; set; } = 0;
            public int DataStartRowIndex { get; set; } = -1;
            public int Stt { get; set; } = -1;
            public int Content { get; set; } = -1;
            public SortedDictionary<int, int> OptionCols { get; set; } = new();
            public int Answer { get; set; } = -1;
            public int Reference { get; set; } = -1;
            public int TopicCode { get; set; } = -1;
            public int TopicName { get; set; } = -1;
            public int Type { get; set; } = -1;
            public int Difficulty { get; set; } = -1;
            public int Duration { get; set; } = -1;
            public int Critical { get; set; } = -1;
            public int SubCategory { get; set; } = -1;
            public int Context { get; set; } = -1;
            public int ContextTitle { get; set; } = -1;
            public int Rubric { get; set; } = -1;
            public int Tags { get; set; } = -1;
            public int MediaUrl { get; set; } = -1;
            public int TargetLevel { get; set; } = -1;
            public int DomainCode { get; set; } = -1;
            public int ContentType { get; set; } = -1;
            public int OptionType { get; set; } = -1;
            public bool IsUnstructuredSingleCell { get; set; } = false;
        }

        private static CognitiveColumnMap? DetectCognitiveHeader(ISheet sheet)
        {
            int maxScan = Math.Min(sheet.LastRowNum, 35);
            int bestScore = -1;
            CognitiveColumnMap? bestMap = null;

            for (int r = 0; r <= maxScan; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null) continue;

                var map = ScanSingleHeaderRow(row, r);

                // MULTI-ROW HEADER FUSION:
                // Nếu dòng r có Nội dung hoặc Đáp án, nhưng chưa có Options (do dòng r merge ô "PHƯƠNG ÁN LỰA CHỌN"),
                // ta kiểm tra dòng r + 1 ngay bên dưới xem có các cột A, B, C, D hay không
                if (r + 1 <= sheet.LastRowNum && map.OptionCols.Count == 0 && (map.Content >= 0 || map.Answer >= 0))
                {
                    var nextRow = sheet.GetRow(r + 1);
                    if (nextRow != null)
                    {
                        var nextMap = ScanSingleHeaderRow(nextRow, r + 1);
                        if (nextMap.OptionCols.Count >= 2)
                        {
                            var fusedMap = new CognitiveColumnMap
                            {
                                HeaderRowIndex = r,
                                DataStartRowIndex = r + 2,
                                Stt = map.Stt >= 0 ? map.Stt : nextMap.Stt,
                                Content = map.Content >= 0 ? map.Content : nextMap.Content,
                                Answer = map.Answer >= 0 ? map.Answer : nextMap.Answer,
                                Reference = map.Reference >= 0 ? map.Reference : nextMap.Reference,
                                TopicCode = map.TopicCode >= 0 ? map.TopicCode : nextMap.TopicCode,
                                TopicName = map.TopicName >= 0 ? map.TopicName : nextMap.TopicName,
                                Difficulty = map.Difficulty >= 0 ? map.Difficulty : nextMap.Difficulty,
                                Critical = map.Critical >= 0 ? map.Critical : nextMap.Critical,
                                Type = map.Type >= 0 ? map.Type : nextMap.Type,
                                Context = map.Context >= 0 ? map.Context : nextMap.Context,
                                ContextTitle = map.ContextTitle >= 0 ? map.ContextTitle : nextMap.ContextTitle,
                                Rubric = map.Rubric >= 0 ? map.Rubric : nextMap.Rubric,
                                Duration = map.Duration >= 0 ? map.Duration : nextMap.Duration,
                                SubCategory = map.SubCategory >= 0 ? map.SubCategory : nextMap.SubCategory,
                                Tags = map.Tags >= 0 ? map.Tags : nextMap.Tags,
                                MediaUrl = map.MediaUrl >= 0 ? map.MediaUrl : nextMap.MediaUrl,
                                TargetLevel = map.TargetLevel >= 0 ? map.TargetLevel : nextMap.TargetLevel,
                                DomainCode = map.DomainCode >= 0 ? map.DomainCode : nextMap.DomainCode
                            };
                            foreach (var opt in nextMap.OptionCols)
                            {
                                fusedMap.OptionCols[opt.Key] = opt.Value;
                            }

                            int fusedScore = CalculateMapScore(fusedMap);
                            if (fusedScore > bestScore)
                            {
                                bestScore = fusedScore;
                                bestMap = fusedMap;
                            }
                        }
                    }
                }

                int score = CalculateMapScore(map);
                if (score >= bestScore && score > 0)
                {
                    bestScore = score;
                    bestMap = map;
                }
            }

            // Nếu đạt điểm chuẩn >= 5 và có cột nội dung câu hỏi
            if (bestScore >= 5 && bestMap != null && bestMap.Content >= 0)
            {
                if (bestMap.OptionCols.Count == 0)
                {
                    bestMap.IsUnstructuredSingleCell = true;
                }
                return bestMap;
            }

            // Safe fallback: Tìm dòng có cấu trúc câu hỏi rõ ràng (không gán cứng cột!)
            for (int r = 0; r <= maxScan; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null) continue;
                var c0 = NormalizeHeader(GetCellValue(row.GetCell(0)));
                var c1 = NormalizeHeader(GetCellValue(row.GetCell(1)));

                var match0 = UniversalColumnTaxonomyMatrix.Instance.MatchColumn(c0);
                var match1 = UniversalColumnTaxonomyMatrix.Instance.MatchColumn(c1);

                if ((match0.TargetField == "STT" || Contains(c0, "stt", "tt") || c0 == "1") && match1.TargetField == "QUESTION_CONTENT")
                {
                    var fallbackMap = new CognitiveColumnMap
                    {
                        HeaderRowIndex = r,
                        DataStartRowIndex = r + 1,
                        Stt = 0,
                        Content = 1
                    };

                    int maxC = Math.Min((int)row.LastCellNum, 25);
                    for (int c = 2; c < maxC; c++)
                    {
                        var cellVal = GetCellValue(row.GetCell(c));
                        var cellMatch = UniversalColumnTaxonomyMatrix.Instance.MatchColumn(cellVal);
                        if (cellMatch.TargetField == "ANSWER_KEY" && fallbackMap.Answer < 0)
                        {
                            fallbackMap.Answer = c;
                        }
                        else if (cellMatch.TargetField == "OPTION" && cellMatch.OptionIndex > 0)
                        {
                            fallbackMap.OptionCols[cellMatch.OptionIndex] = c;
                        }
                        else if (cellMatch.TargetField == "EXPLANATION" && fallbackMap.Reference < 0)
                        {
                            fallbackMap.Reference = c;
                        }
                    }

                    if (fallbackMap.OptionCols.Count == 0)
                    {
                        fallbackMap.IsUnstructuredSingleCell = true;
                    }

                    return fallbackMap;
                }
            }

            return null;
        }

        private static CognitiveColumnMap ScanSingleHeaderRow(IRow row, int r)
        {
            var map = new CognitiveColumnMap { HeaderRowIndex = r, DataStartRowIndex = r + 1 };
            if (row == null) return map;

            int lastCol = Math.Min((int)row.LastCellNum, 25);
            int nonEmptyCount = 0;
            for (int c = row.FirstCellNum; c < lastCol; c++)
            {
                if (!string.IsNullOrWhiteSpace(GetCellValue(row.GetCell(c)))) nonEmptyCount++;
            }

            // Dòng tiêu đề bảng bắt buộc phải có ít nhất 2 ô có dữ liệu (loại trừ banner/tiêu đề tài liệu ở 1 ô đơn độc)
            if (nonEmptyCount < 2) return map;

            for (int c = row.FirstCellNum; c < lastCol; c++)
            {
                var cellVal = GetCellValue(row.GetCell(c));
                if (string.IsNullOrWhiteSpace(cellVal)) continue;

                var match = UniversalColumnTaxonomyMatrix.Instance.MatchColumn(cellVal);
                switch (match.TargetField)
                {
                    case "ANSWER_KEY":
                        if (map.Answer < 0) map.Answer = c;
                        break;
                    case "OPTION":
                        if (match.OptionIndex > 0)
                        {
                            map.OptionCols[match.OptionIndex] = c;
                        }
                        break;
                    case "QUESTION_CONTENT":
                        if (map.Content < 0) map.Content = c;
                        break;
                    case "EXPLANATION":
                        if (map.Reference < 0) map.Reference = c;
                        break;
                    case "TOPIC_CODE":
                        if (map.TopicCode < 0) map.TopicCode = c;
                        break;
                    case "TOPIC_NAME":
                        if (map.TopicName < 0) map.TopicName = c;
                        break;
                    case "STT":
                        if (map.Stt < 0) map.Stt = c;
                        break;
                    case "DIFFICULTY":
                        if (map.Difficulty < 0) map.Difficulty = c;
                        break;
                    case "CRITICAL":
                        if (map.Critical < 0) map.Critical = c;
                        break;
                    case "TYPE":
                        if (map.Type < 0) map.Type = c;
                        break;
                    case "CONTEXT":
                        if (map.Context < 0) map.Context = c;
                        break;
                    case "CONTEXT_TITLE":
                        if (map.ContextTitle < 0) map.ContextTitle = c;
                        break;
                    case "GRADING_RUBRIC":
                        if (map.Rubric < 0) map.Rubric = c;
                        break;
                    case "DURATION":
                        if (map.Duration < 0) map.Duration = c;
                        break;
                    case "SUB_CATEGORY":
                        if (map.SubCategory < 0) map.SubCategory = c;
                        break;
                    case "TAGS":
                        if (map.Tags < 0) map.Tags = c;
                        break;
                    case "MEDIA_URL":
                        if (map.MediaUrl < 0) map.MediaUrl = c;
                        break;
                    case "TARGET_LEVEL":
                        if (map.TargetLevel < 0) map.TargetLevel = c;
                        break;
                    case "DOMAIN_CODE":
                        if (map.DomainCode < 0) map.DomainCode = c;
                        break;
                }
            }

            return map;
        }

        private static int CalculateMapScore(CognitiveColumnMap map)
        {
            // Bắt buộc phải có cột nội dung hoặc đáp án
            if (map.Content < 0 && map.Answer < 0) return 0;

            int score = 0;
            if (map.Content >= 0) score += 5;
            if (map.Answer >= 0) score += 5;
            if (map.OptionCols.Count >= 2) score += map.OptionCols.Count * 2;
            if (map.Reference >= 0) score += 3;
            if (map.Stt >= 0) score += 1;
            if (map.TopicCode >= 0 || map.TopicName >= 0) score += 2;
            if (map.Difficulty >= 0) score += 2;
            if (map.Critical >= 0) score += 2;
            return score;
        }

        // ═════════════════════════════════════════════════════════════════════
        // 4. POLYMORPHIC ANSWER & INTERACTION DECODING ENGINE
        // ═════════════════════════════════════════════════════════════════════
        private static string InferCognitiveQuestionType(string content, List<string> options, string answerRaw)
        {
            var cleanAns = answerRaw.Trim();

            // 1. Nếu có từ 2 phương án trở lên: Tuyệt đối ưu tiên định dạng trắc nghiệm SINGLE
            if (options.Count >= 2)
            {
                // Kiểm tra chùm mệnh đề Đúng / Sai GDPT 2025 (a-Đ, b-S, c-Đ, d-S hoặc Đ,S,Đ,S)
                if (Regex.IsMatch(cleanAns, @"^(?:[a-d]\s*[\-\:\.]\s*[ĐSđsTFtf][\,\;\s]*){2,}$", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(cleanAns, @"^[ĐSđsTFtf](?:[\,\;\s]+[ĐSđsTFtf]){2,}$", RegexOptions.IgnoreCase))
                {
                    return "TRUE_FALSE";
                }

                // Kiểm tra ghép nối thực sự (Matching: 1-A, 2-B, 3-C)
                if (Regex.IsMatch(cleanAns, @"^(?:[0-9a-d]\s*[\-\:\=]\s*[0-9a-d][\,\;\s]*){2,}$", RegexOptions.IgnoreCase))
                {
                    return "MATCHING";
                }

                // Kiểm tra sắp xếp thứ tự thực sự (Ordering: 1,3,2,4 hoặc A-C-B-D)
                if (Regex.IsMatch(cleanAns, @"^[1-8](?:[\,\;\-\>]+[1-8]){2,}$") ||
                    Regex.IsMatch(cleanAns, @"^[A-Ha-h](?:[\,\;\-\>]+[A-Ha-h]){2,}$"))
                {
                    return "ORDERING";
                }

                // Kiểm tra nhiều đáp án (MULTI): Chỉ khi cleanAns chứa nhiều token số/chữ cái và không phải là nội dung phương án
                if (cleanAns.Contains(",") || cleanAns.Contains(";") || cleanAns.Contains("&") || cleanAns.Contains("+"))
                {
                    var isOptionTextMatch = options.Any(o =>
                        string.Equals(CleanAnswerToken(o), cleanAns, StringComparison.OrdinalIgnoreCase));

                    if (!isOptionTextMatch)
                    {
                        var tokens = cleanAns.Split(new[] { ',', ';', '&', '+' }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(t => CleanAnswerToken(t.Trim()))
                                             .Where(t => !string.IsNullOrWhiteSpace(t))
                                             .ToList();

                        if (tokens.Count >= 2)
                        {
                            bool allValidTokens = tokens.All(t =>
                            {
                                if (int.TryParse(t, out int num) && num >= 1 && num <= options.Count) return true;
                                if (t.Length == 1 && char.ToUpperInvariant(t[0]) >= 'A' && char.ToUpperInvariant(t[0]) < 'A' + options.Count) return true;
                                return false;
                            });

                            if (allValidTokens)
                            {
                                return "MULTI";
                            }
                        }
                    }
                }

                // Mọi câu hỏi có >= 2 phương án và có cột đáp án đúng đều là SINGLE
                // Kể cả câu hỏi 2 phương án (Đúng/Sai) vẫn giữ SINGLE để bảo toàn 2 options và chỉ mục 1, 2
                return "SINGLE";
            }

            // 2. Không có phương án (options.Count < 2):
            if (content.Contains("[___]") || content.Contains("____") || content.Contains("(...)"))
            {
                return "FILL_BLANK";
            }
            if (!string.IsNullOrWhiteSpace(cleanAns) && cleanAns.Length < 60)
            {
                return "SHORT_ANSWER";
            }

            return "ESSAY";
        }

        private static string NormalizeCognitiveAnswer(string raw, List<string> options, string questionType)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var v = CleanAnswerToken(raw);

            switch (questionType)
            {
                case "TRUE_FALSE":
                    if (v.Contains(",") || v.Contains(";") || v.Contains("-") || v.Contains(":"))
                        return v;
                    return v is "2" or "FALSE" or "F" or "SAI" or "S" ? "2" : "1";

                case "MULTI":
                    var tokens = v.Split(new[] { ',', ';', '&', '+' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(t => ResolveSingleAnswerId(t.Trim(), options))
                                  .Where(t => !string.IsNullOrWhiteSpace(t))
                                  .Distinct();
                    var joined = string.Join(", ", tokens);
                    return string.IsNullOrWhiteSpace(joined) ? "1" : joined;

                case "MATCHING":
                case "ORDERING":
                case "SHORT_ANSWER":
                case "FILL_BLANK":
                case "ESSAY":
                    return v;

                case "SINGLE":
                default:
                    return ResolveSingleAnswerId(v, options);
            }
        }

        private static string ResolveSingleAnswerId(string raw, List<string> options)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var v = CleanAnswerToken(raw);

            // Bỏ tiền tố "Đáp án", "Phương án", "Lựa chọn", "PA", "Chọn", "Câu", "Option"
            v = Regex.Replace(v, @"^(?:đáp\s*án|phương\s*án|phuong\s*an|dap\s*an|lựa\s*chọn|lua\s*chon|pa|chọn|chon|câu|cau|option)\s*", "", RegexOptions.IgnoreCase).Trim();

            // 1. Nếu là số chỉ mục hợp lệ: "1", "2", "3"...
            if (int.TryParse(v, out int n) && n >= 1 && n <= Math.Max(options.Count, 10))
            {
                return n.ToString();
            }

            // 2. Nếu là số thực double từ Excel (ví dụ "1.0", "2.0")
            if (double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double dVal)
                && dVal >= 1 && dVal <= Math.Max(options.Count, 10) && dVal % 1 == 0)
            {
                return ((int)dVal).ToString();
            }

            // 3. Nếu là chữ cái đơn lẻ A-J
            if (v.Length == 1)
            {
                char c = char.ToUpperInvariant(v[0]);
                if (c >= 'A' && c <= 'J')
                {
                    return (c - 'A' + 1).ToString();
                }
            }

            // 4. Chuẩn hóa qua SmartOptionShufflerService.TranslateAnchorOption
            var vTranslated = SmartOptionShufflerService.TranslateAnchorOption(v);
            var cleanV = Regex.Replace(v, @"^[A-Z0-9][\.\:\)\s\-]+", "", RegexOptions.IgnoreCase).Trim();

            // 5. So khớp nội dung chuỗi trực tiếp với danh sách options
            for (int i = 0; i < options.Count; i++)
            {
                var optText = options[i].Trim();
                var cleanOpt = Regex.Replace(optText, @"^[A-Z0-9][\.\:\)\s\-]+", "", RegexOptions.IgnoreCase).Trim();
                if (string.Equals(v, cleanOpt, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(v, optText, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(cleanV, cleanOpt, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(vTranslated, cleanOpt, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(vTranslated, optText, StringComparison.OrdinalIgnoreCase))
                {
                    return (i + 1).ToString();
                }

                if (cleanV.Length >= 6 && (cleanOpt.StartsWith(cleanV, StringComparison.OrdinalIgnoreCase) || cleanV.StartsWith(cleanOpt, StringComparison.OrdinalIgnoreCase)))
                {
                    return (i + 1).ToString();
                }
            }

            // 6. Nếu v là "Đúng" / "Sai" cho câu hỏi 2 phương án
            if (options.Count >= 2)
            {
                for (int i = 0; i < options.Count; i++)
                {
                    var optLower = options[i].ToLowerInvariant();
                    if (v.Equals("đúng", StringComparison.OrdinalIgnoreCase) && (optLower.Contains("đúng") || optLower.Contains("true")))
                        return (i + 1).ToString();
                    if (v.Equals("sai", StringComparison.OrdinalIgnoreCase) && (optLower.Contains("sai") || optLower.Contains("false")))
                        return (i + 1).ToString();
                }
            }

            // Nếu là số bất kỳ
            if (int.TryParse(v, out int anyNum) && anyNum >= 1 && anyNum <= 10)
            {
                return anyNum.ToString();
            }

            return v;
        }

        private static bool IsOptionMarkedCorrect(string rawOpt, out string cleaned)
        {
            cleaned = rawOpt;
            var t = rawOpt.Trim();

            if (t.StartsWith("*") || t.EndsWith("*") || t.Contains("(Đúng)") || t.Contains("(dung)") || t.Contains("[x]"))
            {
                cleaned = Regex.Replace(t, @"^\*\s*|\s*\*$", "").Trim();
                cleaned = Regex.Replace(cleaned, @"\s*\((?:Đúng|dung|true|correct)\)\s*", "", RegexOptions.IgnoreCase).Trim();
                cleaned = Regex.Replace(cleaned, @"\s*\[x\]\s*", "", RegexOptions.IgnoreCase).Trim();
                return true;
            }

            return false;
        }

        private static bool DetermineIsCritical(string criticalRaw, string contentVal)
        {
            if (!string.IsNullOrWhiteSpace(criticalRaw))
            {
                var c = criticalRaw.ToLowerInvariant();
                if (c == "1" || c == "có" || c == "co" || c == "true" || c == "x" || c.Contains("liệt") || c.Contains("fatal"))
                    return true;
            }

            return DynamicQuestionIngestionEngine.Instance.IsCriticalQuestion(contentVal);
        }

        private static int ParseCognitiveDifficulty(string diffRaw, string contentVal)
        {
            if (int.TryParse(diffRaw.Trim(), out int d) && d >= 1 && d <= 5) return d;

            var lower = diffRaw.ToLowerInvariant();
            if (lower.Contains("nhận biết") || lower.Contains("de") || lower.Contains("easy") || lower.Contains("cơ bản")) return 1;
            if (lower.Contains("thông hiểu") || lower.Contains("vừa") || lower.Contains("medium")) return 2;
            if (lower.Contains("vận dụng cao") || lower.Contains("rất khó") || lower.Contains("hard")) return 4;
            if (lower.Contains("vận dụng") || lower.Contains("khó")) return 3;

            // Heuristic từ độ dài câu hỏi
            if (contentVal.Length > 250) return 3;
            return 2;
        }

        private static int ParseCognitiveDuration(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 60;
            var clean = raw.Trim().ToLowerInvariant();
            var m = Regex.Match(clean, @"(\d+)");
            if (!m.Success) return 60;
            if (int.TryParse(m.Groups[1].Value, out int val))
            {
                if (clean.Contains("phút") || clean.Contains("min") || clean.EndsWith("m") || clean.Contains("p"))
                {
                    return val * 60;
                }
                return val > 0 ? val : 60;
            }
            return 60;
        }

        // ═════════════════════════════════════════════════════════════════════
        // 5. HELPER & TOPIC CONTEXT DETECTORS
        // ═════════════════════════════════════════════════════════════════════
        private static (string Code, string Name, string ParentCode, string ParentName, string Description) DetectTopicFromSheetContext(
            string sheetName, string fileTopicBase, string fileName, int estimatedCount = 0)
        {
            // 1. Xác định tên Thư mục (folderName)
            string folderName = "CHUNG";
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var dir = Path.GetDirectoryName(fileName);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    var lastDir = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    if (!string.IsNullOrWhiteSpace(lastDir)) folderName = lastDir;
                }
            }

            // 2. Sử dụng Động cơ Viết tắt Ngữ nghĩa Thông minh (Smart Semantic Abbreviation Engine - SSAE)
            // Sinh mã ngắn gọn (6-16 ký tự), chuẩn hoá từ điển nghiệp vụ, bảo toàn 100% tiếng Việt có dấu cho Tên
            var (parentCode, parentName, childCode, childName, desc) =
                SmartTopicAbbreviationEngine.BuildHierarchicalTopicInfo(folderName, fileName, sheetName, estimatedCount);

            return (childCode, childName, parentCode, parentName, desc);
        }

        private static string ExtractTopicFromFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return string.Empty;
            var name = Path.GetFileNameWithoutExtension(fileName).Trim();
            name = Regex.Replace(name, @"^\d+\.\s*", "").Trim();
            return name;
        }

        private static bool IsSkippableSheet(string sheetName)
        {
            if (string.IsNullOrWhiteSpace(sheetName)) return true;
            var lower = NormalizeHeader(sheetName);

            // 1. Nếu tên sheet chứa dấu hiệu câu hỏi / đề thi -> Tuyệt đối không bỏ qua
            if (lower.Contains("cau") || lower.Contains("de thi") || lower.Contains("bai thi") || lower.Contains("question") || lower.Contains("quiz") || Regex.IsMatch(lower, @"^\d+\s*cau"))
            {
                // Trừ phi là danh mục văn bản hoặc tài liệu tham khảo
                if (!lower.Contains("van ban") && !lower.Contains("tai lieu") && !lower.Contains("huong dan") && !lower.Contains("danh muc"))
                {
                    return false;
                }
            }

            // 2. Bỏ qua các sheet văn bản, danh mục, tài liệu, báo cáo, hướng dẫn phi câu hỏi
            string[] skippablePatterns = new[]
            {
                "bia", "cover", "readme", "ghi chu", "note", "notes",
                "van ban", "vb", "danh muc", "dm van ban", "dm vb", "list van ban",
                "danh sach van ban", "tai lieu tham khao", "tai lieu", "reference", "references",
                "huong dan", "instruction", "instructions", "help", "guide",
                "thong ke", "bao cao", "summary", "report", "dashboard",
                "danh sach thi sinh", "ds thi sinh", "danh sach hoc vien", "ds hoc vien",
                "danh sach", "candidate", "candidates", "roster",
                "phan cong", "lich thi", "schedule", "quy dinh", "barem"
            };

            foreach (var pattern in skippablePatterns)
            {
                if (lower == pattern || lower.StartsWith(pattern + " ") || lower.EndsWith(" " + pattern) || lower.Contains(" " + pattern + " ")
                    || (pattern.Length >= 4 && lower.Contains(pattern)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidDataRow(string sttVal, string contentVal)
        {
            // Cho phép STT trống, số, hoặc có dạng: "1", "1.", "01", "Câu 1", "Q1", "1)"
            if (!string.IsNullOrWhiteSpace(sttVal))
            {
                var cleanStt = Regex.Replace(sttVal.Trim(), @"^(?:câu|cau|q|stt)\s*", "", RegexOptions.IgnoreCase).TrimEnd('.', ')');
                if (!Regex.IsMatch(cleanStt, @"^\d+$")) return false;
            }
            if (contentVal.Length < 4) return false;
            // Chỉ bỏ qua dòng phân đoạn ngắn không có dấu hiệu câu hỏi
            if (contentVal == contentVal.ToUpperInvariant() && contentVal.Length < 15) return false;
            return true;
        }

        private static bool HasAnswersInRow(IRow row, CognitiveColumnMap map)
        {
            if (map.Answer >= 0 && !string.IsNullOrWhiteSpace(GetCellValue(row, map.Answer))) return true;
            return map.OptionCols.Values.Any(c => !string.IsNullOrWhiteSpace(GetCellValue(row, c)));
        }

        private static bool HasInlineQuestionFormat(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return (text.Contains("\nA.") || text.Contains("\nB.") || text.Contains("\r\nA.") ||
                    Regex.IsMatch(text, @"(?:[A-D]\.\s+.*[A-D]\.\s+)", RegexOptions.Singleline) ||
                    text.Contains("Đáp án:") || text.Contains("ĐÁP ÁN:"));
        }

        private static void EnrichAndFinalizeDto(
            DocxImportPreviewDto dto, string topicCode, string subCategory,
            string contextTitle, string contextContent)
        {
            dto.TopicCode = topicCode;
            dto.SubCategory = string.IsNullOrWhiteSpace(dto.SubCategory) ? subCategory : dto.SubCategory;
            dto.Tags = GenerateSmartTags(topicCode, dto.SubCategory, dto.IsCritical, dto.Difficulty);

            if (!string.IsNullOrWhiteSpace(contextContent) && string.IsNullOrWhiteSpace(dto.ContextContent))
            {
                dto.ContextTitle = contextTitle;
                dto.ContextContent = contextContent;
            }
        }

        private static List<string> GenerateSmartTags(string topicCode, string? subCat, bool isCritical, int difficulty)
        {
            var tags = new List<string>
            {
                topicCode.ToLowerInvariant().Replace("_", "-"),
                $"bloom-level-{difficulty}"
            };

            if (!string.IsNullOrWhiteSpace(subCat))
            {
                tags.Add(subCat.ToLowerInvariant().Replace("_", "-"));
            }

            if (isCritical)
            {
                tags.Add("critical");
                tags.Add("fatal-fail");
            }

            return tags.Distinct().ToList();
        }

        private static string FindLongestCellInRow(IRow row)
        {
            string longest = string.Empty;
            for (int c = row.FirstCellNum; c < row.LastCellNum; c++)
            {
                var val = GetCellValue(row.GetCell(c)).Trim();
                if (val.Length > longest.Length)
                {
                    longest = val;
                }
            }
            return longest;
        }

        // ═════════════════════════════════════════════════════════════════════
        // 6. PAYLOAD BUILDER CHO TẤT CẢ 8 HÌNH THÁI TƯƠNG TÁC
        // ═════════════════════════════════════════════════════════════════════
        private static JsonDocument? BuildPayload(
            string qType,
            List<string> options,
            string answerRaw, string reference, DocxImportPreviewDto? dto = null)
        {
            object payloadObj;
            switch (qType)
            {
                case "SINGLE":
                {
                    var optList = BuildOptionList(options);
                    if (optList.Count < 2) return null;
                    var correctId = ResolveSingleAnswerId(answerRaw, options);
                    payloadObj = new
                    {
                        options = optList,
                        correctAnswer = correctId,
                        explanation = reference,
                        isCritical = dto?.IsCritical ?? false,
                        domainCode = dto?.DomainCode ?? "GENERAL",
                        tags = dto?.Tags ?? new List<string>(),
                        targetLevel = dto?.TargetLevel,
                        assessmentPurpose = dto?.AssessmentPurpose,
                        issuingOrg = dto?.IssuingOrg,
                        benchmarkYear = dto?.BenchmarkYear,
                        benchmarkStandard = dto?.BenchmarkStandard,
                        contextId = dto?.ContextId,
                        contextTitle = dto?.ContextTitle,
                        contextContent = dto?.ContextContent
                    };
                    break;
                }
                case "MULTI":
                {
                    var optList = BuildOptionList(options);
                    if (optList.Count < 2) return null;
                    var correctAnswers = answerRaw.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                                  .Select(s => ResolveSingleAnswerId(s.Trim(), options))
                                                  .Where(s => !string.IsNullOrWhiteSpace(s))
                                                  .Distinct()
                                                  .ToArray();
                    if (correctAnswers.Length == 0) correctAnswers = new[] { "1" };
                    payloadObj = new
                    {
                        options = optList,
                        correctAnswers,
                        explanation = reference,
                        domainCode = dto?.DomainCode ?? "GENERAL",
                        tags = dto?.Tags ?? new List<string>(),
                        targetLevel = dto?.TargetLevel,
                        assessmentPurpose = dto?.AssessmentPurpose,
                        issuingOrg = dto?.IssuingOrg,
                        benchmarkYear = dto?.BenchmarkYear,
                        benchmarkStandard = dto?.BenchmarkStandard,
                        contextId = dto?.ContextId,
                        contextTitle = dto?.ContextTitle,
                        contextContent = dto?.ContextContent
                    };
                    break;
                }
                case "TRUE_FALSE":
                {
                    var tfOptions = options.Count >= 2
                        ? BuildOptionList(options)
                        : new List<object> { new { id = "1", text = "ĐÚNG" }, new { id = "2", text = "SAI" } };
                    var correctId = answerRaw is "2" or "FALSE" or "F" or "SAI" or "S" ? "2" : "1";
                    payloadObj = new
                    {
                        options = tfOptions,
                        correctAnswer = correctId,
                        explanation = reference,
                        domainCode = dto?.DomainCode ?? "GENERAL",
                        tags = dto?.Tags ?? new List<string>(),
                        targetLevel = dto?.TargetLevel,
                        assessmentPurpose = dto?.AssessmentPurpose,
                        issuingOrg = dto?.IssuingOrg,
                        benchmarkYear = dto?.BenchmarkYear,
                        benchmarkStandard = dto?.BenchmarkStandard,
                        contextId = dto?.ContextId,
                        contextTitle = dto?.ContextTitle,
                        contextContent = dto?.ContextContent
                    };
                    break;
                }
                case "SHORT_ANSWER":
                case "FILL_BLANK":
                    payloadObj = new
                    {
                        correctAnswer = answerRaw,
                        explanation = reference,
                        domainCode = dto?.DomainCode ?? "GENERAL",
                        tags = dto?.Tags ?? new List<string>(),
                        targetLevel = dto?.TargetLevel,
                        assessmentPurpose = dto?.AssessmentPurpose,
                        issuingOrg = dto?.IssuingOrg,
                        benchmarkYear = dto?.BenchmarkYear,
                        benchmarkStandard = dto?.BenchmarkStandard,
                        contextId = dto?.ContextId,
                        contextTitle = dto?.ContextTitle,
                        contextContent = dto?.ContextContent
                    };
                    break;
                case "ORDERING":
                {
                    var optList = BuildOptionList(options);
                    var correctOrder = answerRaw.Split(new[] { ',', ';', '-', '>' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(x => x.Trim()).ToArray();
                    payloadObj = new
                    {
                        items = optList,
                        correctOrder,
                        explanation = reference,
                        domainCode = dto?.DomainCode ?? "GENERAL",
                        tags = dto?.Tags ?? new List<string>(),
                        targetLevel = dto?.TargetLevel,
                        assessmentPurpose = dto?.AssessmentPurpose,
                        issuingOrg = dto?.IssuingOrg,
                        benchmarkYear = dto?.BenchmarkYear,
                        benchmarkStandard = dto?.BenchmarkStandard,
                        contextId = dto?.ContextId,
                        contextTitle = dto?.ContextTitle,
                        contextContent = dto?.ContextContent
                    };
                    break;
                }
                case "MATCHING":
                {
                    var lefts = (options.Count > 0 ? options[0] : string.Empty).Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
                    var rights = (options.Count > 1 ? options[1] : string.Empty).Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
                    var leftItems = lefts.Select((t, i) => new { id = (i + 1).ToString(), text = t }).ToList();
                    var rightItems = rights.Select((t, i) => new { id = (i + 1).ToString(), text = t }).ToList();
                    payloadObj = new
                    {
                        leftItems,
                        rightItems,
                        rawPairs = answerRaw,
                        explanation = reference,
                        domainCode = dto?.DomainCode ?? "GENERAL",
                        tags = dto?.Tags ?? new List<string>(),
                        targetLevel = dto?.TargetLevel,
                        assessmentPurpose = dto?.AssessmentPurpose,
                        issuingOrg = dto?.IssuingOrg,
                        benchmarkYear = dto?.BenchmarkYear,
                        benchmarkStandard = dto?.BenchmarkStandard,
                        contextId = dto?.ContextId,
                        contextTitle = dto?.ContextTitle,
                        contextContent = dto?.ContextContent
                    };
                    break;
                }
                case "ESSAY":
                    payloadObj = new
                    {
                        rubric = reference,
                        maxScore = 10.0,
                        domainCode = dto?.DomainCode ?? "GENERAL",
                        tags = dto?.Tags ?? new List<string>(),
                        targetLevel = dto?.TargetLevel,
                        assessmentPurpose = dto?.AssessmentPurpose,
                        issuingOrg = dto?.IssuingOrg,
                        benchmarkYear = dto?.BenchmarkYear,
                        benchmarkStandard = dto?.BenchmarkStandard,
                        contextId = dto?.ContextId,
                        contextTitle = dto?.ContextTitle,
                        contextContent = dto?.ContextContent
                    };
                    break;
                default:
                    return null;
            }

            return JsonSerializer.SerializeToDocument(payloadObj);
        }

        // ═════════════════════════════════════════════════════════════════════
        // 7. UTILITIES & TEXT SANITIZATION (MAPREPL & UNICODE HEALING)
        // ═════════════════════════════════════════════════════════════════════
        private static string CleanText(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            // Khử rác Excel _x000D_, non-breaking space
            var t = input.Replace("_x000D_\n", "\n")
                         .Replace("_x000D_", "\n")
                         .Replace("\u00A0", " ")
                         .Replace("&nbsp;", " ");

            // Sửa lỗi dính chữ phương án phổ biến (ví dụ: A.Đúng -> A. Đúng)
            t = Regex.Replace(t, @"^([A-D0-9])\.([^\s])", "$1. $2");

            return Regex.Replace(t, @"[ \t]+", " ").Trim().Normalize(NormalizationForm.FormC);
        }

        private static string CleanAnswerToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return string.Empty;
            var t = token.Trim();
            if (t.StartsWith("(") && t.EndsWith(")")) t = t.Substring(1, t.Length - 2).Trim();
            if (t.StartsWith("[") && t.EndsWith("]")) t = t.Substring(1, t.Length - 2).Trim();
            t = Regex.Replace(t, @"[\.\:\)\,\-]+$", "").Trim();
            t = Regex.Replace(t, @"^[\(\[\.\:\-]+", "").Trim();
            return t;
        }

        private static string ResolveCorrectText(string raw, List<string> options)
        {
            var id = ResolveSingleAnswerId(raw, options);
            return int.TryParse(id, out var n) && n >= 1 && n <= options.Count ? options[n - 1] : raw;
        }

        private static List<object> BuildOptionList(List<string> options)
        {
            var list = new List<object>();
            for (var i = 0; i < options.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(options[i]))
                {
                    list.Add(new { id = (i + 1).ToString(), text = options[i] });
                }
            }
            return list;
        }

        private static IWorkbook OpenWorkbook(Stream stream)
        {
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            try { return new XSSFWorkbook(ms); }
            catch
            {
                ms.Position = 0;
                return new HSSFWorkbook(ms);
            }
        }

        private static string GetCellValue(ICell? cell)
        {
            if (cell == null) return string.Empty;
            try
            {
                return cell.CellType switch
                {
                    CellType.String => cell.StringCellValue,
                    CellType.Numeric => cell.NumericCellValue % 1 == 0
                                            ? ((long)cell.NumericCellValue).ToString()
                                            : cell.NumericCellValue.ToString(CultureInfo.InvariantCulture),
                    CellType.Boolean => cell.BooleanCellValue.ToString(),
                    CellType.Formula => cell.CachedFormulaResultType == CellType.String
                                            ? cell.StringCellValue
                                            : cell.NumericCellValue.ToString(CultureInfo.InvariantCulture),
                    _ => string.Empty
                };
            }
            catch { return string.Empty; }
        }

        private static string GetCellValue(IRow? row, int col)
        {
            if (row == null || col < 0) return string.Empty;
            return GetCellValue(row.GetCell(col));
        }

        private static string NormalizeHeader(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat == UnicodeCategory.NonSpacingMark) continue;
                var mapped = ch == 'đ' ? 'd' : ch;
                sb.Append(char.IsLetterOrDigit(mapped) ? mapped : ' ');
            }
            return Regex.Replace(sb.ToString().Normalize(NormalizationForm.FormC), @"\s+", " ").Trim();
        }

        private static bool Contains(string source, params string[] patterns)
            => patterns.Any(p => source.Contains(p, StringComparison.OrdinalIgnoreCase));

        private static string GenerateVietnameseSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "GENERAL";
            var normalized = name.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat == UnicodeCategory.NonSpacingMark) continue;
                var mapped = ch == 'đ' ? 'd' : ch;
                if (char.IsLetterOrDigit(mapped)) sb.Append(mapped);
                else if (mapped == ' ' || mapped == '-' || mapped == '_') sb.Append('_');
            }
            return Regex.Replace(sb.ToString().Normalize(NormalizationForm.FormC), @"_+", "_")
                        .Trim('_').ToUpperInvariant();
        }

        private static string DetectMediaType(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "text";
            var trimmed = text.Trim().ToLowerInvariant();
            if (trimmed.EndsWith(".png") || trimmed.EndsWith(".jpg") || trimmed.EndsWith(".jpeg") || trimmed.EndsWith(".webp") || trimmed.EndsWith(".gif"))
                return "image";
            if (trimmed.EndsWith(".mp3") || trimmed.EndsWith(".wav") || trimmed.EndsWith(".ogg") || trimmed.EndsWith(".m4a"))
                return "audio";
            if (trimmed.EndsWith(".mp4") || trimmed.EndsWith(".webm") || trimmed.EndsWith(".avi") || trimmed.EndsWith(".mov"))
                return "video";

            if (trimmed.StartsWith("http://") || trimmed.StartsWith("https://"))
            {
                if (trimmed.Contains("/images/") || trimmed.Contains(".jpg") || trimmed.Contains(".png") || trimmed.Contains(".jpeg") || trimmed.Contains(".webp"))
                    return "image";
                if (trimmed.Contains("/audios/") || trimmed.Contains(".mp3") || trimmed.Contains(".wav"))
                    return "audio";
                if (trimmed.Contains("/videos/") || trimmed.Contains(".mp4") || trimmed.Contains(".webm"))
                    return "video";
            }
            return "text";
        }

        private static string NormalizeFormatType(string raw, string fallback)
        {
            if (string.IsNullOrWhiteSpace(raw)) return fallback;
            var clean = NormalizeHeader(raw);
            if (Contains(clean, "anh", "hinh", "image", "pic")) return "image";
            if (Contains(clean, "am thanh", "nhac", "audio", "sound", "mp3")) return "audio";
            if (Contains(clean, "phim", "video", "clip", "mp4")) return "video";
            if (Contains(clean, "chu", "van ban", "text")) return "text";
            return fallback;
        }
    }
}
