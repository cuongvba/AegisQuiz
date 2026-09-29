/**
 * wmfDecoder.ts — Client-side WMF to Vector SVG Converter & Math Glyph Normalizer v8.5
 * ====================================================================================
 * Chuyển đổi tệp Windows Metafile (WMF) từ MathType/Word sang SVG vector chuẩn.
 * Giải quyết triệt để vấn đề:
 * 1. Trình duyệt web không hỗ trợ định dạng 'image/x-wmf'.
 * 2. Lỗi dấu phẩy trên (Prime A', B', C', D') bị hiển thị thành ký tự cent '¢' (0xA2).
 * 3. Lỗi dấu tích phân bị hiển thị thành chữ 'ò' (0xF2).
 * 4. Lỗi dấu vector ghép bởi font MT Extra (u...r) hiển thị thành chữ 'uur'.
 * 5. Lỗi các ký hiệu toán học Symbol font (≤, ≥, ≠, ∞, ⊥, ∈, ∉, ∪, ∩, ∅, →, ⇒, Δ, π, v.v.).
 */

// Bảng tra cứu ánh xạ ký tự Symbol Font (Adobe Symbol / Charset 2) sang Unicode Entity W3C
const SYMBOL_GLYPH_MAP: Record<string, string> = {
  '¢': '&#x2032;', // 0xA2 -> Prime ′ (Dấu phẩy trên A', B', C', D', y')
  'ò': '&#x222B;', // 0xF2 -> Integral ∫ (Dấu tích phân)
  '¥': '&#x221E;', // 0xA5 -> Infinity ∞ (Vô cùng / vô cực)
  'Å': '&#x22A5;', // 0xC5 -> Perpendicular ⊥ (Vuông góc)
  'Î': '&#x2208;', // 0xCE -> Element of ∈ (Thuộc)
  'Ï': '&#x2209;', // 0xCF -> Not element of ∉ (Không thuộc)
  'È': '&#x222A;', // 0xC8 -> Union ∪ (Hợp tập hợp)
  'Ç': '&#x2229;', // 0xC7 -> Intersection ∩ (Giao tập hợp)
  'Æ': '&#x2205;', // 0xC6 -> Empty set ∅ (Tập rỗng)
  'Ì': '&#x2282;', // 0xCC -> Subset ⊂ (Tập con)
  '\u00CD': '&#x2283;', // 0xCD -> Superset ⊃ (Chứa tập)
  'Ú': '&#x2286;', // 0xDA -> Subset or equal ⊆ (Tập con hoặc bằng)
  '£': '&#x2264;', // 0xA3 -> Less than or equal ≤ (Nhỏ hơn hoặc bằng)
  '³': '&#x2265;', // 0xB3 -> Greater than or equal ≥ (Lớn hơn hoặc bằng)
  '¹': '&#x2260;', // 0xB9 -> Not equal ≠ (Khác / Không bằng)
  '®': '&#x2192;', // 0xAE -> Right arrow → (Mũi tên sang phải / tiến tới)
  '¬': '&#x2190;', // 0xAC -> Left arrow ← (Mũi tên sang trái)
  '«': '&#x2194;', // 0xAB -> Left right arrow ↔ (Mũi tên hai chiều)
  'Þ': '&#x21D2;', // 0xDE -> Rightwards double arrow ⇒ (Kéo theo / Suy ra)
  '\u00DB': '&#x21D4;', // 0xDB -> Left right double arrow ⇔ (Tương đương)
  '\u00DC': '&#x21D0;', // 0xDC -> Leftwards double arrow ⇐
  'Ö': '&#x221A;', // 0xD6 -> Square root √ (Căn bậc hai)
  '×': '&#x22C5;', // 0xD7 -> Center dot ⋅ (Tích vô hướng trong MathType)
  '·': '&#x22C5;', // 0xB7 -> Center dot ⋅
  '±': '&#x00B1;', // 0xB1 -> Plus-minus ± (Cộng trừ)
  '°': '&#x00B0;', // 0xB0 -> Degree ° (Độ)
  '»': '&#x2248;', // 0xBB -> Almost equal ≈ (Xấp xỉ)
  'å': '&#x2211;', // 0xE5 -> Summation ∑ (Tổng sigma)
  'Õ': '&#x220F;', // 0xD5 -> Product ∏ (Tích pi)
  'Â': '&#x2220;', // 0xC2 -> Angle ∠ (Góc)
  '@': '&#x2245;', // 0x40 -> Congruent ≅ (Đồng dạng và bằng)
  '~': '&#x223C;', // 0x7E -> Similar ∼ (Đồng dạng)
  '"': '&#x2200;', // 0x22 -> For all ∀ (Với mọi)
  '$': '&#x2203;', // 0x24 -> There exists ∃ (Tồn tại)
  '\\': '&#x2234;', // 0x5C -> Therefore ∴ (Do đó / Vì vậy)
  '\u0082': '\\',   // 0x82 -> Setminus / Backslash \ (Tập xác định R \ {2})
  '‚': '\\',
  'æ': '&#x239B;',  // 0xE6 -> Left paren top hook ⎛
  'ç': '&#x239C;',  // 0xE7 -> Left paren extender ⎜
  'è': '&#x239D;',  // 0xE8 -> Left paren bottom hook ⎝
  'ö': '&#x239E;',  // 0xF6 -> Right paren top hook ⎞
  '÷': '&#x239F;',  // 0xF7 -> Right paren extender ⎟
  'ø': '&#x23A0;'   // 0xF8 -> Right paren bottom hook ⎠
};

// Bảng tra cứu chữ cái Hy Lạp
const GREEK_LETTERS_MAP: Record<string, string> = {
  a: '&#x03B1;', b: '&#x03B2;', g: '&#x03B3;', d: '&#x03B4;', e: '&#x03B5;',
  z: '&#x03B6;', h: '&#x03B7;', q: '&#x03B8;', i: '&#x03B9;', k: '&#x03BA;',
  l: '&#x03BB;', m: '&#x03BC;', n: '&#x03BD;', x: '&#x03BE;', o: '&#x03BF;',
  p: '&#x03C0;', r: '&#x03C1;', s: '&#x03C3;', t: '&#x03C4;', u: '&#x03C5;',
  f: '&#x03C6;', j: '&#x03D5;', c: '&#x03C7;', y: '&#x03C8;', w: '&#x03C9;',
  A: '&#x0391;', B: '&#x0392;', G: '&#x0393;', D: '&#x0394;', E: '&#x0395;',
  Z: '&#x0396;', H: '&#x0397;', Q: '&#x0398;', I: '&#x0399;', K: '&#x039A;',
  L: '&#x039B;', M: '&#x039C;', N: '&#x039D;', X: '&#x039E;', O: '&#x039F;',
  P: '&#x03A0;', R: '&#x03A1;', S: '&#x03A3;', T: '&#x03A4;', U: '&#x03A5;',
  F: '&#x03A6;', C: '&#x03A7;', Y: '&#x03A8;', W: '&#x03A9;'
};

export function convertWmfBytesToSvg(buffer: Uint8Array): string | null {
  if (!buffer || buffer.length < 40) return null;

  const view = new DataView(buffer.buffer, buffer.byteOffset, buffer.byteLength);
  let offset = 0;
  let width = 300;
  let height = 100;
  let minX = 0;
  let minY = 0;

  // Kiểm tra Aldus Placeable Metafile header (0x9AC6CDD7)
  const magic = view.getUint32(0, true);
  if (magic === 0x9AC6CDD7) {
    const left = view.getInt16(6, true);
    const top = view.getInt16(8, true);
    const right = view.getInt16(10, true);
    const bottom = view.getInt16(12, true);
    minX = left;
    minY = top;
    width = Math.max(10, right - left);
    height = Math.max(10, bottom - top);
    offset = 22;
  }

  offset += 18; // Bỏ qua WMF Standard Header

  const svgElements: string[] = [];
  const gdiTable: Record<number, { face: string; size: number; italic: boolean; weight: string }> = {};
  let curFont = 'serif';
  let curFontSize = 14;
  let curFontItalic = false;
  let curFontWeight = 'normal';
  let curTextColor = '#000000';
  let curX = 0;
  let curY = 0;
  let textAlign = 0;

  function getFreeSlot(): number {
    let s = 0;
    while (gdiTable[s] !== undefined && gdiTable[s] !== null) s++;
    return s;
  }

  while (offset < buffer.length - 6) {
    const sizeWords = view.getUint32(offset, true);
    const func = view.getUint16(offset + 4, true);
    if (sizeWords === 0 || func === 0) break;

    switch (func) {
      case 0x0209: // SetTextColor
        {
          const colorRef = view.getUint32(offset + 6, true);
          const r = colorRef & 0xFF;
          const g = (colorRef >> 8) & 0xFF;
          const b = (colorRef >> 16) & 0xFF;
          curTextColor = `rgb(${r},${g},${b})`;
        }
        break;

      case 0x02FB: // CreateFontIndirect
        {
          const h = Math.abs(view.getInt16(offset + 6, true));
          const weight = view.getInt16(offset + 14, true);
          const italic = view.getUint8(offset + 16) !== 0;
          let face = '';
          const faceOffset = offset + 24;
          for (let i = 0; i < 32; i++) {
            if (faceOffset + i >= buffer.length) break;
            const ch = view.getUint8(faceOffset + i);
            if (ch === 0) break;
            face += String.fromCharCode(ch);
          }
          const slot = getFreeSlot();
          gdiTable[slot] = {
            face: face || 'serif',
            size: h || 14,
            italic,
            weight: weight >= 700 ? 'bold' : 'normal'
          };
        }
        break;

      case 0x01F0: // DeleteObject
        {
          const objId = view.getUint16(offset + 6, true);
          delete gdiTable[objId];
        }
        break;

      case 0x012D: // SelectObject
        {
          const objId = view.getUint16(offset + 6, true);
          if (gdiTable[objId]) {
            const f = gdiTable[objId];
            curFont = f.face;
            curFontSize = f.size;
            curFontItalic = f.italic;
            curFontWeight = f.weight;
          }
        }
        break;

      case 0x012E: // SetTextAlign
        {
          textAlign = view.getUint16(offset + 6, true);
        }
        break;

      case 0x0214: // MoveTo
        curY = view.getInt16(offset + 6, true);
        curX = view.getInt16(offset + 8, true);
        break;

      case 0x0213: // LineTo
        {
          const y = view.getInt16(offset + 6, true);
          const x = view.getInt16(offset + 8, true);
          svgElements.push(
            `<line x1="${curX}" y1="${curY}" x2="${x}" y2="${y}" stroke="${curTextColor}" stroke-width="2" />`
          );
          curX = x;
          curY = y;
        }
        break;

      case 0x0325: // Polyline
        {
          const count = view.getInt16(offset + 6, true);
          const pts: string[] = [];
          for (let i = 0; i < count && offset + 8 + i * 4 + 3 < buffer.length; i++) {
            const px = view.getInt16(offset + 8 + i * 4, true);
            const py = view.getInt16(offset + 10 + i * 4, true);
            pts.push(`${px},${py}`);
          }
          if (pts.length > 0) {
            svgElements.push(
              `<polyline points="${pts.join(' ')}" fill="none" stroke="${curTextColor}" stroke-width="2" />`
            );
          }
        }
        break;

      case 0x0324: // Polygon
        {
          // MathType không dùng đa giác, bỏ qua hoàn toàn để triệt tiêu lỗi ô đen
        }
        break;

      case 0x0A32: // ExtTextOut
        {
          const y = view.getInt16(offset + 6, true);
          const x = view.getInt16(offset + 8, true);
          const count = view.getInt16(offset + 10, true);
          const opts = view.getInt16(offset + 12, true);
          let strOffset = offset + 14;
          if ((opts & 0x0006) !== 0) strOffset += 8;

          if (count > 0 && strOffset + count <= buffer.length) {
            const dxOffset = strOffset + count + (count % 2);
            const recEnd = offset + sizeWords * 2;

            const updateCp = ((textAlign & 0x0001) !== 0) || (x === 0 && y === 0);
            const startX = updateCp ? curX : x;
            const startY = updateCp ? curY : y;
            let charX = startX;

            const isSymbol = curFont.toLowerCase().includes('symbol');
            const displayFont = isSymbol
              ? "'Times New Roman', 'Cambria Math', serif"
              : `"${curFont}", Times, serif`;
            const fontStyle = curFontItalic ? 'italic' : 'normal';

            for (let i = 0; i < count; i++) {
              const b = view.getUint8(strOffset + i);
              const ch = String.fromCharCode(b);
              let glyph: string | null = null;

              if (isSymbol) {
                if (SYMBOL_GLYPH_MAP[ch]) {
                  glyph = SYMBOL_GLYPH_MAP[ch];
                } else if (GREEK_LETTERS_MAP[ch]) {
                  glyph = GREEK_LETTERS_MAP[ch];
                }
              }

              if (!glyph) {
                // Thoát ký tự XML
                if (ch === '&') glyph = '&amp;';
                else if (ch === '<') glyph = '&lt;';
                else if (ch === '>') glyph = '&gt;';
                else glyph = ch;
              }

              svgElements.push(
                `<text x="${charX}" y="${startY}" font-family='${displayFont}' font-size="${curFontSize}" font-style="${fontStyle}" font-weight="${curFontWeight}" fill="${curTextColor}">${glyph}</text>`
              );

              // Tịnh tiến vị trí ký tự tiếp theo theo mảng Dx trong WMF
              let dx = 0;
              if (dxOffset + i * 2 + 1 < recEnd) {
                dx = view.getInt16(dxOffset + i * 2, true);
              }

              if (dx !== 0) {
                charX += dx;
              } else {
                charX += Math.round(curFontSize * 0.55);
              }
            }

            if (updateCp) {
              curX = charX;
              curY = startY;
            }
          }
        }
        break;

      case 0x0626: // META_ESCAPE (MathType MTEF binary metadata - bỏ qua hoàn toàn, không phải hình học)
      case 0x0538: // META_POLYPOLYGON (MathType không sử dụng đa giác; bỏ qua để tránh biến byte nhị phân thành ô đen viên thuốc)
        break;
    }

    offset += sizeWords * 2;
  }

  if (svgElements.length === 0) return null;

  const viewBox = `${minX} ${minY} ${width} ${height}`;
  let svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="${viewBox}" style="display:inline-block;vertical-align:middle;max-height:1.8em;max-width:100%;">\n${svgElements.join('\n')}\n</svg>`;
  return sanitizeMathTypeSvgContent(svg);
}

/**
 * Chuyển đổi và làm sạch các chuỗi SVG MathType (Symbol font & MT Extra vector arrow)
 */
export function sanitizeMathTypeSvgContent(svgStr: string): string {
  if (!svgStr) return svgStr;

  let res = svgStr;

  // 0. Triệt tiêu 100% các khối polygon rác che phủ (nguyên nhân gây ô đen viên thuốc)
  res = res.replace(/<polygon\b[^>]*\/?>/gi, '');

  // 1. Ánh xạ các ký tự Symbol còn sót trong các thẻ text
  for (const [char, entity] of Object.entries(SYMBOL_GLYPH_MAP)) {
    const escapedChar = char.replace(/[-/\\^$*+?.()|[\]{}]/g, '\\$&');
    res = res.replace(new RegExp(`(<text[^>]*>)\\s*${escapedChar}\\s*(</text>)`, 'gi'), `$1${entity}$2`);
  }

  // 2. Chuyển đổi các chuỗi ký tự 'u...r' thành mũi tên Vector SVG sắc nét
  try {
    const textElemRegexAll = /<text\s+([^>]*font-size:([\d.]+)[^>]*)transform="matrix\([^)]+\s+([\d.]+)\s+([\d.]+)\)"[^>]*>([\s\S]*?)<\/text>/gi;
    const allItems: Array<{ full: string; attrs: string; fs: number; x: number; y: number; text: string; isItalic: boolean; isMtExtra: boolean }> = [];
    let tm;
    while ((tm = textElemRegexAll.exec(res)) !== null) {
      const full = tm[0];
      const attrs = tm[1];
      const fs = parseFloat(tm[2]);
      const x = parseFloat(tm[3]);
      const y = parseFloat(tm[4]);
      const text = tm[5].trim();
      const isItalic = attrs.toLowerCase().includes('font-style:italic');
      const isMtExtra = attrs.toLowerCase().includes('mt extra');
      allItems.push({ full, attrs, fs, x, y, text, isItalic, isMtExtra });
    }

    if (allItems.length > 0) {
      const urItems = allItems.filter(i => (i.text === 'u' || i.text === 'r') && !i.isItalic);
      const groups: Array<Array<{ full: string; attrs: string; fs: number; x: number; y: number; text: string; isItalic: boolean; isMtExtra: boolean }>> = [];
      let curGroup: Array<{ full: string; attrs: string; fs: number; x: number; y: number; text: string; isItalic: boolean; isMtExtra: boolean }> = [];
      for (const item of urItems) {
        curGroup.push(item);
        if (item.text === 'r') {
          groups.push([...curGroup]);
          curGroup = [];
        }
      }

      for (const grp of groups) {
        const hasShaft = grp.some(g => g.text === 'u');
        const isMtExtra = grp.some(g => g.isMtExtra);
        const rItem = grp[grp.length - 1];

        if (hasShaft || isMtExtra) {
          // Vector đa ký tự (u...r)
          const minX = Math.min(...grp.map(g => g.x));
          const maxX = Math.max(...grp.map(g => g.x));
          const y = grp[0].y;
          const fs = grp[0].fs;

          const arrowY = y - fs * 0.22;
          const x1 = minX;
          const x2 = maxX + fs * 0.20;
          const sw = Math.max(1.5, fs * 0.038);
          const headLen = fs * 0.15;
          const headH = fs * 0.09;

          const arrowSvg = `<g class="math-vector-arrow"><line x1="${x1.toFixed(2)}" y1="${arrowY.toFixed(2)}" x2="${x2.toFixed(2)}" y2="${arrowY.toFixed(2)}" stroke="currentColor" stroke-width="${sw.toFixed(2)}" stroke-linecap="round"/><path d="M ${(x2 - headLen).toFixed(2)} ${(arrowY - headH).toFixed(2)} L ${x2.toFixed(2)} ${arrowY.toFixed(2)} L ${(x2 - headLen).toFixed(2)} ${(arrowY + headH).toFixed(2)}" fill="none" stroke="currentColor" stroke-width="${sw.toFixed(2)}" stroke-linecap="round" stroke-linejoin="round"/></g>`;

          res = res.replace(grp[0].full, arrowSvg);
          for (let j = 1; j < grp.length; j++) {
            res = res.replace(grp[j].full, '');
          }
        } else if (grp.length === 1 && rItem.text === 'r') {
          // Vector đơn ký tự (chữ 'r' đơn lẻ nằm trên ký tự chữ cái như v, a, b, x, y, z, i, j, k, ...)
          const candidates = allItems.filter(c =>
            c.full !== rItem.full &&
            c.y > rItem.y &&
            (c.y - rItem.y) >= rItem.fs * 0.35 &&
            (c.y - rItem.y) <= rItem.fs * 1.35 &&
            Math.abs(c.x - rItem.x) <= rItem.fs * 0.85
          );

          if (candidates.length > 0) {
            candidates.sort((a, b) => {
              const aAlpha = /^[a-zA-Z]$/.test(a.text) ? 1 : 0;
              const bAlpha = /^[a-zA-Z]$/.test(b.text) ? 1 : 0;
              if (aAlpha !== bAlpha) return bAlpha - aAlpha;
              return Math.abs(a.x - rItem.x) - Math.abs(b.x - rItem.x);
            });

            const underChar = candidates[0];
            const fs = underChar.fs;
            const x1 = Math.min(underChar.x, rItem.x);
            const x2 = x1 + fs * 0.55;
            const arrowY = rItem.y + rItem.fs * 0.15;
            const sw = Math.max(1.5, fs * 0.040);
            const headLen = fs * 0.16;
            const headH = fs * 0.09;

            const arrowSvg = `<g class="math-vector-arrow"><line x1="${x1.toFixed(2)}" y1="${arrowY.toFixed(2)}" x2="${x2.toFixed(2)}" y2="${arrowY.toFixed(2)}" stroke="currentColor" stroke-width="${sw.toFixed(2)}" stroke-linecap="round"/><path d="M ${(x2 - headLen).toFixed(2)} ${(arrowY - headH).toFixed(2)} L ${x2.toFixed(2)} ${arrowY.toFixed(2)} L ${(x2 - headLen).toFixed(2)} ${(arrowY + headH).toFixed(2)}" fill="none" stroke="currentColor" stroke-width="${sw.toFixed(2)}" stroke-linecap="round" stroke-linejoin="round"/></g>`;

            res = res.replace(rItem.full, arrowSvg);
          }
        }
      }
    }
  } catch {
    // Fallback an toàn nếu có lỗi phân tích regex
  }

  // 2b. Tổng hợp dấu ngoặc đơn / ngoặc vuông lớn bao quanh phân số (MathType tall parentheses)
  try {
    const textElemRegexAll = /<text\s+[^>]*font-size:([\d.]+)[^>]*transform="matrix\([^)]+\s+([\d.]+)\s+([\d.]+)\)"[^>]*>([\s\S]*?)<\/text>/gi;
    const allItems: Array<{ full: string; fs: number; x: number; y: number; text: string }> = [];
    let mAll;
    while ((mAll = textElemRegexAll.exec(res)) !== null) {
      allItems.push({
        full: mAll[0],
        fs: parseFloat(mAll[1]),
        x: parseFloat(mAll[2]),
        y: parseFloat(mAll[3]),
        text: mAll[4].trim()
      });
    }

    const isDelimiterPiece = (t: string) => {
      if (!t || t === '\uFFFD' || t === '&#xFFFD;') return true;
      if (t === '&#x239B;' || t === '&#x239C;' || t === '&#x239D;' ||
          t === '&#x239E;' || t === '&#x239F;' || t === '&#x23A0;') return true;
      if (t === '\u239B' || t === '\u239C' || t === '\u239D' ||
          t === '\u239E' || t === '\u239F' || t === '\u23A0') return true;
      if (t === 'æ' || t === 'ç' || t === 'è' || t === 'ö' || t === '÷' || t === 'ø') return true;
      return false;
    };

    const brokenItems = allItems.filter(i => isDelimiterPiece(i.text));
    if (brokenItems.length > 0) {
      const groupsByX: Array<Array<{ full: string; fs: number; x: number; y: number; text: string }>> = [];
      for (const it of brokenItems) {
        const existing = groupsByX.find(g => Math.abs(g[0].x - it.x) < 20);
        if (existing) {
          existing.push(it);
        } else {
          groupsByX.push([it]);
        }
      }

      const validItems = allItems.filter(i => !isDelimiterPiece(i.text));
      const centerX = validItems.length > 0
        ? (Math.min(...validItems.map(i => i.x)) + Math.max(...validItems.map(i => i.x))) / 2
        : 384;

      for (const grp of groupsByX) {
        const x = grp[0].x;
        if (grp.length >= 2) {
          const minY = Math.min(...grp.map(g => g.y)) - grp[0].fs * 0.65;
          const maxY = Math.max(...grp.map(g => g.y)) + grp[0].fs * 0.15;
          const h = maxY - minY;
          const fs = grp[0].fs;
          const strokeW = Math.max(2.5, fs * 0.04);
          const isLeft = x < centerX;

          const bow = Math.min(25, fs * 0.18);
          let pathD = '';
          if (isLeft) {
            const xOuter = x + bow;
            const xInner = x - bow * 0.6;
            pathD = `M ${xOuter.toFixed(1)} ${minY.toFixed(1)} C ${xInner.toFixed(1)} ${(minY + h * 0.25).toFixed(1)}, ${xInner.toFixed(1)} ${(maxY - h * 0.25).toFixed(1)}, ${xOuter.toFixed(1)} ${maxY.toFixed(1)}`;
          } else {
            const xOuter = x - bow;
            const xInner = x + bow * 0.6;
            pathD = `M ${xOuter.toFixed(1)} ${minY.toFixed(1)} C ${xInner.toFixed(1)} ${(minY + h * 0.25).toFixed(1)}, ${xInner.toFixed(1)} ${(maxY - h * 0.25).toFixed(1)}, ${xOuter.toFixed(1)} ${maxY.toFixed(1)}`;
          }

          const parenSvg = `<path d="${pathD}" fill="none" stroke="currentColor" stroke-width="${strokeW.toFixed(1)}" stroke-linecap="round" class="math-delimiter"/>`;
          res = res.replace(grp[0].full, parenSvg);
          for (let j = 1; j < grp.length; j++) {
            res = res.replace(grp[j].full, '');
          }
        } else if (grp.length === 1) {
          const it = grp[0];
          const repl = `<text x="0" y="0" style="font-family:'Times New Roman',serif; font-size:${it.fs.toFixed(1)}; fill:currentColor" transform="matrix(1 0 0 1 ${it.x.toFixed(1)} ${it.y.toFixed(1)})">\\</text>`;
          res = res.replace(it.full, repl);
        }
      }
    }
  } catch {
    // ignore
  }

    // 2c. Giãn nhẹ khoảng cách giữa các ký tự trong công thức (Letter-Spacing: 0.1em cho text có style="...")
    try {
      res = res.replace(/(<text\s+[^>]*(?<![-_\w])style="[^"]*)"/gi, '$1; letter-spacing:0.1em;"');
    } catch {
      // ignore
    }

    // 2c2. Thoát 100% ký tự đặc thù XML (<, >, &) trong nội dung thẻ <text>...</text> (khắc phục lỗi bất phương trình Câu 12)
    try {
      res = res.replace(/(<text\b[^>]*>)([\s\S]*?)(<\/text>)/gi, (_m, open, content, close) => {
        let safeContent = content.replace(/&(?!(?:[a-zA-Z]+|#[0-9]+|#x[0-9a-fA-F]+);)/g, '&amp;');
        safeContent = safeContent.replace(/</g, '&lt;').replace(/>/g, '&gt;');
        return open + safeContent + close;
      });
    } catch {
      // ignore
    }

    // 2d. Tính toán hộp giới hạn thực tế (Auto-Crop True Bounding Box) triệt tiêu 100% khoảng trắng thừa
    try {
      let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;

    const alphaFs: number[] = [];
    const allFs: number[] = [];

    const textNodes = res.matchAll(/<text\b([^>]*)>([\s\S]*?)<\/text>/gi);
    for (const tm of textNodes) {
      const attrs = tm[1];
      const rawText = tm[2].trim();
      let x = 0, y = 0, fs = 14;
      let hasCoords = false;

      const matMatch = attrs.match(/transform="matrix\([^)]+\s+([-+]?[\d.]+)\s+([-+]?[\d.]+)\)"/i);
      if (matMatch) {
        x = parseFloat(matMatch[1]);
        y = parseFloat(matMatch[2]);
        hasCoords = true;
      } else {
        const xMatch = attrs.match(/\bx="([-+]?[\d.]+)"/i);
        const yMatch = attrs.match(/\by="([-+]?[\d.]+)"/i);
        if (xMatch && yMatch) {
          x = parseFloat(xMatch[1]);
          y = parseFloat(yMatch[1]);
          hasCoords = true;
        }
      }

      if (hasCoords) {
        const fsMatch = attrs.match(/font-size[:="]+([-+]?[\d.]+)/i);
        if (fsMatch) {
          fs = parseFloat(fsMatch[1]);
        }

        allFs.push(fs);
        if (/[a-zA-Z0-9]/.test(rawText)) {
          alphaFs.push(fs);
        }

        const decodedLen = Math.max(1, rawText.replace(/&#x?[0-9a-fA-F]+;|&[a-zA-Z]+;/g, 'X').length);
        const w = decodedLen * fs * 0.72;
        minX = Math.min(minX, x);
        minY = Math.min(minY, y - fs * 0.85);
        maxX = Math.max(maxX, x + w);
        maxY = Math.max(maxY, y + fs * 0.25);
      }
    }

    const lMatches = res.matchAll(/<line\s+[^>]*x1="([\d.]+)"\s+y1="([\d.]+)"\s+x2="([\d.]+)"\s+y2="([\d.]+)"/gi);
    for (const lm of lMatches) {
      const x1 = parseFloat(lm[1]), y1 = parseFloat(lm[2]);
      const x2 = parseFloat(lm[3]), y2 = parseFloat(lm[4]);
      minX = Math.min(minX, x1, x2);
      maxX = Math.max(maxX, x1, x2);
      minY = Math.min(minY, y1, y2);
      maxY = Math.max(maxY, y1, y2);
    }

    const pMatches = res.matchAll(/<path\s+[^>]*d="([^"]+)"/gi);
    for (const pm of pMatches) {
      const nums = pm[1].match(/[-+]?[\d.]+/g);
      if (nums) {
        for (let i = 0; i + 1 < nums.length; i += 2) {
          const px = parseFloat(nums[i]);
          const py = parseFloat(nums[i + 1]);
          minX = Math.min(minX, px);
          maxX = Math.max(maxX, px);
          minY = Math.min(minY, py);
          maxY = Math.max(maxY, py);
        }
      }
    }

    if (isFinite(minX) && isFinite(maxX) && minX < maxX) {
      // Auto-Trim chuẩn xác: Không kẹp 0 để không bị xén đỉnh âm của dấu ngoặc/tích phân
      const padX = 10;
      const padY = 6;
      const cropX = minX - padX;
      const cropY = minY - padY;
      const cropW = (maxX - minX) + padX * 2;
      const cropH = (maxY - minY) + padY * 2;

      // Tính font-size cơ sở chuẩn hóa (FS_base)
      const targetFsList = alphaFs.length > 0 ? alphaFs : allFs;
      targetFsList.sort((a, b) => a - b);
      const fsBase = targetFsList.length > 0 ? targetFsList[Math.floor(targetFsList.length / 2)] : 200;

      // Tỷ lệ dòng chuẩn hóa: lineFactor = cropH / fsBase
      const lineFactor = Math.max(0.8, Math.min(10.0, cropH / fsBase));
      const scale = lineFactor > 1.8 ? Math.max(1.55, lineFactor) : 1.0;

      const svgTagMatch = res.match(/<svg\b[^>]*>/i);
      if (svgTagMatch && svgTagMatch.index !== undefined) {
        let svgTag = svgTagMatch[0];
        if (svgTag.includes('viewBox=')) {
          svgTag = svgTag.replace(/viewBox="[^"]*"/i, `viewBox="${cropX.toFixed(1)} ${cropY.toFixed(1)} ${cropW.toFixed(1)} ${cropH.toFixed(1)}"`);
        } else {
          svgTag = svgTag.slice(0, 4) + ` viewBox="${cropX.toFixed(1)} ${cropY.toFixed(1)} ${cropW.toFixed(1)} ${cropH.toFixed(1)}"` + svgTag.slice(4);
        }

        if (/(?<![\w-])width="[^"]*"/i.test(svgTag)) {
          svgTag = svgTag.replace(/(?<![\w-])width="[^"]*"/i, `width="${cropW.toFixed(1)}"`);
        } else {
          svgTag = svgTag.slice(0, 4) + ` width="${cropW.toFixed(1)}"` + svgTag.slice(4);
        }

        if (/(?<![\w-])height="[^"]*"/i.test(svgTag)) {
          svgTag = svgTag.replace(/(?<![\w-])height="[^"]*"/i, `height="${cropH.toFixed(1)}"`);
        } else {
          svgTag = svgTag.slice(0, 4) + ` height="${cropH.toFixed(1)}"` + svgTag.slice(4);
        }

        if (svgTag.includes('data-scale=')) {
          svgTag = svgTag.replace(/data-scale="[^"]*"/i, `data-scale="${scale.toFixed(2)}"`);
        } else {
          svgTag = svgTag.slice(0, 4) + ` data-scale="${scale.toFixed(2)}"` + svgTag.slice(4);
        }

        if (svgTag.includes('data-line-factor=')) {
          svgTag = svgTag.replace(/data-line-factor="[^"]*"/i, `data-line-factor="${lineFactor.toFixed(3)}"`);
        } else {
          svgTag = svgTag.slice(0, 4) + ` data-line-factor="${lineFactor.toFixed(3)}"` + svgTag.slice(4);
        }

        if (svgTag.includes('data-fs-base=')) {
          svgTag = svgTag.replace(/data-fs-base="[^"]*"/i, `data-fs-base="${fsBase.toFixed(1)}"`);
        } else {
          svgTag = svgTag.slice(0, 4) + ` data-fs-base="${fsBase.toFixed(1)}"` + svgTag.slice(4);
        }

        res = res.slice(0, svgTagMatch.index) + svgTag + res.slice(svgTagMatch.index + svgTagMatch[0].length);
      }
    }
  } catch {
    // ignore
  }

  return res;
}

/**
 * Chuyển đổi một chuỗi data URI dạng 'data:image/x-wmf;base64,...' sang 'data:image/svg+xml;base64,...'
 */
export function convertWmfDataUriToSvg(dataUri: string): string {
  if (!dataUri || (!dataUri.startsWith('data:image/x-wmf') && !dataUri.startsWith('data:image/wmf'))) {
    return dataUri;
  }

  try {
    const commaIdx = dataUri.indexOf(',');
    if (commaIdx === -1) return dataUri;
    const base64 = dataUri.substring(commaIdx + 1);
    const binaryStr = atob(base64);
    const len = binaryStr.length;
    const bytes = new Uint8Array(len);
    for (let i = 0; i < len; i++) {
      bytes[i] = binaryStr.charCodeAt(i);
    }

    const svg = convertWmfBytesToSvg(bytes);
    if (svg) {
      const utf8Bytes = new TextEncoder().encode(svg);
      let binary = '';
      for (let i = 0; i < utf8Bytes.length; i++) {
        binary += String.fromCharCode(utf8Bytes[i]);
      }
      return 'data:image/svg+xml;base64,' + btoa(binary);
    }
  } catch (err) {
    console.warn('[wmfDecoder] Lỗi thông dịch WMF sang SVG:', err);
  }

  return dataUri;
}

/**
 * Chuẩn hóa chuỗi SVG Data URI: Đảm bảo có xmlns="http://www.w3.org/2000/svg", loại bỏ <!DOCTYPE>, bổ sung viewBox, làm sạch ký tự và thoát XML (<, >, &)
 */
export function cleanSvgDataUri(dataUri: string): string {
  if (!dataUri || !dataUri.startsWith('data:image/svg+xml')) return dataUri;
  try {
    const commaIdx = dataUri.indexOf(',');
    if (commaIdx === -1) return dataUri;
    const base64Part = dataUri.substring(commaIdx + 1);

    // Giải mã an toàn hỗ trợ đầy đủ chuẩn UTF-8
    let svg = '';
    try {
      const binary = atob(base64Part);
      const bytes = Uint8Array.from(binary, c => c.charCodeAt(0));
      svg = new TextDecoder().decode(bytes);
    } catch {
      svg = atob(base64Part);
    }

    svg = svg.replace(/<!DOCTYPE[\s\S]*?>/gi, '');
    if (!svg.includes('xmlns="http://www.w3.org/2000/svg"')) {
      svg = svg.replace('<svg', '<svg xmlns="http://www.w3.org/2000/svg"');
    }
    const matchW = svg.match(/width="([0-9.]+)"/);
    const matchH = svg.match(/height="([0-9.]+)"/);
    if (matchW && matchH && !svg.includes('viewBox')) {
      svg = svg.replace('<svg', `<svg viewBox="0 0 ${matchW[1]} ${matchH[1]}"`);
    }

    // Cập nhật letter-spacing 0.1em
    if (svg.includes('letter-spacing:0.08em')) {
      svg = svg.replace(/letter-spacing:\s*0\.08em;?/gi, 'letter-spacing:0.1em;');
    } else if (!svg.includes('letter-spacing')) {
      svg = svg.replace(/(<text\s+[^>]*style="[^"]*)"/gi, '$1; letter-spacing:0.1em;"');
    }

    svg = sanitizeMathTypeSvgContent(svg);

    // THOÁT 100% KÝ TỰ XML ĐẶC THÙ TRONG THẺ <text>:
    // Đặc biệt là dấu '<' trong bất phương trình (như Câu 12) khiến SVG bị lỗi cú pháp XML (malformed XML) và trình duyệt từ chối render!
    svg = svg.replace(/(<text\b[^>]*>)([\s\S]*?)(<\/text>)/gi, (_m, open, content, close) => {
      let safeContent = content.replace(/&(?!(?:[a-zA-Z]+|#[0-9]+|#x[0-9a-fA-F]+);)/g, '&amp;');
      safeContent = safeContent.replace(/</g, '&lt;').replace(/>/g, '&gt;');
      return open + safeContent + close;
    });

    const utf8Bytes = new TextEncoder().encode(svg);
    let binary = '';
    for (let i = 0; i < utf8Bytes.length; i++) {
      binary += String.fromCharCode(utf8Bytes[i]);
    }
    return 'data:image/svg+xml;base64,' + btoa(binary);
  } catch {
    return dataUri;
  }
}

/**
 * Chuẩn hóa toàn bộ chuỗi văn bản hoặc đề thi:
 * 1. Tự động chuyển đổi tất cả các WMF Data URI thành SVG Data URI.
 * 2. Làm sạch toàn diện các SVG Data URI theo chuẩn W3C SVG.
 * 3. Chuẩn hóa dấu phẩy trên cho đỉnh hình học (A', B', C', D') và vector nội dòng.
 */
export function sanitizeWmfInText(text: string): string {
  if (!text) return text;

  let result = text;

  // Chuẩn hóa dấu phẩy trên cho đỉnh hình học (A', B', C', D', S', M')
  result = result.replace(/(?<=[A-Z])[’´`′]/g, "'");

  // Chuẩn hóa ký tự mũi tên vector kết hợp Unicode (\u20D7)
  result = result.replace(/([A-Z]{2})\u20D7/g, '$\\overrightarrow{$1}$');
  result = result.replace(/([a-zA-Z])\u20D7/g, '$\\vec{$1}$');

  if (result.includes('data:image/x-wmf') || result.includes('data:image/wmf')) {
    result = result.replace(/data:image\/(?:x-)?wmf;base64,[A-Za-z0-9+/=]+/g, (match) => {
      return convertWmfDataUriToSvg(match);
    });
  }

  if (result.includes('data:image/svg+xml')) {
    result = result.replace(/data:image\/svg\+xml;base64,[A-Za-z0-9+/=]+/g, (match) => {
      return cleanSvgDataUri(match);
    });
  }

  return result;
}
