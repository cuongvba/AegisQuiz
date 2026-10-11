import React, { useState } from 'react';
import { Download, WifiOff, X, Sparkles, Smartphone, Share } from 'lucide-react';
import { usePwaInstall } from '@/shared/hooks/usePwaInstall';

export const PwaInstallBanner: React.FC = () => {
  const { canInstall, isInstalled, isIos, isOffline, installPwa, dismiss } = usePwaInstall();
  const [showIosGuide, setShowIosGuide] = useState<boolean>(false);
  const [isDismissed, setIsDismissed] = useState<boolean>(false);

  if (isDismissed || isInstalled) {
    if (isOffline) {
      return (
        <div className="fixed top-2 left-1/2 -translate-x-1/2 z-50 px-3.5 py-1.5 rounded-full bg-amber-500/90 text-slate-950 text-xs font-black flex items-center gap-2 shadow-xl backdrop-blur-md animate-in fade-in slide-in-from-top duration-200">
          <WifiOff size={14} className="animate-pulse" />
          <span>Đang chạy Chế độ Ngoại Tuyến (Offline Mode)</span>
        </div>
      );
    }
    return null;
  }

  // Nếu không thể install và không phải iOS và đang online thì không hiện
  if (!canInstall && !isIos && !isOffline) {
    return null;
  }

  return (
    <>
      {/* Badge báo mất mạng */}
      {isOffline && (
        <div className="fixed top-2 left-1/2 -translate-x-1/2 z-50 px-3.5 py-1.5 rounded-full bg-amber-500/90 text-slate-950 text-xs font-black flex items-center gap-2 shadow-xl backdrop-blur-md animate-in fade-in slide-in-from-top duration-200">
          <WifiOff size={14} className="animate-pulse" />
          <span>Đang chạy Chế độ Ngoại Tuyến (Offline Mode)</span>
        </div>
      )}

      {/* Banner cài đặt PWA di động */}
      {(canInstall || isIos) && (
        <aside
          aria-label="Cài đặt ứng dụng AegisQuiz lên thiết bị"
          className="fixed bottom-4 left-4 right-4 md:left-auto md:right-6 md:w-96 z-50 p-3.5 rounded-2xl bg-slate-950/95 border border-purple-500/50 shadow-2xl shadow-purple-950/60 backdrop-blur-xl animate-in fade-in slide-in-from-bottom duration-300"
        >
          <div className="flex items-start gap-3">
            <div className="p-2.5 rounded-xl bg-gradient-to-br from-purple-600 to-indigo-600 text-white shadow-lg shadow-purple-600/40 shrink-0">
              <Smartphone size={20} />
            </div>

            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-1.5">
                <span className="text-xs font-black text-white">Cài Đặt App AegisQuiz</span>
                <span className="text-[10px] font-bold px-1.5 py-0.5 rounded-full bg-purple-900/80 text-purple-300 border border-purple-700/50">
                  Offline-Ready
                </span>
              </div>
              <p className="text-[11px] text-slate-300 mt-0.5 leading-snug">
                Trải nghiệm cảm ứng 60fps, làm bài thi ngoại tuyến 100% không lo mất mạng.
              </p>

              {/* Hướng dẫn cho iOS Safari */}
              {showIosGuide && (
                <div className="mt-2 p-2 rounded-xl bg-slate-900/90 border border-slate-800 text-[11px] text-sky-300 space-y-1">
                  <div className="flex items-center gap-1.5 font-bold">
                    <Share size={13} />
                    <span>Bước 1: Bấm nút Chia sẻ ở thanh dưới Safari</span>
                  </div>
                  <div>Bước 2: Chọn <strong>&quot;Thêm vào MH chính&quot;</strong> (Add to Home Screen)</div>
                </div>
              )}

              <div className="flex items-center gap-2 mt-2.5">
                {canInstall ? (
                  <button
                    type="button"
                    onClick={installPwa}
                    className="px-3.5 py-1.5 rounded-xl bg-gradient-to-r from-purple-600 via-indigo-600 to-sky-600 hover:from-purple-500 hover:to-sky-500 text-white text-xs font-black shadow-lg shadow-purple-600/30 flex items-center gap-1.5 cursor-pointer active:scale-95 transition-all"
                  >
                    <Download size={13} />
                    <span>Cài Đặt Ngay</span>
                  </button>
                ) : isIos ? (
                  <button
                    type="button"
                    onClick={() => setShowIosGuide(!showIosGuide)}
                    className="px-3.5 py-1.5 rounded-xl bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold flex items-center gap-1.5 cursor-pointer active:scale-95 transition-all"
                  >
                    <Share size={13} />
                    <span>Xem Cách Cài (iOS)</span>
                  </button>
                ) : null}

                <button
                  type="button"
                  onClick={() => {
                    dismiss();
                    setIsDismissed(true);
                  }}
                  className="px-2.5 py-1.5 rounded-xl text-slate-400 hover:text-white text-xs font-medium cursor-pointer transition-colors"
                >
                  Để sau
                </button>
              </div>
            </div>

            <button
              type="button"
              onClick={() => {
                dismiss();
                setIsDismissed(true);
              }}
              className="text-slate-400 hover:text-white p-1 rounded-lg transition-colors cursor-pointer"
            >
              <X size={15} />
            </button>
          </div>
        </aside>
      )}
    </>
  );
};
