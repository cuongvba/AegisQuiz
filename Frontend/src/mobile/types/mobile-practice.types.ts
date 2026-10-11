export interface PracticeQuestionItem {
  id: string;
  content: string;
  isFatal?: boolean;
  options: Array<{ key: string; text: string }>;
  correctKey: string;
  explanation: string;
}

export interface MobilePracticeRoomProps {
  topicId?: string;
  topicName: string;
  onExit: () => void;
}
