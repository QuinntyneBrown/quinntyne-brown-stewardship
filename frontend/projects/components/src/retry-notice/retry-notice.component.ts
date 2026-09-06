import { Component, input, output } from "@angular/core";
@Component({
  selector: "qbs-retry-notice",
  templateUrl: "./retry-notice.component.html",
  styleUrl: "./retry-notice.component.css",
})
export class RetryNoticeComponent {
  readonly message = input.required<string>();
  readonly retry = output<void>();
}
