import type { DisplayOption, LearnerAnswer, LearnerQuestion } from '@/types/quiz';

export interface QuestionWidgetProps {
  question: LearnerQuestion;
  answer?: LearnerAnswer;
  options: DisplayOption[];
  onUpdate: (partial: Partial<LearnerAnswer>) => void;
  disabled?: boolean;
  showHint?: boolean;
  soundEnabled: boolean;
  t: (key: string) => string;
}
