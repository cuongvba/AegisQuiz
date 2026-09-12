/**
 * AegisQuiz — Shared API Types
 * ================================
 * Typed contracts cho HTTP communication.
 * FSD Layer: shared/api/
 */

// ── Error Types ─────────────────────────────────────────────────────────────

export interface ApiError {
  /** HTTP status code (0 = network error) */
  status: number;
  /** Human-readable error message */
  message: string;
  /** Field-level validation errors từ backend */
  errors?: Record<string, string[]>;
  /** true khi không có internet / server unreachable */
  isNetworkError: boolean;
  /** true khi status === 401 */
  isAuthError: boolean;
  /** true khi status === 403 */
  isForbidden: boolean;
  /** true khi status === 404 */
  isNotFound: boolean;
  /** true khi status >= 500 */
  isServerError: boolean;
}

/** Type guard — kiểm tra xem error có phải ApiError không */
export function isApiError(error: unknown): error is ApiError {
  return (
    typeof error === 'object' &&
    error !== null &&
    'status' in error &&
    'message' in error &&
    'isNetworkError' in error
  );
}

// ── Pagination ───────────────────────────────────────────────────────────────

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface PaginationParams {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

// ── Common Response Wrappers ──────────────────────────────────────────────────

/** Standard success envelope từ AegisQuiz backend */
export interface ApiResponse<T = void> {
  success: boolean;
  data?: T;
  message?: string;
}

/** Upload response */
export interface UploadResponse {
  fileId: string;
  fileName: string;
  fileSize: number;
  mimeType: string;
  url?: string;
}

// ── Request Config Helpers ───────────────────────────────────────────────────

export interface MultipartFormOptions {
  onUploadProgress?: (percent: number) => void;
}
