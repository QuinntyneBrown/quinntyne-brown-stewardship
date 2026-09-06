import { inject, Injectable, signal } from '@angular/core';
import { AvailabilityResult, BookingResult, CurriculumResult, ModuleResult, NoteResult, SaveNoteRequest, ServiceError } from '@qbs/api';
import { MockStateStore } from './mock-state.store';
const key = 'stewardship.programme-mock';
const titles = ['Begin with stewardship', 'Listen before you build', 'The cost of what we build', 'Repair as a discipline', 'Who is not in the room', 'Handle data with care', 'Make access ordinary', 'Choose enough', 'Build for dependable service', 'Share power through practice', 'Measure what matters', 'Carry the work forward'];
import { ProgrammeMockData } from './programme-mock-data';
@Injectable({ providedIn: 'root' })
export class ProgrammeMockStore {
  private readonly flags = inject(MockStateStore);
  readonly data = signal<ProgrammeMockData>(JSON.parse(sessionStorage.getItem(key) ?? 'null') ?? { completed: [], booking: null, past: [], notes: [], taken: [] });
  readonly cohort = { id: 'cohort', mentorName: 'Quinntyne Brown', timeZone: 'America/Toronto', startDate: '2026-09-07', endDate: '2026-11-30', currentWeek: 1, hasEnded: false, sessionAllowance: 6 };
  update(changes: Partial<ProgrammeMockData>) { this.data.update(s => ({ ...s, ...changes })); sessionStorage.setItem(key, JSON.stringify(this.data())); }
  check() {
    if (!this.flags.current().signedIn) throw new ServiceError(401);
    if (this.flags.current().programmeFailure) throw new ServiceError(503, undefined, 'The programme is temporarily unavailable. Please try again.');
  }
  module(ordinal: number): ModuleResult {
    const sections = Array.from({ length: 5 }, (_, i) => ({ id: `section-${ordinal}-${i + 1}`, ordinal: i + 1, title: ['Notice the responsibility', 'Listen to experience', 'Examine the default', 'Try a repair', 'Reflect and prepare'][i], reading: 'Stewardship begins with attention to the people affected by a technical decision. Describe an ordinary task, ask who experiences difficulty, and listen before proposing a solution. Record what would change your view.\n\nChoose a small, reversible improvement. Explain its benefit and its possible cost to the person who will maintain it. Return to the people affected and check whether the improvement serves their actual needs.', isComplete: this.data().completed.includes(`section-${ordinal}-${i + 1}`) }));
    const count = sections.filter(s => s.isComplete).length;
    return { notes: this.data().notes.filter(n => n.moduleId === `module-${ordinal}`), id: `module-${ordinal}`, ordinal, title: titles[ordinal - 1], summary: 'Learn to serve people through deliberate, responsible technical choices.', effortEstimate: '45–60 minutes', practiceSteps: ['Observe a real task with permission.', 'Identify a cost carried by someone else.', 'Try a small change and record what happened.'], sections, preparationPrompts: [{ id: `prompt-${ordinal}`, text: 'Whose experience changed your decision?', answer: this.data().notes.find(n => n.promptId === `prompt-${ordinal}`) ?? null }], completedSections: count, percent: count * 20, resumeSectionId: (sections.find(s => !s.isComplete) ?? sections[4]).id, isComplete: count === 5 };
  }
  curriculum(): CurriculumResult {
    this.check();
    const modules = titles.map((_, i) => this.module(i + 1));
    const current = modules.find(m => !m.isComplete)?.ordinal ?? null;
    const completed = modules.filter(m => m.isComplete).length;
    return { isEnrolled: true, cohort: this.cohort, modules: modules.map(m => ({ id: m.id, ordinal: m.ordinal, title: m.title, summary: m.summary, state: m.isComplete ? 'Complete' : m.ordinal === current ? 'Current' : 'Locked' })), completed, remaining: 12 - completed, percent: Math.round(completed / 12 * 100), currentOrdinal: current, nextSession: this.data().booking };
  }
  availability(day = '2026-09-10'): AvailabilityResult {
    this.check();
    const date = new Date(day + 'T12:00:00Z'); date.setUTCDate(date.getUTCDate() - (date.getUTCDay() + 6) % 7);
    const weekStart = date.toISOString().slice(0, 10);
    const days = Array.from({ length: 7 }, (_, i) => { const d = new Date(date); d.setUTCDate(d.getUTCDate() + i); return d.toISOString().slice(0, 10); });
    return { history: this.data().past, cohort: this.cohort, weekStart, selectedDay: day, days, slots: [14, 18].map(h => ({ id: `slot-${day}-${h}`, startsAt: `${day}T${h}:00:00Z`, durationMinutes: 45, state: this.data().taken.includes(`slot-${day}-${h}`) || this.data().booking?.slotId === `slot-${day}-${h}` ? 'Taken' : 'Open' })), bookedCount: this.data().past.length + (this.data().booking ? 1 : 0), allowance: 6, bookingReason: this.data().booking ? 'You already hold a future session.' : null, nextSession: this.data().booking };
  }
  book(slotId: string, reschedule = false): BookingResult {
    this.check();
    if (this.flags.current().slotConflict) { this.flags.update({ slotConflict: false }); this.update({ taken: [...this.data().taken, slotId] }); throw new ServiceError(409, undefined, 'That slot is no longer available. Choose another time.'); }
    if (this.data().booking && !reschedule) throw new ServiceError(409);
    const parts = /^slot-(\d{4}-\d{2}-\d{2})-(\d{2})$/.exec(slotId)!;
    const current = this.module(this.curriculum().currentOrdinal ?? 12);
    const booking: BookingResult = { id: this.data().booking?.id ?? crypto.randomUUID(), slotId, startsAt: `${parts[1]}T${parts[2]}:00:00Z`, durationMinutes: 45, mentorName: this.cohort.mentorName, timeZone: this.cohort.timeZone, status: 'Booked', canChange: true, changeReason: null, moduleOrdinal: current.ordinal, moduleTitle: current.title };
    this.update({ booking }); return booking;
  }
  note(request: SaveNoteRequest): NoteResult {
    this.check();
    if (!request.body.trim() || request.body.length > 10000) throw new ServiceError(400, undefined, 'Write a note of 1 to 10,000 characters.');
    const old = this.data().notes.find(n => n.id === request.id);
    if (old && old.revision !== request.revision) throw new ServiceError(409, undefined, 'This note changed elsewhere. Reload its latest revision before saving your draft.');
    const note: NoteResult = { id: old?.id ?? crypto.randomUUID(), body: request.body, moduleId: request.moduleId, sessionId: request.sessionId, promptId: request.promptId ?? null, revision: crypto.randomUUID(), revisedAt: new Date().toISOString(), attachmentTitle: request.moduleId ? titles[Number(request.moduleId.split('-')[1]) - 1] : 'Session with Quinntyne Brown', canEdit: true };
    this.update({ notes: [note, ...this.data().notes.filter(n => n.id !== note.id)] }); return note;
  }
}



