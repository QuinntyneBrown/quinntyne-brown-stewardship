import { spawnSync } from "node:child_process";
import { cp, mkdir, rm } from "node:fs/promises";
import { resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";
import "./mirror-tokens.mjs";

function run(command, args, cwd = process.cwd()) {
  const result = spawnSync(command, args, {
    cwd,
    stdio: "inherit",
    shell: process.platform === "win32",
  });
  if (result.status !== 0) process.exit(result.status || 1);
}
run("npm", ["run", "build:libraries"], "frontend");
run("npm", ["run", "build"], "frontend");
run("npm", ["run", "build"], "design-system");
const workspace = resolve(fileURLToPath(new URL("../", import.meta.url)));
const target = resolve(workspace, "backend/src/QuinntyneBrownStewardship.Api/wwwroot");
if (!target.startsWith(workspace + sep)) throw new Error("Asset output escaped the workspace.");
// This directory contains only generated assets. Remove old hashed bundles before packaging.
await rm(target, { recursive: true, force: true });
await mkdir(target, { recursive: true });
await cp("frontend/dist/stewardship/browser", target, { recursive: true });
run("dotnet", [
  "build",
  "backend/QuinntyneBrownStewardship.sln",
  "--configuration",
  "Release",
]);
