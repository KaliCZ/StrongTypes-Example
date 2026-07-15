import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import StarRating from "../../src/components/StarRating.vue";

describe("StarRating", () => {
  it("renders the filled overlay proportional to the value", () => {
    const wrapper = mount(StarRating, { props: { value: 3.5 } });
    expect(wrapper.find(".stars-filled").attributes("style")).toContain("width: 70%");
  });

  it("announces 'not yet rated' for a null value", () => {
    const wrapper = mount(StarRating, { props: { value: null } });
    expect(wrapper.find(".star-rating").attributes("aria-label")).toBe("Not yet rated");
  });

  it("emits the selected star count in editable mode", async () => {
    const wrapper = mount(StarRating, { props: { value: 2, editable: true } });
    await wrapper.findAll("button.star-button")[3]!.trigger("click");
    expect(wrapper.emitted("select")).toEqual([[4]]);
  });

  it("fills exactly the chosen stars in editable mode", () => {
    const wrapper = mount(StarRating, { props: { value: 3, editable: true } });
    const filled = wrapper.findAll("button.star-button.filled");
    expect(filled).toHaveLength(3);
  });
});
