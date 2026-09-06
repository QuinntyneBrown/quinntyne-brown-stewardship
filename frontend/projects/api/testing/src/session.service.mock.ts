import { inject, Injectable } from '@angular/core';
import { ISessionService, ServiceError } from '@qbs/api';
import { ProgrammeMockStore } from './programme-mock.store';
@Injectable()
export class SessionServiceMock implements ISessionService {
  private readonly store = inject(ProgrammeMockStore);
  async availability(day?: string) { return this.store.availability(day); }
  async history() { this.store.check(); return { sessions: this.store.data().past, bookedCount: this.store.data().past.length + (this.store.data().booking ? 1 : 0), allowance: 6 }; }
  async get(id: string) { this.store.check(); const b = [this.store.data().booking, ...this.store.data().past].find(x => x?.id === id); if (!b) throw new ServiceError(404); return b; }
  async preparation(id: string) { const session = await this.get(id); const module = this.store.module(session.moduleOrdinal ?? 12); return { moduleId: module.id, session, moduleOrdinal: module.ordinal, moduleTitle: module.title, prompts: module.preparationPrompts, notes: this.store.data().notes.filter(n => n.sessionId === id) }; }
  async book(slotId: string) { return this.store.book(slotId); }
  async reschedule(id: string, slotId: string) { await this.get(id); return this.store.book(slotId, true); }
  async cancel(id: string) { await this.get(id); this.store.update({ booking: null }); }
}

