import { useState, useEffect } from 'react';
import { 
  X, Save, Sparkles, Layers, Wand2, CheckCheck, FileCode, Eye, 
  FolderPlus, Plus, Check, Tag, Compass, Globe, Building2, Calendar, 
  Award, Hash, ChevronDown, ChevronUp, SlidersHorizontal, Trash2,
  RotateCcw, Loader2
} from 'lucide-react';
import type { BankTopic } from '../types';
import { LETTERS } from '../types';
import { MathRenderer } from '../../../../components/common/MathRenderer';
import { learnerQuizService } from '../../../../services/learner-quiz.service';
import { CreateTopicModal } from './CreateTopicModal';
import { useAdaptiveTenant } from '../../../../hooks/useAdaptiveTenant';

export const DOMAIN_OPTIONS = [
  { code: 'GENERAL', label: '🌐 Tổng Hợp / Mặc Định', short: 'Tổng hợp', icon: '🌐', color: 'from-slate-500 to-gray-600', badge: 'bg-slate-800 text-slate-300 border-slate-700' },
  { code: 'EDUCATION', label: '🎓 Giáo Dục Phổ Thông & ĐH', short: 'Giáo dục', icon: '🎓', color: 'from-blue-500 to-indigo-600', badge: 'bg-blue-950/70 text-blue-300 border-blue-800/60' },
  { code: 'BANKING', label: '🏦 Ngân Hàng & Tài Chính', short: 'Ngân hàng', icon: '🏦', color: 'from-emerald-500 to-teal-600', badge: 'bg-emerald-950/70 text-emerald-300 border-emerald-800/60' },
  { code: 'HEALTHCARE', label: '🩺 Y Tế & Dược Phẩm', short: 'Y tế', icon: '🩺', color: 'from-rose-500 to-pink-600', badge: 'bg-rose-950/70 text-rose-300 border-rose-800/60' },
  { code: 'HSE', label: '🦺 An Toàn Lao Động & HSE', short: 'HSE', icon: '🦺', color: 'from-amber-500 to-yellow-600', badge: 'bg-amber-950/70 text-amber-300 border-amber-800/60' },
  { code: 'GOV_DRIVING', label: '🚗 Sát Hạch Lái Xe (GPLX)', short: 'GPLX', icon: '🚗', color: 'from-cyan-500 to-blue-600', badge: 'bg-cyan-950/70 text-cyan-300 border-cyan-800/60' },
  { code: 'IT_SECURITY', label: '💻 CNTT & An Toàn Thông Tin', short: 'CNTT', icon: '💻', color: 'from-violet-500 to-purple-600', badge: 'bg-violet-950/70 text-violet-300 border-violet-800/60' },
];

export const DOMAIN_SUGGESTED_TAGS: Record<string, string[]> = {
  EDUCATION: ['toan-hoc', 'thpt-quoc-gia', 'lop-12', 'lop-11', 'amsterdam', 'gdpt-2018', 'tieng-anh', 'doc-hieu', 'ngu-van', 'vat-ly', 'hoa-hoc'],
  BANKING: ['tin-dung', 'ngan-quy', 'aml-chong-rua-tien', 'kiem-soat-noi-bo', 'thanh-toan-quoc-te', 'thong-tu-41', 'basel-iii', 'vietcombank', 'bidv'],
  HEALTHCARE: ['bac-si-cki', 'dieu-duong', 'duoc-lam-sang', 'cap-cuu-hoi-suc', 'kiem-soat-nhiem-khuan', 'an-toan-nguoi-benh', 'bo-y-te', 'phac-do-2025'],
  HSE: ['an-toan-lao-dong', 'iso-45001', 'nhom-2', 'nhom-3', 'pccc', 'dien-cao-the', 'hoa-chat-doc-hai', 'khong-gian-kin'],
  GOV_DRIVING: ['gplx-b2', 'gplx-c', 'sa-hinh', 'bien-bao', 'diem-liet', 'nghi-dinh-100', 'luat-giao-thong-duong-bo', 'qcvn-41'],
  IT_SECURITY: ['iso-27001', 'owasp-top-10', 'soc-siem', 'devsecops', 'network-security', 'cloud-aws-azure', 'penetration-test'],
  GENERAL: ['nang-ngach', 'tuyen-dung', 'noi-quy', 'van-hoa-doanh-nghiep', 'ky-nang-mem'],
};

interface DocxPreviewModalProps {
  isOpen: boolean;
  isProcessing: boolean;
  isAiSolving?: boolean;
  isConvertingLatex?: boolean;
  docxPreviewList: any[];
  topics: BankTopic[];
  loading: boolean;
  setDocxPreviewList: (v: any[]) => void;
  onClose: () => void;
  onConfirmImport: () => Promise<void>;
  onAiSolveBatch?: () => Promise<void>;
  onConvertLatex?: () => Promise<void>;
  onTopicCreated?: (topic: BankTopic) => void;
}

// ── Smart Semantic Abbreviation Dictionary cho Frontend ────────────────────
const TOPIC_PHRASE_DICT: Record<string, string> = {
  'khach hang doanh nghiep vua va nho': 'KHDN_SME',
  'khach hang doanh nghiep': 'KHDN',
  'khach hang ca nhan': 'KHCN',
  'tai san bao dam': 'TSBD',
  'tham dinh tai san': 'TDTS',
  'ke toan giao dich noi bo': 'KT_GDNB',
  'ke toan giao dich': 'KTGD',
  'ke toan noi bo': 'KTNB',
  'ke toan tong hop': 'KTTH',
  'ke toan ngan hang': 'KTNH',
  'ke toan': 'KT',
  'kiem toan noi bo': 'KTNB',
  'giao dich vien': 'GDV',
  'kiem ngan vien': 'KNV',
  'kiem ngan': 'KN',
  'thu quy': 'TQ',
  'tien te kho quy': 'TTKQ',
  'thanh toan quoc te': 'TTQT',
  'tin dung quoc te': 'TDQT',
  'tin dung tieu dung': 'TDTD',
  'tin dung ban le': 'TDBL',
  'tin dung': 'TD',
  'cap tin dung': 'TD',
  'cho vay': 'CV',
  'quan tri rui ro tin dung': 'QTRR_TD',
  'quan tri rui ro': 'QTRR',
  'quan ly rui ro': 'QLRR',
  'phong chong rua tien': 'PCRT',
  'chong rua tien': 'AML',
  'nguon von': 'NV',
  'xu ly no': 'XLN',
  'thu hoi no': 'THN',
  'the tin dung': 'THE_TD',
  'dich vu the': 'DV_THE',
  'cong nghe thong tin': 'CNTT',
  'an toan thong tin': 'ATTT',
  'xu ly khieu nai': 'XLKN',
  'cham soc khach hang': 'CSKH',
  'ky nang ban hang': 'KNBH',
  'bao hiem': 'BH',
  'tot nghiep thpt': 'TN_THPT',
  'toan hoc': 'TOAN',
  'vat ly': 'LY',
  'hoa hoc': 'HOA',
  'tieng anh': 'ENG',
  'giay phep lai xe': 'GPLX',
  'sat hach lai xe': 'SHLX',
  'diem liet': 'DL',
  'sa hinh': 'SA_HINH',
  'bien bao': 'BIEN_BAO',
  'hang b2': 'B2',
  'hang c': 'C',
};

const STOP_WORDS_SET = new Set([
  've', 'va', 'cua', 'cac', 'nhung', 'cho', 'trong', 'theo', 'voi', 'boi',
  'quy', 'dinh', 'huong', 'dan', 'nghiep', 'vu', 'ngan', 'hang', 'cau', 'hoi',
  'bo', 'de', 'thi', 'tuyen', 'dung', 'sat', 'hach', 'kiem', 'tra', 'tai', 'lieu'
]);

// Hàm sinh mã code chuẩn tự động từ tên chủ đề (Smart Semantic Abbreviation Engine)
export function generateTopicCode(name: string, maxLen = 14): string {
  if (!name) return '';
  // Xóa STT đầu dòng ("01.", "1 -", "Bài 2: "...)
  const clean = name.replace(/^(?:bài|phần|chương|đề|câu|stt)?\s*\d+[\.\:\-\s_]+\s*/i, '').trim();

  // Bỏ dấu tiếng Việt
  let nonAccent = clean
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[đĐ]/g, 'd')
    .toLowerCase();

  // Tách đợt / năm
  let periodTag = '';
  const mDot = nonAccent.match(/(?:dot|d)\s*0?(\d+)\b/i);
  const mYear = nonAccent.match(/\b20(2[0-9])\b/);
  if (mDot && mYear) periodTag = `D${mDot[1]}_${mYear[1]}`;
  else if (mDot) periodTag = `D${mDot[1]}`;
  else if (mYear) periodTag = mYear[1];

  nonAccent = nonAccent
    .replace(/(?:dot|d)\s*0?\d+\b/gi, ' ')
    .replace(/\b202[0-9]\b/g, ' ')
    .replace(/(?:nam|năm)\s*/gi, ' ');

  const tokens: string[] = [];
  let remaining = nonAccent;

  // Khớp từ điển cụm từ nghiệp vụ
  for (const [phrase, abbr] of Object.entries(TOPIC_PHRASE_DICT)) {
    if (remaining.includes(phrase)) {
      tokens.push(abbr);
      remaining = remaining.replace(phrase, ' ');
    }
  }

  // Tách các từ còn lại, lọc stopwords
  const words = remaining
    .replace(/[^a-z0-9\s_-]/g, ' ')
    .split(/[\s_-]+/)
    .filter(w => w.length > 1 && !STOP_WORDS_SET.has(w));

  if (words.length > 0) {
    if (tokens.length === 0) {
      if (words.length === 1) {
        tokens.push(words[0].toUpperCase());
      } else if (words.length === 2) {
        tokens.push(words[0].slice(0, 3).toUpperCase());
        tokens.push(words[1].slice(0, 3).toUpperCase());
      } else {
        // >= 3 từ: tạo từ ghép chữ cái đầu
        tokens.push(words.map(w => w[0].toUpperCase()).join(''));
      }
    } else if (tokens.length <= 2 && words.length <= 3) {
      const subAcronym = words.map(w => w[0].toUpperCase()).join('');
      if (subAcronym.length >= 2) tokens.push(subAcronym);
    }
  }

  if (periodTag) tokens.push(periodTag);
  if (tokens.length === 0) tokens.push('TOPIC');

  const uniqueTokens = Array.from(new Set(tokens));
  let result = uniqueTokens.join('_').replace(/_+/g, '_').replace(/^_|_$/g, '');
  if (result.length > maxLen) {
    result = result.slice(0, maxLen).replace(/_$/, '');
  }
  return result.toUpperCase();
}

export function DocxPreviewModal({
  isOpen, isProcessing, isAiSolving, isConvertingLatex, docxPreviewList, topics, loading,
  setDocxPreviewList, onClose, onConfirmImport, onAiSolveBatch, onConvertLatex, onTopicCreated,
}: DocxPreviewModalProps) {
  const { domains } = useAdaptiveTenant();
  const activeDomains = domains && domains.length > 0
    ? domains.map((d: any) => ({
        code: d.code,
        label: d.label || d.name,
        short: (d.short || d.name || d.code).length > 15 ? (d.short || d.name || d.code).slice(0, 13) + '...' : (d.short || d.name || d.code),
        icon: d.icon || '🌐',
        color: 'from-purple-500 to-indigo-600',
        badge: 'bg-purple-950/70 text-purple-300 border-purple-800/60'
      }))
    : DOMAIN_OPTIONS;

  const [localTopics, setLocalTopics] = useState<BankTopic[]>(topics);
  const [bulkTopic, setBulkTopic] = useState<string>(topics.length > 0 ? topics[0].code : 'PARTY_BUILDING');
  const [bulkDifficulty, setBulkDifficulty] = useState<number>(3);
  const [showRawOptions, setShowRawOptions] = useState<Record<number, boolean>>({});
  const [selectedExamFilter, setSelectedExamFilter] = useState<string>('ALL');

  // Trạng thái Universal Multi-Industry & Smart Tags
  const [bulkDomain, setBulkDomain] = useState<string>('GENERAL');
  const [bulkTagInput, setBulkTagInput] = useState<string>('');
  const [showBulkTaxonomyModal, setShowBulkTaxonomyModal] = useState<boolean>(false);
  const [showAdvancedBulkTools, setShowAdvancedBulkTools] = useState<boolean>(false);
  const [expandedCoords, setExpandedCoords] = useState<Record<number, boolean>>({});
  const [tagInputPerQ, setTagInputPerQ] = useState<Record<number, string>>({});

  // Trạng thái Form trong BulkTaxonomyModal
  const [modalDomain, setModalDomain] = useState<string>('EDUCATION');
  const [modalTargetLevel, setModalTargetLevel] = useState<string>('');
  const [modalAssessmentPurpose, setModalAssessmentPurpose] = useState<string>('');
  const [modalIssuingOrg, setModalIssuingOrg] = useState<string>('');
  const [modalBenchmarkYear, setModalBenchmarkYear] = useState<string>('2025');
  const [modalBenchmarkStandard, setModalBenchmarkStandard] = useState<string>('');
  const [modalTags, setModalTags] = useState<string[]>([]);
  const [modalNewTag, setModalNewTag] = useState<string>('');
  const [modalOnlyUnassigned, setModalOnlyUnassigned] = useState<boolean>(false);

  // Trạng thái Quick Topic Creator Modal
  const [showCreateTopicModal, setShowCreateTopicModal] = useState(false);
  const [targetQuestionIdx, setTargetQuestionIdx] = useState<number | null>(null);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  // Trạng thái chuẩn hóa chính tả tiếng Việt On-Demand
  const [isCorrectingVN, setIsCorrectingVN] = useState<boolean>(false);
  const [rawOriginalList, setRawOriginalList] = useState<any[] | null>(null);

  // On-Demand Chuẩn hóa Tiếng Việt (VNCorrect & Maprepl theo 670+ quy tắc thực chiến)
  const handleCorrectVietnamese = async () => {
    if (!docxPreviewList || docxPreviewList.length === 0) return;
    setIsCorrectingVN(true);
    try {
      if (!rawOriginalList) {
        setRawOriginalList([...docxPreviewList]);
      }
      const corrected = await learnerQuizService.correctVietnameseQuestions(docxPreviewList);
      if (corrected && Array.isArray(corrected)) {
        setDocxPreviewList(corrected);
        showToast(`✨ Đã chuẩn hóa chính tả tiếng Việt & Maprepl cho ${corrected.length} câu hỏi!`);
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'Lỗi chuẩn hóa tiếng Việt. Vui lòng thử lại.');
    } finally {
      setIsCorrectingVN(false);
    }
  };

  // Hoàn tác về văn bản gốc ban đầu của file
  const handleRevertRawText = () => {
    if (rawOriginalList) {
      setDocxPreviewList([...rawOriginalList]);
      setRawOriginalList(null);
      showToast('Đã hoàn tác về văn bản gốc ban đầu của file!');
    }
  };

  // Đồng bộ topics khi prop từ bên ngoài thay đổi
  useEffect(() => {
    setLocalTopics(topics);
    if (topics.length > 0 && (!bulkTopic || !topics.some(t => t.code === bulkTopic))) {
      setBulkTopic(topics[0].code);
    }
  }, [topics]);

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3500);
  };

  const openCreateTopicModal = (qIdx: number | null = null) => {
    setTargetQuestionIdx(qIdx);
    setShowCreateTopicModal(true);
  };

  if (!isOpen && !isProcessing && !isAiSolving && !isConvertingLatex) return null;

  // Nhận dạng các nhóm đề thi (e.g. TOAN_DE_01, TOAN_DE_02...)
  const examGroups = Array.from(new Set(docxPreviewList.map(q => q.topicCode || 'GENERAL'))).filter(Boolean).sort();
  const hasMultipleExams = examGroups.length > 1;

  const displayedList = selectedExamFilter === 'ALL'
    ? docxPreviewList
    : docxPreviewList.filter(q => (q.topicCode || 'GENERAL') === selectedExamFilter);

  // Fullscreen AI processing overlay
  if (isProcessing || isAiSolving || isConvertingLatex) {
    return (
      <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-md z-50 flex flex-col items-center justify-center space-y-4 text-white">
        <div className="animate-spin text-5xl mb-4 text-purple-400">🌀</div>
        <h3 className="text-lg font-bold text-slate-200">
          {isConvertingLatex
            ? 'Gemini Vision đang OCR & chuyển đổi công thức toán sang mã LaTeX ($...$)...'
            : isAiSolving
            ? 'Gemini AI đang giải đáp án & trích dẫn quy định...'
            : 'Đang đọc và phân tích cấu trúc tệp PDF / Word...'}
        </h3>
        <p className="text-xs text-slate-500 max-w-sm text-center leading-relaxed">
          Quá trình có thể mất từ 5-15 giây tùy số lượng câu hỏi và độ phức tạp của công thức. Vui lòng giữ trình duyệt mở!
        </p>
      </div>
    );
  }

  if (!isOpen) return null;

  const hasFormulaImages = docxPreviewList.some(q => 
    q.optionType === 'image' || 
    (q.options && q.options.some((o: string) => o?.includes('data:image/'))) || 
    q.content?.includes('data:image/')
  );

  const updateItem = (idx: number, field: string, value: any) => {
    const updated = [...docxPreviewList];
    updated[idx] = { ...updated[idx], [field]: value };
    setDocxPreviewList(updated);
  };

  // Áp dụng chủ đề hàng loạt cho toàn bộ câu hỏi
  const handleApplyBulkTopic = (onlyUnassigned: boolean) => {
    if (!bulkTopic) return;
    const updated = docxPreviewList.map(item => {
      if (onlyUnassigned) {
        if (!item.topicCode || item.topicCode === 'GENERAL' || item.topicCode === 'PARTY_BUILDING') {
          return { ...item, topicCode: bulkTopic };
        }
        return item;
      }
      return { ...item, topicCode: bulkTopic };
    });
    setDocxPreviewList(updated);
  };

  // Áp dụng độ khó hàng loạt
  const handleApplyBulkDifficulty = () => {
    const updated = docxPreviewList.map(item => ({ ...item, difficulty: bulkDifficulty }));
    setDocxPreviewList(updated);
  };

  // Tự động nhận diện chủ đề thông minh theo từ khóa danh mục
  const handleAutoClassifyTopics = () => {
    const updated = docxPreviewList.map(item => {
      const text = `${item.content} ${item.options?.join(' ') || ''}`.toLowerCase();
      let bestCode = item.topicCode || (topics.length > 0 ? topics[0].code : 'GENERAL');

      for (const t of topics) {
        const tName = t.name.toLowerCase();
        const tCode = t.code.toLowerCase();
        if (text.includes(tCode) || text.includes(tName)) {
          bestCode = t.code;
          break;
        }
      }

      if (text.includes('kho tiền') || text.includes('chìa khóa') || text.includes('niêm phong') || text.includes('kiểm đếm')) {
        const found = topics.find(t => t.code === '381' || t.name.toLowerCase().includes('kế toán') || t.name.toLowerCase().includes('ngân quỹ'));
        if (found) bestCode = found.code;
      } else if (text.includes('tín dụng') || text.includes('cho vay') || text.includes('thế chấp') || text.includes('cic') || text.includes('nợ xấu')) {
        const found = topics.find(t => t.code === '382' || t.name.toLowerCase().includes('tín dụng') || t.name.toLowerCase().includes('cho vay'));
        if (found) bestCode = found.code;
      } else if (text.includes('đảng') || text.includes('chi bộ') || text.includes('đảng viên') || text.includes('quy định 69')) {
        const found = topics.find(t => t.code === 'PARTY_BUILDING' || t.name.toLowerCase().includes('đảng'));
        if (found) bestCode = found.code;
      } else if (text.includes('mật khẩu') || text.includes('giao thức') || text.includes('itsm') || text.includes('cntt') || text.includes('bảo mật')) {
        const found = topics.find(t => t.code === 'CNTT' || t.name.toLowerCase().includes('công nghệ') || t.name.toLowerCase().includes('cntt'));
        if (found) bestCode = found.code;
      }

      return { ...item, topicCode: bestCode };
    });

    setDocxPreviewList(updated);
    showToast('Đã nhận diện chủ đề theo từ khóa!');
  };

  // Áp dụng Miền ngành nghề hàng loạt
  const handleApplyBulkDomain = () => {
    if (!bulkDomain) return;
    const updated = docxPreviewList.map(item => ({ ...item, domainCode: bulkDomain }));
    setDocxPreviewList(updated);
    const domainObj = activeDomains.find(d => d.code === bulkDomain);
    showToast(`Đã áp dụng ngành '${domainObj?.label || bulkDomain}' cho tất cả ${docxPreviewList.length} câu hỏi!`);
  };

  // Áp dụng đồng thời cả Chủ đề, Độ khó, Ngành nghề cho tất cả câu hỏi trong 1 click
  const handleApplyAllCore = () => {
    const updated = docxPreviewList.map(item => ({
      ...item,
      topicCode: bulkTopic || item.topicCode,
      difficulty: bulkDifficulty || item.difficulty,
      domainCode: bulkDomain || item.domainCode,
    }));
    setDocxPreviewList(updated);
    showToast(`Đã áp dụng Chủ đề, Độ khó & Ngành cho toàn bộ ${docxPreviewList.length} câu hỏi!`);
  };

  // Xóa câu hỏi khỏi danh sách xem trước
  const handleDeleteItem = (targetIdx: number) => {
    const updated = docxPreviewList.filter((_, i) => i !== targetIdx);
    setDocxPreviewList(updated);
  };

  // Thêm thẻ hàng loạt cho tất cả câu hỏi
  const handleAddBulkTag = () => {
    const raw = bulkTagInput.trim().replace(/^#/, '');
    if (!raw) return;
    const updated = docxPreviewList.map(item => {
      const currentTags = Array.isArray(item.tags) ? [...item.tags] : [];
      if (!currentTags.includes(raw)) {
        currentTags.push(raw);
      }
      return { ...item, tags: currentTags };
    });
    setDocxPreviewList(updated);
    setBulkTagInput('');
    showToast(`Đã gán thẻ '#${raw}' cho tất cả ${docxPreviewList.length} câu hỏi!`);
  };

  // Tự động nhận diện toàn diện Hệ 5 Trục Tọa Độ & Thẻ Thông Minh
  const handleAutoClassifyTaxonomyAndTags = () => {
    const globalSniffed = sniffCoordinatesFromDocxList();
    let matched = 0;
    const updated = docxPreviewList.map(item => {
      const text = `${item.content || ''} ${item.options?.join(' ') || ''} ${item.topicCode || ''} ${item.contextContent || ''}`.toLowerCase();
      let domain = item.domainCode && item.domainCode !== 'GENERAL' ? item.domainCode : globalSniffed.domain;
      let tags = Array.isArray(item.tags) ? [...item.tags] : [];
      let targetLevel = item.targetLevel || globalSniffed.targetLevel;
      let assessmentPurpose = item.assessmentPurpose || globalSniffed.assessmentPurpose;
      let issuingOrg = item.issuingOrg || globalSniffed.issuingOrg;
      let benchmarkYear = item.benchmarkYear || (globalSniffed.benchmarkYear ? parseInt(globalSniffed.benchmarkYear) : 2026);
      let benchmarkStandard = item.benchmarkStandard || globalSniffed.benchmarkStandard;

      const addTag = (t: string) => {
        if (!tags.includes(t)) tags.push(t);
      };

      globalSniffed.tags.forEach(t => addTag(t));

      // 1. Sát hạch Giao thông / GPLX
      if (item.subCategory === 'SA_HINH' || item.subCategory === 'BIEN_BAO' || item.subCategory === 'LUAT' ||
          text.includes('sa hình') || text.includes('biển báo') || text.includes('vượt ẩu') ||
          text.includes('tốc độ tối đa') || text.includes('gplx') || text.includes('xe cơ giới') || text.includes('đường bộ')) {
        domain = 'GOV_DRIVING';
        issuingOrg = issuingOrg || 'Cục Đường bộ Việt Nam';
        assessmentPurpose = assessmentPurpose || 'Sát hạch Giấy phép Lái xe (GPLX)';
        benchmarkStandard = benchmarkStandard || 'Nghị định 100/2019/NĐ-CP';
        benchmarkYear = benchmarkYear || 2023;
        addTag('gplx');
        addTag('sat-hach-lai-xe');
        if (text.includes('sa hình') || item.subCategory === 'SA_HINH') addTag('sa-hinh');
        if (text.includes('biển báo') || item.subCategory === 'BIEN_BAO') addTag('bien-bao');
        if (item.isCritical) addTag('diem-liet');
        matched++;
      }
      // 2. Toán học / Giáo dục THPT
      else if (text.includes('đạo hàm') || text.includes('tích phân') || text.includes('nguyên hàm') ||
               text.includes('bảng biến thiên') || text.includes('hàm số') || text.includes('hình nón') ||
               text.includes('toan_de_') || text.includes('toán') || text.includes('gdpt')) {
        domain = 'EDUCATION';
        targetLevel = targetLevel || 'Lớp 12';
        assessmentPurpose = assessmentPurpose || 'Kỳ thi Tốt nghiệp THPT Quốc Gia';
        benchmarkStandard = benchmarkStandard || 'Chương trình GDPT 2018';
        benchmarkYear = benchmarkYear || 2025;
        addTag('toan-hoc');
        addTag('tot-nghiep-2025');
        addTag('gdpt-2018');
        addTag('lop-12');
        matched++;
      }
      // 3. Tiếng Anh / Ngoại ngữ
      else if (text.includes('reading passage') || text.includes('pronunciation') || text.includes('closest in meaning') ||
               text.includes('opposite in meaning') || text.includes('global warming') || text.includes('tiếng anh')) {
        domain = 'EDUCATION';
        targetLevel = targetLevel || 'Lớp 12';
        assessmentPurpose = assessmentPurpose || 'Kỳ thi Tốt nghiệp THPT Quốc Gia';
        benchmarkStandard = benchmarkStandard || 'Chương trình GDPT 2018';
        benchmarkYear = benchmarkYear || 2025;
        addTag('tieng-anh');
        addTag('doc-hieu');
        addTag('tot-nghiep-2025');
        matched++;
      }
      // 4. Ngân hàng & Tài chính
      else if (text.includes('kho tiền') || text.includes('tín dụng') || text.includes('ngân quỹ') ||
               text.includes('rửa tiền') || text.includes('thế chấp') || text.includes('cic') ||
               text.includes('vcb') || text.includes('vietcombank') || text.includes('aml')) {
        domain = 'BANKING';
        targetLevel = targetLevel || 'Chuyên viên chính';
        assessmentPurpose = assessmentPurpose || 'Sát hạch Nghiệp vụ Tuân thủ AML Định kỳ';
        benchmarkStandard = benchmarkStandard || 'Thông tư 41 & Basel III';
        benchmarkYear = benchmarkYear || 2025;
        issuingOrg = issuingOrg || 'Ngân hàng TMCP Ngoại thương (Vietcombank)';
        addTag('ngan-hang');
        addTag('aml');
        addTag('vcb');
        matched++;
      }
      // 5. Y tế & Dược phẩm
      else if (text.includes('bác sĩ') || text.includes('dược lâm sàng') || text.includes('phác đồ điều trị') ||
               text.includes('bệnh viện') || text.includes('bộ y tế') || text.includes('xét nghiệm')) {
        domain = 'HEALTHCARE';
        targetLevel = targetLevel || 'Bác sĩ CKI';
        assessmentPurpose = assessmentPurpose || 'Sát hạch Chuyên môn Y khoa';
        benchmarkStandard = benchmarkStandard || 'Phác đồ Bộ Y tế 2025';
        benchmarkYear = benchmarkYear || 2025;
        addTag('y-te');
        addTag('duoc-hoc');
        matched++;
      }
      // 6. HSE & An toàn Lao động
      else if (text.includes('an toàn lao động') || text.includes('bảo hộ lao động') || text.includes('iso 45001') ||
               text.includes('pccc') || text.includes('phòng cháy')) {
        domain = 'HSE';
        targetLevel = targetLevel || 'Nhóm 2 / Nhóm 3';
        assessmentPurpose = assessmentPurpose || 'Huấn luyện An toàn VSLĐ định kỳ';
        benchmarkStandard = benchmarkStandard || 'ISO 45001:2018';
        benchmarkYear = benchmarkYear || 2025;
        addTag('an-toan-lao-dong');
        addTag('hse');
        matched++;
      }
      // 7. CNTT & An toàn Thông tin
      else if (text.includes('an toàn thông tin') || text.includes('iso 27001') || text.includes('owasp') ||
               text.includes('mật khẩu') || text.includes('phishing') || text.includes('cntt')) {
        domain = 'IT_SECURITY';
        targetLevel = targetLevel || 'Chuyên viên ATTT';
        assessmentPurpose = assessmentPurpose || 'Tuân thủ An ninh mạng & Bảo mật';
        benchmarkStandard = benchmarkStandard || 'ISO/IEC 27001:2022';
        benchmarkYear = benchmarkYear || 2025;
        addTag('an-toan-thong-tin');
        addTag('iso-27001');
        matched++;
      }

      return {
        ...item,
        domainCode: domain,
        tags,
        targetLevel,
        assessmentPurpose,
        issuingOrg,
        benchmarkYear,
        benchmarkStandard
      };
    });

    setDocxPreviewList(updated);
    showToast(`✨ Đã tự động nhận diện và gán Ngành nghề & Smart Tags cho ${updated.length} câu hỏi!`);
  };

  // Thuật toán nhận diện thông minh Hệ 5 Trục Tọa Độ & Smart Tags từ tiêu đề và nội dung file
  const sniffCoordinatesFromDocxList = () => {
    const sample = docxPreviewList.find(q => q.targetLevel || q.assessmentPurpose || q.issuingOrg || (q.domainCode && q.domainCode !== 'GENERAL')) || docxPreviewList[0];

    const allSampleText = docxPreviewList.slice(0, 30).map(q => 
      `${q.contextTitle || ''} ${q.contextContent || ''} ${q.content || ''} ${q.options?.join(' ') || ''}`
    ).join('\n');

    let domain = sample?.domainCode && sample.domainCode !== 'GENERAL' ? sample.domainCode : 'BANKING';
    let targetLevel = sample?.targetLevel || '';
    let assessmentPurpose = sample?.assessmentPurpose || '';
    let issuingOrg = sample?.issuingOrg || '';
    let benchmarkYear = sample?.benchmarkYear ? String(sample.benchmarkYear) : '2026';
    let benchmarkStandard = sample?.benchmarkStandard || '';
    const sniffedTags = new Set<string>(sample?.tags || []);

    // 1. Vị trí công tác / Cấp bậc
    const posMatch = allSampleText.match(/(?:VỊ\s*TRÍ|CHỨC\s*DANH|CẤP\s*BẬC)\s*[:：]\s*([^\r\n,;]+)/i);
    if (posMatch && posMatch[1]) {
      targetLevel = posMatch[1].trim().replace(/\s+/g, ' ');
    } else if (/tín\s*dụng\s*khách\s*hàng\s*doanh\s*nghiệp|tín\s*dụng\s*khdn/i.test(allSampleText)) {
      targetLevel = 'Tín dụng Khách hàng Doanh nghiệp';
    }

    // 2. Mục đích sát hạch / Tiêu đề kỳ kiểm tra
    const purposeMatch = allSampleText.match(/(?:BỘ\s*CÂU\s*HỎI[\s,]*(?:ĐÁP\s*ÁN\s*)?)?(?:ÔN\s*TẬP\s*)?(KỲ\s*KIỂM\s*TRA[^\r\n,;]+(?:NĂM\s*20\d\d)?)/i)
      || allSampleText.match(/(KIỂM\s*TRA\s*CHUYÊN\s*MÔN\s*NGHIỆP\s*VỤ[^\r\n,;]+)/i)
      || allSampleText.match(/(SÁT\s*HẠCH[^\r\n,;]+)/i)
      || allSampleText.match(/(THI\s*TỐT\s*NGHIỆP[^\r\n,;]+)/i);
    if (purposeMatch && purposeMatch[1]) {
      assessmentPurpose = purposeMatch[1].trim().replace(/\s+/g, ' ');
    } else if (/kiểm\s*tra\s*chuyên\s*môn\s*nghiệp\s*vụ\s*định\s*kỳ/i.test(allSampleText)) {
      assessmentPurpose = 'Kiểm tra chuyên môn nghiệp vụ định kỳ tại chi nhánh Đợt 2';
    }

    // 3. Năm ban hành
    const yearMatch = allSampleText.match(/NĂM\s*(20\d\d)/i) || allSampleText.match(/\b(202[4-9])\b/);
    if (yearMatch && yearMatch[1]) {
      benchmarkYear = yearMatch[1].trim();
    }

    // 4. Cơ quan / Chi nhánh
    if (/CHI\s*NHÁNH/i.test(allSampleText)) {
      issuingOrg = 'Chi nhánh';
      if (/AGRIBANK|NÔNG\s*NGHIỆP/i.test(allSampleText)) issuingOrg = 'Agribank (Chi nhánh)';
      else if (/VIETCOMBANK|VCB/i.test(allSampleText)) issuingOrg = 'Vietcombank (Chi nhánh)';
      else if (/BIDV/i.test(allSampleText)) issuingOrg = 'BIDV (Chi nhánh)';
    }

    // 5. Chuẩn mực quy chiếu / Quy định
    const stdMatch = allSampleText.match(/(Quy\s*định\s*(?:số\s*)?[\d/]+[^\r\n,;]*)/i)
      || allSampleText.match(/(Quy\s*chế[^\r\n,;]*)/i)
      || allSampleText.match(/(Thông\s*tư\s*(?:số\s*)?[\d/]+[^\r\n,;]*)/i);
    if (stdMatch && stdMatch[1]) {
      benchmarkStandard = stdMatch[1].trim();
    } else if (assessmentPurpose) {
      benchmarkStandard = `Quy chế kiểm tra nghiệp vụ định kỳ Đợt 2/${benchmarkYear || '2026'}`;
    }

    // 6. Miền ngành
    if (/tín\s*dụng|khdn|khách\s*hàng\s*doanh\s*nghiệp|ngân\s*hàng|ngân\s*quỹ|cho\s*vay|thế\s*chấp|cic|bảo\s*lãnh|chi\s*nhánh/i.test(allSampleText)) {
      domain = 'BANKING';
    } else if (/toán|hình\s*học|tích\s*phân|đạo\s*hàm|vật\s*lý|hóa\s*học|tiếng\s*anh|thpt/i.test(allSampleText)) {
      domain = 'EDUCATION';
    } else if (/sa\s*hình|biển\s*báo|gplx|lái\s*xe|đường\s*bộ/i.test(allSampleText)) {
      domain = 'GOV_DRIVING';
    } else if (/an\s*toàn\s*lao\s*động|pccc|bảo\s*hộ|iso\s*45001/i.test(allSampleText)) {
      domain = 'HSE';
    } else if (/bác\s*sĩ|bệnh\s*viện|dược|phác\s*đồ/i.test(allSampleText)) {
      domain = 'HEALTHCARE';
    } else if (/an\s*toàn\s*thông\s*tin|mật\s*khẩu|iso\s*27001|owasp/i.test(allSampleText)) {
      domain = 'IT_SECURITY';
    }

    // 7. Thẻ thông minh
    if (domain === 'BANKING') sniffedTags.add('ngan-hang');
    if (/tín\s*dụng/i.test(allSampleText)) sniffedTags.add('tin-dung');
    if (/khdn|khách\s*hàng\s*doanh\s*nghiệp/i.test(allSampleText)) {
      sniffedTags.add('tin-dung-khdn');
      sniffedTags.add('khdn');
    }
    if (/định\s*kỳ/i.test(allSampleText)) sniffedTags.add('kiem-tra-dinh-ky');
    if (/đợt\s*2/i.test(allSampleText)) sniffedTags.add('dot-2-2026');
    if (benchmarkYear) sniffedTags.add(`nam-${benchmarkYear}`);
    if (issuingOrg) sniffedTags.add('chi-nhanh');

    docxPreviewList.forEach(q => {
      if (Array.isArray(q.tags)) {
        q.tags.forEach((t: string) => {
          if (t && t.trim()) sniffedTags.add(t.trim().replace(/^#/, ''));
        });
      }
    });

    return {
      domain,
      targetLevel,
      assessmentPurpose,
      issuingOrg,
      benchmarkYear,
      benchmarkStandard,
      tags: Array.from(sniffedTags).filter(Boolean),
    };
  };

  // Mở modal cấu hình đa ngành hàng loạt - Tự động điền tọa độ nhận diện từ tiêu đề/file
  const handleOpenBulkTaxonomyModal = () => {
    const sniffed = sniffCoordinatesFromDocxList();
    setModalDomain(sniffed.domain || bulkDomain || 'BANKING');
    setModalTargetLevel(sniffed.targetLevel || '');
    setModalAssessmentPurpose(sniffed.assessmentPurpose || '');
    setModalIssuingOrg(sniffed.issuingOrg || '');
    setModalBenchmarkYear(sniffed.benchmarkYear || '2026');
    setModalBenchmarkStandard(sniffed.benchmarkStandard || '');
    setModalTags(sniffed.tags || []);
    setModalNewTag('');
    setShowBulkTaxonomyModal(true);
  };

  // Tự động nhận diện lại trong modal bằng AI
  const handleAutoSniffModalCoordinates = () => {
    const sniffed = sniffCoordinatesFromDocxList();
    setModalDomain(sniffed.domain || 'BANKING');
    setModalTargetLevel(sniffed.targetLevel || '');
    setModalAssessmentPurpose(sniffed.assessmentPurpose || '');
    setModalIssuingOrg(sniffed.issuingOrg || '');
    setModalBenchmarkYear(sniffed.benchmarkYear || '2026');
    setModalBenchmarkStandard(sniffed.benchmarkStandard || '');
    setModalTags(sniffed.tags || []);
    showToast('✨ Đã tự động nhận diện Hệ 5 Trục Tọa Độ & Smart Tags từ tiêu đề file!');
  };

  // Áp dụng modal cấu hình đa ngành hàng loạt
  const handleApplyBulkTaxonomyModal = () => {
    const updated = docxPreviewList.map(item => {
      if (modalOnlyUnassigned && item.domainCode && item.domainCode !== 'GENERAL') {
        return item;
      }

      const mergedTags = Array.isArray(item.tags) ? [...item.tags] : [];
      modalTags.forEach(t => {
        if (!mergedTags.includes(t)) mergedTags.push(t);
      });

      return {
        ...item,
        domainCode: modalDomain,
        targetLevel: modalTargetLevel.trim() || item.targetLevel,
        assessmentPurpose: modalAssessmentPurpose.trim() || item.assessmentPurpose,
        issuingOrg: modalIssuingOrg.trim() || item.issuingOrg,
        benchmarkYear: modalBenchmarkYear ? parseInt(modalBenchmarkYear) : item.benchmarkYear,
        benchmarkStandard: modalBenchmarkStandard.trim() || item.benchmarkStandard,
        tags: mergedTags
      };
    });

    setDocxPreviewList(updated);
    setShowBulkTaxonomyModal(false);
    showToast(`Đã thiết lập Đa Ngành & Smart Tags cho ${updated.length} câu hỏi!`);
  };

  // Thêm tag vào một câu hỏi cụ thể
  const handleAddTagToQuestion = (idx: number, rawTag: string) => {
    const cleanTag = rawTag.trim().replace(/^#/, '');
    if (!cleanTag) return;
    const current = docxPreviewList[idx];
    const currentTags = Array.isArray(current.tags) ? [...current.tags] : [];
    if (!currentTags.includes(cleanTag)) {
      currentTags.push(cleanTag);
      updateItem(idx, 'tags', currentTags);
    }
    setTagInputPerQ(prev => ({ ...prev, [idx]: '' }));
  };

  // Xóa tag khỏi một câu hỏi cụ thể
  const handleRemoveTagFromQuestion = (idx: number, tagToRemove: string) => {
    const current = docxPreviewList[idx];
    const currentTags = Array.isArray(current.tags) ? [...current.tags] : [];
    const filtered = currentTags.filter(t => t !== tagToRemove);
    updateItem(idx, 'tags', filtered);
  };

  // Toggle mở rộng tọa độ cho một câu hỏi
  const toggleExpandCoords = (idx: number) => {
    setExpandedCoords(prev => ({ ...prev, [idx]: !prev[idx] }));
  };

  return (
    <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-md z-50 flex items-center justify-center p-4">
      <div className="w-full max-w-6xl h-[92vh] bg-slate-900 border border-slate-800 rounded-3xl p-4 sm:p-6 shadow-2xl flex flex-col justify-between relative overflow-hidden font-sans text-white">
        <button onClick={onClose} className="absolute top-4 right-4 text-slate-400 hover:text-white p-1 hover:bg-slate-800 rounded-lg transition-colors cursor-pointer z-10">
          <X size={20} />
        </button>

        {/* Compact 1-Line Header Bar */}
        <div className="flex items-center justify-between pb-2 mb-2 border-b border-slate-800/80 pr-8 shrink-0">
          <div className="flex items-center gap-2.5">
            <span className="w-7 h-7 rounded-lg bg-gradient-to-br from-purple-500/20 to-blue-500/20 border border-purple-500/30 flex items-center justify-center text-purple-300">
              <Sparkles size={13} />
            </span>
            <h2 className="text-base sm:text-lg font-black text-white tracking-tight flex items-center gap-2">
              Xem Trước & Duyệt Câu Hỏi
              <span className="px-2 py-0.5 text-xs font-black rounded-full bg-blue-500/20 text-blue-300 border border-blue-500/30">
                {docxPreviewList.length} câu
              </span>
            </h2>
            <span className="hidden lg:inline text-[11px] text-slate-500 border-l border-slate-800 pl-2.5">
              Word / Excel / PDF / Gemini AI
            </span>
          </div>

          <div className="flex items-center gap-2 text-xs">
            <span className="px-2 py-0.5 bg-blue-950/60 text-blue-300 border border-blue-800/40 rounded-lg text-[11px] font-bold">
              Trắc nghiệm: {docxPreviewList.filter(q => q.questionType !== 'ESSAY').length}
            </span>
            <span className="px-2 py-0.5 bg-amber-950/60 text-amber-300 border border-amber-800/40 rounded-lg text-[11px] font-bold">
              Tự luận: {docxPreviewList.filter(q => q.questionType === 'ESSAY').length}
            </span>
          </div>
        </div>

        {/* Master Control Capsule (Single-Row Harmonious Design) */}
        <div className="bg-slate-950/80 border border-slate-800/90 rounded-2xl p-2.5 mb-2.5 shadow-inner shrink-0">
          <div className="flex flex-wrap items-center justify-between gap-2 text-xs">
            {/* Core Bulk Selectors */}
            <div className="flex flex-wrap items-center gap-1.5 sm:gap-2">
              {/* Chủ đề */}
              <div className="flex items-center gap-1 bg-slate-900 border border-slate-800 rounded-xl px-2 py-1">
                <Layers size={13} className="text-blue-400 shrink-0" />
                <select
                  value={bulkTopic}
                  onChange={e => {
                    if (e.target.value === '__CREATE_NEW__') {
                      openCreateTopicModal(null);
                    } else {
                      setBulkTopic(e.target.value);
                    }
                  }}
                  className="bg-transparent text-slate-200 text-xs font-medium focus:outline-none cursor-pointer max-w-[150px] sm:max-w-[180px] truncate"
                >
                  {localTopics.map(t => <option key={t.id || t.code} value={t.code} className="bg-slate-900">{t.name} ({t.code})</option>)}
                  <option value="__CREATE_NEW__" className="text-amber-400 font-bold bg-slate-950">➕ Tạo chủ đề mới...</option>
                </select>
                <button
                  type="button"
                  onClick={() => openCreateTopicModal(null)}
                  className="p-0.5 text-slate-400 hover:text-white rounded hover:bg-slate-800 transition-colors"
                  title="Tạo chủ đề mới"
                >
                  <Plus size={12} />
                </button>
              </div>

              {/* Độ khó */}
              <div className="flex items-center gap-1 bg-slate-900 border border-slate-800 rounded-xl px-2 py-1">
                <span className="text-[11px] text-slate-400 font-semibold">Độ khó:</span>
                <select
                  value={bulkDifficulty}
                  onChange={e => setBulkDifficulty(parseInt(e.target.value))}
                  className="bg-transparent text-slate-200 text-xs font-medium focus:outline-none cursor-pointer"
                >
                  {[1, 2, 3, 4, 5].map(d => <option key={d} value={d} className="bg-slate-900">Mức {d}</option>)}
                </select>
              </div>

              {/* Ngành */}
              <div className="flex items-center gap-1 bg-slate-900 border border-purple-500/30 rounded-xl px-2 py-1">
                <Globe size={13} className="text-purple-400 shrink-0" />
                <select
                  value={bulkDomain}
                  onChange={e => setBulkDomain(e.target.value)}
                  className="bg-transparent text-purple-200 text-xs font-bold focus:outline-none cursor-pointer max-w-[130px] sm:max-w-[150px] truncate"
                >
                  {activeDomains.map(d => (
                    <option key={d.code} value={d.code} className="bg-slate-900">{d.icon} {d.short}</option>
                  ))}
                </select>
              </div>

              {/* Nút Áp dụng chung cho bộ 3 */}
              <button
                type="button"
                onClick={handleApplyAllCore}
                className="px-3 py-1.5 bg-blue-600 hover:bg-blue-500 text-white rounded-xl font-bold transition-all text-xs flex items-center gap-1 cursor-pointer active:scale-95 shadow-md shadow-blue-950/50 whitespace-nowrap"
                title="Áp dụng đồng loạt Chủ đề, Độ khó và Ngành cho cả bộ đề"
              >
                <CheckCheck size={13} /> Áp dụng ({docxPreviewList.length})
              </button>
            </div>

            {/* Right Action Tools */}
            <div className="flex items-center gap-1.5">
              {/* VNCorrect Chuẩn hóa Tiếng Việt On-Demand */}
              <button
                type="button"
                onClick={handleCorrectVietnamese}
                disabled={isCorrectingVN}
                className="px-3 py-1.5 bg-gradient-to-r from-emerald-600 via-teal-600 to-emerald-500 hover:from-emerald-500 hover:to-teal-500 text-white rounded-xl font-bold transition-all text-xs flex items-center gap-1.5 cursor-pointer active:scale-95 shadow-md shadow-emerald-950/40 border border-emerald-400/40 whitespace-nowrap disabled:opacity-50"
                title="Chuẩn hóa chính tả tiếng Việt & Maprepl theo 670+ quy tắc thực chiến"
              >
                {isCorrectingVN ? (
                  <Loader2 size={13} className="animate-spin text-emerald-200" />
                ) : (
                  <CheckCheck size={13} className="text-emerald-200" />
                )}
                <span>{isCorrectingVN ? 'Đang chuẩn hóa...' : '✨ VNCorrect'}</span>
              </button>

              {rawOriginalList && (
                <button
                  type="button"
                  onClick={handleRevertRawText}
                  className="px-2.5 py-1.5 bg-slate-850 hover:bg-slate-800 text-amber-300 border border-amber-500/30 rounded-xl font-bold transition-all text-xs flex items-center gap-1 cursor-pointer active:scale-95 whitespace-nowrap"
                  title="Khôi phục văn bản gốc ban đầu của file"
                >
                  <RotateCcw size={12} className="text-amber-400" />
                  <span>Bản gốc</span>
                </button>
              )}

              <button
                type="button"
                onClick={handleOpenBulkTaxonomyModal}
                className="px-3 py-1.5 bg-gradient-to-r from-purple-600 to-indigo-600 hover:from-purple-500 hover:to-indigo-500 text-white rounded-xl font-bold transition-all text-xs flex items-center gap-1.5 cursor-pointer active:scale-95 shadow-md shadow-purple-950/40 border border-purple-400/30 whitespace-nowrap"
              >
                <Tag size={13} /> 🏷️ Thẻ & Tọa độ
              </button>

              <button
                type="button"
                onClick={handleAutoClassifyTaxonomyAndTags}
                className="px-2.5 py-1.5 bg-purple-950/50 hover:bg-purple-900/60 text-purple-300 border border-purple-800/60 rounded-xl font-bold transition-all text-xs flex items-center gap-1 cursor-pointer active:scale-95 whitespace-nowrap"
                title="Tự động nhận diện Ngành và Thẻ bằng AI"
              >
                <Sparkles size={12} className="text-pink-400" /> Auto AI
              </button>

              <button
                type="button"
                onClick={() => setShowAdvancedBulkTools(!showAdvancedBulkTools)}
                className={`px-2.5 py-1.5 rounded-xl font-bold transition-all text-xs flex items-center gap-1 border cursor-pointer active:scale-95 ${
                  showAdvancedBulkTools
                    ? 'bg-slate-800 text-white border-slate-700'
                    : 'bg-slate-900 hover:bg-slate-850 text-slate-400 hover:text-slate-200 border-slate-800'
                }`}
                title="Mở rộng / Thu gọn công cụ bổ trợ (Thẻ chung, OCR LaTeX, Lọc đề)"
              >
                <SlidersHorizontal size={12} />
                <span>Tiện ích</span>
                {showAdvancedBulkTools ? <ChevronUp size={12} /> : <ChevronDown size={12} />}
              </button>
            </div>
          </div>

          {/* Drawer Công cụ Mở rộng (Chỉ bung ra khi người dùng cần) */}
          {showAdvancedBulkTools && (
            <div className="pt-2 mt-2 border-t border-slate-800/80 flex flex-wrap items-center justify-between gap-2 text-xs animate-in fade-in slide-in-from-top-1 duration-150">
              <div className="flex flex-wrap items-center gap-2">
                {/* Thêm thẻ chung */}
                <div className="flex items-center gap-1 bg-slate-900 border border-slate-800 rounded-xl px-2 py-0.5">
                  <Hash size={12} className="text-purple-400" />
                  <input
                    type="text"
                    value={bulkTagInput}
                    onChange={e => setBulkTagInput(e.target.value)}
                    onKeyDown={e => {
                      if (e.key === 'Enter') {
                        e.preventDefault();
                        handleAddBulkTag();
                      }
                    }}
                    placeholder="Thẻ chung (VD: tot-nghiep-2025)..."
                    className="bg-transparent text-xs text-slate-200 w-40 focus:outline-none"
                  />
                  <button
                    type="button"
                    onClick={handleAddBulkTag}
                    className="px-2 py-0.5 bg-purple-600 hover:bg-purple-500 text-white rounded-lg text-[11px] font-bold transition-all"
                  >
                    + Gán
                  </button>
                </div>

                {/* Nhận diện từ khóa */}
                <button
                  onClick={handleAutoClassifyTopics}
                  className="px-2.5 py-1 bg-slate-900 hover:bg-slate-850 text-slate-300 border border-slate-800 rounded-xl font-bold transition-all text-xs flex items-center gap-1 cursor-pointer active:scale-95"
                  title="Phân tích từ khóa nội dung để gán Chủ đề"
                >
                  <Wand2 size={12} /> Nhận diện từ khóa
                </button>

                {/* AI OCR LaTeX */}
                {onConvertLatex && hasFormulaImages && (
                  <button
                    onClick={onConvertLatex}
                    className="px-2.5 py-1 bg-teal-950/40 hover:bg-teal-900/60 text-teal-300 border border-teal-800/60 rounded-xl font-bold transition-all text-xs flex items-center gap-1 cursor-pointer active:scale-95"
                    title="Chuyển đổi ảnh công thức MathType sang mã LaTeX ($...$)"
                  >
                    <FileCode size={12} className="text-teal-400" /> AI OCR LaTeX
                  </button>
                )}
              </div>

              {/* Tabs lọc theo đề thi (nếu có nhiều đề) */}
              {hasMultipleExams && (
                <div className="flex items-center gap-1.5 overflow-x-auto">
                  <span className="text-[10px] font-bold uppercase text-slate-400">Đề:</span>
                  <button
                    type="button"
                    onClick={() => setSelectedExamFilter('ALL')}
                    className={`px-2 py-0.5 rounded-lg text-xs font-bold transition-all ${
                      selectedExamFilter === 'ALL'
                        ? 'bg-blue-600 text-white'
                        : 'bg-slate-900 text-slate-400 hover:text-white border border-slate-800'
                    }`}
                  >
                    Tất cả ({docxPreviewList.length})
                  </button>
                  {examGroups.map(eg => {
                    const count = docxPreviewList.filter(q => (q.topicCode || 'GENERAL') === eg).length;
                    const label = eg.replace('TOAN_DE_', 'Đề ');
                    const isSelected = selectedExamFilter === eg;
                    return (
                      <button
                        key={eg}
                        type="button"
                        onClick={() => setSelectedExamFilter(eg)}
                        className={`px-2 py-0.5 rounded-lg text-xs font-bold transition-all ${
                          isSelected
                            ? 'bg-purple-600 text-white'
                            : 'bg-slate-900 text-slate-400 hover:text-white border border-slate-800'
                        }`}
                      >
                        {label} ({count})
                      </button>
                    );
                  })}
                </div>
              )}
            </div>
          )}
        </div>

        {/* List of items */}
        <div className="flex-1 overflow-y-auto space-y-3 pr-1 my-1">
          {displayedList.map((item, displayedIdx) => {
            const originalIdx = docxPreviewList.indexOf(item);
            const idx = originalIdx >= 0 ? originalIdx : displayedIdx;
            return (
              <div
                key={item.tempId || idx}
                className="bg-slate-950/40 border border-slate-800/60 rounded-2xl p-4 space-y-2.5 hover:border-slate-700/80 transition-all"
              >
              {/* Question Card Header (1 Harmonious Row) */}
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-slate-800/70 pb-2">
                <div className="flex items-center gap-1.5 flex-wrap">
                  <span className="w-6 h-6 rounded-md bg-blue-600/20 border border-blue-500/30 text-blue-400 font-black flex items-center justify-center text-[11px] shrink-0">
                    {idx + 1}
                  </span>
                  <select
                    value={item.questionType}
                    onChange={e => updateItem(idx, 'questionType', e.target.value)}
                    className={`text-[11px] font-black px-2 py-0.5 rounded-lg border cursor-pointer ${
                      item.questionType === 'ESSAY'
                        ? 'bg-amber-950/60 text-amber-300 border-amber-500/50'
                        : item.questionType === 'MULTI'
                        ? 'bg-purple-950/60 text-purple-300 border-purple-500/50'
                        : item.questionType === 'TRUE_FALSE'
                        ? 'bg-emerald-950/60 text-emerald-300 border-emerald-500/50'
                        : 'bg-blue-950/60 text-blue-300 border-blue-500/50'
                    }`}
                  >
                    <option value="SINGLE">📝 Trắc nghiệm</option>
                    <option value="MULTI">☑️ Nhiều đáp án</option>
                    <option value="TRUE_FALSE">⚖️ Đúng / Sai</option>
                    <option value="SHORT_ANSWER">🔢 Trả lời ngắn</option>
                    <option value="ESSAY">✍️ Tự luận</option>
                  </select>

                  {/* Miền Ngành Selector */}
                  <select
                    value={item.domainCode || 'GENERAL'}
                    onChange={e => updateItem(idx, 'domainCode', e.target.value)}
                    className="bg-slate-900 border border-purple-500/30 rounded-lg px-2 py-0.5 text-[11px] text-purple-200 font-bold focus:outline-none focus:border-purple-400 cursor-pointer max-w-[125px] truncate"
                  >
                    {activeDomains.map(d => (
                      <option key={d.code} value={d.code} className="bg-slate-900">{d.icon} {d.short}</option>
                    ))}
                  </select>

                  {/* Chủ đề Selector */}
                  <select
                    value={item.topicCode}
                    onChange={e => {
                      if (e.target.value === '__CREATE_NEW__') {
                        openCreateTopicModal(idx);
                      } else {
                        updateItem(idx, 'topicCode', e.target.value);
                      }
                    }}
                    className="bg-slate-900 border border-slate-800 rounded-lg px-2 py-0.5 text-[11px] text-slate-300 focus:outline-none focus:border-blue-500 cursor-pointer max-w-[140px] truncate"
                  >
                    {localTopics.map(t => <option key={t.id || t.code} value={t.code} className="bg-slate-900">{t.name} ({t.code})</option>)}
                    <option value="__CREATE_NEW__" className="text-amber-400 font-bold bg-slate-950">➕ Tạo mới...</option>
                  </select>

                  {/* Độ khó Selector */}
                  <select
                    value={item.difficulty}
                    onChange={e => updateItem(idx, 'difficulty', parseInt(e.target.value))}
                    className="bg-slate-900 border border-slate-800 rounded-lg px-1.5 py-0.5 text-[11px] text-slate-300 focus:outline-none cursor-pointer"
                  >
                    {[1, 2, 3, 4, 5].map(d => <option key={d} value={d} className="bg-slate-900">Mức {d}</option>)}
                  </select>
                </div>

                <div className="flex items-center gap-1.5 shrink-0">
                  {item.targetLevel && (
                    <span className="text-[10px] font-bold px-1.5 py-0.5 rounded bg-blue-950/60 text-blue-300 border border-blue-800/50">
                      🎯 {item.targetLevel}
                    </span>
                  )}
                  {item.issuingOrg && (
                    <span className="text-[10px] font-bold px-1.5 py-0.5 rounded bg-emerald-950/60 text-emerald-300 border border-emerald-800/50">
                      🏛️ {item.issuingOrg}
                    </span>
                  )}
                  {item.benchmarkYear && (
                    <span className="text-[10px] font-bold px-1.5 py-0.5 rounded bg-amber-950/60 text-amber-300 border border-amber-800/50">
                      📅 {item.benchmarkYear}
                    </span>
                  )}
                  <button
                    type="button"
                    onClick={() => toggleExpandCoords(idx)}
                    className={`px-2 py-0.5 rounded-lg text-[10px] font-bold border flex items-center gap-0.5 cursor-pointer transition-colors ${
                      expandedCoords[idx]
                        ? 'bg-purple-950 text-purple-300 border-purple-500/60 shadow-sm'
                        : 'bg-slate-900 hover:bg-slate-800 text-slate-400 border-slate-800'
                    }`}
                    title="Chỉnh sửa 5 trục tọa độ chi tiết cho câu hỏi này"
                  >
                    <Compass size={11} className="text-purple-400" />
                    <span>{expandedCoords[idx] ? 'Ẩn tọa độ' : 'Tọa độ'}</span>
                  </button>
                  <button
                    type="button"
                    onClick={() => handleDeleteItem(idx)}
                    className="p-1 rounded-lg text-slate-500 hover:text-red-400 hover:bg-red-950/30 transition-colors cursor-pointer"
                    title="Xóa câu hỏi này khỏi danh sách duyệt"
                  >
                    <Trash2 size={12} />
                  </button>
                </div>
              </div>

              {/* Micro Tag Strip (Subtle 22px bar) */}
              <div className="flex items-center justify-between gap-2 text-xs py-0.5">
                <div className="flex items-center gap-1 flex-wrap flex-1 min-w-0">
                  <span className="text-[10px] font-bold text-purple-400/80 flex items-center gap-0.5 shrink-0">
                    <Hash size={10} />
                  </span>
                  {Array.isArray(item.tags) && item.tags.length > 0 ? (
                    item.tags.map((tag: string) => (
                      <span
                        key={tag}
                        className="inline-flex items-center gap-0.5 px-1.5 py-0.2 rounded text-[10px] font-bold bg-purple-950/50 text-purple-300 border border-purple-800/40"
                      >
                        #{tag}
                        <button
                          type="button"
                          onClick={() => handleRemoveTagFromQuestion(idx, tag)}
                          className="hover:text-red-400 cursor-pointer ml-0.5 text-[10px]"
                        >
                          ×
                        </button>
                      </span>
                    ))
                  ) : null}
                  <input
                    type="text"
                    value={tagInputPerQ[idx] || ''}
                    onChange={e => setTagInputPerQ(prev => ({ ...prev, [idx]: e.target.value }))}
                    onKeyDown={e => {
                      if (e.key === 'Enter' || e.key === ',') {
                        e.preventDefault();
                        handleAddTagToQuestion(idx, tagInputPerQ[idx] || '');
                      }
                    }}
                    placeholder="+ Thêm #tag..."
                    className="bg-transparent text-[10px] text-slate-400 w-20 focus:w-28 transition-all focus:outline-none focus:text-white placeholder:text-slate-600"
                  />
                </div>

                {(item.content?.includes('data:image/') || item.content?.includes('$')) && (
                  <span className="text-[10px] font-bold text-teal-400 flex items-center gap-1 bg-teal-950/40 px-1.5 py-0.5 rounded border border-teal-800/40 shrink-0">
                    ⚡ Có công thức/ảnh
                  </span>
                )}
              </div>

              {/* Panel Tọa Độ Mở Rộng */}
              {expandedCoords[idx] && (
                <div className="p-3 bg-slate-950/90 border border-purple-900/40 rounded-xl grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-2.5 text-xs animate-in fade-in duration-150">
                  <div className="space-y-1">
                    <label className="text-[10px] text-slate-400 font-bold">Cấp bậc / Khối lớp</label>
                    <input
                      type="text"
                      value={item.targetLevel || ''}
                      onChange={e => updateItem(idx, 'targetLevel', e.target.value)}
                      placeholder="VD: Lớp 12, Senior RM..."
                      className="w-full bg-slate-900 border border-slate-800 rounded-lg px-2.5 py-1 text-xs text-slate-200 focus:outline-none focus:border-purple-500"
                    />
                  </div>
                  <div className="space-y-1">
                    <label className="text-[10px] text-slate-400 font-bold">Mục đích sát hạch</label>
                    <input
                      type="text"
                      value={item.assessmentPurpose || ''}
                      onChange={e => updateItem(idx, 'assessmentPurpose', e.target.value)}
                      placeholder="VD: Thi tốt nghiệp THPT, AML..."
                      className="w-full bg-slate-900 border border-slate-800 rounded-lg px-2.5 py-1 text-xs text-slate-200 focus:outline-none focus:border-purple-500"
                    />
                  </div>
                  <div className="space-y-1">
                    <label className="text-[10px] text-slate-400 font-bold">Cơ quan / Tổ chức ban hành</label>
                    <input
                      type="text"
                      value={item.issuingOrg || ''}
                      onChange={e => updateItem(idx, 'issuingOrg', e.target.value)}
                      placeholder="VD: Chuyên Amsterdam, VCB..."
                      className="w-full bg-slate-900 border border-slate-800 rounded-lg px-2.5 py-1 text-xs text-slate-200 focus:outline-none focus:border-purple-500"
                    />
                  </div>
                  <div className="space-y-1">
                    <label className="text-[10px] text-slate-400 font-bold">Năm & Chuẩn mực quy chiếu</label>
                    <div className="flex gap-1.5">
                      <input
                        type="number"
                        value={item.benchmarkYear || ''}
                        onChange={e => updateItem(idx, 'benchmarkYear', e.target.value ? parseInt(e.target.value) : undefined)}
                        placeholder="2025"
                        className="w-20 bg-slate-900 border border-slate-800 rounded-lg px-2.5 py-1 text-xs text-slate-200 focus:outline-none focus:border-purple-500"
                      />
                      <input
                        type="text"
                        value={item.benchmarkStandard || ''}
                        onChange={e => updateItem(idx, 'benchmarkStandard', e.target.value)}
                        placeholder="GDPT 2018 / Basel III..."
                        className="flex-1 bg-slate-900 border border-slate-800 rounded-lg px-2.5 py-1 text-xs text-slate-200 focus:outline-none focus:border-purple-500"
                      />
                    </div>
                  </div>
                </div>
              )}

              {item.contextContent && (
                <div className="bg-indigo-950/40 border border-indigo-500/30 rounded-xl p-3.5 text-xs space-y-2">
                  <div className="flex items-center justify-between text-indigo-300 font-bold text-[11px] uppercase tracking-wider">
                    <span className="flex items-center gap-1.5">
                      📖 {item.contextTitle || 'Bài đọc hiểu / Dữ liệu dùng chung'}
                    </span>
                    <span className="text-[10px] bg-indigo-500/20 text-indigo-300 px-2 py-0.5 rounded border border-indigo-500/30 font-semibold lowercase">
                      ngữ cảnh liên kết
                    </span>
                  </div>
                  <div className="text-slate-300 leading-relaxed max-h-36 overflow-y-auto font-sans bg-slate-950/50 p-2.5 rounded-lg border border-indigo-900/30">
                    <MathRenderer content={item.contextContent} />
                  </div>
                </div>
              )}

              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <label className="text-[10px] font-black uppercase text-slate-400 tracking-wide">
                    {item.questionType === 'ESSAY' ? 'Nội dung tình huống / Đề bài tự luận' : 'Nội dung câu hỏi'}
                  </label>
                  {(item.content?.includes('data:image/') || item.content?.includes('$')) && (
                    <span className="text-[10px] font-bold text-teal-400 flex items-center gap-1 bg-teal-950/40 px-2 py-0.5 rounded border border-teal-800/40">
                      ⚡ Có công thức/ảnh minh họa
                    </span>
                  )}
                </div>
                <textarea
                  value={item.content}
                  onChange={e => updateItem(idx, 'content', e.target.value)}
                  className="w-full bg-slate-900 border border-slate-800 rounded-xl px-4 py-2.5 text-sm text-slate-200 focus:outline-none focus:border-blue-500 transition-colors"
                  rows={item.questionType === 'ESSAY' ? 4 : 2}
                />
                {(item.content?.includes('data:image/') || item.content?.includes('$')) && (
                  <div className="p-3 bg-slate-950/70 border border-slate-800/80 rounded-xl text-xs text-slate-300">
                    <div className="text-[10px] uppercase font-bold text-slate-500 mb-1">Xem trước trực quan:</div>
                    <MathRenderer content={item.content} />
                  </div>
                )}
              </div>

              {item.questionType !== 'ESSAY' && item.options && item.options.length > 0 && (
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <label className="text-[10px] font-black uppercase text-slate-400 tracking-wide">
                      Phương án lựa chọn ({item.options.length} phương án)
                    </label>
                    <button
                      type="button"
                      onClick={() => setShowRawOptions(prev => ({ ...prev, [idx]: !prev[idx] }))}
                      className="text-[10px] text-slate-400 hover:text-slate-200 flex items-center gap-1 cursor-pointer"
                    >
                      <Eye size={11} /> {showRawOptions[idx] ? 'Thu gọn mã nguồn' : 'Xem / Sửa chuỗi mã'}
                    </button>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                    {item.options.map((opt: string, optIdx: number) => {
                      const isImageOrMath = opt?.includes('data:image/') || opt?.includes('$');
                      const isSelectedAnswer = item.suggestedAnswer === (optIdx + 1).toString();
                      return (
                        <div
                          key={optIdx}
                          className={`flex flex-col gap-1 p-3 rounded-xl text-xs transition-all border ${
                            isSelectedAnswer
                              ? 'bg-emerald-950/30 border-emerald-500/60 shadow-sm shadow-emerald-950/40'
                              : 'bg-slate-900/60 border-slate-850 text-slate-300'
                          }`}
                        >
                          <div className="flex items-center justify-between mb-1">
                            <div className="flex items-center gap-1.5">
                              <span className={`font-black ${isSelectedAnswer ? 'text-emerald-400' : 'text-slate-400'}`}>
                                {LETTERS[optIdx]}.
                              </span>
                              {isSelectedAnswer && (
                                <span className="text-[10px] font-bold px-1.5 py-0.5 rounded bg-emerald-500/20 text-emerald-300 border border-emerald-500/40 flex items-center gap-0.5">
                                  ✓ Đáp án đúng
                                </span>
                              )}
                            </div>
                            <div className="flex items-center gap-1">
                              {opt?.includes('data:image/') && (
                                <span className="text-[9px] px-1.5 py-0.5 rounded bg-blue-950/60 text-blue-400 border border-blue-800/40 font-mono">
                                  MathType
                                </span>
                              )}
                              {opt?.includes('$') && (
                                <span className="text-[9px] px-1.5 py-0.5 rounded bg-teal-950/60 text-teal-400 border border-teal-800/40 font-mono">
                                  LaTeX
                                </span>
                              )}
                            </div>
                          </div>

                          {/* Live Visual Math & Image rendering */}
                          <div className="min-h-[32px] flex items-center">
                            <MathRenderer content={opt} />
                          </div>

                          {/* Raw text input if user wants to edit or if it's text */}
                          {(showRawOptions[idx] || !isImageOrMath) && (
                            <input
                              type="text"
                              value={opt}
                              onChange={e => {
                                const updated = [...docxPreviewList];
                                updated[idx].options[optIdx] = e.target.value;
                                setDocxPreviewList(updated);
                              }}
                              className="mt-1 bg-slate-950/60 border border-slate-800 px-2 py-1 rounded text-[11px] font-mono text-slate-400 focus:outline-none focus:text-white"
                              placeholder="Nội dung phương án..."
                            />
                          )}
                        </div>
                      );
                    })}
                  </div>
                </div>
              )}

              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="md:col-span-1 space-y-2">
                  <div className="flex items-center justify-between">
                    <label className="text-[10px] font-black uppercase text-slate-400 tracking-wide">
                      {item.questionType === 'ESSAY' ? 'Barem chấm điểm' : 'Đáp án đúng'}
                    </label>
                    {item.suggestedAnswer && (
                      <span className="text-[10px] font-bold text-emerald-400 bg-emerald-950/60 border border-emerald-800/50 px-2 py-0.5 rounded-lg">
                        {item.questionType === 'SINGLE' && parseInt(item.suggestedAnswer) >= 1 && parseInt(item.suggestedAnswer) <= 4
                          ? `Đáp án ${LETTERS[parseInt(item.suggestedAnswer) - 1]}`
                          : item.suggestedAnswer}
                      </span>
                    )}
                  </div>

                  {item.questionType !== 'ESSAY' && item.options && item.options.length > 0 ? (
                    <select
                      value={item.suggestedAnswer}
                      onChange={e => updateItem(idx, 'suggestedAnswer', e.target.value)}
                      className="w-full bg-slate-900 border border-slate-800 rounded-xl px-4 py-2.5 text-xs text-slate-300 focus:outline-none focus:border-blue-500 cursor-pointer"
                    >
                      <option value="">-- Chọn đáp án đúng --</option>
                      {item.options.map((_: any, oIdx: number) => (
                        <option key={oIdx} value={(oIdx + 1).toString()}>{LETTERS[oIdx]} (Lựa chọn {oIdx + 1})</option>
                      ))}
                    </select>
                  ) : (
                    <input
                      type="text"
                      value={item.suggestedAnswer}
                      onChange={e => updateItem(idx, 'suggestedAnswer', e.target.value)}
                      placeholder={item.questionType === 'SHORT_ANSWER' ? 'Nhập giá trị đáp số (ví dụ: 79,2)...' : 'Nhập đáp án đúng...'}
                      className="w-full bg-slate-900 border border-slate-800 rounded-xl px-4 py-2.5 text-xs text-slate-300 focus:outline-none focus:border-blue-500"
                    />
                  )}
                </div>

                <div className="md:col-span-2 space-y-2">
                  <div className="flex items-center justify-between">
                    <label className="text-[10px] font-black uppercase text-slate-400 tracking-wide flex items-center gap-1.5">
                      <span>📘 Lời giải chi tiết & Căn cứ nghiệp vụ</span>
                      {item.aiExplanation && (
                        <span className="text-[9px] px-1.5 py-0.5 rounded bg-blue-950/60 text-blue-300 border border-blue-800/40 font-bold">
                          {item.aiExplanation.length} ký tự
                        </span>
                      )}
                    </label>
                  </div>

                  {/* Hiển thị công thức toán KaTeX & hình vẽ trong lời giải */}
                  {item.aiExplanation && (
                    <div className="p-3 bg-slate-950/80 border border-slate-800 rounded-xl text-xs text-slate-300 space-y-1 max-h-40 overflow-y-auto leading-relaxed">
                      <div className="text-[10px] uppercase font-bold text-slate-500 mb-1">Xem trước lời giải:</div>
                      <MathRenderer content={item.aiExplanation} />
                    </div>
                  )}

                  <textarea
                    value={item.aiExplanation}
                    onChange={e => updateItem(idx, 'aiExplanation', e.target.value)}
                    placeholder="Trích dẫn các bước giải toán, bảng biến thiên, đạo hàm hoặc căn cứ quy định..."
                    className="w-full bg-slate-900 border border-slate-800 rounded-xl px-4 py-2 text-xs text-slate-300 focus:outline-none focus:border-blue-500"
                    rows={item.aiExplanation ? 2 : 3}
                  />
                </div>
              </div>
            </div>
            );
          })}
        </div>

        <div className="flex flex-wrap gap-4 pt-6 border-t border-slate-800 mt-6 shrink-0 items-center justify-between">
          <button
            onClick={onClose}
            className="px-6 py-3.5 bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white rounded-xl font-bold transition-all text-xs border border-slate-800 cursor-pointer"
          >Hủy bỏ</button>

          <div className="flex items-center gap-3">
            {onAiSolveBatch && (
              <button
                onClick={onAiSolveBatch}
                disabled={loading || isAiSolving}
                className="px-5 py-3.5 bg-purple-900/40 hover:bg-purple-800/60 text-purple-300 hover:text-purple-100 border border-purple-500/40 font-bold rounded-xl transition-all shadow-md active:scale-95 text-xs flex items-center justify-center gap-1.5 cursor-pointer disabled:opacity-50"
              >
                <Sparkles size={14} className="text-purple-400" /> 🤖 Nhờ AI (Gemini) giải tự động & trích dẫn
              </button>
            )}
            <button
              onClick={onConfirmImport}
              disabled={loading || isAiSolving}
              className="px-8 py-3.5 bg-gradient-to-r from-blue-500 to-purple-500 hover:from-blue-600 hover:to-purple-600 text-white font-bold rounded-xl transition-all shadow-lg active:scale-95 text-xs flex items-center justify-center gap-1.5 cursor-pointer disabled:opacity-60"
            >
              <Save size={14} /> Xác nhận Lưu {docxPreviewList.length} câu hỏi
            </button>
          </div>
        </div>

        {/* Toast Notification */}
        {toastMessage && (
          <div className="absolute top-6 left-1/2 -translate-x-1/2 z-[80] bg-emerald-600 text-white px-5 py-2.5 rounded-2xl shadow-2xl shadow-emerald-950/60 text-xs font-bold flex items-center gap-2 border border-emerald-400/50 animate-in fade-in slide-in-from-top-3 duration-200">
            <Check size={16} className="text-emerald-200" />
            <span>{toastMessage}</span>
          </div>
        )}

        {/* Quick Create Topic Modal (Isolated State - Zero Lag - Full Root/Parent Topic Integration) */}
        <CreateTopicModal
          isOpen={showCreateTopicModal}
          topics={localTopics}
          targetQuestionIdx={targetQuestionIdx}
          totalQuestions={docxPreviewList.length}
          onClose={() => setShowCreateTopicModal(false)}
          onTopicCreated={(newTopic, applyToAll, targetIdx) => {
            setLocalTopics(prev => {
              if (!prev.some(t => t.code === newTopic.code)) {
                return [...prev, newTopic];
              }
              return prev;
            });
            setBulkTopic(newTopic.code);
            if (applyToAll || targetIdx === null) {
              setDocxPreviewList(docxPreviewList.map(item => ({ ...item, topicCode: newTopic.code })));
              showToast(`✨ Đã tạo và áp dụng chủ đề '${newTopic.name}' (${newTopic.code}) cho toàn bộ ${docxPreviewList.length} câu hỏi!`);
            } else if (targetIdx !== null) {
              updateItem(targetIdx, 'topicCode', newTopic.code);
              showToast(`✨ Đã tạo và áp dụng chủ đề '${newTopic.name}' cho câu hỏi ${targetIdx + 1}!`);
            }
            onTopicCreated?.(newTopic);
          }}
        />

        {/* Bulk Multi-Industry & Smart Tags Modal */}
        {showBulkTaxonomyModal && (
          <div className="fixed inset-0 bg-black/80 backdrop-blur-md z-[75] flex items-center justify-center p-4">
            <div className="w-full max-w-2xl bg-slate-900 border border-purple-500/40 rounded-3xl p-6 sm:p-7 shadow-2xl relative text-left text-white animate-in fade-in zoom-in-95 duration-150 max-h-[90vh] overflow-y-auto">
              <div className="flex items-center justify-between pb-3 mb-5 border-b border-slate-800">
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 rounded-2xl bg-gradient-to-br from-purple-500/20 to-blue-500/20 border border-purple-500/40 flex items-center justify-center text-purple-300">
                    <Tag size={20} />
                  </div>
                  <div>
                    <h3 className="text-lg font-black text-transparent bg-clip-text bg-gradient-to-r from-purple-300 via-indigo-300 to-blue-300">
                      Gán Thẻ & Tọa Độ Đa Ngành Hàng Loạt
                    </h3>
                    <p className="text-[11px] text-slate-400">
                      Cấu hình đồng loạt Hệ 5 Trục Tọa Độ & Thẻ thông minh cho {docxPreviewList.length} câu hỏi đang duyệt
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={handleAutoSniffModalCoordinates}
                    className="px-3 py-1.5 rounded-xl bg-gradient-to-r from-purple-600 via-indigo-600 to-blue-600 hover:from-purple-500 hover:to-indigo-500 text-white text-xs font-bold flex items-center gap-1.5 shadow-lg shadow-purple-600/30 transition-all cursor-pointer active:scale-95 border border-purple-400/40"
                    title="Tự động nhận diện 5 trục tọa độ & Thẻ thông minh từ tiêu đề và nội dung file"
                  >
                    <Sparkles size={13} className="text-amber-300 animate-pulse" />
                    <span>⚡ Nhận diện tự động AI</span>
                  </button>
                  <button
                    type="button"
                    onClick={() => setShowBulkTaxonomyModal(false)}
                    className="text-slate-400 hover:text-white p-1.5 rounded-lg hover:bg-slate-800 transition-colors cursor-pointer"
                  >
                    <X size={18} />
                  </button>
                </div>
              </div>

              {/* Banner hiển thị trạng thái nhận diện tự động */}
              {(modalTargetLevel || modalAssessmentPurpose) && (
                <div className="p-3 bg-purple-950/40 border border-purple-500/40 rounded-2xl flex items-center justify-between text-xs text-purple-200">
                  <div className="flex items-center gap-2 overflow-hidden">
                    <Sparkles size={15} className="text-amber-300 shrink-0" />
                    <span className="truncate">
                      Nhận diện tự động từ tiêu đề: <strong className="text-white">{modalTargetLevel || 'Tín dụng KHDN'}</strong> • {modalAssessmentPurpose} ({modalBenchmarkYear})
                    </span>
                  </div>
                  <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-purple-900/70 border border-purple-500/50 text-purple-300 shrink-0 ml-2">
                    ✓ AI Ready
                  </span>
                </div>
              )}

              <div className="space-y-5">
                {/* 1. Chọn Miền Ngành Nghề */}
                <div className="space-y-2">
                  <label className="text-[11px] font-extrabold uppercase text-purple-400 flex items-center gap-1.5">
                    <Globe size={13} /> 1. Miền Ngành Nghề Đào Tạo (Domain)
                  </label>
                  <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
                    {activeDomains.map(d => (
                      <button
                        key={d.code}
                        type="button"
                        onClick={() => {
                          setModalDomain(d.code);
                        }}
                        className={`p-2.5 rounded-xl border text-xs font-bold flex items-center gap-2 transition-all cursor-pointer text-left ${
                          modalDomain === d.code
                            ? 'bg-purple-950/70 border-purple-500 text-purple-200 shadow-md shadow-purple-950/50'
                            : 'bg-slate-950/50 border-slate-800 text-slate-400 hover:text-slate-200 hover:border-slate-700'
                        }`}
                      >
                        <span className="text-base">{d.icon}</span>
                        <span className="truncate">{(d.label || d.code).replace(/^.*? /, '')}</span>
                      </button>
                    ))}
                  </div>
                </div>

                {/* 2. Hệ Tọa Độ Tri Thức */}
                <div className="space-y-3 bg-slate-950/50 border border-slate-800/80 rounded-2xl p-4">
                  <div className="text-[11px] font-extrabold uppercase text-slate-400 flex items-center gap-1.5">
                    <Compass size={13} className="text-blue-400" /> 2. Hệ Tọa Độ Tri Thức (Quy chuẩn định danh)
                  </div>
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
                    <div className="space-y-1">
                      <label className="text-[10px] text-slate-400 font-bold flex items-center gap-1">
                        <Award size={11} className="text-blue-400" /> Cấp bậc / Khối lớp
                      </label>
                      <input
                        type="text"
                        value={modalTargetLevel}
                        onChange={e => setModalTargetLevel(e.target.value)}
                        placeholder="VD: Lớp 12, Senior RM, Bác sĩ CKI..."
                        className="w-full bg-slate-900 border border-slate-750 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500"
                      />
                    </div>
                    <div className="space-y-1">
                      <label className="text-[10px] text-slate-400 font-bold flex items-center gap-1">
                        🎯 Mục đích sát hạch / Đánh giá
                      </label>
                      <input
                        type="text"
                        value={modalAssessmentPurpose}
                        onChange={e => setModalAssessmentPurpose(e.target.value)}
                        placeholder="VD: Tốt nghiệp THPT, Tuân thủ AML định kỳ..."
                        className="w-full bg-slate-900 border border-slate-750 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500"
                      />
                    </div>
                    <div className="space-y-1">
                      <label className="text-[10px] text-slate-400 font-bold flex items-center gap-1">
                        <Building2 size={11} className="text-emerald-400" /> Đơn vị / Cơ quan ban hành
                      </label>
                      <input
                        type="text"
                        value={modalIssuingOrg}
                        onChange={e => setModalIssuingOrg(e.target.value)}
                        placeholder="VD: Chuyên Amsterdam, Vietcombank, Cục ĐBVN..."
                        className="w-full bg-slate-900 border border-slate-750 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500"
                      />
                    </div>
                    <div className="space-y-1">
                      <label className="text-[10px] text-slate-400 font-bold flex items-center gap-1">
                        <Calendar size={11} className="text-amber-400" /> Năm & Chuẩn mực quy chiếu
                      </label>
                      <div className="flex gap-2">
                        <input
                          type="number"
                          value={modalBenchmarkYear}
                          onChange={e => setModalBenchmarkYear(e.target.value)}
                          placeholder="2025"
                          className="w-24 bg-slate-900 border border-slate-750 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500"
                        />
                        <input
                          type="text"
                          value={modalBenchmarkStandard}
                          onChange={e => setModalBenchmarkStandard(e.target.value)}
                          placeholder="GDPT 2018 / Thông tư 41..."
                          className="flex-1 bg-slate-900 border border-slate-750 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500"
                        />
                      </div>
                    </div>
                  </div>
                </div>

                {/* 3. Thẻ Thông Minh (Tags) */}
                <div className="space-y-2 bg-slate-950/50 border border-slate-800/80 rounded-2xl p-4">
                  <div className="text-[11px] font-extrabold uppercase text-purple-400 flex items-center justify-between">
                    <span className="flex items-center gap-1.5">
                      <Hash size={13} /> 3. Danh Sách Thẻ Thông Minh (Smart Tags)
                    </span>
                    <span className="text-[10px] text-slate-500 font-normal">Gõ tag và nhấn Enter hoặc phẩy</span>
                  </div>

                  <div className="flex flex-wrap items-center gap-2 p-2.5 bg-slate-900 border border-slate-800 rounded-xl min-h-[44px]">
                    {modalTags.map(tag => (
                      <span
                        key={tag}
                        className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-bold bg-purple-950/80 text-purple-300 border border-purple-700/60 shadow-sm"
                      >
                        #{tag}
                        <button
                          type="button"
                          onClick={() => setModalTags(prev => prev.filter(t => t !== tag))}
                          className="hover:text-red-400 cursor-pointer ml-1 text-xs"
                        >
                          ×
                        </button>
                      </span>
                    ))}
                    <input
                      type="text"
                      value={modalNewTag}
                      onChange={e => setModalNewTag(e.target.value)}
                      onKeyDown={e => {
                        if (e.key === 'Enter' || e.key === ',') {
                          e.preventDefault();
                          const val = modalNewTag.trim().replace(/^#/, '');
                          if (val && !modalTags.includes(val)) {
                            setModalTags(prev => [...prev, val]);
                          }
                          setModalNewTag('');
                        }
                      }}
                      placeholder="+ Nhập thẻ mới..."
                      className="bg-transparent text-xs text-white placeholder:text-slate-500 focus:outline-none flex-1 min-w-[120px]"
                    />
                  </div>

                  {/* Gợi ý thẻ theo ngành */}
                  {DOMAIN_SUGGESTED_TAGS[modalDomain] && (
                    <div className="space-y-1 pt-1">
                      <div className="text-[10px] text-slate-500 font-bold">Thẻ phổ biến trong lĩnh vực này:</div>
                      <div className="flex flex-wrap gap-1.5">
                        {DOMAIN_SUGGESTED_TAGS[modalDomain].map(tag => {
                          const isSelected = modalTags.includes(tag);
                          return (
                            <button
                              key={tag}
                              type="button"
                              onClick={() => {
                                if (isSelected) {
                                  setModalTags(prev => prev.filter(t => t !== tag));
                                } else {
                                  setModalTags(prev => [...prev, tag]);
                                }
                              }}
                              className={`px-2 py-0.5 rounded-lg text-[10px] font-bold border transition-colors cursor-pointer ${
                                isSelected
                                  ? 'bg-purple-600 text-white border-purple-500'
                                  : 'bg-slate-900 text-slate-400 hover:text-slate-200 border-slate-800'
                              }`}
                            >
                              #{tag}
                            </button>
                          );
                        })}
                      </div>
                    </div>
                  )}
                </div>

                {/* Tùy chọn phạm vi áp dụng */}
                <div className="p-3 bg-slate-950/60 border border-purple-500/20 rounded-2xl">
                  <label className="flex items-center gap-2.5 cursor-pointer text-xs text-slate-200 select-none">
                    <input
                      type="checkbox"
                      checked={modalOnlyUnassigned}
                      onChange={e => setModalOnlyUnassigned(e.target.checked)}
                      className="rounded bg-slate-900 border-slate-750 text-purple-500 focus:ring-0 cursor-pointer"
                    />
                    <span className="font-medium text-slate-300">
                      Chỉ áp dụng cho các câu hỏi chưa được gán ngành riêng biệt
                    </span>
                  </label>
                </div>

                <div className="flex items-center justify-end gap-3 pt-2">
                  <button
                    type="button"
                    onClick={() => setShowBulkTaxonomyModal(false)}
                    className="px-4 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-750 text-slate-300 text-xs font-bold transition-colors cursor-pointer"
                  >
                    Hủy bỏ
                  </button>
                  <button
                    type="button"
                    onClick={handleApplyBulkTaxonomyModal}
                    className="px-6 py-2.5 rounded-xl bg-gradient-to-r from-purple-600 via-indigo-600 to-blue-600 hover:from-purple-500 hover:to-blue-500 text-white text-xs font-black transition-all shadow-lg shadow-purple-950/50 flex items-center gap-1.5 cursor-pointer active:scale-95"
                  >
                    <CheckCheck size={14} /> Áp dụng cho {docxPreviewList.length} câu hỏi
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
