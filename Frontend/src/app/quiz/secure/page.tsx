'use client';
import { useState, useEffect, useRef, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';

interface Question {
  id: string;
  content: string;
  difficulty: number;
  durationSeconds: number;
  categoryCode: string;
  options: string[];
  correctOption: string;
}

interface UserAnswerRecord {
  questionId: string;
  content: string;
  userAnswer: string;
  correctAnswer: string;
  isCorrect: boolean;
}

export default function SecureExamRoom() {
  const router = useRouter();
  const [examStarted, setExamStarted] = useState(false);
  const [questions, setQuestions] = useState<Question[]>([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [violationCount, setViolationCount] = useState(0);
  const [timeLeft, setTimeLeft] = useState(600); // 10 minutes
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [loading, setLoading] = useState(false);

  const containerRef = useRef<HTMLDivElement>(null);
  const MAX_VIOLATIONS = 3;
  const userId = '9b0a1a1c-a19b-4e12-8822-42171a82ea3c';

  // Tải câu hỏi từ API
  useEffect(() => {
    if (examStarted) {
      setLoading(true);
      fetch('http://localhost:8080/api/quiz/questions?shuffleQuestions=false&shuffleAnswers=false')
        .then(res => res.json())
        .then(data => {
          setQuestions(data.slice(0, 5)); // Lấy 5 câu hỏi đầu để làm bài thi
          setLoading(false);
        })
        .catch(err => {
          console.error(err);
          setLoading(false);
        });
    }
  }, [examStarted]);

  // Bộ đếm thời gian
  useEffect(() => {
    if (!examStarted || timeLeft <= 0) return;
    const timer = setInterval(() => {
      setTimeLeft(prev => {
        if (prev <= 1) {
          clearInterval(timer);
          handleFinishExam();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
    return () => clearInterval(timer);
  }, [examStarted, timeLeft]);

  // Giải quyết nộp bài thi
  const handleFinishExam = useCallback(async () => {
    // Thu thập kết quả
    const recordList: UserAnswerRecord[] = questions.map(q => {
      const uAns = answers[q.id] || '';
      return {
        questionId: q.id,
        content: q.content,
        userAnswer: uAns,
        correctAnswer: q.correctOption,
        isCorrect: uAns === q.correctOption
      };
    });

    // Lưu vào LocalStorage để trang Review lấy
    localStorage.setItem('attempt_review_data', JSON.stringify({
      score: Math.round((recordList.filter(r => r.isCorrect).length / questions.length) * 100),
      correctCount: recordList.filter(r => r.isCorrect).length,
      totalCount: questions.length,
      timeSpent: `${Math.floor((600 - timeLeft) / 60)} phút ${(600 - timeLeft) % 60} giây`,
      items: recordList
    }));

    // Gửi từng nỗ lực lưu xuống DB Postgres
    for (const record of recordList) {
      try {
        await fetch('http://localhost:8080/api/quiz/attempt', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            userId,
            questionId: record.questionId,
            category: 'Thi Sát Hạch',
            difficulty: 3,
            selectedAnswer: record.userAnswer,
            isCorrect: record.isCorrect,
            timeSpentSeconds: 30
          })
        });
      } catch (e) {
        console.error(e);
      }
    }

    // Thoát khỏi toàn màn hình
    if (document.fullscreenElement) {
      document.exitFullscreen().catch(err => console.error(err));
    }

    router.push('/quiz/review/latest');
  }, [questions, answers, timeLeft, router]);

  // Xử lý vi phạm quy chế thi
  const handleViolation = useCallback(() => {
    setViolationCount(prev => {
      const nextVal = prev + 1;
      if (nextVal >= MAX_VIOLATIONS) {
        alert("BẠN ĐÃ VI PHẠM QUY CHẾ THI QUÁ 3 LẦN. HỆ THỐNG TỰ ĐỘNG THU BÀI!");
        // Thực hiện tự động nộp bài
        setTimeout(() => {
          handleFinishExam();
        }, 500);
      }
      return nextVal;
    });
  }, [handleFinishExam]);

  // Đăng ký giám sát trình duyệt
  useEffect(() => {
    if (!examStarted) return;

    const handleVisibilityChange = () => {
      if (document.hidden) {
        handleViolation();
      }
    };

    const handleBlur = () => {
      handleViolation();
    };

    const handleFullscreenChange = () => {
      setIsFullscreen(!!document.fullscreenElement);
      if (!document.fullscreenElement && examStarted) {
        handleViolation();
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);
    window.addEventListener('blur', handleBlur);
    document.addEventListener('fullscreenchange', handleFullscreenChange);

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      window.removeEventListener('blur', handleBlur);
      document.removeEventListener('fullscreenchange', handleFullscreenChange);
    };
  }, [examStarted, handleViolation]);

  const startExam = async () => {
    try {
      if (containerRef.current) {
        await containerRef.current.requestFullscreen();
        setExamStarted(true);
        setIsFullscreen(true);
      }
    } catch (err) {
      alert('Bạn phải đồng ý chế độ Toàn màn hình để khởi động phòng thi bảo mật.');
    }
  };

  const formatTime = (sec: number) => {
    const m = Math.floor(sec / 60).toString().padStart(2, '0');
    const s = (sec % 60).toString().padStart(2, '0');
    return `${m}:${s}`;
  };

  if (!examStarted) {
    return (
      <div className="min-h-screen bg-gray-950 text-white flex flex-col items-center justify-center p-8">
        <div className="max-w-md w-full bg-gray-900 border border-red-900/30 rounded-3xl p-8 shadow-2xl text-center space-y-6">
          <div className="w-16 h-16 bg-red-950/40 border border-red-500/20 text-red-500 rounded-full flex items-center justify-center text-3xl mx-auto animate-pulse">
            ⚠️
          </div>
          <div className="space-y-2">
            <h1 className="text-2xl font-black text-red-400">PHÒNG THI GIÁM SÁT CAO</h1>
            <p className="text-sm text-gray-400">
              Quy chế thi chặt chẽ, tự động khóa màn hình và chống gian lận.
            </p>
          </div>
          <div className="bg-gray-950/60 p-4 rounded-2xl border border-gray-800 text-left text-xs text-gray-400 space-y-2">
            <p>🔴 **Fullscreen Enforced:** Hệ thống sẽ khóa toàn màn hình trong suốt thời gian thi.</p>
            <p>🔴 **Tab Switch Monitoring:** Không được phép chuyển tab, chuyển ứng dụng (Alt + Tab).</p>
            <p>🔴 **Auto-Lockout:** Vi phạm quá **3 lần**, hệ thống tự động nộp bài và thu hồi bài làm.</p>
          </div>
          <div ref={containerRef} className="flex gap-4">
            <button 
              onClick={startExam}
              className="flex-1 bg-red-600 hover:bg-red-500 text-white font-bold py-3.5 rounded-xl transition-all"
            >
              Bắt đầu & Khóa màn hình
            </button>
            <Link href="/" className="px-6 py-3.5 bg-gray-800 hover:bg-gray-700 rounded-xl font-bold transition-colors">
              Trở lại
            </Link>
          </div>
        </div>
      </div>
    );
  }

  if (loading || questions.length === 0) {
    return (
      <div className="min-h-screen bg-gray-950 flex flex-col items-center justify-center text-white">
        <div className="animate-spin text-5xl mb-4">🌀</div>
        <p className="text-gray-400">Đang khởi tạo đề thi bảo mật...</p>
      </div>
    );
  }

  const currentQ = questions[currentIndex];

  return (
    <div ref={containerRef} className="min-h-screen bg-gray-950 text-white flex flex-col font-sans select-none">
      {/* Header */}
      <header className="bg-gray-900 border-b border-gray-800 px-6 h-16 flex items-center justify-between sticky top-0 z-10">
        <div className="flex items-center gap-4">
          <span className="text-red-500 font-extrabold flex items-center gap-2 animate-pulse">
            🔴 ĐANG GIÁM SÁT (PROCTORED)
          </span>
        </div>

        {/* Cảnh báo vi phạm */}
        {violationCount > 0 && (
          <div className="bg-red-950/60 border border-red-500/30 text-red-400 px-4 py-1.5 rounded-full text-xs font-bold animate-bounce">
            ⚠️ Phát hiện chuyển Tab: vi phạm {violationCount}/{MAX_VIOLATIONS} lần
          </div>
        )}

        <div className="flex items-center gap-4">
          <div className="bg-gray-950 border border-gray-800 text-orange-400 px-4 py-1.5 rounded-full font-mono text-sm font-bold">
            ⏱️ {formatTime(timeLeft)}
          </div>
          <button 
            onClick={handleFinishExam}
            className="bg-red-600 hover:bg-red-500 text-white px-5 py-2 rounded-xl text-sm font-bold transition-colors"
          >
            Nộp bài thi
          </button>
        </div>
      </header>

      {/* Main Body */}
      <div className="flex-1 flex flex-col md:flex-row max-w-6xl mx-auto w-full p-6 gap-6 overflow-hidden">
        {/* Question Panel */}
        <main className="flex-1 bg-gray-900 border border-gray-800 rounded-3xl p-8 flex flex-col justify-between shadow-2xl">
          <div>
            <div className="flex justify-between items-center text-xs text-gray-500 mb-6">
              <span>MÔN THI: {currentQ.categoryCode}</span>
              <span>ĐỘ KHÓ: {currentQ.difficulty}</span>
            </div>

            <h2 className="text-2xl font-bold mb-8 text-gray-100 leading-snug">
              Câu hỏi {currentIndex + 1}: {currentQ.content}
            </h2>

            <div className="grid grid-cols-1 gap-3.5">
              {currentQ.options.map((opt, i) => {
                const alphabet = ['A', 'B', 'C', 'D'];
                const isSelected = answers[currentQ.id] === opt;
                return (
                  <button
                    key={i}
                    onClick={() => setAnswers({ ...answers, [currentQ.id]: opt })}
                    className={`w-full text-left px-6 py-4 border rounded-2xl text-sm transition-all ${
                      isSelected 
                        ? 'bg-red-600/10 border-red-500 text-red-400 font-bold' 
                        : 'bg-gray-950 border-gray-850 hover:border-gray-700 text-gray-300'
                    }`}
                  >
                    <span className="font-bold mr-3">{alphabet[i % 4]}.</span> {opt}
                  </button>
                );
              })}
            </div>
          </div>

          {/* Navigation Controls */}
          <div className="flex justify-between items-center pt-8 border-t border-gray-800 mt-8">
            <button
              onClick={() => setCurrentIndex(p => Math.max(0, p - 1))}
              disabled={currentIndex === 0}
              className="px-5 py-2 bg-gray-850 border border-gray-800 rounded-xl hover:bg-gray-800 disabled:opacity-30 disabled:hover:bg-gray-850 font-bold transition-all"
            >
              ◀ Câu trước
            </button>
            <span className="text-xs text-gray-500 font-mono">Tự động lưu bài làm</span>
            <button
              onClick={() => setCurrentIndex(p => Math.min(questions.length - 1, p + 1))}
              disabled={currentIndex === questions.length - 1}
              className="px-5 py-2 bg-red-600 rounded-xl hover:bg-red-500 disabled:opacity-30 disabled:hover:bg-red-600 font-bold transition-all"
            >
              Câu tiếp ▶
            </button>
          </div>
        </main>

        {/* Sidebar Question Grid Navigator */}
        <aside className="w-full md:w-64 bg-gray-900 border border-gray-800 rounded-3xl p-6 shadow-2xl h-fit">
          <h3 className="text-sm font-bold text-gray-400 mb-4 flex justify-between items-center">
            Danh sách câu hỏi
            <span className="px-2 py-0.5 bg-gray-800 rounded text-xs">
              {Object.keys(answers).length} / {questions.length}
            </span>
          </h3>

          <div className="grid grid-cols-4 gap-2">
            {questions.map((q, i) => {
              const isAnswered = !!answers[q.id];
              const isActive = i === currentIndex;
              return (
                <button
                  key={q.id}
                  onClick={() => setCurrentIndex(i)}
                  className={`aspect-square rounded-xl text-sm font-bold flex items-center justify-center transition-all ${
                    isActive 
                      ? 'ring-2 ring-red-500 ring-offset-2 ring-offset-gray-950 bg-red-600 text-white' 
                      : isAnswered
                        ? 'bg-green-600/20 text-green-400 border border-green-500/20'
                        : 'bg-gray-950 text-gray-500 border border-gray-850 hover:bg-gray-900'
                  }`}
                >
                  {i + 1}
                </button>
              );
            })}
          </div>
        </aside>
      </div>
    </div>
  );
}
