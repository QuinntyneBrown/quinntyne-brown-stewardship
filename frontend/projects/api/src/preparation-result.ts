import { BookingResult } from "./booking-result";
import { PromptResult } from "./prompt-result";
import { NoteResult } from "./note-result";
export interface PreparationResult {
  moduleId: string | null;
  session: BookingResult; moduleOrdinal: number | null; moduleTitle: string | null; prompts: PromptResult[]; notes: NoteResult[];
}
