import React, { useMemo } from 'react';
import katex from 'katex';
import 'katex/dist/katex.min.css';
// @ts-ignore
import 'katex/dist/contrib/mhchem';
import { sanitizeWmfInText, convertWmfDataUriToSvg, cleanSvgDataUri } from '@/shared/lib/quiz/wmfDecoder';
import { useMathConfig } from '@/shared/context/MathConfigContext';

interface MathRendererProps {
  content: string;
  className?: string;
  inline?: boolean;
  subjectCode?: string;
  fontScale?: number;
}

/**
 * [World-Class Math & Multimodal Media Renderer]
 * Tự động phân tích và render:
 * 1. Phương án hoặc câu hỏi dạng Data URI Base64 ("data:image/png;base64,...") -> Thẻ <img>
 * 2. Hình vẽ minh họa dạng Markdown "![...](data:image/...)" -> Thẻ <img> kèm chú thích
 * 3. Công thức toán học LaTeX inline ($...$) hoặc block ($$...$$) -> Vector SVG siêu tốc qua KaTeX
 * 4. Văn bản hỗn hợp kết hợp giữa chữ viết và công thức toán học
 * 5. Tự động chuyển đổi định dạng WMF (MathType) sang Vector SVG sắc nét
 */
/**
 * Tính toán độ cao động và căn chỉnh trục dọc cho công thức toán học:
 * 1. Công thức 1 dòng thông thường: tăng kích thước font lên 1 mức (~1.80em)
 * 2. Công thức phức tạp có dấu: độ cao > 1.5 lần độ cao thông thường (~2.80em - 3.20em)
 * 3. Công thức phân số, ma trận n chiều: độ cao co dãn theo số tầng/chiều
 * 4. Công thức dài: chiều dài tự nhiên bao phủ toàn bộ (w-auto max-w-full)
 */
function getFormulaMetrics(src: string, customBaseUnit?: number): { heightEm: number; isComplex: boolean; vAlign: string } {
  let lineFactor = 1.0;
  let heightEm = 1.45;
  let vAlign = '-0.30em';

  try {
    if (src.startsWith('data:image/svg+xml;base64,')) {
      const base64Str = src.slice('data:image/svg+xml;base64,'.length);
      const head = atob(base64Str.slice(0, 1200));
      const lfMatch = head.match(/data-line-factor=["']([\d.]+)["']/i);
      if (lfMatch) {
        lineFactor = parseFloat(lfMatch[1]);
      } else {
        // Fallback: Tính lineFactor trực tiếp từ viewBox và font-size nội tại của SVG
        const vbMatch = head.match(/viewBox=["'][\d.-]+\s+[\d.-]+\s+([\d.-]+)\s+([\d.-]+)["']/i);
        const fsMatches = [...head.matchAll(/font-size:([\d.]+)/gi)].map(m => parseFloat(m[1]));
        if (vbMatch && fsMatches.length > 0) {
          fsMatches.sort((a, b) => a - b);
          const medianFs = fsMatches[Math.floor(fsMatches.length / 2)];
          const h = parseFloat(vbMatch[2]);
          if (medianFs > 0) {
            lineFactor = Math.max(0.8, Math.min(10.0, h / medianFs));
          }
        }
      }
    }
  } catch {}

  // Chiều cao chuẩn hóa: lineFactor * BASE_UNIT (mặc định 1.45 hoặc cấu hình động theo Context/Subject)
  const baseUnit = (customBaseUnit && customBaseUnit > 0) ? customBaseUnit : 1.45;
  heightEm = Math.max(1.20, lineFactor * baseUnit);
  vAlign = `-${((heightEm - 1.0) * 0.48 + 0.12).toFixed(2)}em`;

  return { heightEm, isComplex: lineFactor > 1.8, vAlign };
}

export const MathRenderer: React.FC<MathRendererProps> = ({
  content,
  className = '',
  inline = false,
  subjectCode,
  fontScale
}) => {
  const mathConfig = useMathConfig();
  const effectiveBaseUnit = fontScale ||
    (subjectCode && mathConfig.settings.subjectProfiles?.[subjectCode]?.baseFontUnit) ||
    mathConfig.baseFontUnit ||
    1.45;

  const sanitizedContent = useMemo(() => {
    return sanitizeWmfInText(content || '');
  }, [content]);

  const renderedHtml = useMemo(() => {
    if (!sanitizedContent) return '';

    // Trường hợp 1: Nội dung thuần túy là 1 Data URI ảnh (Phương án dạng ảnh MathType)
    const trimmed = sanitizedContent.trim().replace(/[\.\s\t]+$/, '');
    if (((trimmed.startsWith('data:image/') || trimmed.startsWith('data:image/x-wmf') || trimmed.startsWith('data:image/wmf')) && !/\s/.test(trimmed)) || (trimmed.startsWith('http') && !/\s/.test(trimmed) && (trimmed.endsWith('.png') || trimmed.endsWith('.jpg') || trimmed.endsWith('.webp') || trimmed.endsWith('.svg')))) {
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
    let processed = sanitizedContent.replace(/\[Audio(?:\s+Listening)?\]\(((?:data:audio\/|http)[^)]+)\)/gi, (_match, src) => {
      const token = `__AEGIS_AUDIO_TOKEN_${tokenCounter++}__`;
      mediaTokens[token] = `<div class="my-2 p-2.5 bg-slate-900/90 border border-slate-800 rounded-xl flex flex-col gap-1"><span class="text-[11px] font-bold text-sky-400">🎧 Bài nghe Audio:</span><audio controls class="w-full h-8"><source src="${src}" /></audio></div>`;
      return token;
    });

    // Trường hợp 2: Văn bản có chứa markdown image: ![alt](data:image/... hoặc url)
    processed = processed.replace(/!\[([^\]]*)\]\((data:image\/[^;]+;base64,[^)]+|https?:\/\/[^)\s]+)\)/g, (_match, alt, src) => {
      const safeSrc = (src.startsWith('data:image/x-wmf') || src.startsWith('data:image/wmf')) ? convertWmfDataUriToSvg(src) : src;
      const token = `__AEGIS_IMG_TOKEN_${tokenCounter++}__`;
      mediaTokens[token] = `<div class="my-3 flex flex-col items-center"><div class="inline-block p-2.5 rounded-2xl bg-white border border-slate-200 shadow-md max-w-full"><img src="${safeSrc}" alt="${alt || 'Hình minh họa'}" class="max-h-96 max-w-full rounded-xl object-contain bg-white block mx-auto" /></div><span class="text-[11px] text-slate-400 mt-1.5 italic">${alt || 'Hình minh họa'}</span></div>`;
      return token;
    });

    // Trường hợp 2b: [World-Class Smart Ingestion] Nhận diện chuỗi Data URI Base64 dạng raw đứng rời
    processed = processed.replace(/(?<!(?:src=["']|\())(data:image\/[a-zA-Z0-9.+_-]+;base64,[A-Za-z0-9+/=]+)(?!\))/g, (_match, src) => {
      const safeSrc = (src.startsWith('data:image/x-wmf') || src.startsWith('data:image/wmf'))
        ? convertWmfDataUriToSvg(src)
        : (src.startsWith('data:image/svg+xml') ? cleanSvgDataUri(src) : src);
      const token = `__AEGIS_IMG_TOKEN_${tokenCounter++}__`;
      const metrics = getFormulaMetrics(safeSrc, effectiveBaseUnit);
      mediaTokens[token] = `<img src="${safeSrc}" alt="Công thức" style="height: ${metrics.heightEm.toFixed(2)}em; max-height: var(--math-max-height, 14em); width: auto; max-width: 100%; vertical-align: ${metrics.vAlign}; object-fit: contain;" class="inline-block rounded px-2 py-0.5 mx-1 align-middle border border-slate-300 bg-white shadow-sm hover:scale-105 transition-transform cursor-zoom-in" />`;
      return token;
    });

    // Trường hợp 2c: Tự động bọc $ cho các phương trình hóa học dạng \ce{...} đứng độc lập
    processed = processed.replace(/(?<!\$)\\ce\{([^\}]+)\}(?!\$)/g, '$\\ce{$1}$');

    // Trường hợp 3: Render công thức toán học LaTeX Block ($$ ... $$)
    processed = processed.replace(/\$\$([\s\S]+?)\$\$/g, (_match, equation) => {
      try {
        const normalized = equation
          .trim()
          .replace(/\\vec\{([^{}]+)\}/g, '\\overrightarrow{$1}')
          .replace(/\\vec\s+([a-zA-Z0-9]+)/g, '\\overrightarrow{$1}')
          .replace(/\\hat\{([^{}]+)\}/g, '\\widehat{$1}')
          .replace(/\\hat\s+([a-zA-Z0-9]+)/g, '\\widehat{$1}')
          .replace(/\\angle\s*\{?([A-Z]{3})\}?/g, '\\widehat{$1}')
          .replace(/∠\s*([A-Z]{3})/g, '\\widehat{$1}')
          .replace(/[\u20D7⃗]/g, '');
        const rendered = katex.renderToString(normalized, {
          displayMode: true,
          throwOnError: false,
          strict: false,
        });
        return `<div class="block-math my-2 text-[1.1em] overflow-x-auto overflow-y-hidden py-1 tracking-wide">${rendered}</div>`;
      } catch {
        return `<span class="text-rose-400 font-mono text-xs">[Lỗi công thức: ${equation}]</span>`;
      }
    });

    // Trường hợp 4: Render công thức toán học LaTeX Inline ($ ... $)
    processed = processed.replace(/(?<!\\)\$([^\$\n\r]+?)\$/g, (_match, equation) => {
      try {
        const normalized = equation
          .trim()
          .replace(/\\vec\{([^{}]+)\}/g, '\\overrightarrow{$1}')
          .replace(/\\vec\s+([a-zA-Z0-9]+)/g, '\\overrightarrow{$1}')
          .replace(/\\hat\{([^{}]+)\}/g, '\\widehat{$1}')
          .replace(/\\hat\s+([a-zA-Z0-9]+)/g, '\\widehat{$1}')
          .replace(/\\angle\s*\{?([A-Z]{3})\}?/g, '\\widehat{$1}')
          .replace(/∠\s*([A-Z]{3})/g, '\\widehat{$1}')
          .replace(/[\u20D7⃗]/g, '');

        // Tự động nâng cấp lên \displaystyle cho các cấu trúc đa tầng (phân số, căn thức, tích phân, tổng, giới hạn...)
        // Giúp các ký tự (a, b, c...) giữ nguyên 100% kích thước font chuẩn, không bị co nhỏ (70% scriptstyle) so với font chữ chung
        const hasComplexStructures = /\\(?:frac|dfrac|cfrac|sqrt|int|iint|iiint|oint|sum|prod|lim|matrix|pmatrix|bmatrix|vmatrix|cases)/.test(normalized);
        const formulaToRender = hasComplexStructures && !normalized.includes('\\displaystyle')
          ? `{\\displaystyle ${normalized}}`
          : normalized;

        const rendered = katex.renderToString(formulaToRender, {
          displayMode: false,
          throwOnError: false,
          strict: false,
        });
        return `<span class="inline-math font-medium tracking-wide mx-0.5">${rendered}</span>`;
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
  }, [sanitizedContent]);

  // Nếu là ảnh Data URI thuần túy (duy nhất 1 ảnh không kèm ký tự khác)
  let trimmed = sanitizedContent?.trim()?.replace(/[\.\s\t]+$/, '') || '';
  if (trimmed.startsWith('data:image/x-wmf') || trimmed.startsWith('data:image/wmf')) {
    trimmed = convertWmfDataUriToSvg(trimmed);
  } else if (trimmed.startsWith('data:image/svg+xml')) {
    trimmed = cleanSvgDataUri(trimmed);
  }

  if (((trimmed.startsWith('data:image/') || trimmed.startsWith('data:image/x-wmf')) && !/\s/.test(trimmed)) || (trimmed.startsWith('http') && !/\s/.test(trimmed) && (trimmed.endsWith('.png') || trimmed.endsWith('.jpg') || trimmed.endsWith('.webp') || trimmed.endsWith('.svg')))) {
    const isSvgMath = trimmed.startsWith('data:image/svg+xml');
    const metrics = isSvgMath ? getFormulaMetrics(trimmed, effectiveBaseUnit) : null;
    return (
      <div className={`inline-flex items-center my-1 mx-1 ${className}`}>
        <img
          src={trimmed}
          alt="Công thức / Hình ảnh"
          style={metrics ? { height: `${metrics.heightEm.toFixed(2)}em`, maxHeight: '14em', width: 'auto', maxWidth: '100%', verticalAlign: metrics.vAlign, objectFit: 'contain' } : undefined}
          className={`${metrics ? '' : 'h-auto min-h-[2.2em] max-h-36 w-auto max-w-full'} object-contain rounded-lg border border-slate-300 bg-white px-2 py-0.5 shadow-sm hover:scale-105 transition-transform cursor-zoom-in`}
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
        dangerouslySetInnerHTML={{ __html: renderedHtml || sanitizedContent }}
      />
    );
  }

  return (
    <div
      className={`math-rendered leading-relaxed ${className}`}
      dangerouslySetInnerHTML={{ __html: renderedHtml || sanitizedContent }}
    />
  );
};
export default MathRenderer;
