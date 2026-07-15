import { expect, type Page } from "@playwright/test";

export const demoUser = { email: "demo@productreviews.local", name: "Demo Reviewer" };
export const criticUser = { email: "critic@productreviews.local", name: "Casey Critic" };
// Provisioned by the AppHost (ZitadelHosting.DemoUserPassword); documented in the README.
export const demoPassword = "ProductReviews123!";

/** Signs in through the real Zitadel hosted login (Login V2). */
export async function signIn(page: Page, user: { email: string; name: string }): Promise<void> {
  await page.getByRole("button", { name: "Sign in" }).click();

  await page.waitForURL(/ui\/v2\/login/);
  await page.locator("input:not([type=hidden])").first().fill(user.email);
  await page.keyboard.press("Enter");

  const passwordInput = page.locator('input[type="password"]');
  await passwordInput.waitFor();
  await passwordInput.fill(demoPassword);
  await page.keyboard.press("Enter");

  await page.waitForURL((url) => url.origin === "http://localhost:4173");
  await expect(page.locator(".user-name")).toHaveText(user.name);
}

export async function signOut(page: Page): Promise<void> {
  await page.getByRole("button", { name: "Sign out" }).click();
  await page.waitForURL((url) => url.origin === "http://localhost:4173");
  await expect(page.getByRole("button", { name: "Sign in" })).toBeVisible();
}
