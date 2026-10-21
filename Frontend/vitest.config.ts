import { resolve } from 'path';
import { defineConfig } from 'vitest/config';

/**
 * Vitest tách riêng khỏi vite.config.ts để không kéo plugin Tailwind/React vào test thuần logic.
 * Test nằm ở Frontend/tests (ngoài src) → không ảnh hưởng `tsc -b` và bundle production.
 */
export default defineConfig({
  resolve: {
    alias: { '@': resolve(__dirname, './src') },
  },
  test: {
    include: ['tests/**/*.test.ts'],
    environment: 'node',
  },
});
