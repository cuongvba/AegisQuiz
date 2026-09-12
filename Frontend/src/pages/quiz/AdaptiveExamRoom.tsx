import { useState, useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { 
  Zap, Brain, Award, Clock, ArrowRight, RotateCcw, 
  Sparkles, CheckCircle2, XCircle, AlertCircle, Home, LineChart, Loader2
} from 'lucide-react';
import confetti from 'canvas-confetti';
import { learnerQuizService } from '@/services/learner-quiz.service';
import { QuestionCard } from '@/components/quiz/QuestionCard';
import type { LearnerAnswer, LearnerQuestion } from '@/types/quiz';

export function AdaptiveExamRoom() {
  const navigate = useNavigate();

  // State
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [currentQuestion, setCurrentQuestion] = useState<LearnerQuestion | null>(null);
  const [currentAnswer, setCurrentAnswer] = useState<LearnerAnswer>({});
  const [administeredCount, setAdministeredCount] = useState(0);
  const [maxItems, setMaxItems] = useState(20);
  const [theta, setTheta] = useState(0.0);
  const [se, setSe] = useState(1.0);
  const [thetaHistory, setThetaHistory] = useState<Array<{ step: number; theta: number; se: number }>>([]);
  
  const [isInitializing, setIsInitializing] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [stepFeedback, setStepFeedback] = useState<{ isCorrect: boolean; explanation?: string } | null>(null);
  const [isFinished, setIsFinished] = useState(false);
  const [finalReport, setFinalReport] = useState<any>(null);

  const questionStartTimeRef = useRef<number>(Date.now());

  // Bắt đầu phiên CAT khi vào trang
  useEffect(() => {
    let mounted = true;

    async function initCat() {
      try {
        setIsInitializing(true);
        const res = await learnerQuizService.startCatSession({
          topicCode: 'ALL',
          minItems: 5,
          maxItems: 15,
          seThreshold: 0.35
        });

        if (!mounted) return;

        if (res && res.sessionId) {
          setSessionId(res.sessionId);
          setCurrentQuestion(res.normalizedQuestion || res.question);
          setAdministeredCount(res.administeredCount || 1);
          setMaxItems(res.maxItems || 15);
          setTheta(res.currentTheta || 0.0);
          setThetaHistory([{ step: 0, theta: res.currentTheta || 0.0, se: 1.0 }]);
          questionStartTimeRef.current = Date.now();
        }
      } catch (err) {
        console.error('Failed to start CAT session:', err);
      } finally {
        if (mounted) setIsInitializing(false);
      }
    }

    initCat();

    return () => {
      mounted = false;
    };
  }, []);

  const handleAnswerChange = (_questionId: string, ans: LearnerAnswer) => {
    setCurrentAnswer(ans);
  };

  const handleSubmitStep = async () => {
    if (!sessionId || !currentQuestion || isSubmitting) return;

    const timeSpent = Math.max(1, Math.round((Date.now() - questionStartTimeRef.current) / 1000));
    setIsSubmitting(true);

    try {
      // Chuẩn hóa câu trả lời thành chuỗi
      const selectedStr = typeof currentAnswer === 'object' 
        ? JSON.stringify(currentAnswer) 
        : String(currentAnswer ?? '');

      const res = await learnerQuizService.respondCatSession(sessionId, {
        questionId: currentQuestion.id,
        selectedAnswer: selectedStr,
        timeSpentSeconds: timeSpent
      });

      if (res) {
        // Feedback tức thì
        const isCorrect = res.lastIsCorrect ?? false;
        setStepFeedback({
          isCorrect,
          explanation: res.lastExplanation || currentQuestion.aiExplanation
        });

        if (isCorrect) {
          confetti({ particleCount: 50, spread: 60, origin: { y: 0.8 } });
        }

        const newTheta = res.currentTheta ?? theta;
        const newSe = res.se ?? se;
        setTheta(newTheta);
        setSe(newSe);
        setThetaHistory(prev => [...prev, { step: prev.length, theta: newTheta, se: newSe }]);

        // Đợi 1.5 giây để học viên chiêm nghiệm kết quả trước khi sang câu kế tiếp
        setTimeout(() => {
          setStepFeedback(null);
          setCurrentAnswer({});
          questionStartTimeRef.current = Date.now();

          if (res.finished) {
            setIsFinished(true);
            setFinalReport(res.finalReport || res.report);
            confetti({ particleCount: 120, spread: 100, origin: { y: 0.5 } });
          } else {
            setCurrentQuestion(res.normalizedNextQuestion || res.nextQuestion);
            setAdministeredCount(res.administeredCount || administeredCount + 1);
          }
          setIsSubmitting(false);
        }, 1500);
      }
    } catch (err) {
      console.error('Failed to submit step:', err);
      setIsSubmitting(false);
    }
  };

  // Tính thanh tiến độ hội tụ sai số SE
  const convergencePercent = Math.min(100, Math.max(10, Math.round((1.0 - Math.min(1.0, se)) * 100)));

  if (isInitializing) {
    return (
      <div className="min-h-screen bg-slate-900 text-white flex flex-col items-center justify-center p-4">
        <div className="relative">
          <div className="w-20 h-20 rounded-full border-4 border-indigo-500/20 border-t-indigo-500 animate-spin" />
          <Brain className="w-8 h-8 text-indigo-400 absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 animate-pulse" />
        </div>
        <h2 className="mt-6 text-xl font-black text-slate-100 tracking-wide">
          Đang Khởi Tạo Động Cơ Thích Ứng CAT 3-PL...
        </h2>
        <p className="mt-2 text-sm text-slate-400 text-center max-w-md">
          Hệ thống đang nạp ngân hàng câu hỏi, tính toán hàm thông tin Fisher Information và chuẩn bị lộ trình khảo thí cá nhân hóa cho bạn.
        </p>
      </div>
    );
  }

  // MÀN HÌNH BÁO CÁO KẾT THÚC
  if (isFinished && finalReport) {
    return (
      <div className="min-h-screen bg-slate-950 text-white p-4 sm:p-8 flex flex-col items-center justify-center">
        <div className="max-w-2xl w-full bg-slate-900 border border-slate-800 rounded-3xl p-6 sm:p-10 shadow-2xl relative overflow-hidden">
          <div className="absolute top-0 right-0 -mt-10 -mr-10 w-48 h-48 bg-indigo-500/10 rounded-full blur-3xl pointer-events-none" />
          
          <div className="text-center mb-8">
            <span className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full bg-emerald-500/10 border border-emerald-500/30 text-emerald-400 text-xs font-black uppercase tracking-wider">
              <Award className="w-4 h-4" /> BÁO CÁO NĂNG LỰC THÍCH ỨNG CHUẨN QUỐC TẾ
            </span>
            <h1 className="mt-3 text-3xl sm:text-4xl font-black text-white">
              Đánh Giá Hoàn Tất! 🎉
            </h1>
            <p className="text-sm text-slate-400 mt-2">
              Động cơ CAT đã hội tụ chính xác năng lực thực tế của bạn với sai số chuẩn cực thấp.
            </p>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 sm:gap-4 mb-8">
            <div className="bg-slate-800/80 border border-slate-700/60 rounded-2xl p-4 text-center">
              <div className="text-xs font-bold text-slate-400 mb-1">Điểm Năng Lực (θ)</div>
              <div className="text-2xl sm:text-3xl font-black text-indigo-400">
                {finalReport.theta > 0 ? `+${finalReport.theta}` : finalReport.theta}
              </div>
            </div>
            <div className="bg-slate-800/80 border border-slate-700/60 rounded-2xl p-4 text-center">
              <div className="text-xs font-bold text-slate-400 mb-1">Xếp Hạng Phổ</div>
              <div className="text-2xl sm:text-3xl font-black text-amber-400">
                Top {Math.max(1, Math.round(100 - (finalReport.percentile || 50)))}%
              </div>
            </div>
            <div className="bg-slate-800/80 border border-slate-700/60 rounded-2xl p-4 text-center">
              <div className="text-xs font-bold text-slate-400 mb-1">Độ Chuẩn Xác</div>
              <div className="text-2xl sm:text-3xl font-black text-emerald-400">
                {finalReport.accuracy}%
              </div>
            </div>
            <div className="bg-slate-800/80 border border-slate-700/60 rounded-2xl p-4 text-center">
              <div className="text-xs font-bold text-slate-400 mb-1">Số Câu Cần Thi</div>
              <div className="text-2xl sm:text-3xl font-black text-cyan-400">
                {finalReport.n_items} câu
              </div>
            </div>
          </div>

          <div className="bg-slate-800/50 border border-slate-700/50 rounded-2xl p-4 sm:p-6 mb-8">
            <h3 className="text-sm font-bold text-slate-300 flex items-center gap-2 mb-2">
              <Brain className="w-4 h-4 text-indigo-400" /> Phân Loại Trình Độ: <span className="text-white font-extrabold text-base">{finalReport.level}</span>
            </h3>
            <p className="text-xs sm:text-sm text-slate-400 leading-relaxed">
              Lý do kết thúc: {finalReport.finish_reason}. Thay vì phải làm bài kiểm tra 100 câu cố định gây mệt mỏi, hệ thống đã xác định năng lực của bạn chỉ qua {finalReport.n_items} câu hỏi chọn lọc tối ưu.
            </p>
          </div>

          <div className="flex flex-col sm:flex-row gap-3">
            <button
              onClick={() => navigate('/')}
              className="flex-1 py-3.5 bg-indigo-600 hover:bg-indigo-500 text-white font-bold rounded-2xl shadow-lg shadow-indigo-600/30 transition flex items-center justify-center gap-2 cursor-pointer"
            >
              <Home className="w-4 h-4" /> Về Bảng Điều Khiển
            </button>
            <button
              onClick={() => window.location.reload()}
              className="py-3.5 px-6 bg-slate-800 hover:bg-slate-700 text-slate-200 font-bold rounded-2xl border border-slate-700 transition flex items-center justify-center gap-2 cursor-pointer"
            >
              <RotateCcw className="w-4 h-4" /> Thi Lại CAT
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col font-sans selection:bg-indigo-500 selection:text-white">
      {/* ── TOP HUD ADAPTIVE METRICS BAR ── */}
      <header className="sticky top-0 z-30 bg-slate-900/90 backdrop-blur-md border-b border-slate-800 px-4 py-3">
        <div className="max-w-6xl mx-auto flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-indigo-500/10 border border-indigo-500/30 flex items-center justify-center text-indigo-400">
              <Brain className="w-5 h-5 animate-pulse" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="text-sm font-black text-white">CAT ADAPTIVE TESTING</span>
                <span className="text-[10px] px-2 py-0.5 rounded-full bg-indigo-500/20 text-indigo-300 font-bold">
                  Mô hình IRT 3-PL
                </span>
              </div>
              <div className="text-xs text-slate-400">
                Câu {administeredCount} / Tối đa {maxItems}
              </div>
            </div>
          </div>

          {/* Dynamic Theta & SE meters */}
          <div className="flex items-center gap-4 sm:gap-6">
            <div className="text-right hidden sm:block">
              <div className="text-[10px] font-bold text-slate-400 uppercase">Ước Lượng Năng Lực (θ)</div>
              <div className="text-base font-black text-indigo-400">
                {theta > 0 ? `+${theta.toFixed(2)}` : theta.toFixed(2)}
              </div>
            </div>

            <div className="w-28 sm:w-36">
              <div className="flex justify-between text-[10px] font-bold text-slate-400 mb-1">
                <span>Độ Hội Tụ</span>
                <span>{convergencePercent}%</span>
              </div>
              <div className="w-full bg-slate-800 rounded-full h-2 overflow-hidden border border-slate-700/60">
                <div 
                  className="bg-gradient-to-r from-amber-500 via-indigo-500 to-emerald-400 h-full transition-all duration-500" 
                  style={{ width: `${convergencePercent}%` }}
                />
              </div>
            </div>

            <button
              onClick={() => {
                if (window.confirm('Bạn có chắc muốn tạm dừng phiên thi thích ứng và quay về trang chủ?')) {
                  navigate('/');
                }
              }}
              className="p-2 rounded-xl text-slate-400 hover:text-white hover:bg-slate-800 transition cursor-pointer"
              title="Thoát"
            >
              <Home className="w-5 h-5" />
            </button>
          </div>
        </div>
      </header>

      {/* ── MAIN QUIZ AREA ── */}
      <main className="flex-1 max-w-4xl w-full mx-auto p-4 sm:p-6 flex flex-col justify-center">
        {currentQuestion ? (
          <div className="relative">
            {/* Question Card tương tác 8 loại câu hỏi */}
            <QuestionCard
              question={currentQuestion}
              answer={currentAnswer}
              onAnswerChange={handleAnswerChange}
              t={(key) => key}
            />

            {/* Step Feedback Banner */}
            {stepFeedback && (
              <div className={`mt-4 p-4 rounded-2xl border flex items-start gap-3 animate-in fade-in slide-in-from-bottom-2 ${
                stepFeedback.isCorrect 
                  ? 'bg-emerald-950/40 border-emerald-500/40 text-emerald-200' 
                  : 'bg-rose-950/40 border-rose-500/40 text-rose-200'
              }`}>
                {stepFeedback.isCorrect ? (
                  <CheckCircle2 className="w-6 h-6 text-emerald-400 shrink-0 mt-0.5" />
                ) : (
                  <XCircle className="w-6 h-6 text-rose-400 shrink-0 mt-0.5" />
                )}
                <div>
                  <div className="text-sm font-black">
                    {stepFeedback.isCorrect ? 'Chính xác! Đang nâng độ khó để thử thách bạn...' : 'Chưa đúng! Hệ thống đang điều chỉnh câu hỏi phù hợp...'}
                  </div>
                  {stepFeedback.explanation && (
                    <div className="text-xs text-slate-300 mt-1 opacity-90">
                      💡 {stepFeedback.explanation}
                    </div>
                  )}
                </div>
              </div>
            )}

            {/* Next Action Bar */}
            <div className="mt-6 flex justify-end">
              <button
                onClick={handleSubmitStep}
                disabled={isSubmitting || Object.keys(currentAnswer).length === 0}
                className="px-8 py-3.5 bg-indigo-600 hover:bg-indigo-500 disabled:opacity-40 disabled:hover:bg-indigo-600 text-white font-black text-sm rounded-2xl shadow-xl shadow-indigo-600/25 transition-transform active:scale-95 flex items-center gap-2 cursor-pointer"
              >
                {isSubmitting ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" /> Đang tính toán thích ứng...
                  </>
                ) : (
                  <>
                    Xác Nhận & Tiếp Tục <ArrowRight className="w-4 h-4" />
                  </>
                )}
              </button>
            </div>
          </div>
        ) : (
          <div className="text-center p-8 bg-slate-900 rounded-3xl border border-slate-800">
            <AlertCircle className="w-12 h-12 text-amber-400 mx-auto mb-3" />
            <h3 className="text-lg font-bold">Không tìm thấy câu hỏi thích ứng tiếp theo</h3>
            <button
              onClick={() => navigate('/')}
              className="mt-4 px-6 py-2.5 bg-slate-800 hover:bg-slate-700 text-white font-bold rounded-xl text-sm"
            >
              Về Trang Chủ
            </button>
          </div>
        )}
      </main>
    </div>
  );
}
export default AdaptiveExamRoom;
