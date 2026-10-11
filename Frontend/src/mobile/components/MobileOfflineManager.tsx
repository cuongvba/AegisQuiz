import React, { useState, useEffect } from 'react';
import { DownloadCloud, CheckCircle2, Trash2, HardDrive, WifiOff, Play, Loader2 } from 'lucide-react';
import { useMobileApp } from '../context/MobileAppContext';
import { offlineExamService, OfflineExamPackage } from '@/services/offline-exam.service';

interface MobileOfflineManagerProps {
  onStartOfflineExam: (pkg: OfflineExamPackage) => void;
}

export const MobileOfflineManager: React.FC<MobileOfflineManagerProps> = ({
  onStartOfflineExam,
}) => {
  const { variant, refreshDownloadedCount, triggerHaptic } = useMobileApp();
  const [downloadedList, setDownloadedList] = useState<OfflineExamPackage[]>([]);
  const [isDownloading, setIsDownloading] = useState<boolean>(false);

  const loadList = async () => {
    const list = await offlineExamService.listDownloadedExams();
    setDownloadedList(list);
  };

  useEffect(() => {
    loadList();
  }, []);

  const handleDownloadCurrentDomain = async () => {
    triggerHaptic(20);
    setIsDownloading(true);
    try {
      const newPkg: OfflineExamPackage = {
        examId: `pkg-${variant.id}-${Date.now()}`,
        title: `Bộ Đề Chuẩn ${variant.name}`,
        domainCode: variant.domainCode,
        questionCount: 60,
        downloadedAt: Date.now(),
        questions: [],
      };

      await offlineExamService.saveExamPackageOffline(newPkg);
      await loadList();
      await refreshDownloadedCount();
    } finally {
      setIsDownloading(false);
    }
  };

  return (
    <div className="space-y-4">
      <div className="p-4 rounded-2xl bg-slate-900/80 border border-slate-800 space-y-2">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <HardDrive size={16} className="text-sky-400" />
            <h2 className="text-xs font-black text-white uppercase">Kho Lưu Trữ Ngoại Tuyến</h2>
          </div>
          <span className="text-[10px] font-bold text-slate-400">IndexedDB Sovereign</span>
        </div>
        <p className="text-xs text-slate-300">
          Tải trọn bộ đề thi về điện thoại để luyện tập không cần kết nối mạng. Toàn bộ tiến trình sẽ tự động đồng bộ khi có Internet.
        </p>

        <button
          type="button"
          onClick={handleDownloadCurrentDomain}
          disabled={isDownloading}
          className="w-full mt-2 py-3 rounded-xl font-black text-xs text-slate-950 flex items-center justify-center gap-2 transition-all cursor-pointer active:scale-98 disabled:opacity-50"
          style={{ backgroundColor: variant.colorScheme.primary }}
        >
          {isDownloading ? (
            <>
              <Loader2 size={15} className="animate-spin" />
              <span>Đang Tải Gói Đề Về Máy...</span>
            </>
          ) : (
            <>
              <DownloadCloud size={16} />
              <span>Tải Gói Đề {variant.shortName} (Offline)</span>
            </>
          )}
        </button>
      </div>

      <div className="space-y-2">
        <h3 className="text-xs font-black text-slate-400 uppercase tracking-wider">
          Gói Đề Đã Lưu Trong Máy ({downloadedList.length})
        </h3>

        {downloadedList.length === 0 ? (
          <div className="p-8 text-center rounded-2xl bg-slate-900/40 border border-slate-850 space-y-2">
            <WifiOff size={28} className="mx-auto text-slate-600" />
            <p className="text-xs text-slate-400 font-medium">Chưa có bộ đề nào được tải về máy.</p>
            <p className="text-[11px] text-slate-500">Bấm nút phía trên để tải bộ đề ôn thi ngoại tuyến.</p>
          </div>
        ) : (
          downloadedList.map((pkg) => (
            <div
              key={pkg.examId}
              className="p-3.5 rounded-2xl bg-slate-900/70 border border-slate-800 flex items-center justify-between gap-3"
            >
              <div className="min-w-0">
                <h4 className="text-xs font-bold text-white truncate">{pkg.title}</h4>
                <div className="flex items-center gap-2 text-[10px] text-slate-400 mt-0.5">
                  <span className="text-emerald-400 font-bold flex items-center gap-1">
                    <CheckCircle2 size={11} /> Đã sẵn sàng
                  </span>
                  <span>•</span>
                  <span>{pkg.questionCount} câu hỏi</span>
                </div>
              </div>

              <button
                type="button"
                onClick={() => {
                  triggerHaptic(15);
                  onStartOfflineExam(pkg);
                }}
                className="px-3 py-1.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-xs font-bold text-white flex items-center gap-1 cursor-pointer shrink-0"
              >
                <Play size={12} fill="currentColor" />
                <span>Làm Bài</span>
              </button>
            </div>
          ))
        )}
      </div>
    </div>
  );
};
