import { MockStateStore, StewardshipBridge } from "@qbs/api/testing";

// Only the acceptance composition reaches this file, so the bridge cannot leak into
// the production bundle. Playwright seeds state before bootstrap and steers the
// running application through `configure`.
export function installPlaywrightBridge(store: MockStateStore): void {
  const bridge: StewardshipBridge = (window.__stewardship ??= {});
  bridge.configure = (changes) => store.update(changes);
}
