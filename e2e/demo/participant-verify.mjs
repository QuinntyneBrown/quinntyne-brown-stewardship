import { execFile } from "node:child_process";
import { promisify } from "node:util";
import { readFile, writeFile } from "node:fs/promises";
import { join, resolve, dirname } from "node:path";
import assert from "node:assert/strict";
import { createHash } from "node:crypto";

// DEMO-06: verify the actual encoded output, not repository structure.
const output = resolve(process.env.DEMO_OUTPUT ?? ".local/participant-demo");
const exec = promisify(execFile);
const evidence = JSON.parse(
  await readFile(join(output, "evidence.json"), "utf8"),
);
const narration = JSON.parse(
  await readFile(join(output, "narration-timing.json"), "utf8"),
);
assert.equal(narration.voice, "Microsoft David Desktop");
assert.equal(evidence.rehearsal, false);
assert.equal(evidence.pageErrors.length, 0);
assert.equal(evidence.unexpectedResponses.length, 0);
const ffmpeg = process.env.FFMPEG_PATH;
assert.ok(ffmpeg, "Set FFMPEG_PATH to a full FFmpeg distribution.");
const videoPath = join(output, "staging/stewardship-participant.webm");
const { stdout } = await exec(
  join(dirname(ffmpeg), "ffprobe.exe"),
  ["-v", "error", "-show_format", "-show_streams", "-of", "json", videoPath],
  { timeout: 30000 },
);
const media = JSON.parse(stdout);
const video = media.streams.find((s) => s.codec_type === "video");
const audio = media.streams.find((s) => s.codec_type === "audio");
assert.equal(video.width, 1280);
assert.equal(video.height, 720);
assert.equal(audio?.codec_name, "opus");
assert.ok(
  Math.abs(Number(media.format.duration) - evidence.durationSeconds) < 0.3,
);
for (const [index, chapter] of evidence.timeline.entries()) {
  assert.ok(
    chapter.narrationStartSeconds >=
      chapter.startSeconds + chapter.actionSeconds - 0.1,
  );
  assert.ok(
    chapter.narrationStartSeconds + narration.clips[index].duration <=
      (evidence.timeline[index + 1]?.startSeconds ?? evidence.durationSeconds),
  );
}
const decoded = await exec(
  ffmpeg,
  ["-v", "error", "-i", videoPath, "-f", "null", "-"],
  { timeout: 120000, maxBuffer: 1000000 },
);
assert.equal(
  decoded.stderr.trim(),
  "",
  "Every frame and audio packet must decode.",
);
const levels = await exec(
  ffmpeg,
  [
    "-hide_banner",
    "-i",
    videoPath,
    "-vn",
    "-af",
    "volumedetect",
    "-f",
    "null",
    "-",
  ],
  { timeout: 120000, maxBuffer: 1000000 },
);
const meanDb = Number(levels.stderr.match(/mean_volume: (-?[\d.]+) dB/)?.[1]);
assert.ok(
  meanDb > -40 && meanDb < -5,
  `Narration level ${meanDb} dB is not usable.`,
);
const sha256 = createHash("sha256")
  .update(await readFile(videoPath))
  .digest("hex");
await writeFile(
  join(output, "media-verification.json"),
  JSON.stringify(
    {
      ...media,
      meanDb,
      fullDecode: true,
      sha256,
      verifiedAt: new Date().toISOString(),
    },
    null,
    2,
  ),
);
console.log(
  `PASS DEMO-06: ${Number(media.format.duration).toFixed(2)} seconds, 1280 × 720, Opus audio (${meanDb} dB), full decode.`,
);
