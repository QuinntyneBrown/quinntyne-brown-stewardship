import { Component, computed, input, output } from "@angular/core";
// A stacked pair filling one target. Each half names its own direction and position; the half that cannot act says so and stays inert.
@Component({
  selector: "qbs-order-control",
  templateUrl: "./order-control.component.html",
  styleUrl: "./order-control.component.css",
})
export class OrderControlComponent {
  // A named item ("The bill arrives") or a generic noun ("practice step", "prompt").
  readonly label = input.required<string>();
  readonly generic = input(false);
  readonly position = input.required<number>();
  readonly total = input.required<number>();
  readonly moveUp = output<void>();
  readonly moveDown = output<void>();
  readonly first = computed(() => this.position() <= 1);
  readonly last = computed(() => this.position() >= this.total());
  readonly upName = computed(() => this.first()
    ? this.generic() ? `${this.capital()} ${this.position()} of ${this.total()} cannot move up` : `${this.label()} is first of ${this.total()} and cannot move up`
    : `Move ${this.label()} up from position ${this.position()} of ${this.total()}`);
  readonly downName = computed(() => this.last()
    ? this.generic() ? `${this.capital()} ${this.position()} of ${this.total()} cannot move down` : `${this.label()} is last of ${this.total()} and cannot move down`
    : `Move ${this.label()} down from position ${this.position()} of ${this.total()}`);
  private capital() { const label = this.label(); return label.charAt(0).toUpperCase() + label.slice(1); }
}
