import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import path from 'path';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  server: {
    port: 5173,
    proxy: {
      '/api/auth': {
        target: 'http://localhost:5098',
        changeOrigin: true,
      },
      '/api/health': {
        target: 'http://localhost:5098',
        changeOrigin: true,
      },
      '/api/tasks': {
        target: 'http://localhost:5099',
        changeOrigin: true,
      },
    },
  },
});
