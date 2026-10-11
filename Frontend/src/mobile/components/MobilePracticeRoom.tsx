import React, { useState, useEffect } from 'react';
import { ArrowLeft, Clock, CheckCircle2, ChevronLeft, ChevronRight, Sparkles, AlertTriangle, ShieldCheck } from 'lucide-react';
import { useMobileApp } from '../context/MobileAppContext';
import { offlineExamService } from '@/services/offline-exam.service';
import { KidsPracticeView } from './KidsPracticeView';
import { InteractiveSliceViewer } from './InteractiveSliceViewer';
import { transpileExplanationToSlices } from '../utils/explanation-slice-transpiler';

interface MobilePracticeRoomProps {
  topicName: string;
  onExit: () => void;
}

export const MobilePracticeRoom: React.FC<MobilePracticeRoomProps> = ({
  topicName,
  onExit,
}) => {
  const { variant, triggerHaptic } = useMobileApp();

  // If this variant is configured with Kids Gamified UI, render KidsPracticeView
  if (variant.uiMode === 'gamified_kids') {
    return <KidsPracticeView topicName={topicName} onExit={onExit} />;
  }

  const [currentIdx, setCurrentIdx] = useState<number>(0);
  const [selectedAnswers, setSelectedAnswers] = useState<Record<number, string>>({});
  const [showExplanation, setShowExplanation] = useState<boolean>(false);
  const [showStoryModal, setShowStoryModal] = useState<boolean>(false);
  const [timeLeft, setTimeLeft] = useState<number>(1200); // 20 phút

  // Dữ liệu mẫu mô phỏng bộ câu hỏi chuẩn theo biến thể
  const sampleQuestions = React.useMemo(() => {
    if (variant.id === 'driving') {
      return [
        {
          id: 'q1',
          content: 'Người điều khiển xe mô tô hai bánh, xe gắn máy KHÔNG ĐƯỢC thực hiện những hành vi nào dưới đây?',
          isFatal: true, // Câu điểm liệt
          options: [
            { key: 'A', text: 'Đi vào phần đường dành cho người đi bộ và phương tiện khác.' },
            { key: 'B', text: 'Sử dụng ô, điện thoại di động, thiết bị âm thanh (trừ thiết bị trợ thính).' },
            { key: 'C', text: 'Buông cả hai tay hoặc đi xe bằng một bánh.' },
            { key: 'D', text: 'Tất cả các hành vi nêu trên.' },
          ],
          correctKey: 'D',
          explanation: 'Theo Luật Giao thông Đường bộ, nghiêm cấm người đi xe máy buông hai tay, dùng điện thoại hoặc đi vào hè phố. Đây là câu hỏi điểm liệt cốt tử!',
        },
        {
          id: 'q2',
          content: 'Biển nào báo hiệu "Giao nhau với đường ưu tiên"?',
          isFatal: false,
          options: [
            { key: 'A', text: 'Biển 1 (Tam giác đều đỉnh hướng lên trên).' },
            { key: 'B', text: 'Biển 2 (Tam giác đều đỉnh lộn ngược xuống dưới).' },
            { key: 'C', text: 'Biển 3 (Hình vuông đặt chéo màu vàng).' },
            { key: 'D', text: 'Biển 1 và Biển 3.' },
          ],
          correctKey: 'B',
          explanation: 'Biển số 208 "Giao nhau với đường ưu tiên" có hình tam giác đều ngược đỉnh xuống dưới, nền vàng viền đỏ.',
        },
      ];
    }

    if (variant.id === 'banking') {
      return [
        {
          id: 'qb1',
          content: 'Theo Luật Phòng chống rửa tiền hiện hành, mức giao dịch có giá trị lớn phải báo cáo Ngân hàng Nhà nước là từ bao nhiêu?',
          isFatal: false,
          options: [
            { key: 'A', text: 'Từ 200.000.000 VNĐ trở lên' },
            { key: 'B', text: 'Từ 300.000.000 VNĐ trở lên' },
            { key: 'C', text: 'Từ 400.000.000 VNĐ trở lên' },
            { key: 'D', text: 'Từ 500.000.000 VNĐ trở lên' },
          ],
          correctKey: 'C',
          explanation: 'Theo Quyết định số 11/2023/QĐ-TTg, mức giao dịch có giá trị lớn phải báo cáo là từ 400.000.000 (bốn trăm triệu) đồng trở lên.',
        },
      ];
    }

    return [
      {
        id: 'q_gen',
        content: `Câu hỏi mẫu chuyên ngành ${variant.shortName}: Khái niệm cốt lõi nào sau đây là chuẩn mực hàng đầu?`,
        isFatal: false,
        options: [
          { key: 'A', text: 'Phương án phân tích A theo quy chuẩn quốc tế' },
          { key: 'B', text: 'Phương án phân tích B tối ưu nhận thức' },
          { key: 'C', text: 'Phương án phân tích C đáp ứng tiêu chuẩn ngành' },
          { key: 'D', text: 'Cả 3 phương án trên đều chuẩn xác' },
        ],
        correctKey: 'D',
        explanation: 'Hệ thống tự động đồng bộ câu hỏi theo ma trận chuyên ngành tương ứng.',
      },
    ];
  }, [variant.id, variant.shortName]);

  const currentQ = sampleQuestions[currentIdx] || sampleQuestions[0];
  const totalQuestions = sampleQuestions.length;

  useEffect(() => {
    const timer = setInterval(() => {
      setTimeLeft((t) => (t > 0 ? t - 1 : 0));
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  const handleSelectOption = async (optKey: string) => {
    triggerHaptic(15);
    setSelectedAnswers((prev) => ({ ...prev, [currentIdx]: optKey }));

    // Lưu vết bảo mật vào Merkle Chain Offline DB
    await offlineExamService.recordAnswerAsync('practice-session-active', currentQ.id, optKey);
  };

  const formatTime = (secs: number) => {
    const m = Math.floor(secs / 60);
    const s = secs % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  return (
    <div className="fixed inset-0 z-50 bg-slate-950 flex flex-col font-sans select-none">
      {/* ── Top Bar ────────────────────────────────────────────────────── */}
      <div
        className="px-4 py-3 bg-slate-950/90 backdrop-blur-xl border-b border-slate-800 flex items-center justify-between"
        style={{ paddingTop: 'calc(0.75rem + env(safe-area-inset-top, 0px))' }}
      >
        <button
          type="button"
          onClick={() => {
            triggerHaptic(10);
            onExit();
          }}
          className="p-2 rounded-xl bg-slate-900 border border-slate-800 text-slate-300 hover:text-white cursor-pointer"
        >
          <ArrowLeft size={18} />
        </button>

        <div className="text-center">
          <span className="text-xs font-black text-white block">
            Câu {currentIdx + 1} / {totalQuestions}
          </span>
          <span className="text-[10px] text-slate-400 block truncate max-w-[180px]">{topicName}</span>
        </div>

        <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-xl bg-slate-900 border border-slate-800 text-amber-300 text-xs font-mono font-bold">
          <Clock size={12} />
          <span>{formatTime(timeLeft)}</span>
        </div>
      </div>

      {/* ── Progress Bar ────────────────────────────────────────────────── */}
      <div className="w-full h-1 bg-slate-900">
        <div
          className="h-full transition-all duration-300"
          style={{
            width: `${((currentIdx + 1) / totalQuestions) * 100}%`,
            backgroundColor: variant.colorScheme.primary,
          }}
        />
      </div>

      {/* ── Question Body ──────────────────────────────────────────────── */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {currentQ.isFatal && (
          <div className="p-2.5 rounded-xl bg-red-950/60 border border-red-500/50 flex items-center gap-2 text-xs text-red-200">
            <AlertTriangle size={15} className="text-red-400 shrink-0 animate-bounce" />
            <span className="font-bold">CÂU ĐIỂM LIỆT: Trả lời sai sẽ bị tính điểm liệt trượt sát hạch!</span>
          </div>
        )}

        <div className="p-4 rounded-2xl bg-slate-900/60 border border-slate-800">
          <p className="text-sm font-bold text-white leading-relaxed">{currentQ.content}</p>
        </div>

        {/* Danh sách phương án A, B, C, D */}
        <div className="space-y-2.5">
          {currentQ.options.map((opt) => {
            const isChosen = selectedAnswers[currentIdx] === opt.key;
            return (
              <button
                key={opt.key}
                type="button"
                onClick={() => handleSelectOption(opt.key)}
                className={`w-full text-left p-3.5 rounded-2xl border text-xs font-bold transition-all cursor-pointer flex items-start gap-3 active:scale-98 ${
                  isChosen
                    ? 'bg-slate-900 border-slate-700 ring-2'
                    : 'bg-slate-950 border-slate-800 hover:bg-slate-900/50'
                }`}
                style={{
                  borderColor: isChosen ? variant.colorScheme.primary : undefined,
                }}
              >
                <div
                  className={`w-7 h-7 rounded-xl flex items-center justify-center text-xs font-black shrink-0 transition-colors ${
                    isChosen ? 'text-slate-950 shadow-md' : 'bg-slate-900 text-slate-400 border border-slate-800'
                  }`}
                  style={{
                    backgroundColor: isChosen ? variant.colorScheme.primary : undefined,
                  }}
                >
                  {opt.key}
                </div>
                <div className="flex-1 pt-1 text-slate-200 leading-normal">{opt.text}</div>
              </button>
            );
          })}
        </div>

        {/* Giải thích thông minh & Micro-Lesson Story */}
        {showExplanation && (
          <div className="p-3.5 rounded-2xl bg-purple-950/40 border border-purple-500/40 space-y-2 animate-in fade-in duration-200">
            <div className="flex items-center justify-between text-xs font-bold text-purple-300">
              <span className="flex items-center gap-1.5">
                <Sparkles size={14} /> Giải thích chuẩn:
              </span>
              <button
                type="button"
                onClick={() => setShowStoryModal(true)}
                className="px-2.5 py-1 rounded-full bg-gradient-to-r from-amber-400 to-pink-500 text-slate-950 text-[10px] font-black flex items-center gap-1 active:scale-95 transition-transform cursor-pointer shadow-sm"
              >
                <Sparkles size={11} /> Bài Giảng Slice 🎬
              </button>
            </div>
            <p className="text-xs text-slate-300 leading-relaxed">{currentQ.explanation}</p>
          </div>
        )}
      </div>

      {/* ── Bottom Controls ────────────────────────────────────────────── */}
      <div
        className="p-3 bg-slate-950/90 backdrop-blur-xl border-t border-slate-800 flex items-center justify-between gap-2"
        style={{ paddingBottom: 'calc(0.75rem + env(safe-area-inset-bottom, 12px))' }}
      >
        <button
          type="button"
          onClick={() => {
            triggerHaptic(10);
            setCurrentIdx((i) => Math.max(0, i - 1));
          }}
          disabled={currentIdx === 0}
          className="p-3 rounded-xl bg-slate-900 border border-slate-800 text-slate-300 disabled:opacity-30 cursor-pointer"
        >
          <ChevronLeft size={18} />
        </button>

        <button
          type="button"
          onClick={() => {
            triggerHaptic(15);
            setShowExplanation(!showExplanation);
          }}
          className="px-3.5 py-2.5 rounded-xl bg-slate-900 border border-slate-800 text-purple-300 text-xs font-bold flex items-center gap-1.5 cursor-pointer"
        >
          <Sparkles size={13} />
          <span>{showExplanation ? 'Ẩn Giải Thích' : 'Xem Mẹo/Giải Thích'}</span>
        </button>

        <button
          type="button"
          onClick={() => {
            triggerHaptic(15);
            if (currentIdx < totalQuestions - 1) {
              setCurrentIdx((i) => i + 1);
            } else {
              onExit();
            }
          }}
          className="px-4 py-2.5 rounded-xl font-black text-xs text-slate-950 flex items-center gap-1 cursor-pointer active:scale-95 shadow-lg"
          style={{ backgroundColor: variant.colorScheme.primary }}
        >
          <span>{currentIdx < totalQuestions - 1 ? 'Câu Tiếp' : 'Nộp Bài'}</span>
          <ChevronRight size={16} />
        </button>
      </div>

      {/* Micro-Lesson Interactive Slice Storyboard */}
      {showStoryModal && (
        <InteractiveSliceViewer
          slices={transpileExplanationToSlices(currentQ.explanation, currentQ.content, variant.code)}
          topicName={`${topicName} — Câu ${currentIdx + 1}`}
          onClose={() => setShowStoryModal(false)}
        />
      )}
    </div>
  );
};
