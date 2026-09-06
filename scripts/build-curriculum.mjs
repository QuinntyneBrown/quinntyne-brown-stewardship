import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('../', import.meta.url));
const source = await readFile(`${root}/docs/curriculum/starter.md`, 'utf8');
const namespace = Buffer.from('a02af4acb1e744fe935069aac3179812', 'hex');
function id(name) {
  const bytes = createHash('sha1').update(namespace).update(name).digest().subarray(0, 16);
  bytes[6] = (bytes[6] & 15) | 80;
  bytes[8] = (bytes[8] & 63) | 128;
  const hex = bytes.toString('hex');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}
const modules = source.split(/^# /m).filter(Boolean).map(block => {
  const [intro, ...parts] = block.trim().split(/^## /m);
  const [heading, ...summary] = intro.trim().split('\n');
  const ordinal = Number(heading.slice(0, 2));
  const module = { id: id(`starter/module/${ordinal}`), ordinal, title: heading.slice(3).trim(), summary: summary.join('\n').trim(), effortEstimate: '', practiceSteps: [], sections: [], preparationPrompts: [] };
  for (const part of parts) {
    const [title, ...lines] = part.trim().split('\n');
    const body = lines.join('\n').trim();
    if (title.startsWith('Practice · ')) {
      module.effortEstimate = title.slice('Practice · '.length);
      module.practiceSteps = body.split('\n').filter(line => /^\d+\. /.test(line)).map(line => line.replace(/^\d+\. /, ''));
    } else if (title === 'Prepare') {
      module.preparationPrompts = body.split('\n').filter(line => line.startsWith('- ')).map((line, i) => ({ id: id(`starter/module/${ordinal}/prompt/${i + 1}`), ordinal: i + 1, text: line.slice(2) }));
    } else {
      const position = module.sections.length + 1;
      module.sections.push({ id: id(`starter/module/${ordinal}/section/${position}`), ordinal: position, title, reading: body });
    }
  }
  return module;
});
const destination = `${root}/backend/src/QuinntyneBrownStewardship.Cli/Content`;
await mkdir(destination, { recursive: true });
await writeFile(`${destination}/starter-curriculum.json`, JSON.stringify({ key: 'starter', modules }, null, 2) + '\n');
console.log(`Built ${modules.length} modules from the curriculum manuscript.`);
