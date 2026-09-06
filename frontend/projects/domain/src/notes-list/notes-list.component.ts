import { Component, inject, input, output, signal, computed, OnInit, TemplateRef, contentChild, viewChild, ElementRef } from '@angular/core';
import { RetryNoticeComponent, StatusMessageComponent } from '@qbs/components';
import { ServiceError } from '@qbs/api';
import { RouterLink } from '@angular/router';
import { NOTE_SERVICE, NotesResult } from '@qbs/api';
@Component({
  selector: 'qbs-notes-list', imports: [RouterLink, RetryNoticeComponent, StatusMessageComponent], templateUrl: './notes-list.component.html', styleUrl: './notes-list.component.css'
})
export class NotesListComponent implements OnInit {
  private readonly service = inject(NOTE_SERVICE);
  readonly moduleId = input<string | undefined>(); readonly sessionId = input<string | undefined>(); readonly expired = output<void>();
  readonly data = signal<NotesResult | null>(null); readonly error = signal('');
  ngOnInit() { void this.load(); }
  date(value: string) { return new Intl.DateTimeFormat('en-CA', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)); }
  async load() { this.error.set(''); try { this.data.set(await this.service.list(this.moduleId(), this.sessionId())); } catch(e) { if(e instanceof ServiceError && e.status === 401) this.expired.emit(); else this.error.set(e instanceof Error ? e.message : 'We could not load your notes.'); } }
}
