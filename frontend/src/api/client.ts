import createClient, { type Middleware } from "openapi-fetch";
import type { components, paths } from "./schema";

// Convenience aliases over the generated schema — the ONLY source of API types (ADR-0002).
export type ProductSummary = components["schemas"]["ProductSummaryResponse"];
export type ProductDetail = components["schemas"]["ProductDetailResponse"];
export type Review = components["schemas"]["ReviewResponse"];
export type ReviewsPage = components["schemas"]["ReviewsPageResponse"];
export type SubmitReviewRequest = components["schemas"]["SubmitReviewRequest"];
export type EditReviewRequest = components["schemas"]["EditReviewRequest"];
export type VoteResult = components["schemas"]["VoteResponse"];
export type ReviewSortOption = components["schemas"]["ReviewSortOption"];

// Set once at startup; kept as a callback so this module never imports the auth store.
let tokenProvider: (() => string | null) | null = null;

export function setAuthTokenProvider(provider: () => string | null): void {
  tokenProvider = provider;
}

const authorization: Middleware = {
  onRequest({ request }) {
    const token = tokenProvider?.();
    if (token) {
      request.headers.set("Authorization", `Bearer ${token}`);
    }
    return request;
  },
};

/** Typed client over the generated schema; calls go to the same-origin `/api` proxy. */
export const api = createClient<paths>({ baseUrl: "/" });
api.use(authorization);
