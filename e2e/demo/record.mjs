import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { readFile, writeFile, appendFile, mkdir } from 'node:fs/promises';
import { resolve, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { scenes } from './scenes.mjs';

const exec = promisify(execFile);
const workspace = fileURLToPath(new URL('../../', import.meta.url));
process.chdir(workspace);
const output = resolve('.local/live-demo');
await mkdir(output, { recursive: true });
const run = JSON.parse(await readFile(join(output, 'run.json'), 'utf8'));
const rehearsal = process.argv.includes('--rehearse');
const config = {
  ...run, email: rehearsal ? 'rehearsal@demo.invalid' : 'participant@demo.invalid',
  note: 'Field reflection: listen to the people carrying the cost before choosing a technical solution.',
  revisedNote: 'Field reflection: listen to the people carrying the cost before choosing a technical solution. We will test one small, reversible change and return next week.',
  answer: 'I will bring one decision, the people affected, and one question I have not resolved.',
  sessionNote: 'For our conversation: review the small experiment and agree on what to revisit.',
};
const cliPath = process.env.PLAYWRIGHT_CLI_PATH ?? join(process.env.APPDATA, 'npm/node_modules/@playwright/cli/playwright-cli.js');
const logPath = join(output, rehearsal ? 'rehearsal-cli.log' : 'recording-cli.log');
await writeFile(logPath, '');
async function cli(...args) {
  let stdout, stderr;
  try { ({ stdout, stderr } = await exec(process.execPath, [cliPath, '-s=stewardship-demo', ...args], { maxBuffer: 8_000_000, timeout: 60000 })); }
  catch (error) { throw new Error((error.stderr || `Playwright CLI failed with exit code ${error.code}`).replaceAll(config.password, '[redacted]')); }
  const result = stdout + stderr;
  await appendFile(logPath, result.replaceAll(config.password, '[redacted]') + '\n');
  if (result.includes('### Error')) throw new Error(result.replaceAll(config.password, '[redacted]'));
  const value = result.match(/### Result\r?\n([\s\S]*?)(?=\r?\n### |$)/)?.[1];
  if (!value) return undefined;
  try { return JSON.parse(value.trim()); } catch { return value.trim(); }
}
const pageFiles = ['shell-page', 'sign-in-page', 'curriculum-page', 'module-page', 'notes-page', 'note-editor-page', 'sessions-page', 'preparation-page'];
const pageCode = (await Promise.all(pageFiles.map(name => readFile(new URL(`./page-objects/${name}.mjs`, import.meta.url), 'utf8')))).join('\n').replaceAll('export class ', 'class ');
const construct = `const config=${JSON.stringify(config)}; const screens={shell:new ShellPage(page,config),signIn:new SignInPage(page,config),curriculum:new CurriculumPage(page,config),module:new ModulePage(page,config),notes:new NotesPage(page,config),editor:new NoteEditorPage(page,config),sessions:new SessionsPage(page,config),preparation:new PreparationPage(page,config)};`;
async function code(body) { return cli('run-code', `async page => { ${pageCode}\n${construct}\n${body} }`); }

await cli('close').catch(() => {});
await cli('open', run.baseUrl + '/sign-in', '--config=' + join(output, 'cli.config.json'));
await code(`
  page.__demo={responses:[],pageErrors:[],checks:[],mobileStart:null};
  page.on('response',response=>{ const url=response.url(); if(url.startsWith(config.baseUrl+'/') && (['xhr','fetch'].includes(response.request().resourceType()) || response.status()>=400)) page.__demo.responses.push({method:response.request().method(),path:url.slice(config.baseUrl.length).split('?')[0],status:response.status()}); });
  page.on('pageerror',error=>page.__demo.pageErrors.push(error.message));
  const health=await page.request.get(config.baseUrl+'/health'); if(health.status()!==200) throw new Error('Live database health failed.');
  page.__demo.checks.push('The live HTTPS API and SQL database report Healthy.');
`);
let startedAt = Date.now();
if (!rehearsal) startedAt = await cli('run-code', 'async page => { await page.video().start({size:{width:1440,height:810}}); return Date.now(); }');
const timeline = [];
let deadline = startedAt;
let stopped = false;
try {
  for (const [index, scene] of scenes.entries()) {
    const start = Date.now();
    console.log(`${String(index + 1).padStart(2, '0')}/${scenes.length} ${scene.title}`);
    for (const step of scene.steps ?? [{code:scene.code}]) {
      if (step.command) await cli(...step.command);
      else await code(step.code);
    }
    const elapsed = Date.now() - start;
    timeline.push({ title: scene.title, caption: scene.caption, startSeconds: (start - startedAt) / 1000, actionSeconds: elapsed / 1000, allottedSeconds: scene.seconds });
    deadline += scene.seconds * 1000;
    if (!rehearsal) {
      if (Date.now() > deadline) throw new Error(`Scene ${scene.title} exceeded its recording time; review the rehearsal before recording again.`);
      await new Promise(resolve => setTimeout(resolve, Math.max(0, deadline - Date.now())));
    }
    await writeFile(join(output, rehearsal ? 'rehearsal-progress.json' : 'recording-progress.json'), JSON.stringify(timeline, null, 2));
  }
  const evidence = await cli('run-code', 'async page => ({responses:page.__demo.responses,pageErrors:page.__demo.pageErrors,checks:page.__demo.checks,mobileStart:page.__demo.mobileStart})');
  if (!rehearsal) {
    await cli('run-code', `async page => { await page.video().stop({path:${JSON.stringify(join(output, 'raw.webm'))}}); return 'Recording saved'; }`);
    stopped = true;
  }
  const unexpected = evidence.responses.filter(response => response.status >= 400 && !(
    (response.status === 401 && ['/authentication/session', '/authentication/sign-in'].includes(response.path)) ||
    (response.status === 409 && response.path === '/modules/4')
  ));
  const report = { baseUrl:run.baseUrl, database:run.database, recordedAt:new Date().toISOString(), rehearsal, startedAt, timeline, ...evidence, unexpectedResponses:unexpected };
  await writeFile(join(output, rehearsal ? 'rehearsal.json' : 'evidence.json'), JSON.stringify(report, null, 2));
  if (evidence.pageErrors.length || unexpected.length) throw new Error(`Unexpected browser errors: ${JSON.stringify({errors:evidence.pageErrors,responses:unexpected})}`);
  console.log(`PASS: ${scenes.length} live scenes; ${evidence.responses.length} real HTTP responses; no unexpected browser errors.`);
} finally {
  if (!rehearsal && !stopped) await cli('run-code', `async page => { await page.video().stop({path:${JSON.stringify(join(output, 'interrupted.webm'))}}); }`).catch(() => {});
}
