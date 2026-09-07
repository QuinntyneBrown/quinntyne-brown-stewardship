export class CurriculumPage extends ShellPage {
  async faithtech() {
    await this.go("/curriculum");
    await this.page.getByRole("list", { name: "Module path" }).waitFor();
    const titles = [
      "Prepare to build together",
      "Discover through lament",
      "Discern the wisdom of God",
      "Develop with the Holy Spirit",
      "Demonstrate impact that lasts",
    ];
    for (const title of titles) await this.text(title);
    const response = await this.page.request.get(
      this.config.baseUrl + "/curriculum",
    );
    if (response.status() !== 200)
      throw new Error("DEMO-01: Curriculum API failed.");
    const data = await response.json();
    if (
      JSON.stringify(data.modules.map((m) => m.title)) !==
      JSON.stringify(titles)
    )
      throw new Error("DEMO-01: FaithTech module order differs.");
    await this.page
      .getByRole("list", { name: "Module path" })
      .scrollIntoViewIfNeeded();
    this.page.__demo.checks.push(
      "DEMO-01: The live curriculum contains Prepare followed by the four Ds in order.",
    );
  }
  async ready(completed = 0) {
    await this.text(`${completed} of ${this.config.moduleCount} complete`);
    if (
      (await this.page
        .getByRole("list", { name: "Module path" })
        .getByRole("listitem")
        .count()) !== this.config.moduleCount
    )
      throw new Error("The curriculum path is incomplete.");
  }
  async overview() {
    await this.go("/curriculum");
    await this.ready();
    await this.pause(1200);
    await this.go("/modules/4");
    await this.ready();
    await this.text("Complete the current module to unlock this module.");
  }
  async continue() {
    await this.page
      .getByRole("link", { name: "Continue module", exact: true })
      .click();
  }
  async nextSession() {
    await this.go("/curriculum");
    await this.ready(1);
    const booking = this.page.__demo.booking;
    await this.text(
      `${booking.durationMinutes} minutes with ${booking.mentorName} · ${booking.timeZone}`,
    );
    await this.page
      .getByRole("heading", {
        name: new Intl.DateTimeFormat("en-CA", {
          weekday: "short",
          month: "short",
          day: "numeric",
          hour: "numeric",
          minute: "2-digit",
          timeZoneName: "short",
          timeZone: booking.timeZone,
        }).format(new Date(booking.startsAt)),
      })
      .scrollIntoViewIfNeeded();
    await this.pause(800);
  }
  async completedProgramme() {
    await this.go("/curriculum");
    await this.ready(this.config.moduleCount);
    await this.text("✓ Curriculum complete");
    await this.text(
      "This cohort has ended. Your completed modules remain available.",
    );
    if (await this.page.getByRole("link", { name: "Continue module" }).count())
      throw new Error("A completed curriculum still has a current module.");
    await this.pause(1800);
  }
}
