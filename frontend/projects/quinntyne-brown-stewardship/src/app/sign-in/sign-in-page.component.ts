import { Component, inject, signal } from "@angular/core";
import { Router, ActivatedRoute } from "@angular/router";
import { SIGN_IN_SERVICE } from "@qbs/api";
import { safeDestination } from "../safe-destination";
@Component({
  selector: "qbs-sign-in-page",
  templateUrl: "./sign-in-page.component.html",
  styleUrl: "./sign-in-page.component.css",
})
export class SignInPageComponent {
  private readonly service = inject(SIGN_IN_SERVICE);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  readonly email = signal("");
  readonly password = signal("");
  readonly emailError = signal("");
  readonly passwordError = signal("");
  readonly pending = signal(false);
  readonly message = signal(
    this.route.snapshot.queryParamMap.has("unavailable")
      ? "We could not check your session. Please try signing in again."
      : "",
  );
  async submit(
    event: Event,
    emailInput: HTMLInputElement,
    passwordInput: HTMLInputElement,
  ) {
    event.preventDefault();
    if (this.pending()) return;
    this.emailError.set(
      !this.email().trim()
        ? "Enter your email address."
        : !emailInput.validity.valid
          ? "Enter a valid email address."
          : "",
    );
    this.passwordError.set(!this.password() ? "Enter your password." : "");
    if (this.emailError() || this.passwordError()) {
      (this.emailError() ? emailInput : passwordInput).focus();
      return;
    }
    this.pending.set(true);
    this.message.set("");
    try {
      const result = await this.service.signIn(
        this.email().trim(),
        this.password(),
      );
      this.password.set("");
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
