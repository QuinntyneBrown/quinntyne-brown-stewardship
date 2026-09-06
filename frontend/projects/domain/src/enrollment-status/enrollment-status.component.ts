import {
  Component,
  inject,
  input,
  output,
  signal,
  OnInit,
} from "@angular/core";
import { COHORT_SERVICE, EnrollmentResult, ServiceError } from "@qbs/api";
import { NotEnrolledNoticeComponent } from "@qbs/components";
@Component({
  selector: "qbs-enrollment-status",
  imports: [NotEnrolledNoticeComponent],
  templateUrl: "./enrollment-status.component.html",
  styleUrl: "./enrollment-status.component.css",
})
export class EnrollmentStatusComponent implements OnInit {
  private readonly service = inject(COHORT_SERVICE);
  readonly description = input.required<string>();
  readonly expired = output<void>();
  readonly enrollment = signal<EnrollmentResult | null>(null);
  readonly status = signal<"loading" | "ready" | "error">("loading");
  ngOnInit() {
    void this.load();
  }
  async load() {
    this.status.set("loading");
    this.enrollment.set(null);
    try {
      this.enrollment.set(await this.service.getEnrollment());
      this.status.set("ready");
    } catch (error) {
      if (error instanceof ServiceError && error.status === 401)
        this.expired.emit();
      else this.status.set("error");
    }
  }
}
