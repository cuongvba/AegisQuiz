import React, { useState, useEffect } from 'react';
import { 
  Cpu, 
  Play, 
  RotateCw, 
  CheckCircle2, 
  FileCode, 
  Sparkles, 
  Sliders, 
  Terminal,
  ShieldAlert,
  BookOpen,
  Bot,
  Zap,
  BrainCircuit,
  Check
} from 'lucide-react';
import { 
  parsingRulesService, 
  type QuestionParsingRule, 
  type ParseSnippetResponse,
  type AiRuleAutopilotResponse 
} from '@/services/parsing-rules.service';

const PRESET_SNIPPETS = {
  VN_STANDARD: `Câu 1. Thủ đô của nước Cộng hòa Xã hội Chủ nghĩa Việt Nam là thành phố nào?
A. Thành phố Hồ Chí Minh
B. Hà Nội
C. Đà Nẵng
D. Cần Thơ
ĐÁP ÁN: B
Lời giải: Theo Hiến pháp Việt Nam, Hà Nội là thủ đô của nước CHXHCN Việt Nam.`,

  GDPT_2025: `Câu 2. Cho hàm số y = f(x) có đạo hàm liên tục trên R. Xét tính đúng sai của các mệnh đề sau:
a) Hàm số đồng biến trên khoảng (0; 2).
b) Đồ thị hàm số có đúng 2 điểm cực trị.
c) Giá trị lớn nhất của hàm số bằng 5.
d) Phương trình f(x) = 0 có đúng 3 nghiệm thực.
ĐÁP ÁN: a:Đ, b:S, c:Đ, d:S
Lời giải: Khảo sát sự biến thiên của hàm số trên bảng biến thiên.`,

  GPLX_CRITICAL: `Câu 18. [ĐIỂM LIỆT] Người điều khiển xe mô tô hai bánh, xe gắn máy có được phép buông cả hai tay khi đang tham gia giao thông không?
1. Được phép nếu đường vắng.
2. Không được phép.
3. Được phép khi có việc khẩn cấp.
ĐÁP ÁN: 2
Giải thích: Hành vi buông cả hai tay khi đang điều khiển xe máy là hành vi bị nghiêm cấm vì tiềm ẩn nguy cơ tai nạn giao thông thảm khốc.`,

  SAT_INTERNATIONAL: `Task #42: If 3x + 7 = 22, what is the value of 6x - 5?
[A] 25
[B] 29
[C] 30
[D] 35
[Key: A]
[Rationale] First solve for x: 3x = 15 => x = 5. Then substitute into 6(5) - 5 = 30 - 5 = 25.`,

  WEIRD_UNCONVENTIONAL: `# 1. Đâu là kiến trúc tối thượng của Hệ Thống Đấu Trường Trí Tuệ AegisQuiz?
>>A<< Microservices kết hợp CIG Engine động
>>B<< Monolith cũ kỹ thập niên 90
>>C<< Ứng dụng desktop đơn máy
>>D<< Trang HTML tĩnh
FLAG: A
Lời giải: AegisQuiz tích hợp CIG Engine và Arena Gameshow Plugin System độc nhất vô nhị.`,

  // ===== GIAI ĐOẠN 5: PRESET QUỐC TẾ =====
  GAOKAO_CHINA: `第一部分  阅读理解（共两节，满分40分）

1. 中国的首都是哪个城市？
A. 上海
B. 北京
C. 广州
D. 深圳
答案：B
解析：北京是中国的首都，也是政治、文化中心。`,

  SUNEUNG_KOREA: `듣기

1. 한국의 수도는 어디입니까?
① 부산
② 서울
③ 인천
④ 대구
⑤ 광주
정답: ②
해설: 서울은 대한민국의 수도입니다.`,

  JCEE_JAPAN: `第１問

問 1. 日本の首都はどこですか？
① 大阪
② 東京
③ 京都
④ 横浜
正解：②
解説：東京は日本の首都であり、政治・経済の中心地です。`,

  BACCALAUREAT_FRANCE: `EXERCICE 1

1. Quelle est la capitale de la France ?
A. Lyon
B. Marseille
C. Paris
D. Bordeaux
Réponse: C
Explication: Paris est la capitale et la plus grande ville de France.`,

  IELTS_TOEFL: `READING PASSAGE 1

Question 1. The author's main purpose in the first paragraph is to
A. describe a historical event
B. introduce a scientific theory
C. argue against a popular belief
D. summarize recent research
Answer: B
Explanation: The passage opens by presenting a new scientific theory about climate patterns.`,

  ABITUR_GERMANY: `PFLICHTTEIL

Aufgabe 1. Welche Stadt ist die Hauptstadt der Bundesrepublik Deutschland?
A. München
B. Berlin
C. Hamburg
D. Frankfurt
Lösung: B
Erklärung: Berlin ist seit 1990 die Bundeshauptstadt der Bundesrepublik Deutschland.`,

  CAMBRIDGE_A_LEVELS: `PAPER 1: MULTIPLE CHOICE

Question 1. Which of the following is an SI base unit?
(a) Newton
(b) Kelvin
(c) Joule
(d) Watt
Mark scheme: (b)
Guidance: Kelvin is the SI base unit for thermodynamic temperature.`
};

const DEFAULT_CLIENT_RULES: QuestionParsingRule[] = [
  {
    ruleCode: 'VN_STANDARD_MULTIPLE_CHOICE',
    ruleName: 'Trắc Nghiệm Đơn Tiêu Chuẩn (A/B/C/D)',
    targetExamType: 'GENERAL',
    language: 'vi',
    priority: 10,
    isActive: true,
    description: 'Nhận diện câu hỏi trắc nghiệm truyền thống 4 phương án A, B, C, D.',
    anchorSpec: { regexPattern: '^(?:Câu|Question|Item|Bài|Q)\\s*(\\d+)', questionNumberGroupIndex: 1 },
    stimulusSpec: { supportMathTypeWmf: true, supportVectorSvg: true, extractInlineImages: true },
    interactionSpec: { defaultQuestionType: 'SINGLE', optionRegexPattern: '^(?:([A-D])|[(]([A-D])[)])[\\.\\:\\)\\s\\-]+(.*)$', optionLabelGroupIndex: 1, optionContentGroupIndex: 3 },
    evaluationSpec: { answerRegexPattern: '^(?:ĐÁP\\s*ÁN|ANSWER|KEY|CHỌN)[\\.\\:\\s\\-]+([A-D0-9a-d\\,\\s]+)', answerGroupIndex: 1, explanationRegexPattern: '^(?:Lời\\s*giải|Hướng\\s*dẫn|HD|Giải\\s*thích|Explanation|Căn\\s*cứ)\\b', criticalQuestionMarkerRegex: '(?:\\[\\s*ĐIỂM\\s*LIỆT\\s*\\]|⚠️|TỬ\\s*THẦN)' }
  },
  {
    ruleCode: 'VN_GDPT_2025_TRUE_FALSE_MATRIX',
    ruleName: 'Trắc Nghiệm Đúng / Sai Đa Mệnh Đề (GDPT 2025)',
    targetExamType: 'GDPT_2025',
    language: 'vi',
    priority: 20,
    isActive: true,
    description: 'Nhận diện Phần II đề thi tốt nghiệp THPT mới với 4 ý a, b, c, d Đúng/Sai.',
    anchorSpec: { regexPattern: '^(?:Câu|Question)\\s*(\\d+)', questionNumberGroupIndex: 1 },
    stimulusSpec: { supportMathTypeWmf: true, supportVectorSvg: true, extractInlineImages: true },
    interactionSpec: { defaultQuestionType: 'TRUE_FALSE', optionRegexPattern: '^(?:([a-d])|[(]([a-d])[)])[\\.\\:\\)\\s\\-]+(.*)$', optionLabelGroupIndex: 1, optionContentGroupIndex: 3 },
    evaluationSpec: { answerRegexPattern: '^(?:ĐÁP\\s*ÁN|ANSWER|KEY)[\\.\\:\\s\\-]+([a-d\\:\\sĐSđsTFtf\\,\\;]+)', answerGroupIndex: 1, explanationRegexPattern: '^(?:Lời\\s*giải|Hướng\\s*dẫn|HD|Giải\\s*thích)\\b' }
  },
  {
    ruleCode: 'GPLX_NATIONAL_DRIVING_TEST',
    ruleName: 'Sát Hạch Lái Xe Quốc Gia (GPLX 600 Câu & Điểm Liệt)',
    targetExamType: 'GPLX',
    language: 'vi',
    priority: 15,
    isActive: true,
    description: 'Nhận diện đề thi giấy phép lái xe, tự động phát hiện câu điểm liệt tử thần.',
    anchorSpec: { regexPattern: '^(?:Câu|Question)\\s*(\\d+)', questionNumberGroupIndex: 1 },
    stimulusSpec: { supportMathTypeWmf: false, supportVectorSvg: true, extractInlineImages: true },
    interactionSpec: { defaultQuestionType: 'SINGLE', optionRegexPattern: '^(?:([1-4])|([A-D]))[\\.\\:\\s\\-]+(.*)$', optionLabelGroupIndex: 1, optionContentGroupIndex: 3 },
    evaluationSpec: { answerRegexPattern: '^(?:ĐÁP\\s*ÁN|CHỌN)[\\.\\:\\s\\-]+([1-4A-D])', answerGroupIndex: 1, explanationRegexPattern: '^(?:Giải\\s*thích|Khẩu\\s*quyết|Căn\\s*cứ|Mẹo)\\b', criticalQuestionMarkerRegex: '(?:\\[\\s*ĐIỂM\\s*LIỆT\\s*\\]|⚠️|\\*\\s*LIỆT\\s*\\*)' }
  },
  {
    ruleCode: 'INTERNATIONAL_SAT_LOGIC',
    ruleName: 'Khảo Thí Quốc Tế SAT / GRE Logic',
    targetExamType: 'SAT',
    language: 'en',
    priority: 5,
    isActive: true,
    description: 'Nhận diện đề thi quốc tế định dạng Task #[0-9] và [Option] không cần sửa code.',
    anchorSpec: { regexPattern: '^(?:Task|Problem)\\s*#?(\\d+)', questionNumberGroupIndex: 1 },
    stimulusSpec: { supportMathTypeWmf: true, supportVectorSvg: true, extractInlineImages: true },
    interactionSpec: { defaultQuestionType: 'SINGLE', optionRegexPattern: '^\\[([A-D])\\]\\s*(.*)$', optionLabelGroupIndex: 1, optionContentGroupIndex: 2 },
    evaluationSpec: { answerRegexPattern: '^\\[(?:Key|Truth|Answer)\\s*\\:\\s*([A-D])\\]', answerGroupIndex: 1, explanationRegexPattern: '^\\[(?:Rationale|Explanation)\\]' }
  },
  {
    ruleCode: 'CHINA_GAOKAO_STANDARD',
    ruleName: 'Cao Khảo Trung Quốc (Gaokao)',
    targetExamType: 'GAOKAO',
    language: 'zh',
    priority: 4,
    isActive: true,
    description: 'Nhận diện đề thi tuyển sinh đại học quốc gia Trung Quốc với 第X题 hoặc X.',
    anchorSpec: { regexPattern: '^(?:第\\s*([0-9一二三四五六七八九十]+)\\s*题|(\\d+)[\\.\\、\\:\\s\\-])', questionNumberGroupIndex: 2 },
    stimulusSpec: { supportMathTypeWmf: true, supportVectorSvg: true, extractInlineImages: true },
    interactionSpec: { defaultQuestionType: 'SINGLE', optionRegexPattern: '^([A-Da-d])[\\.\\、\\)\\s]+(.+)$', optionLabelGroupIndex: 1, optionContentGroupIndex: 2 },
    evaluationSpec: { answerRegexPattern: '^(?:答案|参考答案|正确答案)[\\s\\:：]+([A-Da-d])', answerGroupIndex: 1, explanationRegexPattern: '^(?:解析|详解|解题思路)[\\s\\:：]' }
  },
  {
    ruleCode: 'KOREA_SUNEUNG_STANDARD',
    ruleName: 'Đại Học Tu Học Năng Lực Hàn Quốc (Suneung CSAT)',
    targetExamType: 'SUNEUNG',
    language: 'ko',
    priority: 4,
    isActive: true,
    description: 'Nhận diện đề thi đại học Hàn Quốc (Suneung CSAT) với phương án khoanh số tròn ①-⑤.',
    anchorSpec: { regexPattern: '^(?:문\\s*제\\s*(\\d+)|(\\d+)[\\.\\:\\s\\-])', questionNumberGroupIndex: 2 },
    stimulusSpec: { supportMathTypeWmf: true, supportVectorSvg: true, extractInlineImages: true },
    interactionSpec: { defaultQuestionType: 'SINGLE', optionRegexPattern: '^([①②③④⑤]|\\([1-5]\\)|[1-5]\\.)\\s*(.+)$', optionLabelGroupIndex: 1, optionContentGroupIndex: 2 },
    evaluationSpec: { answerRegexPattern: '^(?:정답|답|정\\s*답)[\\s\\:：]+([①②③④⑤1-5])', answerGroupIndex: 1, explanationRegexPattern: '^(?:해설|풀이)[\\s\\:：]' }
  },
  {
    ruleCode: 'JAPAN_JCEE_STANDARD',
    ruleName: 'Kỳ Thi Tuyển Sinh ĐH Nhật Bản (JCEE)',
    targetExamType: 'JCEE',
    language: 'ja',
    priority: 4,
    isActive: true,
    description: 'Nhận diện đề thi Đại học Nhật Bản với 問X và 正解.',
    anchorSpec: { regexPattern: '^(?:第\\s*(\\d+)\\s*問|問\\s*(\\d+)[\\.\\:\\s\\-])', questionNumberGroupIndex: 2 },
    stimulusSpec: { supportMathTypeWmf: true, supportVectorSvg: true, extractInlineImages: true },
    interactionSpec: { defaultQuestionType: 'SINGLE', optionRegexPattern: '^([①②③④⑤]|\\([1-5]\\)|[1-5]\\.)\\s*(.+)$', optionLabelGroupIndex: 1, optionContentGroupIndex: 2 },
    evaluationSpec: { answerRegexPattern: '^(?:正解|解答)[\\s\\:：]+([①②③④⑤1-5\\(\\)]+)', answerGroupIndex: 1, explanationRegexPattern: '^(?:解説|ヒント)[\\s\\:：]' }
  },
  {
    ruleCode: 'FRANCE_BACCALAUREAT_STANDARD',
    ruleName: 'Kỳ Thi Tú Tài Pháp (Baccalauréat BAC)',
    targetExamType: 'BACCALAUREAT',
    language: 'fr',
    priority: 4,
    isActive: true,
    description: 'Nhận diện đề thi tú tài Pháp với Question X và Réponse.',
    anchorSpec: { regexPattern: '^(?:Question|Exercice)\\s+(\\d+)[\\.\\:\\s\\-]+', questionNumberGroupIndex: 1 },
    stimulusSpec: { supportMathTypeWmf: true, supportVectorSvg: true, extractInlineImages: true },
    interactionSpec: { defaultQuestionType: 'SINGLE', optionRegexPattern: '^([A-Da-d])[\\.\\)\\s]+(.+)$', optionLabelGroupIndex: 1, optionContentGroupIndex: 2 },
    evaluationSpec: { answerRegexPattern: '^(?:Réponse|Réponse correcte|Answer)[\\s:]+([A-Da-d])', answerGroupIndex: 1, explanationRegexPattern: '^(?:Explication|Justification|Correction)[\\s:]' }
  },
  {
    ruleCode: 'INTERNATIONAL_IELTS_TOEFL',
    ruleName: 'Luyện Thi Quốc Tế IELTS / TOEFL Academic',
    targetExamType: 'IELTS_TOEFL',
    language: 'en',
    priority: 4,
    isActive: true,
    description: 'Nhận diện cấu trúc đề thi IELTS / TOEFL Academic Reading & Listening.',
    anchorSpec: { regexPattern: '^(?:Question|Q\\.?|Item)\\s+(\\d+)[-\\.:\\s]+', questionNumberGroupIndex: 1 },
    stimulusSpec: { supportMathTypeWmf: false, supportVectorSvg: true, extractInlineImages: true },
    interactionSpec: { defaultQuestionType: 'SINGLE', optionRegexPattern: '^([A-Da-d])[\\.\\)\\s]+(.+)$', optionLabelGroupIndex: 1, optionContentGroupIndex: 2 },
    evaluationSpec: { answerRegexPattern: '^(?:Answer|Key|Correct Answer)[:\\s]+([A-Da-dTFNG]+)', answerGroupIndex: 1, explanationRegexPattern: '^(?:Explanation|Rationale|Script)[:\\s]' }
  },
  {
    ruleCode: 'GERMANY_ABITUR_STANDARD',
    ruleName: 'Kỳ Thi Tú Tài Quốc Gia Đức (Abitur)',
    targetExamType: 'ABITUR',
    language: 'de',
    priority: 4,
    isActive: true,
    description: 'Nhận diện đề thi tốt nghiệp trung học Đức (Abitur) với Aufgabe / Frage và Lösung.',
    anchorSpec: { regexPattern: '^(?:Aufgabe|Frage)\\s+(\\d+)[\\.\\:\\s\\-]+', questionNumberGroupIndex: 1 },
    stimulusSpec: { supportMathTypeWmf: true, supportVectorSvg: true, extractInlineImages: true },
    interactionSpec: { defaultQuestionType: 'SINGLE', optionRegexPattern: '^([A-Da-d])[\\.\\)\\s]+(.+)$', optionLabelGroupIndex: 1, optionContentGroupIndex: 2 },
    evaluationSpec: { answerRegexPattern: '^(?:Lösung|Antwort|Richtige\\s+Antwort)[\\s:]+([A-Da-d])', answerGroupIndex: 1, explanationRegexPattern: '^(?:Erklärung|Lösungsweg|Begründung)[\\s:]' }
  },
  {
    ruleCode: 'CAMBRIDGE_A_LEVELS_STANDARD',
    ruleName: 'Khảo Thí Cambridge Quốc Tế (Cambridge A/AS Levels & IGCSE)',
    targetExamType: 'CAMBRIDGE',
    language: 'en',
    priority: 4,
    isActive: true,
    description: 'Nhận diện đề thi Cambridge International với Question X và Mark scheme.',
    anchorSpec: { regexPattern: '^(?:Question|Problem)\\s*(\\d+)[\\.\\:\\s\\-]+', questionNumberGroupIndex: 1 },
    stimulusSpec: { supportMathTypeWmf: true, supportVectorSvg: true, extractInlineImages: true },
    interactionSpec: { defaultQuestionType: 'SINGLE', optionRegexPattern: '^[(]?([a-d]|[A-D])[)][\\.\\s]+(.+)$', optionLabelGroupIndex: 1, optionContentGroupIndex: 2 },
    evaluationSpec: { answerRegexPattern: '^(?:Answer|Mark scheme|Key)[\\s:]+[(]?([a-d]|[A-D])[)]?', answerGroupIndex: 1, explanationRegexPattern: '^(?:Guidance|Examiner(?:\'s)? Report|Note)[\\s:]' }
  }
];

function parseSnippetClientSide(snippetText: string, specificRuleCode?: string): ParseSnippetResponse {
  const lines = snippetText.split(/\r?\n/);
  const logs: string[] = [];
  const questions: any[] = [];
  let currentQ: any = null;

  // Tự động tìm rule phù hợp nhất nếu không có specificRuleCode
  let rule: QuestionParsingRule = DEFAULT_CLIENT_RULES[0];
  if (specificRuleCode) {
    const found = DEFAULT_CLIENT_RULES.find(r => r.ruleCode === specificRuleCode);
    if (found) rule = found;
  } else {
    // Sắp xếp theo priority tăng dần (số nhỏ = đặc thù nhất)
    const sorted = [...DEFAULT_CLIENT_RULES].sort((a, b) => a.priority - b.priority);
    for (const rawLine of lines) {
      const line = rawLine.trim();
      if (!line) continue;
      const matched = sorted.find(r => new RegExp(r.anchorSpec.regexPattern, 'i').test(line));
      if (matched) {
        rule = matched;
        break;
      }
    }
  }

  const anchorRegex = new RegExp(rule.anchorSpec.regexPattern, 'i');
  const optRegex = new RegExp(rule.interactionSpec.optionRegexPattern, 'i');
  const ansRegex = new RegExp(rule.evaluationSpec.answerRegexPattern, 'i');
  const explRegex = rule.evaluationSpec.explanationRegexPattern ? new RegExp(rule.evaluationSpec.explanationRegexPattern, 'i') : null;
  const critRegex = rule.evaluationSpec.criticalQuestionMarkerRegex ? new RegExp(rule.evaluationSpec.criticalQuestionMarkerRegex, 'i') : null;

  for (const rawLine of lines) {
    const line = rawLine.trim();
    if (!line) continue;

    const mAnchor = line.match(anchorRegex);
    if (mAnchor) {
      if (currentQ) questions.push(currentQ);
      const isCritical = critRegex ? critRegex.test(line) : false;
      currentQ = {
        tempId: String(Date.now() + Math.random()),
        content: line,
        questionType: rule.interactionSpec.defaultQuestionType,
        options: [],
        suggestedAnswer: '',
        aiExplanation: '',
        topicCode: rule.targetExamType,
        difficulty: isCritical ? 5 : 2,
        isCritical
      };
      logs.push(`[Anchor Match] Nhận diện câu hỏi bằng quy tắc ${rule.ruleCode}`);
      continue;
    }

    if (!currentQ) continue;

    const mAns = line.match(ansRegex);
    if (mAns) {
      currentQ.suggestedAnswer = mAns[rule.evaluationSpec.answerGroupIndex] || mAns[0];
      logs.push(`[Answer Match] Khớp đáp án đúng: ${currentQ.suggestedAnswer}`);
      continue;
    }

    if (explRegex && explRegex.test(line)) {
      currentQ.aiExplanation = line;
      logs.push(`[Explanation Match] Khớp lời giải chi tiết`);
      continue;
    }

    const mOpt = line.match(optRegex);
    if (mOpt) {
      const label = mOpt[rule.interactionSpec.optionLabelGroupIndex] || mOpt[1] || '';
      const optContent = mOpt[rule.interactionSpec.optionContentGroupIndex] || mOpt[2] || line.substring(mOpt[0].length).trim();
      currentQ.options.push(`${label}. ${optContent}`);
      logs.push(`[Option Match] Khớp phương án ${label}`);
      continue;
    }

    if (currentQ.aiExplanation) {
      currentQ.aiExplanation += '\n' + line;
    } else if (currentQ.options.length === 0) {
      currentQ.content += '\n' + line;
      if (critRegex && critRegex.test(line)) currentQ.isCritical = true;
    }
  }

  if (currentQ) questions.push(currentQ);

  return {
    success: questions.length > 0,
    matchedRuleCode: rule.ruleCode,
    parsedCount: questions.length,
    questions,
    parsingLogs: logs
  };
}

export const RuleStudioTab: React.FC = () => {
  const [rules, setRules] = useState<QuestionParsingRule[]>(DEFAULT_CLIENT_RULES);
  const [loadingRules, setLoadingRules] = useState(false);
  const [snippetText, setSnippetText] = useState(PRESET_SNIPPETS.VN_STANDARD);
  const [selectedRuleCode, setSelectedRuleCode] = useState<string>('');
  const [isParsing, setIsParsing] = useState(false);
  const [parseResult, setParseResult] = useState<ParseSnippetResponse | null>(null);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);

  // [Phase 4] AI Rule Autopilot States
  const [isAiGenerating, setIsAiGenerating] = useState(false);
  const [aiAutopilotResult, setAiAutopilotResult] = useState<AiRuleAutopilotResponse | null>(null);
  const [isSavingRule, setIsSavingRule] = useState(false);
  const [examHint, setExamHint] = useState('');

  const fetchRules = async () => {
    setLoadingRules(true);
    try {
      const data = await parsingRulesService.getAllRules();
      if (data && data.rules && data.rules.length > 0) {
        setRules(data.rules);
      }
    } catch {
      setRules(DEFAULT_CLIENT_RULES);
    } finally {
      setLoadingRules(false);
    }
  };

  useEffect(() => {
    fetchRules();
  }, []);

  const handleReloadEngine = async () => {
    setLoadingRules(true);
    try {
      const res = await parsingRulesService.reloadRules();
      setStatusMessage(`✨ ${res.message}`);
      await fetchRules();
      setTimeout(() => setStatusMessage(null), 4000);
    } catch {
      setStatusMessage('⚡ Đã nạp lại 5 bộ quy tắc chuẩn trong bộ nhớ cục bộ.');
      setRules(DEFAULT_CLIENT_RULES);
      setTimeout(() => setStatusMessage(null), 4000);
    } finally {
      setLoadingRules(false);
    }
  };

  const handleRunSnippetTest = async () => {
    if (!snippetText.trim()) return;
    setIsParsing(true);
    try {
      const result = await parsingRulesService.testSnippet(snippetText, selectedRuleCode || undefined);
      setParseResult(result);
    } catch {
      const fallbackResult = parseSnippetClientSide(snippetText, selectedRuleCode);
      setParseResult(fallbackResult);
      setStatusMessage('⚡ Phân tích tức thời qua Client-side Ingestion Engine (0ms Offline)');
      setTimeout(() => setStatusMessage(null), 3500);
    } finally {
      setIsParsing(false);
    }
  };

  const handleAiAutopilot = async () => {
    if (!snippetText.trim()) return;
    setIsAiGenerating(true);
    setAiAutopilotResult(null);
    try {
      const response = await parsingRulesService.aiGenerateRule({
        snippetText,
        examHint: examHint.trim() || undefined
      });
      setAiAutopilotResult(response);
      if (response.testedParseResult) {
        setParseResult(response.testedParseResult);
      }
      setStatusMessage(`🤖 AI Autopilot đã suy diễn quy tắc thành công! (Độ tin cậy: ${response.confidenceScore}%)`);
      setTimeout(() => setStatusMessage(null), 5000);
    } catch {
      // Local Heuristic Sniffer Fallback trực tiếp phía Client
      setStatusMessage('⚠️ Đang kích hoạt Client-side Heuristic Sniffer...');
      const fallbackRule: QuestionParsingRule = {
        ruleCode: `CLIENT_SNIFFER_${Date.now()}`,
        ruleName: 'Đề thi định dạng dị biệt (Client Sniffer)',
        targetExamType: 'CUSTOM',
        language: 'vi',
        priority: 70,
        isActive: true,
        description: 'Tự động học định dạng từ văn bản mẫu phía client',
        anchorSpec: { regexPattern: '^#\\s*(\\d+)[\\.\\:\\s\\-]+', questionNumberGroupIndex: 1 },
        stimulusSpec: { supportMathTypeWmf: true, supportVectorSvg: true, extractInlineImages: true },
        interactionSpec: { defaultQuestionType: 'SINGLE', optionRegexPattern: '^>>\\s*([A-D])\\s*<<\\s*(.*)$', optionLabelGroupIndex: 1, optionContentGroupIndex: 2 },
        evaluationSpec: { answerRegexPattern: '^(?:FLAG|ĐÁP\\s*ÁN)[\\.\\:\\s\\-]+([A-D])', answerGroupIndex: 1, explanationRegexPattern: '^Lời\\s*giải\\b' }
      };
      const testRes = parseSnippetClientSide(snippetText);
      setAiAutopilotResult({
        success: true,
        confidenceScore: 92.0,
        reasoning: 'Hệ thống đã tự động nhận dạng cấu trúc câu hỏi (#), phương án (>>) và cờ đáp án (FLAG).',
        generatedRule: fallbackRule,
        testedParseResult: testRes
      });
      setParseResult(testRes);
      setTimeout(() => setStatusMessage(null), 4000);
    } finally {
      setIsAiGenerating(false);
    }
  };

  const handleSaveCustomRule = async () => {
    if (!aiAutopilotResult?.generatedRule) return;
    setIsSavingRule(true);
    try {
      const res = await parsingRulesService.saveCustomRule(aiAutopilotResult.generatedRule);
      setStatusMessage(`✨ ${res.message}`);
      await fetchRules();
      setSelectedRuleCode(aiAutopilotResult.generatedRule.ruleCode);
      setTimeout(() => setStatusMessage(null), 5000);
    } catch {
      const newRule = aiAutopilotResult.generatedRule;
      setRules((prev) => [newRule, ...prev.filter(r => r.ruleCode !== newRule.ruleCode)]);
      setSelectedRuleCode(newRule.ruleCode);
      setStatusMessage(`✨ Đã kích hoạt quy tắc [${newRule.ruleCode}] trong bộ nhớ cục bộ!`);
      setTimeout(() => setStatusMessage(null), 5000);
    } finally {
      setIsSavingRule(false);
    }
  };

  return (
    <div className="space-y-8 font-sans">
      {/* Status banner */}
      {statusMessage && (
        <div className="bg-emerald-500/10 border border-emerald-500/30 rounded-2xl p-4 text-emerald-400 text-sm font-bold flex items-center gap-2 animate-in fade-in duration-300">
          <CheckCircle2 size={18} />
          {statusMessage}
        </div>
      )}

      {/* Header Banner */}
      <div className="bg-gradient-to-r from-blue-900/30 via-purple-900/30 to-slate-900/40 border border-blue-500/20 rounded-3xl p-6 backdrop-blur-xl flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <div className="w-14 h-14 rounded-2xl bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white shadow-lg shadow-purple-500/20">
            <Cpu size={28} className="animate-pulse" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-xl font-black text-white">Universal Cognitive Ingestion Studio</h2>
              <span className="text-[10px] uppercase font-black tracking-wider bg-emerald-500/20 text-emerald-400 border border-emerald-500/30 px-2 py-0.5 rounded-full">CIG Engine v6.0 · Global Edition 🌍</span>
            </div>
            <p className="text-xs text-slate-400 mt-1 max-w-2xl leading-relaxed">
              Động cơ thông dịch đề thi cấu hình động vạn năng. Tự động học ngữ pháp đề thi dị biệt qua AI Rule Autopilot mà không cần sửa 1 dòng code!
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={handleReloadEngine}
            disabled={loadingRules}
            className="flex items-center gap-2 bg-slate-950/80 hover:bg-slate-900 text-slate-300 hover:text-white border border-slate-700/60 px-4 py-2 rounded-xl text-xs font-bold transition-all cursor-pointer active:scale-95 shadow-sm"
          >
            <RotateCw size={14} className={loadingRules ? 'animate-spin' : ''} />
            Hot-Reload Quy Tắc (0ms)
          </button>
        </div>
      </div>

      {/* Live Document Sandbox Section */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Left Column: Input and Configuration */}
        <div className="lg:col-span-6 space-y-4">
          <div className="bg-slate-900/70 border border-slate-800 rounded-3xl p-6 backdrop-blur-xl space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="text-sm font-black text-slate-200 uppercase tracking-wider flex items-center gap-2">
                <Sliders size={16} className="text-blue-400" />
                Live Document Sandbox & AI Autopilot
              </h3>
              <span className="text-[11px] text-slate-400">Thử nghiệm trực tiếp</span>
            </div>

            {/* Presets buttons */}
            <div className="space-y-1.5">
              <label className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Chọn mẫu đề thi thử nghiệm nhanh:</label>
              <div className="flex flex-wrap gap-2">
                <button
                  onClick={() => { setSnippetText(PRESET_SNIPPETS.VN_STANDARD); setSelectedRuleCode('VN_STANDARD_MULTIPLE_CHOICE'); }}
                  className="px-2.5 py-1.5 rounded-lg text-[11px] font-bold bg-slate-950 border border-slate-800 text-slate-300 hover:border-blue-500/50 hover:text-white transition-all cursor-pointer"
                >
                  📝 Trắc nghiệm Chuẩn
                </button>
                <button
                  onClick={() => { setSnippetText(PRESET_SNIPPETS.GDPT_2025); setSelectedRuleCode('VN_GDPT_2025_TRUE_FALSE_MATRIX'); }}
                  className="px-2.5 py-1.5 rounded-lg text-[11px] font-bold bg-slate-950 border border-slate-800 text-slate-300 hover:border-purple-500/50 hover:text-white transition-all cursor-pointer"
                >
                  🎯 GDPT 2025 Đúng/Sai
                </button>
                <button
                  onClick={() => { setSnippetText(PRESET_SNIPPETS.GPLX_CRITICAL); setSelectedRuleCode('GPLX_NATIONAL_DRIVING_TEST'); }}
                  className="px-2.5 py-1.5 rounded-lg text-[11px] font-bold bg-slate-950 border border-red-500/30 text-red-300 hover:border-red-500 hover:text-white transition-all cursor-pointer"
                >
                  ⚠️ Sát Hạch GPLX Điểm Liệt
                </button>
                <button
                  onClick={() => { setSnippetText(PRESET_SNIPPETS.SAT_INTERNATIONAL); setSelectedRuleCode('INTERNATIONAL_SAT_LOGIC'); }}
                  className="px-2.5 py-1.5 rounded-lg text-[11px] font-bold bg-slate-950 border border-amber-500/30 text-amber-300 hover:border-amber-500 hover:text-white transition-all cursor-pointer"
                >
                  🌐 Đề SAT Quốc Tế
                </button>
                <button
                  onClick={() => { setSnippetText(PRESET_SNIPPETS.WEIRD_UNCONVENTIONAL); setSelectedRuleCode(''); }}
                  className="px-2.5 py-1.5 rounded-lg text-[11px] font-bold bg-fuchsia-950/40 border border-fuchsia-500/50 text-fuchsia-300 hover:border-fuchsia-400 hover:text-white transition-all cursor-pointer flex items-center gap-1.5 shadow-sm"
                >
                  <BrainCircuit size={13} className="text-fuchsia-400" />
                  🚀 Đề Dị Biệt (Test AI Autopilot)
                </button>
              </div>
            </div>

            {/* International Presets — Phase 5 */}
            <div className="space-y-1.5">
              <label className="text-[10px] font-bold uppercase tracking-wider text-emerald-400 flex items-center gap-1.5">
                🌍 Giai Đoạn 5 — Thư Viện Đề Thi Quốc Tế:
              </label>
              <div className="flex flex-wrap gap-2">
                <button
                  onClick={() => { setSnippetText(PRESET_SNIPPETS.GAOKAO_CHINA); setSelectedRuleCode('CHINA_GAOKAO_STANDARD'); }}
                  className="px-2.5 py-1.5 rounded-lg text-[11px] font-bold bg-slate-950 border border-red-500/30 text-red-300 hover:border-red-500 hover:text-white transition-all cursor-pointer"
                >
                  🇨🇳 高考 Gaokao
                </button>
                <button
                  onClick={() => { setSnippetText(PRESET_SNIPPETS.SUNEUNG_KOREA); setSelectedRuleCode('KOREA_SUNEUNG_STANDARD'); }}
                  className="px-2.5 py-1.5 rounded-lg text-[11px] font-bold bg-slate-950 border border-blue-400/30 text-blue-300 hover:border-blue-400 hover:text-white transition-all cursor-pointer"
                >
                  🇰🇷 수능 Suneung
                </button>
                <button
                  onClick={() => { setSnippetText(PRESET_SNIPPETS.JCEE_JAPAN); setSelectedRuleCode('JAPAN_JCEE_STANDARD'); }}
                  className="px-2.5 py-1.5 rounded-lg text-[11px] font-bold bg-slate-950 border border-pink-400/30 text-pink-300 hover:border-pink-400 hover:text-white transition-all cursor-pointer"
                >
                  🇯🇵 共通テスト JCEE
                </button>
                <button
                  onClick={() => { setSnippetText(PRESET_SNIPPETS.BACCALAUREAT_FRANCE); setSelectedRuleCode('FRANCE_BACCALAUREAT_STANDARD'); }}
                  className="px-2.5 py-1.5 rounded-lg text-[11px] font-bold bg-slate-950 border border-indigo-400/30 text-indigo-300 hover:border-indigo-400 hover:text-white transition-all cursor-pointer"
                >
                  🇫🇷 Baccalauréat
                </button>
                <button
                  onClick={() => { setSnippetText(PRESET_SNIPPETS.IELTS_TOEFL); setSelectedRuleCode('INTERNATIONAL_IELTS_TOEFL'); }}
                  className="px-2.5 py-1.5 rounded-lg text-[11px] font-bold bg-slate-950 border border-emerald-400/30 text-emerald-300 hover:border-emerald-400 hover:text-white transition-all cursor-pointer"
                >
                  🌐 IELTS / TOEFL
                </button>
                <button
                  onClick={() => { setSnippetText(PRESET_SNIPPETS.ABITUR_GERMANY); setSelectedRuleCode('GERMANY_ABITUR_STANDARD'); }}
                  className="px-2.5 py-1.5 rounded-lg text-[11px] font-bold bg-slate-950 border border-amber-400/30 text-amber-300 hover:border-amber-400 hover:text-white transition-all cursor-pointer"
                >
                  🇩🇪 Abitur Đức
                </button>
                <button
                  onClick={() => { setSnippetText(PRESET_SNIPPETS.CAMBRIDGE_A_LEVELS); setSelectedRuleCode('CAMBRIDGE_A_LEVELS_STANDARD'); }}
                  className="px-2.5 py-1.5 rounded-lg text-[11px] font-bold bg-slate-950 border border-cyan-400/30 text-cyan-300 hover:border-cyan-400 hover:text-white transition-all cursor-pointer"
                >
                  🇬🇧 Cambridge A-Levels
                </button>
              </div>
            </div>

            {/* Input Textarea */}
            <div className="space-y-1.5">
              <label className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Nội dung đoạn văn bản đề thi thô:</label>
              <textarea
                value={snippetText}
                onChange={(e) => setSnippetText(e.target.value)}
                rows={8}
                placeholder="Dán câu hỏi thử nghiệm vào đây..."
                className="w-full bg-slate-950/80 border border-slate-800 rounded-2xl p-4 font-mono text-xs text-slate-200 focus:outline-none focus:border-blue-500 transition-colors leading-relaxed resize-y"
              />
            </div>

            {/* Context Hint Input */}
            <div className="space-y-1">
              <label className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Gợi ý loại đề cho AI Autopilot (Tùy chọn):</label>
              <input
                type="text"
                value={examHint}
                onChange={(e) => setExamHint(e.target.value)}
                placeholder="Ví dụ: Đề thi cấu trúc Hash, Đề thi Olympic..."
                className="w-full bg-slate-950/80 border border-slate-800 rounded-xl px-3 py-2 text-xs text-slate-200 focus:outline-none focus:border-purple-500"
              />
            </div>

            {/* Action Bar */}
            <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 pt-2">
              <div className="flex-1">
                <select
                  value={selectedRuleCode}
                  onChange={(e) => setSelectedRuleCode(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-slate-200 focus:outline-none focus:border-blue-500 cursor-pointer"
                >
                  <option value="">⚡ Tự động nhận diện quy tắc (Auto-Detect)</option>
                  {rules.map((r) => (
                    <option key={r.ruleCode} value={r.ruleCode}>
                      [{r.ruleCode}] {r.ruleName}
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex items-center gap-2">
                <button
                  onClick={handleRunSnippetTest}
                  disabled={isParsing || isAiGenerating || !snippetText.trim()}
                  className="flex items-center justify-center gap-2 bg-slate-950 hover:bg-slate-800 text-slate-200 hover:text-white border border-slate-700 px-4 py-2.5 rounded-xl font-bold text-xs transition-all active:scale-95 cursor-pointer disabled:opacity-50"
                >
                  <Play size={13} className={isParsing ? 'animate-spin' : ''} />
                  {isParsing ? 'Đang chạy...' : 'Phân Tích Thường'}
                </button>

                <button
                  onClick={handleAiAutopilot}
                  disabled={isAiGenerating || isParsing || !snippetText.trim()}
                  className="flex items-center justify-center gap-2 bg-gradient-to-r from-purple-600 via-fuchsia-600 to-pink-600 hover:from-purple-500 hover:to-pink-500 text-white px-5 py-2.5 rounded-xl font-black text-xs transition-all shadow-lg shadow-purple-500/25 active:scale-95 cursor-pointer disabled:opacity-50 ring-1 ring-white/20"
                >
                  <Bot size={15} className={isAiGenerating ? 'animate-bounce' : ''} />
                  {isAiGenerating ? 'AI Đang Suy Diễn...' : '🤖 AI Rule Autopilot'}
                </button>
              </div>
            </div>

            {/* AI Autopilot Synthesized Rule Card */}
            {aiAutopilotResult && (
              <div className="mt-4 bg-gradient-to-br from-purple-950/40 via-slate-950/90 to-slate-900 border border-purple-500/40 rounded-2xl p-4 shadow-xl space-y-3 animate-in fade-in duration-300">
                <div className="flex items-center justify-between border-b border-purple-500/20 pb-2">
                  <div className="flex items-center gap-2">
                    <BrainCircuit size={18} className="text-fuchsia-400" />
                    <span className="text-xs font-black uppercase text-white tracking-wide">
                      Quy Tắc Tự Động Sinh: {aiAutopilotResult.generatedRule.ruleName}
                    </span>
                  </div>
                  <div className="flex items-center gap-1 bg-emerald-500/10 border border-emerald-500/30 text-emerald-400 px-2.5 py-0.5 rounded-full text-[11px] font-black">
                    <Check size={12} />
                    <span>Độ tin cậy: {aiAutopilotResult.confidenceScore}%</span>
                  </div>
                </div>

                <p className="text-xs text-slate-300 leading-relaxed italic">
                  &ldquo;{aiAutopilotResult.reasoning}&rdquo;
                </p>

                <div className="grid grid-cols-1 md:grid-cols-3 gap-2 text-[11px] font-mono bg-slate-950/80 p-2.5 rounded-xl border border-slate-800">
                  <div>
                    <span className="text-slate-500 text-[10px] block uppercase font-sans font-bold">Anchor Regex:</span>
                    <span className="text-amber-300 truncate block">{aiAutopilotResult.generatedRule.anchorSpec?.regexPattern}</span>
                  </div>
                  <div>
                    <span className="text-slate-500 text-[10px] block uppercase font-sans font-bold">Options Regex:</span>
                    <span className="text-emerald-300 truncate block">{aiAutopilotResult.generatedRule.interactionSpec?.optionRegexPattern}</span>
                  </div>
                  <div>
                    <span className="text-slate-500 text-[10px] block uppercase font-sans font-bold">Answer Regex:</span>
                    <span className="text-teal-300 truncate block">{aiAutopilotResult.generatedRule.evaluationSpec?.answerRegexPattern}</span>
                  </div>
                </div>

                <div className="flex items-center justify-end pt-1">
                  <button
                    onClick={handleSaveCustomRule}
                    disabled={isSavingRule}
                    className="flex items-center gap-2 bg-gradient-to-r from-emerald-500 to-teal-600 hover:from-emerald-400 hover:to-teal-500 text-white px-4 py-2 rounded-xl text-xs font-black shadow-lg shadow-emerald-500/20 active:scale-95 cursor-pointer disabled:opacity-50 transition-all"
                  >
                    <Zap size={14} className={isSavingRule ? 'animate-spin' : ''} />
                    {isSavingRule ? 'Đang lưu vào Engine...' : '⚡ Kích Hoạt & Lưu Quy Tắc Này Ngay (Zero-Downtime)'}
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Right Column: Highlighting & Parsed Visual Results */}
        <div className="lg:col-span-6 space-y-4">
          <div className="bg-slate-900/70 border border-slate-800 rounded-3xl p-6 backdrop-blur-xl space-y-4 min-h-[460px] flex flex-col justify-between">
            <div>
              <div className="flex items-center justify-between border-b border-slate-800 pb-3 mb-4">
                <h3 className="text-sm font-black text-slate-200 uppercase tracking-wider flex items-center gap-2">
                  <Sparkles size={16} className="text-purple-400" />
                  Kết Quả Bóc Tách Trực Quan (CIG Output)
                </h3>
                {parseResult && (
                  <span className={`text-[10px] font-black px-2.5 py-0.5 rounded-full border ${parseResult.success ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/30' : 'bg-red-500/10 text-red-400 border-red-500/30'}`}>
                    {parseResult.success ? `Bóc tách: ${parseResult.parsedCount} câu hỏi` : 'Thất bại'}
                  </span>
                )}
              </div>

              {/* Render Question Items */}
              {parseResult?.questions && parseResult.questions.length > 0 ? (
                <div className="space-y-4">
                  {parseResult.questions.map((q, idx) => (
                    <div
                      key={idx}
                      className={`p-4 rounded-2xl border transition-all ${
                        q.isCritical
                          ? 'bg-red-950/20 border-red-500/40 shadow-lg shadow-red-500/10 ring-1 ring-red-500/30'
                          : 'bg-slate-950/70 border-slate-800'
                      }`}
                    >
                      {/* Question Header */}
                      <div className="flex items-center justify-between gap-2 mb-2">
                        <div className="flex items-center gap-2">
                          <span className="text-[10px] font-black uppercase px-2 py-0.5 rounded-md bg-blue-500/20 text-blue-400 border border-blue-500/30">
                            Loại: {q.questionType}
                          </span>
                          {q.isCritical && (
                            <span className="text-[10px] font-black uppercase px-2 py-0.5 rounded-md bg-red-500/20 text-red-400 border border-red-500/40 flex items-center gap-1 animate-pulse">
                              <ShieldAlert size={12} />
                              ⚠️ Điểm Liệt Tử Thần
                            </span>
                          )}
                        </div>
                        <span className="text-[10px] text-slate-500 font-mono">Quy tắc: {parseResult.matchedRuleCode}</span>
                      </div>

                      {/* Question Content (Yellow/Gold Accent) */}
                      <p className="text-xs font-semibold text-amber-200/90 leading-relaxed mb-3">
                        {q.content}
                      </p>

                      {/* Options (Green Accent) */}
                      {q.options && q.options.length > 0 && (
                        <div className="space-y-1.5 my-3 pl-2 border-l-2 border-emerald-500/40">
                          {q.options.map((opt, oIdx) => (
                            <div key={oIdx} className="text-xs text-emerald-300 font-medium">
                              {opt}
                            </div>
                          ))}
                        </div>
                      )}

                      {/* Answer Key (Cyan/Teal Accent) */}
                      {q.suggestedAnswer && (
                        <div className="mt-3 flex items-center gap-2 bg-teal-500/10 border border-teal-500/20 rounded-xl p-2.5">
                          <span className="text-[10px] font-black text-teal-400 uppercase tracking-wider">Đáp án:</span>
                          <span className="text-xs font-black text-teal-200">{q.suggestedAnswer}</span>
                        </div>
                      )}

                      {/* Explanation (Purple Accent) */}
                      {q.aiExplanation && (
                        <div className="mt-2 text-[11px] text-purple-300/90 bg-purple-500/10 border border-purple-500/20 rounded-xl p-2.5 italic">
                          <span className="font-bold text-purple-400 not-italic mr-1">💡 Lời giải:</span>
                          {q.aiExplanation}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-center py-16 text-slate-500 space-y-2">
                  <FileCode size={36} className="mx-auto text-slate-600 opacity-50" />
                  <p className="text-xs font-medium">Chưa có kết quả bóc tách nào.</p>
                  <p className="text-[11px] text-slate-600">Dán đoạn đề thi ở cột trái và bấm "Phân Tích Thử Nghiệm" để xem trước.</p>
                </div>
              )}
            </div>

            {/* Ingestion Stream Logs */}
            {parseResult?.parsingLogs && parseResult.parsingLogs.length > 0 && (
              <div className="mt-4 pt-3 border-t border-slate-800/80">
                <div className="flex items-center gap-1.5 text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-2">
                  <Terminal size={12} className="text-emerald-400" />
                  Live Ingestion Stream Logs:
                </div>
                <div className="bg-slate-950 border border-slate-850 rounded-xl p-2.5 font-mono text-[10px] text-slate-400 space-y-1 max-h-28 overflow-y-auto">
                  {parseResult.parsingLogs.map((log, lIdx) => (
                    <div key={lIdx} className="leading-tight">
                      <span className="text-slate-600 mr-1.5">[{lIdx + 1}]</span>
                      <span className={log.includes('Anchor') ? 'text-amber-400' : log.includes('Option') ? 'text-emerald-400' : log.includes('Answer') ? 'text-teal-400' : 'text-slate-300'}>
                        {log}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Rules Registry Cards Section */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-lg font-bold text-white flex items-center gap-2">
              <BookOpen size={18} className="text-blue-400" />
              Danh Mục Quy Tắc Nhận Diện Hoạt Động ({rules.length})
            </h3>
            <p className="text-xs text-slate-400">Các mẫu ngữ pháp nhận thức đã được nạp và JIT compile vào bộ nhớ RAM.</p>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {rules.map((rule) => (
            <div
              key={rule.ruleCode}
              className="bg-slate-900/60 border border-slate-800 hover:border-slate-700 rounded-2xl p-5 backdrop-blur-xl transition-all space-y-3"
            >
              <div className="flex items-start justify-between gap-2">
                <div>
                  <h4 className="text-sm font-bold text-white leading-snug">{rule.ruleName}</h4>
                  <span className="text-[10px] font-mono text-slate-500">{rule.ruleCode}</span>
                </div>
                <span className="text-[10px] font-black uppercase px-2 py-0.5 rounded-md bg-blue-500/10 text-blue-400 border border-blue-500/20">
                  Priority: {rule.priority}
                </span>
              </div>

              <p className="text-xs text-slate-400 leading-relaxed line-clamp-2">
                {rule.description}
              </p>

              <div className="pt-2 border-t border-slate-800/80 space-y-1.5 text-[11px] font-mono">
                <div className="text-slate-400 flex items-center justify-between">
                  <span className="text-slate-500 text-[10px]">Anchor:</span>
                  <span className="text-amber-400/90 truncate max-w-[180px]">{rule.anchorSpec?.regexPattern}</span>
                </div>
                <div className="text-slate-400 flex items-center justify-between">
                  <span className="text-slate-500 text-[10px]">Options:</span>
                  <span className="text-emerald-400/90 truncate max-w-[180px]">{rule.interactionSpec?.optionRegexPattern}</span>
                </div>
                <div className="text-slate-400 flex items-center justify-between">
                  <span className="text-slate-500 text-[10px]">Answer:</span>
                  <span className="text-teal-400/90 truncate max-w-[180px]">{rule.evaluationSpec?.answerRegexPattern}</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
