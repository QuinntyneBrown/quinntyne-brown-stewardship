import { Component, inject, input, output, signal, computed, OnInit, TemplateRef, contentChild, viewChild, ElementRef } from '@angular/core';
import { RetryNoticeComponent, StatusMessageComponent } from '@qbs/components';
import { ServiceError } from '@qbs/api';
import { NgTemplateOutlet } from '@angular/common';
import { COHORT_SERVICE } from '@qbs/api';
import { NotEnrolledNoticeComponent } from '@qbs/components';
@Component({
  selector: 'qbs-enrollment-gate', imports: [NgTemplateOutlet, NotEnrolledNoticeComponent, RetryNoticeComponent, StatusMessageComponent], templateUrl: './enrollment-gate.component.html', styleUrl: './enrollment-gate.component.css'
})
export class EnrollmentGateComponent implements OnInit {
  private readonly service = inject(COHORT_SERVICE);
  readonly description = input.required<string>();
  readonly content = contentChild.required(TemplateRef);
  readonly expired = output<void>();
  readonly status = signal('loading');
  ngOnInit() { void this.load(); }
  async load() {
    this.status.set('loading');
    try { this.status.set((await this.service.getEnrollment()).isEnrolled ? 'ready' : 'absent'); }
    catch (error) { if (error instanceof ServiceError && error.status === 401) this.expired.emit(); else this.status.set('error'); }
  }
}
