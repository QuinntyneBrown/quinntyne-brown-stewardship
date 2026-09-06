import { AvailabilityResult } from "./availability-result";
import { HistoryResult } from "./history-result";
import { BookingResult } from "./booking-result";
import { PreparationResult } from "./preparation-result";
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ISessionService } from './session-service.contract';
import { programmeRequest } from './programme-request';
@Injectable()
export class SessionService implements ISessionService {
  private readonly http = inject(HttpClient);
  availability(day?: string): Promise<AvailabilityResult> { return programmeRequest<AvailabilityResult>(this.http, 'GET', '/sessions/availability' + (day ? '?day=' + encodeURIComponent(day) : '')); }
  history(): Promise<HistoryResult> { return programmeRequest<HistoryResult>(this.http, 'GET', '/sessions/history'); }
  get(id: string): Promise<BookingResult> { return programmeRequest<BookingResult>(this.http, 'GET', `/sessions/${encodeURIComponent(id)}`); }
  preparation(id: string): Promise<PreparationResult> { return programmeRequest<PreparationResult>(this.http, 'GET', `/sessions/${encodeURIComponent(id)}/preparation`); }
  book(slotId: string): Promise<BookingResult> { return programmeRequest<BookingResult>(this.http, 'POST', '/sessions', { slotId }); }
  reschedule(id: string, slotId: string): Promise<BookingResult> { return programmeRequest<BookingResult>(this.http, 'PUT', `/sessions/${encodeURIComponent(id)}/slot`, { slotId }); }
  cancel(id: string): Promise<void> { return programmeRequest<void>(this.http, 'DELETE', `/sessions/${encodeURIComponent(id)}`, {}); }
}
