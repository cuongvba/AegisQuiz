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
    // Supported languages worldwide (Global 13 Strategic Languages)
    supportedLngs : ['vi', 'en', 'zh', 'es', 'fr', 'ru', 'hi', 'pt', 'id', 'lo', 'ja', 'ko', 'ar'],
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
    ns           : ['admin', 'translation', 'auth', 'ai_settings', 'symposium', 'landing', 'dashboard', 'profile', 'creator', 'admin_questions', 'split_studio', 'white_label', 'paywall', 'admin_payments'],
    defaultNS    : 'translation',

    interpolation: {
      escapeValue: false, // React handles XSS
    },

    react: {
      useSuspense: false,
      bindI18n: 'languageChanged loaded',
      bindI18nStore: 'added removed',
    },

    preload: ['vi', 'en'],

    saveMissing: false,
  });

export default i18n;

// ── Language Switcher Helper ───────────────────────────────────────────────
export const SUPPORTED_LANGUAGES = [
  { code: 'vi', name: 'Tiếng Việt',        flag: '🇻🇳', dir: 'ltr' },
  { code: 'en', name: 'English',           flag: '🇬🇧', dir: 'ltr' },
  { code: 'zh', name: '中文 (Mandarin)',   flag: '🇨🇳', dir: 'ltr' },
  { code: 'es', name: 'Español',           flag: '🇪🇸', dir: 'ltr' },
  { code: 'fr', name: 'Français',          flag: '🇫🇷', dir: 'ltr' },
  { code: 'ru', name: 'Русский',           flag: '🇷🇺', dir: 'ltr' },
  { code: 'hi', name: 'हिन्दी (Hindi)',     flag: '🇮🇳', dir: 'ltr' },
  { code: 'pt', name: 'Português',         flag: '🇧🇷', dir: 'ltr' },
  { code: 'id', name: 'Bahasa Indonesia',  flag: '🇮🇩', dir: 'ltr' },
  { code: 'lo', name: 'ພາສາລາວ (Lao)',      flag: '🇱🇦', dir: 'ltr' },
  { code: 'ja', name: '日本語',            flag: '🇯🇵', dir: 'ltr' },
  { code: 'ko', name: '한국어',            flag: '🇰🇷', dir: 'ltr' },
  { code: 'ar', name: 'العربية',           flag: '🇸🇦', dir: 'rtl' },
] as const;

export type SupportedLanguage = typeof SUPPORTED_LANGUAGES[number]['code'];

// RTL languages (Arabic, Hebrew, Persian, Urdu)
export const RTL_LANGUAGES: string[] = ['ar', 'he', 'fa', 'ur'];

export function isRTL(lang: string): boolean {
  if (!lang) return false;
  const base = lang.toLowerCase().split('-')[0];
  return RTL_LANGUAGES.includes(base);
}
