import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

const port = process.env.PORT ? parseInt(process.env.PORT) : undefined;

export default defineConfig({
  plugins: [react()],
  server: {
    open: true,
    port,
    proxy: {
      '/api': {
        target: process.env.BACKEND_URL,
        changeOrigin: true,
      }
    }
  },
  build: {
    outDir: 'build'
  }
});
