import { Eye, Trash2, Compass, Hash } from 'lucide-react';
import type { BankTopic } from '../../types';
import { LETTERS } from '../../types';
import { MathRenderer } from '../../../../../components/common/MathRenderer';
import { 
  parseMultiTfAnswer, 
  serializeMultiTfAnswer, 
  serializeMultiTfVector, 
  convertMultiToMultiTf, 
  convertMultiTfToMulti 
} from '../../../../../shared/lib/quiz/normalizers';

interface DocxQuestionCardRowProps {
  item: any;
  idx: number;
  activeDomains: any[];
  localTopics: BankTopic[];
  expandedCoords: Record<number, boolean>;
  showRawOptions: Record<number, boolean>;
  tagInputPerQ: Record<number, string>;
  onUpdateItem: (idx: number, field: string, value: any) => void;
  onDeleteItem: (idx: number) => void;
  onToggleExpandCoords: (idx: number) => void;
  onToggleShowRawOptions: (idx: number) => void;
  onAddTagToQuestion: (idx: number, tag: string) => void;
  onRemoveTagFromQuestion: (idx: number, tag: string) => void;
  onSetTagInputPerQ: (idx: number, val: string) => void;
  onOpenCreateTopicModal: (qIdx: number | null) => void;
  onOptionChange: (itemIdx: number, optIdx: number, val: string) => void;
}

export function DocxQuestionCardRow({
  item,
  idx,
  activeDomains,
  localTopics,
  expandedCoords,
  showRawOptions,
  tagInputPerQ,
  onUpdateItem,
  onDeleteItem,
  onToggleExpandCoords,
  onToggleShowRawOptions,
  onAddTagToQuestion,
  onRemoveTagFromQuestion,
  onSetTagInputPerQ,
  onOpenCreateTopicModal,
  onOptionChange,
}: DocxQuestionCardRowProps) {
  return (
    <div className="bg-slate-950/40 border border-slate-800/60 rounded-2xl p-4 space-y-2.5 hover:border-slate-700/80 transition-all">
      {/* Question Card Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-slate-800/70 pb-2">
        <div className="flex items-center gap-1.5 flex-wrap">
          <span className="w-6 h-6 rounded-md bg-blue-600/20 border border-blue-500/30 text-blue-400 font-black flex items-center justify-center text-[11px] shrink-0">
            {idx + 1}
          </span>
          <select
            value={item.questionType}
            onChange={e => {
              const newType = e.target.value;
              const oldType = item.questionType;
              let nextAnswer = item.suggestedAnswer;

              if (newType === 'MULTI_TRUE_FALSE' && oldType !== 'MULTI_TRUE_FALSE') {
                nextAnswer = convertMultiToMultiTf(item.suggestedAnswer, item.options?.length || 4);
              } else if (oldType === 'MULTI_TRUE_FALSE' && (newType === 'MULTI' || newType === 'SINGLE')) {
                nextAnswer = convertMultiTfToMulti(item.suggestedAnswer);
              }

              onUpdateItem(idx, 'questionType', newType);
              onUpdateItem(idx, 'suggestedAnswer', nextAnswer);
            }}
            className={`text-[11px] font-black px-2 py-0.5 rounded-lg border cursor-pointer ${
              item.questionType === 'ESSAY'
                ? 'bg-amber-950/60 text-amber-300 border-amber-500/50'
                : item.questionType === 'MULTI' || item.questionType === 'MULTI_TRUE_FALSE'
                ? 'bg-purple-950/60 text-purple-300 border-purple-500/50'
                : item.questionType === 'TRUE_FALSE'
                ? 'bg-emerald-950/60 text-emerald-300 border-emerald-500/50'
                : 'bg-blue-950/60 text-blue-300 border-blue-500/50'
            }`}
          >
            <option value="SINGLE">📝 Trắc nghiệm</option>
            <option value="MULTI">☑️ Nhiều đáp án</option>
            <option value="TRUE_FALSE">⚖️ Đúng / Sai</option>
            <option value="MULTI_TRUE_FALSE">🎯 Đúng / Sai 4 ý (GDPT 2025)</option>
            <option value="SHORT_ANSWER">🔢 Trả lời ngắn</option>
            <option value="ESSAY">✍️ Tự luận</option>
          </select>

          {/* Miền Ngành Selector */}
          <select
            value={item.domainCode || 'GENERAL'}
            onChange={e => onUpdateItem(idx, 'domainCode', e.target.value)}
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
                onOpenCreateTopicModal(idx);
              } else {
                onUpdateItem(idx, 'topicCode', e.target.value);
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
            onChange={e => onUpdateItem(idx, 'difficulty', parseInt(e.target.value))}
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
            onClick={() => onToggleExpandCoords(idx)}
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
            onClick={() => onDeleteItem(idx)}
            className="p-1 rounded-lg text-slate-500 hover:text-red-400 hover:bg-red-950/30 transition-colors cursor-pointer"
            title="Xóa câu hỏi này khỏi danh sách duyệt"
          >
            <Trash2 size={12} />
          </button>
        </div>
      </div>

      {/* Micro Tag Strip */}
      <div className="flex items-center justify-between gap-2 text-xs py-0.5">
        <div className="flex items-center gap-1 flex-wrap flex-1 min-w-0">
          <span className="text-[10px] font-bold text-purple-400/80 flex items-center gap-0.5 shrink-0">
            <Hash size={10} />
          </span>
          {Array.isArray(item.tags) && item.tags.length > 0
            ? item.tags.map((tag: string) => (
                <span
                  key={tag}
                  className="inline-flex items-center gap-0.5 px-1.5 py-0.2 rounded text-[10px] font-bold bg-purple-950/50 text-purple-300 border border-purple-800/40"
                >
                  #{tag}
                  <button
                    type="button"
                    onClick={() => onRemoveTagFromQuestion(idx, tag)}
                    className="hover:text-red-400 cursor-pointer ml-0.5 text-[10px]"
                  >
                    ×
                  </button>
                </span>
              ))
            : null}
          <input
            type="text"
            value={tagInputPerQ[idx] || ''}
            onChange={e => onSetTagInputPerQ(idx, e.target.value)}
            onKeyDown={e => {
              if (e.key === 'Enter' || e.key === ',') {
                e.preventDefault();
                onAddTagToQuestion(idx, tagInputPerQ[idx] || '');
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
              onChange={e => onUpdateItem(idx, 'targetLevel', e.target.value)}
              placeholder="VD: Lớp 12, Senior RM..."
              className="w-full bg-slate-900 border border-slate-800 rounded-lg px-2.5 py-1 text-xs text-slate-200 focus:outline-none focus:border-purple-500"
            />
          </div>
          <div className="space-y-1">
            <label className="text-[10px] text-slate-400 font-bold">Mục đích sát hạch</label>
            <input
              type="text"
              value={item.assessmentPurpose || ''}
              onChange={e => onUpdateItem(idx, 'assessmentPurpose', e.target.value)}
              placeholder="VD: Thi tốt nghiệp THPT, AML..."
              className="w-full bg-slate-900 border border-slate-800 rounded-lg px-2.5 py-1 text-xs text-slate-200 focus:outline-none focus:border-purple-500"
            />
          </div>
          <div className="space-y-1">
            <label className="text-[10px] text-slate-400 font-bold">Cơ quan ban hành</label>
            <input
              type="text"
              value={item.issuingOrg || ''}
              onChange={e => onUpdateItem(idx, 'issuingOrg', e.target.value)}
              placeholder="VD: Chuyên Amsterdam, VCB..."
              className="w-full bg-slate-900 border border-slate-800 rounded-lg px-2.5 py-1 text-xs text-slate-200 focus:outline-none focus:border-purple-500"
            />
          </div>
          <div className="space-y-1">
            <label className="text-[10px] text-slate-400 font-bold">Năm & Chuẩn mực</label>
            <div className="flex gap-1.5">
              <input
                type="number"
                value={item.benchmarkYear || ''}
                onChange={e => onUpdateItem(idx, 'benchmarkYear', e.target.value ? parseInt(e.target.value) : undefined)}
                placeholder="2025"
                className="w-20 bg-slate-900 border border-slate-800 rounded-lg px-2.5 py-1 text-xs text-slate-200 focus:outline-none focus:border-purple-500"
              />
              <input
                type="text"
                value={item.benchmarkStandard || ''}
                onChange={e => onUpdateItem(idx, 'benchmarkStandard', e.target.value)}
                placeholder="GDPT 2018 / Basel III..."
                className="flex-1 bg-slate-900 border border-slate-800 rounded-lg px-2.5 py-1 text-xs text-slate-200 focus:outline-none focus:border-purple-500"
              />
            </div>
          </div>
        </div>
      )}

      {/* Ngữ Cảnh Dùng Chung (Bài đọc / tình huống) */}
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

      {/* Nội Dung Câu Hỏi */}
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
          onChange={e => onUpdateItem(idx, 'content', e.target.value)}
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

      {/* Phương Án Lựa Chọn */}
      {item.questionType !== 'ESSAY' && item.options && item.options.length > 0 && (
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <label className="text-[10px] font-black uppercase text-slate-400 tracking-wide">
              Phương án lựa chọn ({item.options.length} phương án)
            </label>
            <button
              type="button"
              onClick={() => onToggleShowRawOptions(idx)}
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
                      {item.questionType === 'MULTI_TRUE_FALSE' ? (
                        <span className="font-black text-purple-400 font-mono">
                          {String.fromCharCode('a'.charCodeAt(0) + optIdx)})
                        </span>
                      ) : (
                        <span className={`font-black ${isSelectedAnswer ? 'text-emerald-400' : 'text-slate-400'}`}>
                          {LETTERS[optIdx]}.
                        </span>
                      )}
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

                  <div className="min-h-[32px] flex items-center">
                    <MathRenderer content={opt} />
                  </div>

                  {(showRawOptions[idx] || !isImageOrMath) && (
                    <input
                      type="text"
                      value={opt}
                      onChange={e => onOptionChange(idx, optIdx, e.target.value)}
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

      {/* Đáp án & Lời giải */}
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

          {item.questionType === 'MULTI_TRUE_FALSE' ? (
            <div className="space-y-1.5 p-2 bg-slate-950/80 border border-slate-800 rounded-xl">
              <div className="flex flex-col gap-0.5 pb-1 border-b border-slate-850">
                <div className="text-[10px] text-cyan-400 font-black uppercase tracking-wider flex items-center justify-between">
                  <span>Ma trận Đúng / Sai:</span>
                  <span className="text-[10px] font-mono text-amber-400 bg-amber-950/50 px-1.5 py-0.2 rounded border border-amber-800/50">
                    {serializeMultiTfVector(parseMultiTfAnswer(item.suggestedAnswer))}
                  </span>
                </div>
                <span className="text-[10px] font-mono text-slate-400 truncate">
                  {serializeMultiTfAnswer(parseMultiTfAnswer(item.suggestedAnswer)) || 'Chưa thiết lập'}
                </span>
              </div>
              <div className="grid grid-cols-2 gap-1.5">
                {['a', 'b', 'c', 'd'].map(letter => {
                  const currentMap = parseMultiTfAnswer(item.suggestedAnswer);
                  const val = currentMap[letter];
                  return (
                    <div key={letter} className="flex items-center justify-between gap-1 px-2 py-1 bg-slate-900 rounded-lg border border-slate-800">
                      <span className="font-bold text-[11px] text-purple-400 font-mono">{letter})</span>
                      <div className="flex gap-1 shrink-0">
                        <button
                          type="button"
                          onClick={() => {
                            const next = { ...currentMap, [letter]: true };
                            onUpdateItem(idx, 'suggestedAnswer', serializeMultiTfAnswer(next));
                          }}
                          className={`px-2 py-0.5 rounded text-[10px] font-bold cursor-pointer transition-all ${
                            val === true ? 'bg-emerald-600 text-white shadow-sm' : 'bg-slate-800 text-slate-400 hover:text-white'
                          }`}
                        >
                          Đúng
                        </button>
                        <button
                          type="button"
                          onClick={() => {
                            const next = { ...currentMap, [letter]: false };
                            onUpdateItem(idx, 'suggestedAnswer', serializeMultiTfAnswer(next));
                          }}
                          className={`px-2 py-0.5 rounded text-[10px] font-bold cursor-pointer transition-all ${
                            val === false ? 'bg-rose-600 text-white shadow-sm' : 'bg-slate-800 text-slate-400 hover:text-white'
                          }`}
                        >
                          Sai
                        </button>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          ) : item.questionType !== 'ESSAY' && item.options && item.options.length > 0 ? (
            <select
              value={item.suggestedAnswer}
              onChange={e => onUpdateItem(idx, 'suggestedAnswer', e.target.value)}
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
              onChange={e => onUpdateItem(idx, 'suggestedAnswer', e.target.value)}
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

          {item.aiExplanation && (
            <div className="p-3 bg-slate-950/80 border border-slate-800 rounded-xl text-xs text-slate-300 space-y-1 max-h-40 overflow-y-auto leading-relaxed">
              <div className="text-[10px] uppercase font-bold text-slate-500 mb-1">Xem trước lời giải:</div>
              <MathRenderer content={item.aiExplanation} />
            </div>
          )}

          <textarea
            value={item.aiExplanation}
            onChange={e => onUpdateItem(idx, 'aiExplanation', e.target.value)}
            placeholder="Trích dẫn các bước giải toán, bảng biến thiên, đạo hàm hoặc căn cứ quy định..."
            className="w-full bg-slate-900 border border-slate-800 rounded-xl px-4 py-2 text-xs text-slate-300 focus:outline-none focus:border-blue-500"
            rows={item.aiExplanation ? 2 : 3}
          />
        </div>
      </div>
    </div>
  );
}
