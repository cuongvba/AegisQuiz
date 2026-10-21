import React, { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { FileUp, Loader2, Sparkles, FileText } from 'lucide-react';

interface ZenDropZoneProps {
  onFileSelected: (file: File) => void;
  isLoading: boolean;
}

export const ZenDropZone: React.FC<ZenDropZoneProps> = ({ onFileSelected, isLoading }) => {
  const { t } = useTranslation('creator');
  const [isDragOver, setIsDragOver] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(true);
  };

  const handleDragLeave = () => {
    setIsDragOver(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      const file = e.dataTransfer.files[0];
      if (file.name.endsWith('.docx') || file.name.endsWith('.doc')) {
        onFileSelected(file);
      }
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      onFileSelected(e.target.files[0]);
    }
  };

  return (
    <div
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
      onClick={() => !isLoading && fileInputRef.current?.click()}
      className={`relative group cursor-pointer overflow-hidden rounded-3xl border-2 border-dashed transition-all duration-300 p-10 md:p-14 text-center ${
        isDragOver
          ? 'border-indigo-400 bg-indigo-950/40 shadow-2xl shadow-indigo-500/20 scale-[1.01]'
          : 'border-slate-700/80 hover:border-indigo-500/60 bg-slate-900/40 hover:bg-slate-900/70'
      }`}
    >
      <input
        ref={fileInputRef}
        type="file"
        accept=".docx,.doc"
        onChange={handleFileChange}
        className="hidden"
        disabled={isLoading}
      />

      <div className="flex flex-col items-center justify-center max-w-xl mx-auto space-y-4">
        <div className={`p-5 rounded-2xl transition-transform duration-300 ${
          isDragOver ? 'bg-indigo-500/30 text-indigo-300 scale-110' : 'bg-slate-800 text-indigo-400 group-hover:scale-105 group-hover:bg-indigo-500/20'
        }`}>
          {isLoading ? (
            <Loader2 className="w-10 h-10 animate-spin text-indigo-400" />
          ) : isDragOver ? (
            <Sparkles className="w-10 h-10 animate-pulse text-indigo-300" />
          ) : (
            <FileUp className="w-10 h-10" />
          )}
        </div>

        <div className="space-y-1.5">
          <h3 className="text-xl md:text-2xl font-bold text-white tracking-tight">
            {isLoading ? t('dropZone.processing') : isDragOver ? t('dropZone.dropActive') : t('dropZone.title')}
          </h3>
          <p className="text-sm text-slate-400 max-w-md mx-auto leading-relaxed">
            {t('dropZone.hint')}
          </p>
        </div>

        {!isLoading && (
          <div className="pt-2 flex items-center gap-3">
            <span className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-indigo-600 to-violet-600 hover:from-indigo-500 hover:to-violet-500 text-white font-medium text-sm shadow-lg shadow-indigo-500/20 transition-all">
              <FileText className="w-4 h-4" />
              {t('dropZone.browseButton')}
            </span>
          </div>
        )}
      </div>

      <div className="absolute inset-0 pointer-events-none rounded-3xl bg-gradient-to-t from-indigo-500/5 via-transparent to-transparent opacity-0 group-hover:opacity-100 transition-opacity" />
    </div>
  );
};
