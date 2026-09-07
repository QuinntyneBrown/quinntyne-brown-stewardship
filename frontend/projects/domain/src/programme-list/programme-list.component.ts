import { Component, inject, output, signal, OnInit, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RetryNoticeComponent, StatusMessageComponent, StatePillComponent, EmptyStateComponent, ProgrammeFormComponent } from '@qbs/components';
import { AUTHORING_SERVICE, CurriculaResult, ServiceError, ValidationError } from '@qbs/api';
@Component({
  selector: 'qbs-programme-list', imports: [RouterLink, RetryNoticeComponent, StatusMessageComponent, StatePillComponent, EmptyStateComponent, ProgrammeFormComponent], templateUrl: './programme-list.component.html', styleUrl: './programme-list.component.css'
})
export class ProgrammeListComponent implements OnInit {
  private readonly service = inject(AUTHORING_SERVICE);
  readonly expired = output<void>();
  readonly created = output<string>();
  readonly data = signal<CurriculaResult | null>(null); readonly error = signal('');
  readonly key = signal(''); readonly title = signal(''); readonly keyError = signal(''); readonly titleError = signal(''); readonly creating = signal(false);
  private readonly form = viewChild(ProgrammeFormComponent);
  ngOnInit() { void this.load(); }
  async load() {
    this.error.set('');
    try { this.data.set(await this.service.getProgrammes()); }
    catch (e) { if (e instanceof ServiceError && e.status === 401) this.expired.emit(); else this.error.set(e instanceof Error ? e.message : 'We could not load the programmes.'); }
  }
  // A refused creation stores nothing: the list is as it was, and the typed values stay to correct.
  async create(values: { key: string; title: string }) {
    this.creating.set(true); this.keyError.set(''); this.titleError.set('');
    try { const { id } = await this.service.createProgramme(values.key, values.title); this.created.emit(id); }
    catch (e) {
      if (e instanceof ServiceError && e.status === 401) { this.expired.emit(); return; }
      if (e instanceof ValidationError) { this.keyError.set(e.errorFor('key')); this.titleError.set(e.errorFor('title')); }
      else if (e instanceof ServiceError && e.status === 409) this.keyError.set(e.message);
      else this.error.set(e instanceof Error ? e.message : 'We could not create the programme.');
      this.form()?.focus(this.keyError() || !this.titleError() ? 'key' : 'title');
    } finally { this.creating.set(false); }
  }
  cohorts(count: number) { return count === 0 ? 'no cohort yet' : count === 1 ? '1 cohort follows it' : `${count} cohorts follow it`; }
}
