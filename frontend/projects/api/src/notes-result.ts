import { NoteResult } from "./note-result";
import { NoteAttachmentOption } from "./note-attachment-option";
export interface NotesResult {
  notes: NoteResult[]; attachments: NoteAttachmentOption[]; maxLength: number;
}
