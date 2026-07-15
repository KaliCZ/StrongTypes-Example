import { expect, test, type Page } from "@playwright/test";
import { criticUser, demoUser, signIn, signOut } from "./helpers/auth";

// One continuous story on a fresh (throwaway) E2E database: the demo user writes and
// edits a review, a second user votes on it. Serial by design — each step builds on
// the previous one.
test.describe.configure({ mode: "serial" });

const productPath = "/products/usb-c-cable-pack";

async function openProduct(page: Page): Promise<void> {
  await page.goto(productPath);
  await expect(page.getByRole("heading", { level: 1 })).toContainText("USB-C Cable");
}

test("the demo user signs in and publishes a review", async ({ page }) => {
  await openProduct(page);
  await signIn(page, demoUser);

  await openProduct(page);
  const form = page.locator(".review-form");
  await expect(form).toBeVisible();

  await form.locator(".star-button").nth(4).click();
  await form.getByLabel("Title").fill("Cables that just work");
  await form.getByLabel("Review", { exact: true }).fill("Three cables, zero problems after a month of daily use.");
  await form.getByLabel(/Pros/).fill("Braided jacket feels premium");
  await form.getByRole("button", { name: "Publish review" }).click();

  const ownCard = page.locator(".review-card.mine");
  await expect(ownCard).toContainText("Cables that just work");
  await expect(ownCard).toContainText("Your review");
  await expect(ownCard).toContainText("Braided jacket feels premium");
  await expect(ownCard).toContainText(demoUser.name);

  // A second submission is not offered — one review per reviewer per product.
  await expect(page.locator(".review-form")).toHaveCount(0);
});

test("the demo user edits the review and clears the pros line", async ({ page }) => {
  await openProduct(page);
  await signIn(page, demoUser);

  await openProduct(page);
  await page.locator(".review-card.mine").getByRole("button", { name: "Edit" }).click();

  const form = page.locator(".review-form");
  await form.locator(".star-button").nth(3).click();
  await form.getByLabel(/Pros/).fill("");
  await form.getByRole("button", { name: "Save changes" }).click();

  const ownCard = page.locator(".review-card.mine");
  await expect(ownCard).toContainText("edited");
  await expect(ownCard).not.toContainText("Braided jacket feels premium");
});

test("another user votes the review helpful and the score updates", async ({ page }) => {
  await openProduct(page);
  await signIn(page, criticUser);

  await openProduct(page);
  const demoReview = page.locator(".review-card", { hasText: "Cables that just work" });
  await expect(demoReview.locator(".score")).toHaveText("0");

  await demoReview.getByRole("button", { name: "👍 Helpful" }).click();
  await expect(demoReview.locator(".score")).toHaveText("+1");
  await expect(demoReview.getByRole("button", { name: "👍 Helpful" })).toHaveClass(/active/);

  await signOut(page);
});

test("the demo user deletes the review, freeing the slot", async ({ page }) => {
  await openProduct(page);
  await signIn(page, demoUser);

  await openProduct(page);
  page.on("dialog", (dialog) => void dialog.accept());
  await page.locator(".review-card.mine").getByRole("button", { name: "Delete" }).click();

  await expect(page.locator(".review-card.mine")).toHaveCount(0);
  // The submit form is back — deleting freed the one-review-per-product slot.
  await expect(page.locator(".review-form")).toBeVisible();
});
