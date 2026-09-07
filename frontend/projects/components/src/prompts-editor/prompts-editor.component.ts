import { Component, input, model, output, viewChildren } from "@angular/core";
import { OrderControlComponent } from "../order-control/order-control.component";
import { PromptDraft } from "../prompt-draft";
import { StatePillComponent } from "../state-pill/state-pill.component";
import { TextAreaFieldComponent } from "../text-area-field/text-area-field.component";
// Prompts are saved one by one; an answered prompt keeps its wording revisable and its identity fixed, so it cannot be removed.
@Component({
  selector: "qbs-prompts-editor",
  imports: [OrderControlComponent, StatePillComponent, TextAreaFieldComponent],
  templateUrl: "./prompts-editor.component.html",
  styleUrl: "./prompts-editor.component.css",
})
export class PromptsEditorComponent {
  readonly prompts = model.required<PromptDraft[]>();
  readonly limit = input.required<number>();
  readonly errors = input<Record<string, string>>({});
  // A stored prompt is removed by the server; the page asks first. An unsaved one is dropped here.
  readonly removeRequested = output<PromptDraft>();
  readonly moved = output<string>();
  private readonly fields = viewChildren(TextAreaFieldComponent);
  error(index: number) { return this.errors()[`prompts[${index}]`] ?? ""; }
  set(index: number, text: string) { this.prompts.update(prompts => prompts.map((p, i) => (i === index ? { ...p, text } : p))); }
  add() { this.prompts.update(prompts => [...prompts, { id: null, text: "", answerCount: 0, canRemove: true }]); setTimeout(() => this.fields().at(-1)?.focus()); }
  remove(index: number) {
    const prompt = this.prompts()[index];
    if (prompt.id) this.removeRequested.emit(prompt); else this.prompts.update(prompts => prompts.filter((_, i) => i !== index));
  }
  move(index: number, delta: number) {
    this.prompts.update(prompts => { const next = [...prompts]; const [prompt] = next.splice(index, 1); next.splice(index + delta, 0, prompt); return next; });
    this.moved.emit(`Prompt moved to position ${index + delta + 1} of ${this.prompts().length}.`);
  }
  answers(prompt: PromptDraft) {
    return prompt.answerCount === 0 ? "No answers recorded yet." : `${prompt.answerCount} participant${prompt.answerCount === 1 ? " has" : "s have"} answered this prompt. Revising the wording keeps ${prompt.answerCount === 1 ? "that answer" : "their answers"} attached.`;
  }
  focus(index: number) { this.fields()[index]?.focus(); }
}
