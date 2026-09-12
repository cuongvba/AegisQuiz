'use client';
import { useState, useEffect } from 'react';

interface Question {
  id: string;
  content: string;
  difficulty: number;
  durationSeconds: number;
  categoryCode: string;
  options: string[];
  correctOption: string;
}

export default function KidQuizCard() {
  const [questions, setQuestions] = useState<Question[]>([]);
  const [currentIdx, setCurrentIdx] = useState(0);
  const [selected, setSelected] = useState<number | null>(null);
  const [showResult, setShowResult] = useState(false);
  const [stars, setStars] = useState(0);
  const [isAdaptive, setIsAdaptive] = useState(false);
  const [loading, setLoading] = useState(true);
  const [startTime, setStartTime] = useState<number>(0);
  const [weakestTopic, setWeakestTopic] = useState<string | null>(null);

  const userId = '9b0a1a1c-a19b-4e12-8822-42171a82ea3c';

  const fetchQuestions = async (adaptive: boolean) => {
    setLoading(true);
    try {
      let url = 'http://localhost:8080/api/quiz/questions';
      let options: RequestInit = { method: 'GET' };

      if (adaptive) {
        url = 'http://localhost:8080/api/quiz/adaptive';
        options = {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ userId })
        };
      }

      const res = await resFetch(url, options);
      if (adaptive) {
        setQuestions(res.questions);
        setWeakestTopic(res.weakestCategory);
      } else {
        setQuestions(res);
        setWeakestTopic(null);
      }
      setCurrentIdx(0);
      setSelected(null);
      setShowResult(false);
      setStartTime(Date.now());
    } catch (e) {
      console.error("Lỗi lấy câu hỏi:", e);
    } finally {
      setLoading(false);
    }
  };

  const resFetch = async (url: string, options: RequestInit) => {
    const r = await fetch(url, options);
    return r.json();
  };

  useEffect(() => {
    fetchQuestions(isAdaptive);
  }, [isAdaptive]);

  const handleSelect = async (optIdx: number) => {
    const question = questions[currentIdx];
    const selectedOpt = question.options[optIdx];
    const isCorrect = selectedOpt === question.correctOption;
    
    setSelected(optIdx);
    setShowResult(true);
    
    if (isCorrect) {
      setStars(s => s + 1);
    }

    const timeSpent = Math.round((Date.now() - startTime) / 1000);

    // Gửi lịch sử làm bài về Backend để thuật toán phân tích điểm yếu học tập
    try {
      await fetch('http://localhost:8080/api/quiz/attempt', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          userId,
          questionId: question.id,
          category: question.categoryCode,
          difficulty: question.difficulty,
          selectedAnswer: selectedOpt,
          isCorrect,
          timeSpentSeconds: timeSpent
        })
      });
    } catch (e) {
      console.error("Lỗi gửi lịch sử làm bài:", e);
    }
  };

  const handleNext = () => {
    if (currentIdx + 1 < questions.length) {
      setCurrentIdx(idx => idx + 1);
      setSelected(null);
      setShowResult(false);
      setStartTime(Date.now());
    } else {
      alert(`Chúc mừng! Bạn đã hoàn thành bài thi và đạt ${stars} sao! Lịch sử đã được lưu lại để cố vấn AI phân tích.`);
      fetchQuestions(isAdaptive); // Reset lại đề
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-yellow-100 via-pink-100 to-blue-100 flex items-center justify-center">
        <div className="text-center font-bold text-gray-700">
          <p className="text-4xl animate-spin mb-4">🌀</p>
          <p className="text-lg">Đang tải đề thi thích ứng...</p>
        </div>
      </div>
    );
  }

  if (questions.length === 0) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-yellow-100 via-pink-100 to-blue-100 flex items-center justify-center p-6">
        <div className="bg-white rounded-3xl p-8 border-4 border-red-300 text-center max-w-md shadow-2xl">
          <p className="text-5xl mb-4">⚠️</p>
          <p className="text-xl font-bold text-gray-800 mb-2">Không tìm thấy câu hỏi!</p>
          <p className="text-sm text-gray-500 mb-4">Vui lòng kiểm tra xem Backend đã được khởi tạo và seeding câu hỏi thành công hay chưa.</p>
          <button onClick={() => fetchQuestions(isAdaptive)} className="bg-blue-500 text-white font-bold py-2 px-6 rounded-full">Tải lại</button>
        </div>
      </div>
    );
  }

  const currentQuestion = questions[currentIdx];
  const correctIdx = currentQuestion.options.indexOf(currentQuestion.correctOption);

  return (
    <div className="min-h-screen bg-gradient-to-br from-yellow-100 via-pink-100 to-blue-100 p-6 font-[Comic_Neue]">
      
      {/* TOP TOGGLE */}
      <div className="max-w-2xl mx-auto mb-6 flex justify-between items-center bg-white/70 backdrop-blur px-4 py-2.5 rounded-2xl border border-white/50 shadow-sm">
        <div className="flex items-center gap-2">
          <span className="text-xl">⚙️</span>
          <span className="text-sm font-bold text-gray-700">Chế độ thi:</span>
        </div>
        <div className="flex gap-2">
          <button 
            onClick={() => setIsAdaptive(false)}
            className={`px-3 py-1.5 rounded-xl text-xs font-bold transition-all ${!isAdaptive ? 'bg-blue-600 text-white shadow-md' : 'bg-gray-200 text-gray-600 hover:bg-gray-300'}`}
          >
            Chuẩn
          </button>
          <button 
            onClick={() => setIsAdaptive(true)}
            className={`px-3 py-1.5 rounded-xl text-xs font-bold transition-all ${isAdaptive ? 'bg-purple-600 text-white shadow-md' : 'bg-gray-200 text-gray-600 hover:bg-gray-300'}`}
          >
            🧠 AI Thích ứng
          </button>
        </div>
      </div>

      {isAdaptive && weakestTopic && (
        <div className="max-w-2xl mx-auto mb-6 bg-purple-500 text-white px-4 py-3 rounded-2xl text-center text-sm font-semibold shadow-lg animate-pulse">
          🎯 AI đã phát hiện bạn yếu ở môn **{weakestTopic}** và ưu tiên sinh đề môn này!
        </div>
      )}

      {/* THANH TIẾN TRÌNH DẠNG ĐƯỜNG ĐUA */}
      <div className="max-w-2xl mx-auto mb-6">
        <div className="flex items-center gap-2 mb-2">
          <span className="text-2xl">🏁</span>
          <div className="flex-1 h-6 bg-white rounded-full border-4 border-yellow-300 overflow-hidden">
            <div 
              className="h-full bg-gradient-to-r from-green-400 to-blue-500 rounded-full transition-all duration-700 flex items-center justify-end pr-1" 
              style={{ width: `${((currentIdx + 1) / questions.length) * 100}%` }}
            >
              <span className="text-sm">🏃</span>
            </div>
          </div>
          <span className="text-2xl">🏆</span>
        </div>
        <div className="flex justify-between text-sm font-bold text-gray-700">
          <span>Câu {currentIdx + 1}/{questions.length} ({currentQuestion.categoryCode})</span>
          <span>⭐ {stars} sao</span>
        </div>
      </div>

      {/* CÂU HỎI */}
      <div className="max-w-2xl mx-auto bg-white rounded-3xl border-4 border-yellow-300 shadow-2xl p-8">
        <h2 className="text-3xl font-bold text-center text-gray-800 mb-8 leading-snug">
          🤔 {currentQuestion.content}
        </h2>

        {/* CÁC ĐÁP ÁN */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {currentQuestion.options.map((opt, i) => {
            const colors = ['bg-red-400 hover:bg-red-500', 'bg-blue-400 hover:bg-blue-500', 'bg-green-400 hover:bg-green-500', 'bg-yellow-400 hover:bg-yellow-500'];
            const emojis = ['🅰️', '🅱️', '🅲', '🅳'];
            let extraClass = '';
            
            if (showResult && i === correctIdx) extraClass = 'ring-4 ring-green-600 scale-105';
            if (showResult && selected === i && i !== correctIdx) extraClass = 'ring-4 ring-red-600 opacity-60';

            return (
              <button
                key={i}
                onClick={() => !showResult && handleSelect(i)}
                disabled={showResult}
                className={`${colors[i % 4]} ${extraClass} text-white text-lg font-bold py-6 px-4 rounded-2xl shadow-lg transition-all transform hover:scale-102 active:scale-95 text-left pl-6`}
              >
                {emojis[i % 4]} {opt}
              </button>
            );
          })}
        </div>

        {/* KẾT QUẢ (HOẠT HÌNH) */}
        {showResult && (
          <div className="mt-8 text-center border-t-2 border-dashed border-gray-200 pt-6">
            {selected === correctIdx ? (
              <div className="animate-bounce">
                <p className="text-6xl mb-2">🎉</p>
                <p className="text-2xl font-bold text-green-600">Tuyệt vời! Đúng rồi!</p>
                <p className="text-yellow-500 text-lg">⭐ +1 sao</p>
              </div>
            ) : (
              <div>
                <p className="text-6xl mb-2">😢</p>
                <p className="text-2xl font-bold text-red-500">Sai mất rồi!</p>
                <p className="text-gray-600">Đáp án đúng là: <strong>{currentQuestion.correctOption}</strong></p>
              </div>
            )}
            <button 
              onClick={handleNext} 
              className="mt-6 bg-gradient-to-r from-pink-500 to-purple-500 text-white font-bold py-3 px-8 rounded-full text-lg shadow-lg hover:scale-105 transition-transform"
            >
              {currentIdx + 1 < questions.length ? "Câu tiếp theo →" : "Xem kết quả thi"}
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
