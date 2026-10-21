# 🛡️ QUY CHUẨN KIẾN TRÚC BẮT BUỘC: PHÂN TÁCH RANH GIỚI TRÁCH NHIỆM (SoC) & CHÂN LÝ DUY NHẤT (SSoT) GIỮA DSMC VÀ CIG ENGINE

> **Mã quy chuẩn**: `AEGIS-RULE-INGEST-SOC-SSOT-2026`  
> **Cấp độ pháp lý**: Tối cao — Bắt buộc tuân thủ 100% không ngoại lệ (Zero-Tolerance Architectural Directive)  
> **Phạm vi áp dụng**: Mọi tác vụ đọc, sửa, thêm mới, bảo trì liên quan đến [DocxStateMachineClassifier.cs](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/Docx/DocxStateMachineClassifier.cs), [DynamicQuestionIngestionEngine.cs](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/DynamicQuestionIngestionEngine.cs) và [UniversalCoordinateTaxonomyEngine.cs](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/UniversalCoordinateTaxonomyEngine.cs).  
> **Tác giả phê chuẩn**: No.1 — Tổng Công Trình Sư & Chủ Nhiệm Kiến Trúc Hệ Sinh Thái AegisQuiz.

---

## I. MÔ HÌNH RANH GIỚI TRÁCH NHIỆM TAM GIÁC VÀNG (THE GOLDEN TRIANGLE)

Hệ thống bóc tách đề thi AegisQuiz tuân thủ nghiêm ngặt mô hình phân tách ranh giới 3 phân tầng. Tuyệt đối không để rò rỉ logic giữa các tầng:

```
                  ┌─────────────────────────────────────────────────────────┐
                  │                 TÀI LIỆU OPENXML .DOCX                  │
                  └────────────────────────────┬────────────────────────────┘
                                               │
                                               ▼
         ┌───────────────────────────────────────────────────────────────────────┐
         │       TẦNG 1: SKINNY STATE ORCHESTRATOR (Bộ điều phối trạng thái)     │
         │                  [DocxStateMachineClassifier.cs]                      │
         │  - DUY NHẤT: Quản trị vòng lặp các đoạn văn bản (paragraphs).         │
         │  - DUY NHẤT: Quản lý biến trạng thái (inExplanation, isSolutionPhase, │
         │              currentSection, currentExamCode, scopeWindow).           │
         │  - TUYỆT ĐỐI KHÔNG: Chứa logic bóc tách, regex mẫu, hay taxonomy.     │
         └──────────────────┬─────────────────────────────────┬──────────────────┘
                            │                                 │
         (Bóc tách, Ngữ pháp, Vòng đời)            (Hệ tọa độ, Môn học, Thẻ)
                            ▼                                 ▼
┌───────────────────────────────────────┐ ┌───────────────────────────────────────┐
│ TẦNG 2: CIG RECOGNITION ENGINE (SSoT) │ │ TẦNG 3: COGNITIVE TAXONOMY (SSoT)     │
│  [DynamicQuestionIngestionEngine.cs]  │ │ [UniversalCoordinateTaxonomyEngine.cs]│
│ - Structure.cs: Flatten & Tiêu đề     │ │ - SniffCoordinates: 5 Trục Tọa Độ     │
│ - Extraction.cs: Bóc tách Bottom-Up,  │ │ - Thẻ thông minh (Auto Tags)          │
│   Phương án A/B/C/D, Đáp án, Lời giải │ │ - Topic Inference: Suy luận môn học   │
│ - FinalizeQuestion & Lifecycle Merge  │ │ - EnrichExamTaxonomy & Dominant Topic │
│ - Language.cs: Ngôn ngữ & Grammar     │ │   Propagation (Lan tỏa chủ đề >= 40%) │
└───────────────────────────────────────┘ └───────────────────────────────────────┘
```

---

## II. NĂM ĐIỀU CẤM KỴ TUYỆT ĐỐI (THE 5 NEGATIVE DIRECTIVES)

Khi can thiệp vào mã nguồn bóc tách đề thi, bất kỳ kỹ sư hoặc AI Assistant nào vi phạm 1 trong 5 điều sau đều bị coi là **hủy hoại kiến trúc (Architectural Vandalism)**:

1. **CẤM VIẾT LOGIC PARSING VÀO DSMC:**
   Tuyệt đối **KHÔNG** viết thêm code bóc tách phương án, trích xuất đáp án inline, nối lời giải vi mô hay nhận diện header trong `DocxStateMachineClassifier`. Mọi logic phân tích nội dung phải viết tại `DynamicQuestionIngestionEngine` (các partials: `.Structure.cs`, `.Extraction.cs`, `.Language.cs`).

2. **CẤM HARDCODE TAXONOMY HOẶC MÔN HỌC TRONG DSMC:**
   Tuyệt đối **KHÔNG** viết các switch-case / if-else gán mã môn học, tên môn học (ví dụ: `"MATH" => "Toán học"`, `"TOAN"`, `"HOA_HOC"`) trong `DocxStateMachineClassifier`. Toàn bộ tri thức về môn học, 5 trục tọa độ và lan tỏa chủ đề đề thi bắt buộc phải nằm tại `UniversalCoordinateTaxonomyEngine`.

3. **CẤM TỰ TẠO VÒNG ĐỜI GỘP CÂU HỎI TẠI DSMC:**
   Tuyệt đối **KHÔNG** tự ý can thiệp logic phân định trùng lặp câu hỏi, so khớp ĐỀ_PHẦN_CÂU hoặc gộp lời giải trong DSMC. Toàn bộ lifecycle phải gọi qua `cig.FinalizeAndMergeQuestion(...)`.

4. **CẤM PHÁ VỠ CHỮ KÝ PUBLIC API CỦA DSMC (ZERO BREAKING CHANGE):**
   Mọi phương thức `public static` hiện có trên `DocxStateMachineClassifier` (như `ResolveQuestionBlockBottomUp`, `ExtractUppercaseOptionsFromLine`, `FinalizeQuestion`, `InferTopicFromContent`, v.v.) bắt buộc phải được giữ nguyên chữ ký và đóng vai trò **Facade mỏng (Thin Delegation)** ủy quyền sang `DynamicQuestionIngestionEngine` hoặc `UniversalCoordinateTaxonomyEngine`. Không được phép gây vỡ bất kỳ unit test, controller hay client nào đang gọi.

5. **CẤM VƯỢT NGƯỠNG DUNG LƯỢNG MÃ NGUỒN CỦA DSMC:**
   [DocxStateMachineClassifier.cs](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/Docx/DocxStateMachineClassifier.cs) sau cuộc cải tổ v4.0 được giới hạn tối đa **$\le 1,200$ dòng code**. Mọi hành vi làm phình to file này bằng việc nhồi nhét logic nghiệp vụ đều vi phạm quy chuẩn Skinny Orchestrator.

---

## III. BẢN ĐỒ PHÂN BỔ CHÂN LÝ DUY NHẤT (SSOT ALLOCATION CATALOG)

Trước khi viết bất kỳ dòng mã nào, hãy tra cứu bảng phân bổ sau để đặt code đúng vị trí:

| Loại Nghiệp vụ / Thuật toán | Vị trí Độc tôn (SSoT File) | Ghi chú & Trách nhiệm |
|---|---|---|
| **Làm phẳng OpenXML & Header** | [DynamicQuestionIngestionEngine.Structure.cs](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/DynamicQuestionIngestionEngine.Structure.cs) | `FlattenDocument`, `ProfileHeader`, `TryExtractExamCode`, `TryExtractDurationMinutes`, `SanitizeSubjectHeading`, `BuildExamCode`. |
| **Quét ngược Bottom-Up** | [DynamicQuestionIngestionEngine.Extraction.cs](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/DynamicQuestionIngestionEngine.Extraction.cs) | `ResolveQuestionBlockBottomUp`, `TryExtractOptionsBottomUp`, `TryExtractUppercaseMultilineOptions`, `TryExtractTrueFalseOptions`. |
| **Bóc tách Phương án & Đáp án** | [DynamicQuestionIngestionEngine.Extraction.cs](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/DynamicQuestionIngestionEngine.Extraction.cs) | `ExtractUppercaseOptionsFromLine`, `ExtractOptionsFromLine`, `TryExtractAnswer`, `NormalizeSectionKey`. |
| **Nối Lời giải & Ý con (SubItems)** | [DynamicQuestionIngestionEngine.Extraction.cs](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/DynamicQuestionIngestionEngine.Extraction.cs) | `AppendExplanationLine`, `UpdateTrueFalseAnswer`, `AppendSubItemExplanation`. |
| **Vòng đời & Gộp Câu hỏi (Merge)** | [DynamicQuestionIngestionEngine.Extraction.cs](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/DynamicQuestionIngestionEngine.Extraction.cs) | `FinalizeQuestion`, `FinalizeAndMergeQuestion`, `FindQuestionByHierarchy`, `GetEquivalentSections`. |
| **5 Trục Tọa độ & Thẻ thông minh** | [UniversalCoordinateTaxonomyEngine.cs](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/UniversalCoordinateTaxonomyEngine.cs) | `SniffCoordinates`, `AutoEnrichTaxonomyAndTags`, `EnrichExamTaxonomy`. |
| **Suy luận Môn học & Lan tỏa Đề thi** | [UniversalCoordinateTaxonomyEngine.cs](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/UniversalCoordinateTaxonomyEngine.cs) | `InferTopicFromContent`, `PropagateDominantTopic` ($\ge 40\%$). |
| **Điều phối vòng lặp trạng thái FSM** | [DocxStateMachineClassifier.cs](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/Docx/DocxStateMachineClassifier.cs) | `ParseParagraphList`: Duyệt đoạn văn, chuyển đổi cờ trạng thái, gọi CIG Engine và Taxonomy Engine. |

---

## IV. BỘ LỌC KIỂM THỬ BẮT BUỘC TRƯỚC KHI BÀN GIAO (MANDATORY VERIFICATION GATE)

Khi thực hiện bất kỳ nâng cấp nào liên quan đến bóc đề hoặc classifier, bắt buộc chạy kiểm thử khoanh vùng bằng lệnh:

```bash
dotnet test --filter "FullyQualifiedName~DocxClassifierRegistryComplianceTests|FullyQualifiedName~DocxStateMachineClassifierArchitectureTests|FullyQualifiedName~UniversalCigCoverageTests|FullyQualifiedName~DeToanParserTests|FullyQualifiedName~MultiTrueFalseIsolationTests|FullyQualifiedName~TrackQuestionCreationTests"
```

**Tiêu chí thông qua:**
- **100% Tests PASS (Tối thiểu 64/64 tests).**
- Thời gian thực thi $\le 10$ giây.
- Không phát sinh cảnh báo biên dịch CS0111 (Duplicate Member) hoặc CS0246 (Missing Namespace).
