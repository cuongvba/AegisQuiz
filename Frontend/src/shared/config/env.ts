/**
 * AegisQuiz — Type-Safe Environment Variables
 * =============================================
 * SSOT cho tất cả VITE_* env vars.
 * KHÔNG đọc import.meta.env trực tiếp ở bất kỳ nơi nào khác.
 *
 * Cách dùng:
 *   import { env } from '@/shared/config/env';
 *   const url = env.API_URL;
 */

function getEnv(key: string, defaultValue?: string): string {
  const value = (import.meta.env as Record<string, string | undefined>)[key];
  if (value !== undefined && value !== '') return value;
  if (defaultValue !== undefined) return defaultValue;
  if (import.meta.env.DEV) {
    console.warn(`[env] Missing environment variable: ${key}`);
  }
  return '';
}

export const env = {
  /** Backend API base URL. Trống = relative (proxied qua Vite) */
  API_URL: getEnv('VITE_API_URL', ''),

  /** Auth Hub URL — dùng cho redirect login (mặc định daotao.dehoc.vn trên production) */
  AUTH_HUB_URL: getEnv('VITE_AUTH_HUB_URL', typeof window !== 'undefined' && window.location.hostname.endsWith('dehoc.vn') ? 'https://daotao.dehoc.vn' : (typeof window !== 'undefined' ? window.location.origin : '')),

  /** Môi trường hiện tại */
  MODE: import.meta.env.MODE as 'development' | 'production' | 'test',

  /** true khi đang chạy dev server */
  IS_DEV: import.meta.env.DEV,

  /** true khi đang chạy production build */
  IS_PROD: import.meta.env.PROD,

  /** App version — inject từ package.json qua vite.config */
  APP_VERSION: getEnv('VITE_APP_VERSION', '1.0.0'),

  /** Tên app có thể được override bởi tenant */
  APP_NAME: getEnv('VITE_APP_NAME', 'AegisQuiz'),
} as const;

export type EnvConfig = typeof env;
