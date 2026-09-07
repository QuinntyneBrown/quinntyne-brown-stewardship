import { Component, input, output } from "@angular/core";
// States an absence rather than rendering an empty list, and names the action that ends it.
@Component({
  selector: "qbs-empty-state",
  templateUrl: "./empty-state.component.html",
  styleUrl: "./empty-state.component.css",
})
export class EmptyStateComponent {
  readonly heading = input.required<string>();
  readonly description = input.required<string>();
  readonly action = input<string | null>(null);
  readonly act = output<void>();
}
