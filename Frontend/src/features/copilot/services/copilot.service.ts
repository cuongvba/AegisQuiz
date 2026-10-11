import api from '@/services/api';
import type {
  CoPilotRequest,
  CoPilotResponse,
  CoPilotSlashCommand,
  AiModelOption,
  CoPilotPersona
} from '../types/copilot.types';

export const CoPilotService = {
  /**
   * Gửi prompt hoặc slash command tới Backend Co-Pilot
   */
  async sendMessage(request: CoPilotRequest): Promise<CoPilotResponse> {
    const res = await api.post<CoPilotResponse>('/api/v1/copilot/chat', request);
    return res.data;
  },

  /**
   * Lấy danh sách slash commands được hỗ trợ
   */
  async getCommands(persona?: CoPilotPersona): Promise<CoPilotSlashCommand[]> {
    const params = persona ? { persona } : {};
    const res = await api.get<CoPilotSlashCommand[]>('/api/v1/copilot/commands', { params });
    return res.data;
  },

  /**
   * Lấy danh sách model AI khả dụng từ UniversalAiRouter
   */
  async getActiveModels(): Promise<AiModelOption[]> {
    try {
      const res = await api.get<AiModelOption[]>('/api/v1/copilot/models');
      return res.data;
    } catch {
      return [
        { modelCode: 'claude-3-5-sonnet', displayName: 'Claude 3.5 Sonnet (Khảo Thí)', archetype: 'DeepReasoner' },
        { modelCode: 'gemini-2-5-flash', displayName: 'Gemini 2.5 Flash (Siêu Tốc)', archetype: 'UltraFast' },
        { modelCode: 'deepseek-r1-local', displayName: 'DeepSeek-R1 (Toán & Logic)', archetype: 'DeepReasoner' },
        { modelCode: 'gpt-4o', displayName: 'GPT-4o (Đa phương thức)', archetype: 'MassiveContext' }
      ];
    }
  },

  /**
   * Lấy danh mục các Model AI gom nhóm theo Nhà cung cấp (Provider),
   * tự động phân giải nguồn khóa (User -> Tenant -> System)
   * và đề xuất thông minh mô hình tối ưu theo câu hỏi/chủ đề hiện tại.
   */
  async getGroupedModels(prompt?: string): Promise<import('../types/copilot.types').CoPilotModelGroup[]> {
    try {
      const params = prompt ? { prompt } : {};
      const res = await api.get<import('../types/copilot.types').CoPilotModelGroup[]>('/api/v1/copilot/models/grouped', { params });
      return res.data;
    } catch {
      return [];
    }
  },

  /**
   * Bảo toàn tri thức hội thoại / Artifact vào AegisNotebook
   */
  async exportToNotebook(data: {
    title: string;
    content: string;
    artifactType?: string;
    artifactData?: any;
    targetNotebookId?: string;
  }): Promise<{ success: boolean; notebookId?: string; chapterId?: string; message: string }> {
    const res = await api.post('/api/v1/copilot/export-notebook', data);
    return res.data;
  }
};
