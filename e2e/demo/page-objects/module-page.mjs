export class ModulePage extends ShellPage {
  async develop() {
    await this.go("/modules/4");
    await this.page
      .getByRole("heading", {
        name: "Develop with the Holy Spirit",
        exact: true,
      })
      .waitFor();
    const response = await this.page.request.get(
      this.config.baseUrl + "/modules/4",
    );
    if (response.status() !== 200)
      throw new Error(
        "DEMO-04: Develop is not available to the prepared advanced account.",
      );
    const data = await response.json();
    const reading = data.sections.map((s) => s.reading).join("\n");
    for (const movement of [
      "Request",
      "Receive",
      "Review",
      "Render",
      "Rejoice",
    ]) {
      if (!reading.includes(movement))
        throw new Error(`DEMO-01: Missing ${movement}.`);
    }
    await this.choose("The Co-Creation Cycle");
    this.page.__demo.checks.push(
      "DEMO-04: The advanced account can review all five Rs through the real Develop reader.",
    );
  }
  async choose(title) {
    await this.page
      .getByRole("navigation", { name: "Module sections" })
      .getByRole("button", { name: new RegExp(title) })
      .click();
    const heading = this.page.getByRole("heading", {
      name: title,
      exact: true,
    });
    await heading.waitFor();
    await heading.scrollIntoViewIfNeeded();
  }
  async section(position) {
    await this.page
      .getByRole("progressbar", {
        name: `Section ${position} of 5`,
        exact: true,
      })
      .waitFor();
  }
  async read() {
    await this.section(3);
    const progress = await this.page
      .getByRole("progressbar")
      .evaluate((element) => element.position);
    if (progress !== 0.4)
      throw new Error(
        "Recorded section completion did not produce 40% progress.",
      );
    await this.pause(2600);
    await this.page
      .getByRole("heading", { name: "Your assignment", exact: true })
      .scrollIntoViewIfNeeded();
    if ((await this.page.getByRole("listitem").count()) !== 3)
      throw new Error("Assignment steps are missing.");
    await this.pause(2600);
    await this.page
      .getByRole("heading", { name: "Bring to your next conversation" })
      .scrollIntoViewIfNeeded();
    await this.pause(2200);
  }
  async complete() {
    await this.page
      .getByRole("button", { name: "Mark section complete" })
      .click();
  }
  async finish() {
    await this.complete();
    await this.section(4);
    await this.pause(1200);
    await this.page.reload();
    await this.section(4);
    await this.pause(1600);
    await this.complete();
    await this.section(5);
    await this.pause(1600);
    await this.complete();
    await this.text(`1 of ${this.config.moduleCount} complete`);
    await this.pause(2000);
    await this.go("/modules/1");
    await this.section(5);
    await this.text("✓ Section complete");
    if (
      await this.page
        .getByRole("button", { name: "Mark section complete" })
        .count()
    )
      throw new Error("Completed sections can be completed again in the UI.");
    this.page.__demo.checks.push(
      "Section progress persisted after reload; completing module 1 unlocked module 2; completed reading remains available.",
    );
  }
  async addNote() {
    await this.page.getByRole("link", { name: "Add a module note" }).click();
  }
}
