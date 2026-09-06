import { NoteResult } from "./note-result";
export interface PromptResult {
  id: string; text: string; answer: NoteResult | null;
}
