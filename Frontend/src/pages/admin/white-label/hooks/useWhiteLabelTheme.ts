import { useState, useCallback } from 'react';
import { DEFAULT_THEME_TOKENS, injectThemeTokens, type ThemeTokens } from '@/shared/hooks/useThemeInjector';

export interface WhiteLabelConfig extends ThemeTokens {
  organizationName: string;
  logoUrl: string;
  customDomain: string;
}

const STORAGE_KEY = 'aegisquiz_tenant_theme_tokens';

export function useWhiteLabelTheme() {
  const [config, setConfig] = useState<WhiteLabelConfig>(() => {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      if (saved) {
        return {
          organizationName: 'Tập đoàn Ngân hàng Quốc tế',
          logoUrl: 'https://cdn.aegisquiz.com/tenant-demo-logo.svg',
          customDomain: 'quiz.mybank.vn',
          ...JSON.parse(saved),
        };
      }
    } catch {
      // Fallback
    }
    return {
      organizationName: 'Tập đoàn Ngân hàng Quốc tế',
      logoUrl: 'https://cdn.aegisquiz.com/tenant-demo-logo.svg',
      customDomain: 'quiz.mybank.vn',
      ...DEFAULT_THEME_TOKENS,
    };
  });

  const [isSaved, setIsSaved] = useState(false);

  const updateConfig = useCallback((partial: Partial<WhiteLabelConfig>) => {
    setConfig((prev) => {
      const next = { ...prev, ...partial };
      injectThemeTokens(next);
      return next;
    });
    setIsSaved(false);
  }, []);

  const saveConfig = useCallback(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(config));
    injectThemeTokens(config);
    setIsSaved(true);
    setTimeout(() => setIsSaved(false), 3000);
  }, [config]);

  const resetDefaults = useCallback(() => {
    const next: WhiteLabelConfig = {
      organizationName: 'AegisQuiz Enterprise',
      logoUrl: '',
      customDomain: '',
      ...DEFAULT_THEME_TOKENS,
    };
    setConfig(next);
    injectThemeTokens(next);
    localStorage.removeItem(STORAGE_KEY);
    setIsSaved(true);
    setTimeout(() => setIsSaved(false), 2000);
  }, []);

  return {
    config,
    updateConfig,
    saveConfig,
    resetDefaults,
    isSaved,
  };
}
