'use client';
import { useState } from 'react';
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

export default function CustomQuizPage() {
  // CONFIG STATE
  const [toanCount, setToanCount] = useState(3);
  const [toanRandom, setToanRandom] = useState(true);
  const [toanStartIndex, setToanStartIndex] = useState(1);

  const [itCount, setItCount] = useState(2);
  const [itRandom, setItRandom] = useState(true);
  const [itStartIndex, setItStartIndex] = useState(1);

  const [shuffleQuestions, setShuffleQuestions] = useState(false); // Mặc định false để giữ phân cụm chủ đề!
  const [shuffleAnswers, setShuffleAnswers] = useState(true);

  // QUIZ RUNTIME STATE
  const [quizQuestions, setQuizQuestions] = useState<Question[]>([]);
  const [isPlaying, setIsPlaying] = useState(false);
  const [currentIdx, setCurrentIdx] = useState(0);
  const [selectedOpt, setSelectedOpt] = useState<number | null>(null);
  const [showResult, setShowResult] = useState(false);
  const [score, setScore] = useState(0);
  const [startTime, setStartTime] = useState<number>(0);
  const [loading, setLoading] = useState(false);

  const userId = '9b0a1a1c-a19b-4e12-8822-42171a82ea3c';

  const handleStartQuiz = async () => {
    setLoading(true);
    try {
      const payload = {
        mode: 'study',
        shuffleQuestions,
        shuffleAnswers,
        topicConfigs: [
          {
            topicKey: 'Toán',
            numberOfQuestions: toanCount,
            isRandom: toanRandom,
            startIndex: toanStartIndex
          },
          {
            topicKey: 'IT',
            numberOfQuestions: itCount,
            isRandom: itRandom,
            startIndex: itStartIndex
          }
        ]
      };

      const res = await fetch('http://localhost:8080/api/quiz/practice', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });
      const data = await res.json();
      setQuizQuestions(data);
      setIsPlaying(true);
      setCurrentIdx(0);
      setSelectedOpt(null);
      setShowResult(false);
      setScore(0);
      setStartTime(Date.now());
    } catch (e) {
      alert("Lỗi tải đề thi tự chọn. Đảm bảo backend đang chạy.");
    } finally {
      setLoading(false);
    }
  };

  const handleAnswerClick = async (optIdx: number) => {
    const q = quizQuestions[currentIdx];
    const chosen = q.options[optIdx];
    const isCorrect = chosen === q.correctOption;

    setSelectedOpt(optIdx);
    setShowResult(true);

    if (isCorrect) setScore(s => s + 1);

    const timeSpent = Math.round((Date.now() - startTime) / 1000);

    // Lưu kết quả làm bài vào DB
    try {
      await fetch('http://localhost:8080/api/quiz/attempt', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          userId,
          questionId: q.id,
          category: q.categoryCode,
          difficulty: q.difficulty,
          selectedAnswer: chosen,
          isCorrect,
          timeSpentSeconds: timeSpent
        })
      });
    } catch (e) {
      console.error(e);
    }
  };

  const handleNext = () => {
    if (currentIdx + 1 < quizQuestions.length) {
      setCurrentIdx(idx => idx + 1);
      setSelectedOpt(null);
      setShowResult(false);
      setStartTime(Date.now());
    } else {
      alert(`Bài kiểm tra kết thúc! Điểm số: ${score}/${quizQuestions.length}. Hệ thống đã ghi nhận lịch sử.`);
      setIsPlaying(false);
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-950 flex flex-col items-center justify-center text-white">
        <div className="animate-spin text-5xl mb-4">🌀</div>
        <p className="text-gray-400">Đang khởi tạo đề thi theo cấu trúc của bạn...</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-950 text-white p-8 flex flex-col items-center justify-center">
      {!isPlaying ? (
        /* CONFIGURATION VIEW */
        <div className="max-w-3xl w-full bg-gray-900 border border-gray-800 rounded-3xl p-8 shadow-2xl space-y-8">
          <div>
            <h1 className="text-3xl font-extrabold bg-gradient-to-r from-orange-400 to-amber-500 bg-clip-text text-transparent">
              🛠️ Tự cấu hình Đề thi (Custom Practice)
            </h1>
            <p className="text-sm text-gray-400 mt-1">Kế thừa động cơ cấu trúc đề thi đa cụm chủ đề của DEHOC</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* TOÁN CONFIG */}
            <div className="bg-gray-950 p-6 rounded-2xl border border-gray-800 space-y-4">
              <h2 className="text-lg font-bold text-orange-400">📐 Môn học: Toán</h2>
              <div>
                <label className="text-xs text-gray-400 block mb-1">Số lượng câu hỏi:</label>
                <input 
                  type="number" 
                  value={toanCount} 
                  onChange={(e) => setToanCount(parseInt(e.target.value) || 0)}
                  className="w-full bg-gray-900 border border-gray-800 rounded-xl px-3 py-2 text-sm text-white focus:outline-none focus:border-orange-500"
                />
              </div>
              <div className="flex gap-4">
                <button 
                  onClick={() => setToanRandom(true)}
                  className={`flex-1 py-2 rounded-xl text-xs font-bold transition-all ${toanRandom ? 'bg-orange-600 text-white' : 'bg-gray-900 text-gray-400 border border-gray-800'}`}
                >
                  Lấy Ngẫu Nhiên
                </button>
                <button 
                  onClick={() => setToanRandom(false)}
                  className={`flex-1 py-2 rounded-xl text-xs font-bold transition-all ${!toanRandom ? 'bg-orange-600 text-white' : 'bg-gray-900 text-gray-400 border border-gray-800'}`}
                >
                  Lấy Tuần Tự
                </button>
              </div>
              {!toanRandom && (
                <div>
                  <label className="text-xs text-gray-400 block mb-1">Bắt đầu từ câu thứ:</label>
                  <input 
                    type="number" 
                    value={toanStartIndex} 
                    onChange={(e) => setToanStartIndex(parseInt(e.target.value) || 1)}
                    className="w-full bg-gray-900 border border-gray-800 rounded-xl px-3 py-2 text-sm text-white focus:outline-none focus:border-orange-500"
                  />
                </div>
              )}
            </div>

            {/* IT CONFIG */}
            <div className="bg-gray-950 p-6 rounded-2xl border border-gray-800 space-y-4">
              <h2 className="text-lg font-bold text-blue-400">💻 Môn học: IT (Bảo mật)</h2>
              <div>
                <label className="text-xs text-gray-400 block mb-1">Số lượng câu hỏi:</label>
                <input 
                  type="number" 
                  value={itCount} 
                  onChange={(e) => setItCount(parseInt(e.target.value) || 0)}
                  className="w-full bg-gray-900 border border-gray-800 rounded-xl px-3 py-2 text-sm text-white focus:outline-none focus:border-blue-500"
                />
              </div>
              <div className="flex gap-4">
                <button 
                  onClick={() => setItRandom(true)}
                  className={`flex-1 py-2 rounded-xl text-xs font-bold transition-all ${itRandom ? 'bg-blue-600 text-white' : 'bg-gray-900 text-gray-400 border border-gray-800'}`}
                >
                  Lấy Ngẫu Nhiên
                </button>
                <button 
                  onClick={() => setItRandom(false)}
                  className={`flex-1 py-2 rounded-xl text-xs font-bold transition-all ${!itRandom ? 'bg-blue-600 text-white' : 'bg-gray-900 text-gray-400 border border-gray-800'}`}
                >
                  Lấy Tuần Tự
                </button>
              </div>
              {!itRandom && (
                <div>
                  <label className="text-xs text-gray-400 block mb-1">Bắt đầu từ câu thứ:</label>
                  <input 
                    type="number" 
                    value={itStartIndex} 
                    onChange={(e) => setItStartIndex(parseInt(e.target.value) || 1)}
                    className="w-full bg-gray-900 border border-gray-800 rounded-xl px-3 py-2 text-sm text-white focus:outline-none focus:border-blue-500"
                  />
                </div>
              )}
            </div>
          </div>

          {/* GLOBAL CONFIGURATION OPTIONS */}
          <div className="bg-gray-950 p-6 rounded-2xl border border-gray-800 space-y-4">
            <h3 className="text-sm font-semibold text-gray-400">Cấu hình Đề thi tổng hợp</h3>
            
            <div className="flex items-center justify-between py-2 border-b border-gray-900">
              <div>
                <p className="font-semibold">Xáo trộn câu hỏi toàn cục (Global Shuffle)</p>
                <p className="text-xs text-gray-500">Tắt đi để phân cụm đề thi: Toán đứng trước, IT đứng sau.</p>
              </div>
              <button 
                onClick={() => setShuffleQuestions(!shuffleQuestions)}
                className={`w-12 h-6 rounded-full transition-colors relative ${shuffleQuestions ? 'bg-orange-600' : 'bg-gray-800'}`}
              >
                <div className={`w-4 h-4 rounded-full bg-white absolute top-1 transition-all ${shuffleQuestions ? 'right-1' : 'left-1'}`} />
              </button>
            </div>

            <div className="flex items-center justify-between py-2">
              <div>
                <p className="font-semibold">Xáo trộn phương án trả lời (Option Shuffle)</p>
                <p className="text-xs text-gray-500">Thay đổi ngẫu nhiên thứ tự các đáp án A, B, C, D của từng câu hỏi.</p>
              </div>
              <button 
                onClick={() => setShuffleAnswers(!shuffleAnswers)}
                className={`w-12 h-6 rounded-full transition-colors relative ${shuffleAnswers ? 'bg-orange-600' : 'bg-gray-800'}`}
              >
                <div className={`w-4 h-4 rounded-full bg-white absolute top-1 transition-all ${shuffleAnswers ? 'right-1' : 'left-1'}`} />
              </button>
            </div>
          </div>

          <div className="flex gap-4">
            <button 
              onClick={handleStartQuiz}
              className="flex-1 bg-gradient-to-r from-orange-500 to-amber-600 text-white font-bold py-4 rounded-2xl hover:scale-102 transition-transform shadow-lg shadow-orange-500/20"
            >
              🚀 Bắt đầu Luyện tập
            </button>
            <Link 
              href="/"
              className="px-6 py-4 bg-gray-800 hover:bg-gray-700 text-gray-300 font-bold rounded-2xl transition-colors flex items-center justify-center"
            >
              Trở lại
            </Link>
          </div>
        </div>
      ) : (
        /* QUIZ RUNNING VIEW */
        <div className="max-w-2xl w-full bg-gray-900 border border-gray-800 rounded-3xl p-8 shadow-2xl space-y-6">
          <div className="flex justify-between items-center text-xs text-gray-400">
            <span>Đề thi tự chọn: <strong>{shuffleQuestions ? "Đã xáo trộn toàn bộ" : "Phân cụm theo Chủ đề"}</strong></span>
            <span>Đúng: {score}/{quizQuestions.length} câu</span>
          </div>

          <div className="w-full bg-gray-800 h-2 rounded-full overflow-hidden">
            <div 
              className="h-full bg-orange-500 transition-all duration-300"
              style={{ width: `${((currentIdx + 1) / quizQuestions.length) * 100}%` }}
            />
          </div>

          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <span className={`px-2.5 py-1 rounded-full text-xs font-bold ${quizQuestions[currentIdx].categoryCode === 'Toán' ? 'bg-orange-500/10 text-orange-400' : 'bg-blue-500/10 text-blue-400'}`}>
                {quizQuestions[currentIdx].categoryCode}
              </span>
              <span className="text-xs text-gray-500">Độ khó: {quizQuestions[currentIdx].difficulty}</span>
            </div>
            
            <h2 className="text-2xl font-bold text-gray-100 leading-snug">
              Câu {currentIdx + 1}: {quizQuestions[currentIdx].content}
            </h2>
          </div>

          {/* OPTIONS */}
          <div className="grid grid-cols-1 gap-3 pt-4">
            {quizQuestions[currentIdx].options.map((opt, i) => {
              const alphabet = ['A', 'B', 'C', 'D'];
              const isCorrectOpt = opt === quizQuestions[currentIdx].correctOption;
              
              let btnClass = "bg-gray-950 border-gray-800 hover:border-orange-500 text-gray-300";
              if (showResult) {
                if (isCorrectOpt) {
                  btnClass = "bg-green-600/10 border-green-500 text-green-400 font-bold";
                } else if (selectedOpt === i) {
                  btnClass = "bg-red-600/10 border-red-500 text-red-400 font-bold";
                } else {
                  btnClass = "bg-gray-950/30 border-gray-900 text-gray-600";
                }
              }

              return (
                <button
                  key={i}
                  disabled={showResult}
                  onClick={() => handleAnswerClick(i)}
                  className={`w-full text-left border px-6 py-4 rounded-xl text-sm transition-all ${btnClass}`}
                >
                  <span className="font-bold mr-3">{alphabet[i % 4]}.</span> {opt}
                </button>
              );
            })}
          </div>

          {/* ACTIONS / FEEDBACK */}
          {showResult && (
            <div className="pt-6 border-t border-gray-800 flex justify-between items-center">
              <div className="text-sm">
                {selectedOpt !== null && quizQuestions[currentIdx].options[selectedOpt] === quizQuestions[currentIdx].correctOption ? (
                  <span className="text-green-500 font-bold">🎉 Chính xác! Bạn được cộng điểm.</span>
                ) : (
                  <span className="text-red-500 font-bold">😢 Sai rồi! Đáp án: {quizQuestions[currentIdx].correctOption}</span>
                )}
              </div>
              <button 
                onClick={handleNext}
                className="bg-orange-600 hover:bg-orange-700 px-6 py-2 rounded-xl text-sm font-bold transition-all"
              >
                {currentIdx + 1 < quizQuestions.length ? "Tiếp tục" : "Kết thúc & Xem kết quả"}
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
