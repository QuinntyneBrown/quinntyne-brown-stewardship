import { Component, input } from "@angular/core";
import { PageIntroComponent } from "../page-intro/page-intro.component";
@Component({
  selector: "qbs-cohort-summary",
  imports: [PageIntroComponent],
  templateUrl: "./cohort-summary.component.html",
  styleUrl: "./cohort-summary.component.css",
})
export class CohortSummaryComponent {
  readonly mentorName = input.required<string | null>();
  readonly startDate = input.required<string | null>();
}
