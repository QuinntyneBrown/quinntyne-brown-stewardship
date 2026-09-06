import { BookingResult } from "./booking-result";
export interface HistoryResult {
  sessions: BookingResult[]; bookedCount: number; allowance: number;
}
