import { describe, expect, it } from "vitest";
import type { Review } from "../../src/api/client";
import { buildEditReviewBody, hasChanges, type ReviewFormValues } from "../../src/api/editReviewBody";

function createReview(overrides: Partial<Review> = {}): Review {
  return {
    id: "0197e000-0000-7000-8000-000000000001",
    rating: 4,
    title: "Original title",
    body: "Original body",
    pros: "Original pro",
    cons: null,
    authorName: "Alice",
    score: 0,
    mine: true,
    myVote: null,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: null,
    ...overrides,
  };
}

function formValues(review: Review, overrides: Partial<ReviewFormValues> = {}): ReviewFormValues {
  return {
    rating: review.rating,
    title: review.title,
    body: review.body,
    pros: review.pros ?? "",
    cons: review.cons ?? "",
    ...overrides,
  };
}

describe("buildEditReviewBody — the three-state Maybe contract", () => {
  it("sends nothing when nothing changed", () => {
    const review = createReview();
    const body = buildEditReviewBody(review, formValues(review));
    expect(body).toEqual({});
    expect(hasChanges(body)).toBe(false);
  });

  it("sends only the fields that changed", () => {
    const review = createReview();
    const body = buildEditReviewBody(review, formValues(review, { rating: 2, title: "New title" }));
    expect(body).toEqual({ rating: 2, title: "New title" });
  });

  it("clearing an optional field sends the empty Maybe ({} = None)", () => {
    const review = createReview({ pros: "Original pro" });
    const body = buildEditReviewBody(review, formValues(review, { pros: "" }));
    expect(body).toEqual({ pros: {} });
  });

  it("setting an optional field sends Maybe.Some ({ Value })", () => {
    const review = createReview({ cons: null });
    const body = buildEditReviewBody(review, formValues(review, { cons: "Battery drains fast" }));
    expect(body).toEqual({ cons: { Value: "Battery drains fast" } });
  });

  it("an untouched optional field is omitted entirely (leave unchanged)", () => {
    const review = createReview({ pros: "Original pro", cons: "Original con" });
    const body = buildEditReviewBody(review, formValues(review, { title: "New title" }));
    expect(body).toEqual({ title: "New title" });
    expect(body.pros).toBeUndefined();
    expect(body.cons).toBeUndefined();
  });
});
