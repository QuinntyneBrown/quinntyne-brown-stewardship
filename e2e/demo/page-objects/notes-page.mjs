export class NotesPage extends ShellPage {
  async visibleNote(body) {
    await this.page.getByText(body, { exact: true }).waitFor();
  }
  async edit(body) {
    await this.page
      .getByRole("article")
      .filter({ hasText: body })
      .getByRole("link", { name: "Edit note", exact: true })
      .click();
  }
  async more() {
    await this.go("/notes");
    await this.page
      .getByRole("link", { name: "Edit note", exact: true })
      .first()
      .waitFor();
    const before = await this.page.getByRole("article").count();
    const more = this.page.getByRole("button", {
      name: "More notes",
      exact: true,
    });
    await more.scrollIntoViewIfNeeded();
    await this.pause(800);
    await more.click();
    await this.page.waitForFunction(
      (count) =>
        document.querySelectorAll("qbs-note-collection article").length > count,
      before,
    );
    if (
      !(await this.page
        .getByRole("article")
        .nth(before)
        .evaluate((element) => element === document.activeElement))
    )
      throw new Error("Additional notes did not receive keyboard focus.");
    await this.pause(700);
    await this.fits();
    this.page.__demo.checks.push(
      "The real notes endpoint returned a continuation page; additional notes remain reachable.",
    );
  }
}
