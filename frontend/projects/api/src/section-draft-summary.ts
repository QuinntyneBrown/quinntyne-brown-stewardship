// A section row on the module screen carries its size and what depends on it, never its prose.
export interface SectionDraftSummary {
  id: string; ordinal: number; title: string; wordCount: number; completionCount: number; canRemove: boolean;
}
