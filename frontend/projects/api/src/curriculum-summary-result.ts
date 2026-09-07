import { PublicationState } from "./publication-state";
export interface CurriculumSummaryResult {
  id: string; key: string; title: string; state: PublicationState; moduleCount: number; publishedModuleCount: number; cohortCount: number;
}
