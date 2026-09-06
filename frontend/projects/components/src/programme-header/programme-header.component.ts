import { Component, input, output } from "@angular/core";
import { RouterLink, RouterLinkActive } from "@angular/router";
import { NavigationLink } from "../navigation-link";
@Component({
  selector: "qbs-programme-header",
  imports: [RouterLink, RouterLinkActive],
  templateUrl: "./programme-header.component.html",
  styleUrl: "./programme-header.component.css",
})
export class ProgrammeHeaderComponent {
  readonly links = input.required<readonly NavigationLink[]>();
  readonly pending = input(false);
  readonly signOut = output<void>();
}
