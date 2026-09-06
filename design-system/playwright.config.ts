import { defineConfig } from "@playwright/test";
export default defineConfig({
  testDir: "./tests",
  use: { baseURL: "http://localhost:4318" },
  webServer: {
    command: "npm start",
    url: "http://localhost:4318",
    reuseExistingServer: false,
  },
});
