import { Component, inject, input, output, signal, computed, OnInit, TemplateRef, contentChild, viewChild, ElementRef } from '@angular/core';
import { RetryNoticeComponent, StatusMessageComponent } from '@qbs/components';
import { ServiceError } from '@qbs/api';
import { RouterLink } from '@angular/router';
import { SESSION_SERVICE, CURRICULUM_SERVICE, PreparationResult } from '@qbs/api';
import { formatSession } from '../format-session';
@Component({
  selector: 'qbs-session-preparation', imports: [RouterLink, RetryNoticeComponent, StatusMessageComponent], templateUrl: './session-preparation.component.html', styleUrl: './session-preparation.component.css'
})
export class SessionPreparationComponent implements OnInit {
  private readonly service = inject(SESSION_SERVICE); private readonly curriculum = inject(CURRICULUM_SERVICE);
  readonly id = input.required<string>(); readonly expired = output<void>();
  readonly data = signal<PreparationResult | null>(null); readonly moduleId = signal(''); readonly error = signal('');
  ngOnInit() { void this.load(); }
  date(value: string, zone: string) { return formatSession(value, zone); }
  async load() { this.error.set(''); try { const data = await this.service.preparation(this.id()); this.data.set(data); this.moduleId.set(data.moduleId ?? ''); } catch(e) { if(e instanceof ServiceError && e.status === 401) this.expired.emit(); else this.error.set(e instanceof Error ? e.message : 'We could not load this session.'); } }
}
