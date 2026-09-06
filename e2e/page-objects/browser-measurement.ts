import { expect, Page } from "@playwright/test";

// A measurement helper, independent of screen selectors. Screen page objects
// supply navigation and the actual usable control that ends the measurement.
export class BrowserMeasurement {
  static readonly profile = {
    cpuSlowdown: 4,
    latencyMs: 150,
    downloadMbps: 4,
    uploadMbps: 1,
    serviceDelayMs: 150,
  };
  constructor(private readonly page: Page) {}

  async configure() {
    const cdp = await this.page.context().newCDPSession(this.page);
    await cdp.send("Network.enable");
    await cdp.send("Network.setCacheDisabled", { cacheDisabled: true });
    await cdp.send("Emulation.setCPUThrottlingRate", {
      rate: BrowserMeasurement.profile.cpuSlowdown,
    });
    await cdp.send("Network.emulateNetworkConditions", {
      offline: false,
      latency: BrowserMeasurement.profile.latencyMs,
      downloadThroughput:
        (BrowserMeasurement.profile.downloadMbps * 1_000_000) / 8,
      uploadThroughput: (BrowserMeasurement.profile.uploadMbps * 1_000_000) / 8,
      connectionType: "cellular4g",
    });
    // CDP observes without routing: Playwright routing would change the network
    // measurement. Mock composition must make no API requests at all.
    this.page.on("request", (request) => {
      expect(["fetch", "xhr"]).not.toContain(request.resourceType());
    });
  }

  async load(open: () => Promise<unknown>, ready: () => Promise<unknown>) {
    await open();
    await ready();
    const interactiveMs = await this.page.evaluate(() => performance.now());
    await this.page.evaluate(() => document.fonts.ready);
    await this.page.waitForLoadState("networkidle");
    const resources = await this.page.evaluate(() =>
      [
        ...performance.getEntriesByType("navigation"),
        ...performance.getEntriesByType("resource"),
      ].map((entry) => {
        const resource = entry as PerformanceResourceTiming;
        return {
          url: resource.name,
          bytes: resource.transferSize,
          startMs: resource.startTime,
          endMs: resource.responseEnd,
        };
      }),
    );
    return {
      interactiveMs,
      transferredBytes: resources.reduce((sum, item) => sum + item.bytes, 0),
      resources,
    };
  }
}
