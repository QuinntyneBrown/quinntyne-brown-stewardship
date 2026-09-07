import { Component, computed, inject, input, output, signal, viewChild, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EmptyStateComponent, ErrorMessageComponent, RetryNoticeComponent, StatusMessageComponent, StatePillComponent, TextFieldComponent, TextAreaFieldComponent, PracticeStepsEditorComponent, PromptsEditorComponent, PromptDraft, OrderControlComponent } from '@qbs/components';
import { moveWithin } from '../move-within';
import { AUTHORING_SERVICE, ModuleDraftResult, ModuleResult, SectionDraftSummary, ServiceError, ValidationError } from '@qbs/api';
// One week of a programme: what a participant reads about it, the practice they attempt, and the prompts that prepare the session after it.
// The form is one dirty signal over every part, saved as one revision; prompts are saved one by one after it.
@Component({
  selector: 'qbs-module-editor', imports: [RouterLink, EmptyStateComponent, OrderControlComponent, ErrorMessageComponent, RetryNoticeComponent, StatusMessageComponent, StatePillComponent, TextFieldComponent, TextAreaFieldComponent, PracticeStepsEditorComponent, PromptsEditorComponent], templateUrl: './module-editor.component.html', styleUrl: './module-editor.component.css'
})
export class ModuleEditorComponent implements OnInit {
  private readonly service = inject(AUTHORING_SERVICE);
  readonly id = input.required<string>();
  readonly expired = output<void>(); readonly loaded = output<ModuleDraftResult>(); readonly removeRequested = output<void>(); readonly removed = output<void>(); readonly promptRemovalRequested = output<PromptDraft>(); readonly sectionRemovalRequested = output<SectionDraftSummary>(); readonly sectionAdded = output<string>();
  readonly sectionTitle = signal(''); readonly sectionReading = signal(''); readonly sectionErrors = signal<Record<string, string>>({}); readonly addingSection = signal(false);
  private readonly sectionTitleField = viewChild('sectionTitleField', { read: TextFieldComponent });
  readonly draft = signal<ModuleDraftResult | null>(null); readonly error = signal(''); readonly status = signal(''); readonly busy = signal(false);
  readonly title = signal(''); readonly summary = signal(''); readonly effort = signal(''); readonly steps = signal<string[]>([]); readonly prompts = signal<PromptDraft[]>([]);
  readonly revision = signal('');
  private readonly saved = signal('');
  readonly form = computed(() => JSON.stringify({ title: this.title(), summary: this.summary(), effort: this.effort(), steps: this.steps(), prompts: this.prompts().map(p => p.text) }));
  readonly dirty = computed(() => !!this.draft() && (this.form() !== this.saved() || this.sectionTitle() !== '' || this.sectionReading() !== ''));
  // Refusals keyed as the API keys them, and the aside that lists them in words.
  readonly errors = signal<Record<string, string>>({});
  readonly refused = computed(() => Object.entries(this.errors()).map(([field, message]) => `${this.describe(field)} — ${message}`));
  readonly conflict = signal<ModuleDraftResult | null>(null); readonly showCurrent = signal(false);
  readonly sessionExpired = signal(false);
  readonly unsaved = computed(() => {
    const draft = this.draft(); if (!draft) return [];
    const changed: string[] = [];
    if (this.title() !== draft.title) changed.push('title'); if (this.summary() !== draft.summary) changed.push('summary'); if (this.effort() !== draft.effortEstimate) changed.push('effort estimate');
    if (JSON.stringify(this.steps()) !== JSON.stringify(draft.practiceSteps)) changed.push('practice steps');
    if (JSON.stringify(this.prompts().map(p => p.text)) !== JSON.stringify(draft.prompts.map(p => p.text))) changed.push('prompts');
    return changed;
  });
  readonly unsavedSummary = computed(() => { const fields = this.unsaved(); return fields.length ? `${this.list(fields)} of this module ${fields.length === 1 && !fields[0].endsWith('s') ? 'has' : 'have'} been changed and not saved. If you leave now those changes are lost.` : ''; });
  readonly unsavedLine = computed(() => { const fields = this.unsaved(); return fields.length === 0 ? 'No unsaved changes.' : `${this.count(fields.length, 'field')} changed and not yet saved.`; });
  private readonly titleField = viewChild('titleField', { read: TextFieldComponent });
  private readonly summaryField = viewChild('summaryField', { read: TextAreaFieldComponent });
  private readonly effortField = viewChild('effortField', { read: TextFieldComponent });
  private readonly stepsEditor = viewChild(PracticeStepsEditorComponent);
  private readonly promptsEditor = viewChild(PromptsEditorComponent);
  ngOnInit() { void this.load(); }
  // Loading resets the form to what is stored; a reload after a prompt change keeps what was typed elsewhere in the form.
  async load(keepForm = false) {
    this.error.set('');
    try {
      const draft = await this.service.getModule(this.id());
      this.draft.set(draft); this.revision.set(draft.revision);
      const prompts = draft.prompts.map(p => ({ id: p.id, text: p.text, answerCount: p.answerCount, canRemove: p.canRemove }));
      if (!keepForm) { this.title.set(draft.title); this.summary.set(draft.summary); this.effort.set(draft.effortEstimate); this.steps.set([...draft.practiceSteps]); this.prompts.set(prompts); }
      else this.prompts.update(current => [...prompts.map(p => ({ ...p, text: current.find(c => c.id === p.id)?.text ?? p.text })), ...current.filter(c => !c.id)]);
      this.saved.set(JSON.stringify({ title: draft.title, summary: draft.summary, effort: draft.effortEstimate, steps: draft.practiceSteps, prompts: draft.prompts.map(p => p.text) }));
      this.loaded.emit(draft);
    } catch (e) { if (e instanceof ServiceError && e.status === 401 && !this.draft()) this.expired.emit(); else this.fail(e); }
  }
  async save() {
    const draft = this.draft(); if (!draft || this.busy()) return;
    this.busy.set(true); this.status.set(''); this.error.set(''); this.conflict.set(null); this.sessionExpired.set(false);
    const errors: Record<string, string> = {};
    this.prompts().forEach((p, i) => { if (!p.text.trim()) errors[`prompts[${i}]`] = 'A prompt is required.'; else if (p.text.length > draft.limits.prompt) errors[`prompts[${i}]`] = `A prompt may be at most ${draft.limits.prompt} characters. Shorten it by ${p.text.length - draft.limits.prompt}.`; });
    try {
      if (Object.keys(errors).length) throw new ValidationError(400, undefined, 'Check the highlighted fields.', Object.fromEntries(Object.entries(errors).map(([k, v]) => [k, [v]])));
      const { revision } = await this.service.reviseModule(draft.id, { title: this.title(), summary: this.summary(), effortEstimate: this.effort(), practiceSteps: this.steps(), revision: this.revision() });
      this.revision.set(revision);
      const order: string[] = [];
      for (const prompt of this.prompts()) {
        if (!prompt.id) order.push((await this.service.addPrompt(draft.id, prompt.text)).id);
        else { if (prompt.text !== draft.prompts.find(p => p.id === prompt.id)?.text) await this.service.revisePrompt(prompt.id, prompt.text); order.push(prompt.id); }
      }
      if (order.length > 1 && order.some((id, i) => draft.prompts[i]?.id !== id)) await this.service.reorderPrompts(draft.id, order);
      await this.load(); this.errors.set({}); this.status.set('Module saved.');
    } catch (e) {
      if (e instanceof ValidationError) {
        const refused: Record<string, string> = {};
        for (const key of Object.keys(e.errors)) refused[key === 'text' ? 'prompts[0]' : key] = e.errorFor(key);
        this.errors.set(refused);
        this.status.set(`${this.count(Object.keys(refused).length, 'field')} ${Object.keys(refused).length === 1 ? 'was' : 'were'} refused. Nothing was stored.`);
        setTimeout(() => this.focusFirstError());
      }
      else if (e instanceof ServiceError && e.status === 409) { this.error.set(e.message); try { this.conflict.set(await this.service.getModule(draft.id)); } catch { /* the refusal already states the change */ } }
      else if (e instanceof ServiceError && e.status === 401) this.sessionExpired.set(true);
      else this.fail(e);
    } finally { this.busy.set(false); }
  }
  // Every field returns to the values last saved, and the screen says so.
  discard() { void this.load(); this.errors.set({}); this.conflict.set(null); this.status.set('Changes discarded. Nothing is unsaved.'); }
  // The current content is adopted as the base; what was typed stays in the form to be applied again.
  adoptCurrent() {
    const current = this.conflict(); const previous = this.draft(); if (!current || !previous) return;
    // Fields left untouched take the current content; fields that were changed keep what was typed.
    if (this.title() === previous.title) this.title.set(current.title);
    if (this.summary() === previous.summary) this.summary.set(current.summary);
    if (this.effort() === previous.effortEstimate) this.effort.set(current.effortEstimate);
    if (JSON.stringify(this.steps()) === JSON.stringify(previous.practiceSteps)) this.steps.set([...current.practiceSteps]);
    this.draft.set(current); this.revision.set(current.revision);
    this.saved.set(JSON.stringify({ title: current.title, summary: current.summary, effort: current.effortEstimate, steps: current.practiceSteps, prompts: current.prompts.map(p => p.text) }));
    this.conflict.set(null); this.error.set(''); this.status.set('Reloaded. Your changes are still in the form; save to apply them.');
  }
  async remove() { await this.act(async () => { await this.service.removeModule(this.id()); this.removed.emit(); }); }
  // A new section is placed last; its editor opens at once. Refusals land on the fields of the add form.
  async addSection() {
    const draft = this.draft(); if (!draft || this.busy()) return;
    this.busy.set(true); this.status.set(''); this.error.set(''); this.sectionErrors.set({});
    try { const { id } = await this.service.addSection(draft.id, this.sectionTitle().trim(), this.sectionReading()); this.sectionTitle.set(''); this.sectionReading.set(''); this.sectionAdded.emit(id); }
    catch (e) { if (e instanceof ValidationError) this.sectionErrors.set({ title: e.errorFor('title'), reading: e.errorFor('reading') }); else this.fail(e); }
    finally { this.busy.set(false); }
  }
  startSection() { this.addingSection.set(true); setTimeout(() => this.sectionTitleField()?.focus()); }
  async moveSection(index: number, delta: number) {
    const draft = this.draft(); if (!draft || this.busy()) return;
    const sections = moveWithin(draft.sections, index, delta).map((s, i) => ({ ...s, ordinal: i + 1 }));
    const moved = draft.sections[index];
    this.draft.set({ ...draft, sections }); this.status.set(`${moved.title} moved to position ${index + delta + 1} of ${sections.length}.`); this.error.set('');
    this.busy.set(true);
    try { await this.service.reorderSections(draft.id, sections.map(s => s.id)); }
    catch (e) { this.draft.set(draft); this.status.set(''); this.fail(e); }
    finally { this.busy.set(false); }
  }
  async removeSection(section: SectionDraftSummary) { await this.act(async () => { await this.service.removeSection(section.id); await this.load(true); this.status.set('Section removed.'); }); }
  async removePrompt(prompt: PromptDraft) { await this.act(async () => { if (prompt.id) await this.service.removePrompt(prompt.id); await this.load(true); this.status.set('Prompt removed.'); }); }
  private async act(work: () => Promise<void>) {
    if (this.busy()) return;
    this.busy.set(true); this.status.set(''); this.error.set('');
    try { await work(); } catch (e) { this.fail(e); } finally { this.busy.set(false); }
  }
  private fail(e: unknown) { if (e instanceof ServiceError && e.status === 401) this.sessionExpired.set(true); else this.error.set(e instanceof Error ? e.message : 'The module could not be loaded.'); }
  fieldError(field: string) { return this.errors()[field] ?? ''; }
  // The module as a participant would read it, from the unsaved form: no completion, no answers, no notes.
  async loadPreview(): Promise<ModuleResult> {
    const draft = this.draft(); if (!draft) throw new Error('The module has not loaded yet.');
    const sections = await Promise.all(draft.sections.map(async s => { const section = await this.service.getSection(s.id); return { id: s.id, ordinal: s.ordinal, title: section.title, reading: section.reading, isComplete: false }; }));
    return { id: draft.id, ordinal: draft.ordinal, title: this.title(), summary: this.summary(), effortEstimate: this.effort(), practiceSteps: [...this.steps()], sections,
      preparationPrompts: this.prompts().map((p, i) => ({ id: p.id ?? `unsaved-${i}`, text: p.text, answer: null })), completedSections: 0, percent: 0, resumeSectionId: sections[0]?.id ?? '', isComplete: false, notes: [], notesCursor: null };
  }
  focusFirstError() {
    const keys = Object.keys(this.errors()); if (!keys.length) return;
    const order = ['title', 'summary', 'effortEstimate', ...this.steps().map((_, i) => `practiceSteps[${i}]`), ...this.prompts().map((_, i) => `prompts[${i}]`)];
    const first = order.find(k => keys.includes(k)) ?? keys[0];
    if (first === 'title') this.titleField()?.focus(); else if (first === 'summary') this.summaryField()?.focus(); else if (first === 'effortEstimate') this.effortField()?.focus();
    else if (first.startsWith('practiceSteps')) this.stepsEditor()?.focus(Number(first.slice(14, -1))); else if (first.startsWith('prompts')) this.promptsEditor()?.focus(Number(first.slice(8, -1)));
  }
  private describe(field: string) {
    if (field === 'effortEstimate') return 'Effort estimate';
    if (field.startsWith('practiceSteps')) return `Practice step ${Number(field.slice(14, -1)) + 1}`;
    if (field.startsWith('prompts')) return `Prompt ${Number(field.slice(8, -1)) + 1}`;
    return field.charAt(0).toUpperCase() + field.slice(1);
  }
  private list(items: string[]) { const named = items.map((f, i) => (i === 0 ? 'The ' : 'the ') + f); return named.length <= 1 ? named.join('') : `${named.slice(0, -1).join(', ')} and ${named.at(-1)}`; }
  ordinal(value: number) { return String(value).padStart(2, '0'); }
  count(value: number, noun: string) { return `${value} ${noun}${value === 1 ? '' : 's'}`; }
  completions(count: number) { return count === 0 ? 'no completions recorded' : `${count} participant${count === 1 ? '' : 's'} completed it`; }
}
