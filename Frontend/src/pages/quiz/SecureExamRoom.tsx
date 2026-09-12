import { useState, useEffect, useRef, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import { learnerQuizService } from '@/services/learner-quiz.service';
import { attemptService } from '@/services/attempt.service';
import type { LearnerQuestion, LearnerAnswer } from '@/types/quiz';
import { MathRenderer } from '@/components/common/MathRenderer';

interface UserAnswerRecord {
  questionId: string;
  content: string;
  userAnswer: string;
  correctAnswer: string;
  isCorrect: boolean;
}

const renderQuestionContent = (q: LearnerQuestion) => {
  const type = (q.contentType || q.questionMediaType || 'text').toLowerCase();
  const mediaUrl = q.questionMediaUrl || q.content;
  const hasSeparateText = q.questionMediaUrl && q.content;

  return (
    <div className="space-y-4">
      {(!q.questionMediaUrl || hasSeparateText) && (
        <MathRenderer content={q.content} className="text-lg text-gray-200" />
      )}
      
      {type === 'image' && !q.content?.includes('data:image/') && (
        <div className="my-3 flex justify-center">
          <img src={mediaUrl} className="max-h-64 object-contain rounded-2xl border border-gray-800 shadow-lg" alt="Question media" />
        </div>
      )}
      
      {type === 'audio' && (
        <div className="my-3 flex justify-center bg-gray-950/45 p-4 rounded-2xl border border-gray-800">
          <audio controls src={mediaUrl} className="w-full max-w-md" />
        </div>
      )}
      
      {type === 'video' && (
        <div className="my-3 flex justify-center bg-gray-950/45 p-2 rounded-2xl border border-gray-800">
          <video controls src={mediaUrl} className="max-h-80 w-full max-w-xl rounded-xl object-contain" />
        </div>
      )}
    </div>
  );
};

const renderOptionContent = (opt: string, type?: string) => {
  const mediaType = (type || 'text').toLowerCase();
  if (mediaType === 'audio') {
    return <audio controls src={opt} className="h-8 max-w-full inline-block my-1" onClick={(e) => e.stopPropagation()} />;
  }
  if (mediaType === 'video') {
    return <video controls src={opt} className="max-h-24 rounded-lg object-contain inline-block my-1" onClick={(e) => e.stopPropagation()} />;
  }
  return <MathRenderer content={opt} className="text-gray-200 font-medium" />;
};

export default function SecureExamRoom() {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const [examStarted, setExamStarted] = useState(false);
  const [questions, setQuestions] = useState<LearnerQuestion[]>([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [answers, setAnswers] = useState<Record<string, LearnerAnswer>>({});
  const [violationCount, setViolationCount] = useState(0);
  const [timeLeft, setTimeLeft] = useState(600);
  const [loading, setLoading] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  // [BONUS: Proctoring Hub] SignalR connection ref
  const hubRef = useRef<signalR.HubConnection | null>(null);
  const [hubWarning, setHubWarning] = useState<string | null>(null);
  const MAX_VIOLATIONS = 5; // Nâng lên 5 để khớp với server-side Hub

  // Lấy userId và examSessionId từ localStorage
  const userRaw = localStorage.getItem('user');
  const userId = userRaw ? (JSON.parse(userRaw) as { id?: string })?.id ?? 'guest' : 'guest';
  const examSessionId = id ?? 'exam-session-local';

  // [BONUS: Proctoring Hub] Kết nối SignalR khi bắt đầu thi
  useEffect(() => {
    if (!examStarted) return;

    const token = localStorage.getItem('token') ?? '';
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/proctoring', {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Nhận cảnh báo từ Giám thị
    connection.on('Warning', (data: { message: string; violationCount: number }) => {
      setHubWarning(data.message);
      setViolationCount(data.violationCount);
    });

    // Nhận lệnh hủy bài từ Giám thị
    connection.on('ExamTerminated', (data: { reason: string }) => {
      alert(`⚠️ BÀI THI BỊ HỦY: ${data.reason}`);
      if (document.fullscreenElement) document.exitFullscreen().catch(() => {});
      navigate('/');
    });

    connection.start()
      .then(() => {
        hubRef.current = connection;
        connection.invoke('JoinExam', examSessionId).catch(console.warn);
      })
      .catch((err) => console.warn('[Proctoring] SignalR connect failed (non-critical):', err));

    return () => {
      connection.stop();
      hubRef.current = null;
    };
  }, [examStarted, examSessionId, navigate]);

  useEffect(() => {
    if (!examStarted) return;
    setLoading(true);
    learnerQuizService.getQuestions()
      .then((data) => setQuestions(data.slice(0, 5)))
      .finally(() => setLoading(false));
  }, [examStarted]);

  useEffect(() => {
    if (!examStarted || timeLeft <= 0) return;
    const timer = setInterval(() => {
      setTimeLeft((prev) => {
        if (prev <= 1) { clearInterval(timer); handleFinishExam(); return 0; }
        return prev - 1;
      });
    }, 1000);
    return () => clearInterval(timer);
  }, [examStarted, timeLeft]);

  const handleFinishExam = useCallback(async () => {
    const recordList: UserAnswerRecord[] = questions.map((q) => {
      const ans = answers[q.id];
      const userAns = ans?.selectedOption ?? ans?.shortText ?? '';
      return {
        questionId: q.id,
        content: q.content,
        userAnswer: userAns,
        correctAnswer: q.answerRaw,
        isCorrect: userAns === q.answerRaw,
      };
    });

    localStorage.setItem('attempt_review_data', JSON.stringify({
      score: Math.round((recordList.filter((r) => r.isCorrect).length / Math.max(questions.length, 1)) * 100),
      correctCount: recordList.filter((r) => r.isCorrect).length,
      totalCount: questions.length,
      timeSpent: `${Math.floor((600 - timeLeft) / 60)} phút ${(600 - timeLeft) % 60} giây`,
      items: recordList,
    }));

    // Gửi từng câu lên DB
    for (const record of recordList) {
      try {
        await attemptService.submitAttempt(id ?? 'exam', {
          userId,
          questionId: record.questionId,
          category: 'Thi Sát Hạch',
          difficulty: 3,
          selectedAnswer: record.userAnswer,
          isCorrect: record.isCorrect,
          timeSpentSeconds: 30,
        });
      } catch { /* ignore */ }
    }

    if (document.fullscreenElement) document.exitFullscreen().catch(() => {});
    navigate('/quiz/review/latest');
  }, [questions, answers, timeLeft, navigate, id, userId]);

  const handleViolation = useCallback((violationType = 'tab_switch') => {
    setViolationCount((prev) => {
      const next = prev + 1;
      // Báo cáo lên Server qua SignalR Hub (human-in-the-loop)
      if (hubRef.current?.state === signalR.HubConnectionState.Connected) {
        hubRef.current
          .invoke('ReportSuspiciousActivity', examSessionId, violationType)
          .catch(console.warn);
      }
      // Client-side auto-submit sau MAX_VIOLATIONS (backup nếu Hub offline)
      if (next >= MAX_VIOLATIONS) {
        setTimeout(() => handleFinishExam(), 500);
      }
      return next;
    });
  }, [handleFinishExam, examSessionId]);

  useEffect(() => {
    if (!examStarted) return;
    const onVisibility = () => { if (document.hidden) handleViolation('tab_switch'); };
    const onBlur = () => handleViolation('window_blur');
    const onFs = () => { if (!document.fullscreenElement) handleViolation('fullscreen_exit'); };
    const onContext = (e: MouseEvent) => { e.preventDefault(); handleViolation('context_menu'); };
    document.addEventListener('visibilitychange', onVisibility);
    window.addEventListener('blur', onBlur);
    document.addEventListener('fullscreenchange', onFs);
    document.addEventListener('contextmenu', onContext);
    return () => {
      document.removeEventListener('visibilitychange', onVisibility);
      window.removeEventListener('blur', onBlur);
      document.removeEventListener('fullscreenchange', onFs);
      document.removeEventListener('contextmenu', onContext);
    };
  }, [examStarted, handleViolation]);

  const startExam = async () => {
    try {
      if (containerRef.current) {
        await containerRef.current.requestFullscreen();
        setExamStarted(true);
      }
    } catch {
      alert('Bạn phải đồng ý chế độ Toàn màn hình để khởi động phòng thi bảo mật.');
    }
  };

  const fmt = (s: number) => `${Math.floor(s / 60).toString().padStart(2, '0')}:${(s % 60).toString().padStart(2, '0')}`;

  if (!examStarted) {
    return (
      <div className="min-h-screen bg-gray-950 text-white flex items-center justify-center p-8">
        <div className="max-w-md w-full bg-gray-900 border border-red-900/30 rounded-3xl p-8 text-center space-y-6">
          <div className="w-16 h-16 bg-red-950/40 border border-red-500/20 text-red-500 rounded-full flex items-center justify-center text-3xl mx-auto animate-pulse">⚠️</div>
          <div className="space-y-2">
            <h1 className="text-2xl font-black text-red-400">PHÒNG THI GIÁM SÁT CAO</h1>
            <p className="text-sm text-gray-400">Quy chế thi chặt chẽ, tự động khóa màn hình và chống gian lận.</p>
          </div>
          <div className="bg-gray-950/60 p-4 rounded-2xl border border-gray-800 text-left text-xs text-gray-400 space-y-2">
            <p>🔴 <strong>Fullscreen Enforced:</strong> Hệ thống sẽ khóa toàn màn hình trong suốt thời gian thi.</p>
            <p>🔴 <strong>Tab Switch Monitoring:</strong> Không được phép chuyển tab, Alt+Tab.</p>
            <p>🔴 <strong>Auto-Lockout:</strong> Vi phạm quá <strong>{MAX_VIOLATIONS} lần</strong>, hệ thống tự động nộp bài.</p>
          </div>
          <div ref={containerRef} className="flex gap-4">
            <button
              onClick={startExam}
              className="flex-1 bg-red-600 hover:bg-red-500 text-white font-bold py-3.5 rounded-xl transition-all"
            >
              Bắt đầu & Khóa màn hình
            </button>
            <button
              onClick={() => navigate('/')}
              className="px-6 py-3.5 bg-gray-800 hover:bg-gray-700 rounded-xl font-bold transition-colors"
            >
              Trở lại
            </button>
          </div>
        </div>
      </div>
    );
  }

  if (loading || questions.length === 0) {
    return (
      <div className="min-h-screen bg-gray-950 flex items-center justify-center text-white">
        <div className="animate-spin text-5xl mb-4">🌀</div>
      </div>
    );
  }

  const currentQ = questions[currentIndex];

  return (
    <div ref={containerRef} className="min-h-screen bg-gray-950 text-white flex flex-col select-none">
      <header className="bg-gray-900 border-b border-gray-800 px-6 h-16 flex items-center justify-between sticky top-0 z-10">
        <span className="text-red-500 font-extrabold animate-pulse">🔴 ĐANG GIÁM SÁT</span>
        {violationCount > 0 && (
          <div className="bg-red-950/60 border border-red-500/30 text-red-400 px-4 py-1.5 rounded-full text-xs font-bold animate-bounce">
            ⚠️ Vi phạm: {violationCount}/{MAX_VIOLATIONS}
          </div>
        )}
        <div className="flex items-center gap-4">
          <div className="bg-gray-950 border border-gray-800 text-orange-400 px-4 py-1.5 rounded-full font-mono text-sm font-bold">⏱️ {fmt(timeLeft)}</div>
          <button onClick={handleFinishExam} className="bg-red-600 hover:bg-red-500 text-white px-5 py-2 rounded-xl text-sm font-bold">Nộp bài</button>
        </div>
      </header>

      <div className="flex-1 flex flex-col md:flex-row max-w-6xl mx-auto w-full p-6 gap-6">
        <main className="flex-1 bg-gray-900 border border-gray-800 rounded-3xl p-8 flex flex-col justify-between shadow-2xl">
          <div>
            <div className="flex justify-between text-xs text-gray-500 mb-6">
              <span>MÔN: {currentQ.topicCode ?? currentQ.topicName ?? 'Chung'}</span>
              <span>ĐỘ KHÓ: {currentQ.difficulty}</span>
            </div>
            <div className="text-2xl font-bold mb-8 text-gray-100 flex flex-col gap-2">
              <span className="text-sm text-red-500 font-extrabold uppercase tracking-wide">Câu hỏi {currentIndex + 1}</span>
              {renderQuestionContent(currentQ)}
            </div>
            <div className="grid gap-3">
              {(currentQ.options ?? []).filter((opt) => (opt ?? '').trim().length > 0).map((opt, i) => {
                const alpha = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J'];
                const isSelected = answers[currentQ.id]?.selectedOption === String(i + 1);
                return (
                  <button
                    key={i}
                    onClick={() => setAnswers({ ...answers, [currentQ.id]: { selectedOption: String(i + 1) } })}
                    className={`w-full text-left px-6 py-4 border rounded-2xl text-sm transition-all flex items-center gap-3 ${isSelected ? 'bg-red-600/10 border-red-500 text-red-400 font-bold' : 'bg-gray-950 border-gray-800 hover:border-gray-700 text-gray-300'}`}
                  >
                    <span className="font-bold shrink-0">{alpha[i] ?? String(i + 1)}.</span>
                    <div className="flex-1">{renderOptionContent(opt, currentQ.optionType || currentQ.optionMediaType)}</div>
                  </button>
                );
              })}
            </div>
          </div>

          <div className="flex justify-between items-center pt-8 border-t border-gray-800 mt-8">
            <button
              onClick={() => setCurrentIndex((p) => Math.max(0, p - 1))}
              disabled={currentIndex === 0}
              className="px-5 py-2 bg-gray-850 border border-gray-800 rounded-xl hover:bg-gray-800 disabled:opacity-30 font-bold"
            >◀ Câu trước</button>
            <span className="text-xs text-gray-500">Tự động lưu</span>
            <button
              onClick={() => setCurrentIndex((p) => Math.min(questions.length - 1, p + 1))}
              disabled={currentIndex === questions.length - 1}
              className="px-5 py-2 bg-red-600 rounded-xl hover:bg-red-500 disabled:opacity-30 font-bold"
            >Câu tiếp ▶</button>
          </div>
        </main>

        <aside className="w-full md:w-64 bg-gray-900 border border-gray-800 rounded-3xl p-6 h-fit">
          <h3 className="text-sm font-bold text-gray-400 mb-4 flex justify-between">
            Danh sách câu
            <span className="bg-gray-800 px-2 py-0.5 rounded text-xs">{Object.keys(answers).length}/{questions.length}</span>
          </h3>
          <div className="grid grid-cols-4 gap-2">
            {questions.map((q, i) => {
              const isAnswered = !!answers[q.id];
              return (
                <button
                  key={q.id}
                  onClick={() => setCurrentIndex(i)}
                  className={`aspect-square rounded-xl text-sm font-bold flex items-center justify-center transition-all ${
                    i === currentIndex ? 'ring-2 ring-red-500 bg-red-600 text-white' : isAnswered ? 'bg-green-600/20 text-green-400 border border-green-500/20' : 'bg-gray-950 text-gray-500 border border-gray-800'
                  }`}
                >{i + 1}</button>
              );
            })}
          </div>
        </aside>
      </div>
    </div>
  );
}
