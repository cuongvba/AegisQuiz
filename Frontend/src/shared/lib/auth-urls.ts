/**
 * AegisQuiz — URL helpers for Login / Logout and Redirection
 * Đảm bảo hệ sinh thái dehoc.vn chuyển hướng chính xác về https://daotao.dehoc.vn
 */

export function getAppDomain(): string {
  if (typeof window === 'undefined') return '';
  if (window.location.hostname.endsWith('dehoc.vn')) {
    return 'https://daotao.dehoc.vn';
  }
  return window.location.origin;
}

export function getLoginUrl(): string {
  if (typeof window === 'undefined') return '/login';
  if (window.location.hostname.endsWith('dehoc.vn')) {
    return 'https://daotao.dehoc.vn/login';
  }
  return '/login';
}

export function getAppHomeUrl(): string {
  if (typeof window === 'undefined') return '/';
  if (window.location.hostname.endsWith('dehoc.vn')) {
    return 'https://daotao.dehoc.vn/';
  }
  return '/';
}
