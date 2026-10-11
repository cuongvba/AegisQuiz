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

  // ── Build Optimization (World-Class Chunk Splitting) ─────────────
  build: {
    target:    'es2022',
    sourcemap:  false,
    chunkSizeWarningLimit: 300,

    rollupOptions: {
      output: {
        manualChunks(id: string) {
          // Vendor Isolation
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
          if (id.includes('node_modules/lucide-react')) {
            return 'vendor-icons';
          }
          if (id.includes('node_modules/axios')) {
            return 'vendor-axios';
          }

          // Admin Ingestion Studios Isolation (On-Demand Bundles)
          if (id.includes('/admin/questions/components/docx/')) {
            return 'admin-docx-studio';
          }
          if (id.includes('/admin/questions/components/ExcelIngestionStudioModal')) {
            return 'admin-excel-studio';
          }
          if (id.includes('/admin/questions/components/RuleStudioTab')) {
            return 'admin-cig-studio';
          }
          if (id.includes('/admin/questions/components/BulkTaxonomyModal')) {
            return 'admin-taxonomy-studio';
          }
        },
      },
    },
  },
});
