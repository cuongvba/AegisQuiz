import api from './api';

export interface QuestionParsingRule {
  ruleCode: string;
  ruleName: string;
  targetExamType: string;
  language: string;
  priority: number;
  isActive: boolean;
  description: string;
  anchorSpec: {
    regexPattern: string;
    questionNumberGroupIndex: number;
    sectionMarkers?: string[];
  };
  stimulusSpec: {
    supportMathTypeWmf: boolean;
    supportVectorSvg: boolean;
    extractInlineImages: boolean;
    readingPassageStartRegex?: string;
    readingPassageRangeRegex?: string;
  };
  interactionSpec: {
    defaultQuestionType: string;
    optionRegexPattern: string;
    optionLabelGroupIndex: number;
    optionContentGroupIndex: number;
    trueFalseSubItemRegex?: string;
    shortAnswerPromptRegex?: string;
  };
  evaluationSpec: {
    answerRegexPattern: string;
    answerGroupIndex: number;
    explanationRegexPattern?: string;
    criticalQuestionMarkerRegex?: string;
    scoreStrategy?: string;
  };
}

export interface ParseSnippetResponse {
  success: boolean;
  matchedRuleCode: string;
  parsedCount: number;
  questions: Array<{
    tempId: string;
    content: string;
    questionType: string;
    options: string[];
    suggestedAnswer: string;
    aiExplanation: string;
    topicCode: string;
    difficulty: number;
    isCritical: boolean;
    subCategory?: string;
  }>;
  parsingLogs: string[];
}

export interface AiRuleAutopilotRequest {
  snippetText: string;
  examHint?: string;
  language?: string;
}

export interface AiRuleAutopilotResponse {
  success: boolean;
  confidenceScore: number;
  reasoning: string;
  generatedRule: QuestionParsingRule;
  testedParseResult: ParseSnippetResponse;
}

export const parsingRulesService = {
  async getAllRules(): Promise<{ total: number; rules: QuestionParsingRule[] }> {
    const res = await api.get('/api/admin/parsing-rules');
    return res.data;
  },

  async reloadRules(customJsonConfig?: string): Promise<{ success: boolean; message: string; currentRulesCount: number }> {
    const res = await api.post('/api/admin/parsing-rules/reload', { customJsonConfig });
    return res.data;
  },

  async testSnippet(snippetText: string, specificRuleCode?: string): Promise<ParseSnippetResponse> {
    const res = await api.post('/api/admin/parsing-rules/test-snippet', {
      snippetText,
      specificRuleCode: specificRuleCode || undefined
    });
    return res.data;
  },

  async aiGenerateRule(request: AiRuleAutopilotRequest): Promise<AiRuleAutopilotResponse> {
    const res = await api.post('/api/admin/parsing-rules/ai-generate-rule', request);
    return res.data;
  },

  async saveCustomRule(rule: QuestionParsingRule): Promise<{ success: boolean; message: string; rule: QuestionParsingRule }> {
    const res = await api.post('/api/admin/parsing-rules/save-custom-rule', rule);
    return res.data;
  }
};
