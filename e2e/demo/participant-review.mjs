import { createServer } from "node:http";
import { createReadStream } from "node:fs";
import { mkdir, readFile, stat, writeFile } from "node:fs/promises";
import { resolve, join } from "node:path";
import { chromium } from "../node_modules/@playwright/test/index.mjs";
import { createHash } from "node:crypto";

// Watch the encoded artifact at normal speed, with seeking disabled during the review.
const output = resolve(process.env.DEMO_OUTPUT ?? ".local/participant-demo");
const staging = join(output, "staging");
const chapters = JSON.parse(
  await readFile(
    join(staging, "stewardship-participant-chapters.json"),
    "utf8",
  ),
);
const videoPath = join(staging, "stewardship-participant.webm");
const file = await stat(videoPath);
const server = createServer((req, res) => {
  if (req.url === "/") {
    res.writeHead(200, { "Content-Type": "text/html" });
    res.end(
      '<!doctype html><html lang="en"><title>Stewardship demo playback review</title><style>body{margin:0;background:#111512}video{display:block;width:1280px;height:720px}</style><video controls src="/video.webm"></video></html>',
    );
  } else if (req.url === "/video.webm") {
    const range = req.headers.range?.match(/^bytes=(\d+)-(\d*)$/);
    const start = range ? Number(range[1]) : 0;
    const end = range?.[2]
      ? Math.min(Number(range[2]), file.size - 1)
      : file.size - 1;
    if (start > end || start >= file.size) {
      res.writeHead(416);
      res.end();
      return;
    }
    res.writeHead(range ? 206 : 200, {
      "Content-Type": "video/webm",
      "Accept-Ranges": "bytes",
      "Content-Length": end - start + 1,
      ...(range
        ? { "Content-Range": `bytes ${start}-${end}/${file.size}` }
        : {}),
    });
    createReadStream(videoPath, { start, end }).pipe(res);
  } else {
    res.writeHead(404);
    res.end();
  }
});
let browser;
try {
  await new Promise((resolve) => server.listen(0, "127.0.0.1", resolve));
  browser = await chromium.launch({
    channel: "chrome",
    headless: true,
    args: ["--autoplay-policy=no-user-gesture-required"],
  });
  const page = await browser.newPage({
    viewport: { width: 1280, height: 720 },
  });
  await page.goto(`http://127.0.0.1:${server.address().port}`);
  await page.evaluate(async () => {
    const video = document.querySelector("video");
    window.review = { seeking: 0, errors: [], rateChanges: 0 };
    video.addEventListener("seeking", () => window.review.seeking++);
    video.addEventListener("error", () =>
      window.review.errors.push(video.error?.message),
    );
    video.addEventListener("ratechange", () => window.review.rateChanges++);
    video.playbackRate = 1;
    await video.play();
  });
  const startedAt = Date.now();
  const frames = join(output, "review");
  await mkdir(frames, { recursive: true });
  const checkpoints = [
    { name: "opening", time: 1 },
    ...chapters.map((chapter, index) => ({
      name: `${String(index + 1).padStart(2, "0")}-${chapter.title.replaceAll(/[^a-z0-9]+/gi, "-").toLowerCase()}`,
      time: chapter.narrationStartSeconds + 2,
    })),
    { name: "ending", time: chapters.at(-1).endSeconds - 0.8 },
  ];
  for (const checkpoint of checkpoints) {
    await page.waitForFunction(
      (time) => document.querySelector("video").currentTime >= time,
      checkpoint.time,
      { timeout: 60000 },
    );
    const state = await page.evaluate(() => {
      const video = document.querySelector("video");
      return {
        time: video.currentTime,
        width: video.videoWidth,
        height: video.videoHeight,
        rate: video.playbackRate,
        muted: video.muted,
        ...window.review,
      };
    });
    if (
      state.errors.length ||
      state.seeking ||
      state.rate !== 1 ||
      state.width !== 1280 ||
      state.height !== 720
    )
      throw new Error(`Playback failed: ${JSON.stringify(state)}`);
    // Canvas captures the decoded movie itself, excluding player controls.
    const png = await page.evaluate(() => {
      const video = document.querySelector("video");
      const canvas = document.createElement("canvas");
      canvas.width = 1280;
      canvas.height = 720;
      canvas.getContext("2d").drawImage(video, 0, 0);
      return canvas.toDataURL("image/png").split(",")[1];
    });
    await writeFile(
      join(frames, `${checkpoint.name}.png`),
      Buffer.from(png, "base64"),
    );
    console.log(`Reviewed ${state.time.toFixed(1)}s: ${checkpoint.name}`);
  }
  await page.waitForFunction(
    () => document.querySelector("video").ended,
    null,
    { timeout: 10000 },
  );
  const result = await page.evaluate(() => {
    const video = document.querySelector("video");
    const quality = video.getVideoPlaybackQuality();
    return {
      ...window.review,
      duration: video.duration,
      quality: {
        totalVideoFrames: quality.totalVideoFrames,
        droppedVideoFrames: quality.droppedVideoFrames,
        corruptedVideoFrames: quality.corruptedVideoFrames,
      },
    };
  });
  const sha256 = createHash("sha256")
    .update(await readFile(videoPath))
    .digest("hex");
  await writeFile(
    join(output, "playback-review.json"),
    JSON.stringify(
      {
        ...result,
        sha256,
        wallSeconds: (Date.now() - startedAt) / 1000,
        reviewedAt: new Date().toISOString(),
        checkpoints,
      },
      null,
      2,
    ),
  );
  console.log(
    "PASS: complete normal-speed browser playback, without seeking or decoding errors.",
  );
} finally {
  if (browser) await browser.close();
  await new Promise((resolve) => server.close(resolve));
}
