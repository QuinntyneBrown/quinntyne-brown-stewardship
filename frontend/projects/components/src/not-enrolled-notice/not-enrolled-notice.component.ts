import { Component, input } from "@angular/core";
import { PageIntroComponent } from "../page-intro/page-intro.component";
@Component({
  selector: "qbs-not-enrolled-notice",
  imports: [PageIntroComponent],
  templateUrl: "./not-enrolled-notice.component.html",
  styleUrl: "./not-enrolled-notice.component.css",
})
export class NotEnrolledNoticeComponent {
  readonly description = input.required<string>();
}
