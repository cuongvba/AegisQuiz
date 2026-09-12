import { defineConfig } from 'vite';
import react            from '@vitejs/plugin-react';
import tailwindcss      from '@tailwindcss/vite';
import { resolve }      from 'path';
import pkg              from './package.json';

export default defineConfig({
  plugins: [react(), tailwindcss()],

  resolve: {
    alias: {
      '@': resolve(__dirname, './src'),
    },
  },

  // ── Define global constants ──────────────────────────────────────
  define: {
    'import.meta.env.VITE_APP_VERSION': JSON.stringify(pkg.version),
  },

  // ── Dev Server ──────────────────────────────────────────────────
  server: {
    port: 3000,
    proxy: {
      // Proxy tất cả /api requests về AegisQuiz backend
      '/api': {
        target:      process.env.VITE_API_URL || 'http://localhost:8080',
        changeOrigin: true,
      },
      // SignalR proctoring hub (WebSocket)
      '/hubs': {
        target:      process.env.VITE_API_URL || 'http://localhost:8080',
        changeOrigin: true,
        ws:           true,
      },
    },
  },

  // ── Build Optimization ─────────────────────────────────────────
  build: {
    target:    'es2022',
    sourcemap:  false,
    // Cảnh báo khi chunk > 500KB
    chunkSizeWarningLimit: 500,

    rollupOptions: {
      output: {
        // Manual chunk splitting — tách vendor lớn ra khỏi app bundle
        manualChunks(id: string) {
          if (id.includes('node_modules/react/') || id.includes('node_modules/react-dom/') || id.includes('node_modules/react-router-dom/')) {
            return 'vendor-react';
          }
          if (id.includes('node_modules/@tanstack/react-query')) {
            return 'vendor-query';
          }
          if (id.includes('node_modules/@microsoft/signalr')) {
            return 'vendor-signalr';
          }
          if (id.includes('node_modules/katex')) {
            return 'vendor-katex';
          }
          if (id.includes('node_modules/recharts')) {
            return 'vendor-recharts';
          }
          if (id.includes('node_modules/i18next') || id.includes('node_modules/react-i18next')) {
            return 'vendor-i18n';
          }
          if (id.includes('node_modules/zod')) {
            return 'vendor-zod';
          }
        },
      },
    },
  },
});

