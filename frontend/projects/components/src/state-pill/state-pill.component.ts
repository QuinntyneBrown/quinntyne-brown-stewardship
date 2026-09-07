import { Component, computed, input } from "@angular/core";
// The state is a word first and a mark second, so it survives greyscale and reads aloud as text.
@Component({
  selector: "qbs-state-pill",
  templateUrl: "./state-pill.component.html",
  styleUrl: "./state-pill.component.css",
})
export class StatePillComponent {
  readonly state = input.required<"Draft" | "Published" | "Blocked">();
  readonly meaning = input<"publication" | "removal">("publication");
  readonly label = computed(() => (this.state() === "Blocked" ? "Removal blocked" : this.state()));
  readonly prefix = computed(() => (this.meaning() === "publication" ? "Publication state:" : "Removal:"));
}
