import { CohortFollowerResult } from "./cohort-follower-result";
// Whether a programme may be published, and what a publication would reach and move.
export interface ReadinessResult {
  canPublish: boolean; reason: string | null; emptyModuleTitles: string[]; unpublishedModuleCount: number; activeCohortCount: number; participantsMovedBack: number; movedBackToOrdinal: number | null; cohorts: CohortFollowerResult[];
}
