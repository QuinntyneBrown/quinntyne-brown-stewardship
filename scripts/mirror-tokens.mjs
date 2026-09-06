import { mkdir, copyFile, cp } from "node:fs/promises";
import { fileURLToPath } from "node:url";
const root = fileURLToPath(new URL("../", import.meta.url));
const target = `${root}/frontend/projects/quinntyne-brown-stewardship/src`;
await mkdir(target, { recursive: true });
for (const name of ["tokens.css", "base.css", "fonts.css"])
  await copyFile(`${root}/design-system/${name}`, `${target}/${name}`);
await cp(
  `${root}/design-system/fonts`,
  `${root}/frontend/projects/quinntyne-brown-stewardship/public/fonts`,
  { recursive: true },
);
await copyFile(`${root}/design-system/favicon.svg`, `${root}/frontend/projects/quinntyne-brown-stewardship/public/favicon.svg`);
