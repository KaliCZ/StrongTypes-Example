import { readFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { expect, test } from "@playwright/test";

// The committed openapi.json is what the TypeScript client was generated from
// (ADR-0002). If the running API's document differs, the contract drifted:
// refresh with `npm run refresh:api` (see README) and commit the result.
test("the committed OpenAPI snapshot matches the running API", async ({ request }) => {
  const response = await request.get("/swagger/v1/swagger.json");
  expect(response.ok()).toBe(true);

  const liveDocument = (await response.json()) as Record<string, unknown>;
  const snapshotPath = fileURLToPath(new URL("../../openapi.json", import.meta.url));
  const snapshot = JSON.parse(await readFile(snapshotPath, "utf8")) as Record<string, unknown>;

  // The servers block depends on the host the document is served from.
  delete liveDocument["servers"];
  delete snapshot["servers"];

  expect(liveDocument).toEqual(snapshot);
});
