/// <reference types="vitest/config" />
import vue from "@vitejs/plugin-vue";
import { defineConfig } from "vite";

// PORT and API_PROXY_TARGET are injected by the Aspire AppHost. The proxy keeps the
// browser same-origin with the API — no CORS, no API URL in browser code (ADR-0006).
const port = Number(process.env.PORT ?? 5173);
const apiProxyTarget = process.env.API_PROXY_TARGET ?? "http://localhost:5000";
// /swagger rides along so the OpenAPI document (and its UI) is reachable from the
// frontend origin — the E2E contract test reads it there.
const proxy = {
  "/api": { target: apiProxyTarget, changeOrigin: true },
  "/swagger": { target: apiProxyTarget, changeOrigin: true },
};

export default defineConfig({
  plugins: [vue()],
  server: { port, strictPort: true, proxy },
  preview: { port, strictPort: true, proxy },
  test: {
    environment: "happy-dom",
    include: ["tests/unit/**/*.spec.ts"],
  },
});
