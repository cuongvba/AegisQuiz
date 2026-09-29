/**
 * smartClipboard.ts — World-Class Multimodal Clipboard Ingestion Engine
 * =====================================================================
 * Tự động bóc tách và thông dịch dữ liệu Clipboard khi copy từ Word/PDF:
 * 1. Hình ảnh đồ thị, sa hình, công thức: Tự động chuyển thành Data URI Base64.
 * 2. Word Equation & MathML: Thông dịch sang KaTeX LaTeX ($ ... $).
 * 3. Hóa học: Nhận diện sub/sup, phương trình hóa học, mũi tên phản ứng -> \ce{...}.
 * 4. Nhận diện cấu trúc trắc nghiệm hoàn chỉnh (Thân bài + 4 phương án A, B, C, D + Đáp án).
 */

export interface ParsedQuestionBlock {
  isFullQuestion: boolean;
  stem: string;
  options: string[];
  correctAnswer?: string;
  correctAnswerIndex?: number;
  explanation?: string;
}

export interface SmartPasteResult {
  hasImage: boolean;
  imageDataUri?: string;
  markdownSnippet?: string;
  plainText: string;
  richText?: string;
  text: string;
  hasIntercepted: boolean;
  isFullQuestion: boolean;
  detectedBlock?: ParsedQuestionBlock;
  detectedQuestion?: ParsedQuestionBlock;
}

/**
 * Bộ chuyển đổi MathML sang KaTeX LaTeX chuẩn xác trong bộ nhớ (Client-side MathML-to-LaTeX)
 */
export function convertMathMLToLatex(mathElement: Element): string {
  function traverse(node: Node): string {
    if (node.nodeType === Node.TEXT_NODE) {
      return (node.textContent || '').trim();
    }
    if (node.nodeType !== Node.ELEMENT_NODE) return '';

    const el = node as Element;
    const tag = el.localName.toLowerCase();

    // 1. Token cơ bản
    if (tag === 'mi' || tag === 'mn' || tag === 'mtext') {
      const text = el.textContent || '';
      // Thay thế một số ký hiệu toán học phổ biến
      if (text === 'π' || text === '&pi;') return '\\pi ';
      if (text === 'α' || text === '&alpha;') return '\\alpha ';
      if (text === 'β' || text === '&beta;') return '\\beta ';
      if (text === 'θ' || text === '&theta;') return '\\theta ';
      if (text === 'Δ' || text === '&Delta;') return '\\Delta ';
      if (text === 'λ' || text === '&lambda;') return '\\lambda ';
      if (text === '∞' || text === '&infin;') return '\\infty ';
      return text;
    }

    if (tag === 'mo') {
      const op = (el.textContent || '').trim();
      const opMap: Record<string, string> = {
        '±': '\\pm ',
        '∓': '\\mp ',
        '×': '\\times ',
        '÷': '\\div ',
        '·': '\\cdot ',
        '≤': '\\le ',
        '≥': '\\ge ',
        '≠': '\\ne ',
        '≈': '\\approx ',
        '≡': '\\equiv ',
        '∈': '\\in ',
        '∉': '\\notin ',
        '⊂': '\\subset ',
        '⊃': '\\supset ',
        '∪': '\\cup ',
        '∩': '\\cap ',
        '→': '\\rightarrow ',
        '⇒': '\\Rightarrow ',
        '⇔': '\\Leftrightarrow ',
        '⇌': '\\rightleftharpoons ',
        '∫': '\\int ',
        '∑': '\\sum ',
        '∏': '\\prod ',
        '√': '\\sqrt',
        '∂': '\\partial ',
        '∇': '\\nabla ',
      };
      return opMap[op] || op;
    }

    // 2. Phân số: <mfrac><mi>a</mi><mi>b</mi></mfrac>
    if (tag === 'mfrac') {
      const children = Array.from(el.children);
      const num = children[0] ? traverse(children[0]) : '';
      const den = children[1] ? traverse(children[1]) : '';
      return `\\frac{${num}}{${den}}`;
    }

    // 3. Căn thức: <msqrt> hoặc <mroot>
    if (tag === 'msqrt') {
      const inner = Array.from(el.childNodes).map(traverse).join('');
      return `\\sqrt{${inner}}`;
    }
    if (tag === 'mroot') {
      const children = Array.from(el.children);
      const base = children[0] ? traverse(children[0]) : '';
      const index = children[1] ? traverse(children[1]) : '';
      return `\\sqrt[${index}]{${base}}`;
    }

    // 4. Số mũ & Chỉ số dưới: <msup>, <msub>, <msubsup>
    if (tag === 'msup') {
      const children = Array.from(el.children);
      const base = children[0] ? traverse(children[0]) : '';
      const sup = children[1] ? traverse(children[1]) : '';
      return `{${base}}^{${sup}}`;
    }
    if (tag === 'msub') {
      const children = Array.from(el.children);
      const base = children[0] ? traverse(children[0]) : '';
      const sub = children[1] ? traverse(children[1]) : '';
      return `{${base}}_{${sub}}`;
    }
    if (tag === 'msubsup') {
      const children = Array.from(el.children);
      const base = children[0] ? traverse(children[0]) : '';
      const sub = children[1] ? traverse(children[1]) : '';
      const sup = children[2] ? traverse(children[2]) : '';
      return `{${base}}_{${sub}}^{${sup}}`;
    }

    // 5. Delimiters / Dấu ngoặc: <mfenced>
    if (tag === 'mfenced') {
      const open = el.getAttribute('open') || '(';
      const close = el.getAttribute('close') || ')';
      const inner = Array.from(el.childNodes).map(traverse).join('');
      return `\\left${open}${inner}\\right${close}`;
    }

    // 6. Ma trận / Bảng: <mtable>, <mtr>, <mtd>
    if (tag === 'mtable') {
      const rows = Array.from(el.querySelectorAll(':scope > mtr')).map(r => {
        const cells = Array.from(r.querySelectorAll(':scope > mtd')).map(c =>
          Array.from(c.childNodes).map(traverse).join('')
        );
        return cells.join(' & ');
      });
      return `\\begin{pmatrix}${rows.join(' \\\\ ')}\\end{pmatrix}`;
    }

    // 7. Mover / Munder (Giới hạn, vector, gạch đầu: \lim, \vec, \bar)
    if (tag === 'mover') {
      const children = Array.from(el.children);
      const base = children[0] ? traverse(children[0]) : '';
      const over = children[1] ? (children[1].textContent || '').trim() : '';
      if (over === '^' || over === '̂') return `\\hat{${base}}`;
      if (over === '→' || over === '⃗') return `\\vec{${base}}`;
      if (over === '¯' || over === 'ˉ') return `\\bar{${base}}`;
      return `\\overset{${traverse(children[1])}}{${base}}`;
    }
    if (tag === 'munder') {
      const children = Array.from(el.children);
      const base = children[0] ? traverse(children[0]) : '';
      const under = children[1] ? traverse(children[1]) : '';
      return `\\underset{${under}}{${base}}`;
    }

    // Mặc định: gom tất cả các con
    return Array.from(el.childNodes).map(traverse).join('');
  }

  const latex = traverse(mathElement).trim();
  return latex ? `$${latex}$` : '';
}

/**
 * Thông dịch HTML từ clipboard của Microsoft Word sang văn bản Markdown/KaTeX/mhchem
 */
export function convertWordHtmlToMarkdown(htmlString: string): string {
  const parser = new DOMParser();
  const doc = parser.parseFromString(htmlString, 'text/html');

  // 1. Chuyển đổi toàn bộ các thẻ MathML thành KaTeX LaTeX
  const mathNodes = doc.querySelectorAll('math');
  mathNodes.forEach(m => {
    const latex = convertMathMLToLatex(m);
    const textNode = doc.createTextNode(latex ? ` ${latex} ` : '');
    m.replaceWith(textNode);
  });

  // 2. Chuyển đổi các thẻ sub/sup của Hóa học và Toán học
  const subNodes = doc.querySelectorAll('sub, span[style*="vertical-align:sub"], span[style*="vertical-align: sub"]');
  subNodes.forEach(s => {
    const txt = (s.textContent || '').trim();
    if (txt) {
      s.replaceWith(doc.createTextNode(`_{${txt}}`));
    }
  });

  const supNodes = doc.querySelectorAll('sup, span[style*="vertical-align:super"], span[style*="vertical-align: super"]');
  supNodes.forEach(s => {
    const txt = (s.textContent || '').trim();
    if (txt) {
      s.replaceWith(doc.createTextNode(`^{${txt}}`));
    }
  });

  // 3. Giữ lại gạch chân <u> cho môn Ngoại ngữ
  const uNodes = doc.querySelectorAll('u, span[style*="text-decoration:underline"], span[style*="text-decoration: underline"]');
  uNodes.forEach(u => {
    const txt = (u.textContent || '').trim();
    if (txt) {
      u.replaceWith(doc.createTextNode(`<u>${txt}</u>`));
    }
  });

  // 4. Giữ lại in đậm
  const bNodes = doc.querySelectorAll('b, strong, span[style*="font-weight:bold"], span[style*="font-weight: 700"]');
  bNodes.forEach(b => {
    const txt = (b.textContent || '').trim();
    if (txt) {
      b.replaceWith(doc.createTextNode(`**${txt}**`));
    }
  });

  // 5. Chuẩn hóa hình ảnh nhúng nếu có data URI
  const imgNodes = doc.querySelectorAll('img');
  imgNodes.forEach(img => {
    const src = img.getAttribute('src') || '';
    if (src.startsWith('data:image/')) {
      img.replaceWith(doc.createTextNode(`\n\n![Hình minh họa](${src})\n\n`));
    } else {
      img.remove();
    }
  });

  // Lấy văn bản tổng hợp
  let result = doc.body.textContent || '';

  // 6. Tự động gom các công thức hóa học có dạng Fe2(SO4)3 hoặc H_{2}SO_{4} thành KaTeX/mhchem
  const chemPattern = new RegExp('([A-Z][a-z]?(?:_\\{[0-9]+\\}|\\^\\{[0-9+-]+\\}|[0-9]+)+)', 'g');
  result = result.replace(chemPattern, (match) => {
    return `$\\text{${match}}$`;
  });

  // 7. Chuẩn hóa các mũi tên phản ứng hóa học
  result = result.replace(/(?:-->|->|→)/g, ' $\\rightarrow$ ');
  result = result.replace(/(?:<==>|<=>|⇌)/g, ' $\\rightleftharpoons$ ');

  // Làm sạch khoảng trắng thừa
  result = result.replace(/[ \t]+/g, ' ').replace(/\n\s*\n\s*\n/g, '\n\n').trim();

  return result;
}

/**
 * Nhận diện nếu văn bản dán vào là một câu hỏi hoàn chỉnh kèm 4 phương án
 */
export function detectFullQuestion(text: string): ParsedQuestionBlock {
  const clean = text.trim();
  if (clean.length < 15) {
    return { isFullQuestion: false, stem: clean, options: [] };
  }

  // Mẫu nhận diện câu hỏi trắc nghiệm tiếng Việt và tiếng Anh:
  // "Câu 1: ... A. ... B. ... C. ... D. ... [Đáp án: A]"
  // Hoặc "Question 1: ... A) ... B) ... C) ... D) ..."
  const optAMatch = clean.match(/(?:^|\n|\s+)(?:A[\.\)]|\[A\])\s+/);
  const optBMatch = clean.match(/(?:^|\n|\s+)(?:B[\.\)]|\[B\])\s+/);
  const optCMatch = clean.match(/(?:^|\n|\s+)(?:C[\.\)]|\[C\])\s+/);
  const optDMatch = clean.match(/(?:^|\n|\s+)(?:D[\.\)]|\[D\])\s+/);

  if (optAMatch && optBMatch && optCMatch && optDMatch) {
    const idxA = optAMatch.index! + optAMatch[0].length;
    const startA = optAMatch.index!;
    const idxB = optBMatch.index! + optBMatch[0].length;
    const startB = optBMatch.index!;
    const idxC = optCMatch.index! + optCMatch[0].length;
    const startC = optCMatch.index!;
    const idxD = optDMatch.index! + optDMatch[0].length;
    const startD = optDMatch.index!;

    // Kiểm tra thứ tự A < B < C < D
    if (startA < startB && startB < startC && startC < startD) {
      let stem = clean.substring(0, startA).trim();
      // Bỏ tiền tố "Câu 1:", "Question 1."
      stem = stem.replace(/^(?:Câu|Question|Item|Task|Exercise|Q)\s*\d+[\.\:\s\-]+/i, '').trim();

      let optA = clean.substring(idxA, startB).trim();
      let optB = clean.substring(idxB, startC).trim();
      let optC = clean.substring(idxC, startD).trim();
      let remainder = clean.substring(idxD).trim();

      let optD = remainder;
      let ans: string | undefined = undefined;
      let explanation: string | undefined = undefined;

      // Tìm Đáp án hoặc Lời giải ở đuôi
      const ansMatch = remainder.match(/(?:^|\n|\s+)(?:Đáp\s*án|Đáp\s*số|Key|Answer)[\.\:\s]+([A-D1-4])/i);
      if (ansMatch && ansMatch.index !== undefined) {
        optD = remainder.substring(0, ansMatch.index).trim();
        const rawAns = ansMatch[1].toUpperCase();
        if (rawAns === 'A' || rawAns === '1') ans = '1';
        else if (rawAns === 'B' || rawAns === '2') ans = '2';
        else if (rawAns === 'C' || rawAns === '3') ans = '3';
        else if (rawAns === 'D' || rawAns === '4') ans = '4';

        const afterAns = remainder.substring(ansMatch.index + ansMatch[0].length).trim();
        if (afterAns) {
          explanation = afterAns;
        }
      }

      const explMatch = remainder.match(/(?:^|\n|\s+)(?:Lời\s*giải|Hướng\s*dẫn\s*giải|HDG|Explanation)[\.\:\s]+([\s\S]+)/i);
      if (explMatch && explMatch.index !== undefined) {
        if (!ans) {
          optD = remainder.substring(0, explMatch.index).trim();
        }
        explanation = explMatch[1].trim();
      }

      let ansIdx: number | undefined = undefined;
      if (ans === '1') ansIdx = 0;
      else if (ans === '2') ansIdx = 1;
      else if (ans === '3') ansIdx = 2;
      else if (ans === '4') ansIdx = 3;

      return {
        isFullQuestion: true,
        stem,
        options: [optA, optB, optC, optD],
        correctAnswer: ans,
        correctAnswerIndex: ansIdx,
        explanation
      };
    }
  }

  return { isFullQuestion: false, stem: clean, options: [] };
}

/**
 * Hàm phân tích toàn diện sự kiện Clipboard Paste (onPaste)
 */
export async function processSmartPasteEvent(e: React.ClipboardEvent): Promise<SmartPasteResult> {
  const clipboardData = e.clipboardData;
  if (!clipboardData) {
    return { hasImage: false, plainText: '', text: '', hasIntercepted: false, isFullQuestion: false };
  }

  // 1. Kiểm tra nếu trong Clipboard có file ảnh trực tiếp (Image Blob)
  const items = clipboardData.items;
  if (items) {
    for (let i = 0; i < items.length; i++) {
      const item = items[i];
      if (item.type.startsWith('image/')) {
        const file = item.getAsFile();
        if (file) {
          e.preventDefault();
          const dataUri = await new Promise<string>((resolve) => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result as string);
            reader.readAsDataURL(file);
          });
          const snippet = `\n![Hình minh họa](${dataUri})\n`;

          return {
            hasImage: true,
            imageDataUri: dataUri,
            markdownSnippet: snippet,
            plainText: clipboardData.getData('text/plain') || '',
            text: snippet,
            hasIntercepted: true,
            isFullQuestion: false,
          };
        }
      }
    }
  }

  // 2. Kiểm tra HTML từ Microsoft Word (chứa MathML, Equation, Sub/Sup)
  const htmlData = clipboardData.getData('text/html');
  const plainText = clipboardData.getData('text/plain') || '';

  if (htmlData && (htmlData.includes('<math') || htmlData.includes('mso-') || htmlData.includes('<sub>') || htmlData.includes('<sup>') || htmlData.includes('vertical-align'))) {
    const converted = convertWordHtmlToMarkdown(htmlData);
    if (converted && converted.trim().length > 0) {
      e.preventDefault();
      const detected = detectFullQuestion(converted);
      return {
        hasImage: false,
        plainText,
        richText: converted,
        text: converted,
        hasIntercepted: true,
        isFullQuestion: detected.isFullQuestion,
        detectedBlock: detected,
        detectedQuestion: detected,
      };
    }
  }

  // 3. Fallback văn bản trần: Kiểm tra xem có phải câu hỏi đầy đủ không
  const detected = detectFullQuestion(plainText);
  if (detected.isFullQuestion) {
    e.preventDefault();
    return {
      hasImage: false,
      plainText,
      text: plainText,
      hasIntercepted: true,
      isFullQuestion: true,
      detectedBlock: detected,
      detectedQuestion: detected,
    };
  }

  return {
    hasImage: false,
    plainText,
    text: plainText,
    hasIntercepted: false,
    isFullQuestion: false,
  };
}
