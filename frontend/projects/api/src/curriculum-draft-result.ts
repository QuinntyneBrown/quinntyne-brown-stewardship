import { AuthoringLimits } from "./authoring-limits";
import { ModuleDraftSummary } from "./module-draft-summary";
import { PublicationState } from "./publication-state";
import { ReadinessResult } from "./readiness-result";
export interface CurriculumDraftResult {
  id: string; key: string; title: string; state: PublicationState; createdAt: string; publishedAt: string | null; cohortCount: number; canChangeKey: boolean; canRemove: boolean; modules: ModuleDraftSummary[]; readiness: ReadinessResult; limits: AuthoringLimits;
}
