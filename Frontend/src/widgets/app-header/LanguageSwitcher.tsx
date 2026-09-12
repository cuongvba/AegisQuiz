/**
 * AegisQuiz — LanguageSwitcher Widget
 * =====================================
 * Combo box chuyển đổi ngôn ngữ — tách từ MainLayout.tsx.
 * FSD Layer: widgets/app-header/
 */

import { useLearnerI18n } from '@/lib/i18n';
import { ChevronDown } from 'lucide-react';
import { SUPPORTED_LANGUAGES } from '@/i18n';

export function LanguageSwitcher() {
  const { lang, setLang } = useLearnerI18n();

  return (
    <div className="relative group" role="navigation" aria-label="Chọn ngôn ngữ">
      <button
        className="flex items-center gap-1.5 px-3 py-1.5 rounded-full border
                   border-[var(--border-default)] bg-[var(--surface-primary)]
                   hover:bg-[var(--surface-secondary)] transition-colors
                   text-sm font-bold text-[var(--text-primary)] shadow-sm"
        aria-haspopup="listbox"
        aria-label={`Ngôn ngữ hiện tại: ${lang}`}
      >
        <span className="uppercase">{lang}</span>
        <ChevronDown className="w-4 h-4 text-[var(--text-muted)]" aria-hidden="true" />
      </button>

      {/* Dropdown */}
      <ul
        role="listbox"
        aria-label="Danh sách ngôn ngữ"
        className="absolute end-0 top-full mt-2 w-36 bg-[var(--surface-elevated)]
                   rounded-xl shadow-lg border border-[var(--border-subtle)] p-1
                   opacity-0 pointer-events-none
                   group-hover:opacity-100 group-hover:pointer-events-auto
                   transition-all duration-200 z-[var(--z-dropdown)]
                   animate-scale-in origin-top-right"
      >
        {SUPPORTED_LANGUAGES.map((l) => (
          <li key={l.code} role="option" aria-selected={lang === l.code}>
            <button
              onClick={() => {
                if (l.code === 'vi' || l.code === 'en') {
                  setLang(l.code);
                }
              }}
              className={[
                'w-full text-start px-3 py-2 text-sm rounded-lg font-medium transition-colors',
                lang === l.code
                  ? 'bg-[var(--surface-tertiary)] text-[var(--text-brand)]'
                  : 'text-[var(--text-secondary)] hover:bg-[var(--surface-secondary)]',
              ].join(' ')}
            >
              <span className="flex items-center gap-2">
                <span className="text-base leading-none" aria-hidden="true">{l.flag}</span>
                {l.name}
              </span>
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}
