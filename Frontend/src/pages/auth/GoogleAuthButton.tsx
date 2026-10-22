import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

interface GoogleAuthButtonProps {
  clientId: string;
  onSuccess: (credential: string) => void;
  onError: (errorMessage: string) => void;
  disabled?: boolean;
}

// Chuyển mã ngôn ngữ hệ thống sang mã chuẩn tương thích Google Identity Services
const getGoogleLocale = (langCode: string): string => {
  const base = (langCode || 'vi').toLowerCase().split('-')[0];
  switch (base) {
    case 'zh': return 'zh-CN';
    case 'pt': return 'pt-BR';
    default: return base;
  }
};

export function GoogleAuthButton({ clientId, onSuccess, onError, disabled = false }: GoogleAuthButtonProps) {
  const { t, i18n } = useTranslation('auth');
  const googleBtnRef = useRef<HTMLDivElement>(null);
  const [isRendered, setIsRendered] = useState(false);

  useEffect(() => {
    const currentLang = i18n.language || 'vi';
    const googleLocale = getGoogleLocale(currentLang);
    const scriptId = 'google-jssdk';

    setIsRendered(false);

    // Xóa sạch nội dung DOM của container cũ
    if (googleBtnRef.current) {
      googleBtnRef.current.innerHTML = '';
    }

    // Xóa singleton instance cũ trong window để ép SDK nạp lại từ điển ngôn ngữ tương ứng với hl
    try {
      delete (window as any).google;
    } catch {
      (window as any).google = undefined;
    }

    const renderButton = () => {
      if ((window as any).google?.accounts?.id && googleBtnRef.current) {
        try {
          googleBtnRef.current.innerHTML = '';
          (window as any).google.accounts.id.initialize({
            client_id: clientId,
            callback: (response: any) => {
              if (response?.credential) {
                onSuccess(response.credential);
              }
            },
            auto_select: false,
          });

          (window as any).google.accounts.id.renderButton(googleBtnRef.current, {
            theme: 'filled_black',
            size: 'large',
            width: 380,
            text: 'continue_with',
            shape: 'rectangular',
            logo_alignment: 'center',
          });

          setIsRendered(true);
        } catch (err) {
          console.warn('[GoogleAuthButton] Lỗi khởi tạo Google GIS Button:', err);
        }
      }
    };

    // Gỡ thẻ script cũ nếu đã tồn tại trong DOM
    const oldScript = document.getElementById(scriptId);
    if (oldScript) {
      oldScript.remove();
    }

    // Nạp lại Google GIS script với tham số ngôn ngữ hl chính xác
    const script = document.createElement('script');
    script.id = scriptId;
    script.src = `https://accounts.google.com/gsi/client?hl=${googleLocale}`;
    script.async = true;
    script.defer = true;
    script.onload = () => {
      renderButton();
    };
    script.onerror = () => {
      console.warn('[GoogleAuthButton] Không thể kết nối tới Google Identity Services.');
    };
    document.body.appendChild(script);

    return () => {
      // Cleanup script khi component unmount
      const activeScript = document.getElementById(scriptId);
      if (activeScript) {
        activeScript.remove();
      }
    };
  }, [i18n.language, clientId]);

  const handleManualClick = () => {
    if ((window as any).google?.accounts?.id) {
      (window as any).google.accounts.id.prompt();
    } else {
      onError(t('googleConnecting', 'Đang kết nối dịch vụ Google Identity... Vui lòng thử lại sau giây lát.'));
    }
  };

  return (
    <div className="relative w-full min-h-[40px] flex items-center justify-center rounded-xl overflow-hidden">
      {/* Nút chuẩn thương hiệu Google hiển thị văn bản theo ngôn ngữ hệ thống khi SDK đang nạp */}
      {!isRendered && (
        <button
          type="button"
          onClick={handleManualClick}
          disabled={disabled}
          className="w-full h-[40px] py-2 px-4 bg-[#131314] hover:bg-[#202124] text-white font-medium rounded-xl border border-slate-700/80 transition-all active:scale-[0.98] text-xs flex items-center justify-center gap-3 cursor-pointer shadow-md select-none"
        >
          <svg className="w-4 h-4 shrink-0" viewBox="0 0 24 24">
            <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" />
            <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" />
            <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z" />
            <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z" />
          </svg>
          <span className="truncate">{t('continueWithGoogle', 'Tiếp tục với tài khoản Google')}</span>
        </button>
      )}

      {/* Nút Google GIS chính hãng tự nạp và hiển thị theo ngôn ngữ hl */}
      <div
        ref={googleBtnRef}
        className={`w-full flex justify-center transition-opacity duration-200 ${
          isRendered ? 'opacity-100' : 'opacity-0 pointer-events-none absolute'
        }`}
      />
    </div>
  );
}
