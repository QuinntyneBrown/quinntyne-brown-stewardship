import { Component, input } from "@angular/core";
@Component({
  selector: "qbs-status-message",
  templateUrl: "./status-message.component.html",
  styleUrl: "./status-message.component.css",
})
export class StatusMessageComponent {
  readonly message = input.required<string>();
}
