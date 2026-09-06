import { Injectable, signal } from "@angular/core";
import { MockState } from "./mock-state";
import "./stewardship-bridge";

const STORAGE_KEY = "stewardship.mock-state";

const ABSENT: MockState = {
  signedIn: false,
  sessionFailure: false,
  throttled: false,
  signOutFailure: false,
  enrollmentExpired: false,
  enrollmentFailsOnce: false,
  enrollmentDelayMs: 0,
};

// The mock services share one state store so a session established through the
// sign-in mock is visible to the cohort mock. Persistence keeps that state across a
// reload; the storage key is private to this class, so no test ever names it.
@Injectable({ providedIn: "root" })
export class MockStateStore {
  private readonly state = signal<MockState>({
    ...ABSENT,
    ...this.restore(),
    ...window.__stewardship?.seed,
  });
  readonly current = this.state.asReadonly();

  constructor() {
    this.persist();
  }

  update(changes: Partial<MockState>) {
    this.state.update((state) => ({ ...state, ...changes }));
    this.persist();
  }

  private restore(): Partial<MockState> {
    const stored = sessionStorage.getItem(STORAGE_KEY);
    return stored ? (JSON.parse(stored) as Partial<MockState>) : {};
  }

  private persist() {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(this.state()));
  }
}
