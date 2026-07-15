/** Turns an RFC 7807 problem (plain or validation) into a single displayable message. */
export function problemMessage(problem: unknown): string {
  if (typeof problem === "object" && problem !== null) {
    const details = problem as { errors?: Record<string, string[]>; detail?: string; title?: string };
    if (details.errors) {
      const firstField = Object.values(details.errors).find((messages) => messages.length > 0);
      if (firstField?.[0]) {
        return firstField[0];
      }
    }
    if (details.detail) {
      return details.detail;
    }
    if (details.title) {
      return details.title;
    }
  }
  return "Something went wrong. Please try again.";
}
