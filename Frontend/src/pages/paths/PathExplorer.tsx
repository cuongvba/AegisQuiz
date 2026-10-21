import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { MapPin, CheckCircle, Lock, Play, Star, BookOpen, Loader2, Sparkles, ArrowRight } from 'lucide-react';
import api from '@/services/api';

interface TopicNode {
  code: string;
  name: string;
  count: number;
  totalCount?: number;
}

export function PathExplorer() {
  const navigate = useNavigate();
  const { t } = useTranslation('translation');

  const { data: topics = [], isLoading } = useQuery<TopicNode[]>({
    queryKey: ['path-explorer-topics'],
    queryFn: async () => {
      const res = await api.get('/api/quiz/topics/counts');
      return Array.isArray(res.data) ? res.data.filter((x: TopicNode) => x.count > 0 || (x.totalCount ?? 0) > 0) : [];
    },
    staleTime: 5 * 60_000,
  });

  return (
    <div className="max-w-7xl mx-auto px-4 py-8 pb-24 text-slate-100">
      {/* ── Header ────────────────────────────────────────────────────────── */}
      <div className="text-center mb-16">
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-cyan-500/10 border border-cyan-500/20 text-cyan-400 text-xs font-bold uppercase tracking-wider mb-3">
          <Sparkles className="w-3.5 h-3.5" />
          {t('paths.badge', 'Bản Đồ Lộ Trình Tri Thức')}
        </div>
        <h1 className="text-3xl sm:text-4xl font-black text-white mb-4 flex items-center justify-center gap-3">
          <MapPin className="text-cyan-400" size={36} />
          {t('paths.title', 'Lộ Trình Khảo Thí & Chinh Phục Chuyên Đề')}
        </h1>
        <p className="text-slate-400 font-medium text-base max-w-2xl mx-auto">
          {t('paths.subtitle', 'Hoàn thành các trạm kiến thức để nâng cao năng lực toán học và nghiệp vụ. Thu thập điểm số và chứng chỉ chuyên gia!')}
        </p>
      </div>

      {isLoading ? (
        <div className="flex flex-col items-center justify-center py-24 gap-3">
          <Loader2 className="w-10 h-10 text-cyan-400 animate-spin" />
          <p className="text-slate-400 text-sm font-medium">{t('common.loading', 'Đang tải bản đồ tri thức...')}</p>
        </div>
      ) : topics.length === 0 ? (
        <div className="max-w-md mx-auto text-center py-16 px-6 bg-slate-900/60 rounded-3xl border border-slate-800 backdrop-blur-xl">
          <BookOpen className="w-12 h-12 text-slate-500 mx-auto mb-4" />
          <h3 className="text-lg font-bold text-white mb-2">{t('paths.emptyTitle', 'Chưa có trạm chuyên đề')}</h3>
          <p className="text-sm text-slate-400 mb-6">
            {t('paths.emptyDesc', 'Hệ thống đang chuẩn bị các ngân hàng câu hỏi. Bạn có thể vào phòng luyện thi tổng hợp.')}
          </p>
          <button
            onClick={() => navigate('/practice')}
            className="px-6 py-2.5 rounded-xl bg-gradient-to-r from-cyan-500 to-indigo-600 text-white font-bold text-sm shadow-lg shadow-cyan-500/20 hover:brightness-110 transition-all cursor-pointer inline-flex items-center gap-2"
          >
            {t('paths.goToPractice', 'Vào Luyện Thi Ngay')}
            <ArrowRight size={16} />
          </button>
        </div>
      ) : (
        <div className="relative max-w-3xl mx-auto py-6">
          {/* Trục nối giữa các node */}
          <div className="absolute top-0 bottom-0 left-1/2 w-1 -translate-x-1/2 bg-gradient-to-b from-cyan-500 via-indigo-500 to-purple-600 opacity-30 z-0" />

          <div className="flex flex-col gap-12 relative z-10 w-full px-4">
            {topics.map((node, index) => {
              const isLeft = index % 2 === 0;
              const isActive = index === 0;
              const qCount = node.totalCount ?? node.count ?? 0;

              return (
                <div
                  key={node.code}
                  className={`flex items-center justify-between w-full relative transition-all duration-300 hover:-translate-y-1 ${
                    isLeft ? 'flex-row-reverse md:flex-row' : 'flex-row md:flex-row-reverse'
                  }`}
                >
                  {/* Cột đệm tạo bố cục zic-zac */}
                  <div className="hidden md:block w-[45%]" />

                  {/* Card nội dung trạm */}
                  <div
                    className={`w-full md:w-[45%] rounded-3xl p-6 shadow-2xl border transition-all ${
                      isActive
                        ? 'bg-slate-900/90 border-cyan-500/60 shadow-cyan-500/20 ring-2 ring-cyan-500/30'
                        : 'bg-slate-900/60 border-slate-800 hover:border-slate-700'
                    }`}
                  >
                    <div className="flex justify-between items-start mb-3">
                      <span className="px-2.5 py-0.5 rounded-full text-[10px] font-black uppercase tracking-wider bg-cyan-500/10 text-cyan-400 border border-cyan-500/20">
                        {node.code}
                      </span>
                      {isActive ? (
                        <Play size={20} className="text-cyan-400 fill-current" />
                      ) : (
                        <CheckCircle size={20} className="text-emerald-400" />
                      )}
                    </div>

                    <h3 className="text-lg font-bold text-white mb-1.5">{node.name}</h3>

                    <p className="text-xs text-slate-400 mb-5">
                      {qCount > 0 ? `${qCount} câu hỏi sẵn sàng kiểm tra` : 'Chuyên đề ôn tập chuyên sâu'}
                    </p>

                    <button
                      onClick={() => navigate(`/practice?topic=${encodeURIComponent(node.code)}`)}
                      className="w-full py-2.5 rounded-xl font-bold text-xs shadow-md transition-all flex items-center justify-center gap-1.5 cursor-pointer bg-gradient-to-r from-cyan-500 to-indigo-600 text-white hover:brightness-110 active:scale-98"
                    >
                      <Play size={14} className="fill-current" />
                      {t('paths.startPractice', 'Chinh Phục Trạm Này')}
                    </button>
                  </div>

                  {/* Node icon giữa trục nối */}
                  <div
                    className={`absolute left-1/2 -translate-x-1/2 w-10 h-10 rounded-full border-2 shadow-lg flex items-center justify-center z-20 ${
                      isActive
                        ? 'bg-cyan-500 border-white text-slate-950 animate-pulse'
                        : 'bg-slate-900 border-slate-700 text-slate-300'
                    }`}
                  >
                    <Star size={14} fill="currentColor" />
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
export default PathExplorer;