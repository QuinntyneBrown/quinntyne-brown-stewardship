import { computed, inject, Injectable, signal } from '@angular/core';
import { AuthoringLimits, AvailabilityResult, BookingResult, CohortSummary, CreatedResult, CurriculaResult, CurriculumDraftResult, CurriculumResult, ModuleDraftResult, ModuleResult, ModuleRevisionRequest, NoteResult, PublicationResult, ReadinessResult, RevisionResult, SaveNoteRequest, SectionDraftResult, SectionRevisionRequest, ServiceError, ValidationError } from '@qbs/api';
import { MockStateStore } from './mock-state.store';
import { ProgrammeMockData } from './programme-mock-data';
import { DEFAULT_SHAPE, ProgrammeShape } from './programme-shape';
import { MockCurriculum, MockModule, MockPrompt, MockSection } from './mock-curriculum';
const key = 'stewardship.programme-mock';
const titles = ['Begin with stewardship', 'Listen before you build', 'The cost of what we build', 'Repair as a discipline', 'Who is not in the room', 'Handle data with care', 'Make access ordinary', 'Choose enough', 'Build for dependable service', 'Share power through practice', 'Measure what matters', 'Carry the work forward'];
const sectionTitles = ['Notice the responsibility', 'Listen to experience', 'Examine the default', 'Try a repair', 'Reflect and prepare'];
const reading = 'Stewardship begins with attention to the people affected by a technical decision. Describe an ordinary task, ask who experiences difficulty, and listen before proposing a solution. Record what would change your view.\n\nChoose a small, reversible improvement. Explain its benefit and its possible cost to the person who will maintain it. Return to the people affected and check whether the improvement serves their actual needs.';
const startDate = '2026-09-07';
// The maxima the mock server enforces, stated once like the API options they stand in for.
const limits: AuthoringLimits = { key: 100, title: 120, summary: 400, reading: 12000, effortEstimate: 60, practiceStep: 400, prompt: 400 };
function fabricateModule(prefix: string, ordinal: number, sectionCount: number, published: boolean): MockModule {
  return { id: `${prefix}module-${ordinal}`, ordinal, title: titles[(ordinal - 1) % titles.length], summary: 'Learn to serve people through deliberate, responsible technical choices.', effortEstimate: '45–60 minutes', practiceSteps: ['Observe a real task with permission.', 'Identify a cost carried by someone else.', 'Try a small change and record what happened.'], state: published ? 'Published' : 'Draft', revision: `revision-${prefix}module-${ordinal}`,
    sections: Array.from({ length: sectionCount }, (_, i) => ({ id: `${prefix}section-${ordinal}-${i + 1}`, ordinal: i + 1, title: sectionTitles[i % sectionTitles.length], reading, revision: `revision-${prefix}section-${ordinal}-${i + 1}`, completionCount: 0 })),
    prompts: [{ id: `${prefix}prompt-${ordinal}`, ordinal: 1, text: 'Whose experience changed your decision?', answerCount: 0 }], noteCount: 0 };
}
// The world the mockups draw: the participant's programme built from the seeded shape, beside a published eight-module programme and a draft of four whose third module has no sections.
function fabricate(shape: ProgrammeShape, noProgrammes: boolean | undefined): MockCurriculum[] {
  if (noProgrammes) return [];
  const foundations = ['What a founder owes', 'The first promise', 'Choose enough', 'Hand it on'].map((title, i) => ({ ...fabricateModule('c3-', i + 1, i === 2 ? 0 : 3, false), title }));
  return [
    { id: 'curriculum-1', key: 'stewardship-core', title: 'Stewardship — core', state: shape.published ? 'Published' : 'Draft', createdAt: '2026-02-04T09:00:00Z', publishedAt: shape.published ? '2026-05-14T09:12:00Z' : null, cohortCount: shape.published ? 2 : 0, modules: [...Array.from({ length: shape.moduleCount }, (_, i) => fabricateModule('', i + 1, shape.sectionCount, shape.published)), ...Array.from({ length: shape.pendingModules ?? 0 }, (_, i) => fabricateModule('pending-', shape.moduleCount + i + 1, 3, false))] },
    { id: 'curriculum-2', key: 'redemptive-intensive', title: 'Redemptive practice intensive', state: 'Published', createdAt: '2026-03-01T09:00:00Z', publishedAt: '2026-05-14T09:12:00Z', cohortCount: 1, modules: Array.from({ length: 8 }, (_, i) => fabricateModule('c2-', i + 1, 4, true)) },
    { id: 'curriculum-3', key: 'foundations', title: 'Foundations for founders', state: 'Draft', createdAt: '2026-08-20T11:38:00Z', publishedAt: null, cohortCount: 0, modules: foundations },
  ];
}
@Injectable({ providedIn: 'root' })
export class ProgrammeMockStore {
  private readonly flags = inject(MockStateStore);
  readonly data = signal<ProgrammeMockData>(JSON.parse(sessionStorage.getItem(key) ?? 'null') ?? { completed: [], booking: null, past: [], notes: [], taken: [], ...window.__stewardship?.programmeSeed });
  // The fabricated programme and cohort take their size from the seed; a spec states the shape it needs.
  readonly shape = computed<ProgrammeShape>(() => ({ ...DEFAULT_SHAPE, ...this.data().shape }));
  readonly limits = limits;
  constructor() { if (!this.data().curricula) this.update({ curricula: fabricate(this.shape(), this.data().noProgrammes) }); }
  update(changes: Partial<ProgrammeMockData>) { this.data.update(s => ({ ...s, ...changes })); sessionStorage.setItem(key, JSON.stringify(this.data())); }
  waitForResponse() { return this.flags.waitForResponse(); }
  private curriculaList() { return this.data().curricula ?? []; }
  // The enrolled cohort follows the first programme; a participant reads its published modules only.
  private programme() { return this.curriculaList()[0]; }
  private publishedModules() { const programme = this.programme(); return programme?.state === 'Published' ? programme.modules.filter(m => m.state === 'Published').sort((a, b) => a.ordinal - b.ordinal) : []; }
  lastOrdinal() { return this.publishedModules().at(-1)?.ordinal ?? 0; }
  cohort(): CohortSummary {
    const { durationWeeks, sessionCadenceWeeks } = this.shape();
    const end = new Date(startDate + 'T00:00:00Z'); end.setUTCDate(end.getUTCDate() + durationWeeks * 7);
    return { id: 'cohort', mentorName: 'Quinntyne Brown', timeZone: 'America/Toronto', startDate, endDate: end.toISOString().slice(0, 10), currentWeek: 1, hasEnded: false, sessionAllowance: Math.floor(durationWeeks / sessionCadenceWeeks), durationWeeks, sessionCadenceWeeks };
  }
  notePage(moduleId?: string, sessionId?: string, cursor?: string, generalOnly = false) {
    const candidates = this.data().notes.filter(n => (!moduleId || n.moduleId === moduleId) && (!sessionId || n.sessionId === sessionId) && (!generalOnly || n.promptId === null));
    const index = cursor ? candidates.findIndex(n => n.id === cursor) + 1 : 0;
    if (cursor && index === 0) throw new ServiceError(400, undefined, 'The notes cursor is invalid.');
    const notes: NoteResult[] = []; let bytes = 0;
    for (const note of candidates.slice(index)) {
      const size = new TextEncoder().encode(JSON.stringify(note)).length;
      if (notes.length && (notes.length === 20 || bytes + size > 16000)) break;
      notes.push(note); bytes += size;
    }
    return { notes, nextCursor: index + notes.length < candidates.length ? notes.at(-1)!.id : null };
  }
  check() {
    if (!this.flags.current().signedIn) throw new ServiceError(401);
    if (this.flags.current().programmeFailure) throw new ServiceError(503, undefined, 'The programme is temporarily unavailable. Please try again.');
  }
  private read(module: MockModule): ModuleResult {
    const sections = module.sections.map(s => ({ id: s.id, ordinal: s.ordinal, title: s.title, reading: s.reading, isComplete: this.data().completed.includes(s.id) }));
    const count = sections.filter(s => s.isComplete).length;
    const page = this.notePage(module.id, undefined, undefined, true);
    return { notes: page.notes, notesCursor: page.nextCursor, id: module.id, ordinal: module.ordinal, title: module.title, summary: module.summary, effortEstimate: module.effortEstimate, practiceSteps: module.practiceSteps, sections, preparationPrompts: module.prompts.map(p => ({ id: p.id, text: p.text, answer: this.data().notes.find(n => n.promptId === p.id) ?? null })), completedSections: count, percent: sections.length ? Math.round(count / sections.length * 100) : 0, resumeSectionId: (sections.find(s => !s.isComplete) ?? sections.at(-1))?.id ?? '', isComplete: sections.length > 0 && count === sections.length };
  }
  module(ordinal: number): ModuleResult | null { const module = this.publishedModules().find(m => m.ordinal === ordinal); return module ? this.read(module) : null; }
  moduleById(id: string): ModuleResult | null { const module = this.publishedModules().find(m => m.id === id); return module ? this.read(module) : null; }
  curriculum(): CurriculumResult {
    this.check();
    const published = this.programme()?.state === 'Published';
    const modules = this.publishedModules().map(m => this.read(m));
    if (!published) return { isEnrolled: true, isProgrammePublished: false, cohort: this.cohort(), modules: [], completed: 0, remaining: 0, percent: 0, currentOrdinal: null, nextSession: this.data().booking };
    const current = modules.find(m => !m.isComplete)?.ordinal ?? null;
    const completed = modules.filter(m => m.isComplete).length;
    return { isEnrolled: true, isProgrammePublished: true, cohort: this.cohort(), modules: modules.map(m => ({ id: m.id, ordinal: m.ordinal, title: m.title, summary: m.summary, state: m.isComplete ? 'Complete' : m.ordinal === current ? 'Current' : 'Locked' })), completed, remaining: modules.length - completed, percent: modules.length ? Math.round(completed / modules.length * 100) : 0, currentOrdinal: current, nextSession: this.data().booking };
  }
  availability(day = '2026-09-10'): AvailabilityResult {
    this.check();
    const date = new Date(day + 'T12:00:00Z'); date.setUTCDate(date.getUTCDate() - (date.getUTCDay() + 6) % 7);
    const weekStart = date.toISOString().slice(0, 10);
    const days = Array.from({ length: 7 }, (_, i) => { const d = new Date(date); d.setUTCDate(d.getUTCDate() + i); return d.toISOString().slice(0, 10); });
    const cohort = this.cohort();
    return { history: this.data().past, cohort, weekStart, selectedDay: day, days, slots: [14, 18].map(h => ({ id: `slot-${day}-${h}`, startsAt: `${day}T${h}:00:00Z`, durationMinutes: 45, state: this.data().taken.includes(`slot-${day}-${h}`) || this.data().booking?.slotId === `slot-${day}-${h}` ? 'Taken' : 'Open' })), bookedCount: this.data().past.length + (this.data().booking ? 1 : 0), allowance: cohort.sessionAllowance, bookingReason: this.data().booking ? 'You already hold a future session.' : null, nextSession: this.data().booking };
  }
  book(slotId: string, reschedule = false): BookingResult {
    this.check();
    if (this.flags.current().slotConflict) { this.flags.update({ slotConflict: false }); this.update({ taken: [...this.data().taken, slotId] }); throw new ServiceError(409, undefined, 'That slot is no longer available. Choose another time.'); }
    if (this.data().booking && !reschedule) throw new ServiceError(409);
    const parts = /^slot-(\d{4}-\d{2}-\d{2})-(\d{2})$/.exec(slotId)!;
    const current = this.module(this.curriculum().currentOrdinal ?? this.lastOrdinal());
    const booking: BookingResult = { id: this.data().booking?.id ?? crypto.randomUUID(), slotId, startsAt: `${parts[1]}T${parts[2]}:00:00Z`, durationMinutes: 45, mentorName: this.cohort().mentorName, timeZone: this.cohort().timeZone, status: 'Booked', canChange: true, changeReason: null, moduleOrdinal: current?.ordinal ?? null, moduleTitle: current?.title ?? null };
    this.update({ booking }); return booking;
  }
  note(request: SaveNoteRequest): NoteResult {
    this.check();
    if (!request.body.trim() || request.body.length > 10000) throw new ServiceError(400, undefined, 'Write a note of 1 to 10,000 characters.');
    const old = this.data().notes.find(n => n.id === request.id);
    if (old && old.revision !== request.revision) throw new ServiceError(409, undefined, 'This note changed elsewhere. Reload its latest revision before saving your draft.');
    const note: NoteResult = { id: old?.id ?? crypto.randomUUID(), body: request.body, moduleId: request.moduleId, sessionId: request.sessionId, promptId: request.promptId ?? null, revision: crypto.randomUUID(), revisedAt: new Date().toISOString(), attachmentTitle: request.moduleId ? this.moduleById(request.moduleId)?.title ?? 'Module' : 'Session with Quinntyne Brown', canEdit: true };
    this.update({ notes: [note, ...this.data().notes.filter(n => n.id !== note.id)] }); return note;
  }
  // --- Authoring. Every refusal carries the sentence the mockups draw for it.
  private findProgramme(id: string) { return this.curriculaList().find(c => c.id === id) ?? (() => { throw new ServiceError(404, undefined, 'Programme not found.'); })(); }
  private saveProgramme(programme: MockCurriculum) { this.update({ curricula: this.curriculaList().map(c => c.id === programme.id ? programme : c) }); }
  private refuse(errors: Record<string, string[]>) { if (Object.keys(errors).length) throw new ValidationError(400, undefined, 'Check the highlighted fields.', errors); }
  private lengthErrors(field: string, label: string, value: string, maximum: number, errors: Record<string, string[]>) {
    const article = 'aeiou'.includes(label[0]) ? 'An' : 'A'; const length = [...value].length;
    if (!value.trim()) errors[field] = [`${article} ${label} is required.`];
    else if (length > maximum) errors[field] = [`${article} ${label} may be at most ${maximum} characters. Shorten it by ${length - maximum}.`];
  }
  private keyErrors(value: string, errors: Record<string, string[]>) {
    this.lengthErrors('key', 'key', value, limits.key, errors);
    if (!errors['key'] && !/^[a-z0-9-]+$/.test(value)) errors['key'] = ['A key uses lowercase letters, digits and hyphens only.'];
  }
  private keyTaken(key: string, except?: string) { if (this.curriculaList().some(c => c.key === key && c.id !== except)) throw new ServiceError(409, undefined, `The key ${key} is already used by another programme. Choose a different key.`); }
  curricula(): CurriculaResult {
    return { programmes: this.curriculaList().map(c => ({ id: c.id, key: c.key, title: c.title, state: c.state, moduleCount: c.modules.length, publishedModuleCount: c.modules.filter(m => m.state === 'Published').length, cohortCount: c.cohortCount })), limits };
  }
  createProgramme(key: string, title: string): CreatedResult {
    const errors: Record<string, string[]> = {}; this.keyErrors(key, errors); this.lengthErrors('title', 'title', title, limits.title, errors); this.refuse(errors);
    this.keyTaken(key);
    const programme: MockCurriculum = { id: crypto.randomUUID(), key, title, state: 'Draft', createdAt: new Date().toISOString(), publishedAt: null, cohortCount: 0, modules: [] };
    this.update({ curricula: [...this.curriculaList(), programme] });
    return { id: programme.id };
  }
  private readiness(programme: MockCurriculum): ReadinessResult {
    const empty = programme.modules.filter(m => m.sections.length === 0).sort((a, b) => a.ordinal - b.ordinal);
    const reason = programme.modules.length === 0 ? 'This programme cannot be published. It has no modules, so a participant would have nothing to read.'
      : empty.length ? `${empty.length} module${empty.length === 1 ? ' carries' : 's carry'} no section: ${empty.map(m => `${String(m.ordinal).padStart(2, '0')} ${m.title}`).join(', ')}. Every module needs at least one before this programme can be published.` : null;
    const { durationWeeks, sessionCadenceWeeks } = this.shape();
    const cohorts = Array.from({ length: programme.cohortCount }, (_, i) => ({ id: `${programme.id}-cohort-${i + 1}`, startDate, endDate: this.cohort().endDate, durationWeeks, sessionCadenceWeeks, sessionAllowance: Math.floor(durationWeeks / sessionCadenceWeeks), hasEnded: false }));
    return { canPublish: reason === null, reason, emptyModuleTitles: empty.map(m => `${String(m.ordinal).padStart(2, '0')} ${m.title}`), unpublishedModuleCount: programme.modules.filter(m => m.state === 'Draft').length, activeCohortCount: programme.cohortCount, participantsMovedBack: 0, movedBackToOrdinal: null, cohorts };
  }
  programmeDraft(id: string): CurriculumDraftResult {
    const programme = this.findProgramme(id);
    return { id: programme.id, key: programme.key, title: programme.title, state: programme.state, createdAt: programme.createdAt, publishedAt: programme.publishedAt, cohortCount: programme.cohortCount, canChangeKey: programme.cohortCount === 0, canRemove: programme.cohortCount === 0,
      modules: [...programme.modules].sort((a, b) => a.ordinal - b.ordinal).map(m => ({ id: m.id, ordinal: m.ordinal, title: m.title, summary: m.summary, state: m.state, sectionCount: m.sections.length, stepCount: m.practiceSteps.length, promptCount: m.prompts.length })),
      readiness: this.readiness(programme), limits };
  }
  renameProgramme(id: string, title: string) {
    const programme = this.findProgramme(id);
    const errors: Record<string, string[]> = {}; this.lengthErrors('title', 'title', title, limits.title, errors); this.refuse(errors);
    this.saveProgramme({ ...programme, title });
  }
  changeKey(id: string, key: string) {
    const programme = this.findProgramme(id);
    if (programme.cohortCount > 0) throw new ServiceError(409, undefined, `${programme.cohortCount} cohort${programme.cohortCount === 1 ? '' : 's'} follow this programme. The key cannot be changed while they do.`);
    const errors: Record<string, string[]> = {}; this.keyErrors(key, errors); this.refuse(errors);
    this.keyTaken(key, id);
    this.saveProgramme({ ...programme, key });
  }
  // Publication is refused for the reason the draft already states; otherwise every module becomes readable at once.
  publish(id: string): PublicationResult {
    const programme = this.findProgramme(id);
    const readiness = this.readiness(programme);
    if (!readiness.canPublish) throw new ServiceError(409, undefined, readiness.reason!);
    const publishedAt = new Date().toISOString();
    this.saveProgramme({ ...programme, state: 'Published', publishedAt, modules: programme.modules.map(m => ({ ...m, state: 'Published' })) });
    return { state: 'Published', publishedAt, publishedModuleCount: programme.modules.length };
  }
  removeProgramme(id: string) {
    const programme = this.findProgramme(id);
    if (programme.cohortCount > 0) throw new ServiceError(409, undefined, `${programme.cohortCount} cohort${programme.cohortCount === 1 ? '' : 's'} follow this programme. It cannot be removed while they do.`);
    this.update({ curricula: this.curriculaList().filter(c => c.id !== id) });
  }
  // --- Modules and prompts. What participants recorded comes from the seeded completions and notes.
  private findModule(id: string) {
    for (const programme of this.curriculaList()) { const module = programme.modules.find(m => m.id === id); if (module) return { programme, module }; }
    throw new ServiceError(404, undefined, 'Module not found.');
  }
  private saveModule(programme: MockCurriculum, module: MockModule) { this.saveProgramme({ ...programme, modules: programme.modules.map(m => m.id === module.id ? module : m) }); }
  private completionCount(section: MockSection) { return this.data().completed.includes(section.id) ? 1 : section.completionCount; }
  private answerCount(prompt: MockPrompt) { return this.data().notes.filter(n => n.promptId === prompt.id).length + prompt.answerCount; }
  private moduleNoteCount(module: MockModule) { return this.data().notes.filter(n => n.moduleId === module.id && n.promptId === null).length + module.noteCount; }
  private dependents(module: MockModule) {
    const completions = module.sections.reduce((sum, s) => sum + this.completionCount(s), 0);
    const notes = this.moduleNoteCount(module); const answers = module.prompts.reduce((sum, p) => sum + this.answerCount(p), 0);
    const parts: string[] = [];
    if (completions) parts.push(`${completions} completion${completions === 1 ? ' is' : 's are'} recorded against the sections of this module.`);
    if (notes) parts.push(`${notes} note${notes === 1 ? ' is' : 's are'} attached to this module.`);
    if (answers) parts.push(`${answers} participant${answers === 1 ? ' has' : 's have'} answered a prompt in this module.`);
    return { completions, notes, answers, reason: parts.length ? parts.join(' ') + ' It cannot be removed while those records stand.' : null };
  }
  moduleDraft(id: string): ModuleDraftResult {
    const { programme, module } = this.findModule(id);
    const dependents = this.dependents(module);
    return { id: module.id, curriculumId: programme.id, curriculumKey: programme.key, curriculumTitle: programme.title, ordinal: module.ordinal, moduleCount: programme.modules.length,
      title: module.title, summary: module.summary, effortEstimate: module.effortEstimate, practiceSteps: [...module.practiceSteps], state: module.state, revision: module.revision,
      completionCount: dependents.completions, noteCount: dependents.notes, answerCount: dependents.answers, canRemove: !dependents.reason, removalReason: dependents.reason,
      sections: [...module.sections].sort((a, b) => a.ordinal - b.ordinal).map(s => ({ id: s.id, ordinal: s.ordinal, title: s.title, wordCount: s.reading.split(/\s+/).filter(Boolean).length, completionCount: this.completionCount(s), canRemove: this.completionCount(s) === 0 })),
      prompts: [...module.prompts].sort((a, b) => a.ordinal - b.ordinal).map(p => ({ id: p.id, ordinal: p.ordinal, text: p.text, answerCount: this.answerCount(p), canRemove: this.answerCount(p) === 0 })), limits };
  }
  addModule(curriculumId: string, title: string, summary: string): CreatedResult {
    const programme = this.findProgramme(curriculumId);
    const errors: Record<string, string[]> = {}; this.lengthErrors('title', 'title', title, limits.title, errors); this.lengthErrors('summary', 'summary', summary, limits.summary, errors); this.refuse(errors);
    const module: MockModule = { id: crypto.randomUUID(), ordinal: programme.modules.length + 1, title, summary, effortEstimate: '', practiceSteps: [], state: 'Draft', revision: crypto.randomUUID(), sections: [], prompts: [], noteCount: 0 };
    this.saveProgramme({ ...programme, modules: [...programme.modules, module] });
    return { id: module.id };
  }
  reviseModule(id: string, request: ModuleRevisionRequest, flags: MockStateStore): RevisionResult {
    let { programme, module } = this.findModule(id);
    // Another administrator saved first: the stored revision moves on and this save is refused once.
    if (flags.current().staleRevision) { flags.update({ staleRevision: false }); module = { ...module, title: `${module.title} (revised elsewhere)`, revision: crypto.randomUUID() }; this.saveModule(programme, module); }
    const errors: Record<string, string[]> = {};
    this.lengthErrors('title', 'title', request.title, limits.title, errors); this.lengthErrors('summary', 'summary', request.summary, limits.summary, errors); this.lengthErrors('effortEstimate', 'effort estimate', request.effortEstimate, limits.effortEstimate, errors);
    request.practiceSteps.forEach((step, i) => this.lengthErrors(`practiceSteps[${i}]`, 'practice step', step, limits.practiceStep, errors));
    this.refuse(errors);
    if (module.revision !== request.revision) throw new ServiceError(409, undefined, 'This module changed since it was opened. Reload it to see the current content.');
    const revision = crypto.randomUUID();
    this.saveModule(programme, { ...module, title: request.title, summary: request.summary, effortEstimate: request.effortEstimate, practiceSteps: [...request.practiceSteps], revision });
    return { revision };
  }
  removeModule(id: string) {
    const { programme, module } = this.findModule(id);
    const dependents = this.dependents(module);
    if (dependents.reason) throw new ServiceError(409, undefined, dependents.reason);
    this.saveProgramme({ ...programme, modules: programme.modules.filter(m => m.id !== id).sort((a, b) => a.ordinal - b.ordinal).map((m, i) => ({ ...m, ordinal: i + 1 })) });
  }
  addPrompt(moduleId: string, text: string): CreatedResult {
    const { programme, module } = this.findModule(moduleId);
    const errors: Record<string, string[]> = {}; this.lengthErrors('text', 'prompt', text, limits.prompt, errors); this.refuse(errors);
    const prompt: MockPrompt = { id: crypto.randomUUID(), ordinal: module.prompts.length + 1, text, answerCount: 0 };
    this.saveModule(programme, { ...module, prompts: [...module.prompts, prompt] });
    return { id: prompt.id };
  }
  private findPrompt(id: string) {
    for (const programme of this.curriculaList()) for (const module of programme.modules) { const prompt = module.prompts.find(p => p.id === id); if (prompt) return { programme, module, prompt }; }
    throw new ServiceError(404, undefined, 'Prompt not found.');
  }
  revisePrompt(id: string, text: string) {
    const { programme, module, prompt } = this.findPrompt(id);
    const errors: Record<string, string[]> = {}; this.lengthErrors('text', 'prompt', text, limits.prompt, errors); this.refuse(errors);
    this.saveModule(programme, { ...module, prompts: module.prompts.map(p => p.id === id ? { ...p, text } : p) });
  }
  // --- Sections.
  private findSection(id: string) {
    for (const programme of this.curriculaList()) for (const module of programme.modules) { const section = module.sections.find(s => s.id === id); if (section) return { programme, module, section }; }
    throw new ServiceError(404, undefined, 'Section not found.');
  }
  sectionDraft(id: string): SectionDraftResult {
    const { programme, module, section } = this.findSection(id);
    const completions = this.completionCount(section);
    return { id: section.id, moduleId: module.id, moduleOrdinal: module.ordinal, moduleTitle: module.title, curriculumId: programme.id, curriculumTitle: programme.title, ordinal: section.ordinal, sectionCount: module.sections.length,
      title: section.title, reading: section.reading, revision: section.revision, completionCount: completions, canRemove: completions === 0, createdAt: '2026-02-04T09:00:00Z',
      siblings: [...module.sections].sort((a, b) => a.ordinal - b.ordinal).map(s => ({ id: s.id, ordinal: s.ordinal, title: s.title })), limits };
  }
  addSection(moduleId: string, title: string, reading: string): CreatedResult {
    const { programme, module } = this.findModule(moduleId);
    const errors: Record<string, string[]> = {}; this.lengthErrors('title', 'title', title, limits.title, errors); this.lengthErrors('reading', 'reading', reading, limits.reading, errors); this.refuse(errors);
    const section: MockSection = { id: crypto.randomUUID(), ordinal: module.sections.length + 1, title, reading, revision: crypto.randomUUID(), completionCount: 0 };
    this.saveModule(programme, { ...module, sections: [...module.sections, section] });
    return { id: section.id };
  }
  reviseSection(id: string, request: SectionRevisionRequest, flags: MockStateStore): RevisionResult {
    let { programme, module, section } = this.findSection(id);
    if (flags.current().staleRevision) { flags.update({ staleRevision: false }); section = { ...section, title: `${section.title} (revised elsewhere)`, revision: crypto.randomUUID() }; this.saveModule(programme, { ...module, sections: module.sections.map(s => s.id === id ? section : s) }); }
    const errors: Record<string, string[]> = {}; this.lengthErrors('title', 'title', request.title, limits.title, errors); this.lengthErrors('reading', 'reading', request.reading, limits.reading, errors); this.refuse(errors);
    if (section.revision !== request.revision) throw new ServiceError(409, undefined, 'This section changed since it was opened. Reload it to see the current content.');
    const revision = crypto.randomUUID();
    this.saveModule(programme, { ...module, sections: module.sections.map(s => s.id === id ? { ...s, title: request.title, reading: request.reading, revision } : s) });
    return { revision };
  }
  removeSection(id: string) {
    const { programme, module, section } = this.findSection(id);
    const completions = this.completionCount(section);
    if (completions > 0) throw new ServiceError(409, undefined, `${completions} completion${completions === 1 ? ' is' : 's are'} recorded against this section. It cannot be removed while ${completions === 1 ? 'that record stands' : 'those records stand'}.`);
    this.saveModule(programme, { ...module, sections: module.sections.filter(s => s.id !== id).sort((a, b) => a.ordinal - b.ordinal).map((s, i) => ({ ...s, ordinal: i + 1 })) });
  }
  // --- Ordering. A submitted list must name every child once; a refusal leaves the stored order as it was.
  private arrange<T extends { id: string; ordinal: number }>(children: T[], order: string[], noun: string, parent: string, flags: MockStateStore): T[] {
    if (flags.current().reorderRefused) { flags.update({ reorderRefused: false }); throw new ServiceError(409, undefined, `The submitted order does not match the ${noun} of this ${parent}. Reload and try again.`); }
    if (order.length !== children.length || new Set(order).size !== order.length || order.some(id => !children.some(c => c.id === id))) throw new ServiceError(409, undefined, `The submitted order does not match the ${noun} of this ${parent}. Reload and try again.`);
    return order.map((id, index) => ({ ...children.find(c => c.id === id)!, ordinal: index + 1 }));
  }
  reorderModules(curriculumId: string, order: string[], flags: MockStateStore) {
    const programme = this.findProgramme(curriculumId);
    this.saveProgramme({ ...programme, modules: this.arrange(programme.modules, order, 'modules', 'programme', flags) });
  }
  reorderSections(moduleId: string, order: string[], flags: MockStateStore) {
    const { programme, module } = this.findModule(moduleId);
    this.saveModule(programme, { ...module, sections: this.arrange(module.sections, order, 'sections', 'module', flags) });
  }
  reorderPrompts(moduleId: string, order: string[], flags: MockStateStore) {
    const { programme, module } = this.findModule(moduleId);
    this.saveModule(programme, { ...module, prompts: this.arrange(module.prompts, order, 'prompts', 'module', flags) });
  }
  removePrompt(id: string) {
    const { programme, module, prompt } = this.findPrompt(id);
    const answers = this.answerCount(prompt);
    if (answers > 0) throw new ServiceError(409, undefined, `${answers} participant${answers === 1 ? ' has' : 's have'} answered this prompt. It cannot be removed while ${answers === 1 ? 'that answer stands' : 'their answers stand'}.`);
    this.saveModule(programme, { ...module, prompts: module.prompts.filter(p => p.id !== id).sort((a, b) => a.ordinal - b.ordinal).map((p, i) => ({ ...p, ordinal: i + 1 })) });
  }
}
