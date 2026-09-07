import { Component, input, model, output, viewChild } from "@angular/core";
import { TextFieldComponent } from "../text-field/text-field.component";
// Takes a key and a title, emits them once both are present, and reports the field message the API returns.
@Component({
  selector: "qbs-programme-form",
  imports: [TextFieldComponent],
  templateUrl: "./programme-form.component.html",
  styleUrl: "./programme-form.component.css",
})
export class ProgrammeFormComponent {
  readonly keyMax = input.required<number>();
  readonly titleMax = input.required<number>();
  readonly pending = input(false);
  readonly key = model("");
  readonly title = model("");
  readonly keyError = model("");
  readonly titleError = model("");
  readonly submitted = output<{ key: string; title: string }>();
  private readonly keyField = viewChild.required("keyField", { read: TextFieldComponent });
  private readonly titleField = viewChild.required("titleField", { read: TextFieldComponent });
  submit(event: Event) {
    event.preventDefault();
    if (this.pending()) return;
    this.keyError.set(this.key().trim() ? "" : "A key is required.");
    this.titleError.set(this.title().trim() ? "" : "A title is required.");
    if (this.keyError() || this.titleError()) { this.focus(this.keyError() ? "key" : "title"); return; }
    this.submitted.emit({ key: this.key().trim(), title: this.title().trim() });
  }
  // Focus moves to the field the message names, so the refusal is where the correction is made.
  focus(field: "key" | "title") { (field === "key" ? this.keyField() : this.titleField()).focus(); }
}
