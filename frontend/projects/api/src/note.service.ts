import { NotesResult } from "./notes-result";
import { NoteResult } from "./note-result";
import { SaveNoteRequest } from "./save-note-request";
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { INoteService } from './note-service.contract';
import { programmeRequest } from './programme-request';
@Injectable()
export class NoteService implements INoteService {
  private readonly http = inject(HttpClient);
  list(moduleId?: string, sessionId?: string, cursor?: string, generalOnly = false): Promise<NotesResult> { return programmeRequest<NotesResult>(this.http, 'GET', '/notes?' + new URLSearchParams({ ...(moduleId ? { moduleId } : {}), ...(sessionId ? { sessionId } : {}), ...(cursor ? { cursor } : {}), ...(generalOnly ? { generalOnly: 'true' } : {}) }).toString()); }
  get(id: string): Promise<NoteResult> { return programmeRequest<NoteResult>(this.http, 'GET', `/notes/${encodeURIComponent(id)}`); }
  save(note: SaveNoteRequest): Promise<NoteResult> { return programmeRequest<NoteResult>(this.http, 'POST', '/notes', note); }
}
