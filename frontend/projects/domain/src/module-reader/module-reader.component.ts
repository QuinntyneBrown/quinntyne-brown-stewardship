import { Component, inject, input, output, signal, computed, OnInit, viewChild, ElementRef } from '@angular/core';
import { RetryNoticeComponent, StatusMessageComponent } from '@qbs/components';
import { ServiceError } from '@qbs/api';
import { RouterLink } from '@angular/router';
import { CURRICULUM_SERVICE, ModuleResult } from '@qbs/api';
import { ProgressSummaryComponent } from '@qbs/components';
import { NoteCollectionComponent } from '../note-collection/note-collection.component';
@Component({
  selector: 'qbs-module-reader', imports: [RouterLink, RetryNoticeComponent, StatusMessageComponent, ProgressSummaryComponent, NoteCollectionComponent], templateUrl: './module-reader.component.html', styleUrl: './module-reader.component.css'
})
export class ModuleReaderComponent implements OnInit {
  private readonly service = inject(CURRICULUM_SERVICE);
  readonly ordinal = input<number | null>(null);
  // A module supplied by an author's preview is rendered as given, with no request and no completion action.
  readonly preview = input<ModuleResult | null>(null); readonly expired = output<void>(); readonly completed = output<void>(); readonly locked = output<string>();
  readonly data = signal<ModuleResult | null>(null); readonly error = signal(''); readonly saving = signal(false); readonly selected = signal('');
  readonly section = computed(() => this.data()?.sections.find(s => s.id === this.selected()));
  readonly heading = viewChild<ElementRef<HTMLElement>>('sectionHeading');
  ngOnInit() { const preview = this.preview(); if (preview) { this.data.set(preview); this.selected.set(preview.resumeSectionId); } else void this.load(); }
  async load() {
    this.error.set('');
    try { const module = await this.service.getModule(this.ordinal()); this.data.set(module); this.selected.set(module.resumeSectionId); }
    catch (e) { if (e instanceof ServiceError && e.status === 401) this.expired.emit(); else if (e instanceof ServiceError && e.status === 409) this.locked.emit(e.message); else if (e instanceof ServiceError && e.status === 404) this.locked.emit('That module is not available. Your curriculum shows what is open to you.'); else this.error.set(e instanceof Error ? e.message : 'We could not load this module.'); }
  }
  // Reading content is one string; a blank line starts a new paragraph. The preview renders by the same rule.
  paragraphs(reading: string) { return reading.split(/\r?\n\r?\n/); }
  choose(id: string) { this.selected.set(id); setTimeout(() => this.heading()?.nativeElement.focus()); }
  async complete() {
    const section = this.section(); if (!section || section.isComplete || this.saving()) return;
    this.saving.set(true); this.error.set('');
    try { const result = await this.service.completeSection(section.id); if (result.isModuleComplete) this.completed.emit(); else { await this.load(); setTimeout(() => this.heading()?.nativeElement.focus()); } }
    catch (e) { if (e instanceof ServiceError && e.status === 401) this.expired.emit(); else this.error.set(e instanceof Error ? e.message : 'We could not save your progress.'); }
    finally { this.saving.set(false); }
  }
}

