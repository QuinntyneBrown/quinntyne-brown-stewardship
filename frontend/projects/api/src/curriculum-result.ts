import { CohortSummary } from "./cohort-summary";
import { ModulePathItem } from "./module-path-item";
import { BookingResult } from "./booking-result";
export interface CurriculumResult {
  isEnrolled: boolean; cohort: CohortSummary | null; modules: ModulePathItem[]; completed: number; remaining: number; percent: number; currentOrdinal: number | null; nextSession: BookingResult | null;
}
