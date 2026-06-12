import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // During development, only the SPA files are served from Vite.
    // API calls go directly to the Identity service (CORS is configured there).
  },
});
