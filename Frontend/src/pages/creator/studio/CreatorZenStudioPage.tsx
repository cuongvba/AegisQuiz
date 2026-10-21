import React from 'react';
import { useTranslation } from 'react-i18next';
import { Sparkles, AlertCircle } from 'lucide-react';
import { useZenStudio } from './hooks/useZenStudio';
import { ZenTrialBanner } from './components/ZenTrialBanner';
import { ZenDropZone } from './components/ZenDropZone';
import { ZenExamPreview } from './components/ZenExamPreview';
import { ZenRoomLauncherModal } from './components/ZenRoomLauncherModal';

export const CreatorZenStudioPage: React.FC = () => {
  const { t } = useTranslation('creator');
  const {
    partnership,
    isUploading,
    uploadResult,
    isLaunching,
    launchResult,
    errorMessage,
    handleUploadFile,
    handleLaunchRoom,
    handleReset,
    handleCloseModal,
    updateQuestion,
  } = useZenStudio();

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 py-10 px-4 sm:px-6 lg:px-8">
      <div className="max-w-5xl mx-auto space-y-8">
        {/* Header Section */}
        <div className="text-center space-y-3">
          <div className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full bg-indigo-500/10 border border-indigo-500/20 text-indigo-400 text-xs font-semibold">
            <Sparkles className="w-3.5 h-3.5" />
            <span>AegisQuiz Creator Ecosystem</span>
          </div>
          <h1 className="text-3xl md:text-5xl font-extrabold tracking-tight text-white">
            {t('title')}
          </h1>
          <p className="text-sm md:text-base text-slate-400 max-w-2xl mx-auto">
            {t('subtitle')}
          </p>
        </div>

        {/* 90-Day Free Trial Banner */}
        <ZenTrialBanner partnership={partnership} />

        {/* Error message alert */}
        {errorMessage && (
          <div className="flex items-center gap-2.5 p-4 rounded-2xl bg-rose-500/15 border border-rose-500/30 text-rose-300 text-xs md:text-sm animate-fadeIn">
            <AlertCircle className="w-5 h-5 shrink-0" />
            <span>{errorMessage}</span>
          </div>
        )}

        {/* Main Workspace: DropZone OR Preview */}
        {!uploadResult ? (
          <ZenDropZone onFileSelected={handleUploadFile} isLoading={isUploading} />
        ) : (
          <ZenExamPreview
            data={uploadResult}
            onLaunch={handleLaunchRoom}
            onReset={handleReset}
            onUpdateQuestion={updateQuestion}
            isLaunching={isLaunching}
          />
        )}

        {/* Realtime Classroom Launcher Modal */}
        {launchResult && (
          <ZenRoomLauncherModal room={launchResult} onClose={handleCloseModal} />
        )}
      </div>
    </div>
  );
};

export default CreatorZenStudioPage;
