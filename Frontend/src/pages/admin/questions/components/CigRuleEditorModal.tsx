import React, { useState } from 'react';
import { X, Save, ShieldAlert, Cpu } from 'lucide-react';
import { QuestionParsingRule } from '@/services/parsing-rules.service';

interface Props {
  rule: QuestionParsingRule;
  isOpen: boolean;
  onClose: () => void;
  onSave: (rule: QuestionParsingRule) => Promise<void>;
}

export const CigRuleEditorModal: React.FC<Props> = ({ rule, isOpen, onClose, onSave }) => {
  const [formData, setFormData] = useState<QuestionParsingRule>({ ...rule });
  const [activeTab, setActiveTab] = useState<'anchor' | 'stimulus' | 'interaction' | 'evaluation'>('anchor');
  const [saving, setSaving] = useState(false);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      await onSave(formData);
      onClose();
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md animate-in fade-in duration-200">
      <div className="bg-slate-900 border border-slate-700/80 rounded-3xl w-full max-w-3xl overflow-hidden shadow-2xl flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="p-6 border-b border-slate-800 bg-slate-950/60 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-purple-500/20 text-purple-400 flex items-center justify-center">
              <Cpu size={22} />
            </div>
            <div>
              <h3 className="text-base font-black text-white flex items-center gap-2">
                Biên Tập Quy Tắc CIG 4-Specs
                <span className="text-[10px] font-mono bg-purple-500/20 text-purple-300 px-2 py-0.5 rounded-full border border-purple-500/30">
                  {formData.ruleCode}
                </span>
              </h3>
              <p className="text-xs text-slate-400">Chỉnh sửa tham số nhận diện 4 chiều mà không cần compile lại hệ thống</p>
            </div>
          </div>
          <button onClick={onClose} className="p-2 text-slate-400 hover:text-white rounded-xl hover:bg-slate-800 transition-colors">
            <X size={18} />
          </button>
        </div>

        {/* Specs Tabs */}
        <div className="flex border-b border-slate-800 px-6 gap-4 bg-slate-950/30">
          {(['anchor', 'stimulus', 'interaction', 'evaluation'] as const).map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`py-3 text-xs font-black uppercase tracking-wider border-b-2 transition-all cursor-pointer ${
                activeTab === tab ? 'border-purple-500 text-purple-400' : 'border-transparent text-slate-500 hover:text-slate-300'
              }`}
            >
              {tab === 'anchor' && '1. Anchor (Điểm Neo)'}
              {tab === 'stimulus' && '2. Stimulus (Bối Cảnh)'}
              {tab === 'interaction' && '3. Interaction (Tương Tác)'}
              {tab === 'evaluation' && '4. Evaluation (Đánh Giá)'}
            </button>
          ))}
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="p-6 overflow-y-auto space-y-4 flex-1 font-mono text-xs text-slate-300">
          {/* General Info */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3 p-4 bg-slate-950/50 rounded-2xl border border-slate-800/80 font-sans">
            <div>
              <label className="text-[10px] font-bold text-slate-400 uppercase">Tên Quy Tắc</label>
              <input
                type="text"
                value={formData.ruleName}
                onChange={(e) => setFormData({ ...formData, ruleName: e.target.value })}
                className="w-full bg-slate-900 border border-slate-700 rounded-xl px-3 py-1.5 text-xs text-white mt-1"
                required
              />
            </div>
            <div>
              <label className="text-[10px] font-bold text-slate-400 uppercase">Ngôn Ngữ</label>
              <select
                value={formData.language}
                onChange={(e) => setFormData({ ...formData, language: e.target.value })}
                className="w-full bg-slate-900 border border-slate-700 rounded-xl px-3 py-1.5 text-xs text-white mt-1"
              >
                <option value="vi">Tiếng Việt (vi)</option>
                <option value="en">Tiếng Anh (en)</option>
                <option value="zh">Tiếng Trung (zh)</option>
                <option value="ko">Tiếng Hàn (ko)</option>
                <option value="ja">Tiếng Nhật (ja)</option>
                <option value="fr">Tiếng Pháp (fr)</option>
                <option value="de">Tiếng Đức (de)</option>
              </select>
            </div>
            <div>
              <label className="text-[10px] font-bold text-slate-400 uppercase">Độ Ưu Tiên (Priority)</label>
              <input
                type="number"
                value={formData.priority}
                onChange={(e) => setFormData({ ...formData, priority: parseInt(e.target.value) || 10 })}
                className="w-full bg-slate-900 border border-slate-700 rounded-xl px-3 py-1.5 text-xs text-white mt-1"
              />
            </div>
          </div>

          {/* Chiều 1: AnchorSpec */}
          {activeTab === 'anchor' && (
            <div className="space-y-4">
              <div>
                <label className="block text-[11px] font-bold text-amber-400 mb-1">Regex Nhận Diện Đầu Câu (Anchor Pattern):</label>
                <textarea
                  rows={3}
                  value={formData.anchorSpec?.regexPattern || ''}
                  onChange={(e) => setFormData({
                    ...formData,
                    anchorSpec: { ...formData.anchorSpec, regexPattern: e.target.value }
                  })}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl p-3 text-amber-300 focus:outline-none focus:border-purple-500"
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-[10px] text-slate-400 mb-1">Group Index Số Câu:</label>
                  <input
                    type="number"
                    value={formData.anchorSpec?.questionNumberGroupIndex ?? 1}
                    onChange={(e) => setFormData({
                      ...formData,
                      anchorSpec: { ...formData.anchorSpec, questionNumberGroupIndex: parseInt(e.target.value) || 1 }
                    })}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-white"
                  />
                </div>
              </div>
            </div>
          )}

          {/* Chiều 2: StimulusSpec */}
          {activeTab === 'stimulus' && (
            <div className="space-y-4">
              <div className="grid grid-cols-3 gap-3 p-3 bg-slate-950/40 rounded-xl border border-slate-800">
                <label className="flex items-center gap-2 cursor-pointer text-slate-300 text-xs">
                  <input
                    type="checkbox"
                    checked={formData.stimulusSpec?.supportMathTypeWmf ?? true}
                    onChange={(e) => setFormData({
                      ...formData,
                      stimulusSpec: { ...formData.stimulusSpec, supportMathTypeWmf: e.target.checked }
                    })}
                  />
                  Hỗ trợ MathType WMF
                </label>
                <label className="flex items-center gap-2 cursor-pointer text-slate-300 text-xs">
                  <input
                    type="checkbox"
                    checked={formData.stimulusSpec?.supportVectorSvg ?? true}
                    onChange={(e) => setFormData({
                      ...formData,
                      stimulusSpec: { ...formData.stimulusSpec, supportVectorSvg: e.target.checked }
                    })}
                  />
                  Vector WMF to SVG
                </label>
                <label className="flex items-center gap-2 cursor-pointer text-slate-300 text-xs">
                  <input
                    type="checkbox"
                    checked={formData.stimulusSpec?.extractInlineImages ?? true}
                    onChange={(e) => setFormData({
                      ...formData,
                      stimulusSpec: { ...formData.stimulusSpec, extractInlineImages: e.target.checked }
                    })}
                  />
                  Trích xuất ảnh nội dòng
                </label>
              </div>
              <div>
                <label className="block text-[11px] font-bold text-blue-400 mb-1">Regex Mở Đầu Bài Đọc Hiểu (Reading Passage):</label>
                <input
                  type="text"
                  value={formData.stimulusSpec?.readingPassageStartRegex || ''}
                  onChange={(e) => setFormData({
                    ...formData,
                    stimulusSpec: { ...formData.stimulusSpec, readingPassageStartRegex: e.target.value }
                  })}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl p-3 text-blue-300 focus:outline-none"
                />
              </div>
            </div>
          )}

          {/* Chiều 3: InteractionSpec */}
          {activeTab === 'interaction' && (
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-[10px] text-slate-400 mb-1">Loại Câu Hỏi Mặc Định:</label>
                  <select
                    value={formData.interactionSpec?.defaultQuestionType || 'SINGLE'}
                    onChange={(e) => setFormData({
                      ...formData,
                      interactionSpec: { ...formData.interactionSpec, defaultQuestionType: e.target.value }
                    })}
                    className="w-full bg-slate-950 border border-slate-800 rounded-xl px-3 py-2 text-white"
                  >
                    <option value="SINGLE">Trắc nghiệm đơn (SINGLE)</option>
                    <option value="MULTI">Nhiều lựa chọn (MULTI)</option>
                    <option value="TRUE_FALSE">Đúng / Sai (TRUE_FALSE)</option>
                    <option value="SHORT_ANSWER">Trả lời ngắn (SHORT_ANSWER)</option>
                    <option value="ESSAY">Tự luận (ESSAY)</option>
                  </select>
                </div>
              </div>
              <div>
                <label className="block text-[11px] font-bold text-emerald-400 mb-1">Regex Nhận Diện Phương Án (Options):</label>
                <textarea
                  rows={2}
                  value={formData.interactionSpec?.optionRegexPattern || ''}
                  onChange={(e) => setFormData({
                    ...formData,
                    interactionSpec: { ...formData.interactionSpec, optionRegexPattern: e.target.value }
                  })}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl p-3 text-emerald-300 focus:outline-none"
                />
              </div>
            </div>
          )}

          {/* Chiều 4: EvaluationSpec */}
          {activeTab === 'evaluation' && (
            <div className="space-y-4">
              <div>
                <label className="block text-[11px] font-bold text-teal-400 mb-1">Regex Khớp Dòng Đáp Án (Answer):</label>
                <input
                  type="text"
                  value={formData.evaluationSpec?.answerRegexPattern || ''}
                  onChange={(e) => setFormData({
                    ...formData,
                    evaluationSpec: { ...formData.evaluationSpec, answerRegexPattern: e.target.value }
                  })}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl p-3 text-teal-300 focus:outline-none"
                />
              </div>
              <div>
                <label className="block text-[11px] font-bold text-purple-400 mb-1">Regex Tiền Tố Lời Giải / Hướng Dẫn (Explanation):</label>
                <input
                  type="text"
                  value={formData.evaluationSpec?.explanationRegexPattern || ''}
                  onChange={(e) => setFormData({
                    ...formData,
                    evaluationSpec: { ...formData.evaluationSpec, explanationRegexPattern: e.target.value }
                  })}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl p-3 text-purple-300 focus:outline-none"
                />
              </div>
            </div>
          )}

          {/* Footer Actions */}
          <div className="pt-4 border-t border-slate-800 flex items-center justify-end gap-3">
            <button type="button" onClick={onClose} className="px-4 py-2 text-xs font-bold text-slate-400 hover:text-white rounded-xl">
              Hủy
            </button>
            <button
              type="submit"
              disabled={saving}
              className="flex items-center gap-2 bg-gradient-to-r from-purple-600 to-indigo-600 hover:from-purple-500 hover:to-indigo-500 text-white px-5 py-2 rounded-xl text-xs font-bold shadow-lg shadow-purple-500/20 active:scale-95 transition-all cursor-pointer"
            >
              <Save size={14} />
              {saving ? 'Đang lưu...' : 'Lưu Quy Tắc (Hot-Reload 0ms)'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
