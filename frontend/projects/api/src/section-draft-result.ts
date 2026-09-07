import { AuthoringLimits } from "./authoring-limits";
import { SectionSibling } from "./section-sibling";
export interface SectionDraftResult {
  id: string; moduleId: string; moduleOrdinal: number; moduleTitle: string; curriculumId: string; curriculumTitle: string; ordinal: number; sectionCount: number;
  title: string; reading: string; revision: string; completionCount: number; canRemove: boolean; createdAt: string; siblings: SectionSibling[]; limits: AuthoringLimits;
}
