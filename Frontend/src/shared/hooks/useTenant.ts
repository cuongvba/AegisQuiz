/**
 * AegisQuiz — useTenant Hook (Refactored)
 * =========================================
 * Wrapper hook cho TenantContext.
 * Thay thế useAdaptiveTenant.ts cũ — backward compatible API.
 *
 * FSD Layer: shared/hooks/
 *
 * Migration từ useAdaptiveTenant:
 *   TRƯỚC: import { useAdaptiveTenant } from '@/hooks/useAdaptiveTenant';
 *   SAU:   import { useTenant } from '@/shared/hooks/useTenant';
 *          // API giống nhau — drop-in replacement
 */

export { useTenant } from '@/app/providers/TenantProvider';
export type {
  TenantContextValue,
  TenantScale,
  TenantConfig,
  TenantThemeConfig,
  DomainOption,
} from '@/app/providers/TenantProvider';
