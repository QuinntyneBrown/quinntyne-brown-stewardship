import { Component, input } from "@angular/core";
@Component({
  selector: "qbs-error-message",
  templateUrl: "./error-message.component.html",
  styleUrl: "./error-message.component.css",
})
export class ErrorMessageComponent {
  readonly message = input.required<string>();
}
