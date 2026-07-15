// Refreshes the committed OpenAPI snapshot from a running API (default: the dev
// stack's frontend origin, which proxies /swagger). Follow with generate:api —
// `npm run refresh:api` does both.
import { writeFile } from "node:fs/promises";

const sourceUrl = process.env.OPENAPI_SOURCE_URL ?? "http://localhost:5173/swagger/v1/swagger.json";

const response = await fetch(sourceUrl);
if (!response.ok) {
  console.error(`Could not fetch ${sourceUrl} (${response.status}). Is the stack running (aspire run)?`);
  process.exit(1);
}

const document = await response.text();
await writeFile(new URL("../openapi.json", import.meta.url), document);
console.log(`openapi.json refreshed from ${sourceUrl}`);
