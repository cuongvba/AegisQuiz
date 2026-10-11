export type SliceConceptType = 'clue' | 'concept' | 'step' | 'memory_hook';

export interface LessonSlice {
  id: string;
  order: number;
  conceptType: SliceConceptType;
  badgeLabel: string;
  title: string;
  content: string;
  visualIcon: string;
  speechText: string;
  highlightPhrase?: string;
  accentColor: string;
}

export interface MicroLessonData {
  questionId: string;
  topicName: string;
  questionPrompt: string;
  slices: LessonSlice[];
}
