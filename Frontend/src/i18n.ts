/**
 * AegisQuiz i18n Configuration
 * ================================
 * [Phase B — Multi-Language Support]
 * Supports: Vietnamese (vi), English (en)
 * Planned: Japanese (ja), Korean (ko), Arabic (ar - RTL)
 *
 * Usage:
 *   import { useTranslation } from 'react-i18next';
 *   const { t, i18n } = useTranslation();
 *   t('quiz.startExam') // → "Start Exam" or "Bắt đầu thi"
 *   i18n.changeLanguage('en')
 */

import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import Backend from 'i18next-http-backend';
import LanguageDetector from 'i18next-browser-languagedetector';

i18n
  // Load locale files từ /public/locales/{lang}/translation.json
  .use(Backend)

  // Tự động detect ngôn ngữ từ: URL, localStorage, browser settings
  .use(LanguageDetector)

  // Tích hợp với React
  .use(initReactI18next)

  .init({
    // Supported languages
    supportedLngs : ['vi', 'en', 'ja', 'ko'],
    fallbackLng   : 'vi',               // Fallback về tiếng Việt

    // Load path (public/locales/{lng}/{ns}.json)
    backend: {
      loadPath: '/locales/{{lng}}/{{ns}}.json',
    },

    // Language detection order
    detection: {
      order            : ['querystring', 'localStorage', 'navigator'],
      lookupQuerystring: 'lang',
      lookupLocalStorage: 'aegisquiz-lang',
      caches           : ['localStorage'],
    },

    // Default namespace
    ns           : ['translation'],
    defaultNS    : 'translation',

    interpolation: {
      escapeValue: false,   // React handles XSS
    },

    // Performance: Preload ngôn ngữ chính
    preload: ['vi', 'en'],

    // Dev: log missing keys
    saveMissing: import.meta.env.DEV,
    missingKeyHandler: (lngs: readonly string[], ns: string, key: string) => {
      if (import.meta.env.DEV) {
        console.warn(`[i18n] Missing key: "${ns}:${key}" for languages: ${lngs.join(', ')}`);
      }
    },
  });

export default i18n;

// ── Language Switcher Helper ───────────────────────────────────────────────
export const SUPPORTED_LANGUAGES = [
  { code: 'vi', name: 'Tiếng Việt', flag: '🇻🇳' },
  { code: 'en', name: 'English',    flag: '🇬🇧' },
  { code: 'ja', name: '日本語',     flag: '🇯🇵' },  // Coming Phase 3
  { code: 'ko', name: '한국어',     flag: '🇰🇷' },  // Coming Phase 3
] as const;

export type SupportedLanguage = typeof SUPPORTED_LANGUAGES[number]['code'];

// RTL languages (Phase 3 — Arabic, Hebrew)
export const RTL_LANGUAGES: string[] = ['ar', 'he'];

export function isRTL(lang: string): boolean {
  return RTL_LANGUAGES.includes(lang);
}
