import { createHash } from "node:crypto";
import { execFileSync } from "node:child_process";
import { readFile, writeFile, mkdir, rename, access } from "node:fs/promises";
import { resolve, join, sep } from "node:path";
import assert from "node:assert/strict";

const output = resolve(process.env.DEMO_OUTPUT ?? ".local/participant-demo");
const root = resolve("docs/demo");
const staging = join(output, "staging");
assert.ok(
  output.startsWith(resolve(".local") + sep),
  "Promotion staging must belong to this workspace.",
);
const readJson = async (name) =>
  JSON.parse(await readFile(join(output, name), "utf8"));
const media = await readJson("media-verification.json");
const playback = await readJson("playback-review.json");
const visual = await readJson("visual-review.json");
const cleanup = await readJson("cleanup.json");
const video = await readFile(join(staging, "stewardship-participant.webm"));
const sha256 = createHash("sha256").update(video).digest("hex");
assert.equal(media.sha256, sha256);
assert.equal(playback.sha256, sha256);
assert.equal(visual.sha256, sha256);
assert.ok(visual.notes?.trim(), "Document the visual inspection.");
assert.equal(
  cleanup.errors.length,
  0,
  "Resolve cleanup failures before delivery.",
);
assert.equal(playback.seeking, 0);
assert.equal(playback.errors.length, 0);
assert.ok(playback.wallSeconds >= playback.duration - 2);
assert.ok(media.fullDecode);
const chapters = JSON.parse(
  await readFile(
    join(staging, "stewardship-participant-chapters.json"),
    "utf8",
  ),
);
const evidence = await readJson("evidence.json");
const revision = execFileSync("git", ["rev-parse", "HEAD"], {
  encoding: "utf8",
  timeout: 10000,
}).trim();
const time = (seconds) =>
  `${Math.floor(seconds / 60)}:${String(Math.floor(seconds % 60)).padStart(2, "0")}`;
const generated = `<!-- generated-demo:start -->
**Recorded and reviewed:** ${evidence.recordedAt.slice(0, 10)}. [Watch the narrated walkthrough](stewardship-participant.webm).

[![FaithTech participant curriculum](stewardship-participant-poster.png)](stewardship-participant.webm)

${time(Number(media.format.duration))} (${Number(media.format.duration).toFixed(2)} seconds) · 1280 × 720 · ${(video.length / 1048576).toFixed(2)} MiB · VP9 / Opus.

[Captions](stewardship-participant.vtt) · [Transcript](stewardship-participant-transcript.md) · [Chapter metadata](stewardship-participant-chapters.json) · [Verification](stewardship-participant-verification.json)

| Time | Verified workflow |
| --- | --- |
${chapters.map((chapter) => `| ${time(chapter.startSeconds)} | ${chapter.title} |`).join("\n")}

All ${evidence.timeline.length} chapters passed against the real API (${evidence.responses.length} observed HTTP responses; no unexpected failures or browser exceptions). Full media decode, normal-speed browser playback, and chapter-frame visual review passed. Cleanup completed without errors.

Source base revision: \`${revision}\`, plus the recording changes delivered alongside these artifacts. SHA-256: \`${sha256}\`.
<!-- generated-demo:end -->`;
const readme = await readFile(join(root, "README.md"), "utf8");
assert.ok(
  readme.includes("<!-- generated-demo:start -->") &&
    readme.includes("<!-- generated-demo:end -->"),
);
await writeFile(
  join(staging, "README.md"),
  readme.replace(
    /<!-- generated-demo:start -->[\s\S]*?<!-- generated-demo:end -->/,
    generated,
  ),
);
await writeFile(
  join(staging, "stewardship-participant-verification.json"),
  JSON.stringify(
    {
      sha256,
      baseRevision: revision,
      recordedAt: evidence.recordedAt,
      chapters: evidence.timeline.length,
      observedHttpResponses: evidence.responses.length,
      checks: evidence.checks,
      unexpectedResponses: evidence.unexpectedResponses,
      pageErrors: evidence.pageErrors,
      media: {
        duration: Number(media.format.duration),
        width: 1280,
        height: 720,
        bytes: video.length,
        meanDb: media.meanDb,
        fullDecode: media.fullDecode,
      },
      playback,
      visual,
      cleanup: { errors: cleanup.errors, completedAt: cleanup.completedAt },
    },
    null,
    2,
  ),
);
const names = [
  "stewardship-participant.webm",
  "stewardship-participant-poster.png",
  "stewardship-participant.vtt",
  "stewardship-participant-transcript.md",
  "stewardship-participant-chapters.json",
  "stewardship-participant-verification.json",
  "README.md",
];
for (const name of names) await access(join(staging, name));
const backup = join(output, "promotion-backup-" + Date.now());
await mkdir(backup);
const backedUp = [],
  promoted = [];
try {
  for (const name of names) {
    try {
      await access(join(root, name));
    } catch (error) {
      if (error.code === "ENOENT") continue;
      throw error;
    }
    await rename(join(root, name), join(backup, name));
    backedUp.push(name);
  }
  for (const name of names) {
    await rename(join(staging, name), join(root, name));
    promoted.push(name);
  }
} catch (error) {
  const rollbackErrors = [];
  for (const name of promoted.reverse()) {
    try {
      await rename(join(root, name), join(staging, name));
    } catch (failure) {
      rollbackErrors.push(failure.message);
    }
  }
  for (const name of backedUp.reverse()) {
    try {
      await rename(join(backup, name), join(root, name));
    } catch (failure) {
      rollbackErrors.push(failure.message);
    }
  }
  if (rollbackErrors.length)
    throw new AggregateError(
      [error, ...rollbackErrors],
      `Promotion rollback incomplete; backup remains at ${backup}`,
    );
  throw error;
}
console.log(
  `Published the verified participant video set to ${root}. Prior files retained at ${backup}.`,
);
