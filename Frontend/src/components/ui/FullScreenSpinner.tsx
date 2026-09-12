import { Loader2 } from 'lucide-react';

export function FullScreenSpinner({ text = 'Preparing something awesome...' }: { text?: string }) {
  return (
    <div className="fixed inset-0 z-50 flex flex-col items-center justify-center bg-slate-50/80 backdrop-blur-sm animate-in fade-in duration-300">
      <Loader2 className="h-10 w-10 animate-spin text-cyan-500 mb-4" />
      <p className="text-sm font-medium text-slate-500 tracking-wide animate-pulse">{text}</p>
    </div>
  );
}