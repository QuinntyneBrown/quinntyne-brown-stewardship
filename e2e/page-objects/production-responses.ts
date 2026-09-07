import { readFile } from 'node:fs/promises';
import { Page } from '@playwright/test';
import type { PerformanceState } from '../../frontend/projects/quinntyne-brown-stewardship/src/app/testing/performance-state';
import { BrowserMeasurement } from './browser-measurement';

export class ProductionResponses {
  constructor(private readonly page: Page) {}

  async install(screen: string) {
    const fixture: { measuredAt: string; noteId: string; sessionId: string; programmeId: string; moduleId: string; sectionId: string; responses: PerformanceState['responses'] } = JSON.parse(await readFile('../.local/production-responses.json', 'utf8'));
    if (screen === 'sign-in') fixture.responses['/authentication/session'].body = null;
    if (screen === 'not-enrolled') fixture.responses['/enrollment'].body = { isEnrolled: false };
    // The authoring screens are read by the administrator the capture signed in separately.
    if (screen.startsWith('admin-')) fixture.responses['/authentication/session'] = fixture.responses['/authentication/session (administrator)'];
    await this.page.addInitScript(({ responses, profile }) => {
      window.__stewardshipPerformance = { responses, calls: [], latencyMs: profile.latencyMs, downloadBytesPerMs: profile.downloadMbps * 1000000 / 8 / 1000 };
    }, { responses: fixture.responses, profile: BrowserMeasurement.profile });
    return { notePath: `/notes/${fixture.noteId}`, noteBody: (fixture.responses[`/notes/${fixture.noteId}`].body as { body: string }).body, sessionPath: `/sessions/${fixture.sessionId}`, programmeId: fixture.programmeId, moduleId: fixture.moduleId, sectionId: fixture.sectionId, measuredAt: fixture.measuredAt };
  }

  async transfer() {
    return this.page.evaluate(() => {
      const state = window.__stewardshipPerformance;
      const requests = state.calls.map(path => ({ path, bytes: state.responses[path].compressedBytes + state.responses[path].headerBytes }));
      return { requests, bytes: requests.reduce((sum, request) => sum + request.bytes, 0) };
    });
  }
}
