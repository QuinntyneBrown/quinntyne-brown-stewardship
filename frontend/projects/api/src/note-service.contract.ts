import { NotesResult } from "./notes-result";
import { NoteResult } from "./note-result";
import { SaveNoteRequest } from "./save-note-request";
export interface INoteService {
  list(moduleId?: string, sessionId?: string, cursor?: string, generalOnly?: boolean): Promise<NotesResult>;
  get(id: string): Promise<NoteResult>;
  save(note: SaveNoteRequest): Promise<NoteResult>;
}
