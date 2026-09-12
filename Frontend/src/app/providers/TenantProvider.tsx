/**
 * AegisQuiz — TenantProvider
 * ===========================
 * GIẢI QUYẾT GAP-01 & GAP-02:
 *  - Zero full-page reload khi switch tenant
 *  - CSS variable injection vào document.documentElement
 *  - Multi-industry domain fetching với cache
 *  - RTL support thông qua I18nProvider coordination
 *
 * Flow:
 *   1. Mount → đọc tenantId từ AuthContext
 *   2. Fetch TenantConfig từ API (hoặc dùng preset)
 *   3. applyTenantTheme() → inject CSS vars lên :root
 *   4. Toàn bộ app re-brand ngay lập tức (no reload)
 *
 * FSD Layer: app/providers/
 */

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { apiClient } from '@/shared/api/client';

// ── Types ───────────────────────────────────────────────────────────────────

export type TenantScale =
  | 'individual'
  | 'team'
  | 'classroom'
  | 'organization'
  | 'enterprise'
  | 'global';

export interface TenantThemeConfig {
  brandPrimary:      string;
  brandPrimaryDark:  string;
  brandPrimaryLight: string;
  brandSecondary:    string;
  brandAccent:       string;
  fontHeading:       string;
  fontBody:          string;
  radiusButton:      string;
  radiusCard:        string;
  radiusInput:       string;
  tenantId:          string;
  locale:            string;
  isRTL:             boolean;
}

export interface DomainOption {
  code:             string;
  name:             string;
  description?:     string;
  icon?:            string;
  colorBadge?:      string;
  isSystemStandard?: boolean;
  parentDomainCode?: string;
}

export interface TenantConfig {
  tenantId:   string;
  tenantName: string;
  scaleType:  TenantScale;
  theme:      TenantThemeConfig;
  domains:    DomainOption[];
  logoUrl?:   string;
  locale:     string;
  isRTL:      boolean;
}

export interface TenantContextValue {
  config:           TenantConfig;
  isLoading:        boolean;
  /** Danh sách ngành/lĩnh vực của tenant */
  domains:          DomainOption[];
  /** Scale hiện tại */
  scaleType:        TenantScale;
  tenantName:       string;
  /** Progressive disclosure flags */
  isSimpleMode:     boolean;
  isEducationMode:  boolean;
  isEnterpriseMode: boolean;
  /** Reload tenant config từ server */
  refreshConfig:    () => Promise<void>;
  /** Switch theme ngay lập tức (no reload) */
  applyTheme:       (theme: Partial<TenantThemeConfig>) => void;
}

// ── Default Config ──────────────────────────────────────────────────────────

export const DEFAULT_DOMAINS: DomainOption[] = [
  { code: 'BANKING',    name: 'Tài chính - Ngân hàng',    icon: 'Landmark',     colorBadge: '#059669', isSystemStandard: true },
  { code: 'HEALTHCARE', name: 'Y tế - Sức khỏe',          icon: 'HeartPulse',   colorBadge: '#dc2626', isSystemStandard: true },
  { code: 'EDUCATION',  name: 'Giáo dục & Học thuật',     icon: 'GraduationCap',colorBadge: '#2563eb', isSystemStandard: true },
  { code: 'IT_SECURITY',name: 'An toàn TT & CNTT',        icon: 'ShieldCheck',  colorBadge: '#0284c7', isSystemStandard: true },
  { code: 'HSE',        name: 'An toàn - Môi trường LĐ',  icon: 'HardHat',      colorBadge: '#d97706', isSystemStandard: true },
  { code: 'GOV_DRIVING',name: 'Sát hạch Lái xe Quốc gia', icon: 'Car',         colorBadge: '#7c3aed', isSystemStandard: true },
  { code: 'GENERAL',    name: 'Tổng hợp / Đại cương',     icon: 'Layers',       colorBadge: '#4b5563', isSystemStandard: true },
];

const DEFAULT_THEME: TenantThemeConfig = {
  brandPrimary:      '#06b6d4',
  brandPrimaryDark:  '#0891b2',
  brandPrimaryLight: '#cffafe',
  brandSecondary:    '#3b82f6',
  brandAccent:       '#f59e0b',
  fontHeading:       "'Inter', system-ui, sans-serif",
  fontBody:          "'Inter', system-ui, sans-serif",
  radiusButton:      '9999px',
  radiusCard:        '1rem',
  radiusInput:       '0.75rem',
  tenantId:          'default',
  locale:            'vi',
  isRTL:             false,
};

const DEFAULT_CONFIG: TenantConfig = {
  tenantId:   'default',
  tenantName: 'AegisQuiz',
  scaleType:  'enterprise',
  theme:      DEFAULT_THEME,
  domains:    DEFAULT_DOMAINS,
  locale:     'vi',
  isRTL:      false,
};

// ── Theme Injection ─────────────────────────────────────────────────────────

function applyTenantTheme(theme: TenantThemeConfig): void {
  const root = document.documentElement;

  root.style.setProperty('--tenant-brand-primary',      theme.brandPrimary);
  root.style.setProperty('--tenant-brand-primary-dark', theme.brandPrimaryDark);
  root.style.setProperty('--tenant-brand-primary-light',theme.brandPrimaryLight);
  root.style.setProperty('--tenant-brand-secondary',    theme.brandSecondary);
  root.style.setProperty('--tenant-brand-accent',       theme.brandAccent);
  root.style.setProperty('--tenant-font-heading',       theme.fontHeading);
  root.style.setProperty('--tenant-font-body',          theme.fontBody);
  root.style.setProperty('--tenant-radius-button',      theme.radiusButton);
  root.style.setProperty('--tenant-radius-card',        theme.radiusCard);
  root.style.setProperty('--tenant-radius-input',       theme.radiusInput);
  root.style.setProperty(
    '--tenant-brand-gradient',
    `linear-gradient(135deg, ${theme.brandPrimary}, ${theme.brandSecondary})`,
  );

  // data-tenant attribute → triggers CSS preset overrides in tenant.css
  root.setAttribute('data-tenant', theme.tenantId);

  // RTL direction
  root.dir  = theme.isRTL ? 'rtl' : 'ltr';
  root.lang = theme.locale;
}

// ── Context ────────────────────────────────────────────────────────────────

const TenantContext = createContext<TenantContextValue | null>(null);

// ── Provider ───────────────────────────────────────────────────────────────

/** Singleton domain cache để tránh fetch nhiều lần */
let _domainCache: DomainOption[] | null = null;

export function TenantProvider({ children }: { children: ReactNode }) {
  const [config, setConfig]     = useState<TenantConfig>(DEFAULT_CONFIG);
  const [isLoading, setLoading] = useState(false);

  // Áp dụng theme từ localStorage để tránh FOUC (Flash of Unstyled Content)
  useEffect(() => {
    const raw = localStorage.getItem('tenant_config');
    if (raw) {
      try {
        const saved = JSON.parse(raw) as TenantConfig;
        setConfig(saved);
        applyTenantTheme(saved.theme);
      } catch { /* ignore */ }
    } else {
      applyTenantTheme(DEFAULT_THEME);
    }
  }, []);

  // Fetch domains từ API (một lần, cached)
  useEffect(() => {
    if (_domainCache) {
      setConfig((prev) => ({ ...prev, domains: _domainCache! }));
      return;
    }
    apiClient
      .get<DomainOption[]>('/api/quiz/domains')
      .then((res) => {
        if (Array.isArray(res.data) && res.data.length > 0) {
          _domainCache = res.data;
          setConfig((prev) => ({ ...prev, domains: res.data }));
        }
      })
      .catch(() => {
        _domainCache = DEFAULT_DOMAINS;
      });
  }, []);

  const refreshConfig = useCallback(async () => {
    setLoading(true);
    try {
      const tenantId = localStorage.getItem('tenant_id') || localStorage.getItem('tenantId');
      if (!tenantId) return;
      const res = await apiClient.get<TenantConfig>(`/api/tenants/${tenantId}/config`);
      const newConfig = res.data;
      setConfig(newConfig);
      applyTenantTheme(newConfig.theme);
      localStorage.setItem('tenant_config', JSON.stringify(newConfig));
    } catch { /* keep existing */ }
    finally { setLoading(false); }
  }, []);

  const applyTheme = useCallback((overrides: Partial<TenantThemeConfig>) => {
    setConfig((prev) => {
      const newTheme = { ...prev.theme, ...overrides };
      const newConfig = { ...prev, theme: newTheme };
      applyTenantTheme(newTheme);
      localStorage.setItem('tenant_config', JSON.stringify(newConfig));
      return newConfig;
    });
  }, []);

  const scaleType  = config.scaleType;
  const value = useMemo<TenantContextValue>(() => ({
    config,
    isLoading,
    domains:          config.domains,
    scaleType,
    tenantName:       config.tenantName,
    isSimpleMode:     scaleType === 'individual' || scaleType === 'team',
    isEducationMode:  scaleType === 'classroom'  || scaleType === 'organization',
    isEnterpriseMode: scaleType === 'enterprise' || scaleType === 'global',
    refreshConfig,
    applyTheme,
  }), [config, isLoading, scaleType, refreshConfig, applyTheme]);

  return <TenantContext.Provider value={value}>{children}</TenantContext.Provider>;
}

// ── Hook ───────────────────────────────────────────────────────────────────

export function useTenant(): TenantContextValue {
  const ctx = useContext(TenantContext);
  if (!ctx) throw new Error('[AegisQuiz] useTenant phải được dùng bên trong <TenantProvider>');
  return ctx;
}
