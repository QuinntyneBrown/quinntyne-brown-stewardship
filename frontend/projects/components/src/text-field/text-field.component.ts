import {
  Component,
  computed,
  ElementRef,
  input,
  model,
  viewChild,
} from "@angular/core";
@Component({
  selector: "qbs-text-field",
  templateUrl: "./text-field.component.html",
  styleUrl: "./text-field.component.css",
})
export class TextFieldComponent {
  readonly label = input.required<string>();
  readonly name = input.required<string>();
  readonly type = input.required<string>();
  readonly autocomplete = input.required<string>();
  readonly maxLength = input.required<number>();
  readonly error = input("");
  readonly value = model("");
  readonly errorId = computed(() => `${this.name()}-error`);
  private readonly control =
    viewChild.required<ElementRef<HTMLInputElement>>("control");
  get valid() {
    return this.control().nativeElement.validity.valid;
  }
  focus() {
    this.control().nativeElement.focus();
  }
}
