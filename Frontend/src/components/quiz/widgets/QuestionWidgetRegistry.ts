import { lazy } from 'react';
import type { ComponentType } from 'react';
import type { QuestionWidgetProps } from './types';

// ─── Dynamic Micro-Widget Registry (100% OCP - Open/Closed Principle) ───────
export const QuestionWidgetRegistry: Record<string, React.LazyExoticComponent<ComponentType<QuestionWidgetProps>>> = {
  SINGLE: lazy(() => import('./SingleChoiceWidget')),
  MULTI: lazy(() => import('./MultiChoiceWidget')),
  MULTI_TRUE_FALSE: lazy(() => import('./MultiTrueFalseWidget')),
  SHORT_ANSWER: lazy(() => import('./ShortAnswerWidget')),
  FILL_BLANK: lazy(() => import('./ShortAnswerWidget')),
  MATCHING: lazy(() => import('./MatchingWidget')),
  ORDERING: lazy(() => import('./OrderingWidget')),
  ESSAY: lazy(() => import('./EssayWidget')),
};

export function resolveQuestionWidget(questionType: string, optionsCount: number) {
  // Chuẩn hóa loại câu hỏi: nếu TRUE_FALSE mà có 4 phương án GDPT 2025 thì ánh xạ sang MULTI_TRUE_FALSE
  if (questionType === 'TRUE_FALSE') {
    if (optionsCount >= 4) {
      return QuestionWidgetRegistry['MULTI_TRUE_FALSE'];
    }
    return QuestionWidgetRegistry['SINGLE'];
  }

  const widget = QuestionWidgetRegistry[questionType];
  return widget || QuestionWidgetRegistry['SINGLE'];
}
