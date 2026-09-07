import { execFile } from "node:child_process";
import { promisify } from "node:util";
import { readFile, writeFile, appendFile, mkdir } from "node:fs/promises";
import { resolve, join, basename } from "node:path";
import { fileURLToPath } from "node:url";
import { scenes as legacyScenes } from "./scenes.mjs";
import { scenes as participantScenes } from "./participant-scenes.mjs";

const exec = promisify(execFile);
const workspace = fileURLToPath(new URL("../../", import.meta.url));
process.chdir(workspace);
const participant = process.argv.includes("--participant");
const scenes = participant ? participantScenes : legacyScenes;
const output = resolve(process.env.DEMO_OUTPUT ?? ".local/live-demo");
const session = participant ? basename(output) : "stewardship-demo";
const width = participant ? 1280 : 1440;
const height = participant ? 620 : 810;
await mkdir(output, { recursive: true });
const run = JSON.parse(await readFile(join(output, "run.json"), "utf8"));
const rehearsal = process.argv.includes("--rehearse");
const speech = participant
  ? JSON.parse(await readFile(join(output, "narration-timing.json"), "utf8"))
  : null;
const config = {
  ...run,
  email: rehearsal ? "rehearsal@demo.invalid" : "participant@demo.invalid",
  note: "Discover notes: lament this before solving it, and name who has least choice.",
  revisedNote:
    "Discover notes: lament this before solving it, and name who has least choice. We will take the problem statement back to them for correction.",
  answer:
    "I will bring one lament, the people it names, and one question I have not resolved.",
  sessionNote:
    "For our conversation: review the discernment and agree which posture fits.",
};
const cliPath =
  process.env.PLAYWRIGHT_CLI_PATH ??
  join(
    process.env.APPDATA,
    "npm/node_modules/@playwright/cli/playwright-cli.js",
  );
const logPath = join(
  output,
  rehearsal ? "rehearsal-cli.log" : "recording-cli.log",
);
await writeFile(logPath, "");
async function cli(...args) {
  let stdout, stderr;
  try {
    ({ stdout, stderr } = await exec(
      process.execPath,
      [cliPath, `-s=${session}`, ...args],
      { maxBuffer: 8_000_000, timeout: 60000 },
    ));
  } catch (error) {
    throw new Error(
      (
        error.stderr || `Playwright CLI failed with exit code ${error.code}`
      ).replaceAll(config.password, "[redacted]"),
    );
  }
  const result = stdout + stderr;
  await appendFile(
    logPath,
    result.replaceAll(config.password, "[redacted]") + "\n",
  );
  if (result.includes("### Error"))
    throw new Error(result.replaceAll(config.password, "[redacted]"));
  if (result.includes("### Modal state"))
    return { modal: result.slice(result.indexOf("### Modal state")) };
  const value = result.match(/### Result\r?\n([\s\S]*?)(?=\r?\n### |$)/)?.[1];
  if (!value) return undefined;
  try {
    return JSON.parse(value.trim());
  } catch {
    return value.trim();
  }
}
const pageFiles = [
  "shell-page",
  "sign-in-page",
  "curriculum-page",
  "module-page",
  "notes-page",
  "note-editor-page",
  "sessions-page",
  "preparation-page",
];
const pageCode = (
  await Promise.all(
    pageFiles.map((name) =>
      readFile(new URL(`./page-objects/${name}.mjs`, import.meta.url), "utf8"),
    ),
  )
)
  .join("\n")
  .replaceAll("export class ", "class ");
const construct = `const config=${JSON.stringify(config)}; const screens={shell:new ShellPage(page,config),signIn:new SignInPage(page,config),curriculum:new CurriculumPage(page,config),module:new ModulePage(page,config),notes:new NotesPage(page,config),editor:new NoteEditorPage(page,config),sessions:new SessionsPage(page,config),preparation:new PreparationPage(page,config)};`;
async function code(body, allowDialog = false) {
  const result = await cli(
    "run-code",
    `async page => { ${pageCode}\n${construct}\n${body}\nreturn {completed:true}; }`,
  );
  if (result?.completed) return;
  if (
    allowDialog &&
    result?.modal?.includes("Leave this note? Your unsaved text will be lost.")
  )
    return;
  throw new Error(
    "The live scene was interrupted before its assertions completed.",
  );
}

await writeFile(
  join(output, "cli.config.json"),
  JSON.stringify(
    {
      browser: {
        browserName: "chromium",
        isolated: true,
        launchOptions: { channel: "chrome", headless: true },
        contextOptions: {
          ignoreHTTPSErrors: true,
          viewport: { width, height },
          deviceScaleFactor: 1,
        },
      },
      outputDir: join(output, "cli"),
      timeouts: { action: 10000, navigation: 20000 },
    },
    null,
    2,
  ),
);
let stopped = false;
if (!participant) await cli("close").catch(() => {});
try {
  await cli(
    "open",
    run.baseUrl + "/sign-in",
    "--config=" + join(output, "cli.config.json"),
  );
  await code(`
  page.__demo={responses:[],pageErrors:[],checks:[],mobileStart:null};
  page.on('response',response=>{ const url=response.url(); if(url.startsWith(config.baseUrl+'/') && (['xhr','fetch'].includes(response.request().resourceType()) || response.status()>=400)) page.__demo.responses.push({method:response.request().method(),path:url.slice(config.baseUrl.length).split('?')[0],status:response.status()}); });
  page.on('pageerror',error=>page.__demo.pageErrors.push(error.message));
  const health=await page.request.get(config.baseUrl+'/health',{maxRetries:2,timeout:10000}); if(health.status()!==200 || (await health.json()).status!=='Healthy') throw new Error('Live database health failed.');
  page.__demo.checks.push('The live HTTPS API and SQL database report Healthy.');
  const favicon=await page.request.get(config.baseUrl+'/favicon.svg'); if(favicon.status()!==200) throw new Error('The application favicon is missing.');
  page.__demo.checks.push('The application favicon returns 200.');
`);
  let startedAt = Date.now();
  if (!rehearsal)
    startedAt = await cli(
      "run-code",
      `async page => { await page.video().start({size:{width:${width},height:${height}}}); return Date.now(); }`,
    );
  const timeline = [];
  let deadline = startedAt;
  try {
    for (const [index, scene] of scenes.entries()) {
      const start = Date.now();
      console.log(
        `${String(index + 1).padStart(2, "0")}/${scenes.length} ${scene.title}`,
      );
      for (const step of scene.steps ?? [{ code: scene.code }]) {
        if (step.command) await cli(...step.command);
        else await code(step.code, step.dialog === true);
      }
      const elapsed = Date.now() - start;
      timeline.push({
        title: scene.title,
        caption: scene.caption,
        startSeconds: (start - startedAt) / 1000,
        actionSeconds: elapsed / 1000,
        allottedSeconds: scene.seconds,
        ...(participant
          ? { narrationStartSeconds: (Date.now() - startedAt) / 1000 + 0.3 }
          : {}),
      });
      deadline = participant
        ? Date.now() + (speech.clips[index].duration + 1.2) * 1000
        : deadline + scene.seconds * 1000;
      if (!rehearsal) {
        if (Date.now() > deadline)
          throw new Error(
            `Scene ${scene.title} exceeded its recording time; review the rehearsal before recording again.`,
          );
        await new Promise((resolve) =>
          setTimeout(resolve, Math.max(0, deadline - Date.now())),
        );
      }
      await writeFile(
        join(
          output,
          rehearsal ? "rehearsal-progress.json" : "recording-progress.json",
        ),
        JSON.stringify(timeline, null, 2),
      );
    }
    const evidence = await cli(
      "run-code",
      "async page => ({responses:page.__demo.responses,pageErrors:page.__demo.pageErrors,checks:page.__demo.checks,mobileStart:page.__demo.mobileStart})",
    );
    const finishedAt = Date.now();
    if (!rehearsal) {
      await cli(
        "run-code",
        `async page => { await page.video().stop({path:${JSON.stringify(join(output, "raw.webm"))}}); return 'Recording saved'; }`,
      );
      stopped = true;
    }
    const unexpected = evidence.responses.filter(
      (response) =>
        response.status >= 400 &&
        !(
          (response.status === 401 &&
            ["/authentication/session", "/authentication/sign-in"].includes(
              response.path,
            )) ||
          (!participant &&
            response.status === 409 &&
            response.path === "/modules/4")
        ),
    );
    const report = {
      baseUrl: run.baseUrl,
      database: run.database,
      recordedAt: new Date().toISOString(),
      rehearsal,
      startedAt,
      timeline,
      durationSeconds: (finishedAt - startedAt) / 1000,
      ...evidence,
      unexpectedResponses: unexpected,
    };
    await writeFile(
      join(output, rehearsal ? "rehearsal.json" : "evidence.json"),
      JSON.stringify(report, null, 2),
    );
    if (evidence.pageErrors.length || unexpected.length)
      throw new Error(
        `Unexpected browser errors: ${JSON.stringify({ errors: evidence.pageErrors, responses: unexpected })}`,
      );
    console.log(
      `PASS: ${scenes.length} live scenes; ${evidence.responses.length} real HTTP responses; no unexpected browser errors.`,
    );
  } finally {
    if (!rehearsal && !stopped)
      await cli(
        "run-code",
        `async page => { await page.video().stop({path:${JSON.stringify(join(output, "interrupted.webm"))}}); }`,
      ).catch(() => {});
  }
} finally {
  if (participant)
    await cli("close").catch((error) => {
      throw new Error(`Demo browser cleanup failed: ${error.message}`);
    });
}
