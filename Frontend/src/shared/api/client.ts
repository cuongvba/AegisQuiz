/**
 * AegisQuiz — Shared API Client
 * ================================
 * Axios instance tập trung với:
 *  - Auto-inject Bearer token + X-Tenant-ID
 *  - Response error interceptor với typed ApiError
 *  - GUID validation cho tenant ID
 *
 * Refactored từ: src/services/api.ts
 * FSD Layer: shared/api/
 *
 * Cách dùng:
 *   import { apiClient } from '@/shared/api/client';
 *   const data = await apiClient.get<MyType>('/api/something');
 */

import axios, { type AxiosError } from 'axios';
import { env } from '@/shared/config/env';
import type { ApiError } from './types';

const GUID_REGEX = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

/**
 * Resolve Tenant ID từ localStorage.
 * Hỗ trợ multiple key formats cho backward compatibility.
 */
function resolveTenantId(): string | null {
  // Direct key lookups
  const direct = localStorage.getItem('tenant_id') ?? localStorage.getItem('tenantId');
  if (direct && GUID_REGEX.test(direct)) return direct;

  // Nested trong user object
  const userRaw = localStorage.getItem('user');
  if (!userRaw) return null;

  try {
    const user = JSON.parse(userRaw) as Record<string, unknown>;
    const candidate = (user['tenantId'] ?? user['tenant_id']) as string | undefined;
    if (candidate && GUID_REGEX.test(candidate)) return candidate;
  } catch {
    // Malformed user JSON — ignore
  }

  return null;
}

/**
 * Resolve Bearer token từ localStorage.
 */
function resolveToken(): string | null {
  return localStorage.getItem('token');
}

// ── Axios Instance ──────────────────────────────────────────────────────────

export const apiClient = axios.create({
  baseURL: env.API_URL,
  timeout: 30_000,
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  },
});

// ── Request Interceptor — Auth + Tenant Headers ─────────────────────────────

apiClient.interceptors.request.use(
  (config) => {
    const token = resolveToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    const tenantId = resolveTenantId();
    if (tenantId) {
      config.headers['X-Tenant-ID'] = tenantId;
    }

    return config;
  },
  (error) => Promise.reject(error),
);

// ── Response Interceptor — Typed Error Handling ─────────────────────────────

apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<{ message?: string; errors?: Record<string, string[]> }>) => {
    const status = error.response?.status ?? 0;
    const message =
      error.response?.data?.message ??
      error.message ??
      'Đã xảy ra lỗi không xác định';

    const apiError: ApiError = {
      status,
      message,
      errors: error.response?.data?.errors,
      isNetworkError: !error.response,
      isAuthError: status === 401,
      isForbidden: status === 403,
      isNotFound: status === 404,
      isServerError: status >= 500,
    };

    // Auto-logout khi token hết hạn
    if (status === 401) {
      const hasToken = !!localStorage.getItem('token');
      if (hasToken) {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        // Emit custom event để AuthProvider lắng nghe
        window.dispatchEvent(new CustomEvent('aegis:auth:expired'));
      }
    }

    return Promise.reject(apiError);
  },
);

/**
 * @deprecated Dùng apiClient thay vì default export.
 * Giữ lại để backward compat với code cũ import api from '@/services/api'.
 */
export default apiClient;
