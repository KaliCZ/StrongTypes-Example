import { describe, expect, it } from "vitest";
import { problemMessage } from "../../src/api/problem";

describe("problemMessage", () => {
  it("prefers the first field error of a ValidationProblemDetails", () => {
    expect(problemMessage({ title: "Bad Request", errors: { title: ["Title must not be empty."] } })).toBe(
      "Title must not be empty.",
    );
  });

  it("falls back to detail, then title", () => {
    expect(problemMessage({ title: "Conflict", detail: "You already reviewed this product." })).toBe(
      "You already reviewed this product.",
    );
    expect(problemMessage({ title: "Conflict" })).toBe("Conflict");
  });

  it("gives a generic message for unrecognized shapes", () => {
    expect(problemMessage(undefined)).toBe("Something went wrong. Please try again.");
  });
});
