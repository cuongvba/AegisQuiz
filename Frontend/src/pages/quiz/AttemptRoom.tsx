import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Clock, Send, ChevronLeft, ChevronRight, Save, Loader2 } from 'lucide-react';

import { attemptService } from '@/services/attempt.service';
import { learnerQuizService } from '@/services/learner-quiz.service';
import type { LearnerQuestion } from '@/types/quiz';
import { QuestionCard } from '@/components/quiz/QuestionCard';

export function AttemptRoom() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  // Fetch from bank
  const { data: fetchedQuestions, isLoading } = useQuery({
    queryKey: ['questions-bank', id],
    queryFn: () => learnerQuizService.getQuestions(),
  });

  const questions: LearnerQuestion[] = (fetchedQuestions && fetchedQuestions.length > 0)
    ? fetchedQuestions.slice(0, 20)
    : [];

  const [currentIndex, setCurrentIndex] = useState(0);
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [timeLeft, setTimeLeft] = useState(1800); // 30 minutes

  // Countdown Timer
  useEffect(() => {
    const timer = setInterval(() => {
      setTimeLeft((prev) => (prev > 0 ? prev - 1 : 0));
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  const formatTime = (seconds: number) => {
    const m = Math.floor(seconds / 60).toString().padStart(2, '0');
    const s = (seconds % 60).toString().padStart(2, '0');
    return `${m}:${s}`;
  };

  const handleSelectOption = (qId: string, option: string) => {
    const newAnswers = { ...answers, [qId]: option };
    setAnswers(newAnswers);
    // Auto-save progress to local storage
    localStorage.setItem(`attempt_draft_${id}`, JSON.stringify(newAnswers));
  };

  // Nộp bài
  const submitMutation = useMutation({
    mutationFn: async () => {
      // Gọi service API thật thay vì mock
      return attemptService.submitAttempt(id!, { answers });
    },
    onSuccess: (data: { attemptId?: string; id?: string }) => {
      localStorage.removeItem(`attempt_draft_${id}`);
      navigate(`/quiz/${id}/review/${data.attemptId || data.id || 'recent'}`);
    },
  });

  if (isLoading) {
    return (
      <div className="min-h-screen bg-slate-50 flex items-center justify-center flex-col gap-4">
        <Loader2 size={40} className="text-indigo-600 animate-spin" />
        <h2 className="text-xl font-bold text-slate-700">Đang tải đề thi...</h2>
      </div>
    );
  }

  const currentQ = (questions[currentIndex] as LearnerQuestion) || null;

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col font-sans">
      {/* Header */}
      <header className="bg-white border-b border-slate-200 sticky top-0 z-10 px-4 h-16 flex items-center justify-between shadow-sm">
        <div className="flex items-center gap-4">
          <button 
            onClick={() => {
              if (window.confirm('Bạn có chắc muốn thoát phòng thi? Tiến trình làm bài sẽ được lưu tạm.')) {
                navigate('/');
              }
            }} 
            className="text-slate-500 hover:text-slate-700 font-medium text-sm flex items-center gap-1 cursor-pointer"
          >
            <ChevronLeft size={16} /> Thoát
          </button>
          <div className="border-l border-slate-200 pl-4">
            <h1 className="font-bold text-slate-800 text-sm sm:text-base">Bài Thi Đánh Giá Năng Lực</h1>
            <span className="text-xs text-slate-400">Đa phương thức 8 dạng câu hỏi</span>
          </div>
        </div>
        
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2 px-3 py-1.5 bg-rose-50 text-rose-600 rounded-full font-bold text-sm">
            <Clock size={16} />
            {formatTime(timeLeft)}
          </div>
          <button 
            onClick={() => {
              if (window.confirm('Bạn có chắc chắn muốn nộp bài thi ngay bây giờ?')) {
                submitMutation.mutate();
              }
            }}
            disabled={submitMutation.isPending}
            className="flex items-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white px-5 py-2 rounded-xl font-bold text-sm shadow-md shadow-indigo-600/20 transition-transform active:scale-95 disabled:opacity-50 cursor-pointer"
          >
            <Send size={16} />
            {submitMutation.isPending ? 'Đang nộp...' : 'Nộp bài'}
          </button>
        </div>
      </header>

      <div className="flex-1 flex flex-col md:flex-row max-w-7xl mx-auto w-full p-4 gap-6">
        
        {/* Main Question Area với QuestionCard hoàn chỉnh */}
        <main className="flex-1 flex flex-col">
          {currentQ ? (
            <div className="flex-1">
              <QuestionCard
                question={currentQ}
                answer={{
                  selectedOption: answers[currentQ.id],
                  shortText: answers[currentQ.id]
                }}
                onAnswerChange={(qId, ans) => {
                  const val = ans.selectedOption || ans.shortText || '';
                  handleSelectOption(qId, val);
                }}
                t={(k) => k}
              />
            </div>
          ) : (
            <div className="p-8 text-center bg-white rounded-2xl border border-slate-200">
              <p className="text-slate-500">Không có câu hỏi.</p>
            </div>
          )}

          {/* Footer Controls */}
          <div className="mt-4 bg-white border border-slate-200 p-4 rounded-2xl flex items-center justify-between shadow-sm">
            <button 
              onClick={() => setCurrentIndex(prev => Math.max(0, prev - 1))}
              disabled={currentIndex === 0}
              className="px-4 py-2 text-slate-600 font-medium hover:bg-slate-100 disabled:opacity-40 disabled:hover:bg-transparent rounded-xl flex items-center gap-1 transition-colors cursor-pointer"
            >
              <ChevronLeft size={20} /> Trước
            </button>

            <span className="text-sm font-medium text-slate-500 flex items-center gap-1">
              <Save size={14} className="text-emerald-500" /> Tự động lưu
            </span>

            <button 
              onClick={() => setCurrentIndex(prev => Math.min(questions.length - 1, prev + 1))}
              disabled={currentIndex === questions.length - 1}
              className="px-5 py-2 bg-slate-900 text-white font-medium hover:bg-slate-800 disabled:opacity-40 rounded-xl flex items-center gap-1 transition-colors shadow-sm cursor-pointer"
            >
              Tiếp <ChevronRight size={20} />
            </button>
          </div>
        </main>

        {/* Sidebar Navigator */}
        <aside className="w-full md:w-80 shrink-0 flex flex-col gap-4">
          <div className="bg-white rounded-2xl shadow-sm border border-slate-200 p-5">
            <h3 className="font-bold text-slate-800 mb-4 flex items-center justify-between">
              Bảng câu hỏi
              <span className="text-xs font-semibold px-2 py-1 bg-indigo-100 text-indigo-700 rounded-md">
                {Object.keys(answers).length} / {questions.length}
              </span>
            </h3>
            
            <div className="grid grid-cols-5 gap-2">
              {questions.map((q, i) => {
                const isAnswered = !!answers[q.id];
                const isActive = i === currentIndex;
                
                return (
                  <button
                    key={q.id}
                    onClick={() => setCurrentIndex(i)}
                    className={`aspect-square rounded-xl font-bold text-sm flex items-center justify-center transition-all cursor-pointer ${
                      isActive 
                        ? 'ring-2 ring-indigo-600 ring-offset-2 bg-indigo-600 text-white' 
                        : isAnswered
                          ? 'bg-emerald-100 text-emerald-700 border border-emerald-200'
                          : 'bg-slate-100 text-slate-500 border border-slate-200 hover:bg-slate-200'
                    }`}
                  >
                    {i + 1}
                  </button>
                );
              })}
            </div>
          </div>
        </aside>

      </div>
    </div>
  );
}