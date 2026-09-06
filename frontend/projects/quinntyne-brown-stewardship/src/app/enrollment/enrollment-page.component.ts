import { Component, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { EnrollmentStatusComponent } from "@qbs/domain";
import { SIGN_IN_SERVICE } from "@qbs/api";
import {
  ErrorMessageComponent,
  NavigationLink,
  ProgrammeHeaderComponent,
  SkipLinkComponent,
} from "@qbs/components";
@Component({
  selector: "qbs-enrollment-page",
  imports: [
    EnrollmentStatusComponent,
    ErrorMessageComponent,
    ProgrammeHeaderComponent,
    SkipLinkComponent,
  ],
  templateUrl: "./enrollment-page.component.html",
  styleUrl: "./enrollment-page.component.css",
})
export class EnrollmentPageComponent {
  private readonly router = inject(Router);
  private readonly service = inject(SIGN_IN_SERVICE);
  readonly links: readonly NavigationLink[] = [
    { path: "/curriculum", label: "Curriculum" },
    { path: "/sessions", label: "Sessions" },
    { path: "/notes", label: "Notes" },
  ];
  readonly pending = signal(false);
  readonly error = signal("");
  description() {
    if (this.router.url.startsWith("/sessions"))
      return "Sessions are booked with the mentor your cohort assigns, so there is nothing to book yet.";
    if (this.router.url.startsWith("/notes"))
      return "Notes belong to your modules and sessions, so there are no notes to write yet.";
    return "Until then there is no curriculum to read.";
  }
  async signOut() {
    if (this.pending()) return;
    this.pending.set(true);
    this.error.set("");
    try {
      await this.service.signOut();
      await this.router.navigateByUrl("/sign-in", { replaceUrl: true });
    } catch {
      this.error.set("We could not sign you out. Please try again.");
    } finally {
      this.pending.set(false);
    }
  }
  async expired() {
    await this.router.navigate(["/sign-in"], {
      queryParams: { returnUrl: this.router.url },
      replaceUrl: true,
    });
  }
}
