/**
 * AegisQuiz — ErrorBoundary
 * ==========================
 * Global error boundary để bắt lỗi runtime từ lazy-loaded pages.
 * Giải quyết GAP-10: Không có ErrorBoundary.
 *
 * Features:
 *  ✅ Catch render errors từ bất kỳ component nào
 *  ✅ Hiển thị UI thân thiện thay vì crash toàn app
 *  ✅ Retry mechanism
 *  ✅ Report lỗi (extensible cho Sentry/LogRocket)
 *
 * FSD Layer: shared/ui/
 */

import { Component, type ErrorInfo, type ReactNode } from 'react';

interface ErrorBoundaryProps {
  children: ReactNode;
  /** Custom fallback UI. Nếu không có, dùng default. */
  fallback?: ReactNode;
  /** Callback khi có lỗi — dùng để report lên error tracking */
  onError?: (error: Error, info: ErrorInfo) => void;
}

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, info: ErrorInfo): void {
    console.error('[AegisQuiz ErrorBoundary]', error, info);
    this.props.onError?.(error, info);
  }

  handleRetry = (): void => {
    this.setState({ hasError: false, error: null });
  };

  render(): ReactNode {
    if (!this.state.hasError) {
      return this.props.children;
    }

    if (this.props.fallback) {
      return this.props.fallback;
    }

    return (
      <div
        role="alert"
        aria-live="assertive"
        style={{
          minHeight:      '100vh',
          display:        'flex',
          alignItems:     'center',
          justifyContent: 'center',
          padding:        '2rem',
          background:     'var(--surface-page, #f8fafc)',
        }}
      >
        <div
          style={{
            maxWidth:     '480px',
            width:        '100%',
            textAlign:    'center',
            padding:      '2.5rem',
            background:   'var(--surface-primary, #fff)',
            borderRadius: 'var(--radius-3xl, 1.5rem)',
            boxShadow:    'var(--shadow-xl)',
            border:       '1px solid var(--border-default, #e2e8f0)',
          }}
        >
          {/* Icon */}
          <div style={{ fontSize: '3.5rem', marginBottom: '1rem' }} aria-hidden="true">
            ⚠️
          </div>

          <h1
            style={{
              fontSize:    'var(--text-xl, 1.25rem)',
              fontWeight:  'var(--font-bold, 700)',
              color:       'var(--text-primary, #1e293b)',
              marginBottom:'0.5rem',
            }}
          >
            Đã xảy ra lỗi không mong muốn
          </h1>

          <p
            style={{
              fontSize:    'var(--text-sm, 0.875rem)',
              color:       'var(--text-secondary, #64748b)',
              marginBottom:'1.5rem',
              lineHeight:  '1.6',
            }}
          >
            Hệ thống gặp sự cố. Vui lòng thử lại hoặc tải lại trang.
          </p>

          {/* Error detail — only in dev */}
          {import.meta.env.DEV && this.state.error && (
            <details
              style={{
                marginBottom: '1.5rem',
                textAlign:    'start',
                background:   'var(--state-danger-bg, #ffe4e6)',
                padding:      '1rem',
                borderRadius: 'var(--radius-lg, 0.5rem)',
                fontSize:     'var(--text-xs, 0.75rem)',
                color:        'var(--state-danger-text, #e11d48)',
              }}
            >
              <summary style={{ cursor: 'pointer', fontWeight: '600' }}>
                Chi tiết lỗi (DEV only)
              </summary>
              <pre style={{ marginTop: '0.5rem', whiteSpace: 'pre-wrap', wordBreak: 'break-all' }}>
                {this.state.error.message}
                {'\n\n'}
                {this.state.error.stack}
              </pre>
            </details>
          )}

          <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'center', flexWrap: 'wrap' }}>
            <button
              onClick={this.handleRetry}
              style={{
                padding:      '0.625rem 1.5rem',
                background:   'var(--brand-gradient, linear-gradient(135deg,#06b6d4,#2563eb))',
                color:        '#fff',
                border:       'none',
                borderRadius: 'var(--radius-full, 9999px)',
                fontWeight:   '700',
                fontSize:     'var(--text-sm, 0.875rem)',
                cursor:       'pointer',
              }}
              aria-label="Thử lại"
            >
              🔄 Thử lại
            </button>

            <button
              onClick={() => window.location.reload()}
              style={{
                padding:      '0.625rem 1.5rem',
                background:   'var(--surface-secondary, #f1f5f9)',
                color:        'var(--text-primary, #1e293b)',
                border:       '1px solid var(--border-default, #e2e8f0)',
                borderRadius: 'var(--radius-full, 9999px)',
                fontWeight:   '700',
                fontSize:     'var(--text-sm, 0.875rem)',
                cursor:       'pointer',
              }}
              aria-label="Tải lại trang"
            >
              Tải lại trang
            </button>
          </div>
        </div>
      </div>
    );
  }
}
