import { CanDeactivateFn } from '@angular/router';
// An authoring screen decides for itself whether it may be left; the answer may wait on a dialog.
export const leaveGuard: CanDeactivateFn<{ canLeave(): boolean | Promise<boolean> }> = component => component.canLeave();
