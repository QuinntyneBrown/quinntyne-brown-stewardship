import { Component, inject, signal } from "@angular/core";
import { Router, ActivatedRoute } from "@angular/router";
import { SIGN_IN_SERVICE } from "@qbs/api";
import {
  Credentials,
  PageIntroComponent,
  SignatureHeaderComponent,
  SignInFormComponent,
} from "@qbs/components";
import { safeDestination } from "../safe-destination";
@Component({
  selector: "qbs-sign-in-page",
  imports: [PageIntroComponent, SignatureHeaderComponent, SignInFormComponent],
  templateUrl: "./sign-in-page.component.html",
  styleUrl: "./sign-in-page.component.css",
})
export class SignInPageComponent {
  private readonly service = inject(SIGN_IN_SERVICE);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  readonly pending = signal(false);
  readonly message = signal(
    this.route.snapshot.queryParamMap.has("unavailable")
      ? "We could not check your session. Please try signing in again."
      : "",
  );
  async signIn(credentials: Credentials) {
    this.pending.set(true);
    this.message.set("");
    try {
      const result = await this.service.signIn(
        credentials.emailAddress,
        credentials.password,
      );
      if (result.statusCode === 200)
        await this.router.navigateByUrl(
          safeDestination(this.route.snapshot.queryParamMap.get("returnUrl")),
          { replaceUrl: true },
        );
      else
        this.message.set(
          result.retryAt
            ? `${result.message} Try again at ${new Date(result.retryAt).toLocaleTimeString([], { hour: "numeric", minute: "2-digit" })}.`
            : result.message || "Unable to sign in. Please try again.",
        );
    } catch {
      this.message.set("We could not sign you in. Please try again.");
    } finally {
      this.pending.set(false);
    }
  }
}
