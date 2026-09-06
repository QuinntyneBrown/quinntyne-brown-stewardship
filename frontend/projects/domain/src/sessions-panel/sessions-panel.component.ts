import { Component, inject, input, output, signal, computed, OnInit, TemplateRef, contentChild, viewChild, ElementRef } from '@angular/core';
import { RetryNoticeComponent, StatusMessageComponent } from '@qbs/components';
import { ServiceError } from '@qbs/api';
import { RouterLink } from '@angular/router';
import { SESSION_SERVICE, AvailabilityResult, BookingResult } from '@qbs/api';
import { SlotPickerComponent } from '@qbs/components';
import { formatSession } from '../format-session';
@Component({
  selector: 'qbs-sessions-panel', imports: [RouterLink, RetryNoticeComponent, StatusMessageComponent, SlotPickerComponent], templateUrl: './sessions-panel.component.html', styleUrl: './sessions-panel.component.css'
})
export class SessionsPanelComponent implements OnInit {
  private readonly service = inject(SESSION_SERVICE);
  readonly expired = output<void>(); readonly requestCancel = output<BookingResult>();
  readonly data = signal<AvailabilityResult | null>(null); readonly selected = signal(''); readonly changing = signal(false);
  readonly error = signal(''); readonly busy = signal(false); readonly loading = signal(false); readonly message = signal('');
  readonly days = computed(() => this.data()?.days.map(id => ({ id, label: new Intl.DateTimeFormat('en-CA', { weekday: 'short', month: 'short', day: 'numeric', timeZone: 'UTC' }).format(new Date(id + 'T12:00:00Z')), disabled: id < this.data()!.cohort.startDate || id >= this.data()!.cohort.endDate })) ?? []);
  readonly slots = computed(() => this.data()?.slots.map(s => ({ id: s.id, label: this.date(s.startsAt, this.data()!.cohort.timeZone), state: s.state })) ?? []);
  ngOnInit() { void this.load(); }
  date(value: string, zone: string) { return formatSession(value, zone); }
  async load(day?: string, keepError = false) {
    if (!keepError) this.error.set(''); this.loading.set(true); this.selected.set('');
    try { this.data.set(await this.service.availability(day)); }
    catch (e) { this.fail(e); } finally { this.loading.set(false); }
  }
  fail(e: unknown) { if (e instanceof ServiceError && e.status === 401) this.expired.emit(); else this.error.set(e instanceof Error ? e.message : 'The session request failed. Please try again.'); }
  week(offset: number) { const data = this.data(); if (!data) return; const d = new Date(data.weekStart + 'T12:00:00Z'); d.setUTCDate(d.getUTCDate() + offset * 7); const day = d.toISOString().slice(0, 10); void this.load(day < data.cohort.startDate ? data.cohort.startDate : day); }
  previousWeek() { const d = this.data(); return !!d && d.weekStart > d.cohort.startDate; }
  nextWeek() { const d = this.data(); if (!d) return false; const date = new Date(d.weekStart + 'T12:00:00Z'); date.setUTCDate(date.getUTCDate() + 7); return date.toISOString().slice(0, 10) < d.cohort.endDate; }
  startChange() { this.changing.set(true); this.selected.set(''); this.message.set('Choose a new time for your session.'); }
  async confirm() {
    if (!this.selected() || this.busy()) return; this.busy.set(true); this.error.set('');
    try { const data = this.data()!; if (this.changing() && data.nextSession) await this.service.reschedule(data.nextSession.id, this.selected()); else await this.service.book(this.selected()); this.changing.set(false); this.message.set('Your session is confirmed.'); await this.load(data.selectedDay); }
    catch (e) { this.fail(e); if (e instanceof ServiceError && e.status === 409) await this.load(this.data()?.selectedDay, true); }
    finally { this.busy.set(false); }
  }
  async cancelConfirmed(id: string) {
    this.busy.set(true); this.error.set('');
    try { await this.service.cancel(id); this.changing.set(false); this.message.set('Your session was cancelled. The slot is available again.'); await this.load(this.data()?.selectedDay); }
    catch (e) { this.fail(e); } finally { this.busy.set(false); }
  }
}
