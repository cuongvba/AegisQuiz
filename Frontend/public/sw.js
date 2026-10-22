/**
 * Aegis Service Worker — High-Performance Offline-First Engine (PWA 2026)
 * Quản lý bộ đệm App Shell, font KaTeX và điều hướng ngoại tuyến khi mất sóng Internet.
 */

const CACHE_NAME = 'aegis-app-shell-v1';
const STATIC_ASSETS = [
  '/',
  '/index.html',
  '/manifest.webmanifest',
  '/favicon.svg',
  '/apple-touch-icon.png'
];

// 1. Cài đặt Service Worker và lưu bộ đệm App Shell
self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => {
      return cache.addAll(STATIC_ASSETS);
    }).then(() => self.skipWaiting())
  );
});

// 2. Kích hoạt và dọn dẹp các cache phiên bản cũ
self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((cacheNames) => {
      return Promise.all(
        cacheNames
          .filter((name) => name !== CACHE_NAME)
          .map((name) => caches.delete(name))
      );
    }).then(() => self.clients.claim())
  );
});

// 3. Xử lý Intercept các yêu cầu mạng (Fetch Event)
self.addEventListener('fetch', (event) => {
  const url = new URL(event.request.url);

  // Tuyệt đối không cache API requests và SignalR WebSockets
  if (url.pathname.startsWith('/api') || url.pathname.startsWith('/hubs') || url.pathname.startsWith('/ws')) {
    return;
  }

  // Đối với các tài nguyên tĩnh đã băm tên tệp (JS, CSS, WOFF2, Fonts, Icons)
  if (
    url.pathname.match(/\.(?:js|css|woff2?|ttf|svg|png|jpg|webp|ico)$/) ||
    url.hostname.includes('fonts.gstatic.com') ||
    url.hostname.includes('fonts.googleapis.com')
  ) {
    event.respondWith(
      caches.open(CACHE_NAME).then(async (cache) => {
        const cachedResponse = await cache.match(event.request);
        if (cachedResponse) {
          // Trả về cache ngay lập tức (Cache-First) để đạt 60fps
          return cachedResponse;
        }

        try {
          const networkResponse = await fetch(event.request);
          if (networkResponse && networkResponse.status === 200) {
            cache.put(event.request, networkResponse.clone());
          }
          return networkResponse;
        } catch {
          return cachedResponse;
        }
      })
    );
    return;
  }

  // Đối với các điều hướng trang (SPA Navigation)
  if (event.request.mode === 'navigate') {
    event.respondWith(
      fetch(event.request).catch(async () => {
        const cache = await caches.open(CACHE_NAME);
        const fallback = await cache.match('/index.html');
        return fallback || fetch(event.request);
      })
    );
  }
});

// 4. Lắng nghe thông điệp từ ứng dụng (Client Messages)
self.addEventListener('message', (event) => {
  if (event.data && event.data.type === 'SKIP_WAITING') {
    self.skipWaiting();
  }
});
