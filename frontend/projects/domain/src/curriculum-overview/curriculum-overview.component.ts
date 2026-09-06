import { Component, inject, input, output, signal, computed, OnInit, TemplateRef, contentChild, viewChild, ElementRef } from '@angular/core';
import { RetryNoticeComponent, StatusMessageComponent } from '@qbs/components';
import { ServiceError } from '@qbs/api';
import { RouterLink } from '@angular/router';
import { CURRICULUM_SERVICE, CurriculumResult } from '@qbs/api';
import { ProgressSummaryComponent, ModulePathComponent } from '@qbs/components';
import { formatSession } from '../format-session';
@Component({
  selector: 'qbs-curriculum-overview', imports: [RouterLink, RetryNoticeComponent, StatusMessageComponent, ProgressSummaryComponent, ModulePathComponent], templateUrl: './curriculum-overview.component.html', styleUrl: './curriculum-overview.component.css'
})
export class CurriculumOverviewComponent implements OnInit {
  private readonly service = inject(CURRICULUM_SERVICE);
  readonly expired = output<void>(); readonly notice = input('');
  readonly data = signal<CurriculumResult | null>(null); readonly error = signal('');
  readonly current = computed(() => this.data()?.modules.find(m => m.ordinal === this.data()?.currentOrdinal));
  ngOnInit() { void this.load(); }
  async load() { this.error.set(''); try { this.data.set(await this.service.getCurriculum()); } catch (e) { if (e instanceof ServiceError && e.status === 401) this.expired.emit(); else this.error.set(e instanceof Error ? e.message : 'We could not load your curriculum.'); } }
  date(value: string, zone: string) { return formatSession(value, zone); }
}
