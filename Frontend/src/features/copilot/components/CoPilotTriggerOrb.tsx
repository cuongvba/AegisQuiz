import React, { useEffect } from 'react';
import { Brain, Sparkles } from 'lucide-react';

interface CoPilotTriggerOrbProps {
  isOpen: boolean;
  onToggle: () => void;
}

export const CoPilotTriggerOrb: React.FC<CoPilotTriggerOrbProps> = ({ isOpen, onToggle }) => {
  // Listen for global shortcut Ctrl + J or Cmd + J
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && (e.key === 'j' || e.key === 'J')) {
        e.preventDefault();
        onToggle();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onToggle]);

  if (isOpen) return null;

  return (
    <div className="fixed bottom-6 right-6 z-40">
      <button
        type="button"
        onClick={onToggle}
        title="Mở Aegis Sovereign Co-Pilot (Ctrl + J)"
        className="group relative flex items-center justify-center w-13 h-13 rounded-full bg-gradient-to-tr from-cyan-600 via-indigo-600 to-purple-600 text-white shadow-xl shadow-cyan-900/40 hover:scale-105 active:scale-95 transition-all duration-300 ring-2 ring-cyan-400/40 hover:ring-cyan-300"
      >
        {/* Pulsating ring */}
        <span className="absolute -inset-1 rounded-full bg-gradient-to-r from-cyan-500 to-purple-500 opacity-40 blur-sm group-hover:opacity-75 animate-pulse transition-opacity" />

        {/* Icon & Sparkle */}
        <Brain className="w-6 h-6 text-white relative z-10 transition-transform group-hover:rotate-6" />
        <Sparkles className="w-3.5 h-3.5 text-cyan-200 absolute -top-0.5 -right-0.5 z-10 animate-bounce" />

        {/* Shortcut badge tooltip on hover */}
        <span className="absolute right-full mr-3 px-2 py-1 rounded-md text-[10px] font-mono tracking-wide bg-slate-900 text-cyan-300 border border-slate-700 shadow-md whitespace-nowrap opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none">
          Co-Pilot (Ctrl+J)
        </span>
      </button>
    </div>
  );
};
