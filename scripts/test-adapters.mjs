import { build } from "../frontend/node_modules/esbuild/lib/main.js";
import { spawnSync } from "node:child_process";
await build({
  entryPoints: ["frontend/tests/adapter-acceptance.ts"],
  outfile: ".local/adapter-acceptance.mjs",
  bundle: true,
  platform: "node",
  format: "esm",
  target: "node22",
});
const result = spawnSync(process.execPath, [".local/adapter-acceptance.mjs"], {
  stdio: "inherit",
});
if (result.error) throw result.error;
process.exit(result.status ?? 1);
