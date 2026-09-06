import { CohortSummary } from "./cohort-summary";
import { SlotResult } from "./slot-result";
import { BookingResult } from "./booking-result";
export interface AvailabilityResult {
  history: BookingResult[];
  cohort: CohortSummary; weekStart: string; selectedDay: string; days: string[]; slots: SlotResult[]; bookedCount: number; allowance: number; bookingReason: string | null; nextSession: BookingResult | null;
}
