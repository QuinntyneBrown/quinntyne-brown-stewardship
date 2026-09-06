import { Component, input } from "@angular/core";
@Component({
  selector: "qbs-page-intro",
  templateUrl: "./page-intro.component.html",
  styleUrl: "./page-intro.component.css",
})
export class PageIntroComponent {
  readonly eyebrow = input.required<string>();
  readonly heading = input.required<string>();
  readonly headingId = input<string | null>(null);
}
