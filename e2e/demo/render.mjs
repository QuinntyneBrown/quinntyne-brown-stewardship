import { execFile } from "node:child_process";
import { promisify } from "node:util";
import { mkdir, readFile, readdir, writeFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { dirname, join, resolve } from "node:path";
import { scenes } from "./scenes.mjs";

const exec = promisify(execFile);
process.chdir(fileURLToPath(new URL("../../", import.meta.url)));
const output = resolve(".local/live-demo");
await mkdir(join(output, "narration"), { recursive: true });
const narration = [
  "Welcome to Stewardship. This walkthrough uses the running Angular application, its real HTTPS API, and a dedicated SQL database. A protected module first asks us to sign in. Empty fields and incorrect credentials receive clear feedback.",
  "Enrollment is explicit. This prepared account has no cohort, so it sees an explanation instead of an empty curriculum. Once a participant is enrolled, their cohort determines the programme, their mentor, and the available learning path.",
  "Our enrolled participant has five ordered modules, cohort dates, and a named mentor. Progress comes from recorded work. Trying to open a future module returns to the curriculum with an explanation of how to unlock it.",
  "Continue module resumes at the first incomplete section. This participant has completed two of five sections. The reader contains the learning material, every step of the practice assignment, and preparation prompts to bring into a mentor conversation.",
  "Mark a section complete, then reload. The saved resume point survives. Completing the remaining sections unlocks the next module and updates the curriculum. Previously completed reading stays available for review, with its completion status preserved.",
  "Notes belong to the work they describe. From the module, we create a reflection. An empty note cannot be saved. The complete text is submitted to the live API and appears in the participant's notes after saving.",
  "A reflection can change as the work develops. Here we revise it, attempt to leave, and decline the browser's unsaved change warning. The draft remains intact. Saving and reloading confirms that the revised text was persisted.",
  "The notes destination brings together module and session reflections. This account has enough historical notes for another page. More notes retrieves that page from the real API and moves keyboard focus to the newly loaded content.",
  "Sessions include earlier conversations and their notes. For a new conversation, we browse the mentor's published availability. Taken times are labelled and disabled; open times can be selected. Dates and times follow the cohort's time zone.",
  "Select an open time and confirm one booking. The session allowance moves from two to three of five. Returning to the curriculum shows the same upcoming conversation, with its mentor, time, duration, and time zone.",
  "Preparation stays connected to the current module. Open the booked conversation, choose a prompt, and write an answer. After saving, the answer appears beneath the original prompt, ready to review before meeting with the mentor.",
  "A general session note is separate from a prompt answer. We record what to discuss, save it, and return to the conversation. The saved reflection appears in that session's notes, keeping the context with the booking.",
  "Before the change window closes, a booking can move to another open slot. Cancellation asks for confirmation. Keeping the session leaves it in place; confirming cancellation releases the booking and restores the participant's remaining allowance.",
  "This prepared account has a session in twelve hours. The application explains that changes close twenty four hours before the start. Reschedule and cancel are both disabled. The upcoming conversation and its preparation remain visible.",
  "A second prepared account has finished the programme. All five modules are complete, and the cohort has ended. The curriculum remains available for review. Five historical sessions remain visible, while new bookings are unavailable.",
  "The same programme also works on a narrow screen. At three hundred and ninety pixels wide, we sign in using the keyboard, then use the menu to reach curriculum, notes, and sessions. Each screen fits without horizontal scrolling.",
  "Finally, sign out. Browser history and protected links return to sign in, and the API refuses access from the revoked session.",
];
const clips = scenes.map((scene, index) => ({
  text: narration[index],
  path: join(output, "narration", `${String(index + 1).padStart(2, "0")}.wav`),
}));
if (process.argv.includes("--narrate")) {
  await writeFile(
    join(output, "narration.json"),
    JSON.stringify(clips, null, 2),
  );
  const result = await exec(
    "powershell.exe",
    ["-NoProfile", "-File", "e2e/demo/narrate.ps1"],
    { timeout: 120000 },
  );
  console.log(result.stdout.trim());
  process.exit(0);
}

const evidence = JSON.parse(
  await readFile(join(output, "evidence.json"), "utf8"),
);
if (
  evidence.rehearsal ||
  evidence.timeline.length !== scenes.length ||
  evidence.pageErrors.length ||
  evidence.unexpectedResponses.length
) {
  throw new Error(
    "A successful, complete live recording is required before rendering.",
  );
}
let ffmpeg = process.env.FFMPEG_PATH;
if (!ffmpeg) {
  const distribution = (await readdir(join(output, "ffmpeg"))).find((name) =>
    name.startsWith("ffmpeg-"),
  );
  if (!distribution)
    throw new Error("Install a full FFmpeg distribution or set FFMPEG_PATH.");
  ffmpeg = join(output, "ffmpeg", distribution, "bin", "ffmpeg.exe");
}
const ffprobe = join(dirname(ffmpeg), "ffprobe.exe");
async function probe(path) {
  const result = await exec(ffprobe, [
    "-v",
    "error",
    "-show_format",
    "-show_streams",
    "-of",
    "json",
    path,
  ]);
  return JSON.parse(result.stdout);
}
const raw = await probe(join(output, "raw.webm"));
if (Number(raw.format.duration) < 300)
  throw new Error("The live browser recording is shorter than five minutes.");
const assTime = (value) => {
  const cs = Math.round(value * 100);
  return `${Math.floor(cs / 360000)}:${String(Math.floor(cs / 6000) % 60).padStart(2, "0")}:${String(Math.floor(cs / 100) % 60).padStart(2, "0")}.${String(cs % 100).padStart(2, "0")}`;
};
const vttTime = (value) =>
  new Date(Math.round(value * 1000)).toISOString().slice(11, 23);
const escapeAss = (value) =>
  value.replaceAll("\\", "\\\\").replaceAll("{", "\\{").replaceAll("}", "\\}");
let ass = `[Script Info]
ScriptType: v4.00+
PlayResX: 1440
PlayResY: 930
WrapStyle: 0

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: Title,Segoe UI,26,&H005EC8E6,&H005EC8E6,&H00000000,&H00000000,-1,0,0,0,100,100,0,0,1,0,0,7,32,32,22,1
Style: Caption,Segoe UI,22,&H00F5F0E8,&H00F5F0E8,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,0,0,7,32,32,58,1

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
`;
let vtt = "WEBVTT\n\n";
let metadata =
  ";FFMETADATA1\ntitle=Stewardship — five-minute live walkthrough\n";
const filters = [];
const args = ["-y", "-i", join(output, "raw.webm")];
for (const [index, scene] of evidence.timeline.entries()) {
  const start = index === 0 ? 0 : scene.startSeconds;
  const end = evidence.timeline[index + 1]?.startSeconds ?? 300;
  ass += `Dialogue: 0,${assTime(start)},${assTime(end)},Title,,0,0,0,,{\\pos(32,829)}${escapeAss(scene.title)}\n`;
  ass += `Dialogue: 0,${assTime(start)},${assTime(end)},Caption,,0,0,0,,{\\pos(32,869)}${escapeAss(scene.caption)}\n`;
  vtt += `${vttTime(start)} --> ${vttTime(end)}\n${scene.title}\n${scene.caption}\n\n`;
  metadata += `[CHAPTER]\nTIMEBASE=1/1000\nSTART=${Math.round(start * 1000)}\nEND=${Math.round(end * 1000)}\ntitle=${scene.title}\n`;
  const clip = await probe(clips[index].path);
  const duration = Number(clip.format.duration);
  const available = end - start - 1;
  const tempo = Math.max(1, duration / available);
  if (tempo > 1.4) throw new Error(`Narration for ${scene.title} is too long.`);
  args.push("-i", clips[index].path);
  filters.push(
    `[${index + 1}:a]atempo=${tempo.toFixed(5)},adelay=${Math.round((start + 0.4) * 1000)}:all=1[a${index}]`,
  );
}
await writeFile(join(output, "captions.ass"), ass);
await writeFile(join(output, "chapters.vtt"), vtt);
await writeFile(join(output, "chapters.ffmeta"), metadata);
args.push("-f", "ffmetadata", "-i", join(output, "chapters.ffmeta"));
const mobileStart = (evidence.mobileStart - evidence.startedAt) / 1000;
if (!Number.isFinite(mobileStart) || mobileStart <= 0 || mobileStart >= 300)
  throw new Error("The mobile chapter timestamp is missing.");
filters.push("[0:v]split=2[desktop][mobile]");
filters.push(
  "[mobile]crop=390:810:0:0,pad=1440:810:525:0:color=0x111512[centered]",
);
filters.push(
  `[desktop][centered]overlay=enable='gte(t,${mobileStart.toFixed(3)})',pad=1440:930:0:0:color=0x111512,ass=captions.ass[v]`,
);
filters.push(
  `${clips.map((_, index) => `[a${index}]`).join("")}amix=inputs=${clips.length}:normalize=0,apad,atrim=duration=300[a]`,
);
await writeFile(join(output, "render.ffscript"), filters.join(";\n"));
args.push(
  "-filter_complex",
  filters.join(";"),
  "-map",
  "[v]",
  "-map",
  "[a]",
  "-map_metadata",
  String(clips.length + 1),
  "-map_chapters",
  String(clips.length + 1),
  "-c:v",
  "libx264",
  "-preset",
  "medium",
  "-crf",
  "20",
  "-pix_fmt",
  "yuv420p",
  "-r",
  "25",
  "-c:a",
  "aac",
  "-b:a",
  "128k",
  "-t",
  "300",
  "-movflags",
  "+faststart",
  join(output, "stewardship-live-demo-5min.mp4"),
);
console.log(
  "Rendering five minutes of live browser footage with chapter captions and narration.",
);
const rendered = await exec(ffmpeg, args, {
  cwd: output,
  maxBuffer: 8_000_000,
  timeout: 600000,
});
await writeFile(join(output, "render.log"), rendered.stderr);
const final = await probe(join(output, "stewardship-live-demo-5min.mp4"));
const video = final.streams.find((stream) => stream.codec_type === "video");
const audio = final.streams.find((stream) => stream.codec_type === "audio");
if (
  Number(final.format.duration) !== 300 ||
  video.width !== 1440 ||
  video.height !== 930 ||
  video.codec_name !== "h264" ||
  audio?.codec_name !== "aac"
)
  throw new Error("The final video failed its media checks.");
await writeFile(
  join(output, "media-verification.json"),
  JSON.stringify(final, null, 2),
);
const escapeHtml = (value) =>
  value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll('"', "&quot;");
const chapterLinks = evidence.timeline
  .map((scene, index) => {
    const seconds = index === 0 ? 0 : scene.startSeconds;
    const time = `${Math.floor(seconds / 60)}:${String(Math.round(seconds % 60)).padStart(2, "0")}`;
    return `<li><button type="button" data-seconds="${seconds}"><time>${time}</time> ${escapeHtml(scene.title)}</button></li>`;
  })
  .join("\n");
await writeFile(
  join(output, "index.html"),
  `<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><link rel="icon" href="data:,">
<title>Stewardship — five-minute live walkthrough</title>
<style>body{margin:0;background:#111512;color:#e8f0f5;font:18px/1.5 system-ui,sans-serif}main{max-width:1440px;margin:auto;padding:24px}h1{font-size:28px}video{display:block;width:100%;max-height:78vh;background:#000}a{color:#e6c85e}ol{list-style:none;padding:0;columns:2}button{border:0;background:transparent;color:inherit;padding:10px;text-align:left;cursor:pointer;font:inherit}button:hover,button:focus-visible{background:#2b342e}time{color:#e6c85e;font-variant-numeric:tabular-nums;margin-right:12px}@media(max-width:650px){ol{columns:1}}</style>
</head><body><main><h1>Stewardship: the live participant experience</h1>
<p>5:00 · Real HTTPS API and SQL database · Captions and synthetic narration</p>
<video controls preload="metadata" aria-label="Five-minute Stewardship walkthrough" src="stewardship-live-demo-5min.mp4"></video>
<p><a href="stewardship-live-demo-5min.mp4" download>Download MP4</a> · <a href="evidence.json">Recording evidence</a></p>
<ol>${chapterLinks}</ol>
<script>const video=document.querySelector('video');document.querySelectorAll('[data-seconds]').forEach(button=>button.addEventListener('click',()=>{video.currentTime=Number(button.dataset.seconds);video.play().catch(()=>{});video.scrollIntoView({behavior:'smooth',block:'center'});}));</script>
</main></body></html>`,
);
console.log(
  `Saved ${final.format.duration}s, ${video.width}×${video.height}, H.264/AAC: stewardship-live-demo-5min.mp4`,
);
