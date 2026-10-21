/**
 * [Aegis Client-Side Docx Extractor v1.0]
 * Bóc tách tệp OOXML Word (.docx) siêu tốc chạy 100% trên RAM trình duyệt:
 * - Giải mã nhị phân ZIP Local Headers & Deflate-raw bằng DecompressionStream native.
 * - Phân tích cú pháp DOM XML w:p, w:r, w:t, w:b, w:u.
 * - Nhận diện câu hỏi, phương án A/B/C/D, đáp án in đậm/gạch chân theo chuẩn CIG.
 * - Giới hạn kích thước < 250 dòng code theo chuẩn AGENTS.md.
 */

export interface ClientParsedQuestion {
  tempId: string;
  orderIndex: number;
  content: string;
  options: string[];
  suggestedAnswer: string;
  questionType: 'SINGLE' | 'MULTI' | 'TRUE_FALSE' | 'ESSAY';
  difficulty: number;
  topicCode?: string;
}

export async function extractDocumentXmlFromDocx(file: File): Promise<string> {
  const buffer = await file.arrayBuffer();
  const view = new DataView(buffer);
  const bytes = new Uint8Array(buffer);
  let offset = 0;

  while (offset + 30 < buffer.byteLength) {
    const sig = view.getUint32(offset, true);
    if (sig !== 0x04034b50) {
      offset++;
      continue;
    }

    const method = view.getUint16(offset + 8, true);
    let compSize = view.getUint32(offset + 18, true);
    const nameLen = view.getUint16(offset + 26, true);
    const extraLen = view.getUint16(offset + 28, true);

    const nameBytes = bytes.slice(offset + 30, offset + 30 + nameLen);
    const fileName = new TextDecoder().decode(nameBytes);
    const dataStart = offset + 30 + nameLen + extraLen;

    if (fileName === 'word/document.xml') {
      if (compSize === 0 || compSize === 0xffffffff) {
        // Tìm chữ ký kết thúc hoặc entry tiếp theo
        let nextEntry = dataStart;
        while (nextEntry + 4 < buffer.byteLength) {
          const nextSig = view.getUint32(nextEntry, true);
          if (nextSig === 0x04034b50 || nextSig === 0x02014b50) break;
          nextEntry++;
        }
        compSize = nextEntry - dataStart;
      }

      const compressedChunk = bytes.slice(dataStart, dataStart + compSize);
      if (method === 0) {
        return new TextDecoder('utf-8').decode(compressedChunk);
      } else if (method === 8) {
        const ds = new DecompressionStream('deflate-raw');
        const writer = ds.writable.getWriter();
        writer.write(compressedChunk);
        writer.close();
        const reader = ds.readable.getReader();
        const chunks: Uint8Array[] = [];
        while (true) {
          const { done, value } = await reader.read();
          if (done) break;
          if (value) chunks.push(value);
        }
        const totalLen = chunks.reduce((acc, c) => acc + c.length, 0);
        const merged = new Uint8Array(totalLen);
        let pos = 0;
        for (const c of chunks) {
          merged.set(c, pos);
          pos += c.length;
        }
        return new TextDecoder('utf-8').decode(merged);
      }
    }

    offset = dataStart + (compSize > 0 ? compSize : 0);
  }

  throw new Error('Không tìm thấy word/document.xml trong tệp DOCX');
}

export function parseDocxParagraphs(xmlString: string): Array<{ text: string; isBold: boolean; isUnderline: boolean }> {
  const parser = new DOMParser();
  const xmlDoc = parser.parseFromString(xmlString, 'text/xml');
  const paragraphs = xmlDoc.getElementsByTagName('w:p');
  const result: Array<{ text: string; isBold: boolean; isUnderline: boolean }> = [];

  for (let i = 0; i < paragraphs.length; i++) {
    const p = paragraphs[i];
    const runs = p.getElementsByTagName('w:r');
    let pText = '';
    let hasBold = false;
    let hasUnderline = false;

    for (let j = 0; j < runs.length; j++) {
      const r = runs[j];
      const isB = r.getElementsByTagName('w:b').length > 0;
      const isU = r.getElementsByTagName('w:u').length > 0;
      if (isB) hasBold = true;
      if (isU) hasUnderline = true;

      const textNodes = r.getElementsByTagName('w:t');
      for (let k = 0; k < textNodes.length; k++) {
        pText += textNodes[k].textContent || '';
      }
    }

    const trimmed = pText.trim();
    if (trimmed.length > 0) {
      result.push({ text: trimmed, isBold: hasBold, isUnderline: hasUnderline });
    }
  }

  return result;
}

export function parseQuestionsFromParagraphs(paragraphs: Array<{ text: string; isBold: boolean; isUnderline: boolean }>): ClientParsedQuestion[] {
  const questions: ClientParsedQuestion[] = [];
  let currentQ: ClientParsedQuestion | null = null;
  const questionAnchorRegex = /^(?:Câu|Question|Bài|Bai|Item)\s*(\d+)[\.\:\-\s]+(.*)/i;
  const optionRegex = /^([A-D])[\.\:\)\s]+(.*)/i;

  for (let idx = 0; idx < paragraphs.length; idx++) {
    const { text, isBold, isUnderline } = paragraphs[idx];
    const qMatch = text.match(questionAnchorRegex);

    if (qMatch) {
      if (currentQ) {
        questions.push(currentQ);
      }
      currentQ = {
        tempId: `client_q_${idx + 1}`,
        orderIndex: questions.length + 1,
        content: qMatch[2] || text,
        options: [],
        suggestedAnswer: '',
        questionType: 'SINGLE',
        difficulty: 3,
      };
      continue;
    }

    if (currentQ) {
      const optMatch = text.match(optionRegex);
      if (optMatch) {
        const optLetter = optMatch[1].toUpperCase();
        currentQ.options.push(text);
        if (isBold || isUnderline || text.includes('*')) {
          currentQ.suggestedAnswer = optLetter;
        }
      } else {
        if (currentQ.options.length === 0) {
          currentQ.content += '\n' + text;
        }
      }
    }
  }

  if (currentQ) {
    questions.push(currentQ);
  }

  return questions;
}
