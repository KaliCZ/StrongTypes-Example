import { defineConfig } from "@playwright/test";

// The E2E stack is the real thing: the Aspire AppHost boots Postgres, Zitadel (+ its
// hosted login), the API, and this frontend as a production preview build on :4173.
// Sign-in happens through the real Zitadel form — no bypass (ADR-0007).
const webBaseUrl = "http://localhost:4173";

export default defineConfig({
  testDir: "tests/e2e",
  fullyParallel: false,
  workers: 1,
  timeout: 120_000,
  expect: { timeout: 15_000 },
  use: {
    baseURL: webBaseUrl,
    trace: "retain-on-failure",
  },
  webServer: {
    command: "npm run build && dotnet run --project ../src/ProductReviews.AppHost",
    url: `${webBaseUrl}/api/products`,
    reuseExistingServer: true,
    timeout: 600_000,
    env: { ProductReviews__E2E: "true" },
  },
});
