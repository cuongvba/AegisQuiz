/**
 * AegisQuiz — useTenantTheme Hook
 * =================================
 * Tiện ích truy cập và tùy biến theme động theo Tenant.
 * Cung cấp accessors cho primary/secondary colors, CSS inline styles, và custom injection.
 *
 * FSD Layer: shared/hooks/
 */

import { useMemo } from 'react';
import { useTenant } from './useTenant';
import type { TenantThemeConfig } from '@/entities/tenant/model/types';

export interface UseTenantThemeReturn {
  theme: TenantThemeConfig;
  primaryColor: string;
  secondaryColor: string;
  accentColor: string;
  headingFont: string;
  bodyFont: string;
  buttonRadius: string;
  cardRadius: string;
  inputRadius: string;
  isRTL: boolean;
  applyTheme: (theme: Partial<TenantThemeConfig>) => void;
  getInlineStyleVars: () => React.CSSProperties;
}

export function useTenantTheme(): UseTenantThemeReturn {
  const { config, applyTheme } = useTenant();
  const theme = config.theme;

  const inlineStyleVars = useMemo<React.CSSProperties>(() => {
    return {
      '--color-brand-primary': theme.brandPrimary,
      '--color-brand-primary-dark': theme.brandPrimaryDark,
      '--color-brand-primary-light': theme.brandPrimaryLight,
      '--color-brand-secondary': theme.brandSecondary,
      '--color-brand-accent': theme.brandAccent,
      '--font-family-heading': theme.fontHeading,
      '--font-family-sans': theme.fontBody,
      '--radius-btn': theme.radiusButton,
      '--radius-card': theme.radiusCard,
      '--radius-input': theme.radiusInput,
    } as React.CSSProperties;
  }, [theme]);

  return {
    theme,
    primaryColor: theme.brandPrimary,
    secondaryColor: theme.brandSecondary,
    accentColor: theme.brandAccent,
    headingFont: theme.fontHeading,
    bodyFont: theme.fontBody,
    buttonRadius: theme.radiusButton,
    cardRadius: theme.radiusCard,
    inputRadius: theme.radiusInput,
    isRTL: theme.isRTL || config.isRTL || false,
    applyTheme,
    getInlineStyleVars: () => inlineStyleVars,
  };
}
