import { Component, input } from "@angular/core";
@Component({
  selector: "qbs-skip-link",
  templateUrl: "./skip-link.component.html",
  styleUrl: "./skip-link.component.css",
})
export class SkipLinkComponent {
  readonly target = input.required<string>();
}
