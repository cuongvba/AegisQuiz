import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { QRCodeSVG } from 'qrcode.react';
import { X, Copy, Check, ExternalLink, Play } from 'lucide-react';
import { ZenRoomLaunchResult } from '../types/zen-studio.types';

interface ZenRoomLauncherModalProps {
  room: ZenRoomLaunchResult;
  onClose: () => void;
}

export const ZenRoomLauncherModal: React.FC<ZenRoomLauncherModalProps> = ({ room, onClose }) => {
  const { t } = useTranslation('creator');
  const [copiedPin, setCopiedPin] = useState(false);
  const [copiedLink, setCopiedLink] = useState(false);

  const fullJoinUrl = window.location.origin + room.joinUrl;

  const handleCopyPin = () => {
    navigator.clipboard.writeText(room.pinCode);
    setCopiedPin(true);
    setTimeout(() => setCopiedPin(false), 2000);
  };

  const handleCopyLink = () => {
    navigator.clipboard.writeText(fullJoinUrl);
    setCopiedLink(true);
    setTimeout(() => setCopiedLink(false), 2000);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-md animate-fadeIn">
      <div className="relative w-full max-w-lg rounded-3xl bg-slate-900 border border-indigo-500/30 p-6 md:p-8 shadow-2xl text-center">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 p-2 text-slate-400 hover:text-white rounded-xl hover:bg-slate-800 transition-colors"
        >
          <X className="w-5 h-5" />
        </button>

        <div className="inline-flex p-3 rounded-2xl bg-indigo-500/20 text-indigo-400 mb-3">
          <Play className="w-6 h-6 fill-current" />
        </div>

        <h3 className="text-xl md:text-2xl font-bold text-white mb-1">
          {t('launcher.modalTitle')}
        </h3>
        <p className="text-xs md:text-sm text-slate-300 mb-6">
          {t('launcher.modalSubtitle')}
        </p>

        {/* QR Code Canvas */}
        <div className="inline-block p-4 bg-white rounded-2xl shadow-xl shadow-indigo-950/50 mb-5">
          <QRCodeSVG value={fullJoinUrl} size={180} level="H" includeMargin />
        </div>

        {/* PIN Code Box */}
        <div className="bg-slate-950/80 rounded-2xl border border-indigo-500/40 p-4 mb-6">
          <div className="text-xs uppercase tracking-widest text-indigo-400 font-semibold mb-1">
            {t('launcher.pinLabel')}
          </div>
          <div className="text-4xl md:text-5xl font-mono font-black tracking-widest text-white mb-3">
            {room.pinCode}
          </div>
          <div className="flex items-center justify-center gap-2">
            <button
              onClick={handleCopyPin}
              className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white transition-colors"
            >
              {copiedPin ? <Check className="w-3.5 h-3.5" /> : <Copy className="w-3.5 h-3.5" />}
              {copiedPin ? t('trialBanner.copied') : t('launcher.copyPin')}
            </button>
            <button
              onClick={handleCopyLink}
              className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 transition-colors border border-slate-700"
            >
              {copiedLink ? <Check className="w-3.5 h-3.5" /> : <ExternalLink className="w-3.5 h-3.5" />}
              {copiedLink ? t('trialBanner.copied') : t('launcher.copyLink')}
            </button>
          </div>
        </div>

        {/* Action Button */}
        <div className="flex flex-col gap-2">
          <a
            href={room.joinUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="w-full py-3 px-4 rounded-xl font-bold text-sm bg-gradient-to-r from-emerald-500 to-teal-500 hover:from-emerald-400 hover:to-teal-400 text-white shadow-lg shadow-emerald-500/20 transition-all flex items-center justify-center gap-2"
          >
            <Play className="w-4 h-4 fill-current" />
            {t('launcher.openArena')}
          </a>
          <button
            onClick={onClose}
            className="w-full py-2.5 text-xs font-medium text-slate-400 hover:text-slate-200 transition-colors"
          >
            {t('launcher.close')}
          </button>
        </div>
      </div>
    </div>
  );
};
