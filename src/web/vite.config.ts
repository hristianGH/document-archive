import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Development only: the same paths nginx serves, sent to the services' launchSettings ports.
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api/archive': { target: 'http://localhost:5099', rewrite: path => path.replace(/^\/api\/archive/, '') },
      '/api/export': { target: 'http://localhost:5186', rewrite: path => path.replace(/^\/api\/export/, '') }
    }
  }
});
