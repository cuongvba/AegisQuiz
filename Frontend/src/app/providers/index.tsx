/**
 * AegisQuiz — Root Providers Composition
 * ========================================
 * Compose tất cả providers theo đúng thứ tự dependency.
 *
 * Thứ tự quan trọng:
 *   I18nProvider     — không phụ thuộc gì
 *   QueryProvider    — không phụ thuộc gì
 *   AuthProvider     — dùng QueryClient nếu cần server auth check
 *   TenantProvider   — (Phase 2) phụ thuộc AuthProvider để biết tenantId
 *
 * FSD Layer: app/providers/
 */

import { type ReactNode } from 'react';
import { I18nProvider }   from './I18nProvider';
import { QueryProvider }  from './QueryProvider';
import { AuthProvider }   from './AuthProvider';
import { TenantProvider } from './TenantProvider';

interface AppProvidersProps {
  children: ReactNode;
}

export function AppProviders({ children }: AppProvidersProps) {
  return (
    <I18nProvider>
      <QueryProvider>
        <AuthProvider>
          <TenantProvider>
            {children}
          </TenantProvider>
        </AuthProvider>
      </QueryProvider>
    </I18nProvider>
  );
}
