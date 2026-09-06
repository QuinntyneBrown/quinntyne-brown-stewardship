import { AvailabilityResult } from "./availability-result";
import { HistoryResult } from "./history-result";
import { BookingResult } from "./booking-result";
import { PreparationResult } from "./preparation-result";
export interface ISessionService {
  availability(day?: string): Promise<AvailabilityResult>;
  history(): Promise<HistoryResult>;
  get(id: string): Promise<BookingResult>;
  preparation(id: string): Promise<PreparationResult>;
  book(slotId: string): Promise<BookingResult>;
  reschedule(id: string, slotId: string): Promise<BookingResult>;
  cancel(id: string): Promise<void>;
}
