import api from './api';

export interface NotebookSummaryDto {
  id: string;
  title: string;
  description: string;
  originalFileName: string;
  fileSizeBytes: number;
  totalPages: number;
  status: 'Processing' | 'Structuring' | 'Generating' | 'Ready' | 'Failed' | string;
  domainCode: string;
  bankTopicId?: string;
  bankTopicTitle?: string;
  sourceCount: number;
  chapterCount: number;
  totalQuestions: number;
  createdAt: string;
  errorMessage?: string;
}

export interface NotebookSourceDto {
  id: string;
  notebookId: string;
  title: string;
  originalFileName: string;
  sourceUrl: string;
  fileSizeBytes: number;
  totalPages: number;
  type: 'Pdf' | 'Docx' | 'Excel' | 'WebUrl' | 'PlainText' | string;
  authority: number; // 4: SupremeLegal, 3: Regulatory, 2: InternalSop, 1: ReferenceCase
  status: string;
  uploadedAt: string;
}

export interface NotebookChatMessageDto {
  id: string;
  notebookId: string;
  sender: 'User' | 'Assistant';
  roleName: string;
  message: string;
  citationsJson: string;
  createdAt: string;
}

export interface NotebookChapterDto {
  id: string;
  notebookId: string;
  orderIndex: number;
  title: string;
  startPage: number;
  endPage: number;
  summary: string;
  rawContent: string;
  sourceIds: string[];
  keyPoints: string[];
  pinpointCoordinatesJson: string;
  questionCount: number;
  generatedQuestionIds: string[];
}

export interface NotebookDetailDto extends NotebookSummaryDto {
  sourcePdfUrl: string;
  cognitiveStatsJson: string;
  conflictReportJson: string;
  personaName: string;
  personaRole: number;
  customPersonaPrompt?: string;
  sources: NotebookSourceDto[];
  chapters: NotebookChapterDto[];
  chatMessages: NotebookChatMessageDto[];
}

export interface CreateHubParams {
  title: string;
  description?: string;
  domainCode?: string;
  bankTopicId?: string;
  personaName?: string;
  personaRole?: number;
  customPersonaPrompt?: string;
}

export interface UploadNotebookParams {
  title?: string;
  description?: string;
  domainCode?: string;
  bankTopicId?: string;
  authorityLevel?: number;
}

export interface AddSourceUrlParams {
  url: string;
  title?: string;
  authorityLevel?: number;
}

export interface UpdatePersonaParams {
  personaName: string;
  personaRole: number;
  customPersonaPrompt?: string;
}

export interface GenerateQuizParams {
  questionCount?: number;
  difficultyBloomLevel?: number;
  questionTypes?: string[];
}

export interface GenerateCrossQuizParams {
  totalQuestionCount?: number;
  singleSourcePercent?: number;
  crossSourcePercent?: number;
  targetBloomLevel?: number;
  questionTypes?: string[];
}

export interface PublishToBankParams {
  targetBankTopicId: string;
  chapterId?: string;
  visibilityScope?: 'PRIVATE' | 'COMMUNITY' | 'PUBLIC';
}

export const notebookService = {
  async getNotebooks(page = 1, pageSize = 50): Promise<NotebookSummaryDto[]> {
    const res = await api.get<NotebookSummaryDto[]>('/api/v1/notebooks', {
      params: { page, pageSize }
    });
    return res.data;
  },

  async getNotebookById(id: string): Promise<NotebookDetailDto> {
    const res = await api.get<NotebookDetailDto>(`/api/v1/notebooks/${id}`);
    return res.data;
  },

  async createHub(params: CreateHubParams): Promise<NotebookSummaryDto> {
    const res = await api.post<NotebookSummaryDto>('/api/v1/notebooks/hub', params);
    return res.data;
  },

  async uploadNotebook(
    file: File, 
    params?: UploadNotebookParams, 
    onProgress?: (percent: number) => void
  ): Promise<NotebookSummaryDto> {
    const formData = new FormData();
    formData.append('file', file);
    if (params?.title) formData.append('title', params.title);
    if (params?.description) formData.append('description', params.description);
    if (params?.domainCode) formData.append('domainCode', params.domainCode);
    if (params?.bankTopicId) formData.append('bankTopicId', params.bankTopicId);
    if (params?.authorityLevel) formData.append('authorityLevel', params.authorityLevel.toString());

    const res = await api.post<NotebookSummaryDto>('/api/v1/notebooks/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      onUploadProgress: (progressEvent) => {
        if (progressEvent.total) {
          const percent = Math.round((progressEvent.loaded * 100) / progressEvent.total);
          onProgress?.(percent);
        }
      },
    });
    return res.data;
  },

  async addSourceFile(
    notebookId: string,
    file: File,
    authorityLevel = 2,
    onProgress?: (percent: number) => void
  ): Promise<NotebookSourceDto> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('authorityLevel', authorityLevel.toString());

    const res = await api.post<NotebookSourceDto>(`/api/v1/notebooks/${notebookId}/sources`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      onUploadProgress: (progressEvent) => {
        if (progressEvent.total) {
          const percent = Math.round((progressEvent.loaded * 100) / progressEvent.total);
          onProgress?.(percent);
        }
      },
    });
    return res.data;
  },

  async addSourceUrl(notebookId: string, params: AddSourceUrlParams): Promise<NotebookSourceDto> {
    const res = await api.post<NotebookSourceDto>(`/api/v1/notebooks/${notebookId}/sources/url`, params);
    return res.data;
  },

  async getSources(notebookId: string): Promise<NotebookSourceDto[]> {
    const res = await api.get<NotebookSourceDto[]>(`/api/v1/notebooks/${notebookId}/sources`);
    return res.data;
  },

  async removeSource(notebookId: string, sourceId: string): Promise<{ success: boolean; message: string }> {
    const res = await api.delete<{ success: boolean; message: string }>(`/api/v1/notebooks/${notebookId}/sources/${sourceId}`);
    return res.data;
  },

  async deleteNotebook(id: string): Promise<{ success: boolean; message: string }> {
    const res = await api.delete<{ success: boolean; message: string }>(`/api/v1/notebooks/${id}`);
    return res.data;
  },

  async updatePersona(notebookId: string, params: UpdatePersonaParams): Promise<{ success: boolean; message: string }> {
    const res = await api.put<{ success: boolean; message: string }>(`/api/v1/notebooks/${notebookId}/persona`, params);
    return res.data;
  },

  async sendChatMessage(notebookId: string, message: string): Promise<NotebookChatMessageDto> {
    const res = await api.post<NotebookChatMessageDto>(`/api/v1/notebooks/${notebookId}/chat`, { message });
    return res.data;
  },

  async getChatHistory(notebookId: string): Promise<NotebookChatMessageDto[]> {
    const res = await api.get<NotebookChatMessageDto[]>(`/api/v1/notebooks/${notebookId}/chat`);
    return res.data;
  },

  async clearChatHistory(notebookId: string): Promise<{ success: boolean; message: string }> {
    const res = await api.delete<{ success: boolean; message: string }>(`/api/v1/notebooks/${notebookId}/chat`);
    return res.data;
  },

  async generateChapterQuiz(
    notebookId: string, 
    chapterId: string, 
    params?: GenerateQuizParams
  ): Promise<any[]> {
    const res = await api.post<any[]>(
      `/api/v1/notebooks/${notebookId}/chapters/${chapterId}/generate-quiz`, 
      params || { questionCount: 10, difficultyBloomLevel: 2 }
    );
    return res.data;
  },

  async generateCrossQuiz(
    notebookId: string,
    params?: GenerateCrossQuizParams
  ): Promise<any[]> {
    const res = await api.post<any[]>(
      `/api/v1/notebooks/${notebookId}/generate-cross-quiz`,
      params || { totalQuestionCount: 10, singleSourcePercent: 40, crossSourcePercent: 60, targetBloomLevel: 5 }
    );
    return res.data;
  },

  async publishToBank(
    notebookId: string, 
    params: PublishToBankParams
  ): Promise<{ success: boolean; publishedCount: number; message: string }> {
    const res = await api.post<{ success: boolean; publishedCount: number; message: string }>(
      `/api/v1/notebooks/${notebookId}/publish-to-bank`, 
      params
    );
    return res.data;
  }
};
