import { Component, input } from "@angular/core";
@Component({
  selector: "qbs-not-enrolled-notice",
  templateUrl: "./not-enrolled-notice.component.html",
  styleUrl: "./not-enrolled-notice.component.css",
})
export class NotEnrolledNoticeComponent {
  readonly description = input.required<string>();
}
