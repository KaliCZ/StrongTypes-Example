import type { EditReviewRequest, Review } from "./client";

export interface ReviewFormValues {
  rating: number;
  title: string;
  body: string;
  pros: string;
  cons: string;
}

/**
 * Builds the PATCH body from what actually changed — the three-state `Maybe` contract
 * from the OpenAPI schema (see ADR-0002 and the EditReviewRequest DTO):
 *   - field omitted            → leave unchanged
 *   - `{}` (Maybe.None)        → clear the optional field
 *   - `{ Value: "…" }`         → set the optional field
 * Required fields (rating/title/body) can only be changed, never cleared, so they use
 * plain optional properties.
 */
export function buildEditReviewBody(original: Review, edited: ReviewFormValues): EditReviewRequest {
  const body: EditReviewRequest = {};

  if (edited.rating !== original.rating) {
    body.rating = edited.rating;
  }
  if (edited.title !== original.title) {
    body.title = edited.title;
  }
  if (edited.body !== original.body) {
    body.body = edited.body;
  }
  if (edited.pros !== (original.pros ?? "")) {
    body.pros = edited.pros === "" ? {} : { Value: edited.pros };
  }
  if (edited.cons !== (original.cons ?? "")) {
    body.cons = edited.cons === "" ? {} : { Value: edited.cons };
  }

  return body;
}

export function hasChanges(request: EditReviewRequest): boolean {
  return Object.keys(request).length > 0;
}
