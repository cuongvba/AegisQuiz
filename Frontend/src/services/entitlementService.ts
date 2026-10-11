import { apiClient } from '@/shared/api/client';
import type { EntitlementsResponse } from '@/types/entitlement';

export const entitlementService = {
  /**
   * Lấy ma trận bản quyền 4 tầng hiện tại từ Backend
   */
  async getCurrentEntitlements(): Promise<EntitlementsResponse> {
    const response = await apiClient.get<EntitlementsResponse>('/api/entitlements/current');
    return response.data;
  },
};
