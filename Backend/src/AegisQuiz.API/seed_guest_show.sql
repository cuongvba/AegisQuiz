-- ==============================================================================
-- AEGISQUIZ — SEED DATA: GUEST_SHOW CHỦ ĐỀ TRẢI NGHIỆM ĐỈNH CAO (ALL QUESTION TYPES)
-- ==============================================================================

-- 1. XÓA BẢN GHI CŨ NẾU CÓ
DELETE FROM "Questions" WHERE "CategoryCode" = 'GUEST_SHOW';
DELETE FROM "BankTopics" WHERE "Code" = 'GUEST_SHOW';
DELETE FROM "QuestionContexts" WHERE "Id" = 'a0000000-0000-0000-0000-000000000001'::uuid;

-- 2. TẠO CHỦ ĐỀ GUEST_SHOW TRONG BANKTOPICS
INSERT INTO "BankTopics" (
    "Id", "TenantId", "Code", "Name", "Description",
    "CategoryCode", "DomainCode", "Scope", "VisibilityScope",
    "DepthLevel", "DisplayOrder", "QuestionCountCached", "Enabled",
    "MaterializedPath", "AllowedTenantIds", "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'c8aeae8d-daa2-4c78-9fa1-dd8d08ab12c4'::uuid,
    'GUEST_SHOW',
    '🌟 GuestShow — Đấu Trường Trải Nghiệm Đỉnh Cao',
    'Chuyên đề tinh hoa phô diễn toàn diện sức mạnh AegisQuiz: 7 dạng câu hỏi, KaTeX vi tích phân, ảnh minh họa Retina, ngữ cảnh đọc hiểu liên kết (Question Context) và lời giải chi tiết thông minh.',
    'GUEST_SHOW',
    'GENERAL',
    'COMMUNITY',
    'PUBLIC',
    1,
    0,
    9,
    true,
    '/GUEST_SHOW/',
    ARRAY[]::text[],
    NOW(),
    NOW()
);

-- 3. TẠO QUESTION CONTEXT (DÙNG CHUNG CHO CÂU 3 & CÂU 4)
INSERT INTO "QuestionContexts" (
    "Id", "TenantId", "Title", "Content", "ContentType",
    "ContentHash", "CreatedAt", "UpdatedAt"
) VALUES (
    'a0000000-0000-0000-0000-000000000001'::uuid,
    'c8aeae8d-daa2-4c78-9fa1-dd8d08ab12c4'::uuid,
    'Case Study: Kiến Trúc Bảo Mật Zero-Trust & Chuyển Đổi Số Ngân Hàng AegisBank',
    'Ngân hàng Quốc tế AegisBank đang triển khai chuyển đổi số toàn diện hệ thống Core Banking sang kiến trúc Microservices phân tán trên nền tảng Hybrid Multi-Cloud. Hệ thống xử lý trung bình $120.000$ giao dịch/giây (TPS) trong các khung giờ cao điểm. Để bảo đảm an ninh mạng theo chuẩn **PCI-DSS v4.0** và tiêu chuẩn **ISO/IEC 27001**, kiến trúc sư trưởng chỉ đạo áp dụng nguyên lý **Zero-Trust Network Access (ZTNA)** với phương châm *"Never Trust, Always Verify"*. Toàn bộ các dịch vụ nội bộ bắt buộc phải xác thực song phương qua giao thức **mTLS (Mutual TLS)** sử dụng cặp khóa mật mã đường cong elliptic **ECDSA P-256**, kết hợp mã hóa luồng dữ liệu AES-256-GCM ở trạng thái lưu trữ (Data-at-Rest) và truyền tải (Data-in-Transit).',
    'markdown',
    'hash_guestshow_context_01',
    NOW(),
    NOW()
);

-- 4. CÂU 1: SINGLE CHOICE + CÔNG THỨC TOÁN HỌC PHỨC TẠP KaTeX
INSERT INTO "Questions" (
    "Id", "TenantId", "QuestionType", "CategoryCode", "DomainCode",
    "Content", "Options", "CorrectOption", "Difficulty", "DurationSeconds",
    "IsPublic", "IsCritical", "Payload", "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'c8aeae8d-daa2-4c78-9fa1-dd8d08ab12c4'::uuid,
    'SINGLE',
    'GUEST_SHOW',
    'GENERAL',
    'Trong không gian $Oxyz$ và giải tích nâng cao, cho ma trận hiệp phương sai của hệ thống tín hiệu $A = \begin{pmatrix} 2 & 1 \\ 1 & 3 \end{pmatrix}$. Hãy tính giá trị của tích phân suy rộng sau đây biểu diễn năng lượng hội tụ xung lượng: $$I = \int_{0}^{\pi} e^{2x} \sin(3x) \, dx$$',
    ARRAY[
        '$I = \frac{3(e^{2\pi} + 1)}{13}$',
        '$I = \frac{2(e^{2\pi} - 1)}{13}$',
        '$I = \frac{3(e^{2\pi} - 1)}{13}$',
        '$I = \frac{e^{2\pi} + 3}{13}$'
    ],
    'A',
    4,
    90,
    true,
    false,
    jsonb_build_object(
        'correctAnswer', 'A',
        'explanation', 'Áp dụng phương pháp tích phân từng phần 2 lần với $u = \sin(3x)$ và $dv = e^{2x}dx$:' || chr(10) ||
                       '- Lần 1: $u = \sin(3x) \Rightarrow du = 3\cos(3x)dx$, $v = \frac{1}{2}e^{2x}$. Ta có: $I = \left[ \frac{1}{2}e^{2x}\sin(3x) \right]_{0}^{\pi} - \frac{3}{2}\int_{0}^{\pi} e^{2x}\cos(3x)dx = 0 - \frac{3}{2} J$.' || chr(10) ||
                       '- Lần 2: Đặt $J = \int_{0}^{\pi} e^{2x}\cos(3x)dx \Rightarrow J = \left[ \frac{1}{2}e^{2x}\cos(3x) \right]_{0}^{\pi} + \frac{3}{2}I = -\frac{1}{2}(e^{2\pi} + 1) + \frac{3}{2}I$.' || chr(10) ||
                       '- Thay vào hệ thức ta được: $I = -\frac{3}{2} \left[ -\frac{1}{2}(e^{2\pi} + 1) + \frac{3}{2}I \right] \Leftrightarrow \frac{13}{4}I = \frac{3}{4}(e^{2\pi} + 1) \Leftrightarrow I = \frac{3(e^{2\pi} + 1)}{13}$.' || chr(10) ||
                       'Do đó phương án chính xác là **A**.'
    ),
    NOW(),
    NOW()
);

-- 5. CÂU 2: SINGLE CHOICE + HÌNH ẢNH MINH HỌA RETINA (BIỂN BÁO GIAO THÔNG)
INSERT INTO "Questions" (
    "Id", "TenantId", "QuestionType", "CategoryCode", "DomainCode",
    "Content", "Options", "CorrectOption", "Difficulty", "DurationSeconds",
    "IsPublic", "IsCritical", "Payload", "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'c8aeae8d-daa2-4c78-9fa1-dd8d08ab12c4'::uuid,
    'SINGLE',
    'GUEST_SHOW',
    'GENERAL',
    'Quan sát biển báo giao thông chuẩn quy chuẩn Quốc gia QCVN 41:2019/BGTVT trong hình ảnh minh họa dưới đây. Biển báo này có ý nghĩa hiệu lực như thế nào đối với người điều khiển phương tiện?',
    ARRAY[
        'Biển báo P.102: "Cấm đi ngược chiều" — Cấm tất cả các loại xe đi vào theo chiều đặt biển, trừ các xe được ưu tiên theo luật định.',
        'Biển báo P.101: "Đường cấm" — Cấm tất cả các loại phương tiện cơ giới và thô sơ đi lại cả hai hướng.',
        'Biển báo P.123a: "Cấm rẽ trái" đối với tất cả các loại phương tiện cơ giới đường bộ.',
        'Biển báo P.130: "Cấm dừng xe và đỗ xe" trên đoạn đường quy định.'
    ],
    'A',
    2,
    45,
    true,
    true,
    jsonb_build_object(
        'correctAnswer', 'A',
        'imageUrl', 'data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 240 240" width="240" height="240"><circle cx="120" cy="120" r="110" fill="%23dc2626" stroke="%23ffffff" stroke-width="8"/><rect x="36" y="98" width="168" height="44" rx="8" fill="%23ffffff"/></svg>',
        'explanation', 'Đây là biển báo số **P.102: Cấm đi ngược chiều**.' || chr(10) ||
                       '- **Ý nghĩa**: Báo đường cấm tất cả các loại xe (cơ giới và thô sơ) đi vào theo chiều đặt biển, trừ các xe được ưu tiên theo quy định của pháp luật giao thông đường bộ.' || chr(10) ||
                       '- **Căn cứ pháp lý**: Quy chuẩn kỹ thuật Quốc gia về báo hiệu đường bộ QCVN 41:2019/BGTVT.' || chr(10) ||
                       '- **Mẹo ghi nhớ**: Nền đỏ hình tròn có một vạch trắng nằm ngang ở giữa luôn là tín hiệu Cấm đi ngược chiều.'
    ),
    NOW(),
    NOW()
);

-- 6. CÂU 3: LIÊN KẾT QUESTION CONTEXT (CASE STUDY ZERO-TRUST) - LOẠI SINGLE
INSERT INTO "Questions" (
    "Id", "TenantId", "QuestionType", "CategoryCode", "DomainCode", "ContextId",
    "Content", "Options", "CorrectOption", "Difficulty", "DurationSeconds",
    "IsPublic", "IsCritical", "Payload", "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'c8aeae8d-daa2-4c78-9fa1-dd8d08ab12c4'::uuid,
    'SINGLE',
    'GUEST_SHOW',
    'GENERAL',
    'a0000000-0000-0000-0000-000000000001'::uuid,
    'Dựa vào ngữ cảnh kiến trúc hệ thống trên, chuẩn mực quốc tế nào được AegisBank áp dụng làm căn cứ cốt lõi để bảo mật dữ liệu thẻ thanh toán và toàn bộ luồng giao dịch tài chính?',
    ARRAY[
        'PCI-DSS v4.0 (Payment Card Industry Data Security Standard)',
        'CMMI Level 5 (Capability Maturity Model Integration)',
        'ITIL v3 Foundation Framework',
        'TOGAF 9.2 Enterprise Architecture Framework'
    ],
    'A',
    3,
    60,
    true,
    false,
    jsonb_build_object(
        'correctAnswer', 'A',
        'contextTitle', 'Case Study: Kiến Trúc Bảo Mật Zero-Trust & Chuyển Đổi Số Ngân Hàng AegisBank',
        'contextContent', 'Ngân hàng Quốc tế AegisBank đang triển khai chuyển đổi số toàn diện hệ thống Core Banking sang kiến trúc Microservices phân tán trên nền tảng Hybrid Multi-Cloud. Hệ thống xử lý trung bình $120.000$ giao dịch/giây (TPS) trong các khung giờ cao điểm. Để bảo đảm an ninh mạng theo chuẩn **PCI-DSS v4.0** và tiêu chuẩn **ISO/IEC 27001**, kiến trúc sư trưởng chỉ đạo áp dụng nguyên lý **Zero-Trust Network Access (ZTNA)** với phương châm *"Never Trust, Always Verify"*. Toàn bộ các dịch vụ nội bộ bắt buộc phải xác thực song phương qua giao thức **mTLS (Mutual TLS)** sử dụng cặp khóa mật mã đường cong elliptic **ECDSA P-256**, kết hợp mã hóa luồng dữ liệu AES-256-GCM ở trạng thái lưu trữ (Data-at-Rest) và truyền tải (Data-in-Transit).',
        'explanation', 'Theo ngữ cảnh đã nêu rõ: "Để bảo đảm an ninh mạng theo chuẩn PCI-DSS v4.0 và tiêu chuẩn ISO/IEC 27001...". PCI-DSS v4.0 là tiêu chuẩn quốc tế bắt buộc ngặt nghèo nhất dành cho các tổ chức xử lý, lưu trữ và truyền tải dữ liệu thẻ ngân hàng.'
    ),
    NOW(),
    NOW()
);

-- 7. CÂU 4: LIÊN KẾT QUESTION CONTEXT - LOẠI SHORT_ANSWER (ĐIỀN KHUYẾT / TRẢ LỜI NGẮN)
INSERT INTO "Questions" (
    "Id", "TenantId", "QuestionType", "CategoryCode", "DomainCode", "ContextId",
    "Content", "Options", "CorrectOption", "Difficulty", "DurationSeconds",
    "IsPublic", "IsCritical", "Payload", "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'c8aeae8d-daa2-4c78-9fa1-dd8d08ab12c4'::uuid,
    'SHORT_ANSWER',
    'GUEST_SHOW',
    'GENERAL',
    'a0000000-0000-0000-0000-000000000001'::uuid,
    'Từ nội dung case study trên, chuẩn thuật toán mật mã đường cong elliptic nào được chỉ định để xác thực song phương mTLS giữa các microservices? (Gợi ý: Nhập chính xác tên thuật toán, ví dụ: ECDSA P-256)',
    ARRAY[]::text[],
    'ECDSA P-256',
    3,
    60,
    true,
    false,
    jsonb_build_object(
        'correctAnswer', 'ECDSA P-256',
        'contextTitle', 'Case Study: Kiến Trúc Bảo Mật Zero-Trust & Chuyển Đổi Số Ngân Hàng AegisBank',
        'contextContent', 'Ngân hàng Quốc tế AegisBank đang triển khai chuyển đổi số toàn diện hệ thống Core Banking sang kiến trúc Microservices phân tán trên nền tảng Hybrid Multi-Cloud. Hệ thống xử lý trung bình $120.000$ giao dịch/giây (TPS) trong các khung giờ cao điểm. Để bảo đảm an ninh mạng theo chuẩn **PCI-DSS v4.0** và tiêu chuẩn **ISO/IEC 27001**, kiến trúc sư trưởng chỉ đạo áp dụng nguyên lý **Zero-Trust Network Access (ZTNA)** với phương châm *"Never Trust, Always Verify"*. Toàn bộ các dịch vụ nội bộ bắt buộc phải xác thực song phương qua giao thức **mTLS (Mutual TLS)** sử dụng cặp khóa mật mã đường cong elliptic **ECDSA P-256**, kết hợp mã hóa luồng dữ liệu AES-256-GCM ở trạng thái lưu trữ (Data-at-Rest) và truyền tải (Data-in-Transit).',
        'explanation', 'Trong bối cảnh bài toán: "...sử dụng cặp khóa mật mã đường cong elliptic ECDSA P-256...". ECDSA P-256 (hoặc secp256r1) đem lại tốc độ bắt tay TLS cao hơn 4-6 lần và kích thước chữ ký nhỏ hơn đáng kể so với RSA-4096.'
    ),
    NOW(),
    NOW()
);

-- 8. CÂU 5: DẠNG MULTI (CHỌN NHIỀU ĐÁP ÁN ĐÚNG - CHECKBOX)
INSERT INTO "Questions" (
    "Id", "TenantId", "QuestionType", "CategoryCode", "DomainCode",
    "Content", "Options", "CorrectOption", "Difficulty", "DurationSeconds",
    "IsPublic", "IsCritical", "Payload", "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'c8aeae8d-daa2-4c78-9fa1-dd8d08ab12c4'::uuid,
    'MULTI',
    'GUEST_SHOW',
    'GENERAL',
    'Theo khuyến nghị an toàn thông tin OWASP Top 10 và nguyên lý Defense-in-Depth, những biện pháp kỹ thuật nào sau đây là giải pháp CHÍNH XÁC giúp phòng chống triệt để lỗ hổng SQL Injection? (Chọn tất cả các phương án đúng)',
    ARRAY[
        'Sử dụng Parameterized Queries (Prepared Statements) hoặc Object-Relational Mapping (ORM) chuẩn hóa.',
        'Thực thi Stored Procedures có tham số hóa toàn bộ dữ liệu đầu vào.',
        'Áp dụng nguyên tắc đặc quyền tối thiểu (Principle of Least Privilege) cho tài khoản kết nối Database.',
        'Nối chuỗi trực tiếp ký tự do người dùng nhập vào câu lệnh SQL SELECT/INSERT để tăng tốc độ truy vấn.'
    ],
    'A,B,C',
    3,
    60,
    true,
    false,
    jsonb_build_object(
        'correctAnswers', jsonb_build_array('A', 'B', 'C'),
        'options', jsonb_build_array(
            jsonb_build_object('id', 'A', 'text', 'Sử dụng Parameterized Queries (Prepared Statements) hoặc ORM chuẩn hóa.'),
            jsonb_build_object('id', 'B', 'text', 'Thực thi Stored Procedures có tham số hóa toàn bộ dữ liệu đầu vào.'),
            jsonb_build_object('id', 'C', 'text', 'Áp dụng nguyên tắc đặc quyền tối thiểu cho tài khoản kết nối Database.'),
            jsonb_build_object('id', 'D', 'text', 'Nối chuỗi trực tiếp ký tự do người dùng nhập vào câu lệnh SQL.')
        ),
        'explanation', 'Các biện pháp phòng thủ vững chắc bao gồm A, B và C:' || chr(10) ||
                       '- **A & B**: Tách biệt hoàn toàn mã SQL và dữ liệu nhập, ngăn chặn engine hiểu lầm dữ liệu là lệnh thực thi.' || chr(10) ||
                       '- **C**: Hạn chế phạm vi tấn công nếu lỡ có điểm rò rỉ mã.' || chr(10) ||
                       '- **D**: Là nguyên nhân cốt tử trực tiếp tạo ra lỗ hổng SQL Injection nghiêm trọng!'
    ),
    NOW(),
    NOW()
);

-- 9. CÂU 6: DẠNG MULTI_TRUE_FALSE (ĐÚNG/SAI 4 MỆNH ĐỀ — CHUẨN GDPT 2025 BỘ GIÁO DỤC)
INSERT INTO "Questions" (
    "Id", "TenantId", "QuestionType", "CategoryCode", "DomainCode",
    "Content", "Options", "CorrectOption", "Difficulty", "DurationSeconds",
    "IsPublic", "IsCritical", "Payload", "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'c8aeae8d-daa2-4c78-9fa1-dd8d08ab12c4'::uuid,
    'MULTI_TRUE_FALSE',
    'GUEST_SHOW',
    'GENERAL',
    'Cho hàm số bậc ba $y = f(x) = x^3 - 3x^2 + 2$ có đồ thị $(C)$. Xét tính Đúng hay Sai của các mệnh đề sau đây theo định dạng đề thi mới tốt nghiệp THPT 2025:',
    ARRAY[
        'a) Tập xác định của hàm số là $D = \mathbb{R}$.',
        'b) Đạo hàm của hàm số là $f''(x) = 3x^2 - 6x$ và có hai nghiệm phân biệt $x_1 = 0, x_2 = 2$.',
        'c) Hàm số đạt cực đại tại điểm $x = 2$ với giá trị cực đại $y_{\text{CĐ}} = -2$.',
        'd) Điểm uốn của đồ thị $(C)$ là điểm $I(1; 0)$, đồng thời $I$ là tâm đối xứng của đồ thị.'
    ],
    'T,T,F,T',
    4,
    120,
    true,
    false,
    jsonb_build_object(
        'correctAnswers', jsonb_build_object('a', true, 'b', true, 'c', false, 'd', true),
        'answerRaw', 'a:T,b:T,c:F,d:T',
        'subQuestions', jsonb_build_array(
            jsonb_build_object('id', 'a', 'text', 'a) Tập xác định của hàm số là D = R', 'isCorrect', true),
            jsonb_build_object('id', 'b', 'text', 'b) Đạo hàm f''(x) = 3x^2 - 6x có hai nghiệm phân biệt x_1 = 0, x_2 = 2', 'isCorrect', true),
            jsonb_build_object('id', 'c', 'text', 'c) Hàm số đạt cực đại tại điểm x = 2 với giá trị cực đại y_CĐ = -2', 'isCorrect', false),
            jsonb_build_object('id', 'd', 'text', 'd) Điểm uốn của đồ thị là I(1; 0) và I là tâm đối xứng của đồ thị', 'isCorrect', true)
        ),
        'explanation', 'Đánh giá chi tiết từng mệnh đề:' || chr(10) ||
                       '- **a) ĐÚNG**: Hàm đa thức xác định trên toàn trục số thực $\mathbb{R}$.' || chr(10) ||
                       '- **b) ĐÚNG**: $f''(x) = 3x^2 - 6x = 3x(x - 2) = 0 \Leftrightarrow x = 0$ hoặc $x = 2$.' || chr(10) ||
                       '- **c) SAI**: Qua $x = 0$, $f''(x)$ đổi dấu từ dương sang âm nên $x = 0$ là điểm cực đại ($y_{\text{CĐ}} = 2$). Qua $x = 2$, đạo hàm đổi dấu từ âm sang dương nên $x = 2$ là điểm **cực tiểu** ($y_{\text{CT}} = -2$).' || chr(10) ||
                       '- **d) ĐÚNG**: $f''''(x) = 6x - 6 = 0 \Leftrightarrow x = 1 \Rightarrow y = 0$. Điểm uốn $I(1; 0)$ chính là tâm đối xứng của đồ thị hàm bậc ba.'
    ),
    NOW(),
    NOW()
);

-- 10. CÂU 7: DẠNG MATCHING (NỐI CẶP Ý A - B)
INSERT INTO "Questions" (
    "Id", "TenantId", "QuestionType", "CategoryCode", "DomainCode",
    "Content", "Options", "CorrectOption", "Difficulty", "DurationSeconds",
    "IsPublic", "IsCritical", "Payload", "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'c8aeae8d-daa2-4c78-9fa1-dd8d08ab12c4'::uuid,
    'MATCHING',
    'GUEST_SHOW',
    'GENERAL',
    'Hãy ghép nối chính xác mỗi giao thức mạng ở cột bên trái với cổng dịch vụ (Port) và tầng tương ứng trong mô hình mạng tiêu chuẩn:',
    ARRAY[
        'HTTPS - Cổng 443 (TLS Encrypted Web)',
        'DNS - Cổng 53 (Domain Name System)',
        'SSH - Cổng 22 (Secure Remote Shell)',
        'PostgreSQL - Cổng 5432 (RDBMS Engine)'
    ],
    'HTTPS:Cổng 443 (TLS Encrypted Web),DNS:Cổng 53 (Domain Name System),SSH:Cổng 22 (Secure Remote Shell),PostgreSQL:Cổng 5432 (RDBMS Engine)',
    3,
    60,
    true,
    false,
    jsonb_build_object(
        'leftItems', jsonb_build_array('HTTPS', 'DNS', 'SSH', 'PostgreSQL'),
        'rightItems', jsonb_build_array('Cổng 443 (TLS Encrypted Web)', 'Cổng 53 (Domain Name System)', 'Cổng 22 (Secure Remote Shell)', 'Cổng 5432 (RDBMS Engine)'),
        'correctPairs', jsonb_build_array(
            jsonb_build_object('left', 'HTTPS', 'right', 'Cổng 443 (TLS Encrypted Web)'),
            jsonb_build_object('left', 'DNS', 'right', 'Cổng 53 (Domain Name System)'),
            jsonb_build_object('left', 'SSH', 'right', 'Cổng 22 (Secure Remote Shell)'),
            jsonb_build_object('left', 'PostgreSQL', 'right', 'Cổng 5432 (RDBMS Engine)')
        ),
        'explanation', 'Cổng mạng chuẩn hóa theo IANA:' || chr(10) ||
                       '- **HTTPS**: Port 443/TCP cho truyền thông web bảo mật SSL/TLS.' || chr(10) ||
                       '- **DNS**: Port 53/UDP & TCP để phân giải tên miền.' || chr(10) ||
                       '- **SSH**: Port 22/TCP cho phiên kết nối điều khiển từ xa bảo mật.' || chr(10) ||
                       '- **PostgreSQL**: Port 5432/TCP cho hệ quản trị cơ sở dữ liệu quan hệ.'
    ),
    NOW(),
    NOW()
);

-- 11. CÂU 8: DẠNG ORDERING (SẮP XẾP QUY TRÌNH / THỨ TỰ THEO CHUẨN NIST)
INSERT INTO "Questions" (
    "Id", "TenantId", "QuestionType", "CategoryCode", "DomainCode",
    "Content", "Options", "CorrectOption", "Difficulty", "DurationSeconds",
    "IsPublic", "IsCritical", "Payload", "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'c8aeae8d-daa2-4c78-9fa1-dd8d08ab12c4'::uuid,
    'ORDERING',
    'GUEST_SHOW',
    'GENERAL',
    'Hãy sắp xếp các giai đoạn sau đây theo đúng trình tự vòng đời phản ứng sự cố an toàn thông tin (Incident Response Lifecycle) theo chuẩn quốc tế NIST SP 800-61 Rev. 2:',
    ARRAY[
        'Chuẩn bị (Preparation)',
        'Phát hiện & Phân tích (Detection & Analysis)',
        'Ngăn chặn, Xóa bỏ & Khôi phục (Containment, Eradication & Recovery)',
        'Hoạt động sau sự cố (Post-Incident Activity / Lessons Learned)'
    ],
    '1,2,3,4',
    3,
    60,
    true,
    false,
    jsonb_build_object(
        'items', jsonb_build_array(
            'Chuẩn bị (Preparation)',
            'Phát hiện & Phân tích (Detection & Analysis)',
            'Ngăn chặn, Xóa bỏ & Khôi phục (Containment, Eradication & Recovery)',
            'Hoạt động sau sự cố (Post-Incident Activity / Lessons Learned)'
        ),
        'correctOrder', jsonb_build_array('1', '2', '3', '4'),
        'explanation', 'Chuẩn NIST SP 800-61 Rev. 2 quy định 4 pha tuần tự khép kín:' || chr(10) ||
                       '1. **Preparation**: Chuẩn bị công cụ, quy trình và chính sách an ninh.' || chr(10) ||
                       '2. **Detection & Analysis**: Giám sát SIEM/SOC và đánh giá mức độ nghiêm trọng.' || chr(10) ||
                       '3. **Containment, Eradication & Recovery**: Cách ly nguồn lây nhiễm, triệt tiêu mã độc và phục hồi hệ thống.' || chr(10) ||
                       '4. **Post-Incident Activity**: Tổng kết bài học kinh nghiệm và cập nhật tài liệu kiến trúc.'
    ),
    NOW(),
    NOW()
);

-- 12. CÂU 9: DẠNG ESSAY (TỰ LUẬN KÈM RUBRIC CHẤM ĐIỂM AI)
INSERT INTO "Questions" (
    "Id", "TenantId", "QuestionType", "CategoryCode", "DomainCode",
    "Content", "Options", "CorrectOption", "Difficulty", "DurationSeconds",
    "IsPublic", "IsCritical", "GradingRubric", "Payload", "CreatedAt", "UpdatedAt"
) VALUES (
    gen_random_uuid(),
    'c8aeae8d-daa2-4c78-9fa1-dd8d08ab12c4'::uuid,
    'ESSAY',
    'GUEST_SHOW',
    'GENERAL',
    'Phân tích các ưu điểm và thách thức khi chuyển đổi mô hình lưu trữ từ CRUD truyền thống sang Event Sourcing kết hợp CQRS trong hệ thống ngân hàng cốt lõi (Core Banking). Nêu ví dụ về tính bất biến của sổ cái kế toán (Audit Log).',
    ARRAY[]::text[],
    '',
    5,
    180,
    true,
    false,
    'Barem chấm điểm (Thang 10 điểm):' || chr(10) ||
    '1. Nêu đúng bản chất Event Sourcing: Lưu trữ chuỗi sự kiện bất biến (3 điểm).' || chr(10) ||
    '2. Phân tích ưu điểm: Audit Trail 100%, Time-travel tái hiện quá khứ, tối ưu ghi (3 điểm).' || chr(10) ||
    '3. Nêu thách thức: Eventual Consistency, độ trễ Projection, tăng dung lượng (2 điểm).' || chr(10) ||
    '4. Ví dụ Core Banking: Sổ cái không UPDATE/DELETE mà dùng bút toán đảo Reversal Entry (2 điểm).',
    jsonb_build_object(
        'rubric', 'Barem chấm điểm (Thang 10 điểm):' || chr(10) ||
                  '1. Nêu đúng bản chất Event Sourcing: Lưu trữ chuỗi sự kiện bất biến (3 điểm).' || chr(10) ||
                  '2. Phân tích ưu điểm: Audit Trail 100%, Time-travel tái hiện quá khứ, tối ưu ghi (3 điểm).' || chr(10) ||
                  '3. Nêu thách thức: Eventual Consistency, độ trễ Projection, tăng dung lượng (2 điểm).' || chr(10) ||
                  '4. Ví dụ Core Banking: Sổ cái không UPDATE/DELETE mà dùng bút toán đảo Reversal Entry (2 điểm).',
        'maxScore', 10.0,
        'explanation', 'Kiến trúc Event Sourcing kết hợp CQRS là hình mẫu chuẩn mực cho Fintech và Ngân hàng hiện đại:' || chr(10) ||
                       '- **Bản chất**: Trạng thái hiện tại được suy luận từ tổng các sự kiện trong quá khứ.' || chr(10) ||
                       '- **Audit Trail 100%**: Không ai có thể chỉnh sửa số dư trái phép vì mỗi giao dịch là một bản ghi bất biến không thể xóa.' || chr(10) ||
                       '- **Bút toán đảo**: Khi xảy ra giao dịch nhầm, kế toán ngân hàng luôn thực hiện một giao dịch đảo ngược (Reversal Transaction) chứ không bao giờ xóa giao dịch cũ.'
    ),
    NOW(),
    NOW()
);
