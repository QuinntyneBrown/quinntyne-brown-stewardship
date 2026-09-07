import { inject, Injectable } from '@angular/core';
import { ISessionService, ServiceError } from '@qbs/api';
import { ProgrammeMockStore } from './programme-mock.store';
@Injectable()
export class SessionServiceMock implements ISessionService {
  private readonly store = inject(ProgrammeMockStore);
  async availability(day?: string) { await this.store.waitForResponse(); return this.store.availability(day); }
  async history() { this.store.check(); return { sessions: this.store.data().past, bookedCount: this.store.data().past.length + (this.store.data().booking ? 1 : 0), allowance: this.store.cohort().sessionAllowance }; }
  async get(id: string) { await this.store.waitForResponse(); this.store.check(); const b = [this.store.data().booking, ...this.store.data().past].find(x => x?.id === id); if (!b) throw new ServiceError(404); return b; }
  async preparation(id: string) { const session = await this.get(id); const module = this.store.module(session.moduleOrdinal ?? this.store.lastOrdinal()); const page = this.store.notePage(undefined, id); return { moduleId: module?.id ?? null, session, moduleOrdinal: module?.ordinal ?? null, moduleTitle: module?.title ?? null, prompts: module?.preparationPrompts ?? [], notes: page.notes, notesCursor: page.nextCursor }; }
  async book(slotId: string) { return this.store.book(slotId); }
  async reschedule(id: string, slotId: string) { await this.get(id); return this.store.book(slotId, true); }
  async cancel(id: string) { await this.get(id); this.store.update({ booking: null }); }
}
