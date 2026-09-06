export interface MockState {
  signedIn: boolean;
  sessionFailure: boolean;
  throttled: boolean;
  signOutFailure: boolean;
  enrollmentExpired: boolean;
  enrollmentFailsOnce: boolean;
  enrollmentDelayMs: number;
}
