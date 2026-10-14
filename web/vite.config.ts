import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import path from 'node:path'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': path.resolve(import.meta.dirname, 'src') },
  },
  server: {
    proxy: {
      "/api": {
        target: "https://localhost",
        secure: false,
      },
      "/media": {
        target: "http://localhost:3902",
        rewrite: (path) => path.replace(/^\/media/, ""),
        headers: { Host: "media.web.garage.localhost" },
      },
    },
  },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: "./src/test/setup.ts",
  },
});
