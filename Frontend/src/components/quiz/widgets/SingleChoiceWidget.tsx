import { parseIndexList, getDisplayOptions } from '@/lib/quiz-helpers';
import type { QuestionWidgetProps } from './types';
import { OptionChoiceItem } from './OptionChoiceItem';
import { playSynthGameSound } from './sound-effects';

export default function SingleChoiceWidget({
  question,
  answer,
  onUpdate,
  disabled,
  showHint,
  soundEnabled
}: QuestionWidgetProps) {
  const displayOptions = getDisplayOptions(question);
  const correct = parseIndexList(question.answerRaw);

  const handleSelect = (optionId: string) => {
    if (disabled) return;
    playSynthGameSound('select', soundEnabled);
    onUpdate({ selectedOption: optionId });
  };

  return (
    <div className="space-y-3">
      {displayOptions.map((option, idx) => {
        const isSelected = answer?.selectedOption === option.id;
        const isCorrectOpt = correct.includes(option.id);

        return (
          <OptionChoiceItem
            key={option.id}
            option={option}
            selected={isSelected}
            index={idx}
            disabled={disabled}
            showHint={showHint}
            isCorrectOpt={isCorrectOpt}
            onSelect={() => handleSelect(option.id)}
          />
        );
      })}
    </div>
  );
}
