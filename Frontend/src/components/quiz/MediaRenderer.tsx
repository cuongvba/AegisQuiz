import { normalizeMediaType } from '@/lib/quiz-helpers';

type Props = {
  url?: string;
  mediaType?: string;
  className?: string;
};

export function MediaRenderer({ url, mediaType, className }: Props) {
  if (!url || !url.trim()) return null;

  const type = normalizeMediaType(mediaType);

  if (type.startsWith('image')) {
    return <img src={url} alt="question media" className={`h-auto w-full rounded-2xl border border-white/40 object-cover ${className ?? ''}`} />;
  }

  if (type.startsWith('audio')) {
    return <audio controls src={url} className={`w-full ${className ?? ''}`} />;
  }

  if (type.startsWith('video')) {
    return <video controls src={url} className={`h-auto w-full rounded-2xl border border-white/40 ${className ?? ''}`} />;
  }

  return (
    <a
      href={url}
      target="_blank"
      rel="noreferrer"
      className={`inline-flex rounded-xl border border-sky-300 bg-sky-50 px-3 py-2 text-sm font-semibold text-sky-700 hover:bg-sky-100 ${className ?? ''}`}
    >
      Open media link
    </a>
  );
}
