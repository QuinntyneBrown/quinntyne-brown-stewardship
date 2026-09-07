import { Component, input } from "@angular/core";
import { PageIntroComponent } from "../page-intro/page-intro.component";
@Component({
  selector: "qbs-not-enrolled-notice",
  imports: [PageIntroComponent],
  templateUrl: "./not-enrolled-notice.component.html",
  styleUrl: "./not-enrolled-notice.component.css",
})
export class NotEnrolledNoticeComponent {
  readonly eyebrow = input("Enrollment");
  readonly heading = input("You are not yet enrolled in a cohort.");
  readonly intro = input("A cohort will be assigned to you before the programme begins, and you will be told when it starts.");
  readonly description = input.required<string>();
}
