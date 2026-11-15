import { defineConfig, devices } from "@playwright/test";

const url = new URL(process.env.FORMA_E2E_URL ?? "http://localhost:5080/");
if (!url.pathname.endsWith("/")) url.pathname += "/";
url.search = "";
url.hash = "";
const baseURL = url.href;

export default defineConfig({
  testDir: ".",
  fullyParallel: true,
  timeout: 45_000,
  expect: { timeout: 12_000 },
  use: {
    baseURL,
    timezoneId: "UTC",
    trace: "retain-on-failure",
  },
  projects: [
    {
      name: "chromium",
      use: {
        ...devices["Desktop Chrome"],
        launchOptions: process.env.CHROMIUM_PATH
          ? { executablePath: process.env.CHROMIUM_PATH }
          : {},
      },
    },
    { name: "firefox", use: { ...devices["Desktop Firefox"] } },
    { name: "webkit", use: { ...devices["Desktop Safari"] } },
    {
      name: "android-emulated",
      use: {
        ...devices["Pixel 7"],
        launchOptions: process.env.CHROMIUM_PATH
          ? { executablePath: process.env.CHROMIUM_PATH }
          : {},
      },
      grep: /@mobile/,
    },
    { name: "ios-emulated", use: { ...devices["iPhone 13"] }, grep: /@mobile/ },
  ],
});
