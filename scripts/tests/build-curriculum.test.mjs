import test from "node:test";
import assert from "node:assert/strict";
import { mkdtemp, mkdir, readFile, writeFile, cp, rm } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join, resolve, basename } from "node:path";
import { execFileSync } from "node:child_process";

test("DEMO-01: a Windows manuscript preserves reading sections, practice, and preparation prompts on import", async () => {
  const root = await mkdtemp(join(tmpdir(), "stewardship-curriculum-"));
  try {
    await mkdir(join(root, "scripts"), { recursive: true });
    await mkdir(join(root, "docs/curriculum"), { recursive: true });
    await cp(
      new URL("../build-curriculum.mjs", import.meta.url),
      join(root, "scripts/build-curriculum.mjs"),
    );
    const lines = [
      "# 01 Develop",
      "",
      "A practice of co-creation.",
      "",
      "## Read together",
      "",
      "Request, Receive, Review, Render, Rejoice.",
      "",
      "## Practice · 15 minutes",
      "",
      "1. Name the next useful step.",
      "",
      "## Prepare",
      "",
      "- What did the team receive?",
      "",
    ];
    const outputs = [];
    for (const newline of ["\n", "\r\n"]) {
      await writeFile(
        join(root, "docs/curriculum/starter.md"),
        lines.join(newline),
      );
      execFileSync(
        process.execPath,
        [join(root, "scripts/build-curriculum.mjs")],
        { timeout: 10000 },
      );
      outputs.push(
        JSON.parse(
          await readFile(
            join(
              root,
              "backend/src/QuinntyneBrownStewardship.Cli/Content/starter-curriculum.json",
            ),
            "utf8",
          ),
        ),
      );
    }
    assert.deepEqual(outputs[1], outputs[0]);
    const module = outputs[1].modules[0];
    assert.equal(module.sections.length, 1);
    assert.equal(
      module.sections[0].reading,
      "Request, Receive, Review, Render, Rejoice.",
    );
    assert.deepEqual(module.practiceSteps, ["Name the next useful step."]);
    assert.equal(
      module.preparationPrompts[0].text,
      "What did the team receive?",
    );
  } finally {
    assert.equal(resolve(root, ".."), resolve(tmpdir()));
    assert.ok(basename(root).startsWith("stewardship-curriculum-"));
    await rm(root, { recursive: true, force: true });
  }
});
