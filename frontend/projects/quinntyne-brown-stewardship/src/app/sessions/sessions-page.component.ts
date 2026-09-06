import { Component, inject, input, output, signal, computed, OnInit, TemplateRef, contentChild, viewChild, ElementRef } from '@angular/core';
import { Router } from '@angular/router';
import { SessionsPanelComponent } from '@qbs/domain';
import { BookingResult } from '@qbs/api';
import { redirectToSignIn } from '../redirect-to-sign-in';
@Component({
  selector: 'qbs-sessions-page', imports: [SessionsPanelComponent], templateUrl: './sessions-page.component.html', styleUrl: './sessions-page.component.css'
})
export class SessionsPageComponent {
  private readonly router = inject(Router); readonly panel = viewChild.required(SessionsPanelComponent); readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('cancelDialog');
  readonly cancelling = signal<string | null>(null);
  expired() { return redirectToSignIn(this.router); }
  requestCancel(session: BookingResult) { this.cancelling.set(session.id); this.dialog().nativeElement.showModal(); }
  close() { this.dialog().nativeElement.close(); this.cancelling.set(null); }
  async confirmCancel() { const id = this.cancelling(); if (!id) return; await this.panel().cancelConfirmed(id); this.close(); }
}
