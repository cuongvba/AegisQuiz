import { useState, useRef, useEffect } from 'react';
import { useLearnerI18n } from '@/lib/i18n';
import { ChevronDown, Globe, Check } from 'lucide-react';
import { SUPPORTED_LANGUAGES } from '@/i18n';

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
        className={`flex items-center gap-1.5 px-2.5 py-1.5 rounded-xl border transition-all duration-200
                   text-xs font-black cursor-pointer select-none
                   ${isOpen 
                     ? 'border-cyan-500/60 bg-cyan-500/10 text-cyan-400 shadow-md shadow-cyan-500/10 ring-2 ring-cyan-500/20' 
                     : 'border-slate-700/80 bg-slate-900/60 hover:bg-slate-800 text-slate-300 hover:text-white hover:border-slate-600'
                   }`}
        aria-haspopup="listbox"
        aria-expanded={isOpen}
        aria-label={`Ngôn ngữ hiện tại: ${currentLangObj.name}`}
      >
        <span className="text-sm leading-none">{currentLangObj.flag}</span>
        <span className="uppercase tracking-wider font-extrabold">{lang}</span>
        <ChevronDown 
          className={`w-3.5 h-3.5 text-slate-400 transition-transform duration-200 ${isOpen ? 'rotate-180 text-cyan-400' : ''}`} 
          aria-hidden="true" 
        />
      </button>

      {/* Controlled Dropdown Menu */}
      {isOpen && (
        <ul
          role="listbox"
          aria-label="Danh sách ngôn ngữ"
          className="absolute end-0 top-full mt-2 w-44 bg-slate-900/95 backdrop-blur-xl
                     rounded-2xl shadow-2xl border border-slate-700/80 p-1.5
                     z-50 animate-in fade-in zoom-in-95 duration-150 origin-top-right
                     ring-1 ring-white/10"
        >
          <div className="px-2.5 py-1.5 text-[10px] font-black uppercase text-slate-400 tracking-wider flex items-center gap-1.5 border-b border-slate-800/80 mb-1">
            <Globe size={11} className="text-cyan-400" />
            <span>Ngôn ngữ • Language</span>
          </div>

          {SUPPORTED_LANGUAGES.map((l) => {
            const isSelected = lang === l.code;
            return (
              <li key={l.code} role="option" aria-selected={isSelected}>
                <button
                  type="button"
                  onClick={() => {
                    if (l.code === 'vi' || l.code === 'en') {
                      setLang(l.code);
                    }
                    setIsOpen(false);
                  }}
                  className={`w-full text-start px-2.5 py-2 text-xs rounded-xl font-bold transition-all
                             flex items-center justify-between group
                             ${isSelected
                               ? 'bg-gradient-to-r from-cyan-500/20 to-indigo-500/20 text-cyan-300 border border-cyan-500/30'
                               : 'text-slate-300 hover:bg-slate-800 hover:text-white'
                             }`}
                >
                  <span className="flex items-center gap-2.5">
                    <span className="text-base leading-none shadow-sm">{l.flag}</span>
                    <span>{l.name}</span>
                  </span>
                  {isSelected && (
                    <Check size={14} className="text-cyan-400" />
                  )}
                </button>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
