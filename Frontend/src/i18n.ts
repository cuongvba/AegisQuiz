/**
 * AegisQuiz i18n Configuration — World-Class Multilingual & RTL Sovereign Architecture
 * ====================================================================================
 * Fully supports:
 * - Vietnamese (vi) 🇻🇳
 * - English (en) 🇬🇧
 * - Japanese (ja) 🇯🇵
 * - Korean (ko) 🇰🇷
 * - Arabic (ar) 🇸🇦 with Bi-directional RTL layout
 */

import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import Backend from 'i18next-http-backend';
import LanguageDetector from 'i18next-browser-languagedetector';

i18n
  // Load locale files from /public/locales/{lang}/{ns}.json
  .use(Backend)

  // Auto detect language from querystring, localStorage, navigator
  .use(LanguageDetector)

  // Integrate with React
  .use(initReactI18next)

  .init({
    // Supported languages worldwide
    supportedLngs : ['vi', 'en', 'ja', 'ko', 'ar'],
    fallbackLng   : 'vi',

    // Load path with cache-busting
    backend: {
      loadPath: '/locales/{{lng}}/{{ns}}.json?v=' + (import.meta.env.VITE_APP_VERSION || '20261010_r1'),
    },

    // Language detection order
    detection: {
      order            : ['querystring', 'localStorage', 'navigator'],
      lookupQuerystring: 'lang',
      lookupLocalStorage: 'aegisquiz-lang',
      caches           : ['localStorage'],
    },

    // Namespaces
    ns           : ['translation', 'ai_settings', 'symposium', 'landing', 'dashboard', 'profile'],
    defaultNS    : 'translation',

    interpolation: {
      escapeValue: false, // React handles XSS
    },

    preload: ['vi', 'en'],

    saveMissing: false,
  });

export default i18n;

// ── Language Switcher Helper ───────────────────────────────────────────────
export const SUPPORTED_LANGUAGES = [
  { code: 'vi', name: 'Tiếng Việt', flag: '🇻🇳', dir: 'ltr' },
  { code: 'en', name: 'English',    flag: '🇬🇧', dir: 'ltr' },
  { code: 'ja', name: '日本語',     flag: '🇯🇵', dir: 'ltr' },
  { code: 'ko', name: '한국어',     flag: '🇰🇷', dir: 'ltr' },
  { code: 'ar', name: 'العربية',    flag: '🇸🇦', dir: 'rtl' },
] as const;

export type SupportedLanguage = typeof SUPPORTED_LANGUAGES[number]['code'];

// RTL languages (Arabic, Hebrew, Persian, Urdu)
export const RTL_LANGUAGES: string[] = ['ar', 'he', 'fa', 'ur'];

export function isRTL(lang: string): boolean {
  if (!lang) return false;
  const base = lang.toLowerCase().split('-')[0];
  return RTL_LANGUAGES.includes(base);
}
