/**
 * AegisQuiz — I18nProvider
 * =========================
 * Quản lý RTL/LTR direction + i18n initialization.
 * FSD Layer: app/providers/
 */

import { useEffect, type ReactNode } from 'react';
import { I18nextProvider } from 'react-i18next';
import i18n, { isRTL } from '@/i18n';

interface I18nProviderProps {
  children: ReactNode;
}

export function I18nProvider({ children }: I18nProviderProps) {
  // Sync document direction & lang attribute với ngôn ngữ hiện tại
  useEffect(() => {
    const applyDirection = (lang: string) => {
      const rtl = isRTL(lang);
      document.documentElement.dir  = rtl ? 'rtl' : 'ltr';
      document.documentElement.lang = lang;
    };

    // Apply ngay khi mount
    applyDirection(i18n.language || 'vi');

    // Listen to language change
    const handleChange = (lang: string) => applyDirection(lang);
    i18n.on('languageChanged', handleChange);
    return () => { i18n.off('languageChanged', handleChange); };
  }, []);

  return <I18nextProvider i18n={i18n}>{children}</I18nextProvider>;
}
