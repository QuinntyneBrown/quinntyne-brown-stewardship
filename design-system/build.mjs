import { cp, mkdir } from "node:fs/promises";
await mkdir("dist", { recursive: true });
for (const file of [
  "index.html",
  "favicon.svg",
  "tokens.css",
  "base.css",
  "catalogue.css",
  "fonts.css",
  "fonts",
])
  await cp(file, `dist/${file}`, { recursive: true });
