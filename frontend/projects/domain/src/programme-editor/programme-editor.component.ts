import { Component, computed, inject, input, output, signal, viewChild, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RetryNoticeComponent, StatusMessageComponent, StatePillComponent, EmptyStateComponent, TextFieldComponent, TextAreaFieldComponent, OrderControlComponent } from '@qbs/components';
import { moveWithin } from '../move-within';
import { AUTHORING_SERVICE, CurriculumDraftResult, ServiceError, ValidationError } from '@qbs/api';
import { PublishPanelComponent } from '../publish-panel/publish-panel.component';
// The programme's own record: its title and key are saved separately, because only the key is guarded by the cohorts following it.
@Component({
  selector: 'qbs-programme-editor', imports: [RouterLink, RetryNoticeComponent, StatusMessageComponent, StatePillComponent, EmptyStateComponent, TextFieldComponent, TextAreaFieldComponent, OrderControlComponent, PublishPanelComponent], templateUrl: './programme-editor.component.html', styleUrl: './programme-editor.component.css'
})
export class ProgrammeEditorComponent implements OnInit {
  private readonly service = inject(AUTHORING_SERVICE);
  readonly id = input.required<string>();
  readonly expired = output<void>(); readonly loaded = output<CurriculumDraftResult>(); readonly removeRequested = output<void>(); readonly removed = output<void>(); readonly moduleAdded = output<string>();
  readonly moduleTitle = signal(''); readonly moduleSummary = signal(''); readonly moduleTitleError = signal(''); readonly moduleSummaryError = signal('');
  private readonly moduleTitleField = viewChild('moduleTitleField', { read: TextFieldComponent });
  readonly draft = signal<CurriculumDraftResult | null>(null); readonly error = signal(''); readonly status = signal(''); readonly busy = signal(false);
  readonly title = signal(''); readonly key = signal(''); readonly titleError = signal(''); readonly keyError = signal('');
  readonly dirty = computed(() => { const draft = this.draft(); return !!draft && (this.title() !== draft.title || this.key() !== draft.key || this.moduleTitle() !== '' || this.moduleSummary() !== ''); });
  readonly meta = computed(() => { const draft = this.draft(); return draft ? `${draft.key} · created ${this.day(draft.createdAt)} · ${this.cohorts(draft.cohortCount)}` : ''; });
  readonly shape = computed(() => { const draft = this.draft(); if (!draft) return ''; const sections = draft.modules.reduce((sum, m) => sum + m.sectionCount, 0); return `${this.count(draft.modules.length, 'module')} · ${draft.modules.filter(m => m.state === 'Published').length} published · ${this.count(sections, 'section')}`; });
  readonly removalReason = computed(() => { const draft = this.draft(); return draft && !draft.canRemove ? `${this.count(draft.cohortCount, 'cohort')} follow this programme. It cannot be removed while ${draft.cohortCount === 1 ? 'it does' : 'they do'}.` : ''; });
  ngOnInit() { void this.load(); }
  async load() {
    this.error.set('');
    try { const draft = await this.service.getProgramme(this.id()); this.draft.set(draft); this.title.set(draft.title); this.key.set(draft.key); this.loaded.emit(draft); }
    catch (e) { this.fail(e); }
  }
  async saveTitle() { await this.write(async () => { this.titleError.set(''); await this.service.renameProgramme(this.id(), this.title().trim()); this.status.set('Title saved.'); }, e => this.titleError.set(e.errorFor('title'))); }
  async saveKey() { await this.write(async () => { this.keyError.set(''); await this.service.changeKey(this.id(), this.key().trim()); this.status.set('Key saved.'); }, e => this.keyError.set(e.errorFor('key')), e => this.keyError.set(e.message)); }
  // A new module takes the last position in draft; the editor for it opens at once.
  async addModule() {
    await this.write(async () => {
      this.moduleTitleError.set(''); this.moduleSummaryError.set('');
      const { id } = await this.service.addModule(this.id(), this.moduleTitle().trim(), this.moduleSummary().trim());
      this.moduleTitle.set(''); this.moduleSummary.set(''); this.moduleAdded.emit(id);
    }, e => { this.moduleTitleError.set(e.errorFor('title')); this.moduleSummaryError.set(e.errorFor('summary')); }, undefined, false);
  }
  focusAddModule() { this.moduleTitleField()?.focus(); }
  // The move shows at once and is announced; a refusal puts the stored order back and says why.
  async move(index: number, delta: number) {
    const draft = this.draft(); if (!draft || this.busy()) return;
    const modules = moveWithin(draft.modules, index, delta).map((m, i) => ({ ...m, ordinal: i + 1 }));
    const moved = draft.modules[index];
    this.draft.set({ ...draft, modules }); this.status.set(`${moved.title} moved to position ${index + delta + 1} of ${modules.length}.`); this.error.set('');
    this.busy.set(true);
    try { await this.service.reorderModules(draft.id, modules.map(m => m.id)); }
    catch (e) { this.draft.set(draft); this.status.set(''); if (e instanceof ServiceError && e.status === 401) this.expired.emit(); else this.error.set(e instanceof Error ? e.message : 'The order could not be saved.'); }
    finally { this.busy.set(false); }
  }
  // The page owns the dialog; it asks before calling remove, so the consequence is stated before the record goes.
  async remove() { await this.write(async () => { await this.service.removeProgramme(this.id()); this.removed.emit(); }, undefined, undefined, false); }
  private async write(act: () => Promise<void>, refused?: (e: ValidationError) => void, conflicted?: (e: ServiceError) => void, reload = true) {
    if (this.busy()) return;
    this.busy.set(true); this.status.set(''); this.error.set('');
    try { await act(); if (reload) await this.load(); }
    catch (e) {
      if (e instanceof ValidationError && refused) refused(e);
      else if (e instanceof ServiceError && e.status === 409 && conflicted) conflicted(e);
      else this.fail(e);
    } finally { this.busy.set(false); }
  }
  private fail(e: unknown) { if (e instanceof ServiceError && e.status === 401) this.expired.emit(); else this.error.set(e instanceof Error ? e.message : 'The programme could not be loaded.'); }
  ordinal(value: number) { return String(value).padStart(2, '0'); }
  count(value: number, noun: string) { return `${value} ${noun}${value === 1 ? '' : 's'}`; }
  private cohorts(value: number) { return value === 0 ? 'no cohort follows it yet' : value === 1 ? '1 cohort follows it' : `${value} cohorts follow it`; }
  private day(value: string) { return new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'long' }).format(new Date(value)); }
}
