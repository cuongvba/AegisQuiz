import { useState, useEffect, useRef, useMemo } from 'react';
import { FileUp, Plus, HelpCircle, Star, Sparkles, Trash2, Edit, X, Loader2, Save, FolderOpen, Layers, ArrowUpDown, Eye, EyeOff, Lock, Unlock, ToggleLeft, ToggleRight } from 'lucide-react';
import { learnerQuizService } from '@/services/learner-quiz.service';
import type { LearnerQuestion, QuestionType } from '@/types/quiz';

interface BankTopic {
  id: string;
  code: string;
  name: string;
  description?: string;
  categoryCode: string;
  enabled: boolean;
  visibilityScope: string;
  parentId?: string;
}

export function AdminQuestionsPage() {
  const [activeTab, setActiveTab] = useState<'questions' | 'topics'>('questions');
  const [questions, setQuestions] = useState<LearnerQuestion[]>([]);
  const [topics, setTopics] = useState<BankTopic[]>([]);
  const [loading, setLoading] = useState(false);
  const [dragActive, setDragActive] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // States cho Modal Thêm/Sửa câu hỏi
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [userRoleOverride, setUserRoleOverride] = useState<'admin' | 'viewer'>('admin');
  const hasWritePermission = userRoleOverride === 'admin';
  const [editingId, setEditingId] = useState<string | null>(null);
  const [content, setContent] = useState('');
  const [questionType, setQuestionType] = useState<QuestionType>('SINGLE');
  const [difficulty, setDifficulty] = useState(1);
  const [categoryCode, setCategoryCode] = useState('');
  const [durationSeconds, setDurationSeconds] = useState(60);
  
  // Option fields (N phương án động)
  const [options, setOptions] = useState<string[]>(['', '', '', '']);
  const [citation, setCitation] = useState('');
  const [correctAnswer, setCorrectAnswer] = useState('');
  const [contentType, setContentType] = useState('text');
  const [optionType, setOptionType] = useState('text');

  // States cho Modal Thêm/Sửa chủ đề
  const [isTopicModalOpen, setIsTopicModalOpen] = useState(false);
  const [isEditingTopic, setIsEditingTopic] = useState(false);
  const [editingTopicId, setEditingTopicId] = useState<string | null>(null);
  const [topicCode, setTopicCode] = useState('');
  const [topicName, setTopicName] = useState('');
  const [topicDesc, setTopicDesc] = useState('');
  const [topicCat, setTopicCat] = useState('');
  const [topicVisibility, setTopicVisibility] = useState('PUBLIC');
  const [topicParentId, setTopicParentId] = useState('');

  // States cho tính năng Import DOCX
  const [docxPreviewList, setDocxPreviewList] = useState<any[]>([]);
  const [showDocxPreviewModal, setShowDocxPreviewModal] = useState(false);
  const [isDocxProcessing, setIsDocxProcessing] = useState(false);

  const [showMissingOnly, setShowMissingOnly] = useState(false);
  const missingAnswersCount = useMemo(() => {
    return questions.filter(q => !q.answerRaw || !q.answerRaw.trim()).length;
  }, [questions]);

  const LETTERS = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J'];

  // States cho Tìm kiếm, Lọc và Phân trang (Câu hỏi)
  const [qSearch, setQSearch] = useState('');
  const [qTypeFilter, setQTypeFilter] = useState('');
  const [qTopicFilter, setQTopicFilter] = useState('');
  const [qPage, setQPage] = useState(1);
  const qPageSize = 10;

  // States cho Tìm kiếm và Phân trang (Chủ đề)
  const [tSearch, setTSearch] = useState('');
  const [tPage, setTPage] = useState(1);
  const tPageSize = 10;

  // States và hàm cho tính năng sắp xếp theo header
  const [qSortField, setQSortField] = useState<string>('stt');
  const [qSortAsc, setQSortAsc] = useState<boolean>(true);
  const [tSortField, setTSortField] = useState<string>('code');
  const [tSortAsc, setTSortAsc] = useState<boolean>(true);

  const handleQSort = (field: string) => {
    if (qSortField === field) {
      setQSortAsc(!qSortAsc);
    } else {
      setQSortField(field);
      setQSortAsc(true);
    }
  };

  const handleTSort = (field: string) => {
    if (tSortField === field) {
      setTSortAsc(!tSortAsc);
    } else {
      setTSortField(field);
      setTSortAsc(true);
    }
  };

  const renderSortIcon = (field: string, currentField: string, isAsc: boolean) => {
    const active = field === currentField;
    return (
      <span className={`inline-flex ml-1.5 transition-colors ${active ? 'text-blue-400' : 'text-slate-600 group-hover:text-slate-400'}`}>
        <ArrowUpDown size={11} className={active && !isAsc ? 'rotate-180 transform transition-transform' : 'transition-transform'} />
      </span>
    );
  };

  const getTopicFullPath = (topic: BankTopic, allTopics: BankTopic[]): string => {
    const path: string[] = [topic.name];
    let parentId = topic.parentId;
    const visited = new Set<string>([topic.id]);
    
    while (parentId) {
      if (visited.has(parentId)) break;
      visited.add(parentId);
      
      const parent = allTopics.find(t => t.id === parentId);
      if (!parent) break;
      
      path.unshift(parent.name);
      parentId = parent.parentId;
    }
    
    return path.join(' → ');
  };

  const getQuestionTypeLabel = (type: string): string => {
    const map: Record<string, string> = {
      SINGLE: 'Trắc nghiệm 1 đáp án',
      MULTI: 'Nhiều đáp án đúng',
      TRUE_FALSE: 'Đúng / Sai',
      SHORT_ANSWER: 'Trả lời ngắn',
      FILL_BLANK: 'Điền vào chỗ trống',
      ORDERING: 'Sắp xếp thứ tự',
      MATCHING: 'Nối cặp',
      ESSAY: 'Tự luận',
    };
    return map[type] ?? type;
  };

  const getCorrectAnswerDisplay = (q: LearnerQuestion): { label: string; text: string } | null => {
    if (!q.answerRaw || !q.answerRaw.trim()) return null;

    if (q.questionType === 'SINGLE' || q.questionType === 'TRUE_FALSE') {
      const idx = parseInt(q.answerRaw) - 1;
      if (q.options && q.options[idx] !== undefined) {
        const label = q.questionType === 'TRUE_FALSE' 
          ? (idx === 0 ? 'Đúng (1)' : 'Sai (2)')
          : `${LETTERS[idx] || String(idx + 1)} (${idx + 1})`;
        return { label, text: q.options[idx] };
      }
      return { label: q.answerRaw, text: '' };
    }

    if (q.questionType === 'MULTI') {
      const indices = q.answerRaw.split(/[;,\s]+/).map(x => parseInt(x) - 1).filter(x => !isNaN(x));
      const labels: string[] = [];
      const texts: string[] = [];
      indices.forEach(idx => {
        if (q.options && q.options[idx] !== undefined) {
          const letterLabel = LETTERS[idx] || String(idx + 1);
          labels.push(`${letterLabel} (${idx + 1})`);
          texts.push(`${letterLabel}. ${q.options[idx]}`);
        } else {
          labels.push(String(idx + 1));
        }
      });
      return { label: labels.join(', '), text: texts.join(' | ') };
    }

    if (q.questionType === 'SHORT_ANSWER' || q.questionType === 'FILL_BLANK' || q.questionType === 'ESSAY') {
      return { label: q.answerRaw, text: '' };
    }

    return { label: q.answerRaw, text: '' };
  };

  const getCorrectAnswerPlaceholder = (): string => {
    switch (questionType) {
      case 'SINGLE':
        return 'Ví dụ: A (hoặc 1)';
      case 'MULTI':
        return 'Ví dụ: A,C (các phương án đúng cách nhau bởi dấu phẩy)';
      case 'TRUE_FALSE':
        return 'Điền 1 (Đúng) hoặc 2 (Sai)';
      case 'SHORT_ANSWER':
      case 'FILL_BLANK':
        return 'Điền từ/cụm từ chính xác (Ví dụ: quang hợp)';
      case 'ORDERING':
        return 'Điền thứ tự đúng của các phương án (Ví dụ: A,B,C,D hoặc 1,2,3,4)';
      case 'MATCHING':
        return 'Điền danh sách các cặp đúng (Ví dụ: Hà Nội:Việt Nam, Tokyo:Nhật Bản)';
      case 'ESSAY':
        return 'Đáp án mẫu hoặc hướng dẫn chấm điểm (Không bắt buộc)';
      default:
        return 'Điền đáp án chính xác...';
    }
  };

  // Reset trang về 1 khi thay đổi điều kiện lọc
  useEffect(() => {
    setQPage(1);
  }, [qSearch, qTypeFilter, qTopicFilter]);

  useEffect(() => {
    setTPage(1);
  }, [tSearch]);

  // Tính toán danh sách câu hỏi sau khi lọc và phân trang
  const filteredQuestions = useMemo(() => {
    const list = questions.filter((q) => {
      const matchSearch = q.content.toLowerCase().includes(qSearch.toLowerCase()) || q.id.toLowerCase().includes(qSearch.toLowerCase());
      const matchType = !qTypeFilter || q.questionType === qTypeFilter;
      const matchTopic = !qTopicFilter || q.topicCode === qTopicFilter;
      const matchMissing = !showMissingOnly || !q.answerRaw || !q.answerRaw.trim();
      return matchSearch && matchType && matchTopic && matchMissing;
    });

    if (qSortField) {
      list.sort((a, b) => {
        let valA: any = '';
        let valB: any = '';

        if (qSortField === 'stt') {
          valA = questions.indexOf(a);
          valB = questions.indexOf(b);
        } else if (qSortField === 'content') {
          valA = a.content || '';
          valB = b.content || '';
        } else if (qSortField === 'questionType') {
          valA = a.questionType || '';
          valB = b.questionType || '';
        } else if (qSortField === 'topicCode') {
          const topicA = topics.find(t => t.code === a.topicCode || t.categoryCode === a.topicCode);
          const topicB = topics.find(t => t.code === b.topicCode || t.categoryCode === b.topicCode);
          valA = topicA ? getTopicFullPath(topicA, topics) : a.topicCode || '';
          valB = topicB ? getTopicFullPath(topicB, topics) : b.topicCode || '';
        } else if (qSortField === 'difficulty') {
          valA = a.difficulty || 0;
          valB = b.difficulty || 0;
        }

        if (typeof valA === 'string' && typeof valB === 'string') {
          return qSortAsc 
            ? valA.localeCompare(valB, 'vi', { sensitivity: 'base' })
            : valB.localeCompare(valA, 'vi', { sensitivity: 'base' });
        } else {
          return qSortAsc 
            ? (valA > valB ? 1 : valA < valB ? -1 : 0)
            : (valB > valA ? 1 : valB < valA ? -1 : 0);
        }
      });
    }

    return list;
  }, [questions, qSearch, qTypeFilter, qTopicFilter, qSortField, qSortAsc, topics]);

  const pagedQuestions = useMemo(() => {
    const start = (qPage - 1) * qPageSize;
    return filteredQuestions.slice(start, start + qPageSize);
  }, [filteredQuestions, qPage, qPageSize]);

  const qTotalPages = Math.ceil(filteredQuestions.length / qPageSize);

  // Tính toán danh sách chủ đề sau khi lọc và phân trang
  const filteredTopics = useMemo(() => {
    const list = topics.filter((t) => {
      const path = getTopicFullPath(t, topics).toLowerCase();
      return path.includes(tSearch.toLowerCase()) || t.code.toLowerCase().includes(tSearch.toLowerCase()) || (t.description || '').toLowerCase().includes(tSearch.toLowerCase());
    });

    if (tSortField) {
      list.sort((a, b) => {
        let valA: any = '';
        let valB: any = '';

        if (tSortField === 'code') {
          valA = a.code || '';
          valB = b.code || '';
        } else if (tSortField === 'name') {
          valA = getTopicFullPath(a, topics);
          valB = getTopicFullPath(b, topics);
        } else if (tSortField === 'description') {
          valA = a.description || '';
          valB = b.description || '';
        } else if (tSortField === 'visibilityScope') {
          valA = a.visibilityScope || '';
          valB = b.visibilityScope || '';
        }

        if (typeof valA === 'string' && typeof valB === 'string') {
          return tSortAsc 
            ? valA.localeCompare(valB, 'vi', { sensitivity: 'base' })
            : valB.localeCompare(valA, 'vi', { sensitivity: 'base' });
        } else {
          return tSortAsc 
            ? (valA > valB ? 1 : valA < valB ? -1 : 0)
            : (valB > valA ? 1 : valB < valA ? -1 : 0);
        }
      });
    }

    return list;
  }, [topics, tSearch, tSortField, tSortAsc]);

  const pagedTopics = useMemo(() => {
    const start = (tPage - 1) * tPageSize;
    return filteredTopics.slice(start, start + tPageSize);
  }, [filteredTopics, tPage, tPageSize]);

  const tTotalPages = Math.ceil(filteredTopics.length / tPageSize);

  const fetchData = async () => {
    setLoading(true);
    try {
      const [questionsData, topicsData] = await Promise.all([
        learnerQuizService.getQuestions(),
        learnerQuizService.getTopics(),
      ]);
      setQuestions(questionsData);
      setTopics(topicsData);
      
      // Chọn chủ đề mặc định đầu tiên nếu có chủ đề
      if (topicsData.length > 0) {
        setCategoryCode(topicsData[0].code);
      }
    } catch (err) {
      console.error(err);
    }
    setLoading(false);
  };

  useEffect(() => {
    fetchData();
  }, []);

  // 1. LUỒNG NHẬP EXCEL & DOCX
  const handleFileImport = async (file: File) => {
    setIsDocxProcessing(true);
    try {
      const res = await learnerQuizService.importQuestions(file);
      if (res && res.sheets && Array.isArray(res.sheets)) {
        const allQuestions: any[] = [];
        res.sheets.forEach((sheet: any) => {
          (sheet.questions || []).forEach((q: any) => {
            allQuestions.push({
              ...q,
              topicCode: q.topicCode || sheet.detectedTopicCode || (topics.length > 0 ? topics[0].code : 'GENERAL'),
              difficulty: q.difficulty || 3,
            });
          });
        });
        setDocxPreviewList(allQuestions);
        setShowDocxPreviewModal(true);
      } else {
        alert(`Nhập thành công ${res.count ?? 0} câu hỏi từ file Excel vào cơ sở dữ liệu!`);
        fetchData();
      }
    } catch (err: any) {
      console.error(err);
      alert('Nhập câu hỏi thất bại. Vui lòng kiểm tra định dạng file Excel.');
    } finally {
      setIsDocxProcessing(false);
    }
  };

  const handleFileImportDocx = async (file: File) => {
    setIsDocxProcessing(true);
    try {
      const data = await learnerQuizService.importQuestionsDocx(file);
      // Gán mặc định topicCode và difficulty nếu API chưa trả về
      const mappedData = data.map((item: any) => ({
        ...item,
        topicCode: item.topicCode || (topics.length > 0 ? topics[0].code : 'PARTY_BUILDING'),
        difficulty: item.difficulty || 3
      }));
      setDocxPreviewList(mappedData);
      setShowDocxPreviewModal(true);
    } catch (err: any) {
      console.error(err);
      alert(err.response?.data?.message || 'Nhập câu hỏi từ file DOCX thất bại. Vui lòng thử lại.');
    } finally {
      setIsDocxProcessing(false);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      const file = e.target.files[0];
      if (file.name.toLowerCase().endsWith('.docx')) {
        handleFileImportDocx(file);
      } else {
        handleFileImport(file);
      }
    }
  };

  const onButtonClick = () => {
    fileInputRef.current?.click();
  };

  const handleSuggestAnswer = async (q: LearnerQuestion) => {
    setIsDocxProcessing(true);
    try {
      const res = await learnerQuizService.suggestQuestionAnswer(q.id);
      openEditModal(q);
      setIsEditing(true); // Đặt thành chế độ sửa
      setCorrectAnswer(res.suggestedAnswer || '');
      setCitation(res.aiExplanation || '');
    } catch (err: any) {
      console.error(err);
      alert(err.response?.data?.message || 'Không thể lấy gợi ý từ AI.');
    } finally {
      setIsDocxProcessing(false);
    }
  };

  // 2. LUỒNG THAO TÁC CÂU HỎI
  const openAddModal = () => {
    setEditingId(null);
    setContent('');
    setQuestionType('SINGLE');
    setDifficulty(1);
    setCategoryCode(topics.length > 0 ? topics[0].code : '');
    setDurationSeconds(60);
    setOptions(['', '', '', '']);
    setCitation('');
    setCorrectAnswer('');
    setContentType('text');
    setOptionType('text');
    setIsEditing(true);
    setIsModalOpen(true);
  };

  const openEditModal = (q: LearnerQuestion) => {
    setEditingId(q.id);
    setContent(q.content);
    setQuestionType(q.questionType);
    setDifficulty(q.difficulty);
    setCategoryCode(q.topicCode || (topics.length > 0 ? topics[0].code : ''));
    setDurationSeconds(q.durationSec || 60);

    const optList = q.options && q.options.length > 0 ? [...q.options] : [];
    
    while (optList.length < 4) optList.push('');
    setOptions(optList);

    setCitation(q.citation || '');
    setCorrectAnswer(q.answerRaw || '');
    setContentType(q.contentType || q.questionMediaType || 'text');
    setOptionType(q.optionType || q.optionMediaType || 'text');
    setIsEditing(false);
    setIsModalOpen(true);
  };

  const handleToggleActive = async (q: LearnerQuestion) => {
    if (!hasWritePermission) {
      alert('Bạn không có quyền thực hiện thao tác này. Vui lòng chuyển vai trò thành Admin.');
      return;
    }
    const nextEnabled = q.enabled === false ? true : false;
    try {
      const payload = {
        content: q.content,
        questionType: q.questionType,
        difficulty: q.difficulty,
        durationSeconds: q.durationSec,
        categoryCode: q.topicCode || '',
        options: q.options,
        correctOption: q.answerRaw,
        citation: q.citation,
        contentType: q.contentType,
        optionType: q.optionType,
        payload: {
          correctAnswer: q.answerRaw,
          options: q.options,
          enabled: nextEnabled,
        }
      };
      await learnerQuizService.updateQuestion(q.id, payload);
      setQuestions(prev => prev.map(item => item.id === q.id ? { ...item, enabled: nextEnabled } : item));
    } catch (err) {
      alert('Không thể cập nhật trạng thái câu hỏi.');
    }
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm('Bạn có chắc chắn muốn xoá câu hỏi này?')) return;
    try {
      await learnerQuizService.deleteQuestion(id);
      fetchData();
    } catch (err) {
      alert('Không thể xoá câu hỏi. Vui lòng kiểm tra lại kết nối.');
    }
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    const filteredOptions = options.filter(x => x.trim() !== '');

    const payload = {
      content,
      questionType,
      difficulty,
      durationSeconds,
      categoryCode,
      options: filteredOptions,
      correctOption: correctAnswer,
      citation,
      contentType,
      optionType,
    };

    try {
      if (editingId) {
        await learnerQuizService.updateQuestion(editingId, payload);
      } else {
        await learnerQuizService.createQuestion(payload);
      }
      setIsModalOpen(false);
      fetchData();
    } catch (err) {
      alert('Lỗi lưu thông tin câu hỏi. Vui lòng thử lại.');
    }
  };

  // 3. LUỒNG THAO TÁC CHỦ ĐỀ
  const openAddTopicModal = () => {
    setEditingTopicId(null);
    setTopicCode('');
    setTopicName('');
    setTopicDesc('');
    setTopicCat('');
    setTopicVisibility('PUBLIC');
    setTopicParentId('');
    setIsEditingTopic(true);
    setIsTopicModalOpen(true);
  };

  const openEditTopicModal = (topic: BankTopic) => {
    setEditingTopicId(topic.id);
    setTopicCode(topic.code);
    setTopicName(topic.name);
    setTopicDesc(topic.description || '');
    setTopicCat(topic.categoryCode);
    setTopicVisibility(topic.visibilityScope);
    setTopicParentId(topic.parentId || '');
    setIsEditingTopic(false);
    setIsTopicModalOpen(true);
  };

  const handleDeleteTopic = async (id: string) => {
    const topic = topics.find(t => t.id === id);
    if (!topic) return;

    if (!window.confirm(`Bạn có chắc chắn muốn xoá chủ đề "${topic.name}"?`)) return;

    const deleteQuestions = window.confirm(
      `Bạn có muốn XÓA TOÀN BỘ câu hỏi thuộc chủ đề "${topic.name}" (và tất cả chủ đề con nếu có) không?\n\n` +
      `- Bấm OK: Xóa cả chủ đề + toàn bộ chủ đề con + toàn bộ câu hỏi liên quan.\n` +
      `- Bấm Cancel: Giữ lại câu hỏi (chuyển về chủ đề cha) và nâng cấp các chủ đề con lên cấp trên.`
    );

    try {
      const res: any = await learnerQuizService.deleteTopic(id, deleteQuestions);
      if (res?.message) alert(res.message);
      fetchData();
    } catch (err: any) {
      alert(err?.response?.data?.message || 'Không thể xoá chủ đề. Vui lòng kiểm tra lại kết nối.');
    }
  };

  const handleToggleActiveTopic = async (topic: BankTopic) => {
    if (!hasWritePermission) return;
    const nextEnabled = topic.enabled !== false ? false : true;
    try {
      const payload = {
        code: topic.code,
        name: topic.name,
        description: topic.description || '',
        categoryCode: topic.categoryCode,
        parentId: topic.parentId || null,
        enabled: nextEnabled,
        visibilityScope: topic.visibilityScope
      };
      await learnerQuizService.updateTopic(topic.id, payload);
      setTopics(prev => prev.map(item => item.id === topic.id ? { ...item, enabled: nextEnabled } : item));
      alert(nextEnabled ? 'Đã kích hoạt chủ đề!' : 'Đã vô hiệu hóa chủ đề!');
    } catch (err) {
      alert('Không thể thay đổi trạng thái chủ đề.');
    }
  };

  const handleSaveTopic = async (e: React.FormEvent) => {
    e.preventDefault();
    const existing = topics.find(t => t.id === editingTopicId);
    const isEnabled = existing ? existing.enabled !== false : true;
    const payload = {
      code: topicCode,
      name: topicName,
      description: topicDesc,
      categoryCode: topicCat || topicCode,
      parentId: topicParentId || null,
      enabled: isEnabled,
      visibilityScope: topicVisibility
    };

    try {
      if (editingTopicId) {
        await learnerQuizService.updateTopic(editingTopicId, payload);
      } else {
        await learnerQuizService.createTopic(payload);
      }
      setIsTopicModalOpen(false);
      fetchData();
    } catch (err) {
      alert('Lỗi lưu thông tin chủ đề. Vui lòng thử lại.');
    }
  };

  return (
    <div className="space-y-8 font-sans">
      {missingAnswersCount > 0 && (
        <div className="bg-gradient-to-r from-amber-500/10 to-orange-500/10 border border-amber-500/30 rounded-2xl p-4 flex items-center justify-between gap-4 backdrop-blur-md">
          <div className="flex items-center gap-3">
            <span className="w-10 h-10 bg-amber-500/20 text-amber-400 rounded-xl flex items-center justify-center font-bold text-lg">⚠️</span>
            <div className="text-left text-white">
              <h4 className="text-sm font-bold text-slate-200">Phát hiện câu hỏi chưa có đáp án</h4>
              <p className="text-xs text-slate-400 mt-0.5">Hệ thống quét thấy có {missingAnswersCount} câu hỏi chưa được cấu hình đáp án đúng. Đề thi sẽ tự động ẩn các câu hỏi này để tránh lỗi.</p>
            </div>
          </div>
          <button 
            onClick={() => setShowMissingOnly(!showMissingOnly)}
            className={`px-4 py-2 rounded-xl text-xs font-bold transition-all border cursor-pointer ${showMissingOnly ? 'bg-amber-500 text-slate-950 border-amber-500 font-extrabold shadow-md shadow-amber-500/10' : 'bg-slate-950 hover:bg-slate-850 text-amber-400 border-amber-500/30'}`}
          >
            {showMissingOnly ? 'Hiển thị tất cả' : 'Lọc xử lý ngay'}
          </button>
        </div>
      )}

      <div className="flex flex-col sm:flex-row justify-between sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-black text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-purple-400 tracking-tight">
            Ngân hàng Câu hỏi
          </h1>
          <p className="text-sm text-slate-400 mt-1 font-medium">Bảng điều phối, nhập xuất Excel và quản trị chủ đề.</p>
        </div>
        
        <div className="flex flex-wrap items-center gap-3">
          {/* Simulated Role Switcher */}
          <div className="flex items-center gap-1.5 bg-slate-950/60 border border-slate-850 rounded-xl px-2.5 py-1.5 text-xs select-none">
            <span className="text-[10px] text-slate-500 font-bold uppercase tracking-wider">Vai trò:</span>
            <button
              onClick={() => setUserRoleOverride('admin')}
              className={`px-2.5 py-0.5 rounded-lg text-[10px] font-black transition-colors cursor-pointer ${
                userRoleOverride === 'admin' 
                  ? 'bg-blue-600/20 text-blue-400 border border-blue-500/30' 
                  : 'text-slate-400 hover:text-white border border-transparent'
              }`}
            >
              Admin
            </button>
            <button
              onClick={() => setUserRoleOverride('viewer')}
              className={`px-2.5 py-0.5 rounded-lg text-[10px] font-black transition-colors cursor-pointer ${
                userRoleOverride === 'viewer' 
                  ? 'bg-amber-600/20 text-amber-400 border border-amber-500/30' 
                  : 'text-slate-400 hover:text-white border border-transparent'
              }`}
            >
              Viewer
            </button>
          </div>

          {hasWritePermission && (
            <>
              <button 
                onClick={openAddTopicModal}
                className="flex items-center gap-2 bg-slate-900 hover:bg-slate-850 text-slate-350 border border-slate-800 px-4 py-2.5 rounded-xl font-bold transition-all text-xs cursor-pointer active:scale-95"
              >
                <FolderOpen size={14} /> Thêm Chủ đề mới
              </button>
              <button 
                onClick={openAddModal}
                className="flex items-center gap-2 bg-gradient-to-r from-blue-500 to-purple-500 hover:from-blue-600 hover:to-purple-600 text-white px-5 py-2.5 rounded-xl font-bold transition-all shadow-lg shadow-blue-500/10 active:scale-95 text-xs cursor-pointer"
              >
                <Plus size={14} /> Thêm câu hỏi thủ công
              </button>
            </>
          )}
        </div>
      </div>

      {/* UPLOAD ZONE */}
      <input 
        type="file" 
        ref={fileInputRef} 
        onChange={handleFileChange} 
        accept=".xlsx,.xls,.docx" 
        className="hidden" 
      />
      {hasWritePermission && (
        <div
          onDragOver={(e) => { e.preventDefault(); setDragActive(true); }}
          onDragLeave={() => setDragActive(false)}
          onDrop={(e) => { 
            e.preventDefault(); 
            setDragActive(false); 
            if (e.dataTransfer.files && e.dataTransfer.files[0]) {
              const file = e.dataTransfer.files[0];
              if (file.name.toLowerCase().endsWith('.docx')) {
                handleFileImportDocx(file);
              } else {
                handleFileImport(file);
              }
            }
          }}
          onClick={onButtonClick}
          className={`border-2 border-dashed rounded-3xl p-10 text-center transition-all cursor-pointer backdrop-blur-md relative overflow-hidden group
            ${dragActive 
              ? 'border-blue-500 bg-blue-500/10' 
              : 'border-slate-800 bg-slate-900/40 hover:border-slate-700 hover:bg-slate-900/60'}`}
        >
          <div className="absolute top-0 right-0 w-24 h-24 bg-blue-500/5 rounded-full blur-2xl group-hover:scale-150 transition-transform" />
          <div className="w-16 h-16 bg-slate-950/60 border border-slate-800 rounded-full flex items-center justify-center text-slate-400 mx-auto mb-4 group-hover:scale-110 transition-transform">
            <FileUp size={28} />
          </div>
          <h3 className="font-bold text-lg text-slate-200">Kéo thả file Excel (.xlsx) / Word (.docx) hoặc Nhấp vào đây để tải lên</h3>
          <p className="text-xs text-slate-500 mt-2 font-medium max-w-md mx-auto leading-relaxed">
            Nhập nhanh câu hỏi qua <span className="text-green-400 font-semibold">Excel</span> hoặc tải lên tệp <span className="text-purple-400 font-semibold">Word (DOCX)</span> chứa câu hỏi để Gemini AI tự giải đáp án và căn cứ pháp lý.
          </p>
        </div>
      )}

      {/* TAB SELECTOR */}
      <div className="flex border-b border-slate-800 gap-6">
        <button 
          onClick={() => setActiveTab('questions')}
          className={`pb-3 text-sm font-black tracking-wide border-b-2 transition-all cursor-pointer uppercase ${activeTab === 'questions' ? 'border-blue-500 text-blue-400' : 'border-transparent text-slate-500 hover:text-slate-305'}`}
        >
          Ngân hàng Câu hỏi ({questions.length})
        </button>
        <button 
          onClick={() => setActiveTab('topics')}
          className={`pb-3 text-sm font-black tracking-wide border-b-2 transition-all cursor-pointer uppercase ${activeTab === 'topics' ? 'border-blue-500 text-blue-400' : 'border-transparent text-slate-500 hover:text-slate-305'}`}
        >
          Quản lý Chủ đề ({topics.length})
        </button>
      </div>

      {/* DYNAMIC TAB BODY */}
      <div className="bg-slate-900/60 rounded-3xl border border-slate-800 overflow-hidden backdrop-blur-xl">
        <div className="p-6 border-b border-slate-850 bg-slate-900/30 flex items-center justify-between">
          <h2 className="text-lg font-bold text-slate-200 flex items-center gap-2">
            {activeTab === 'questions' ? (
              <>
                <HelpCircle size={20} className="text-purple-400" /> Ngân hàng Câu hỏi Sát hạch
              </>
            ) : (
              <>
                <Layers size={20} className="text-blue-400" /> Danh mục Chủ đề hệ thống
              </>
            )}
          </h2>
          <button 
            onClick={fetchData}
            className="text-xs text-slate-400 hover:text-white font-bold tracking-wider uppercase bg-slate-950/60 border border-slate-850 px-3.5 py-1.5 rounded-full cursor-pointer transition-colors"
          >
            Làm mới ↻
          </button>
        </div>

        {/* BỘ LỌC TÌM KIẾM CHUYÊN NGHIỆP */}
        <div className="p-6 border-b border-slate-850 bg-slate-900/10 grid grid-cols-1 sm:grid-cols-4 gap-4">
          {activeTab === 'questions' ? (
            <>
              {/* Ô tìm kiếm câu hỏi */}
              <div className="space-y-1">
                <label className="text-[10px] font-black uppercase text-slate-500 tracking-wider">Tìm kiếm nội dung</label>
                <input 
                  type="text"
                  placeholder="Nhập từ khóa hoặc ID..."
                  value={qSearch}
                  onChange={(e) => setQSearch(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500"
                />
              </div>

              {/* Lọc loại câu hỏi */}
              <div className="space-y-1">
                <label className="text-[10px] font-black uppercase text-slate-500 tracking-wider">Loại câu hỏi</label>
                <select
                  value={qTypeFilter}
                  onChange={(e) => setQTypeFilter(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500 cursor-pointer"
                >
                  <option value="">Tất cả các loại</option>
                  <option value="SINGLE">Trắc nghiệm 1 đáp án</option>
                  <option value="MULTI">Nhiều đáp án đúng</option>
                  <option value="TRUE_FALSE">Đúng / Sai</option>
                  <option value="SHORT_ANSWER">Trả lời ngắn</option>
                  <option value="FILL_BLANK">Điền vào chỗ trống</option>
                  <option value="ORDERING">Sắp xếp thứ tự</option>
                  <option value="MATCHING">Nối cặp</option>
                  <option value="ESSAY">Tự luận (AI chấm)</option>
                </select>
              </div>

              {/* Lọc chủ đề */}
              <div className="space-y-1">
                <label className="text-[10px] font-black uppercase text-slate-500 tracking-wider">Theo chủ đề</label>
                <select
                  value={qTopicFilter}
                  onChange={(e) => setQTopicFilter(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500 cursor-pointer"
                >
                  <option value="">Tất cả chủ đề</option>
                  {topics.map(t => (
                    <option key={t.code} value={t.code}>{getTopicFullPath(t, topics)}</option>
                  ))}
                </select>
              </div>

              {/* Lọc theo trạng thái đáp án */}
              <div className="space-y-1">
                <label className="text-[10px] font-black uppercase text-slate-500 tracking-wider">Trạng thái đáp án</label>
                <select
                  value={showMissingOnly ? "missing" : "all"}
                  onChange={(e) => setShowMissingOnly(e.target.value === "missing")}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500 cursor-pointer"
                >
                  <option value="all">Tất cả câu hỏi</option>
                  <option value="missing">Chưa cấu hình đáp án</option>
                </select>
              </div>
            </>
          ) : (
            <div className="sm:col-span-3 space-y-1">
              <label className="text-[10px] font-black uppercase text-slate-500 tracking-wider">Tìm kiếm chủ đề</label>
              <input 
                type="text"
                placeholder="Tìm theo tên chủ đề, mô tả hoặc mã chủ đề..."
                value={tSearch}
                onChange={(e) => setTSearch(e.target.value)}
                className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500"
              />
            </div>
          )}
        </div>

        <div className="overflow-x-auto">
          {loading ? (
            <div className="py-20 flex flex-col items-center justify-center gap-3">
              <Loader2 className="animate-spin text-blue-500" size={32} />
              <p className="text-xs text-slate-400 font-medium">Đang đồng bộ hóa dữ liệu...</p>
            </div>
          ) : activeTab === 'questions' ? (
            <div className="w-full overflow-x-auto">
              <table className="w-full text-left border-collapse min-w-[700px]">
                <thead>
                  <tr className="border-b border-slate-850 text-slate-400 text-xs font-black tracking-wider uppercase bg-slate-900/20 select-none">
                    <th className="p-4 w-20 text-center cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => handleQSort('stt')}>
                      <div className="flex items-center justify-center">
                        STT {renderSortIcon('stt', qSortField, qSortAsc)}
                      </div>
                    </th>
                    <th className="p-4 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => handleQSort('content')}>
                      <div className="flex items-center">
                        Nội dung câu hỏi {renderSortIcon('content', qSortField, qSortAsc)}
                      </div>
                    </th>
                    <th className="p-4 w-32 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => handleQSort('questionType')}>
                      <div className="flex items-center">
                        Loại {renderSortIcon('questionType', qSortField, qSortAsc)}
                      </div>
                    </th>
                    <th className="p-4 w-44 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => handleQSort('topicCode')}>
                      <div className="flex items-center">
                        Chủ đề {renderSortIcon('topicCode', qSortField, qSortAsc)}
                      </div>
                    </th>
                    <th className="p-4 w-32 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => handleQSort('difficulty')}>
                      <div className="flex items-center">
                        Độ khó {renderSortIcon('difficulty', qSortField, qSortAsc)}
                      </div>
                    </th>
                    <th className="p-4 w-24 text-right sticky right-0 bg-slate-900 z-10 border-l border-slate-800 shadow-[-4px_0_8px_rgba(0,0,0,0.15)]">Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {pagedQuestions.map((q: LearnerQuestion, idx: number) => {
                    const matchedTopic = topics.find(t => t.code === q.topicCode || t.categoryCode === q.topicCode);
                    return (
                      <tr key={q.id || idx} className="border-b border-slate-850/60 hover:bg-slate-800/10 transition-colors">
                        <td className="p-4 text-center text-xs text-slate-450 font-bold" title={`ID: ${q.id}`}>
                          {(qPage - 1) * qPageSize + idx + 1}
                        </td>
                        <td className="p-4 font-bold text-slate-200 text-sm leading-relaxed">
                          <div>{q.content}</div>
                          {(() => {
                            const correct = getCorrectAnswerDisplay(q);
                            if (!correct) {
                              return (
                                <div className="mt-2 flex items-center gap-2 flex-wrap text-xs font-semibold select-none">
                                  <span className="text-amber-450 bg-amber-500/10 px-2.5 py-0.5 rounded-lg border border-amber-500/20 text-[10px] uppercase tracking-wider font-black">
                                    ⚠️ Chưa cấu hình đáp án đúng!
                                  </span>
                                </div>
                              );
                            }
                            return (
                              <div className="mt-2 flex items-center gap-2 flex-wrap text-xs font-semibold select-none">
                                <span className="text-emerald-450 bg-emerald-500/10 px-2.5 py-0.5 rounded-lg border border-emerald-500/20 text-[10px] uppercase tracking-wider font-black">
                                  Đáp án đúng: {correct.label}
                                </span>
                                {correct.text && (
                                  <span className="text-slate-400 font-medium italic text-sm max-w-lg truncate" title={correct.text}>
                                    ({correct.text})
                                  </span>
                                )}
                              </div>
                            );
                          })()}
                        </td>
                        <td className="p-4">
                          <span className="px-2.5 py-1 rounded-lg text-[10px] font-black uppercase tracking-wider bg-blue-500/10 text-blue-400 border border-blue-500/20">
                            {getQuestionTypeLabel(q.questionType)}
                          </span>
                        </td>
                        <td className="p-4 text-slate-355 text-xs">
                          {matchedTopic ? `${getTopicFullPath(matchedTopic, topics)}` : q.topicCode || 'Chưa phân nhóm'}
                        </td>
                        <td className="p-4">
                          <div className="flex gap-0.5 text-amber-500">
                            {Array.from({ length: q.difficulty || 1 }).map((_, i) => (
                              <Star key={i} size={12} fill="currentColor" />
                            ))}
                          </div>
                        </td>
                        <td className="p-4 text-right sticky right-0 bg-slate-900/95 z-10 border-l border-slate-850/60 shadow-[-4px_0_8px_rgba(0,0,0,0.15)] transition-colors">
                          <div className="flex items-center justify-end gap-2">
                            <button 
                              onClick={() => {
                                openEditModal(q);
                                setIsEditing(false);
                              }}
                              className="p-2 text-slate-400 hover:text-white hover:bg-slate-800 rounded-lg transition-colors cursor-pointer" 
                              title="Xem chi tiết"
                            >
                              <Eye size={14} />
                            </button>
                            {(!q.answerRaw || !q.answerRaw.trim()) && hasWritePermission && (
                              <button 
                                onClick={() => handleSuggestAnswer(q)}
                                className="p-2 text-purple-400 hover:text-purple-300 hover:bg-purple-950/20 rounded-lg transition-colors cursor-pointer animate-pulse" 
                                title="Tự động giải bằng AI"
                              >
                                <Sparkles size={14} />
                              </button>
                            )}
                            <button 
                              onClick={() => handleToggleActive(q)}
                              className={`p-2 rounded-lg transition-colors cursor-pointer ${
                                q.enabled !== false
                                  ? 'text-emerald-450 hover:text-emerald-300 hover:bg-emerald-950/20'
                                  : 'text-slate-500 hover:text-slate-300 hover:bg-slate-850'
                              } ${!hasWritePermission ? 'opacity-50 cursor-not-allowed' : ''}`}
                              title={q.enabled !== false ? "Vô hiệu hóa" : "Kích hoạt"}
                            >
                              {q.enabled !== false ? <ToggleRight size={18} /> : <ToggleLeft size={18} />}
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                  {pagedQuestions.length === 0 && (
                    <tr>
                      <td colSpan={6} className="text-center py-20 text-slate-500 text-xs font-bold">
                        Không tìm thấy câu hỏi nào phù hợp với bộ lọc.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>

              {/* PHÂN TRANG CÂU HỎI */}
              <div className="p-4 border-t border-slate-850 flex flex-col sm:flex-row items-center justify-between gap-4 bg-slate-900/10">
                <span className="text-xs text-slate-400 font-semibold">
                  Hiển thị từ {filteredQuestions.length > 0 ? (qPage - 1) * qPageSize + 1 : 0} đến {Math.min(qPage * qPageSize, filteredQuestions.length)} trong tổng số {filteredQuestions.length} câu hỏi
                </span>
                <div className="flex items-center gap-1.5">
                  <button
                    onClick={() => setQPage(p => Math.max(1, p - 1))}
                    disabled={qPage === 1}
                    className="px-3 py-1.5 rounded-lg bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white border border-slate-855 disabled:opacity-40 disabled:cursor-not-allowed text-xs font-bold transition-all"
                  >
                    Trước
                  </button>
                  {Array.from({ length: qTotalPages }).map((_, idx) => {
                    const pNum = idx + 1;
                    if (pNum === 1 || pNum === qTotalPages || Math.abs(pNum - qPage) <= 1) {
                      return (
                        <button
                          key={pNum}
                          onClick={() => setQPage(pNum)}
                          className={`px-3 py-1.5 rounded-lg border text-xs font-bold transition-all ${
                            qPage === pNum
                              ? 'bg-blue-650 border-blue-500 text-white shadow'
                              : 'bg-slate-950 border-slate-850 text-slate-400 hover:bg-slate-850 hover:text-white'
                          }`}
                        >
                          {pNum}
                        </button>
                      );
                    }
                    if (pNum === 2 || pNum === qTotalPages - 1) {
                      return <span key={pNum} className="text-slate-600 text-xs px-1 select-none">...</span>;
                    }
                    return null;
                  })}
                  <button
                    onClick={() => setQPage(p => Math.min(qTotalPages, p + 1))}
                    disabled={qPage === qTotalPages || qTotalPages === 0}
                    className="px-3 py-1.5 rounded-lg bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white border border-slate-855 disabled:opacity-40 disabled:cursor-not-allowed text-xs font-bold transition-all"
                  >
                    Sau
                  </button>
                </div>
              </div>
            </div>
          ) : (
            <div className="w-full overflow-x-auto">
              <table className="w-full text-left border-collapse min-w-[700px]">
                <thead>
                  <tr className="border-b border-slate-850 text-slate-400 text-xs font-black tracking-wider uppercase bg-slate-900/20 select-none">
                    <th className="p-4 w-40 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => handleTSort('code')}>
                      <div className="flex items-center">
                        Mã chủ đề {renderSortIcon('code', tSortField, tSortAsc)}
                      </div>
                    </th>
                    <th className="p-4 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => handleTSort('name')}>
                      <div className="flex items-center">
                        Tên chủ đề {renderSortIcon('name', tSortField, tSortAsc)}
                      </div>
                    </th>
                    <th className="p-4">
                      <div className="flex items-center text-slate-400">
                        Đường dẫn (Path)
                      </div>
                    </th>
                    <th className="p-4 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => handleTSort('description')}>
                      <div className="flex items-center">
                        Mô tả {renderSortIcon('description', tSortField, tSortAsc)}
                      </div>
                    </th>
                    <th className="p-4 w-40 cursor-pointer hover:bg-slate-850/50 hover:text-white group transition-colors" onClick={() => handleTSort('visibilityScope')}>
                      <div className="flex items-center">
                        Phạm vi hiển thị {renderSortIcon('visibilityScope', tSortField, tSortAsc)}
                      </div>
                    </th>
                    <th className="p-4 w-24 text-right sticky right-0 bg-slate-900 z-10 border-l border-slate-800 shadow-[-4px_0_8px_rgba(0,0,0,0.15)]">Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {pagedTopics.map((t: BankTopic, idx: number) => (
                    <tr key={t.id || idx} className="border-b border-slate-850/60 hover:bg-slate-800/10 transition-colors">
                      <td className="p-4 font-mono text-xs font-black text-blue-400 select-all">
                        {t.code}
                      </td>
                      <td className="p-4 font-bold text-slate-200 text-sm">
                        {t.name}
                      </td>
                      <td className="p-4 text-xs text-slate-300">
                        {getTopicFullPath(t, topics).replace(/ → /g, '/')}
                      </td>
                      <td className="p-4 text-xs text-slate-400 leading-relaxed max-w-xs truncate">
                        {t.description || '_'}
                      </td>
                      <td className="p-4">
                        <span className={`px-2 py-0.5 rounded text-[10px] font-black tracking-wider uppercase border ${
                          t.visibilityScope === 'PUBLIC' 
                            ? 'bg-green-500/10 text-green-400 border-green-500/20' 
                            : 'bg-purple-500/10 text-purple-400 border-purple-500/20'
                        }`}>
                          {t.visibilityScope}
                        </span>
                      </td>
                      <td className="p-4 text-right sticky right-0 bg-slate-900/95 z-10 border-l border-slate-850/60 shadow-[-4px_0_8px_rgba(0,0,0,0.15)] transition-colors">
                        <div className="flex items-center justify-end gap-2">
                          <button 
                            onClick={() => {
                              openEditTopicModal(t);
                              setIsEditingTopic(false);
                            }}
                            className="p-2 text-slate-400 hover:text-white hover:bg-slate-800 rounded-lg transition-colors cursor-pointer" 
                            title="Xem chi tiết"
                          >
                            <Eye size={14} />
                          </button>
                          <button 
                            onClick={() => handleToggleActiveTopic(t)}
                            className={`p-2 rounded-lg transition-colors cursor-pointer ${
                              t.enabled !== false
                                ? 'text-emerald-450 hover:text-emerald-300 hover:bg-emerald-950/20'
                                : 'text-slate-500 hover:text-slate-300 hover:bg-slate-850'
                            } ${!hasWritePermission ? 'opacity-50 cursor-not-allowed' : ''}`}
                            title={t.enabled !== false ? "Vô hiệu hóa" : "Kích hoạt"}
                          >
                            {t.enabled !== false ? <ToggleRight size={18} /> : <ToggleLeft size={18} />}
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                  {pagedTopics.length === 0 && (
                    <tr>
                      <td colSpan={5} className="text-center py-20 text-slate-500 text-xs font-bold">
                        Không tìm thấy chủ đề nào phù hợp với bộ lọc.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>

              {/* PHÂN TRANG CHỦ ĐỀ */}
              <div className="p-4 border-t border-slate-850 flex flex-col sm:flex-row items-center justify-between gap-4 bg-slate-900/10">
                <span className="text-xs text-slate-400 font-semibold">
                  Hiển thị từ {filteredTopics.length > 0 ? (tPage - 1) * tPageSize + 1 : 0} đến {Math.min(tPage * tPageSize, filteredTopics.length)} trong tổng số {filteredTopics.length} chủ đề
                </span>
                <div className="flex items-center gap-1.5">
                  <button
                    onClick={() => setTPage(p => Math.max(1, p - 1))}
                    disabled={tPage === 1}
                    className="px-3 py-1.5 rounded-lg bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white border border-slate-855 disabled:opacity-40 disabled:cursor-not-allowed text-xs font-bold transition-all"
                  >
                    Trước
                  </button>
                  {Array.from({ length: tTotalPages }).map((_, idx) => {
                    const pNum = idx + 1;
                    if (pNum === 1 || pNum === tTotalPages || Math.abs(pNum - tPage) <= 1) {
                      return (
                        <button
                          key={pNum}
                          onClick={() => setTPage(pNum)}
                          className={`px-3 py-1.5 rounded-lg border text-xs font-bold transition-all ${
                            tPage === pNum
                              ? 'bg-blue-650 border-blue-500 text-white shadow'
                              : 'bg-slate-950 border-slate-850 text-slate-400 hover:bg-slate-850 hover:text-white'
                          }`}
                        >
                          {pNum}
                        </button>
                      );
                    }
                    if (pNum === 2 || pNum === tTotalPages - 1) {
                      return <span key={pNum} className="text-slate-600 text-xs px-1 select-none">...</span>;
                    }
                    return null;
                  })}
                  <button
                    onClick={() => setTPage(p => Math.min(tTotalPages, p + 1))}
                    disabled={tPage === tTotalPages || tTotalPages === 0}
                    className="px-3 py-1.5 rounded-lg bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white border border-slate-855 disabled:opacity-40 disabled:cursor-not-allowed text-xs font-bold transition-all"
                  >
                    Sau
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* ======================================================== */}
      {/* 🛠️ ADD / EDIT QUESTION MODAL */}
      {/* ======================================================== */}
      {isModalOpen && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="w-full max-w-3xl lg:max-w-6xl bg-slate-900 border border-slate-800 rounded-3xl p-6 md:p-8 shadow-2xl relative space-y-6 max-h-[95vh] md:max-h-[90vh] overflow-y-auto">
            <button 
              onClick={() => setIsModalOpen(false)}
              className="absolute top-4 right-4 text-slate-400 hover:text-white p-1.5 hover:bg-slate-800 rounded-xl transition-colors cursor-pointer"
            >
              <X size={18} />
            </button>

            <div>
              <h2 className="text-xl font-black text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-purple-400">
                {isEditing ? (editingId ? 'Cập nhật Câu hỏi' : 'Tạo Câu hỏi Mới') : 'Chi tiết Câu hỏi'}
              </h2>
              <p className="text-[10px] text-slate-500 font-bold uppercase tracking-widest mt-1">Cấu hình ngân hàng đề thi</p>
            </div>

            <form onSubmit={handleSave} className="space-y-6">
              <div className="grid grid-cols-1 lg:grid-cols-5 gap-6">
                
                {/* 📝 PANEL TRÁI: Nội dung câu hỏi & Các phương án trả lời */}
                <div className="lg:col-span-3 space-y-4 bg-slate-950/20 p-4 rounded-2xl border border-slate-800/40">
                  <h3 className="text-xs font-black uppercase text-slate-400 tracking-wider">Nội dung & Phương án</h3>

                  {/* Nội dung câu hỏi */}
                  <div className="space-y-1">
                    <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider">Nội dung câu hỏi</label>
                    <textarea 
                      required
                      placeholder="Điền nội dung câu hỏi sát hạch..." 
                      value={content}
                      onChange={(e) => setContent(e.target.value)}
                      disabled={!isEditing}
                      className="w-full h-24 bg-slate-950/80 border border-slate-800 rounded-xl px-4 py-3 text-sm text-white placeholder-slate-600 focus:outline-none focus:border-blue-500 transition-colors resize-none disabled:opacity-75 disabled:cursor-not-allowed"
                    />
                  </div>

                  {/* Option Inputs (hiển thị nếu loại câu hỏi có phương án) */}
                  {(questionType === 'SINGLE' || questionType === 'MULTI' || questionType === 'ORDERING' || questionType === 'MATCHING') ? (
                    <div className="space-y-2.5">
                      <div>
                        <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider block">Các phương án lựa chọn (Cho phép tự xuống dòng)</label>
                        {questionType === 'MATCHING' && (
                          <p className="text-[9px] text-cyan-400 font-bold mt-0.5">Định dạng nối cặp: Vế trái =&gt; Vế phải (Ví dụ: Hà Nội =&gt; Việt Nam)</p>
                        )}
                        {questionType === 'ORDERING' && (
                          <p className="text-[9px] text-cyan-400 font-bold mt-0.5">Nhập các phần tử cần sắp xếp thứ tự vào từng phương án dưới đây.</p>
                        )}
                      </div>
                      
                      <div className="space-y-2">
                        {options.map((opt, idx) => {
                          const isCorrect = questionType === 'SINGLE'
                            ? correctAnswer === String(idx + 1)
                            : correctAnswer.split(/[;,]+/).map(x => x.trim()).filter(Boolean).includes(String(idx + 1));
                          
                          return (
                            <div key={idx} className="flex items-start gap-2">
                              <div className="flex flex-col items-center gap-1 shrink-0 mt-1">
                                <span className="w-12 text-center text-[10px] font-black py-2 rounded-xl bg-slate-950 border border-slate-800 text-slate-400 select-none">
                                  {LETTERS[idx] ?? String(idx + 1)} ({idx + 1})
                                </span>
                                {(questionType === 'SINGLE' || questionType === 'MULTI') && (
                                  <label className={`flex items-center gap-1 cursor-pointer px-1.5 py-0.5 rounded-lg border transition-all select-none ${
                                    isCorrect 
                                      ? 'bg-emerald-950/30 border-emerald-500/50 text-emerald-400 font-bold' 
                                      : 'bg-slate-950/40 border-slate-850 text-slate-500 hover:border-slate-850'
                                  } ${!isEditing ? 'pointer-events-none opacity-80' : ''}`}>
                                    <input 
                                      type={questionType === 'SINGLE' ? 'radio' : 'checkbox'}
                                      name="correct-answer-toggle"
                                      checked={isCorrect}
                                      disabled={!isEditing}
                                      onChange={() => {
                                        if (questionType === 'SINGLE') {
                                          setCorrectAnswer(String(idx + 1));
                                        } else {
                                          const current = correctAnswer.split(/[;,]+/).map(x => x.trim()).filter(Boolean);
                                          const next = current.includes(String(idx + 1))
                                            ? current.filter(x => x !== String(idx + 1))
                                            : [...current, String(idx + 1)];
                                          setCorrectAnswer(next.sort((a, b) => Number(a) - Number(b)).join(','));
                                        }
                                      }}
                                      className="w-3 h-3 text-emerald-600 bg-slate-950 border-slate-800 rounded focus:ring-emerald-500 cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
                                    />
                                    <span className="text-[9px]">Đúng</span>
                                  </label>
                                )}
                              </div>
                              <textarea 
                                placeholder={questionType === 'MATCHING' ? `Cặp ${idx + 1} (Ví dụ: Chó => Sủa)` : `Phương án ${LETTERS[idx] ?? String(idx + 1)}`} 
                                value={opt} 
                                onChange={(e) => {
                                  const next = [...options];
                                  next[idx] = e.target.value;
                                  setOptions(next);
                                }}
                                disabled={!isEditing}
                                className="flex-1 bg-slate-950/60 border border-slate-800 rounded-xl px-3.5 py-2 text-xs text-white focus:outline-none focus:border-blue-500 resize-none h-14 disabled:opacity-75 disabled:cursor-not-allowed"
                              />
                              {isEditing && options.length > 2 && (
                                <button
                                  type="button"
                                  onClick={() => {
                                    const next = options.filter((_, i) => i !== idx);
                                    setOptions(next);
                                    // Adjust correct answers
                                    if (questionType === 'SINGLE' || questionType === 'MULTI') {
                                      const current = correctAnswer.split(/[;,]+/).map(x => x.trim()).filter(Boolean);
                                      const adjusted = current
                                        .map(val => {
                                          const num = Number(val);
                                          if (num === idx + 1) return null; // deleted
                                          if (num > idx + 1) return String(num - 1); // shifted down
                                          return val;
                                        })
                                        .filter((x): x is string => x !== null);
                                      setCorrectAnswer(adjusted.join(','));
                                    }
                                  }}
                                  className="p-2.5 bg-red-950/40 hover:bg-red-900/40 text-red-400 rounded-xl border border-red-900/20 transition-colors mt-1.5 cursor-pointer active:scale-95"
                                  title="Xóa phương án này"
                                >
                                  <Trash2 size={14} />
                                </button>
                              )}
                            </div>
                          );
                        })}

                        {isEditing && (
                          <button
                            type="button"
                            onClick={() => setOptions([...options, ''])}
                            className="flex items-center gap-1.5 text-xs text-blue-400 hover:text-blue-300 font-bold px-3.5 py-2 rounded-xl border border-dashed border-blue-900/60 bg-blue-950/15 hover:bg-blue-950/30 transition-all active:scale-95 cursor-pointer ml-14 w-fit"
                          >
                            <Plus size={12} /> Thêm phương án {LETTERS[options.length] ?? String(options.length + 1)} ({options.length + 1})
                          </button>
                        )}
                      </div>
                    </div>
                  ) : (
                    <div className="p-6 border border-dashed border-slate-850 rounded-2xl bg-slate-950/20 text-slate-500 text-center min-h-[200px] flex flex-col items-center justify-center">
                      <p className="text-xs font-semibold">Loại câu hỏi này không yêu cầu phương án lựa chọn.</p>
                      <p className="text-[10px] text-slate-600 mt-1">Đáp án đúng sẽ được điền trực tiếp vào ô tương ứng ở cột bên phải.</p>
                    </div>
                  )}
                </div>

                {/* ⚙️ PANEL PHẢI: Cấu hình phân nhóm & Đáp án chính xác */}
                <div className="lg:col-span-2 space-y-4 bg-slate-950/20 p-4 rounded-2xl border border-slate-800/40 flex flex-col justify-between">
                  <div className="space-y-4">
                    <h3 className="text-xs font-black uppercase text-slate-400 tracking-wider">Cấu hình & Đáp án</h3>

                    {/* Hàng Loại câu hỏi và Đáp án chính xác */}
                    <div className="grid grid-cols-2 gap-4">
                      {/* Loại câu hỏi */}
                      <div className="space-y-1">
                        <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider">Loại câu hỏi</label>
                        <select 
                          value={questionType}
                          onChange={(e) => setQuestionType(e.target.value as QuestionType)}
                          disabled={!isEditing}
                          className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-white focus:outline-none focus:border-blue-500 transition-colors cursor-pointer disabled:opacity-75 disabled:cursor-not-allowed"
                        >
                          <option value="SINGLE">Trắc nghiệm 1 đáp án</option>
                          <option value="MULTI">Nhiều đáp án đúng</option>
                          <option value="TRUE_FALSE">Đúng / Sai</option>
                          <option value="SHORT_ANSWER">Trả lời ngắn</option>
                          <option value="FILL_BLANK">Điền vào chỗ trống</option>
                          <option value="ORDERING">Sắp xếp thứ tự</option>
                          <option value="MATCHING">Nối cặp</option>
                          <option value="ESSAY">Tự luận (AI chấm)</option>
                        </select>
                      </div>

                      {/* Đáp án chính xác */}
                      <div className="space-y-1">
                        <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider block">Đáp án chính xác</label>
                        {questionType === 'TRUE_FALSE' ? (
                          <div className="flex gap-2 pt-0.5">
                            <button
                              type="button"
                              onClick={() => setCorrectAnswer('1')}
                              disabled={!isEditing}
                              className={`flex-1 py-2 px-3 rounded-xl border text-xs font-black transition-all cursor-pointer disabled:opacity-75 disabled:cursor-not-allowed ${
                                correctAnswer === '1'
                                  ? 'bg-emerald-950/40 border-emerald-500 text-emerald-400 shadow-md shadow-emerald-950/50'
                                  : 'bg-slate-950 border-slate-800 text-slate-500 hover:border-slate-700 hover:text-slate-400'
                              }`}
                            >
                              Đúng (1)
                            </button>
                            <button
                              type="button"
                              onClick={() => setCorrectAnswer('2')}
                              disabled={!isEditing}
                              className={`flex-1 py-2 px-3 rounded-xl border text-xs font-black transition-all cursor-pointer disabled:opacity-75 disabled:cursor-not-allowed ${
                                correctAnswer === '2'
                                  ? 'bg-rose-950/40 border-rose-500 text-rose-400 shadow-md shadow-rose-950/50'
                                  : 'bg-slate-950 border-slate-800 text-slate-500 hover:border-slate-700 hover:text-slate-400'
                              }`}
                            >
                              Sai (2)
                            </button>
                          </div>
                        ) : (
                          <input 
                            type="text" 
                            required={questionType !== 'ESSAY'}
                            placeholder={getCorrectAnswerPlaceholder()} 
                            value={correctAnswer} 
                            onChange={(e) => setCorrectAnswer(e.target.value)}
                            disabled={!isEditing}
                            className={`w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-xs text-white focus:outline-none focus:border-blue-500 disabled:opacity-75 disabled:cursor-not-allowed ${
                              (questionType === 'SINGLE' || questionType === 'MULTI') ? 'opacity-70 bg-slate-950/40 pointer-events-none' : ''
                            }`}
                            readOnly={questionType === 'SINGLE' || questionType === 'MULTI'}
                            title={(questionType === 'SINGLE' || questionType === 'MULTI') ? "Vui lòng tích chọn trực tiếp phương án đúng ở bên trái" : ""}
                          />
                        )}
                      </div>
                    </div>

                    {/* Hàng định dạng Nội dung và Phương án trên cùng 1 dòng */}
                    <div className="grid grid-cols-2 gap-4">
                      {/* Định dạng Nội dung */}
                      <div className="space-y-1">
                        <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider">Định dạng Nội dung</label>
                        <select 
                          value={contentType}
                          onChange={(e) => setContentType(e.target.value)}
                          disabled={!isEditing}
                          className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-white focus:outline-none focus:border-blue-500 transition-colors cursor-pointer disabled:opacity-75 disabled:cursor-not-allowed"
                        >
                          <option value="text">Văn bản thường (Text)</option>
                          <option value="image">Hình ảnh (Image URL)</option>
                          <option value="audio">Âm thanh (Audio URL)</option>
                          <option value="video">Phim / Video (Video URL)</option>
                        </select>
                      </div>

                      {/* Định dạng Phương án */}
                      <div className="space-y-1">
                        <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider">Định dạng Phương án</label>
                        <select 
                          value={optionType}
                          onChange={(e) => setOptionType(e.target.value)}
                          disabled={!isEditing || !(questionType === 'SINGLE' || questionType === 'MULTI' || questionType === 'ORDERING' || questionType === 'MATCHING')}
                          className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-white focus:outline-none focus:border-blue-500 transition-colors cursor-pointer disabled:opacity-40 disabled:cursor-not-allowed"
                        >
                          <option value="text">Văn bản thường (Text)</option>
                          <option value="image">Hình ảnh (Image URL)</option>
                          <option value="audio">Âm thanh (Audio URL)</option>
                          <option value="video">Phim / Video (Video URL)</option>
                        </select>
                      </div>
                    </div>

                    {/* Chủ đề phân nhóm */}
                    <div className="space-y-1">
                      <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider">Chủ đề phân nhóm</label>
                      {topics.length > 0 ? (
                        <select 
                          value={categoryCode}
                          onChange={(e) => setCategoryCode(e.target.value)}
                          disabled={!isEditing}
                          className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-white focus:outline-none focus:border-blue-500 transition-colors cursor-pointer disabled:opacity-75 disabled:cursor-not-allowed"
                        >
                          {topics.map(t => (
                            <option key={t.code} value={t.code}>{getTopicFullPath(t, topics)} ({t.code})</option>
                          ))}
                        </select>
                      ) : (
                        <div className="text-xs text-amber-500 p-2 bg-amber-500/10 rounded-xl border border-amber-500/20 font-bold">
                          Cần tạo Chủ đề trước!
                        </div>
                      )}
                    </div>

                    {/* Độ khó + Thời lượng */}
                    <div className="grid grid-cols-2 gap-4">
                      <div className="space-y-1">
                        <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider">Độ khó (1-5 Sao)</label>
                        <input 
                          type="number"
                          min={1}
                          max={5}
                          required
                          value={difficulty}
                          onChange={(e) => setDifficulty(Number(e.target.value))}
                          disabled={!isEditing}
                          className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500 transition-colors disabled:opacity-75 disabled:cursor-not-allowed"
                        />
                      </div>

                      <div className="space-y-1">
                        <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider">Thời gian (Giây)</label>
                        <input 
                          type="number"
                          min={10}
                          required
                          value={durationSeconds}
                          onChange={(e) => setDurationSeconds(Number(e.target.value))}
                          disabled={!isEditing}
                          className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500 transition-colors disabled:opacity-75 disabled:cursor-not-allowed"
                        />
                      </div>
                    </div>

                    {/* Trích dẫn nguồn câu hỏi (textarea 2 dòng) */}
                    <div className="space-y-1">
                      <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider block">Trích dẫn nguồn câu hỏi (Tài liệu tham khảo)</label>
                      <textarea 
                        placeholder="Ví dụ: Điều 12 Luật Doanh nghiệp hoặc TT 39/2016/TT-NHNN" 
                        value={citation} 
                        onChange={(e) => setCitation(e.target.value)}
                        rows={2}
                        disabled={!isEditing}
                        className="w-full bg-slate-950/60 border border-slate-800 rounded-xl px-3.5 py-2 text-xs text-white focus:outline-none focus:border-blue-500 transition-colors resize-none h-14 disabled:opacity-75 disabled:cursor-not-allowed"
                      />
                    </div>
                  </div>

                  {/* Action Buttons */}
                  {!isEditing ? (
                    <div className="space-y-3 pt-4 border-t border-slate-800/80">
                      {hasWritePermission ? (
                        <>
                          <div className="grid grid-cols-4 gap-2">
                            <button
                              type="button"
                              onClick={() => {
                                openAddModal();
                                setIsEditing(true);
                              }}
                              className="py-2.5 bg-slate-950 hover:bg-slate-850 text-blue-450 border border-slate-850 rounded-xl font-bold transition-all text-[10px] flex flex-col items-center justify-center gap-1 cursor-pointer"
                              title="Tạo câu hỏi mới"
                            >
                              <Plus size={14} /> Mới
                            </button>
                            <button
                              type="button"
                              onClick={() => setIsEditing(true)}
                              className="py-2.5 bg-slate-950 hover:bg-slate-850 text-amber-450 border border-slate-850 rounded-xl font-bold transition-all text-[10px] flex flex-col items-center justify-center gap-1 cursor-pointer"
                              title="Chỉnh sửa câu hỏi"
                            >
                              <Edit size={14} /> Sửa
                            </button>
                            <button
                              type="button"
                              onClick={async () => {
                                if (editingId) {
                                  const activeQuestion = questions.find(q => q.id === editingId);
                                  if (activeQuestion) {
                                    const nextEnabled = activeQuestion.enabled === false ? true : false;
                                    try {
                                      const payload = {
                                        content,
                                        questionType,
                                        difficulty,
                                        durationSeconds,
                                        categoryCode,
                                        options: options.filter(x => x.trim() !== ''),
                                        correctOption: correctAnswer,
                                        citation,
                                        contentType,
                                        optionType,
                                        payload: {
                                          correctAnswer: correctAnswer,
                                          options: options.filter(x => x.trim() !== ''),
                                          enabled: nextEnabled,
                                        }
                                      };
                                      await learnerQuizService.updateQuestion(editingId, payload);
                                      setQuestions(prev => prev.map(item => item.id === editingId ? { ...item, enabled: nextEnabled } : item));
                                      alert(nextEnabled ? 'Đã kích hoạt câu hỏi!' : 'Đã vô hiệu hóa câu hỏi!');
                                    } catch (err) {
                                      alert('Không thể thay đổi trạng thái câu hỏi.');
                                    }
                                  }
                                }
                              }}
                              className={`py-2.5 border border-slate-850 rounded-xl font-bold transition-all text-[10px] flex flex-col items-center justify-center gap-1 cursor-pointer bg-slate-950 hover:bg-slate-850 ${
                                questions.find(q => q.id === editingId)?.enabled !== false
                                  ? 'text-orange-400'
                                  : 'text-emerald-450'
                              }`}
                              title="Bật/Tắt Kích hoạt"
                            >
                              {questions.find(q => q.id === editingId)?.enabled !== false ? <Lock size={14} /> : <Unlock size={14} />} 
                              {questions.find(q => q.id === editingId)?.enabled !== false ? 'Khóa' : 'Kích hoạt'}
                            </button>
                            <button
                              type="button"
                              onClick={() => {
                                if (editingId) {
                                  handleDelete(editingId);
                                  setIsModalOpen(false);
                                }
                              }}
                              className="py-2.5 bg-slate-950 hover:bg-slate-850 text-red-405 border border-slate-850 rounded-xl font-bold transition-all text-[10px] flex flex-col items-center justify-center gap-1 cursor-pointer"
                              title="Xóa câu hỏi"
                            >
                              <Trash2 size={14} /> Xóa
                            </button>
                          </div>
                          <button
                            type="button"
                            onClick={() => setIsModalOpen(false)}
                            className="w-full py-2.5 bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white border border-slate-800 rounded-xl font-bold transition-all text-xs cursor-pointer"
                          >
                            Đóng
                          </button>
                        </>
                      ) : (
                        <button
                          type="button"
                          onClick={() => setIsModalOpen(false)}
                          className="w-full py-3 bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white border border-slate-800 rounded-xl font-bold transition-all text-xs cursor-pointer"
                        >
                          Đóng
                        </button>
                      )}
                    </div>
                  ) : (
                    <div className="flex gap-3 pt-4 border-t border-slate-800/80">
                      <button
                        type="button"
                        onClick={() => {
                          if (editingId) {
                            const original = questions.find(q => q.id === editingId);
                            if (original) {
                              openEditModal(original);
                            }
                            setIsEditing(false);
                          } else {
                            setIsModalOpen(false);
                          }
                        }}
                        className="flex-1 py-3 bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white rounded-xl font-bold transition-all text-xs border border-slate-800 cursor-pointer"
                      >
                        Hủy bỏ
                      </button>
                      <button
                        type="submit"
                        className="flex-1 py-3 bg-gradient-to-r from-blue-500 to-purple-500 hover:from-blue-600 hover:to-purple-600 text-white font-bold rounded-xl transition-all shadow-md shadow-blue-500/10 active:scale-95 text-xs flex items-center justify-center gap-1.5 cursor-pointer"
                      >
                        <Save size={14} /> Lưu Thay đổi
                      </button>
                    </div>
                  )}
                </div>

              </div>
            </form>
          </div>
        </div>
      )}

      {/* ======================================================== */}
      {/* 📁 ADD / EDIT TOPIC MODAL */}
      {/* ======================================================== */}
      {isTopicModalOpen && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="w-full max-w-md bg-slate-900 border border-slate-800 rounded-3xl p-6 md:p-8 shadow-2xl relative space-y-6">
            <button 
              onClick={() => setIsTopicModalOpen(false)}
              className="absolute top-4 right-4 text-slate-400 hover:text-white p-1.5 hover:bg-slate-800 rounded-xl transition-colors cursor-pointer"
            >
              <X size={18} />
            </button>

            <div>
              <h2 className="text-xl font-black text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-purple-400">
                {isEditingTopic ? (editingTopicId ? 'Cập nhật Chủ đề' : 'Thêm Chủ đề Mới') : 'Chi tiết Chủ đề'}
              </h2>
              <p className="text-[10px] text-slate-500 font-bold uppercase tracking-widest mt-1">Danh mục ngân hàng câu hỏi</p>
            </div>

            <form onSubmit={handleSaveTopic} className="space-y-4">
              <div className="space-y-1">
                <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">Mã Chủ đề (Ví dụ: IT, TOAN, ENGLISH)</label>
                <input 
                  type="text" 
                  required
                  placeholder="Nhập mã viết hoa không dấu..." 
                  value={topicCode}
                  onChange={(e) => setTopicCode(e.target.value.toUpperCase())}
                  disabled={!isEditingTopic || !!editingTopicId}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-xs text-white focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
                />
              </div>

              <div className="space-y-1">
                <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">Tên Chủ đề (Hiển thị học viên)</label>
                <input 
                  type="text" 
                  required
                  placeholder="Ví dụ: Công nghệ thông tin, Toán cao cấp..." 
                  value={topicName}
                  onChange={(e) => setTopicName(e.target.value)}
                  disabled={!isEditingTopic}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-xs text-white focus:outline-none focus:border-blue-500 disabled:opacity-75 disabled:cursor-not-allowed"
                />
              </div>

              {editingTopicId && (
                <div className="space-y-1">
                  <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">Số lượng câu hỏi trực thuộc</label>
                  <div className="w-full bg-slate-950/60 border border-slate-800 rounded-xl px-3.5 py-2.5 text-xs text-blue-400 font-black select-none">
                    {questions.filter(q => q.topicCode === topicCode).length} câu hỏi
                  </div>
                </div>
              )}

              <div className="space-y-1">
                <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">Mô tả chủ đề</label>
                <textarea 
                  placeholder="Mô tả nội dung học và kiểm tra của chủ đề này..." 
                  value={topicDesc}
                  onChange={(e) => setTopicDesc(e.target.value)}
                  disabled={!isEditingTopic}
                  className="w-full h-20 bg-slate-950 border border-slate-800 rounded-xl px-4 py-3 text-xs text-white placeholder-slate-600 focus:outline-none focus:border-blue-500 transition-colors resize-none disabled:opacity-75 disabled:cursor-not-allowed"
                />
              </div>

              <div className="space-y-1">
                <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">Chủ đề cha (Phân cấp)</label>
                <select 
                  value={topicParentId}
                  onChange={(e) => setTopicParentId(e.target.value)}
                  disabled={!isEditingTopic}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-white focus:outline-none focus:border-blue-500 cursor-pointer disabled:opacity-75 disabled:cursor-not-allowed"
                >
                  <option value="">(Không có chủ đề cha)</option>
                  {topics
                    .filter(t => t.id !== editingTopicId)
                    .map(t => (
                      <option key={t.id} value={t.id}>{getTopicFullPath(t, topics)}</option>
                    ))
                  }
                </select>
              </div>

              <div className="space-y-1">
                <label className="text-[10px] font-black uppercase text-slate-400 tracking-wider">Phạm vi hiển thị</label>
                <select 
                  value={topicVisibility}
                  onChange={(e) => setTopicVisibility(e.target.value)}
                  disabled={!isEditingTopic}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-white focus:outline-none focus:border-blue-500 cursor-pointer disabled:opacity-75 disabled:cursor-not-allowed"
                >
                  <option value="PUBLIC">Cộng đồng (PUBLIC)</option>
                  <option value="PRIVATE">Nội bộ (PRIVATE)</option>
                </select>
              </div>

              {!isEditingTopic ? (
                <div className="space-y-3 pt-4 border-t border-slate-800/80">
                  {hasWritePermission ? (
                    <>
                      <div className="grid grid-cols-4 gap-2">
                        <button
                          type="button"
                          onClick={() => {
                            openAddTopicModal();
                            setIsEditingTopic(true);
                          }}
                          className="py-2.5 bg-slate-950 hover:bg-slate-850 text-blue-450 border border-slate-850 rounded-xl font-bold transition-all text-[10px] flex flex-col items-center justify-center gap-1 cursor-pointer"
                          title="Tạo chủ đề mới"
                        >
                          <Plus size={14} /> Mới
                        </button>
                        <button
                          type="button"
                          onClick={() => setIsEditingTopic(true)}
                          className="py-2.5 bg-slate-950 hover:bg-slate-850 text-amber-450 border border-slate-850 rounded-xl font-bold transition-all text-[10px] flex flex-col items-center justify-center gap-1 cursor-pointer"
                          title="Chỉnh sửa chủ đề"
                        >
                          <Edit size={14} /> Sửa
                        </button>
                        <button
                          type="button"
                          onClick={async () => {
                            if (editingTopicId) {
                              const activeTopic = topics.find(t => t.id === editingTopicId);
                              if (activeTopic) {
                                await handleToggleActiveTopic(activeTopic);
                              }
                            }
                          }}
                          className={`py-2.5 border border-slate-850 rounded-xl font-bold transition-all text-[10px] flex flex-col items-center justify-center gap-1 cursor-pointer bg-slate-950 hover:bg-slate-850 ${
                            topics.find(t => t.id === editingTopicId)?.enabled !== false
                              ? 'text-orange-400'
                              : 'text-emerald-450'
                          }`}
                          title="Bật/Tắt Kích hoạt"
                        >
                          {topics.find(t => t.id === editingTopicId)?.enabled !== false ? <Lock size={14} /> : <Unlock size={14} />} 
                          {topics.find(t => t.id === editingTopicId)?.enabled !== false ? 'Khóa' : 'Kích hoạt'}
                        </button>
                        <button
                          type="button"
                          onClick={() => {
                            if (editingTopicId) {
                              handleDeleteTopic(editingTopicId);
                              setIsTopicModalOpen(false);
                            }
                          }}
                          className="py-2.5 bg-slate-950 hover:bg-slate-850 text-red-405 border border-slate-850 rounded-xl font-bold transition-all text-[10px] flex flex-col items-center justify-center gap-1 cursor-pointer"
                          title="Xóa chủ đề"
                        >
                          <Trash2 size={14} /> Xóa
                        </button>
                      </div>
                      <button
                        type="button"
                        onClick={() => setIsTopicModalOpen(false)}
                        className="w-full py-2.5 bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white border border-slate-800 rounded-xl font-bold transition-all text-xs cursor-pointer"
                      >
                        Đóng
                      </button>
                    </>
                  ) : (
                    <button
                      type="button"
                      onClick={() => setIsTopicModalOpen(false)}
                      className="w-full py-3 bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white border border-slate-800 rounded-xl font-bold transition-all text-xs cursor-pointer"
                    >
                      Đóng
                    </button>
                  )}
                </div>
              ) : (
                <div className="flex gap-3 pt-4 border-t border-slate-800/80">
                  <button
                    type="button"
                    onClick={() => {
                      if (editingTopicId) {
                        const original = topics.find(t => t.id === editingTopicId);
                        if (original) {
                          openEditTopicModal(original);
                        }
                        setIsEditingTopic(false);
                      } else {
                        setIsTopicModalOpen(false);
                      }
                    }}
                    className="flex-1 py-3 bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white rounded-xl font-bold transition-all text-xs border border-slate-800 cursor-pointer"
                  >
                    Hủy bỏ
                  </button>
                  <button
                    type="submit"
                    className="flex-1 py-3 bg-gradient-to-r from-blue-500 to-purple-500 hover:from-blue-600 hover:to-purple-600 text-white font-bold rounded-xl transition-all shadow-md shadow-blue-500/10 active:scale-95 text-xs flex items-center justify-center gap-1.5 cursor-pointer"
                  >
                    <Save size={14} /> Lưu Chủ đề
                  </button>
                </div>
              )}
            </form>
          </div>
        </div>
      )}

      {/* 🔮 MODAL XEM TRƯỚC VÀ DUYỆT CÂU HỎI IMPORT DOCX (AI CHECKED) */}
      {showDocxPreviewModal && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-md z-50 flex items-center justify-center p-4">
          <div className="w-full max-w-6xl h-[90vh] bg-slate-900 border border-slate-800 rounded-3xl p-8 shadow-2xl flex flex-col justify-between relative overflow-hidden font-sans text-white">
            <button 
              onClick={() => setShowDocxPreviewModal(false)}
              className="absolute top-6 right-6 text-slate-400 hover:text-white p-1 hover:bg-slate-800 rounded-lg transition-colors cursor-pointer"
            >
              <X size={20} />
            </button>

            <div className="space-y-2 mb-6">
              <div className="inline-flex items-center gap-1.5 px-3 py-1 bg-purple-950/50 border border-purple-500/30 text-purple-300 rounded-full text-xs font-black tracking-wide uppercase">
                <Sparkles size={12} /> Cảnh giới 7: AI-Native Parser
              </div>
              <h2 className="text-2xl font-black text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-purple-400">
                Xem Trước & Duyệt Câu Hỏi (AI Checked 🛡️)
              </h2>
              <p className="text-xs text-slate-400 text-left">
                Tìm thấy {docxPreviewList.length} câu hỏi từ tệp tin DOCX. Vui lòng đối chiếu kết quả tự giải từ Gemini AI trước khi xác nhận lưu vào hệ thống.
              </p>
            </div>

            <div className="flex-1 overflow-y-auto space-y-4 pr-2">
              {docxPreviewList.map((item, idx) => (
                <div key={item.tempId} className="bg-slate-950/50 border border-slate-850 p-6 rounded-2xl space-y-4 text-left">
                  <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
                    <span className="text-xs text-blue-400 font-extrabold uppercase bg-blue-950/40 px-3 py-1 rounded-md border border-blue-900/30">
                      Câu hỏi {idx + 1} ({item.questionType === 'SINGLE' ? 'Trắc nghiệm' : 'Tự luận tình huống'})
                    </span>
                    <div className="flex items-center gap-3 w-full md:w-auto">
                      <div className="flex items-center gap-1.5 text-xs text-slate-400">
                        <span>Chủ đề:</span>
                        <select 
                          value={item.topicCode}
                          onChange={(e) => {
                            const updated = [...docxPreviewList];
                            updated[idx].topicCode = e.target.value;
                            setDocxPreviewList(updated);
                          }}
                          className="bg-slate-900 border border-slate-800 rounded-lg px-2.5 py-1 text-xs text-slate-300 focus:outline-none focus:border-blue-500 cursor-pointer"
                        >
                          {topics.map(t => (
                            <option key={t.id} value={t.code}>{t.name} ({t.code})</option>
                          ))}
                        </select>
                      </div>
                      <div className="flex items-center gap-1.5 text-xs text-slate-400">
                        <span>Độ khó:</span>
                        <select 
                          value={item.difficulty}
                          onChange={(e) => {
                            const updated = [...docxPreviewList];
                            updated[idx].difficulty = parseInt(e.target.value);
                            setDocxPreviewList(updated);
                          }}
                          className="bg-slate-900 border border-slate-800 rounded-lg px-2 py-1 text-xs text-slate-300 focus:outline-none cursor-pointer"
                        >
                          {[1, 2, 3, 4, 5].map(d => (
                            <option key={d} value={d}>{d}</option>
                          ))}
                        </select>
                      </div>
                    </div>
                  </div>

                  <div className="space-y-2">
                    <label className="text-[10px] font-black uppercase text-slate-400 tracking-wide">Nội dung câu hỏi</label>
                    <textarea 
                      value={item.content}
                      onChange={(e) => {
                        const updated = [...docxPreviewList];
                        updated[idx].content = e.target.value;
                        setDocxPreviewList(updated);
                      }}
                      className="w-full bg-slate-900 border border-slate-800 rounded-xl px-4 py-2.5 text-sm text-slate-200 placeholder-slate-600 focus:outline-none focus:border-blue-500 transition-colors"
                      rows={2}
                    />
                  </div>

                  {item.questionType === 'SINGLE' && (
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                      {item.options.map((opt: string, optIdx: number) => (
                        <div key={optIdx} className="flex items-center gap-2 bg-slate-900/60 border border-slate-850 px-4 py-2.5 rounded-xl text-xs text-slate-300">
                          <span className="font-bold text-slate-500">{LETTERS[optIdx]}.</span>
                          <input 
                            type="text"
                            value={opt}
                            onChange={(e) => {
                              const updated = [...docxPreviewList];
                              updated[idx].options[optIdx] = e.target.value;
                              setDocxPreviewList(updated);
                            }}
                            className="bg-transparent w-full text-slate-300 focus:outline-none"
                          />
                        </div>
                      ))}
                    </div>
                  )}

                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div className="md:col-span-1 space-y-2">
                      <label className="text-[10px] font-black uppercase text-slate-400 tracking-wide">Đáp án đúng (Gemini giải gợi ý)</label>
                      {item.questionType === 'SINGLE' ? (
                        <select 
                          value={item.suggestedAnswer}
                          onChange={(e) => {
                            const updated = [...docxPreviewList];
                            updated[idx].suggestedAnswer = e.target.value;
                            setDocxPreviewList(updated);
                          }}
                          className="w-full bg-slate-900 border border-slate-800 rounded-xl px-4 py-2.5 text-xs text-slate-300 focus:outline-none focus:border-blue-500 cursor-pointer"
                        >
                          <option value="">-- Chọn đáp án đúng --</option>
                          {item.options.map((_: any, oIdx: number) => (
                            <option key={oIdx} value={(oIdx + 1).toString()}>{LETTERS[oIdx]} (Lựa chọn {oIdx + 1})</option>
                          ))}
                        </select>
                      ) : (
                        <textarea 
                          value={item.suggestedAnswer}
                          onChange={(e) => {
                            const updated = [...docxPreviewList];
                            updated[idx].suggestedAnswer = e.target.value;
                            setDocxPreviewList(updated);
                          }}
                          placeholder="Mẫu bài làm / Barem chấm điểm tình huống..."
                          className="w-full bg-slate-900 border border-slate-800 rounded-xl px-4 py-2.5 text-xs text-slate-300 focus:outline-none focus:border-blue-500"
                          rows={3}
                        />
                      )}
                    </div>
                    <div className="md:col-span-2 space-y-2">
                      <label className="text-[10px] font-black uppercase text-slate-400 tracking-wide">Giải thích / Căn cứ pháp lý (AI Generated)</label>
                      <textarea 
                        value={item.aiExplanation}
                        onChange={(e) => {
                          const updated = [...docxPreviewList];
                          updated[idx].aiExplanation = e.target.value;
                          setDocxPreviewList(updated);
                        }}
                        className="w-full bg-slate-900 border border-slate-800 rounded-xl px-4 py-2.5 text-xs text-slate-300 focus:outline-none focus:border-blue-500"
                        rows={3}
                      />
                    </div>
                  </div>
                </div>
              ))}
            </div>

            <div className="flex gap-4 pt-6 border-t border-slate-800 mt-6 shrink-0">
              <button 
                onClick={() => setShowDocxPreviewModal(false)}
                className="px-6 py-3.5 bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white rounded-xl font-bold transition-all text-xs border border-slate-800 cursor-pointer"
              >
                Hủy bỏ
              </button>
              <button 
                onClick={async () => {
                  setLoading(true);
                  try {
                    const res = await learnerQuizService.confirmImportQuestionsDocx(docxPreviewList);
                    alert(`Đã lưu thành công ${res.count} câu hỏi từ bản nháp vào hệ thống!`);
                    setShowDocxPreviewModal(false);
                    fetchData();
                  } catch (err: any) {
                    console.error(err);
                    alert('Lỗi lưu câu hỏi vào hệ thống.');
                  } finally {
                    setLoading(false);
                  }
                }}
                className="flex-1 py-3.5 bg-gradient-to-r from-blue-500 to-purple-500 hover:from-blue-600 hover:to-purple-600 text-white font-bold rounded-xl transition-all shadow-lg active:scale-95 text-xs flex items-center justify-center gap-1.5 cursor-pointer"
              >
                <Save size={14} /> Xác nhận Lưu {docxPreviewList.length} câu hỏi vào Cơ sở dữ liệu
              </button>
            </div>
          </div>
        </div>
      )}

      {/* 🌀 FULLSCREEN LOADER KHI ĐANG GIẢI DOCX BẰNG AI */}
      {isDocxProcessing && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-md z-50 flex flex-col items-center justify-center space-y-4 text-white">
          <div className="animate-spin text-5xl mb-4 text-purple-400">🌀</div>
          <h3 className="text-lg font-bold text-slate-200">Đang xử lý tệp Word & AI giải câu hỏi...</h3>
          <p className="text-xs text-slate-500 max-w-sm text-center leading-relaxed">
            Google Gemini AI đang phân tích nội dung câu hỏi, đối chiếu các Quy định Đảng và Luật pháp để giải tìm đáp án gợi ý. Quá trình này có thể mất từ 15-30 giây. Vui lòng không tắt trình duyệt!
          </p>
        </div>
      )}
    </div>
  );
}
export default AdminQuestionsPage;
