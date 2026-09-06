import { Component, input, output, signal, viewChild } from "@angular/core";
import { Credentials } from "../credentials";
import { ErrorMessageComponent } from "../error-message/error-message.component";
import { TextFieldComponent } from "../text-field/text-field.component";
@Component({
  selector: "qbs-sign-in-form",
  imports: [ErrorMessageComponent, TextFieldComponent],
  templateUrl: "./sign-in-form.component.html",
  styleUrl: "./sign-in-form.component.css",
})
export class SignInFormComponent {
  readonly pending = input(false);
  readonly message = input("");
  readonly submitted = output<Credentials>();
  readonly emailAddress = signal("");
  readonly password = signal("");
  readonly emailError = signal("");
  readonly passwordError = signal("");
  private readonly emailField = viewChild.required("emailField", {
    read: TextFieldComponent,
  });
  private readonly passwordField = viewChild.required("passwordField", {
    read: TextFieldComponent,
  });
  submit(event: Event) {
    event.preventDefault();
    if (this.pending()) return;
    this.emailError.set(
      !this.emailAddress().trim()
        ? "Enter your email address."
        : !this.emailField().valid
          ? "Enter a valid email address."
          : "",
    );
    this.passwordError.set(!this.password() ? "Enter your password." : "");
    if (this.emailError() || this.passwordError()) {
      (this.emailError() ? this.emailField() : this.passwordField()).focus();
      return;
    }
    const credentials = {
      emailAddress: this.emailAddress().trim(),
      password: this.password(),
    };
    this.password.set("");
    this.submitted.emit(credentials);
  }
}
