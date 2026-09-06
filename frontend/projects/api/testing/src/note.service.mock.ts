import { inject, Injectable } from '@angular/core';
import { INoteService, SaveNoteRequest, ServiceError } from '@qbs/api';
import { ProgrammeMockStore } from './programme-mock.store';
@Injectable()
export class NoteServiceMock implements INoteService {
  private readonly store = inject(ProgrammeMockStore);
  async list(moduleId?: string, sessionId?: string) {
    this.store.check();
    const curriculum = this.store.curriculum();
    return { notes: this.store.data().notes.filter(n => (!moduleId || n.moduleId === moduleId) && (!sessionId || n.sessionId === sessionId)), attachments: [
      ...curriculum.modules.filter(m => m.state !== 'Locked').map(m => ({ moduleId: m.id, sessionId: null as string | null, title: m.title })),
      ...[this.store.data().booking, ...this.store.data().past].filter(b => b !== null).map(b => ({ moduleId: null as string | null, sessionId: b.id, title: 'Session with ' + b.mentorName }))
    ], maxLength: 10000 };
  }
  async get(id: string) { this.store.check(); const n = this.store.data().notes.find(n => n.id === id); if (!n) throw new ServiceError(404); return n; }
  async save(note: SaveNoteRequest) { return this.store.note(note); }
}
