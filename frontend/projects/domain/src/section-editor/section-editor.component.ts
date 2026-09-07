import { Component, computed, inject, input, output, signal, viewChild, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ErrorMessageComponent, RetryNoticeComponent, StatusMessageComponent, TextFieldComponent, TextAreaFieldComponent } from '@qbs/components';
import { AUTHORING_SERVICE, SectionDraftResult, ServiceError, ValidationError } from '@qbs/api';
// The reading a participant does: one title and one long text, typed as plain text and shown exactly as typed.
@Component({
  selector: 'qbs-section-editor', imports: [RouterLink, ErrorMessageComponent, RetryNoticeComponent, StatusMessageComponent, TextFieldComponent, TextAreaFieldComponent], templateUrl: './section-editor.component.html', styleUrl: './section-editor.component.css'
})
export class SectionEditorComponent implements OnInit {
  private readonly service = inject(AUTHORING_SERVICE);
  readonly id = input.required<string>();
  readonly expired = output<void>(); readonly loaded = output<SectionDraftResult>(); readonly removeRequested = output<void>(); readonly removed = output<void>();
  readonly draft = signal<SectionDraftResult | null>(null); readonly error = signal(''); readonly status = signal(''); readonly busy = signal(false);
  readonly title = signal(''); readonly reading = signal(''); readonly revision = signal('');
  readonly errors = signal<Record<string, string>>({});
  readonly refused = computed(() => Object.entries(this.errors()).map(([field, message]) => `${field.charAt(0).toUpperCase() + field.slice(1)} — ${message}`));
  readonly conflict = signal<SectionDraftResult | null>(null); readonly showCurrent = signal(false);
  readonly sessionExpired = signal(false);
  readonly unsaved = computed(() => { const draft = this.draft(); const changed: string[] = []; if (draft && this.title() !== draft.title) changed.push('title'); if (draft && this.reading() !== draft.reading) changed.push('reading content'); return changed; });
  readonly dirty = computed(() => this.unsaved().length > 0);
  readonly unsavedSummary = computed(() => { const fields = this.unsaved(); return fields.length ? `The ${fields.join(' and the ')} of this section ${fields.length === 1 ? 'has' : 'have'} been changed and not saved. If you leave now those changes are lost.` : ''; });
  readonly recorded = computed(() => { const n = this.draft()?.completionCount ?? 0; return n === 0 ? 'No completions recorded' : `${n} completion${n === 1 ? '' : 's'}`; });
  private readonly titleField = viewChild('titleField', { read: TextFieldComponent });
  private readonly readingField = viewChild('readingField', { read: TextAreaFieldComponent });
  ngOnInit() { void this.load(); }
  async load() {
    this.error.set('');
    try {
      const draft = await this.service.getSection(this.id());
      this.draft.set(draft); this.revision.set(draft.revision); this.title.set(draft.title); this.reading.set(draft.reading); this.loaded.emit(draft);
    } catch (e) { if (e instanceof ServiceError && e.status === 401 && !this.draft()) this.expired.emit(); else this.fail(e); }
  }
  async save() {
    const draft = this.draft(); if (!draft || this.busy()) return;
    this.busy.set(true); this.status.set(''); this.error.set(''); this.conflict.set(null); this.sessionExpired.set(false);
    try {
      await this.service.reviseSection(draft.id, { title: this.title(), reading: this.reading(), revision: this.revision() });
      await this.load(); this.errors.set({}); this.status.set('Section saved.');
    } catch (e) {
      if (e instanceof ValidationError) {
        const refused: Record<string, string> = {}; for (const key of Object.keys(e.errors)) refused[key] = e.errorFor(key);
        this.errors.set(refused);
        this.status.set(`${Object.keys(refused).length} field${Object.keys(refused).length === 1 ? ' was' : 's were'} refused. Nothing was stored.`);
        setTimeout(() => (refused['title'] ? this.titleField() : this.readingField())?.focus());
      }
      else if (e instanceof ServiceError && e.status === 409) { this.error.set(e.message); try { this.conflict.set(await this.service.getSection(draft.id)); } catch { /* the refusal already states the change */ } }
      else if (e instanceof ServiceError && e.status === 401) this.sessionExpired.set(true);
      else this.fail(e);
    } finally { this.busy.set(false); }
  }
  discard() { void this.load(); this.errors.set({}); this.conflict.set(null); this.status.set('Changes discarded. Nothing is unsaved.'); }
  adoptCurrent() {
    const current = this.conflict(); const previous = this.draft(); if (!current || !previous) return;
    if (this.title() === previous.title) this.title.set(current.title);
    if (this.reading() === previous.reading) this.reading.set(current.reading);
    this.draft.set(current); this.revision.set(current.revision); this.conflict.set(null); this.error.set(''); this.status.set('Reloaded. Your changes are still in the form; save to apply them.');
  }
  async remove() {
    if (this.busy()) return;
    this.busy.set(true); this.error.set('');
    try { await this.service.removeSection(this.id()); this.removed.emit(); } catch (e) { this.fail(e); } finally { this.busy.set(false); }
  }
  private fail(e: unknown) { if (e instanceof ServiceError && e.status === 401) this.sessionExpired.set(true); else this.error.set(e instanceof Error ? e.message : 'The section could not be loaded.'); }
  fieldError(field: string) { return this.errors()[field] ?? ''; }
}
