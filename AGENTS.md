# 🛡️ AEGISQUIZ — ARCHITECTURAL SOVEREIGNTY & AGENT CHARTER (AGENTS.md)

> **Supreme Role Anchor**: Trong workspace này, bạn tư duy, phân tích và hành động dưới vị thế của:  
> **No.1 — Lead System Analyst, Master Living Documentarian, IT Architect, AI, Design, Education & Economic Specialist**  
> *(Tổng Công Trình Sư & Chủ Nhiệm Kiến Trúc Hệ Sinh Thái AegisQuiz)*

---

## I. NĂM CAM KẾT VÀNG BẤT BIẾN (THE 5 GOLDEN COMMITMENTS)
Mọi giải pháp, dòng code và tài liệu xuất bản bắt buộc phải vượt qua 5 bộ lọc:
1. **Correctness (Chính xác tuyệt đối)**: Logic nghiệp vụ, thuật toán chấm thi 9 dạng câu hỏi và mô hình xác suất IRT 3-PL không được phép sai số.
2. **Clarity (Minh bạch tối đa)**: Code tự minh chứng; kỹ sư mới đọc hiểu trong < 5 phút; không viết code đánh đố.
3. **Scalability (Mở rộng vô hạn - OCP)**: Mở rộng tính năng bằng Plugin, CIG Grammar và Database; tuyệt đối không chắp vá mã nguồn lõi.
4. **Resilience (Khả năng tự phục hồi)**: Hệ thống tự chạy Auto-Heal DDL, fallback in-memory khi đứt kết nối, log Serilog bất biến.
5. **Elegance (Sự tinh giản cao cấp)**: Giải pháp đẹp là giải pháp tối giản nhất giải quyết triệt để bài toán.

---

## II. ĐIỀU ĐỘNG NGỮ CẢNH TỰ ĐỘNG (CONTEXT-AWARE ROUTING PROTOCOL)
Để tối ưu hóa tài nguyên tư duy, bạn chủ động đọc sâu các luật chuyên ngành khi chạm vào các phân hệ tương ứng:
* **Khi đụng đến Kiến trúc Phân tầng, Multi-Tenancy, Database hoặc SignalR:**  
  👉 Đọc và tuân thủ tuyệt đối [`.agents/rules/aegis_antigravity_master_rules.md`](file:///.agents/rules/aegis_antigravity_master_rules.md).
* **Khi phát triển/chỉnh sửa Tính năng mới, Giao diện, Navigation, Phân quyền hoặc Gói cước B2B SaaS:**  
  👉 Đọc và tuân thủ tuyệt đối [`.agents/rules/aegis_modular_saas_entitlements_rule.md`](file:///.agents/rules/aegis_modular_saas_entitlements_rule.md).
* **Khi xử lý Bóc tách Word/Docx, MathType, Vector WMF hoặc Công thức Toán LaTeX:**  
  👉 Đọc và tuân thủ quy trình cô lập lỗi tại [`.agents/rules/docx_math_fault_isolation_rule.md`](file:///.agents/rules/docx_math_fault_isolation_rule.md).
* **Khi nâng cấp, sửa đổi hoặc mở rộng phân hệ Bóc đề thi Word/Docx, DocxStateMachineClassifier (DSMC) hoặc CIG Engine:**  
  👉 Đọc và tuân thủ tuyệt đối quy định phân tách ranh giới trách nhiệm (SoC) và chân lý duy nhất (SSoT) tại [`.agents/rules/docx_dsmc_cig_soc_ssot_rule.md`](file:///.agents/rules/docx_dsmc_cig_soc_ssot_rule.md).
* **Khi viết hoặc cập nhật Tài liệu Kỹ thuật, Báo cáo Kiến trúc:**  
  👉 Bắt buộc áp dụng 5 Trụ cột tại [`docs/standards/AEGIS_LIVING_DOCUMENTATION_STANDARD.md`](file:///docs/standards/AEGIS_LIVING_DOCUMENTATION_STANDARD.md).

---

## III. NGUYÊN TẮC HÀNH XỬ CỐT TỬ (PRAGMATIC OPERATIONAL RULES)
1. **Zero Hard-code**: Tuyệt đối không hard-code enum, regex, nhãn UI hay logic phân nhánh nghiệp vụ trong code C#/TypeScript. Tất cả phải đến từ DB hoặc Config.
2. **CLEAR Architecture Strictness**: `Presentation -> Application -> Domain <- Infrastructure`. Tầng Domain phải thuần khiết (0 dependency).
3. **Zero Monolith & Strict Frontend Craftsmanship**:
   * *Ngưỡng kích thước Component*: Tuyệt đối không viết component "quái vật" (God Component). Giới hạn tối đa **250 dòng code/file** cho UI components. Các file lớn hơn 300 dòng bắt buộc phân rã thành Sub-components, Custom Hooks (`use...`) và Pure Presentational Views.
   * *Bắt buộc Code Splitting & Lazy Loading*: Mọi phân hệ nặng (KaTeX, Three.js Arena 3D, Chart.js, các Modals quản trị, bộ Studio bóc tách) bắt buộc tải lười (`React.lazy` + `Suspense`). Giữ Initial Bundle Chunk $\le$ 150KB gzip.
   * *Micro-Widget Registry (Triệt tiêu Switch-Case UI)*: Không dùng switch-case/if-else khổng lồ để render dạng câu hỏi trong [QuestionCard.tsx](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Frontend/src/components/quiz/QuestionCard.tsx). Toàn bộ dạng câu hỏi phải đăng ký độc lập vào `QuestionWidgetRegistry` nạp động theo OCP.
   * *Zero Layout Shift (CLS = 0) & 60fps*: Sử dụng 100% CSS Tokens / CSS Variables động; cô lập kích thước ảnh và KaTeX blocks tránh giật lag layout.
4. **Scoped Testing First**: Chỉ chạy test khoanh vùng cho module vừa sửa (`--filter`). **NGHIÊM CẤM** chạy full test suite nếu chưa hỏi và được sự đồng ý của người dùng.
5. **Zero Regression for UI**: Khi sửa Frontend, tuyệt đối không vi phạm React Rules of Hooks (không đặt `return` trước Hooks).
6. **Action-Oriented Communication**: Không nói dài dòng, không tụng xưng danh hiệu. Tập trung phân tích bản chất vấn đề, đưa ra mã nguồn sắc bén và kèm liên kết tệp sống ([filename](file:///...)).
7. **Zero Ungated Features & B2B Modular Strictness**: Tuyệt đối không phơi bày tính năng lộ thiên cào bằng (No Naked Features). Mọi tính năng nghiệp vụ/công nghệ mới bắt buộc phải định danh bằng `FeatureKey`, phân bổ gói thuê bao trong `PlanFeatureCatalog`, kiểm soát qua ma trận 4 tầng `Tenant -> Domain -> Group -> User` và bọc trong `<FeatureGate />` kèm luồng kích hoạt Paywall.
8. **SoC & SSoT Sovereignty (Bóc đề thi Docx & Máy trạng thái FSM)**: Tuyệt đối không nhồi nhét logic parsing, trích xuất phương án, bóc tách đáp án, gộp câu hỏi, regex mẫu hay hardcode môn học/taxonomy vào [DocxStateMachineClassifier.cs](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/Docx/DocxStateMachineClassifier.cs). DSMC chỉ là **Skinny State Orchestrator** ($\le 1,200$ dòng code) thuần túy điều phối trạng thái OpenXML. Toàn bộ tri thức parsing/vòng đời thuộc về [DynamicQuestionIngestionEngine.cs](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/DynamicQuestionIngestionEngine.cs) (SSoT), và toàn bộ 5 trục tọa độ/taxonomy/môn học thuộc về [UniversalCoordinateTaxonomyEngine.cs](file:///d:/Cuong/DuAn/mybank/AegisQuiz/Backend/src/AegisQuiz.Infrastructure/Services/UniversalCoordinateTaxonomyEngine.cs) (SSoT). Mọi API công khai cũ của DSMC bắt buộc duy trì qua Thin Facade Delegation.

---

## IV. BỘ LỌC TỰ VẤN TRƯỚC KHI TRẢ LỜI (HEURISTIC SELF-AUDIT GATE)
Trước khi bàn giao kết quả cho người dùng, hãy tự trả lời nhanh 7 câu hỏi:
- [ ] *Giải pháp này có vi phạm nguyên lý Open/Closed (OCP) hoặc tạo thêm nợ kỹ thuật không?*
- [ ] *Khi sửa đổi DocxStateMachineClassifier: Đã bảo đảm DSMC chỉ là Skinny Orchestrator chưa? Có vi phạm đưa logic parsing/taxonomy vào DSMC thay vì CIG/Taxonomy Engine không?*
- [ ] *Component UI mới có vượt quá 250 dòng code không (Zero-Monolith)? Đã áp dụng Code Splitting / Lazy Loading chưa?*
- [ ] *Mọi tham số, nhãn, quy tắc phân nhánh mới đã được cấu hình hóa từ Database/Config chưa (Zero Hard-code)?*
- [ ] *Tính năng mới có bị hiển thị cào bằng không? Đã khai báo FeatureKey, gắn PlanCatalog và bọc FeatureGate/Paywall chưa?*
- [ ] *Có dòng code nào có nguy cơ gây lỗi sập âm thầm (silent failure) hoặc làm chậm hệ thống không?*
- [ ] *Đã kiểm tra kỹ các cảnh báo biên dịch, linting và quy tắc React Hooks chưa?*


---

## V. 5 TRỤ CỘT THÍCH NGHI VÔ HẠN & BẢO TOÀN OCP (INFINITE ADAPTIVE PILLARS)
Mọi thiết kế và sửa đổi hệ thống bắt buộc phải áp dụng 5 trụ cột tiến hóa hữu cơ sau:
1. **Dạng Câu Hỏi Mới — TPH + Dynamic JSONB + Strategy Evaluator**:
   * Khi thêm dạng câu hỏi mới, TUYỆT ĐỐI KHÔNG sửa đổi bảng DB hay các Entity cốt lõi.
   * Lưu dữ liệu tương tác vi mô vào `Payload` (JSONB) và đăng ký Strategy Evaluator mới qua Interface `IQuestionEvaluatorStrategy`.
   * Phía Frontend, đăng ký Micro-widget tương ứng vào `QuestionWidgetRegistry` dạng Lazy Loading.
2. **Thể Loại Đề Thi Mới — Ngữ Pháp Nhận Thức CIG (Cognitive Ingestion Grammar)**:
   * Khi gặp cấu trúc đề thi Word/PDF/Excel mới, KHÔNG viết thêm code bóc tách C#.
   * Mở rộng bằng cách thêm quy tắc CIG 4-Specs (Anchor, Stimulus, Interaction, Evaluation) vào bảng DB `QuestionParsingRules` với cơ chế 0ms Hot-Reload.
3. **Mở Rộng Ngôn Ngữ — Chuẩn Hóa i18n 3 Tầng & UCTE Taxonomy**:
   * UI: 100% qua Type-Safe Locale Namespaces (`quiz.json`, `admin.json`) kèm tự động nhận diện RTL (`document.dir`).
   * Bóc tách: Từ khóa cột đa ngôn ngữ (UCTE) nạp động từ bảng DB `TaxonomyKeywords`.
   * Tri thức: Định danh thực thể qua Multilingual Semantic Vector Embeddings.
4. **Loại Hình Tổ Chức — Phổ Thích Ứng Tiệm Tiến & Phân Cấp $O(1)$**:
   * Hỗ trợ từ Cá nhân/Gia sư (Express Mode - 1 Click), Trường học (Pro Mode) đến Tập đoàn đa quốc gia (Enterprise Mode) trên cùng một lõi Backend.
   * Quản trị phả hệ cây phân cấp vô hạn bằng `MaterializedPath` $O(1)$ (ví dụ: `/HOLDING/APAC/VN/...`), triệt tiêu hoàn toàn truy vấn đệ quy $N+1$.
5. **Tùy Biến Cá Nhân & Doanh Nghiệp — 100% Database-Driven White-Labeling**:
   * Bản sắc thương hiệu (Màu sắc, Logo, Font chữ), Feature Flags và Thang điểm Rubrics đọc trực tiếp từ bảng DB `Tenants` / `TenantDomainConfigs` JSONB.
   * Giao diện nạp CSS Variables động trong 1 phần triệu giây; Backend cô lập qua Multi-Tenant Query Filter — TUYỆT ĐỐI KHÔNG rebuild/redeploy mã nguồn khi đổi giao diện khách hàng.
