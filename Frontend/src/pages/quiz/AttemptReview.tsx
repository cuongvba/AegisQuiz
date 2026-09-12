import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Zap, ArrowLeft, Target, RefreshCw, Clock, Loader2 } from 'lucide-react';
import confetti from 'canvas-confetti';

import { attemptService } from '@/services/attempt.service';
import { MathRenderer } from '@/components/common/MathRenderer';

interface ResultItem {
  id: string;
  content: string;
  userAnswer: string;
  correctAnswer: string;
  isCorrect: boolean;
  explanation?: string;
}

interface Insight {
  topic: string;
  strength: string;
}

interface AttemptResult {
  score: number;
  correctCount: number;
  totalCount: number;
  timeSpent: string;
  insights?: Insight[];
  items: ResultItem[];
}

export function AttemptReview() {
  const { quizId, attemptId } = useParams<{ quizId: string; attemptId: string }>();
  const navigate = useNavigate();

  const [aiAnalysis, setAiAnalysis] = useState<string | null>(null);
  const [analyzing, setAnalyzing] = useState(false);
  const [showExplanationId, setShowExplanationId] = useState<string | null>(null);

  const { data: realResult, isLoading } = useQuery({
    queryKey: ['attempt-answers', quizId, attemptId],
    queryFn: () => attemptService.getAttemptAnswers(quizId!, attemptId!),
    enabled: !!quizId && !!attemptId,
  });

  if (isLoading) {
    return (
      <div className="min-h-screen bg-slate-50 flex items-center justify-center flex-col gap-4">
        <Loader2 size={40} className="text-indigo-600 animate-spin" />
        <h2 className="text-xl font-bold text-slate-700">Đang chấm điểm...</h2>
      </div>
    );
  }

  // MOCK DATA fallback
  const result: AttemptResult = realResult || {
    score: 85,
    correctCount: 8,
    totalCount: 10,
    timeSpent: '12:45',
    insights: [
      { topic: 'React Hooks', strength: 'Strong' },
      { topic: 'State Management', strength: 'Needs Review' }
    ],
    items: Array.from({ length: 10 }).map((_, i) => ({
      id: `q${i + 1}`,
      content: `Câu hỏi số ${i + 1}: Về vòng đời component React...`,
      userAnswer: 'D',
      correctAnswer: i % 3 === 0 ? 'C' : 'D',
      isCorrect: i % 3 !== 0,
      explanation: 'Đây là giải thích mặc định của câu hỏi.',
    }))
  };

  const handleAiAnalysis = async () => {
    setAnalyzing(true);
    // const res = await aiService.analyzeAttempt("quiz1", attemptId!);
    // Simulate AI response
    setTimeout(() => {
      setAiAnalysis("AI nhận xét: Tuyệt vời! Bạn nắm rất chắc về React Hook. Tuy nhiên phần Context API còn hơi hổng. Cần ôn tập thêm về Reducer nhé! ✨");
      setAnalyzing(false);
      confetti({ particleCount: 100, spread: 70, origin: { y: 0.6 } });
    }, 1500);
  };

  const toggleExplanation = (qId: string) => {
    setShowExplanationId(showExplanationId === qId ? null : qId);
  };

  return (
    <div className="min-h-screen bg-slate-50 font-sans pb-20">
      {/* Header */}
      <header className="bg-indigo-600 border-b border-indigo-700 sticky top-0 z-10 px-4 py-3 text-white shadow-md">
        <div className="max-w-4xl mx-auto w-full flex items-center justify-between">
          <button 
            onClick={() => navigate('/')} 
            className="text-indigo-200 hover:text-white font-medium text-sm flex items-center gap-1 transition-colors"
          >
            <ArrowLeft size={16} /> Quay lại Dashboard
          </button>
          <h1 className="font-bold text-lg">Báo cáo kết quả {attemptId}</h1>
          <div className="w-24"></div> {/* spacer for centering */}
        </div>
      </header>

      <main className="max-w-4xl mx-auto p-4 space-y-6 mt-6">
        
        {/* Score Card */}
        <section className="bg-white rounded-3xl p-8 shadow-sm border border-slate-200 flex flex-col md:flex-row items-center gap-8 justify-between">
          <div className="flex items-center gap-6">
            <div className="relative">
              <svg className="w-32 h-32 transform -rotate-90">
                  <circle cx="64" cy="64" r="56" stroke="currentColor" strokeWidth="12" fill="transparent" className="text-slate-100" />
<circle cx="64" cy="64" r="56" stroke="currentColor" strokeWidth="12" fill="transparent" strokeDasharray={351.858} strokeDashoffset={351.858 - (351.858 * result.score) / 100} className="text-emerald-500" />
                </svg>
                <div className="absolute inset-0 flex items-center justify-center flex-col">
                  <span className="text-3xl font-black text-slate-800">{result.score}</span>
                  <span className="text-xs font-bold text-slate-500">Điểm</span>
                </div>
              </div>

              <div>
                <h2 className="text-2xl font-bold text-slate-900 mb-1">Hoàn thành xuất sắc! 🎉</h2>
                <p className="text-slate-500 font-medium">Bạn đã trả lời đúng {result.correctCount}/{result.totalCount}</p>
                <p className="text-slate-400 text-sm mt-1 flex items-center gap-1">
                  <Clock size={14} /> Thời gian làm bài: {result.timeSpent}
                </p>
              </div>
            </div>
  
            <div className="flex flex-col gap-3 min-w-[200px]">
              {(result.insights ?? []).map((ins: {topic: string; strength: string}, idx: number) => (
              <div key={idx} className="bg-slate-50 rounded-xl p-3 flex justify-between items-center border border-slate-100">
                <span className="text-sm font-semibold text-slate-700">{ins.topic}</span>
                <span className={`text-xs font-bold px-2 py-1 rounded-md ${ins.strength === 'Strong' ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'}`}>{ins.strength}</span>
              </div>
            ))}
          </div>
        </section>

        {/* AI Action Panel */}
        <section className="bg-gradient-to-r from-violet-600 to-indigo-600 rounded-3xl p-8 shadow-lg text-white text-center pb-8 pt-8">
          <h3 className="text-xl font-bold mb-2 flex items-center justify-center gap-2">
            <Zap size={24} className="text-amber-300" fill="currentColor"/> 
            Muốn hiểu rõ hơn về năng lực?
          </h3>
          <p className="text-indigo-100 mb-6 font-medium max-w-lg mx-auto">Hệ thống AI Omni Core sẽ phân tích toàn bộ đáp án của bạn và đưa ra lời khuyên lộ trình tối ưu nhất.</p>
          
          {!aiAnalysis && (
            <button 
              onClick={handleAiAnalysis}
              disabled={analyzing}
              className="bg-white text-indigo-700 hover:bg-slate-50 font-black px-6 py-3 rounded-xl shadow-lg transition-transform hover:-translate-y-1 flex items-center justify-center mx-auto gap-2 text-lg disabled:opacity-80"
            >
              {analyzing ? <RefreshCw size={20} className="animate-spin" /> : '🪄 Nhờ AI Phân tích bài làm'}
            </button>
          )}

          {aiAnalysis && (
            <div className="bg-white/10 backdrop-blur-sm border border-white/20 rounded-2xl p-6 text-left shadow-inner animate-in fade-in slide-in-from-bottom-4">
              <p className="text-lg leading-relaxed">{aiAnalysis}</p>
            </div>
          )}
        </section>

        {/* Question Details */}
        <section className="space-y-4 pt-4">
          <h3 className="text-xl font-bold text-slate-900 border-b border-slate-200 pb-2 mb-6">Chi tiết từng câu</h3>
          
          {result.items.map((item, idx) => (
            <div key={item.id} className={`bg-white rounded-2xl p-6 border-2 transition-all ${item.isCorrect ? 'border-transparent shadow-sm' : 'border-rose-200 shadow-md bg-rose-50/30'}`}>
              <div className="flex justify-between items-start mb-4">
                <span className={`px-3 py-1 text-sm font-bold rounded-lg ${item.isCorrect ? 'bg-emerald-100 text-emerald-700' : 'bg-rose-100 text-rose-700'}`}>
                  Câu {idx + 1} • {item.isCorrect ? 'Đúng' : 'Sai'}
                </span>
                
                {!item.isCorrect && (
                  <button 
                    onClick={() => toggleExplanation(item.id)}
                    className="text-xs font-bold bg-amber-100 text-amber-700 px-3 py-1.5 rounded-lg hover:bg-amber-200 transition-colors flex items-center gap-1"
                  >
                    <Target size={14} /> 💡 AI Giải thích
                  </button>
                )}
              </div>
              
              <div className="text-slate-800 font-medium mb-4 leading-relaxed">
                <MathRenderer content={item.content} />
              </div>
              
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mt-4 bg-slate-50 p-4 rounded-xl border border-slate-100">
                <div className="flex items-center gap-2">
                  <span className="text-xs font-bold text-slate-400 uppercase tracking-wider">Của bạn</span>
                  <span className={`font-bold ${item.isCorrect ? 'text-emerald-600' : 'text-rose-600'}`}>{item.userAnswer}</span>
                </div>
                <div className="flex items-center gap-2">
                  <span className="text-xs font-bold text-slate-400 uppercase tracking-wider">Đáp án chuẩn</span>
                  <span className="font-bold text-slate-800">{item.correctAnswer}</span>
                </div>
              </div>

              {/* Toggling AI Explanation */}
              {showExplanationId === item.id && (
                <div className="mt-4 bg-indigo-50 border border-indigo-100 p-5 rounded-xl animate-in fade-in slide-in-from-top-2">
                  <div className="text-indigo-900 text-sm font-medium leading-relaxed">
                    <span className="font-bold mb-2 flex items-center gap-2"><Zap size={16} fill="currentColor" className="text-indigo-600" /> Omni AI Giải thích chi tiết:</span>
                    <MathRenderer content={item.explanation || 'Chưa có giải thích chi tiết cho câu hỏi này.'} />
                  </div>
                </div>
              )}
            </div>
          ))}
        </section>

      </main>
    </div>
  );
}
