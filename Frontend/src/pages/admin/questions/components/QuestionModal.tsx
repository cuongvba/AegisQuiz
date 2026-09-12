import { useState, useEffect } from 'react';
import { X, Plus, Trash2, Save, Edit, Lock, Unlock, BookOpen, Sparkles, Check, Users, User, ShieldCheck } from 'lucide-react';
import type { LearnerQuestion, QuestionType } from '@/types/quiz';
import { LETTERS, getCorrectAnswerPlaceholder, type BankTopic, isOptionSelectedAsCorrect } from '../types';
import { getTopicFullPath } from '../types';
import { MathRenderer } from '@/components/common/MathRenderer';
import { canManageResource, type UserProfile } from '@/types/auth';

interface QuestionModalProps {
  isOpen: boolean;
  isEditing: boolean;
  editingId: string | null;
  hasWritePermission: boolean;
  currentUser?: UserProfile | null;
  questions: LearnerQuestion[];
  topics: BankTopic[];

  // Scope & Ownership
  scope?: 'COMMUNITY' | 'TENANT' | 'TEAM' | 'PERSONAL';
  setScope?: (v: 'COMMUNITY' | 'TENANT' | 'TEAM' | 'PERSONAL') => void;

  // Form state
  content: string;
  questionType: QuestionType;
  difficulty: number;
  categoryCode: string;
  durationSeconds: number;
  options: string[];
  citation: string;
  correctAnswer: string;
  contentType: string;
  optionType: string;

  // Setters
  setContent: (v: string) => void;
  setQuestionType: (v: QuestionType) => void;
  setDifficulty: (v: number) => void;
  setCategoryCode: (v: string) => void;
  setDurationSeconds: (v: number) => void;
  setOptions: (v: string[]) => void;
  setCitation: (v: string) => void;
  setCorrectAnswer: (v: string) => void;
  setContentType: (v: string) => void;
  setOptionType: (v: string) => void;
  setIsEditing: (v: boolean) => void;

  // Universal Context props
  contexts?: any[];
  contextId?: string;
  contextTitle?: string;
  contextContent?: string;
  setContextId?: (v: string) => void;
  setContextTitle?: (v: string) => void;
  setContextContent?: (v: string) => void;
  isStickyContext?: boolean;
  setIsStickyContext?: (v: boolean) => void;
  stickyCount?: number;
  setStickyCount?: (v: React.SetStateAction<number>) => void;
  onSaveAndContinue?: (e: React.FormEvent) => void;

  // Universal Multi-Industry & Smart Tag props
  domainCode?: string;
  setDomainCode?: (v: string) => void;
  tags?: string[];
  setTags?: (v: string[]) => void;
  targetLevel?: string;
  setTargetLevel?: (v: string) => void;
  assessmentPurpose?: string;
  setAssessmentPurpose?: (v: string) => void;
  issuingOrg?: string;
  setIssuingOrg?: (v: string) => void;
  benchmarkYear?: number;
  setBenchmarkYear?: (v: number | undefined) => void;

  // Actions
  onClose: () => void;
  onSave: (e: React.FormEvent) => void;
  onOpenAdd: () => void;
  onOpenEdit: (q: LearnerQuestion) => void;
  onDelete: (id: string) => void;
  onToggleActive: (q: LearnerQuestion) => void;
}

export function QuestionModal({
  isOpen, isEditing, editingId, hasWritePermission,
  currentUser, scope = 'COMMUNITY', setScope,
  questions, topics,
  content, questionType, difficulty, categoryCode, durationSeconds,
  options, citation, correctAnswer, contentType, optionType,
  setContent, setQuestionType, setDifficulty, setCategoryCode, setDurationSeconds,
  setOptions, setCitation, setCorrectAnswer, setContentType, setOptionType, setIsEditing,
  contexts = [], contextId = '', contextTitle = '', contextContent = '',
  setContextId, setContextTitle, setContextContent,
  isStickyContext = false, setIsStickyContext,
  stickyCount = 0, setStickyCount,
  onSaveAndContinue,
  domainCode = 'EDUCATION', setDomainCode,
  tags = [], setTags,
  targetLevel = '', setTargetLevel,
  assessmentPurpose = '', setAssessmentPurpose,
  issuingOrg = '', setIssuingOrg,
  benchmarkYear = 2025, setBenchmarkYear,
  onClose, onSave, onOpenAdd, onOpenEdit, onDelete, onToggleActive,
}: QuestionModalProps) {
  const [contextMode, setContextMode] = useState<'new' | 'existing'>('new');
  const [modalTagInput, setModalTagInput] = useState('');

  // Hỗ trợ phím tắt Ctrl + Enter để Lưu & Thêm câu tiếp theo
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
        if (isEditing && !editingId && onSaveAndContinue) {
          e.preventDefault();
          onSaveAndContinue(e as any);
        }
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isEditing, editingId, onSaveAndContinue]);

  if (!isOpen) return null;

  const hasOptions = questionType === 'SINGLE' || questionType === 'MULTI' || questionType === 'ORDERING' || questionType === 'MATCHING';

  const activeQuestion = questions.find(q => q.id === editingId);
  const isManageable = editingId ? canManageResource(currentUser, activeQuestion) : hasWritePermission;

  return (
    <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div className="w-full max-w-3xl lg:max-w-6xl bg-slate-900 border border-slate-800 rounded-3xl p-6 md:p-8 shadow-2xl relative space-y-6 max-h-[95vh] md:max-h-[90vh] overflow-y-auto">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 text-slate-400 hover:text-white p-1.5 hover:bg-slate-800 rounded-xl transition-colors cursor-pointer"
        >
          <X size={18} />
        </button>

        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-800/60 pb-4">
          <div>
            <h2 className="text-xl font-black text-transparent bg-clip-text bg-gradient-to-r from-blue-400 to-purple-400">
              {isEditing ? (editingId ? 'Cập nhật Câu hỏi' : 'Tạo Câu hỏi Mới') : 'Chi tiết Câu hỏi'}
            </h2>
            <p className="text-[10px] text-slate-500 font-bold uppercase tracking-widest mt-1">Cấu hình ngân hàng đề thi đa tầng & quyền sở hữu</p>
          </div>

          {/* Attribution & Scope Badge */}
          {activeQuestion && (
            <div className="flex items-center gap-2 flex-wrap">
              <span className={`px-2.5 py-1 rounded-xl text-[11px] font-bold flex items-center gap-1.5 border ${
                activeQuestion.scope === 'TEAM'
                  ? 'bg-amber-950/60 text-amber-300 border-amber-500/40'
                  : activeQuestion.scope === 'PERSONAL'
                  ? 'bg-purple-950/60 text-purple-300 border-purple-500/40'
                  : 'bg-blue-950/60 text-blue-300 border-blue-500/40'
              }`}>
                {activeQuestion.scope === 'TEAM' ? <Users size={12} /> : activeQuestion.scope === 'PERSONAL' ? <User size={12} /> : <ShieldCheck size={12} />}
                {activeQuestion.scope === 'TEAM' ? 'Cấp Nhóm' : activeQuestion.scope === 'PERSONAL' ? 'Cá nhân' : 'Toàn cơ quan'}
              </span>

              {(activeQuestion.orgUnitName || activeQuestion.creatorName) && (
                <span className="px-2.5 py-1 rounded-xl text-[11px] font-medium bg-slate-950/80 text-slate-300 border border-slate-800 flex items-center gap-1.5">
                  {activeQuestion.orgUnitName && <span>🏢 {activeQuestion.orgUnitName}</span>}
                  {activeQuestion.creatorName && <span>✍️ {activeQuestion.creatorName}</span>}
                </span>
              )}
            </div>
          )}
        </div>

        <form onSubmit={onSave} className="space-y-6">
          <div className="grid grid-cols-1 lg:grid-cols-5 gap-6">

            {/* PANEL TRÁI: Nội dung & Phương án */}
            <div className="lg:col-span-3 space-y-4 bg-slate-950/20 p-4 rounded-2xl border border-slate-800/40">
              <h3 className="text-xs font-black uppercase text-slate-400 tracking-wider">Nội dung & Phương án</h3>

              {/* [UNIVERSAL CONTEXT] Khối cấu hình / ghim bài đọc hiểu dùng chung */}
              {isEditing && !editingId ? (
                <div className="bg-slate-950/80 border border-indigo-500/40 rounded-2xl p-4 space-y-3 shadow-inner">
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-black text-indigo-300 uppercase tracking-wider flex items-center gap-1.5">
                      <BookOpen size={14} className="text-indigo-400" /> Bài đọc hiểu / Ngữ cảnh dùng chung
                    </span>
                    {setIsStickyContext && (
                      <label className="flex items-center gap-2 cursor-pointer select-none bg-indigo-950/60 hover:bg-indigo-900/60 px-2.5 py-1 rounded-xl border border-indigo-500/30 transition-colors">
                        <input
                          type="checkbox"
                          checked={isStickyContext}
                          onChange={e => setIsStickyContext(e.target.checked)}
                          className="w-3.5 h-3.5 rounded text-indigo-600 focus:ring-indigo-500 bg-slate-900 border-slate-750 cursor-pointer"
                        />
                        <span className="text-[11px] font-bold text-amber-300">📌 Ghim cho các câu sau</span>
                      </label>
                    )}
                  </div>

                  {isStickyContext && stickyCount > 0 && (
                    <div className="px-3 py-1.5 bg-gradient-to-r from-amber-500/10 to-orange-500/10 border border-amber-500/30 text-amber-300 rounded-xl text-xs font-semibold flex items-center justify-between">
                      <span>✨ Đang ở phiên ghim: Đã lưu {stickyCount} câu cho bài đọc này</span>
                      <button
                        type="button"
                        onClick={() => {
                          if (setIsStickyContext) setIsStickyContext(false);
                          if (setStickyCount) setStickyCount(0);
                          if (setContextId) setContextId('');
                          if (setContextTitle) setContextTitle('');
                          if (setContextContent) setContextContent('');
                        }}
                        className="text-[10px] text-amber-400 underline hover:text-white font-bold cursor-pointer"
                      >
                        Bỏ ghim & Đổi bài đọc
                      </button>
                    </div>
                  )}

                  {/* Lựa chọn Viết mới / Chọn từ ngân hàng */}
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={() => setContextMode('new')}
                      className={`px-3 py-1 rounded-lg text-xs font-bold transition-all cursor-pointer ${
                        contextMode === 'new'
                          ? 'bg-indigo-600 text-white shadow-sm'
                          : 'bg-slate-900 text-slate-400 hover:text-white border border-slate-800'
                      }`}
                    >
                      + Viết bài đọc mới
                    </button>
                    {contexts.length > 0 && (
                      <button
                        type="button"
                        onClick={() => setContextMode('existing')}
                        className={`px-3 py-1 rounded-lg text-xs font-bold transition-all cursor-pointer ${
                          contextMode === 'existing'
                            ? 'bg-indigo-600 text-white shadow-sm'
                            : 'bg-slate-900 text-slate-400 hover:text-white border border-slate-800'
                        }`}
                      >
                        Chọn từ ngân hàng ({contexts.length})
                      </button>
                    )}
                  </div>

                  {contextMode === 'existing' && contexts.length > 0 ? (
                    <div className="space-y-2">
                      <select
                        value={contextId}
                        onChange={e => {
                          const sel = contexts.find(c => c.id === e.target.value);
                          if (sel) {
                            if (setContextId) setContextId(sel.id);
                            if (setContextTitle) setContextTitle(sel.title);
                            if (setContextContent) setContextContent(sel.content);
                          } else {
                            if (setContextId) setContextId('');
                            if (setContextTitle) setContextTitle('');
                            if (setContextContent) setContextContent('');
                          }
                        }}
                        className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2 text-xs text-slate-200 focus:outline-none focus:border-indigo-500 cursor-pointer"
                      >
                        <option value="">-- Chọn bài đọc hiểu dùng chung có sẵn --</option>
                        {contexts.map(c => (
                          <option key={c.id} value={c.id}>
                            {c.title} ({c.questionCount || 0} câu liên kết)
                          </option>
                        ))}
                      </select>
                      {contextContent && (
                        <div className="text-xs text-slate-300 max-h-32 overflow-y-auto p-2.5 bg-slate-950/80 rounded-xl border border-indigo-900/30 whitespace-pre-wrap leading-relaxed">
                          {contextContent}
                        </div>
                      )}
                    </div>
                  ) : (
                    <div className="space-y-2">
                      <input
                        type="text"
                        placeholder="Tiêu đề bài đọc (ví dụ: Bài đọc hiểu Tiếng Anh - Passage 1)..."
                        value={contextTitle}
                        onChange={e => setContextTitle && setContextTitle(e.target.value)}
                        className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white placeholder-slate-600 focus:outline-none focus:border-indigo-500"
                      />
                      <textarea
                        placeholder="Dán toàn văn nội dung bài đọc hiểu / tình huống dùng chung cho các câu hỏi tại đây..."
                        value={contextContent}
                        onChange={e => setContextContent && setContextContent(e.target.value)}
                        rows={3}
                        className="w-full bg-slate-900 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white placeholder-slate-600 focus:outline-none focus:border-indigo-500 resize-none"
                      />
                    </div>
                  )}
                </div>
              ) : activeQuestion?.contextContent ? (
                <div className="p-3.5 bg-indigo-950/30 border border-indigo-500/30 rounded-2xl space-y-2">
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-bold text-indigo-300 flex items-center gap-1.5">
                      📖 {activeQuestion.contextTitle || 'Bài đọc hiểu dùng chung'}
                    </span>
                    <span className="text-[10px] bg-indigo-500/20 text-indigo-300 px-2 py-0.5 rounded border border-indigo-500/30 font-semibold">
                      Ngữ cảnh / Dữ liệu dùng chung
                    </span>
                  </div>
                  <div className="text-xs text-slate-300 max-h-44 overflow-y-auto leading-relaxed whitespace-pre-wrap bg-slate-950/60 p-3 rounded-xl border border-indigo-900/40 font-sans">
                    <MathRenderer content={activeQuestion.contextContent} />
                  </div>
                </div>
              ) : null}

              <div className="space-y-1">
                <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider">Nội dung câu hỏi</label>
                <textarea
                  required
                  placeholder="Điền nội dung câu hỏi sát hạch (hỗ trợ KaTeX $...$, hình ảnh Base64, ngữ âm <u>...</u>)..."
                  value={content}
                  onChange={e => setContent(e.target.value)}
                  disabled={!isEditing}
                  className="w-full h-24 bg-slate-950/80 border border-slate-800 rounded-xl px-4 py-3 text-sm text-white placeholder-slate-600 focus:outline-none focus:border-blue-500 transition-colors resize-none disabled:opacity-75 disabled:cursor-not-allowed"
                />
                {(content.includes('$') || content.includes('data:image/') || content.includes('<u>') || content.includes('**')) && (
                  <div className="p-3 bg-slate-950/80 border border-slate-800 rounded-xl text-xs text-slate-300 space-y-1">
                    <span className="text-[10px] font-bold text-sky-400 uppercase tracking-wider block">Trực quan hóa Nội dung (KaTeX / Ảnh / Ngữ âm):</span>
                    <MathRenderer content={content} />
                  </div>
                )}
              </div>

              {hasOptions ? (
                <div className="space-y-2.5">
                  <div>
                    <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider block">Các phương án lựa chọn</label>
                    {questionType === 'MATCHING' && <p className="text-[9px] text-cyan-400 font-bold mt-0.5">Định dạng nối cặp: Vế trái =&gt; Vế phải</p>}
                    {questionType === 'ORDERING' && <p className="text-[9px] text-cyan-400 font-bold mt-0.5">Nhập các phần tử cần sắp xếp thứ tự.</p>}
                  </div>

                  <div className="space-y-2">
                    {options.map((opt, idx) => {
                      const isCorrect = isOptionSelectedAsCorrect(opt, idx, correctAnswer, questionType, options);

                      return (
                        <div
                          key={idx}
                          className={`flex items-start gap-2 p-2 rounded-2xl border transition-all ${
                            isCorrect
                              ? 'bg-emerald-950/20 border-emerald-500/50 shadow-sm shadow-emerald-950/40 ring-1 ring-emerald-500/20'
                              : 'border-transparent'
                          }`}
                        >
                          <div className="flex flex-col items-center gap-1 shrink-0 mt-1">
                            <span className={`w-12 text-center text-[10px] font-black py-2 rounded-xl border select-none transition-colors ${
                              isCorrect
                                ? 'bg-emerald-500 text-slate-950 border-emerald-400 font-extrabold shadow-sm'
                                : 'bg-slate-950 border-slate-800 text-slate-400'
                            }`}>
                              {LETTERS[idx] ?? String(idx + 1)} ({idx + 1})
                            </span>
                            {(questionType === 'SINGLE' || questionType === 'MULTI') && (
                              <label className={`flex items-center gap-1 cursor-pointer px-2 py-0.5 rounded-lg border transition-all select-none ${
                                isCorrect
                                  ? 'bg-emerald-500/20 border-emerald-500 text-emerald-300 font-bold shadow-sm shadow-emerald-500/20'
                                  : 'bg-slate-950/40 border-slate-850 text-slate-500 hover:border-slate-800'
                              } ${!isEditing ? 'pointer-events-none' : ''}`}>
                                <input
                                  type={questionType === 'SINGLE' ? 'radio' : 'checkbox'}
                                  name="correct-answer-toggle"
                                  checked={isCorrect}
                                  disabled={!isEditing}
                                  onChange={() => {
                                    if (questionType === 'SINGLE') {
                                      setCorrectAnswer(String(idx + 1));
                                    } else {
                                      const current = (correctAnswer || '').split(/[;,]+/).map(x => x.trim()).filter(Boolean);
                                      const val = String(idx + 1);
                                      const next = current.includes(val)
                                        ? current.filter(x => x !== val)
                                        : [...current, val];
                                      setCorrectAnswer(next.sort((a, b) => Number(a) - Number(b)).join(','));
                                    }
                                  }}
                                  className="w-3.5 h-3.5 text-emerald-600 bg-slate-950 border-slate-800 rounded focus:ring-emerald-500 cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
                                />
                                <span className={`text-[9px] ${isCorrect ? 'text-emerald-300 font-extrabold' : 'text-slate-400'}`}>Đúng</span>
                              </label>
                            )}
                          </div>
                          <div className="flex-1 flex flex-col gap-1">
                            <textarea
                              placeholder={questionType === 'MATCHING' ? `Cặp ${idx + 1} (Ví dụ: Chó => Sủa)` : `Phương án ${LETTERS[idx] ?? String(idx + 1)}`}
                              value={opt}
                              onChange={e => {
                                const next = [...options];
                                next[idx] = e.target.value;
                                setOptions(next);
                              }}
                              disabled={!isEditing}
                              className={`w-full border rounded-xl px-3.5 py-2 text-xs text-white focus:outline-none resize-none h-14 disabled:opacity-90 disabled:cursor-not-allowed transition-colors ${
                                isCorrect
                                  ? 'bg-emerald-950/30 border-emerald-500/60 focus:border-emerald-400'
                                  : 'bg-slate-950/60 border-slate-800 focus:border-blue-500'
                              }`}
                            />
                            {(opt.includes('$') || opt.includes('data:image/') || opt.includes('<u>') || opt.includes('**')) && (
                              <div className="p-2 bg-slate-950/90 border border-slate-850 rounded-xl text-xs text-slate-300">
                                <MathRenderer content={opt} />
                              </div>
                            )}
                          </div>
                          {isEditing && options.length > 2 && (
                            <button
                              type="button"
                              onClick={() => {
                                const next = options.filter((_, i) => i !== idx);
                                setOptions(next);
                                if (questionType === 'SINGLE' || questionType === 'MULTI') {
                                  const current = correctAnswer.split(/[;,]+/).map(x => x.trim()).filter(Boolean);
                                  const adjusted = current
                                    .map(val => {
                                      const num = Number(val);
                                      if (num === idx + 1) return null;
                                      if (num > idx + 1) return String(num - 1);
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

            {/* PANEL PHẢI: Cấu hình & Đáp án */}
            <div className="lg:col-span-2 space-y-4 bg-slate-950/20 p-4 rounded-2xl border border-slate-800/40 flex flex-col justify-between">
              <div className="space-y-4">
                <h3 className="text-xs font-black uppercase text-slate-400 tracking-wider">Cấu hình & Đáp án</h3>

                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-1">
                    <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider">Loại câu hỏi</label>
                    <select
                      value={questionType}
                      onChange={e => setQuestionType(e.target.value as QuestionType)}
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

                  <div className="space-y-1.5">
                    <label className="text-[10px] font-bold text-slate-400 uppercase tracking-wider block">
                      Đáp án chính xác
                    </label>
                    {questionType === 'TRUE_FALSE' ? (
                      <div className="flex gap-2 pt-0.5">
                        <button
                          type="button" onClick={() => setCorrectAnswer('1')} disabled={!isEditing}
                          className={`flex-1 py-2 px-3 rounded-xl border text-xs font-black transition-all cursor-pointer disabled:opacity-75 disabled:cursor-not-allowed ${correctAnswer === '1' ? 'bg-emerald-950/40 border-emerald-500 text-emerald-400 shadow-sm shadow-emerald-500/20' : 'bg-slate-950 border-slate-800 text-slate-500 hover:border-slate-700'}`}
                        >Đúng (1)</button>
                        <button
                          type="button" onClick={() => setCorrectAnswer('2')} disabled={!isEditing}
                          className={`flex-1 py-2 px-3 rounded-xl border text-xs font-black transition-all cursor-pointer disabled:opacity-75 disabled:cursor-not-allowed ${correctAnswer === '2' ? 'bg-rose-950/40 border-rose-500 text-rose-400 shadow-sm shadow-rose-500/20' : 'bg-slate-950 border-slate-800 text-slate-500 hover:border-slate-700'}`}
                        >Sai (2)</button>
                      </div>
                    ) : (questionType === 'SINGLE' || questionType === 'MULTI') ? (
                      <div className="space-y-2">
                        {(() => {
                          const matchedIndices: number[] = [];
                          options.forEach((opt, i) => {
                            if (isOptionSelectedAsCorrect(opt, i, correctAnswer, questionType, options)) {
                              matchedIndices.push(i);
                            }
                          });

                          if (matchedIndices.length > 0) {
                            return (
                              <div className="p-2.5 bg-emerald-950/40 border border-emerald-500/60 rounded-xl space-y-1.5 shadow-sm shadow-emerald-950/60">
                                <div className="flex items-center justify-between">
                                  <span className="text-[10px] font-bold uppercase text-emerald-400 tracking-wider flex items-center gap-1">
                                    <Check size={12} className="text-emerald-300" />
                                    {questionType === 'SINGLE' ? 'Phương án đúng' : `Phương án đúng (${matchedIndices.length})`}
                                  </span>
                                  <span className="text-[10px] font-mono text-emerald-300 font-bold">
                                    {matchedIndices.map(i => `${LETTERS[i]} (${i + 1})`).join(', ')}
                                  </span>
                                </div>
                                <div className="space-y-1 max-h-24 overflow-y-auto">
                                  {matchedIndices.map(i => (
                                    <div key={i} className="text-xs text-emerald-200 flex items-start gap-1.5 bg-emerald-900/30 px-2 py-1 rounded-lg border border-emerald-500/20">
                                      <span className="font-black text-emerald-400 shrink-0">{LETTERS[i]} ({i + 1}):</span>
                                      <span className="truncate">{options[i] || `(Phương án ${LETTERS[i]})`}</span>
                                    </div>
                                  ))}
                                </div>
                              </div>
                            );
                          }

                          return (
                            <div className="p-2.5 bg-amber-950/40 border border-amber-500/50 rounded-xl text-xs text-amber-300 flex items-start gap-2">
                              <span className="text-sm shrink-0">⚠️</span>
                              <div className="space-y-0.5 min-w-0">
                                <p className="font-bold">Chưa xác định phương án đúng</p>
                                <p className="text-[10px] text-amber-400/90 truncate">
                                  {correctAnswer ? `Giá trị: "${correctAnswer}"` : 'Vui lòng chọn nút "Đúng" ở danh sách phương án.'}
                                </p>
                              </div>
                            </div>
                          );
                        })()}

                        {/* Quick selector dropdown when editing */}
                        {isEditing && questionType === 'SINGLE' && (
                          <div className="space-y-1">
                            <label className="text-[9px] text-slate-400 font-semibold uppercase">Chọn nhanh đáp án đúng</label>
                            <select
                              value={(() => {
                                const idx = options.findIndex((opt, i) => isOptionSelectedAsCorrect(opt, i, correctAnswer, 'SINGLE', options));
                                return idx !== -1 ? String(idx + 1) : '';
                              })()}
                              onChange={e => setCorrectAnswer(e.target.value)}
                              className="w-full bg-slate-950 border border-emerald-500/40 rounded-xl px-3 py-2 text-xs text-emerald-300 focus:outline-none focus:border-emerald-400 cursor-pointer"
                            >
                              <option value="">-- Chọn phương án đúng --</option>
                              {options.map((opt, i) => (
                                <option key={i} value={String(i + 1)}>
                                  Phương án {LETTERS[i]} ({i + 1}): {opt ? (opt.length > 35 ? opt.slice(0, 35) + '...' : opt) : `(Phương án ${LETTERS[i]})`}
                                </option>
                              ))}
                            </select>
                          </div>
                        )}
                      </div>
                    ) : (
                      <input
                        type="text"
                        required={questionType !== 'ESSAY'}
                        placeholder={getCorrectAnswerPlaceholder(questionType)}
                        value={correctAnswer}
                        onChange={e => setCorrectAnswer(e.target.value)}
                        disabled={!isEditing}
                        className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2.5 text-xs text-white focus:outline-none focus:border-blue-500 disabled:opacity-75 disabled:cursor-not-allowed"
                      />
                    )}
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-1">
                    <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider">Định dạng Nội dung</label>
                    <select value={contentType} onChange={e => setContentType(e.target.value)} disabled={!isEditing} className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-white focus:outline-none focus:border-blue-500 cursor-pointer disabled:opacity-75 disabled:cursor-not-allowed">
                      <option value="text">Văn bản thường</option>
                      <option value="image">Hình ảnh (URL)</option>
                      <option value="audio">Âm thanh (URL)</option>
                      <option value="video">Phim / Video (URL)</option>
                    </select>
                  </div>
                  <div className="space-y-1">
                    <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider">Định dạng Phương án</label>
                    <select value={optionType} onChange={e => setOptionType(e.target.value)} disabled={!isEditing || !hasOptions} className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-white focus:outline-none focus:border-blue-500 cursor-pointer disabled:opacity-40 disabled:cursor-not-allowed">
                      <option value="text">Văn bản thường</option>
                      <option value="image">Hình ảnh (URL)</option>
                      <option value="audio">Âm thanh (URL)</option>
                      <option value="video">Phim / Video (URL)</option>
                    </select>
                  </div>
                </div>

                <div className="space-y-1">
                  <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider">Chủ đề phân nhóm</label>
                  {topics.length > 0 ? (
                    <select value={categoryCode} onChange={e => setCategoryCode(e.target.value)} disabled={!isEditing} className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2.5 text-xs text-white focus:outline-none focus:border-blue-500 cursor-pointer disabled:opacity-75 disabled:cursor-not-allowed">
                      {topics.map(t => (
                        <option key={t.code} value={t.code}>{getTopicFullPath(t, topics)} ({t.code})</option>
                      ))}
                    </select>
                  ) : (
                    <div className="text-xs text-amber-500 p-2 bg-amber-500/10 rounded-xl border border-amber-500/20 font-bold">Cần tạo Chủ đề trước!</div>
                  )}
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-1">
                    <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider">Độ khó (1-5 Sao)</label>
                    <input type="number" min={1} max={5} required value={difficulty} onChange={e => setDifficulty(Number(e.target.value))} disabled={!isEditing} className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500 disabled:opacity-75 disabled:cursor-not-allowed" />
                  </div>
                  <div className="space-y-1">
                    <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider">Thời gian (Giây)</label>
                    <input type="number" min={10} required value={durationSeconds} onChange={e => setDurationSeconds(Number(e.target.value))} disabled={!isEditing} className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-blue-500 disabled:opacity-75 disabled:cursor-not-allowed" />
                  </div>
                </div>

                <div className="space-y-1">
                  <label className="text-[10px] font-bold text-slate-500 uppercase tracking-wider block">Trích dẫn nguồn câu hỏi</label>
                  <textarea
                    placeholder="Ví dụ: Điều 12 Luật Doanh nghiệp hoặc TT 39/2016/TT-NHNN..."
                    value={citation}
                    onChange={e => setCitation(e.target.value)}
                    rows={2}
                    disabled={!isEditing}
                    className="w-full bg-slate-950/60 border border-slate-800 rounded-xl px-3.5 py-2 text-xs text-white focus:outline-none focus:border-blue-500 resize-none h-14 disabled:opacity-75 disabled:cursor-not-allowed"
                  />
                  {(citation.includes('$') || citation.includes('data:image/') || citation.includes('<u>') || citation.includes('**')) && (
                    <div className="p-2.5 bg-slate-950/80 border border-slate-800 rounded-xl text-xs text-slate-300">
                      <span className="text-[10px] font-bold text-sky-400 uppercase tracking-wider block mb-1">Trực quan hóa Trích dẫn:</span>
                      <MathRenderer content={citation} />
                    </div>
                  )}
                </div>

                {/* 👥 PHẠM VI SỞ HỮU & PHÂN QUYỀN (ENTERPRISE MULTI-TIER) */}
                <div className="space-y-1.5 p-3 bg-slate-950/40 rounded-2xl border border-slate-800">
                  <div className="flex items-center justify-between">
                    <label className="text-[10px] font-bold text-amber-400 uppercase tracking-wider flex items-center gap-1.5">
                      <Users size={12} className="text-amber-400" />
                      Phạm vi sở hữu & Đóng góp
                    </label>
                    <span className="text-[10px] text-slate-500 font-mono">
                      {currentUser?.role === 'TeamLeader' ? 'Cấp: Team Leader' : 'Cấp: Quản trị viên'}
                    </span>
                  </div>
                  <select
                    value={scope}
                    onChange={e => setScope?.(e.target.value as any)}
                    disabled={!isEditing}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-amber-500 cursor-pointer disabled:opacity-75"
                  >
                    {currentUser?.role === 'TeamLeader' ? (
                      <>
                        <option value="TEAM">👥 Nhóm nội bộ ({currentUser?.orgUnitName || 'Đơn vị của tôi'})</option>
                        <option value="PERSONAL">👤 Cá nhân (Chỉ tôi nhìn thấy)</option>
                      </>
                    ) : (
                      <>
                        <option value="COMMUNITY">🌐 Toàn cơ quan / Hệ thống (Toàn thể cán bộ)</option>
                        <option value="TEAM">👥 Cấp Nhóm / Đơn vị nội bộ</option>
                        <option value="PERSONAL">👤 Cá nhân riêng tư</option>
                      </>
                    )}
                  </select>
                  <p className="text-[10px] text-slate-500 italic">
                    {scope === 'TEAM'
                      ? 'Thành viên cùng nhóm có thể luyện tập; TeamLeader toàn quyền quản lý.'
                      : scope === 'COMMUNITY'
                      ? 'Câu hỏi công khai cho toàn thể cán bộ trong cơ quan/hệ thống.'
                      : 'Chỉ riêng bạn có quyền xem và sử dụng câu hỏi này.'}
                  </p>
                </div>

                {/* 🌐 PHÂN HỆ ĐA NGÀNH & SMART TAGS */}
                <div className="p-3.5 bg-slate-950/40 border border-slate-800/80 rounded-2xl space-y-3">
                  <div className="flex items-center justify-between">
                    <label className="text-[10px] font-bold text-indigo-400 uppercase tracking-wider flex items-center gap-1.5">
                      <Sparkles size={12} className="text-amber-400" />
                      Phân Loại Đa Ngành & Thẻ Thông Minh
                    </label>
                  </div>

                  {/* Lĩnh vực / Domain */}
                  <div className="space-y-1">
                    <label className="text-[9px] font-semibold text-slate-400 uppercase">Miền ngành nghề</label>
                    <select
                      value={domainCode}
                      onChange={e => setDomainCode?.(e.target.value)}
                      disabled={!isEditing}
                      className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-xs text-white focus:outline-none focus:border-indigo-500 cursor-pointer disabled:opacity-75"
                    >
                      <option value="EDUCATION">🎓 Giáo dục & Ngoại ngữ</option>
                      <option value="BANKING">🏦 Ngân hàng & Tài chính</option>
                      <option value="HEALTHCARE">🏥 Y tế & Dược phẩm</option>
                      <option value="HSE">🛡️ An toàn lao động HSE</option>
                      <option value="GOV_DRIVING">🚗 Sát hạch Giao thông</option>
                      <option value="IT_SECURITY">💻 Công nghệ & An ninh mạng</option>
                      <option value="GENERAL">🌐 Chung / Đa lĩnh vực</option>
                    </select>
                  </div>

                  {/* 4 trục tọa độ */}
                  <div className="grid grid-cols-2 gap-2">
                    <div>
                      <input
                        type="text"
                        placeholder="Khối/Cấp bậc (vd: Lớp 12, RM)"
                        value={targetLevel}
                        onChange={e => setTargetLevel?.(e.target.value)}
                        disabled={!isEditing}
                        className="w-full bg-slate-950 border border-slate-800 rounded-lg px-2.5 py-1.5 text-[11px] text-white placeholder-slate-600 focus:outline-none focus:border-indigo-500 disabled:opacity-75"
                      />
                    </div>
                    <div>
                      <input
                        type="text"
                        placeholder="Kỳ thi (vd: Tốt nghiệp, AML)"
                        value={assessmentPurpose}
                        onChange={e => setAssessmentPurpose?.(e.target.value)}
                        disabled={!isEditing}
                        className="w-full bg-slate-950 border border-slate-800 rounded-lg px-2.5 py-1.5 text-[11px] text-white placeholder-slate-600 focus:outline-none focus:border-indigo-500 disabled:opacity-75"
                      />
                    </div>
                    <div>
                      <input
                        type="text"
                        placeholder="Trường/Đơn vị (vd: Ams, VCB)"
                        value={issuingOrg}
                        onChange={e => setIssuingOrg?.(e.target.value)}
                        disabled={!isEditing}
                        className="w-full bg-slate-950 border border-slate-800 rounded-lg px-2.5 py-1.5 text-[11px] text-white placeholder-slate-600 focus:outline-none focus:border-indigo-500 disabled:opacity-75"
                      />
                    </div>
                    <div>
                      <input
                        type="number"
                        placeholder="Năm (vd: 2025)"
                        value={benchmarkYear ?? ''}
                        onChange={e => setBenchmarkYear?.(e.target.value ? Number(e.target.value) : undefined)}
                        disabled={!isEditing}
                        className="w-full bg-slate-950 border border-slate-800 rounded-lg px-2.5 py-1.5 text-[11px] text-white placeholder-slate-600 focus:outline-none focus:border-indigo-500 disabled:opacity-75"
                      />
                    </div>
                  </div>

                  {/* Smart Tag Chips */}
                  <div className="space-y-1.5">
                    <label className="text-[9px] font-semibold text-slate-400 uppercase flex items-center justify-between">
                      <span>Thẻ #Hashtag</span>
                      <span className="text-[9px] text-slate-500 font-normal">Gõ tag rồi Enter hoặc phẩy</span>
                    </label>
                    {isEditing && (
                      <div className="flex gap-1.5">
                        <input
                          type="text"
                          placeholder="Thêm #tag..."
                          value={modalTagInput}
                          onChange={e => setModalTagInput(e.target.value)}
                          onKeyDown={e => {
                            if (e.key === 'Enter' || e.key === ',') {
                              e.preventDefault();
                              let clean = modalTagInput.trim();
                              if (!clean) return;
                              if (!clean.startsWith('#')) clean = `#${clean}`;
                              if (tags && !tags.includes(clean)) {
                                setTags?.([...tags, clean]);
                              }
                              setModalTagInput('');
                            }
                          }}
                          className="flex-1 bg-slate-950 border border-slate-800 rounded-lg px-2.5 py-1.5 text-xs text-white placeholder-slate-600 focus:outline-none focus:border-indigo-500"
                        />
                        <button
                          type="button"
                          onClick={() => {
                            let clean = modalTagInput.trim();
                            if (!clean) return;
                            if (!clean.startsWith('#')) clean = `#${clean}`;
                            if (tags && !tags.includes(clean)) {
                              setTags?.([...tags, clean]);
                            }
                            setModalTagInput('');
                          }}
                          className="px-3 py-1.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg text-xs font-semibold"
                        >
                          +
                        </button>
                      </div>
                    )}

                    {/* Chips hiển thị */}
                    <div className="flex flex-wrap gap-1.5 min-h-[28px] p-2 bg-slate-950/60 rounded-xl border border-slate-850">
                      {(!tags || tags.length === 0) ? (
                        <span className="text-[10px] text-slate-600 italic">Chưa có hashtag</span>
                      ) : (
                        tags.map(t => (
                          <span
                            key={t}
                            className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-semibold bg-indigo-950/60 border border-indigo-700/50 text-indigo-300"
                          >
                            {t}
                            {isEditing && (
                              <button
                                type="button"
                                onClick={() => setTags?.(tags.filter(x => x !== t))}
                                className="hover:text-red-400 transition-colors"
                              >
                                &times;
                              </button>
                            )}
                          </span>
                        ))
                      )}
                    </div>
                  </div>
                </div>
              </div>

              {/* Action Buttons */}
              {!isEditing ? (
                <div className="space-y-3 pt-4 border-t border-slate-800/80">
                  {hasWritePermission ? (
                    <>
                      <div className="grid grid-cols-4 gap-2">
                        <button type="button" onClick={() => { onOpenAdd(); setIsEditing(true); }} className="py-2.5 bg-slate-950 hover:bg-slate-850 text-blue-450 border border-slate-850 rounded-xl font-bold transition-all text-[10px] flex flex-col items-center justify-center gap-1 cursor-pointer" title="Tạo câu hỏi mới"><Plus size={14} /> Mới</button>
                        <button
                          type="button"
                          onClick={() => {
                            if (!isManageable) {
                              alert('Bạn chỉ có quyền chỉnh sửa câu hỏi do chính bạn hoặc nhóm bạn tạo ra.');
                              return;
                            }
                            setIsEditing(true);
                          }}
                          disabled={!isManageable}
                          className={`py-2.5 border rounded-xl font-bold transition-all text-[10px] flex flex-col items-center justify-center gap-1 ${
                            isManageable
                              ? 'bg-slate-950 hover:bg-slate-850 text-amber-450 border-slate-850 cursor-pointer'
                              : 'bg-slate-950/40 text-slate-600 border-slate-800/40 cursor-not-allowed opacity-50'
                          }`}
                          title={isManageable ? "Chỉnh sửa" : "Bạn không sở hữu câu hỏi này"}
                        >
                          <Edit size={14} /> Sửa
                        </button>
                        <button
                          type="button"
                          disabled={!isManageable}
                          onClick={async () => {
                            if (!isManageable) {
                              alert('Bạn chỉ có quyền khóa/mở câu hỏi do chính bạn hoặc nhóm bạn tạo ra.');
                              return;
                            }
                            if (editingId && activeQuestion) await onToggleActive(activeQuestion);
                          }}
                          className={`py-2.5 border rounded-xl font-bold transition-all text-[10px] flex flex-col items-center justify-center gap-1 ${
                            isManageable
                              ? `bg-slate-950 hover:bg-slate-850 cursor-pointer border-slate-850 ${activeQuestion?.enabled !== false ? 'text-orange-400' : 'text-emerald-450'}`
                              : 'bg-slate-950/40 text-slate-600 border-slate-800/40 cursor-not-allowed opacity-50'
                          }`}
                          title={isManageable ? "Bật/Tắt Kích hoạt" : "Bạn không sở hữu câu hỏi này"}
                        >
                          {activeQuestion?.enabled !== false ? <Lock size={14} /> : <Unlock size={14} />}
                          {activeQuestion?.enabled !== false ? 'Khóa' : 'Kích hoạt'}
                        </button>
                        <button
                          type="button"
                          disabled={!isManageable}
                          onClick={() => {
                            if (!isManageable) {
                              alert('Bạn chỉ có quyền xóa câu hỏi do chính bạn hoặc nhóm bạn tạo ra.');
                              return;
                            }
                            if (editingId) { onDelete(editingId); onClose(); }
                          }}
                          className={`py-2.5 border rounded-xl font-bold transition-all text-[10px] flex flex-col items-center justify-center gap-1 ${
                            isManageable
                              ? 'bg-slate-950 hover:bg-slate-850 text-red-405 border-slate-850 cursor-pointer'
                              : 'bg-slate-950/40 text-slate-600 border-slate-800/40 cursor-not-allowed opacity-50'
                          }`}
                          title={isManageable ? "Xóa câu hỏi" : "Bạn không sở hữu câu hỏi này"}
                        >
                          <Trash2 size={14} /> Xóa
                        </button>
                      </div>
                      <button type="button" onClick={onClose} className="w-full py-2.5 bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white border border-slate-800 rounded-xl font-bold transition-all text-xs cursor-pointer">Đóng</button>
                    </>
                  ) : (
                    <button type="button" onClick={onClose} className="w-full py-3 bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white border border-slate-800 rounded-xl font-bold transition-all text-xs cursor-pointer">Đóng</button>
                  )}
                </div>
              ) : (
                <div className="flex flex-col sm:flex-row gap-2.5 pt-4 border-t border-slate-800/80">
                  <button
                    type="button"
                    onClick={() => {
                      if (editingId) {
                        const original = questions.find(q => q.id === editingId);
                        if (original) onOpenEdit(original);
                        setIsEditing(false);
                      } else {
                        onClose();
                      }
                    }}
                    className="py-3 px-4 bg-slate-950 hover:bg-slate-850 text-slate-400 hover:text-white rounded-xl font-bold transition-all text-xs border border-slate-800 cursor-pointer"
                  >Hủy bỏ</button>

                  {!editingId && onSaveAndContinue && (
                    <button
                      type="button"
                      onClick={onSaveAndContinue}
                      className="flex-1 py-3 px-4 bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white font-bold rounded-xl transition-all shadow-md shadow-emerald-500/20 active:scale-95 text-xs flex items-center justify-center gap-1.5 cursor-pointer border border-emerald-400/30"
                      title="Lưu câu hiện tại và giữ nguyên bài đọc để nhập tiếp câu sau"
                    >
                      <Sparkles size={14} className="text-amber-300 animate-pulse" />
                      <span>💾 Lưu & Thêm câu tiếp (Ctrl+Enter)</span>
                    </button>
                  )}

                  <button
                    type="submit"
                    className="flex-1 py-3 px-4 bg-gradient-to-r from-blue-500 to-purple-500 hover:from-blue-600 hover:to-purple-600 text-white font-bold rounded-xl transition-all shadow-md shadow-blue-500/10 active:scale-95 text-xs flex items-center justify-center gap-1.5 cursor-pointer"
                  >
                    <Save size={14} /> {!editingId ? 'Lưu & Hoàn tất' : 'Lưu Thay đổi'}
                  </button>
                </div>
              )}
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}
