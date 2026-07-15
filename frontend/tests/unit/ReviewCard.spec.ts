import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import type { Review } from "../../src/api/client";
import ReviewCard from "../../src/components/ReviewCard.vue";

function createReview(overrides: Partial<Review> = {}): Review {
  return {
    id: "0197e000-0000-7000-8000-000000000001",
    rating: 4,
    title: "Solid product",
    body: "Does what it promises.",
    pros: "Sturdy",
    cons: null,
    authorName: "Alice",
    score: 3,
    mine: false,
    myVote: null,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: null,
    ...overrides,
  };
}

describe("ReviewCard", () => {
  it("marks the viewer's own review and never offers voting on it", () => {
    const wrapper = mount(ReviewCard, { props: { review: createReview({ mine: true }), canVote: true } });
    expect(wrapper.find(".badge").text()).toBe("Your review");
    expect(wrapper.findAll(".vote-button")).toHaveLength(0);
    expect(wrapper.text()).toContain("You can't vote on your own review");
    expect(wrapper.find(".own-actions").exists()).toBe(true);
  });

  it("disables vote buttons for signed-out viewers", () => {
    const wrapper = mount(ReviewCard, { props: { review: createReview(), canVote: false } });
    for (const button of wrapper.findAll(".vote-button")) {
      expect(button.attributes("disabled")).toBeDefined();
    }
  });

  it("emits vote when casting and removeVote when clicking the active direction", async () => {
    const withoutVote = mount(ReviewCard, { props: { review: createReview(), canVote: true } });
    await withoutVote.findAll(".vote-button")[0]!.trigger("click");
    expect(withoutVote.emitted("vote")).toEqual([[true]]);

    const withUpvote = mount(ReviewCard, { props: { review: createReview({ myVote: true }), canVote: true } });
    await withUpvote.findAll(".vote-button")[0]!.trigger("click");
    expect(withUpvote.emitted("removeVote")).toHaveLength(1);
    expect(withUpvote.emitted("vote")).toBeUndefined();
  });

  it("shows pros and cons lines only when present", () => {
    const wrapper = mount(ReviewCard, {
      props: { review: createReview({ pros: "Light", cons: "Pricey" }), canVote: true },
    });
    expect(wrapper.find(".review-pro").text()).toContain("Light");
    expect(wrapper.find(".review-con").text()).toContain("Pricey");

    const bare = mount(ReviewCard, { props: { review: createReview({ pros: null, cons: null }), canVote: true } });
    expect(bare.find(".review-pro").exists()).toBe(false);
    expect(bare.find(".review-con").exists()).toBe(false);
  });
});
