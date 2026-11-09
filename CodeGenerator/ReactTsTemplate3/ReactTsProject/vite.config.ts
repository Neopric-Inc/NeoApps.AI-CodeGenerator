import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";

// REACT_APP_* variables (.env or the environment) are exposed as import.meta.env.REACT_APP_*.
export default defineConfig({
  plugins: [react()],
  envPrefix: ["VITE_", "REACT_APP_"],
  resolve: { alias: { neoapps: path.resolve(__dirname, "src/neoapps"), custom: path.resolve(__dirname, "src/custom") } },
  build: { outDir: "build", chunkSizeWarningLimit: 2000 },
  server: { port: 3100 },
  preview: { port: 3100 },
});
