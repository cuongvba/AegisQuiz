import { useState, useCallback } from 'react';

interface SignRequest {
  contentB64: string;
  typeSign: 'text' | 'PDF' | 'XML' | 'HASH';
  pinCode?: string;
  serial?: string;
}

interface SignResponse {
  codeStatus: string;
  messageNotify?: string;
  dataSigned?: string;
  dataSigned2?: string;
  ctsInfo?: string;
}

export const useAgribankPKI = (wsUrl = 'ws://127.0.0.1:8764/plugin/sign') => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const signData = useCallback((request: SignRequest): Promise<SignResponse> => {
    return new Promise((resolve, reject) => {
      setLoading(true);
      setError(null);

      const socket = new WebSocket(wsUrl);

      socket.onopen = () => {
        console.log('Đã kết nối tới Agribank PKI Plugin');
        // Gửi yêu cầu ký số
        const payload = {
          dataToSign: {
            contentB64: request.contentB64,
            type: request.typeSign,
            pinCode: request.pinCode || '',
            serial: request.serial || '',
            isIgnoreView: '',
            multiple: ''
          }
        };
        socket.send(JSON.stringify(payload));
      };

      socket.onmessage = (event) => {
        try {
          const rawResponse = JSON.parse(event.data);
          console.log('Nhận kết quả từ Plugin:', rawResponse);

          const codeStatus = String(rawResponse.codeStatus ?? rawResponse.CODE_STATUS ?? '');
          const messageNotify = String(rawResponse.messageNotify ?? rawResponse.MESSAGE_NOTIFY ?? '');
          const dataSigned = String(rawResponse.dataSigned ?? rawResponse.DATA_SIGNED ?? rawResponse.dataSigned2 ?? rawResponse.DATA_SIGNED2 ?? '');
          const ctsInfo = String(rawResponse.ctsInfo ?? rawResponse.CTS_INFO ?? rawResponse.certificate ?? rawResponse.certTokenInfo?.cert ?? '');

          if (codeStatus === '200' || codeStatus === '0') {
            resolve({
              codeStatus,
              messageNotify,
              dataSigned,
              ctsInfo
            });
          } else {
            let errorMsg = messageNotify;
            if (codeStatus === '404') {
              errorMsg = 'Không tìm thấy thiết bị USB Token hoặc chứng thư số hợp lệ. Vui lòng cắm USB Token và kiểm tra Driver.';
            } else if (!errorMsg || errorMsg.trim() === '') {
              errorMsg = `Lỗi hệ thống ký số PKI (Mã lỗi: ${codeStatus})`;
            }
            setError(errorMsg);
            reject(new Error(errorMsg));
          }
        } catch (err) {
          setError('Không thể phân tích dữ liệu phản hồi từ plugin.');
          reject(err);
        } finally {
          socket.close();
        }
      };

      socket.onerror = (err) => {
        console.error('Lỗi kết nối WebSocket:', err);
        setError('Không thể kết nối tới Agribank PKI Plugin. Hãy chắc chắn ứng dụng đã được khởi động.');
        reject(new Error('Kết nối thất bại tới Plugin'));
        setLoading(false);
      };

      socket.onclose = () => {
        console.log('Đã ngắt kết nối với Agribank PKI Plugin');
        setLoading(false);
      };
    });
  }, [wsUrl]);

  return { signData, loading, error };
};
