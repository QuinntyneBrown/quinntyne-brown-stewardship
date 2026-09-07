import { Component, computed, input } from "@angular/core";
import { RouterLink } from "@angular/router";
import { BreadcrumbItem } from "../breadcrumb-item";
// The authoring routes are flat, so every authoring screen renders the trail back to the programme index.
// Below the breakpoint the trail condenses to the single level above, which stays the way back.
@Component({
  selector: "qbs-breadcrumb",
  imports: [RouterLink],
  templateUrl: "./breadcrumb.component.html",
  styleUrl: "./breadcrumb.component.css",
})
export class BreadcrumbComponent {
  readonly trail = input.required<readonly BreadcrumbItem[]>();
  readonly parent = computed(() => { const trail = this.trail(); return trail.length > 1 ? trail[trail.length - 2] : null; });
}
