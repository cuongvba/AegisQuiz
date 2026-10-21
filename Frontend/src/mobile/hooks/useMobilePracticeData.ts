import { useState, useEffect } from 'react';
import { learnerQuizService } from '@/services/learner-quiz.service';
import { offlineExamService } from '@/services/offline-exam.service';
import { PracticeQuestionItem } from '../types/mobile-practice.types';

interface UseMobilePracticeDataParams {
  topicId?: string;
  domainCode?: string;
  topicName: string;
  shortName: string;
  shuffleQuestions?: boolean;
}

export function useMobilePracticeData({
  topicId,
  domainCode,
  topicName,
  shortName,
  shuffleQuestions = true,
}: UseMobilePracticeDataParams) {
  const [dataSource, setDataSource] = useState<'db' | 'offline' | 'fallback'>('db');
  const [loading, setLoading] = useState<boolean>(true);
  const [questions, setQuestions] = useState<PracticeQuestionItem[]>([
    {
      id: 'q_fb_1',
      content: `[Bộ Đề Chuẩn ${shortName}] Khái niệm cốt lõi nào sau đây là chuẩn mực hàng đầu trong lĩnh vực?`,
      options: ['A', 'B', 'C', 'D'].map((k) => ({ key: k, text: `Phương án chuẩn ngành ${k}` })),
      correctKey: 'A',
      explanation: 'Chuẩn kiến thức tích hợp hệ thống bảo đảm an toàn nghiệp vụ.',
    },
  ]);

  useEffect(() => {
    let isMounted = true;
    const loadRealQuestions = async () => {
      setLoading(true);
      const targetKey = topicId || domainCode || 'ALL';

      // 1. Kiểm tra IndexedDB offline cache
      try {
        const cachedPkg = await offlineExamService.getOfflineExamPackage(targetKey);
        if (cachedPkg && Array.isArray(cachedPkg.questions) && cachedPkg.questions.length > 0) {
          if (isMounted) {
            setQuestions(cachedPkg.questions);
            setDataSource('offline');
            setLoading(false);
          }
          return;
        }
      } catch {}

      // 2. Tải từ DB thực qua learnerQuizService
      try {
        const rawList = await learnerQuizService.generatePracticeQuestions({
          topicConfigs: [{ topicKey: targetKey, numberOfQuestions: 30, isRandom: shuffleQuestions, startIndex: 1 }],
        });

        if (Array.isArray(rawList) && rawList.length > 0) {
          const mapped: PracticeQuestionItem[] = rawList.map((q: any) => {
            let rawOptions = q.options;
            if (typeof rawOptions === 'string') {
              try { rawOptions = JSON.parse(rawOptions); } catch {}
            }
            if ((!Array.isArray(rawOptions) || rawOptions.length === 0) && q.payload) {
              if (Array.isArray(q.payload.correctPairs)) {
                rawOptions = q.payload.correctPairs.map((p: any) => `${p.left} - ${p.right}`);
              } else if (Array.isArray(q.payload.leftItems) && Array.isArray(q.payload.rightItems)) {
                rawOptions = q.payload.leftItems.map((l: any, i: number) => `${l} - ${q.payload.rightItems[i] || ''}`);
              }
            }
            return {
              id: String(q.id),
              content: q.content || q.text || 'Nội dung câu hỏi',
              isFatal: !!(q.isCritical || q.isFatal || (q.content && q.content.includes('*'))),
              options: Array.isArray(rawOptions) && rawOptions.length > 0
                ? rawOptions.map((o: any, oIdx: number) => ({
                    key: ['A', 'B', 'C', 'D', 'E', 'F'][oIdx] || String.fromCharCode(65 + oIdx),
                    text: typeof o === 'string' ? o : (o.content || o.text || String(o)),
                  }))
                : [
                    { key: 'A', text: 'Phương án A' },
                    { key: 'B', text: 'Phương án B' },
                    { key: 'C', text: 'Phương án C' },
                    { key: 'D', text: 'Phương án D' },
                  ],
              correctKey: q.correctAnswer || q.answerRaw || (q.options?.find?.((o: any) => o.isCorrect)?.key) || q.correctOption || 'A',
              explanation: q.citation || q.explanation || q.payload?.explanation || 'Xem hướng dẫn chi tiết theo quy chuẩn hệ thống.',
              questionType: q.questionType || 'SINGLE',
              contextTitle: q.contextTitle || q.context?.title || q.payload?.contextTitle,
              contextContent: q.contextContent || q.context?.content || q.payload?.contextContent,
              imageUrl: q.imageUrl || q.questionMediaUrl || q.payload?.imageUrl || q.payload?.mediaUrl,
              payload: q.payload,
            };
          });

          if (isMounted) {
            setQuestions(mapped);
            setDataSource('db');
            offlineExamService.saveExamPackageOffline({
              examId: targetKey,
              title: topicName,
              domainCode: domainCode || 'GENERAL',
              questionCount: mapped.length,
              downloadedAt: Date.now(),
              questions: mapped,
            });
          }
        }
      } catch {
        if (isMounted) setDataSource('fallback');
      } finally {
        if (isMounted) setLoading(false);
      }
    };

    loadRealQuestions();
    return () => { isMounted = false; };
  }, [topicId, domainCode, topicName, shortName, shuffleQuestions]);

  return {
    questions,
    setQuestions,
    dataSource,
    loading,
  };
}
