import { MockState } from "./mock-state";

// The seam Playwright drives the mocked services through. `seed` is written before
// the application bootstraps; `configure` is installed by the acceptance host once
// the mock state store exists, so a running application can be steered mid-test.
export interface StewardshipBridge {
  seed?: Partial<MockState>;
  configure?(changes: Partial<MockState>): void;
}

declare global {
  interface Window {
    __stewardship?: StewardshipBridge;
  }
}
