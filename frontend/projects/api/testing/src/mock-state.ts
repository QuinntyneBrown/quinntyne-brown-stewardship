export interface MockState {
  enrolled?: boolean;
  programmeFailure?: boolean;
  slotConflict?: boolean;
  signedIn: boolean;
  sessionFailure: boolean;
  throttled: boolean;
  signOutFailure: boolean;
  enrollmentExpired: boolean;
  enrollmentFailsOnce: boolean;
  enrollmentDelayMs: number;
}
