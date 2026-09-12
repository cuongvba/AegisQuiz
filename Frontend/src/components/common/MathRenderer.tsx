import React, { useMemo } from 'react';
import katex from 'katex';
import 'katex/dist/katex.min.css';

interface MathRendererProps {
  content: string;
  className?: string;
  inline?: boolean;
}

/**
 * [World-Class Math & Multimodal Media Renderer]
 * Tự động phân tích và render:
 * 1. Phương án hoặc câu hỏi dạng Data URI Base64 ("data:image/png;base64,...") -> Thẻ <img>
 * 2. Hình vẽ minh họa dạng Markdown "![...](data:image/...)" -> Thẻ <img> kèm chú thích
 * 3. Công thức toán học LaTeX inline ($...$) hoặc block ($$...$$) -> Vector SVG siêu tốc qua KaTeX
 * 4. Văn bản hỗn hợp kết hợp giữa chữ viết và công thức toán học
 */
export const MathRenderer: React.FC<MathRendererProps> = ({ content, className = '', inline = false }) => {
  const renderedHtml = useMemo(() => {
    if (!content) return '';

    // Trường hợp 1: Nội dung thuần túy là Data URI ảnh (Phương án dạng ảnh MathType)
    const trimmed = content.trim();
    if (trimmed.startsWith('data:image/') || (trimmed.startsWith('http') && (trimmed.endsWith('.png') || trimmed.endsWith('.jpg') || trimmed.endsWith('.webp')))) {
      return null; // Render trực tiếp bằng thẻ img
    }

    // Trường hợp 1b: Nội dung là file âm thanh (Đề thi kỹ năng nghe Listening)
    if (trimmed.startsWith('data:audio/') || (trimmed.startsWith('http') && (trimmed.endsWith('.mp3') || trimmed.endsWith('.wav') || trimmed.endsWith('.ogg') || trimmed.endsWith('.m4a')))) {
      return `<div class="my-2 p-2 bg-slate-900/80 border border-slate-800 rounded-xl flex items-center gap-2"><audio controls class="w-full h-8"><source src="${trimmed}" /></audio></div>`;
    }

    // Bản đồ token hóa bảo vệ các phần tử media (Ảnh Markdown, Audio, Raw Data URI)
    const mediaTokens: Record<string, string> = {};
    let tokenCounter = 0;

    // Hỗ trợ link audio nhúng dạng [Audio](url)
    let processed = content.replace(/\[Audio(?:\s+Listening)?\]\(((?:data:audio\/|http)[^)]+)\)/gi, (_match, src) => {
      const token = `__AEGIS_AUDIO_TOKEN_${tokenCounter++}__`;
      mediaTokens[token] = `<div class="my-2 p-2.5 bg-slate-900/90 border border-slate-800 rounded-xl flex flex-col gap-1"><span class="text-[11px] font-bold text-sky-400">🎧 Bài nghe Audio:</span><audio controls class="w-full h-8"><source src="${src}" /></audio></div>`;
      return token;
    });

    // Trường hợp 2: Văn bản có chứa markdown image: ![alt](data:image/... hoặc url)
    processed = processed.replace(/!\[([^\]]*)\]\((data:image\/[^;]+;base64,[^)]+|https?:\/\/[^)\s]+)\)/g, (_match, alt, src) => {
      const token = `__AEGIS_IMG_TOKEN_${tokenCounter++}__`;
      mediaTokens[token] = `<div class="my-3 flex flex-col items-center"><img src="${src}" alt="${alt || 'Hình minh họa'}" class="max-h-96 max-w-full rounded-xl border border-slate-700/50 shadow-md object-contain bg-white/5 p-2" /><span class="text-[11px] text-slate-400 mt-1 italic">${alt || 'Hình minh họa'}</span></div>`;
      return token;
    });

    // Trường hợp 2b: [World-Class Smart Ingestion] Nhận diện chuỗi Data URI Base64 dạng raw đứng rời
    processed = processed.replace(/(?<!(?:src=["']|\())(data:image\/[a-zA-Z0-9.+_-]+;base64,[A-Za-z0-9+/=]+)(?!\))/g, (_match, src) => {
      const token = `__AEGIS_IMG_TOKEN_${tokenCounter++}__`;
      mediaTokens[token] = `<img src="${src}" alt="Hình vẽ / Công thức" class="inline-block max-h-14 max-w-full rounded-md border border-slate-700/60 bg-white/95 p-1 mx-1.5 align-middle shadow-sm hover:scale-110 transition-transform cursor-pointer" />`;
      return token;
    });

    // Trường hợp 3: Render công thức toán học LaTeX Block ($$ ... $$)
    processed = processed.replace(/\$\$([\s\S]+?)\$\$/g, (_match, equation) => {
      try {
        return katex.renderToString(equation.trim(), {
          displayMode: true,
          throwOnError: false,
        });
      } catch {
        return `<span class="text-rose-400 font-mono text-xs">[Lỗi công thức: ${equation}]</span>`;
      }
    });

    // Trường hợp 4: Render công thức toán học LaTeX Inline ($ ... $)
    processed = processed.replace(/(?<!\\)\$([^\$\n\r]+?)\$/g, (_match, equation) => {
      try {
        return katex.renderToString(equation.trim(), {
          displayMode: false,
          throwOnError: false,
        });
      } catch {
        return `<span class="text-rose-400 font-mono text-xs">[Lỗi công thức: ${equation}]</span>`;
      }
    });

    // Trường hợp 5: Hỗ trợ gạch chân cho Đề thi Ngoại ngữ (Phát âm, Trọng âm, Tìm lỗi sai)
    processed = processed.replace(/<u>([\s\S]+?)<\/u>/gi, (_match, text) => {
      return `<u class="underline decoration-2 decoration-sky-400 underline-offset-4 font-bold text-sky-300">${text}</u>`;
    });

    // In đậm markdown **text**
    processed = processed.replace(/\*\*([^\*]+?)\*\*/g, '<strong>$1</strong>');

    // Chuyển ký tự xuống dòng thành thẻ <br/> nếu không nằm trong thẻ block
    processed = processed.replace(/\n/g, '<br/>');

    // Phục hồi nguyên vẹn các Media Token (ảnh, audio)
    for (const [token, html] of Object.entries(mediaTokens)) {
      processed = processed.split(token).join(html);
    }

    return processed;
  }, [content]);

  // Nếu là ảnh Data URI thuần túy
  const trimmed = content?.trim() || '';
  if (trimmed.startsWith('data:image/') || (trimmed.startsWith('http') && (trimmed.endsWith('.png') || trimmed.endsWith('.jpg') || trimmed.endsWith('.webp')))) {
    return (
      <div className={`inline-flex items-center my-1 ${className}`}>
        <img
          src={trimmed}
          alt="Công thức / Hình ảnh"
          className="max-h-16 max-w-full object-contain rounded-lg border border-slate-700/40 bg-white/95 p-1 shadow-sm hover:scale-105 transition-transform cursor-zoom-in"
          onClick={(e) => {
            e.stopPropagation();
            window.open(trimmed, '_blank');
          }}
        />
      </div>
    );
  }

  if (inline) {
    return (
      <span
        className={`math-rendered-inline inline align-baseline ${className}`}
        dangerouslySetInnerHTML={{ __html: renderedHtml || content }}
      />
    );
  }

  return (
    <div
      className={`math-rendered leading-relaxed ${className}`}
      dangerouslySetInnerHTML={{ __html: renderedHtml || content }}
    />
  );
};
export default MathRenderer;
