import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./performance",
  outputDir: "../.local/browser-performance-results",
  workers: 1,
  timeout: 60000,
  expect: { timeout: 15000 },
  reporter: [
    ["list"],
    ["json", { outputFile: "../.local/web-performance.json" }],
  ],
  use: {
    baseURL: "http://localhost:4319",
    viewport: { width: 390, height: 844 },
    trace: "retain-on-failure",
  },
  webServer: {
    command: "node ../scripts/serve-performance.mjs",
    url: "http://localhost:4319",
    reuseExistingServer: false,
  },
});
