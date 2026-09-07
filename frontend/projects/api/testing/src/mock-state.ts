export interface MockState {
  enrolled?: boolean;
  programmeFailure?: boolean;
  slotConflict?: boolean;
  administrator?: boolean;
  authoringFailure?: boolean;
  // The next module save finds the module revised elsewhere and is refused once.
  staleRevision?: boolean;
  // The next reorder is refused once, as if the list had changed elsewhere.
  reorderRefused?: boolean;
  signedIn: boolean;
  sessionFailure: boolean;
  throttled: boolean;
  signOutFailure: boolean;
  enrollmentExpired: boolean;
  enrollmentFailsOnce: boolean;
  enrollmentDelayMs: number;
  responseDelayMs?: number;
}
