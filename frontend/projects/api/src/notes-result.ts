import { NoteResult } from "./note-result";
import { NoteAttachmentOption } from "./note-attachment-option";
export interface NotesResult {
  nextCursor: string | null;
  notes: NoteResult[]; attachments: NoteAttachmentOption[]; maxLength: number;
}
