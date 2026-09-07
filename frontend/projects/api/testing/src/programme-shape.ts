// The only place the fabricated programme's size and the fabricated cohort's shape are stated.
// A test that needs another shape seeds one; nothing else in the mocks carries a literal count.
export interface ProgrammeShape {
  moduleCount: number;
  sectionCount: number;
  published: boolean;
  // Draft modules appended after the last publication, awaiting the next one.
  pendingModules?: number;
  durationWeeks: number;
  sessionCadenceWeeks: number;
}
export const DEFAULT_SHAPE: ProgrammeShape = { moduleCount: 12, sectionCount: 5, published: true, durationWeeks: 12, sessionCadenceWeeks: 2 };
