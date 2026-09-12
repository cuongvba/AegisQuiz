'use client';
import { useState, useEffect } from 'react';
import Link from 'next/link';

interface AttemptItem {
  questionId: string;
  content: string;
  userAnswer: string;
  correctAnswer: string;
  isCorrect: boolean;
}

interface AttemptReviewData {
  score: number;
  correctCount: number;
  totalCount: number;
  timeSpent: string;
  items: AttemptItem[];
}

export default function AttemptReviewPage() {
  const [data, setData] = useState<AttemptReviewData | null>(null);
  const [aiAnalysis, setAiAnalysis] = useState<string | null>(null);
  const [analyzing, setAnalyzing] = useState(false);
  const [explanations, setExplanations] = useState<Record<string, string>>({});
  const [loadingExplId, setLoadingExplId] = useState<string | null>(null);

  useEffect(() => {
    const raw = localStorage.getItem('attempt_review_data');
    if (raw) {
      try {
        setData(JSON.parse(raw));
      } catch (e) {
        console.error("Lỗi đọc dữ liệu ôn tập", e);
      }
    }
  }, []);

  const handleAiAnalysis = async () => {
    if (!data) return;
    setAnalyzing(true);
    try {
      const res = await fetch('http://localhost:8080/api/quiz/attempt/analyze', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ attempts: data.items })
      });
      const resData = await res.json();
      setAiAnalysis(resData.analysis);
    } catch (e) {
      alert("Lỗi gọi AI Phân tích bài thi");
    } finally {
      setAnalyzing(false);
    }
  };

  const handleToggleExplanation = async (qId: string, uAns: string) => {
    if (explanations[qId]) {
      // Toggle off if already exists
      const copy = { ...explanations };
      delete copy[qId];
      setExplanations(copy);
      return;
    }

    setLoadingExplId(qId);
    try {
      const res = await fetch(`http://localhost:8080/api/quiz/${qId}/explain`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ selectedAnswer: uAns })
      });
      const resData = await res.json();
      setExplanations({ ...explanations, [qId]: resData.explanation });
    } catch (e) {
      alert("Lỗi tải giải thích câu hỏi");
    } finally {
      setLoadingExplId(null);
    }
  };

  if (!data) {
    return (
      <div className="min-h-screen bg-gray-950 text-white flex flex-col items-center justify-center">
        <p className="text-gray-400 mb-4">Không tìm thấy dữ liệu lượt thi gần nhất.</p>
        <Link href="/" className="px-6 py-2 bg-gray-800 text-white font-bold rounded-xl hover:bg-gray-700">
          Về Trang Chủ
        </Link>
      </div>
    );
  }

  // Draw circular progress svg variables
  const radius = 52;
  const stroke = 8;
  const normalizedRadius = radius - stroke * 2;
  const circumference = normalizedRadius * 2 * Math.PI;
  const strokeDashoffset = circumference - (data.score / 100) * circumference;

  return (
    <div className="min-h-screen bg-gray-950 text-white py-12 px-6 font-sans">
      <div className="max-w-4xl mx-auto space-y-8">
        
        {/* Top Header */}
        <div className="flex justify-between items-center">
          <h1 className="text-2xl font-black text-gray-100">Báo cáo Kết quả Lượt thi</h1>
          <Link href="/" className="text-xs text-gray-400 hover:text-white px-4 py-2 border border-gray-850 rounded-xl">
            ← Quay lại Trang Chủ
          </Link>
        </div>

        {/* Score Ring Summary Card */}
        <section className="bg-gray-900 border border-gray-850 rounded-3xl p-8 flex flex-col md:flex-row justify-between items-center gap-8 shadow-2xl">
          <div className="flex items-center gap-6">
            {/* Circular Progress Circle */}
            <div className="relative w-28 h-28 flex items-center justify-center">
              <svg className="w-full h-full transform -rotate-90">
                <circle
                  stroke="#1f2937"
                  fill="transparent"
                  strokeWidth={stroke}
                  r={normalizedRadius}
                  cx={56}
                  cy={56}
                />
                <circle
                  stroke="#ef4444"
                  fill="transparent"
                  strokeWidth={stroke}
                  strokeDasharray={circumference + ' ' + circumference}
                  style={{ strokeDashoffset }}
                  r={normalizedRadius}
                  cx={56}
                  cy={56}
                  className="transition-all duration-500 ease-out"
                />
              </svg>
              <div className="absolute flex flex-col items-center">
                <span className="text-2xl font-black">{data.score}%</span>
                <span className="text-[10px] text-gray-500 uppercase font-bold">Hoàn thành</span>
              </div>
            </div>

            <div className="space-y-1">
              <h2 className="text-2xl font-extrabold text-gray-100">Làm tốt lắm! 🎉</h2>
              <p className="text-sm text-gray-400 font-medium">Bạn đã trả lời đúng {data.correctCount} trên {data.totalCount} câu hỏi.</p>
              <p className="text-xs text-gray-500">⏱️ Thời gian hoàn tất: {data.timeSpent}</p>
            </div>
          </div>

          <div className="w-full md:w-auto flex flex-col gap-2.5">
            <div className="bg-gray-950 px-5 py-3.5 border border-gray-850 rounded-2xl flex justify-between items-center text-xs w-60">
              <span className="text-gray-400 font-semibold">Chẩn đoán tổng quan</span>
              <span className="font-bold text-red-400">AI Enabled</span>
            </div>
          </div>
        </section>

        {/* AI Action Assistant Panel */}
        <section className="bg-gradient-to-tr from-red-650 to-orange-600 rounded-3xl p-8 text-center text-white space-y-5 shadow-lg shadow-red-500/10">
          <div className="space-y-1.5">
            <h3 className="text-xl font-bold flex items-center justify-center gap-2">
              🧠 AI Gemini Chẩn Đoán Lộ Trình Ôn Tập
            </h3>
            <p className="text-sm text-red-100 max-w-lg mx-auto">
              Gia sư AI sẽ phân tích các lỗi sai của bạn trong lượt thi vừa rồi để lên lộ trình lấp hổng kiến thức.
            </p>
          </div>

          {!aiAnalysis ? (
            <button 
              onClick={handleAiAnalysis}
              disabled={analyzing}
              className="bg-white text-red-600 font-bold px-6 py-3 rounded-2xl shadow-lg transition-transform hover:scale-102 flex items-center justify-center mx-auto gap-2 text-sm disabled:opacity-80"
            >
              {analyzing ? (
                <>
                  <span className="animate-spin mr-1">🌀</span> Đang giải mã bài thi...
                </>
              ) : '🪄 Nhờ AI Phân tích bài làm'}
            </button>
          ) : (
            <div className="bg-gray-950/40 border border-white/10 rounded-2xl p-6 text-left shadow-inner whitespace-pre-line text-sm text-gray-200 leading-relaxed">
              {aiAnalysis}
            </div>
          )}
        </section>

        {/* Detailed Question Review List */}
        <section className="space-y-4">
          <h3 className="text-lg font-bold text-gray-300 border-b border-gray-850 pb-2">Chi tiết bài làm</h3>

          {data.items.map((item, idx) => (
            <div 
              key={item.questionId}
              className={`bg-gray-900 border-2 rounded-2xl p-6 transition-all ${
                item.isCorrect ? 'border-transparent shadow-sm' : 'border-red-900/20 bg-red-950/5 shadow-md'
              }`}
            >
              <div className="flex justify-between items-start mb-4">
                <span className={`text-xs font-bold px-2.5 py-1 rounded-lg ${
                  item.isCorrect ? 'bg-green-500/10 text-green-400' : 'bg-red-500/10 text-red-400'
                }`}>
                  Câu {idx + 1} • {item.isCorrect ? 'Đúng' : 'Sai'}
                </span>

                {!item.isCorrect && (
                  <button 
                    onClick={() => handleToggleExplanation(item.questionId, item.userAnswer)}
                    disabled={loadingExplId === item.questionId}
                    className="text-xs font-bold bg-amber-500/10 text-amber-400 border border-amber-500/20 px-3 py-1.5 rounded-xl hover:bg-amber-500/25 transition-all flex items-center gap-1.5 disabled:opacity-60"
                  >
                    {loadingExplId === item.questionId ? '⏳ Đang phân tích...' : '💡 AI Giải thích'}
                  </button>
                )}
              </div>

              <p className="text-gray-200 font-semibold mb-4 leading-relaxed">{item.content}</p>

              {/* Answers visual wrapper */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 p-4 bg-gray-950 rounded-2xl border border-gray-850 text-xs">
                <div className="flex items-center gap-2">
                  <span className="text-gray-500 uppercase tracking-wider font-semibold">Đáp án của bạn:</span>
                  <span className={`font-bold ${item.isCorrect ? 'text-green-400' : 'text-red-400'}`}>
                    {item.userAnswer || 'Chưa chọn'}
                  </span>
                </div>
                <div className="flex items-center gap-2">
                  <span className="text-gray-500 uppercase tracking-wider font-semibold">Đáp án chuẩn:</span>
                  <span className="font-bold text-green-400">{item.correctAnswer}</span>
                </div>
              </div>

              {/* Explanations drawer */}
              {explanations[item.questionId] && (
                <div className="mt-4 bg-gray-950/80 border border-amber-500/10 rounded-2xl p-5 text-xs text-amber-300/90 whitespace-pre-line leading-relaxed shadow-inner">
                  {explanations[item.questionId]}
                </div>
              )}
            </div>
          ))}
        </section>

      </div>
    </div>
  );
}
