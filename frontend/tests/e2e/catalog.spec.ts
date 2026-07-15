import { expect, test } from "@playwright/test";

test("the seeded catalog is browsable without signing in", async ({ page }) => {
  await page.goto("/");

  await expect(page.locator(".product-card")).toHaveCount(10);
  await expect(page.locator(".product-card").first()).toContainText("Sony WH-1000XM5");

  await page.getByRole("link", { name: /Sony WH-1000XM5/ }).click();
  await expect(page.getByRole("heading", { level: 1 })).toContainText("Sony WH-1000XM5");
  await expect(page.locator(".review-card").first()).toBeVisible();
});

test("reviews can be sorted and filtered by stars", async ({ page }) => {
  await page.goto("/products/acme-smartwatch");

  await expect(page.locator(".review-card")).toHaveCount(4);

  // Only the 1★ and 2★ reviews remain after filtering.
  await page.locator(".star-filter label", { hasText: "1★" }).locator("input").check();
  await page.locator(".star-filter label", { hasText: "2★" }).locator("input").check();
  await expect(page.locator(".review-card")).toHaveCount(3);

  await page.getByLabel("Sort by").selectOption("Newest");
  await expect(page.locator(".review-card")).toHaveCount(3);
});

test("an unknown product shows a friendly not-found message", async ({ page }) => {
  await page.goto("/products/definitely-not-a-product");
  await expect(page.locator(".error-banner")).toContainText("does not exist");
});
