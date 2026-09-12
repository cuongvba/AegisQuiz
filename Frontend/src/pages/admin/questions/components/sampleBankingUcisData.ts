/**
 * Universal Cognitive Ingestion Studio (UCIS v3.0)
 * Realistic Benchmark Dataset: "1. Tín dụng KHDN.xlsx" (240 questions)
 * Features: Multi-sheet, Skipped Reference Sheet, Auto Schema Mapping, Cognitive Health Radar
 */

export function getSampleBankingUcisData() {
  const sampleBankQuestions = [
    {
      stt: 1,
      content: 'Thời hạn cấp tín dụng trung hạn tối đa theo quy định hiện hành là bao nhiêu năm?',
      options: ['1 năm', '3 năm', '5 năm', 'Trên 5 năm'],
      correctAnswer: '3', // corresponds to option index 2: "5 năm"
      suggestedAnswer: '3',
      reference: 'Khoản 1 Điều 13 Thông tư 39/2016/TT-NHNN quy định về hoạt động cho vay của TCTD',
      difficulty: 2,
      isCritical: false,
    },
    {
      stt: 2,
      content: 'Hồ sơ đề nghị cấp tín dụng doanh nghiệp bắt buộc phải có tài liệu nào sau đây?',
      options: [
        'Báo cáo tài chính tối thiểu 02 năm gần nhất',
        'Phương án sử dụng vốn khả thi và kế hoạch trả nợ',
        'Tài liệu chứng minh tài sản bảo đảm hợp pháp',
        'Tất cả các tài liệu trên',
      ],
      correctAnswer: '4',
      suggestedAnswer: '4',
      reference: 'Điều 16 Thông tư 39/2016/TT-NHNN & Quy trình thẩm định tín dụng KHDN',
      difficulty: 2,
      isCritical: false,
    },
    {
      stt: 3,
      content: 'Tỷ lệ an toàn vốn tối thiểu (CAR) áp dụng đối với ngân hàng thương mại theo Thông tư 41/2016/TT-NHNN là bao nhiêu?',
      options: ['8%', '9%', '10%', '12%'],
      correctAnswer: '1',
      suggestedAnswer: '1',
      reference: 'Thông tư 41/2016/TT-NHNN quy định tỷ lệ an toàn vốn đối với ngân hàng thương mại chuẩn Basel II',
      difficulty: 3,
      isCritical: false,
    },
    {
      stt: 4,
      content: 'Theo Luật các TCTD 2024, tổng mức dư nợ cấp tín dụng đối với một khách hàng không được vượt quá bao nhiêu % vốn tự có của ngân hàng thương mại?',
      options: ['10%', '14% (lộ trình giảm dần)', '15%', '25%'],
      correctAnswer: '2',
      suggestedAnswer: '2',
      reference: 'Điều 136 Luật các Tổ chức tín dụng số 32/2024/QH15 quy định giới hạn cấp tín dụng',
      difficulty: 4,
      isCritical: true,
    },
    {
      stt: 5,
      content: 'Tài sản nào sau đây KHÔNG được sử dụng làm tài sản bảo đảm tiền vay theo quy định pháp luật?',
      options: [
        'Bất động sản có giấy chứng nhận quyền sử dụng đất hợp pháp',
        'Phương tiện vận tải cơ giới có đăng ký',
        'Tài sản đang có tranh chấp hoặc bị kê biên để thi hành án',
        'Hàng hóa luân chuyển trong quá trình sản xuất kinh doanh',
      ],
      correctAnswer: '3',
      suggestedAnswer: '3',
      reference: 'Bộ luật Dân sự 2015 & Nghị định 21/2021/NĐ-CP về bảo đảm thực hiện nghĩa vụ',
      difficulty: 1,
      isCritical: true, // Điểm liệt an toàn pháp lý
    },
    {
      stt: 6,
      content: 'Biện pháp cấp bảo lãnh ngân hàng cho bên nhận bảo lãnh là nghiệp vụ thuộc nhóm nào sau đây?',
      options: ['Huy động vốn', 'Cấp tín dụng', 'Dịch vụ thanh toán trung gian', 'Kinh doanh ngoại tệ'],
      correctAnswer: '2',
      suggestedAnswer: '2',
      reference: 'Khoản 22 Điều 4 Luật các Tổ chức tín dụng 2024',
      difficulty: 2,
      isCritical: false,
    },
    {
      stt: 7,
      content: 'Khách hàng doanh nghiệp được xếp hạng tín dụng nội bộ mức nào thì được xem xét cấp tín dụng không có bảo đảm bằng tài sản theo quy chế?',
      options: [
        'Hạng AAA và AA',
        'Hạng A trở lên theo hệ thống chấm điểm xếp hạng tín dụng nội bộ',
        'Hạng B trở lên',
        'Tất cả các khách hàng có bảo lãnh của công ty mẹ',
      ],
      correctAnswer: '2',
      suggestedAnswer: '2',
      reference: 'Quy chế xếp hạng tín dụng nội bộ và quản trị rủi ro tín dụng 2026',
      difficulty: 3,
      isCritical: false,
    },
    {
      stt: 8,
      content: 'Thẩm quyền phê duyệt khoản cấp tín dụng vượt hạn mức chi nhánh loại 1 thuộc về cấp nào?',
      options: [
        'Giám đốc Chi nhánh tự quyết định và báo cáo sau',
        'Hội đồng tín dụng Trụ sở chính / Ban Điều hành theo ủy quyền',
        'Phòng Khách hàng Doanh nghiệp Trụ sở chính',
        'Ban Kiểm soát nội bộ',
      ],
      correctAnswer: '2',
      suggestedAnswer: '2',
      reference: 'Quy chế phân cấp thẩm quyền phán quyết tín dụng toàn hệ thống 2026',
      difficulty: 3,
      isCritical: true,
    },
    {
      stt: 9,
      content: 'Khi xác định nhu cầu vốn lưu động của doanh nghiệp, công thức tính nào sau đây là chuẩn xác?',
      options: [
        'Nhu cầu VLĐ = Chi phí SXKD cần thiết cho chu kỳ KD - Vốn tự có và vốn huy động khác',
        'Nhu cầu VLĐ = Tổng tài sản ngắn hạn - Tổng nợ ngắn hạn',
        'Nhu cầu VLĐ = Doanh thu thuần / Số vòng quay hàng tồn kho',
        'Nhu cầu VLĐ = Lợi nhuận trước thuế + Khấu hao tài sản cố định',
      ],
      correctAnswer: '1',
      suggestedAnswer: '1',
      reference: 'Sổ tay thẩm định tài chính khách hàng doanh nghiệp 2026',
      difficulty: 3,
      isCritical: false,
    },
    {
      stt: 10,
      content: 'Thời hạn tối đa kể từ khi phát sinh nợ quá hạn mà TCTD phải gửi thông báo cho khách hàng là bao nhiêu ngày?',
      options: ['3 ngày làm việc', '5 ngày làm việc', '7 ngày làm việc', '10 ngày làm việc'],
      correctAnswer: '2',
      suggestedAnswer: '2',
      reference: 'Điều 20 Thông tư 39/2016/TT-NHNN về theo dõi và xử lý nợ quá hạn',
      difficulty: 2,
      isCritical: false,
    },
    {
      stt: 11,
      content: 'Chỉ số khả năng thanh toán lãi vay (ICR - Interest Coverage Ratio) được tính bằng:',
      options: [
        'EBIT / Chi phí lãi vay',
        'EBITDA / Doanh thu thuần',
        'Lợi nhuận ròng / Tổng nợ phải trả',
        'Lưu chuyển tiền thuần từ HĐKD / Vốn chủ sở hữu',
      ],
      correctAnswer: '1',
      suggestedAnswer: '1',
      reference: 'Phân tích báo cáo tài chính doanh nghiệp - Phân hệ thẩm định tín dụng',
      difficulty: 3,
      isCritical: false,
    },
    {
      stt: 12,
      content: 'Nhóm nợ nào sau đây thuộc danh mục Nợ xấu (NPL) theo Thông tư 11/2021/TT-NHNN?',
      options: [
        'Nhóm 1 (Nợ đủ tiêu chuẩn) và Nhóm 2 (Nợ cần chú ý)',
        'Nhóm 2 (Nợ cần chú ý) và Nhóm 3 (Nợ dưới tiêu chuẩn)',
        'Nhóm 3 (Nợ dưới tiêu chuẩn), Nhóm 4 (Nợ nghi ngờ) và Nhóm 5 (Nợ có khả năng mất vốn)',
        'Chỉ riêng Nhóm 5 (Nợ có khả năng mất vốn)',
      ],
      correctAnswer: '3',
      suggestedAnswer: '3',
      reference: 'Điều 10 Thông tư 11/2021/TT-NHNN về phân loại nợ và trích lập dự phòng rủi ro',
      difficulty: 2,
      isCritical: true,
    },
  ];

  // Sinh đủ 240 câu hỏi chuyên môn tín dụng KHDN chuẩn xác
  const totalTarget = 240;
  const questions: any[] = [];

  const domains = ['Thẩm định dự án', 'Bảo đảm tiền vay', 'Hạn mức tín dụng', 'Cho vay đồng tài trợ', 'Tài trợ thương mại & L/C', 'Quản trị nợ có vấn đề', 'Kiểm tra sau cấp tín dụng', 'Luật các TCTD 2024'];
  const bloomWeights = [1, 2, 2, 3, 3, 3, 4];

  for (let i = 1; i <= totalTarget; i++) {
    const baseIdx = (i - 1) % sampleBankQuestions.length;
    const base = sampleBankQuestions[baseIdx];
    const cycle = Math.floor((i - 1) / sampleBankQuestions.length);
    const domain = domains[(i - 1) % domains.length];
    const diff = bloomWeights[(i - 1) % bloomWeights.length];
    const isCritical = (i % 13 === 0);

    const questionContent = cycle === 0 
      ? base.content 
      : `[Chuyên đề ${domain} #${cycle + 1}] ${base.content}`;

    questions.push({
      id: `q_sample_bank_${i}`,
      tempId: `studio_temp_${i}`,
      content: questionContent,
      questionType: 'SINGLE',
      options: [...base.options],
      answerRaw: base.correctAnswer,
      suggestedAnswer: base.correctAnswer,
      reference: base.reference,
      difficulty: diff,
      isCritical: isCritical,
      topicCode: 'CREDIT_KHDN',
      domainCode: 'BANKING',
      targetLevel: 'Chuyên viên Tín dụng KHDN',
      assessmentPurpose: 'Sát hạch chuẩn chức danh 2026',
      issuingOrg: 'Agribank',
      benchmarkYear: 2026,
      benchmarkStandard: 'Basel II/III',
      tags: ['Tín dụng KHDN', domain, 'Sát hạch 2026'],
      orderIndex: i,
    });
  }

  return {
    fileName: '1. Tín dụng KHDN.xlsx',
    totalSheets: 2,
    totalQuestions: 240,
    detectedDomainCode: 'BANKING',
    detectedDomainName: 'Ngân hàng & Tài chính',
    detectedTargetLevel: 'Chuyên viên Tín dụng Doanh nghiệp (KHDN)',
    detectedAssessmentPurpose: 'Thi nâng ngạch & sát hạch chuẩn chức danh 2026',
    detectedIssuingOrg: 'Agribank - Ngân hàng Nông nghiệp & Phát triển Nông thôn Việt Nam',
    detectedBenchmarkYear: 2026,
    detectedBenchmarkStandard: 'Khung năng lực tín dụng Basel II/III & Thông tư 39/2016/TT-NHNN',
    detectedTags: ['Tín dụng KHDN', 'Thẩm định dự án', 'Bảo đảm tiền vay', 'Luật các TCTD 2024', 'Xếp hạng tín dụng'],
    warnings: [
      'Phát hiện Sheet "Văn bản tham chiếu" không chứa cấu trúc câu hỏi thi. Đã tự động phân loại thành Sheet Tài liệu tham khảo và bỏ qua nhập đề.',
      'Tất cả 240 câu hỏi được ánh xạ tự động đáp án chuẩn xác từ dạng số [1, 2, 3, 4] sang phương án nội dung tương ứng theo chuẩn UCIS v3.0.',
    ],
    skippedSheets: [
      {
        sheetName: 'Văn bản tham chiếu',
        skipReason: 'REFERENCE_DOCUMENTATION',
        message: 'Chứa danh mục 18 thông tư, nghị định và quy chế nội bộ ngân hàng. Không chứa cột câu hỏi thi trắc nghiệm (Đã tự động bảo vệ dữ liệu).',
      },
    ],
    sheets: [
      {
        sheetIndex: 0,
        sheetName: 'Tín dụng KHDN',
        sheetRole: 'EXAM_QUESTIONS',
        confidenceScore: 98.8,
        headerRowIndex: 3,
        rowCount: 244,
        columnCount: 8,
        detectedParentTopicCode: '2026_DOT2',
        detectedParentTopicName: '2026-DOT2',
        detectedTopicCode: '2026_DOT2_1_TIN_DUNG_KHDN_240_CAU',
        detectedTopicName: '1. Tín dụng KHDN - 240 câu',
        detectedTopicDescription: '1. Tín dụng KHDN - 240 câu | Đường dẫn: 2026-DOT2 | Số lượng: 240 câu hỏi',
        detectedDomainCode: 'BANKING',
        detectedTargetLevel: 'Chuyên viên KHDN',
        detectedAssessmentPurpose: 'Sát hạch nghiệp vụ 2026',
        detectedIssuingOrg: 'Agribank',
        detectedBenchmarkYear: 2026,
        detectedBenchmarkStandard: 'Basel II/III',
        detectedTags: ['Tín dụng KHDN', 'Thẩm định dự án', 'Bảo đảm tiền vay', 'Luật TCTD 2024'],
        isEnabledForImport: true,
        healthStats: {
          totalQuestions: 240,
          validQuestions: 240,
          missingAnswers: 0,
          fatalErrors: 0,
          syntaxWarnings: 0,
          criticalCount: 18,
          difficultyBreakdown: { 1: 34, 2: 70, 3: 102, 4: 34 },
        },
        columnMapping: {
          stt: 0,
          content: 1,
          option_1: 2,
          option_2: 3,
          option_3: 4,
          option_4: 5,
          answer: 6,
          reference: 7,
        },
        sampleRows: [
          ['STT', 'CÂU HỎI', 'ĐÁP ÁN 1', 'ĐÁP ÁN 2', 'ĐÁP ÁN 3', 'ĐÁP ÁN 4', 'ĐÁP ÁN ĐÚNG', 'TRÍCH DẪN NGUỒN'],
          ['1', 'Thời hạn cấp tín dụng trung hạn tối đa theo quy định hiện hành là bao nhiêu năm?', '1 năm', '3 năm', '5 năm', 'Trên 5 năm', '3', 'Khoản 1 Điều 13 Thông tư 39/2016/TT-NHNN'],
          ['2', 'Hồ sơ đề nghị cấp tín dụng doanh nghiệp bắt buộc phải có tài liệu nào sau đây?', 'Báo cáo tài chính tối thiểu 02 năm gần nhất', 'Phương án sử dụng vốn khả thi', 'Tài liệu chứng minh tài sản bảo đảm', 'Tất cả các tài liệu trên', '4', 'Điều 16 Thông tư 39/2016/TT-NHNN'],
          ['3', 'Tỷ lệ an toàn vốn tối thiểu (CAR) áp dụng đối với ngân hàng thương mại theo Thông tư 41/2016/TT-NHNN là bao nhiêu?', '8%', '9%', '10%', '12%', '1', 'Thông tư 41/2016/TT-NHNN chuẩn Basel II'],
          ['4', 'Theo Luật các TCTD 2024, tổng mức dư nợ cấp tín dụng đối với một khách hàng không được vượt quá bao nhiêu % vốn tự có?', '10%', '14% (lộ trình)', '15%', '25%', '2', 'Điều 136 Luật các Tổ chức tín dụng số 32/2024/QH15'],
          ['5', 'Tài sản nào sau đây KHÔNG được sử dụng làm tài sản bảo đảm tiền vay theo quy định pháp luật?', 'Bất động sản có GCN quyền sử dụng đất', 'Phương tiện vận tải cơ giới', 'Tài sản đang có tranh chấp hoặc bị kê biên', 'Hàng hóa luân chuyển', '3', 'Bộ luật Dân sự 2015 & Nghị định 21/2021/NĐ-CP'],
          ['6', 'Biện pháp cấp bảo lãnh ngân hàng cho bên nhận bảo lãnh là nghiệp vụ thuộc nhóm nào?', 'Huy động vốn', 'Cấp tín dụng', 'Dịch vụ thanh toán trung gian', 'Kinh doanh ngoại tệ', '2', 'Khoản 22 Điều 4 Luật các TCTD 2024'],
          ['7', 'Khách hàng doanh nghiệp được xếp hạng tín dụng nội bộ mức nào thì được xem xét cấp tín dụng không có bảo đảm?', 'Hạng AAA và AA', 'Hạng A trở lên theo quy chế', 'Hạng B trở lên', 'Tùy phê duyệt HĐQT', '2', 'Quy chế xếp hạng tín dụng nội bộ 2026'],
          ['8', 'Thẩm quyền phê duyệt khoản cấp tín dụng vượt hạn mức chi nhánh thuộc về cấp nào?', 'Giám đốc Chi nhánh', 'Hội đồng tín dụng Trụ sở chính', 'Phòng Khách hàng Doanh nghiệp TSC', 'Ban Kiểm soát', '2', 'Quy chế phân cấp thẩm quyền 2026'],
        ],
        questions: questions,
      },
    ],
  };
}
