import { AuthoringLimits } from "./authoring-limits";
import { CurriculumSummaryResult } from "./curriculum-summary-result";
export interface CurriculaResult {
  programmes: CurriculumSummaryResult[]; limits: AuthoringLimits;
}
