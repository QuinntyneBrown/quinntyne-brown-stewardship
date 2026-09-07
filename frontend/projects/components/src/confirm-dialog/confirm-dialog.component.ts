import { Component, ElementRef, input, output, viewChild } from "@angular/core";
// A native dialog: focus enters it, stays inside it while it is open, and returns to the opener when it closes.
// Keeping is the emphasised, focused choice; the consequence is stated before it can be confirmed.
@Component({
  selector: "qbs-confirm-dialog",
  templateUrl: "./confirm-dialog.component.html",
  styleUrl: "./confirm-dialog.component.css",
})
export class ConfirmDialogComponent {
  readonly heading = input.required<string>();
  readonly consequence = input.required<string>();
  readonly confirmLabel = input.required<string>();
  readonly keepLabel = input.required<string>();
  readonly decided = output<boolean>();
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>("dialog");
  private resolve: ((confirmed: boolean) => void) | null = null;
  open(): Promise<boolean> {
    this.dialog().nativeElement.showModal();
    return new Promise((resolve) => (this.resolve = resolve));
  }
  decide(confirmed: boolean) {
    this.dialog().nativeElement.close();
    this.resolve?.(confirmed); this.resolve = null;
    this.decided.emit(confirmed);
  }
  // Escape closes a native dialog; that is a decision to keep.
  cancelled(event: Event) { event.preventDefault(); this.decide(false); }
}
