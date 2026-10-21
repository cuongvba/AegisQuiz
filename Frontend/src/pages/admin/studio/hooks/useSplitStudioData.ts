import { useState, useCallback } from 'react';

export interface StudioQuestionItem {
  id: string;
  sourceAnchor: string;
  originalSnippet: string;
  questionText: string;
  options: { key: string; text: string; isCorrect: boolean }[];
  explanation: string;
  rubricScore?: string;
  confidenceScore: number;
  subject: string;
  difficulty: number;
  isVerified: boolean;
}

export function useSplitStudioData() {
  const [documentTitle, setDocumentTitle] = useState<string>('Bo_De_Thi_Toan_Chuyen_2026.docx');
  const [sourceContent, setSourceContent] = useState<string[]>([]);
  const [activeQuestionId, setActiveQuestionId] = useState<string | null>(null);
  const [filterType, setFilterType] = useState<'ALL' | 'REVIEW' | 'VERIFIED' | 'MATH'>('ALL');
  const [isSaving, setIsSaving] = useState(false);
  const [lastSavedTime, setLastSavedTime] = useState<Date | null>(new Date());
  const [isAiSolving, setIsAiSolving] = useState(false);

  // Khởi tạo tập câu hỏi bóc tách mẫu tương thích Docx/Excel
  const [questions, setQuestions] = useState<StudioQuestionItem[]>([
    {
      id: 'q-1',
      sourceAnchor: 'Câu 1:',
      originalSnippet: 'Câu 1: Cho hàm số y = f(x) có đạo hàm f\'(x) = x(x - 1)^2(x + 2). Số điểm cực trị của hàm số là:\nA. 3\nB. 2\nC. 1\nD. 0',
      questionText: 'Cho hàm số $y = f(x)$ có đạo hàm $f\'(x) = x(x - 1)^2(x + 2)$. Số điểm cực trị của hàm số là:',
      options: [
        { key: 'A', text: '3', isCorrect: false },
        { key: 'B', text: '2', isCorrect: true },
        { key: 'C', text: '1', isCorrect: false },
        { key: 'D', text: '0', isCorrect: false },
      ],
      explanation: 'Ta có $f\'(x) = 0 \\iff x = 0, x = 1$ (bội 2), $x = -2$. Đạo hàm chỉ đổi dấu khi qua $x = 0$ và $x = -2$. Do đó hàm số có 2 điểm cực trị.',
      rubricScore: 'Đúng nghiệm x=0, x=-2: 0.5đ; Lý giải nghiệm bội chẵn: 0.5đ',
      confidenceScore: 98,
      subject: 'Toán học',
      difficulty: 0.35,
      isVerified: true,
    },
    {
      id: 'q-2',
      sourceAnchor: 'Câu 2:',
      originalSnippet: 'Câu 2: Nguyên hàm của hàm số f(x) = e^(2x) là:\nA. e^(2x) + C\nB. 1/2 e^(2x) + C\nC. 2e^(2x) + C\nD. 1/2 e^x + C',
      questionText: 'Nguyên hàm của hàm số $f(x) = e^{2x}$ là:',
      options: [
        { key: 'A', text: '$e^{2x} + C$', isCorrect: false },
        { key: 'B', text: '$\\frac{1}{2}e^{2x} + C$', isCorrect: true },
        { key: 'C', text: '$2e^{2x} + C$', isCorrect: false },
        { key: 'D', text: '$\\frac{1}{2}e^{x} + C$', isCorrect: false },
      ],
      explanation: 'Áp dụng công thức $\\int e^{ax} dx = \\frac{1}{a}e^{ax} + C$ với $a = 2$.',
      rubricScore: 'Áp dụng đúng công thức nguyên hàm hàm số mũ: 1.0đ',
      confidenceScore: 95,
      subject: 'Toán học',
      difficulty: -0.15,
      isVerified: true,
    },
    {
      id: 'q-3',
      sourceAnchor: 'Câu 3:',
      originalSnippet: 'Câu 3: Trong không gian Oxyz, phương trình mặt phẳng (P) đi qua M(1; -2; 3) và vuông góc với d: x/1 = (y-1)/2 = (z+1)/-1 là...',
      questionText: 'Trong không gian $Oxyz$, phương trình mặt phẳng $(P)$ đi qua $M(1; -2; 3)$ và vuông góc với đường thẳng $d: \\frac{x}{1} = \\frac{y-1}{2} = \\frac{z+1}{-1}$ là:',
      options: [
        { key: 'A', text: '$x + 2y - z + 6 = 0$', isCorrect: true },
        { key: 'B', text: '$x + 2y - z - 6 = 0$', isCorrect: false },
        { key: 'C', text: '$x - 2y + z - 8 = 0$', isCorrect: false },
        { key: 'D', text: '$2x + y - z + 3 = 0$', isCorrect: false },
      ],
      explanation: 'Vectơ pháp tuyến của mặt phẳng là $\\vec{n} = \\vec{u}_d = (1; 2; -1)$. Phương trình: $1(x - 1) + 2(y + 2) - 1(z - 3) = 0 \\iff x + 2y - z + 6 = 0$.',
      rubricScore: 'Xác định VTPT: 0.5đ; Viết đúng phương trình tổng quát: 0.5đ',
      confidenceScore: 92,
      subject: 'Hình học Oxyz',
      difficulty: 0.55,
      isVerified: false,
    },
  ]);

  const updateQuestionText = useCallback((id: string, text: string) => {
    setQuestions((prev) =>
      prev.map((q) => (q.id === id ? { ...q, questionText: text } : q))
    );
  }, []);

  const toggleOptionCorrect = useCallback((questionId: string, optionKey: string) => {
    setQuestions((prev) =>
      prev.map((q) => {
        if (q.id !== questionId) return q;
        return {
          ...q,
          options: q.options.map((opt) => ({
            ...opt,
            isCorrect: opt.key === optionKey,
          })),
        };
      })
    );
  }, []);

  const deleteQuestion = useCallback((id: string) => {
    setQuestions((prev) => prev.filter((q) => q.id !== id));
  }, []);

  const triggerAiRubric = useCallback(async () => {
    setIsAiSolving(true);
    // Giả lập kết nối AI Symposium / Rubric Generator
    await new Promise((resolve) => setTimeout(resolve, 800));
    setQuestions((prev) =>
      prev.map((q) => ({
        ...q,
        isVerified: true,
        rubricScore: q.rubricScore || 'AI Auto-Generated Barem: Từng bước suy luận (0.5đ) + Kết quả chính xác (0.5đ)',
      }))
    );
    setIsAiSolving(false);
  }, []);

  const filteredQuestions = questions.filter((q) => {
    if (filterType === 'REVIEW') return !q.isVerified;
    if (filterType === 'VERIFIED') return q.isVerified;
    if (filterType === 'MATH') return q.questionText.includes('$');
    return true;
  });

  return {
    documentTitle,
    setDocumentTitle,
    sourceContent,
    setSourceContent,
    questions,
    filteredQuestions,
    activeQuestionId,
    setActiveQuestionId,
    filterType,
    setFilterType,
    isSaving,
    lastSavedTime,
    isAiSolving,
    updateQuestionText,
    toggleOptionCorrect,
    deleteQuestion,
    triggerAiRubric,
  };
}
