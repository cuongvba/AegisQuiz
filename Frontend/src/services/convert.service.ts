import api from './api';

export interface ConvertTemplateInfo {
  templateCode: string;
  displayName: string;
  description: string;
  supportsMultiSheet: boolean;
  isNativeIsomorphic: boolean;
}

export interface ConvertExportOptions {
  templateCode?: string;
  examTitle?: string;
  issuingOrg?: string;
  targetLevel?: string;
  domainCode?: string;
  includeExplanation?: boolean;
  includeImages?: boolean;
}

/**
 * Tạo hoặc trích xuất tên tệp tải về có ý nghĩa, lưu giữ tiếng Việt đẹp mắt,
 * tránh trường hợp tên vô nghĩa hoặc biến thành chuỗi gạch dưới dài dằng dặc.
 */
function resolveDownloadFileName(
  contentDisposition?: string,
  preferredTitle?: string,
  templateCode: string = 'NATIVE_AEGIS',
  suffix?: string
): string {
  if (contentDisposition) {
    // 1. Ưu tiên RFC 5987 / RFC 6266 filename*=UTF-8''...
    const utf8Match = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
    if (utf8Match && utf8Match[1]) {
      try {
        return decodeURIComponent(utf8Match[1].trim());
      } catch {
        // Fallback nếu URI decode lỗi
      }
    }
    // 2. Fallback filename="..."
    const normalMatch = contentDisposition.match(/filename="?([^";]+)"?/i);
    if (normalMatch && normalMatch[1]) {
      return normalMatch[1].trim();
    }
  }

  // 3. Tự sinh tên gợi nhớ bảo toàn Unicode tiếng Việt
  const raw = (preferredTitle || 'De_Thi_AegisQuiz').trim();
  // Loại bỏ các ký tự cấm của OS: \ / : * ? " < > |
  const safe = raw
    .replace(/[\\/:*?"<>|]+/g, '_')
    .replace(/[\s_]+/g, '_')
    .replace(/^_+|_+$/g, '');

  const now = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  const timeStr = `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}_${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`;

  if (suffix) {
    return `${safe || 'De_Thi'}_${suffix}_${timeStr}.xlsx`;
  }
  return `${safe || 'De_Thi'}_${templateCode.toUpperCase()}_${timeStr}.xlsx`;
}

export const convertService = {
  /**
   * Lấy danh sách các mẫu xuất bản bảng tính Excel sẵn có trong hệ thống
   */
  async getTemplates(): Promise<ConvertTemplateInfo[]> {
    try {
      const response = await api.get('/api/v1/convert/templates');
      return response.data?.data || [];
    } catch {
      return [
        {
          templateCode: 'NATIVE_AEGIS',
          displayName: 'Aegis Native Isomorphic (Chuẩn Toàn Năng)',
          description: 'Mẫu Excel chuẩn mực tương thích 100% với Aegis, bảo toàn đẳng cấu 9 dạng câu hỏi, LaTeX, hình ảnh và 5 trục tọa độ tri thức.',
          supportsMultiSheet: false,
          isNativeIsomorphic: true,
        },
        {
          templateCode: 'GDPT_2025',
          displayName: 'Bộ GD&ĐT GDPT 2025 (Chuẩn 3 Phần Chuyên Biệt)',
          description: 'Tự động phân tách câu hỏi thành 3 sheet: Phần I (Nhiều lựa chọn), Phần II (Đúng/Sai 4 ý), Phần III (Trả lời ngắn).',
          supportsMultiSheet: true,
          isNativeIsomorphic: false,
        },
        {
          templateCode: 'AZOTA',
          displayName: 'Azota / LMS Việt Nam (Tối Giản Phổ Thông)',
          description: 'Mẫu bảng tính tương thích trực tiếp các hệ thống thi trắc nghiệm Azota, Subiz, OLM, K12Online.',
          supportsMultiSheet: false,
          isNativeIsomorphic: false,
        },
        {
          templateCode: 'CUSTOM_BYOT',
          displayName: 'Tùy biến theo Mẫu tải lên (Custom BYOT Template)',
          description: 'Tự động nhận diện cấu trúc và điền dữ liệu vào bất kỳ tệp mẫu Excel (.xlsx) nào do người dùng tải lên.',
          supportsMultiSheet: false,
          isNativeIsomorphic: false,
        },
      ];
    }
  },

  /**
   * Chuyển đổi trực tiếp tài liệu Word/PDF sang tệp Excel (.xlsx) và kích hoạt tải về
   */
  async convertFileToExcel(
    file: File,
    options: ConvertExportOptions = {},
    customTemplateFile?: File
  ): Promise<void> {
    const formData = new FormData();
    formData.append('file', file);
    if (options.templateCode) formData.append('templateCode', options.templateCode);
    if (customTemplateFile) formData.append('customTemplateFile', customTemplateFile);
    if (options.examTitle) formData.append('examTitle', options.examTitle);
    if (options.issuingOrg) formData.append('issuingOrg', options.issuingOrg);
    if (options.targetLevel) formData.append('targetLevel', options.targetLevel);
    if (options.domainCode) formData.append('domainCode', options.domainCode);
    formData.append('includeExplanation', String(options.includeExplanation ?? true));
    formData.append('includeImages', String(options.includeImages ?? true));

    try {
      const response = await api.post('/api/v1/convert/file-to-excel', formData, {
        responseType: 'blob',
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });

      const blob = new Blob([response.data], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;

      const baseName = file.name.substring(0, file.name.lastIndexOf('.')) || 'Exam';
      const disposition = response.headers?.['content-disposition'];
      a.download = resolveDownloadFileName(disposition, options.examTitle || baseName, options.templateCode || 'NATIVE_AEGIS');

      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err: any) {
      if (err.response?.data instanceof Blob) {
        try {
          const text = await err.response.data.text();
          const json = JSON.parse(text);
          if (json.message) throw new Error(json.message);
        } catch (e: any) {
          if (e.message && e !== err) throw e;
        }
      }
      throw err;
    }
  },

  /**
   * Xuất bản danh sách câu hỏi xem trước (Preview AST) sang file Excel (.xlsx) và kích hoạt tải về
   */
  async exportPreviewToExcel(
    questions: any[],
    options: ConvertExportOptions = {}
  ): Promise<void> {
    const templateCode = options.templateCode || 'NATIVE_AEGIS';
    try {
      const response = await api.post('/api/v1/convert/preview-to-excel', questions, {
        params: {
          templateCode,
          examTitle: options.examTitle,
          includeExplanation: options.includeExplanation ?? true,
          includeImages: options.includeImages ?? true,
        },
        responseType: 'blob',
      });

      const blob = new Blob([response.data], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;

      const disposition = response.headers?.['content-disposition'];
      a.download = resolveDownloadFileName(disposition, options.examTitle || 'De_Thi_AegisQuiz', templateCode);

      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err: any) {
      if (err.response?.data instanceof Blob) {
        try {
          const text = await err.response.data.text();
          const json = JSON.parse(text);
          if (json.message) throw new Error(json.message);
        } catch (e: any) {
          if (e.message && e !== err) throw e;
        }
      }
      throw err;
    }
  },

  /**
   * [BYOT] Xuất bản danh sách câu hỏi xem trước theo Template Mẫu (.xlsx) do người dùng tải lên
   */
  async exportPreviewToCustomTemplate(
    questions: any[],
    templateFile: File,
    options: ConvertExportOptions = {}
  ): Promise<void> {
    const formData = new FormData();
    formData.append('templateFile', templateFile);
    formData.append('questionsJson', JSON.stringify(questions));
    if (options.examTitle) formData.append('examTitle', options.examTitle);
    formData.append('includeExplanation', String(options.includeExplanation ?? true));
    formData.append('includeImages', String(options.includeImages ?? true));

    try {
      const response = await api.post('/api/v1/convert/preview-to-custom-template', formData, {
        responseType: 'blob',
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });

      const blob = new Blob([response.data], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;

      const baseName = templateFile.name.substring(0, templateFile.name.lastIndexOf('.')) || 'CustomTemplate';
      const disposition = response.headers?.['content-disposition'];
      a.download = resolveDownloadFileName(disposition, options.examTitle || baseName, 'CUSTOM', `${baseName}_Exported`);

      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err: any) {
      if (err.response?.data instanceof Blob) {
        try {
          const text = await err.response.data.text();
          const json = JSON.parse(text);
          if (json.message) throw new Error(json.message);
        } catch (e: any) {
          if (e.message && e !== err) throw e;
        }
      }
      throw err;
    }
  },

  /**
   * [Universal Exam Transpiler] Lấy danh sách các chuẩn xuất bản quốc tế (Moodle XML, QTI 2.1, GIFT)
   */
  async getAvailableTranspilerFormats(): Promise<any[]> {
    const res = await api.get('/api/v1/convert/transpiler-formats');
    return res.data?.data || [];
  },

  /**
   * [Universal Exam Transpiler] Xuất bản danh sách câu hỏi xem trước sang chuẩn quốc tế (Moodle XML, QTI Zip, GIFT)
   */
  async exportPreviewToStandard(
    questions: any[],
    formatCode: string = 'MOODLE_XML',
    options: { examTitle?: string; categoryName?: string; includeExplanation?: boolean; includeImages?: boolean } = {}
  ): Promise<void> {
    try {
      const response = await api.post('/api/v1/convert/preview-transpile', questions, {
        params: {
          formatCode,
          examTitle: options.examTitle,
          categoryName: options.categoryName,
          includeExplanation: options.includeExplanation ?? true,
          includeImages: options.includeImages ?? true,
        },
        responseType: 'blob',
      });

      const contentType = (response.headers?.['content-type'] as string) || 'application/octet-stream';
      const blob = new Blob([response.data], { type: contentType });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;

      let defaultExt = '.xml';
      if (formatCode === 'QTI_PACKAGE') defaultExt = '.qti.zip';
      else if (formatCode === 'MOODLE_GIFT') defaultExt = '.gift.txt';

      const disposition = response.headers?.['content-disposition'] as string | undefined;
      a.download = resolveDownloadFileName(
        disposition, 
        options.examTitle || 'AegisQuiz_Transpiled', 
        formatCode,
        undefined
      ).replace(/\.xlsx$/, defaultExt);

      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err: any) {
      if (err.response?.data instanceof Blob) {
        try {
          const text = await err.response.data.text();
          const json = JSON.parse(text);
          if (json.message) throw new Error(json.message);
        } catch (e: any) {
          if (e.message && e !== err) throw e;
        }
      }
      throw err;
    }
  },
};
