export interface PracticeQuestionItem {
  id: string;
  content: string;
  isFatal?: boolean;
  options: Array<{ key: string; text: string }>;
  correctKey: string;
  explanation: string;
  questionType?: string;
  contextTitle?: string;
  contextContent?: string;
  imageUrl?: string;
  payload?: any;
}

export interface MobilePracticeRoomProps {
  topicId?: string;
  topicName: string;
  onExit: () => void;
}
