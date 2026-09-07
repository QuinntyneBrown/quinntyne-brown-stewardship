import { Component, computed, ElementRef, input, model, viewChild } from "@angular/core";
// A long authored field. The count keeps counting past the maximum so the overage is visible before the boundary refuses it.
@Component({
  selector: "qbs-text-area-field",
  templateUrl: "./text-area-field.component.html",
  styleUrl: "./text-area-field.component.css",
})
export class TextAreaFieldComponent {
  readonly label = input.required<string>();
  readonly name = input.required<string>();
  readonly maxLength = input.required<number>();
  readonly tall = input(false);
  readonly error = input("");
  readonly value = model("");
  readonly errorId = computed(() => `${this.name()}-error`);
  readonly length = computed(() => [...this.value()].length);
  readonly over = computed(() => this.length() > this.maxLength());
  private readonly control = viewChild.required<ElementRef<HTMLTextAreaElement>>("control");
  focus() { this.control().nativeElement.focus(); }
}
