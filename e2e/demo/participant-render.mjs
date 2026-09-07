import { execFile } from "node:child_process";
import { promisify } from "node:util";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import { resolve, join, dirname } from "node:path";
import { scenes } from "./participant-scenes.mjs";

const exec = promisify(execFile);
const output = resolve(process.env.DEMO_OUTPUT ?? ".local/participant-demo");
const ffmpeg = process.env.FFMPEG_PATH;
if (!ffmpeg) throw new Error("Set FFMPEG_PATH to a full FFmpeg distribution.");
const probe = async (path) =>
  JSON.parse(
    (
      await exec(
        join(dirname(ffmpeg), "ffprobe.exe"),
        ["-v", "error", "-show_format", "-show_streams", "-of", "json", path],
        { timeout: 30000 },
      )
    ).stdout,
  );
const voice = "Microsoft David Desktop";
await mkdir(join(output, "narration"), { recursive: true });
if (process.argv.includes("--narrate")) {
  const clips = scenes.map((scene, index) => ({
    text: scene.narration,
    path: join(output, "narration", `${index + 1}.wav`),
  }));
  const input = join(output, "narration.json");
  await writeFile(input, JSON.stringify(clips, null, 2));
  const result = await exec(
    "powershell.exe",
    [
      "-NoProfile",
      "-File",
      "e2e/demo/narrate.ps1",
      "-InputPath",
      input,
      "-VoiceName",
      voice,
    ],
    { timeout: 120000 },
  );
  for (const clip of clips)
    clip.duration = Number((await probe(clip.path)).format.duration);
  await writeFile(
    join(output, "narration-timing.json"),
    JSON.stringify({ voice, clips }, null, 2),
  );
  console.log(result.stdout.trim());
  process.exit(0);
}
const evidence = JSON.parse(
  await readFile(join(output, "evidence.json"), "utf8"),
);
const speech = JSON.parse(
  await readFile(join(output, "narration-timing.json"), "utf8"),
);
if (
  evidence.rehearsal ||
  evidence.timeline.length !== scenes.length ||
  evidence.pageErrors.length ||
  evidence.unexpectedResponses.length ||
  speech.voice !== voice
)
  throw new Error("A complete passing take with David narration is required.");
const duration = evidence.durationSeconds;
const raw = await probe(join(output, "raw.webm"));
if (Number(raw.format.duration) < duration - 0.5)
  throw new Error("The continuous capture ended early.");
const staging = join(output, "staging");
await mkdir(staging, { recursive: true });
const stamp = (value) =>
  new Date(Math.round(value * 1000)).toISOString().slice(11, 23);
const assStamp = (value) => {
  const cs = Math.round(value * 100);
  return `${Math.floor(cs / 360000)}:${String(Math.floor(cs / 6000) % 60).padStart(2, "0")}:${String(Math.floor(cs / 100) % 60).padStart(2, "0")}.${String(cs % 100).padStart(2, "0")}`;
};
const escapeAss = (text) =>
  text.replaceAll("\\", "\\\\").replaceAll("{", "\\{").replaceAll("}", "\\}");
let ass = `[Script Info]
ScriptType: v4.00+
PlayResX: 1280
PlayResY: 720
WrapStyle: 0

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Title,Segoe UI,24,&H005EC8E6,&H005EC8E6,&H00000000,&H00000000,-1,0,0,0,100,100,0,0,1,0,0,7,24,24,0,1
Style: Caption,Segoe UI,21,&H00F5F0E8,&H00F5F0E8,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,0,0,7,24,24,0,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
`;
const args = ["-y", "-i", join(output, "raw.webm")];
const filters = [];
let vtt = "WEBVTT\n\n";
let transcript =
  "# Stewardship participant walkthrough\n\nNarrator: Microsoft David Desktop (Windows synthetic voice).\n\n";
const chapters = [];
for (const [index, chapter] of evidence.timeline.entries()) {
  const start = index === 0 ? 0 : chapter.startSeconds;
  const end = evidence.timeline[index + 1]?.startSeconds ?? duration;
  const clip = speech.clips[index];
  if (
    clip.text !== scenes[index].narration ||
    chapter.narrationStartSeconds + clip.duration > end
  )
    throw new Error(`Narration does not fit ${chapter.title}.`);
  const successStart = chapter.narrationStartSeconds;
  ass += `Dialogue: 0,${assStamp(start)},${assStamp(end)},Title,,0,0,0,,{\\pos(24,631)}${escapeAss(chapter.title)}\n`;
  // Outcome captions appear after the action succeeds; title alone introduces the action.
  ass += `Dialogue: 0,${assStamp(successStart)},${assStamp(end)},Caption,,0,0,0,,{\\pos(24,668)}${escapeAss(chapter.caption)}\n`;
  const sentences = clip.text
    .match(/[^.!?]+[.!?]+|[^.!?]+$/g)
    .map((s) => s.trim());
  const letters = sentences.reduce((sum, s) => sum + s.length, 0);
  let cue = successStart;
  for (const sentence of sentences) {
    const next = cue + (clip.duration * sentence.length) / letters;
    vtt += `${stamp(cue)} --> ${stamp(next)}\n${sentence}\n\n`;
    cue = next;
  }
  transcript += `## ${stamp(start)} — ${chapter.title}\n\n${clip.text}\n\n`;
  chapters.push({
    title: chapter.title,
    startSeconds: start,
    endSeconds: end,
    narrationStartSeconds: successStart,
    caption: chapter.caption,
  });
  args.push("-i", clip.path);
  filters.push(
    `[${index + 1}:a]adelay=${Math.round(successStart * 1000)}:all=1[a${index}]`,
  );
}
const mobileStart = (evidence.mobileStart - evidence.startedAt) / 1000;
if (
  !Number.isFinite(mobileStart) ||
  mobileStart <= 0 ||
  mobileStart >= duration
)
  throw new Error("Missing mobile capture timestamp.");
filters.push("[0:v]split=2[desktop][mobile]");
filters.push(
  "[mobile]crop=390:620:0:0,pad=1280:620:445:0:color=0x111512[centered]",
);
filters.push(
  `[desktop][centered]overlay=enable='gte(t,${mobileStart})',pad=1280:720:0:0:color=0x111512,ass=participant-captions.ass[v]`,
);
filters.push(
  `${speech.clips.map((_, index) => `[a${index}]`).join("")}amix=inputs=${speech.clips.length}:normalize=0,alimiter=limit=0.95,apad,atrim=duration=${duration}[a]`,
);
await writeFile(join(output, "participant-captions.ass"), ass);
await writeFile(
  join(output, "participant-render.ffscript"),
  filters.join(";\n"),
);
await writeFile(join(staging, "stewardship-participant.vtt"), vtt);
await writeFile(
  join(staging, "stewardship-participant-transcript.md"),
  transcript,
);
await writeFile(
  join(staging, "stewardship-participant-chapters.json"),
  JSON.stringify(chapters, null, 2),
);
args.push(
  "-filter_complex",
  filters.join(";\n"),
  "-map",
  "[v]",
  "-map",
  "[a]",
  "-c:v",
  "libvpx-vp9",
  "-crf",
  "30",
  "-b:v",
  "0",
  "-deadline",
  "good",
  "-cpu-used",
  "4",
  "-row-mt",
  "1",
  "-c:a",
  "libopus",
  "-b:a",
  "96k",
  "-t",
  String(duration),
  join(staging, "stewardship-participant.webm"),
);
const result = await exec(ffmpeg, args, {
  cwd: output,
  timeout: 900000,
  maxBuffer: 8000000,
});
await writeFile(join(output, "render.log"), result.stderr);
const posterTime = chapters[1].narrationStartSeconds + 2;
await exec(
  ffmpeg,
  [
    "-y",
    "-ss",
    String(posterTime),
    "-i",
    join(staging, "stewardship-participant.webm"),
    "-frames:v",
    "1",
    join(staging, "stewardship-participant-poster.png"),
  ],
  { timeout: 30000 },
);
console.log(
  `Rendered ${duration.toFixed(1)} seconds with ${chapters.length} narrated chapters to staging.`,
);
