import React, { useState } from 'react';
import { X, SlidersHorizontal, Clock, Shuffle, CheckCircle, Timer } from 'lucide-react';
import { useLearnerI18n } from '@/lib/i18n';

export interface MobileSessionConfig {
  enableQuestionTimer: boolean;
  shuffleOptions: boolean;
  shuffleQuestions: boolean;
  autoNextOnTimeout: boolean;
  timeLimitMin: number;
}

export const DEFAULT_MOBILE_SESSION_CONFIG: MobileSessionConfig = {
  enableQuestionTimer: false, // Mặc định OFF theo yêu cầu người dùng
  shuffleOptions: true,
  shuffleQuestions: true,
  autoNextOnTimeout: true,
  timeLimitMin: 20,
};

export const getStoredMobileConfig = (): MobileSessionConfig => {
  try {
    const raw = localStorage.getItem('aegis_mobile_session_config');
    if (raw) return { ...DEFAULT_MOBILE_SESSION_CONFIG, ...JSON.parse(raw) };
  } catch {}
  return DEFAULT_MOBILE_SESSION_CONFIG;
};

export const saveStoredMobileConfig = (cfg: MobileSessionConfig) => {
  try {
    localStorage.setItem('aegis_mobile_session_config', JSON.stringify(cfg));
  } catch {}
};

interface MobileSessionConfigModalProps {
  isOpen: boolean;
  onClose: () => void;
  config: MobileSessionConfig;
  onSave: (newConfig: MobileSessionConfig) => void;
}

export const MobileSessionConfigModal: React.FC<MobileSessionConfigModalProps> = ({
  isOpen,
  onClose,
  config: initialConfig,
  onSave,
}) => {
  const { t } = useLearnerI18n();
  const [config, setConfig] = useState<MobileSessionConfig>(initialConfig);

  if (!isOpen) return null;

  const handleSave = () => {
    saveStoredMobileConfig(config);
    onSave(config);
    onClose();
  };

  return (
    <div className="fixed inset-0 z-50 flex items-end sm:items-center justify-center bg-black/75 backdrop-blur-sm p-0 sm:p-4 animate-in fade-in duration-200">
      <div className="w-full max-w-md bg-slate-900 border border-slate-800 rounded-t-3xl sm:rounded-3xl p-5 shadow-2xl space-y-4 max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-slate-800 pb-3">
          <div className="flex items-center gap-2">
            <div className="p-2 rounded-xl bg-cyan-950 text-cyan-400 border border-cyan-800">
              <SlidersHorizontal size={16} />
            </div>
            <div>
              <h3 className="text-sm font-black text-white">{t('mobileSessionConfig') || 'Cấu hình phiên làm bài'}</h3>
              <p className="text-[10px] text-slate-400">Tùy biến nhịp độ làm bài & khống chế thời gian</p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 cursor-pointer"
          >
            <X size={18} />
          </button>
        </div>

        {/* Các Tùy Chọn */}
        <div className="space-y-3">
          {/* 1. Khống chế thời gian từng câu (Bắt Expired) */}
          <div className="p-3.5 rounded-2xl bg-slate-950 border border-slate-800 space-y-1.5">
            <div className="flex items-start justify-between gap-3">
              <div className="flex items-start gap-2">
                <Clock size={16} className="text-rose-400 mt-0.5 shrink-0" />
                <div>
                  <div className="text-xs font-bold text-white">
                    {t('enableQuestionTimer') || 'Khống chế thời gian từng câu (Bắt Expired)'}
                  </div>
                  <p className="text-[10px] text-slate-400 leading-tight mt-0.5">
                    {t('enableQuestionTimerDesc') || 'Khóa câu và đếm ngược nghiêm ngặt khi hết giờ. Mặc định: TẮT.'}
                  </p>
                </div>
              </div>
              <input
                type="checkbox"
                checked={config.enableQuestionTimer}
                onChange={(e) => setConfig({ ...config, enableQuestionTimer: e.target.checked })}
                className="h-5 w-5 rounded border-slate-700 bg-slate-800 text-rose-500 focus:ring-rose-500 cursor-pointer shrink-0"
              />
            </div>
            {config.enableQuestionTimer && (
              <div className="text-[10px] text-rose-300 bg-rose-950/40 p-2 rounded-xl border border-rose-900/50">
                ⚠️ Chế độ khống chế đang BẬT: Mỗi câu hỏi sẽ bị khóa khi hết thời gian quy định!
              </div>
            )}
          </div>

          {/* 2. Xáo trộn phương án */}
          <div className="p-3.5 rounded-2xl bg-slate-950 border border-slate-800 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Shuffle size={16} className="text-cyan-400 shrink-0" />
              <div>
                <div className="text-xs font-bold text-white">{t('shuffleOptions') || 'Xáo vị trí phương án'}</div>
                <p className="text-[10px] text-slate-400">Đổi ngẫu nhiên thứ tự các đáp án A, B, C, D</p>
              </div>
            </div>
            <input
              type="checkbox"
              checked={config.shuffleOptions}
              onChange={(e) => setConfig({ ...config, shuffleOptions: e.target.checked })}
              className="h-5 w-5 rounded border-slate-700 bg-slate-800 text-cyan-500 focus:ring-cyan-500 cursor-pointer"
            />
          </div>

          {/* 3. Xáo trộn câu hỏi */}
          <div className="p-3.5 rounded-2xl bg-slate-950 border border-slate-800 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Shuffle size={16} className="text-purple-400 shrink-0" />
              <div>
                <div className="text-xs font-bold text-white">{t('shuffleQuestions') || 'Xáo trộn thứ tự câu hỏi'}</div>
                <p className="text-[10px] text-slate-400">Trộn đề thi ngẫu nhiên giữa các chủ đề</p>
              </div>
            </div>
            <input
              type="checkbox"
              checked={config.shuffleQuestions}
              onChange={(e) => setConfig({ ...config, shuffleQuestions: e.target.checked })}
              className="h-5 w-5 rounded border-slate-700 bg-slate-800 text-purple-500 focus:ring-purple-500 cursor-pointer"
            />
          </div>

          {/* 4. Tự động qua câu khi hết giờ */}
          <div className="p-3.5 rounded-2xl bg-slate-950 border border-slate-800 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Timer size={16} className="text-amber-400 shrink-0" />
              <div>
                <div className="text-xs font-bold text-white">{t('autoNextTimeout') || 'Tự động qua câu khi hết giờ'}</div>
                <p className="text-[10px] text-slate-400">Tự động chuyển câu tiếp theo nếu hết thời gian</p>
              </div>
            </div>
            <input
              type="checkbox"
              checked={config.autoNextOnTimeout}
              onChange={(e) => setConfig({ ...config, autoNextOnTimeout: e.target.checked })}
              className="h-5 w-5 rounded border-slate-700 bg-slate-800 text-amber-500 focus:ring-amber-500 cursor-pointer"
            />
          </div>

          {/* 5. Thời gian tổng bài thi */}
          <div className="p-3.5 rounded-2xl bg-slate-950 border border-slate-800 flex items-center justify-between">
            <div>
              <div className="text-xs font-bold text-white">{t('timeLimitMinutes') || 'Thời gian làm bài (phút)'}</div>
              <p className="text-[10px] text-slate-400">Tổng thời gian cho cả phiên thi (0 = không giới hạn)</p>
            </div>
            <input
              type="number"
              min={0}
              max={180}
              value={config.timeLimitMin}
              onChange={(e) => setConfig({ ...config, timeLimitMin: Math.max(0, Number(e.target.value) || 0) })}
              className="w-16 p-2 rounded-xl bg-slate-900 border border-slate-700 text-center text-xs font-mono font-bold text-white focus:outline-none focus:border-cyan-500"
            />
          </div>
        </div>

        {/* Nút hành động */}
        <div className="pt-2 flex items-center gap-2">
          <button
            type="button"
            onClick={onClose}
            className="flex-1 py-3 rounded-2xl border border-slate-800 text-xs font-bold text-slate-300 hover:bg-slate-800 cursor-pointer"
          >
            Đóng
          </button>
          <button
            type="button"
            onClick={handleSave}
            className="flex-1 py-3 rounded-2xl bg-gradient-to-r from-cyan-500 to-blue-600 text-slate-950 font-black text-xs flex items-center justify-center gap-1.5 shadow-lg active:scale-95 cursor-pointer"
          >
            <CheckCircle size={15} />
            <span>{t('saveAndApply') || 'Lưu & Áp Dụng'}</span>
          </button>
        </div>
      </div>
    </div>
  );
};
