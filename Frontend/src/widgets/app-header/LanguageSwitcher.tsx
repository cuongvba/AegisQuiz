import { useState, useRef, useEffect } from 'react';
import { useLearnerI18n } from '@/lib/i18n';
import { ChevronDown, Globe, Check } from 'lucide-react';
import i18n, { SUPPORTED_LANGUAGES, isRTL } from '@/i18n';
import { FlagIcon } from '@/shared/ui/FlagIcon';

export function LanguageSwitcher() {
  const { lang, setLang } = useLearnerI18n();
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  // Close when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen]);

  const currentLangObj = SUPPORTED_LANGUAGES.find((l) => l.code === lang) ?? SUPPORTED_LANGUAGES[0];

  return (
    <div ref={containerRef} className="relative" role="navigation" aria-label="Chọn ngôn ngữ">
      <button
        type="button"
        onClick={() => setIsOpen((prev) => !prev)}
        className={`flex items-center gap-2 px-2.5 py-1.5 rounded-full border transition-all duration-200
                   text-xs font-mono font-bold cursor-pointer select-none
                   ${isOpen 
                     ? 'border-sky-500/60 bg-sky-500/15 text-sky-400 shadow-md shadow-sky-500/10 ring-2 ring-sky-500/20' 
                     : 'border-slate-800 bg-slate-900/90 hover:bg-slate-800 text-sky-400 hover:text-sky-300 hover:border-slate-700'
                   }`}
        aria-haspopup="listbox"
        aria-expanded={isOpen}
        aria-label={`Ngôn ngữ: ${currentLangObj.name}`}
      >
        <div className="w-5 h-3.5 rounded-[3px] overflow-hidden shadow-sm shrink-0 ring-1 ring-white/20 flex items-center justify-center">
          <FlagIcon code={lang} className="w-full h-full object-cover" />
        </div>
        <span className="uppercase tracking-wider font-black text-xs text-slate-100">{lang}</span>
        <ChevronDown size={11} className={`text-slate-400 transition-transform duration-200 ${isOpen ? 'rotate-180 text-sky-400' : ''}`} />
      </button>

      {/* Controlled Dropdown Menu */}
      {isOpen && (
        <div
          className="absolute end-0 top-full mt-2 w-56 bg-slate-900/95 backdrop-blur-xl
                     rounded-2xl shadow-2xl border border-slate-700/80 p-1.5
                     z-50 animate-in fade-in zoom-in-95 duration-150 origin-top-right
                     ring-1 ring-white/10"
        >
          <div className="px-2.5 py-1.5 text-[10px] font-black uppercase text-slate-400 tracking-wider flex items-center justify-between border-b border-slate-800/80 mb-1">
            <span className="flex items-center gap-1.5">
              <Globe size={11} className="text-cyan-400" />
              <span>Language • Ngôn ngữ</span>
            </span>
            <span className="text-[9px] font-mono text-cyan-400 font-bold bg-cyan-500/10 px-1.5 py-0.5 rounded-full border border-cyan-500/20">
              13 Global
            </span>
          </div>

          <ul
            role="listbox"
            aria-label="Danh sách ngôn ngữ"
            className="max-h-72 overflow-y-auto space-y-0.5 pr-0.5 scrollbar-thin scrollbar-thumb-slate-700 scrollbar-track-transparent"
          >
            {SUPPORTED_LANGUAGES.map((l) => {
              const isSelected = lang === l.code;
              return (
                <li key={l.code} role="option" aria-selected={isSelected}>
                  <button
                    type="button"
                    onClick={async () => {
                      const code = l.code;
                      setLang(code as any);
                      try {
                        await i18n.changeLanguage(code);
                      } catch (err) {
                        console.warn('[LanguageSwitcher] changeLanguage failed:', err);
                      }
                      const rtl = isRTL(code);
                      document.documentElement.dir = rtl ? 'rtl' : 'ltr';
                      document.documentElement.lang = code;
                      setIsOpen(false);
                    }}
                    className={`w-full text-start px-2.5 py-2 text-xs rounded-xl font-medium transition-all
                               flex items-center justify-between group
                               ${isSelected
                                 ? 'bg-gradient-to-r from-cyan-500/20 to-indigo-500/20 text-cyan-300 border border-cyan-500/30'
                                 : 'text-slate-300 hover:bg-slate-800 hover:text-white'
                               }`}
                  >
                    <span className="flex items-center gap-2.5">
                      <div className="w-5.5 h-3.5 rounded-[3px] overflow-hidden shadow-sm border border-white/10 shrink-0 flex items-center justify-center group-hover:scale-105 transition-transform">
                        <FlagIcon code={l.code} className="w-full h-full object-cover" />
                      </div>
                      <span className="font-semibold">{l.name}</span>
                    </span>
                    <div className="flex items-center gap-1.5">
                      <span className="text-[10px] uppercase font-mono font-bold text-slate-500 group-hover:text-slate-300 transition-colors">
                        {l.code}
                      </span>
                      {isSelected && (
                        <Check size={14} className="text-cyan-400 shrink-0" />
                      )}
                    </div>
                  </button>
                </li>
              );
            })}
          </ul>
        </div>
      )}
    </div>
  );
}
