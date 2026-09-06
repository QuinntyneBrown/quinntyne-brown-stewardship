import { execFile } from "node:child_process";
import { promisify } from "node:util";
import { readFile, writeFile } from "node:fs/promises";
import { join } from "node:path";
import { fileURLToPath } from "node:url";

process.chdir(fileURLToPath(new URL("../../", import.meta.url)));
const run = JSON.parse(await readFile(".local/live-demo/run.json", "utf8"));
const cliPath =
  process.env.PLAYWRIGHT_CLI_PATH ??
  join(
    process.env.APPDATA,
    "npm/node_modules/@playwright/cli/playwright-cli.js",
  );
const code = `async page => {
  const config=${JSON.stringify(run)};
  const note=page.__demo.note;
  if(!note?.id) throw new Error('The completed recording must remain open.');
  const checks=[];
  for(const [email,expected] of [['participant@demo.invalid',200],['rehearsal@demo.invalid',404],['mentor@demo.invalid',200]]) {
    const context=await page.context().browser().newContext({ignoreHTTPSErrors:true});
    try {
      const request=context.request;
      const csrf=await(await request.get(config.baseUrl+'/authentication/csrf')).json();
      const signedIn=await request.post(config.baseUrl+'/authentication/sign-in',{headers:{'X-CSRF-TOKEN':csrf.token},data:{emailAddress:email,password:config.password}});
      if(signedIn.status()!==200) throw new Error('Could not authenticate the access-check account.');
      const response=await request.get(config.baseUrl+'/notes/'+note.id);
      if(response.status()!==expected) throw new Error('Unexpected note authorization status for '+email+': '+response.status());
      if(expected===200 && (await response.json()).body!==note.body) throw new Error('Authorized note contents differ from the saved revision.');
      checks.push({account:email,operation:'Read the recorded participant note',expectedStatus:expected,actualStatus:response.status()});
      const refused=await request.post(config.baseUrl+'/authentication/sign-out');
      if(refused.status()!==400) throw new Error('A request without a CSRF token was accepted.');
      checks.push({account:email,operation:'Mutating request without a CSRF token',expectedStatus:400,actualStatus:refused.status()});
      const fresh=await(await request.get(config.baseUrl+'/authentication/csrf')).json();
      const signOut=await request.post(config.baseUrl+'/authentication/sign-out',{headers:{'X-CSRF-TOKEN':fresh.token}});
      if(signOut.status()!==204) throw new Error('The verification session could not be revoked.');
    } finally { await context.close(); }
  }
  return {verifiedAt:new Date().toISOString(),baseUrl:config.baseUrl,checks};
}`;
try {
  const { stdout } = await promisify(execFile)(
    process.execPath,
    [cliPath, "-s=stewardship-demo", "run-code", code],
    { timeout: 60000, maxBuffer: 1_000_000 },
  );
  if (stdout.includes("### Error")) throw new Error(stdout);
  const json = stdout.match(/### Result\r?\n([\s\S]*?)(?=\r?\n### |$)/)?.[1];
  const evidence = JSON.parse(json);
  await writeFile(
    ".local/live-demo/access-verification.json",
    JSON.stringify(evidence, null, 2),
  );
  console.log(
    `PASS: ${evidence.checks.length} live note-privacy and CSRF checks.`,
  );
} catch (error) {
  console.error(String(error.message).replaceAll(run.password, "[redacted]"));
  process.exitCode = 1;
}
