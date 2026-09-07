import { Component, input, model, output, viewChildren } from "@angular/core";
import { OrderControlComponent } from "../order-control/order-control.component";
import { TextAreaFieldComponent } from "../text-area-field/text-area-field.component";
// The ordered steps of a practice assignment. Order and number are the list itself; the module saves them whole.
@Component({
  selector: "qbs-practice-steps-editor",
  imports: [OrderControlComponent, TextAreaFieldComponent],
  templateUrl: "./practice-steps-editor.component.html",
  styleUrl: "./practice-steps-editor.component.css",
})
export class PracticeStepsEditorComponent {
  readonly steps = model.required<string[]>();
  readonly limit = input.required<number>();
  // Refusals keyed as the API keys them: practiceSteps[2].
  readonly errors = input<Record<string, string>>({});
  // The new arrangement, in words, for the screen to announce.
  readonly moved = output<string>();
  private readonly fields = viewChildren(TextAreaFieldComponent);
  error(index: number) { return this.errors()[`practiceSteps[${index}]`] ?? ""; }
  set(index: number, value: string) { this.steps.update(steps => steps.map((s, i) => (i === index ? value : s))); }
  add() { this.steps.update(steps => [...steps, ""]); setTimeout(() => this.fields().at(-1)?.focus()); }
  remove(index: number) { this.steps.update(steps => steps.filter((_, i) => i !== index)); }
  move(index: number, delta: number) {
    this.steps.update(steps => { const next = [...steps]; const [step] = next.splice(index, 1); next.splice(index + delta, 0, step); return next; });
    this.moved.emit(`Practice step moved to position ${index + delta + 1} of ${this.steps().length}.`);
  }
  focus(index: number) { this.fields()[index]?.focus(); }
}
