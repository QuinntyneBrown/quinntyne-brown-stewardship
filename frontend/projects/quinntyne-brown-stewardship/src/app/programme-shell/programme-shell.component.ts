import { Component, inject, input, output, signal, computed, OnInit, TemplateRef, contentChild, viewChild, ElementRef } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { EnrollmentGateComponent } from '@qbs/domain';
import { ProgrammeHeaderComponent, SkipLinkComponent, ErrorMessageComponent } from '@qbs/components';
import { SIGN_IN_SERVICE } from '@qbs/api';
import { redirectToSignIn } from '../redirect-to-sign-in';
@Component({
  selector: 'qbs-programme-shell', imports: [RouterOutlet, EnrollmentGateComponent, ProgrammeHeaderComponent, SkipLinkComponent, ErrorMessageComponent], templateUrl: './programme-shell.component.html', styleUrl: './programme-shell.component.css'
})
export class ProgrammeShellComponent {
  private readonly router = inject(Router); private readonly signIn = inject(SIGN_IN_SERVICE);
  readonly outlet = viewChild(RouterOutlet);
  readonly links = [{ path: '/curriculum', label: 'Curriculum' }, { path: '/sessions', label: 'Sessions' }, { path: '/notes', label: 'Notes' }];
  readonly pending = signal(false); readonly error = signal('');
  description() { return this.router.url.startsWith('/sessions') ? 'Sessions are booked with the mentor your cohort assigns, so there is nothing to book yet.' : this.router.url.startsWith('/notes') ? 'Notes belong to your modules and sessions, so there are no notes to write yet.' : 'Until then there is no curriculum to read.'; }
  expired() { return redirectToSignIn(this.router); }
  async signOut() {
    if (this.pending()) return;
    const active = this.outlet()?.isActivated ? this.outlet()?.component as { canLeave?: () => boolean } : null;
    if (active?.canLeave && !active.canLeave()) return;
    this.pending.set(true); this.error.set('');
    try { await this.signIn.signOut(); await this.router.navigateByUrl('/sign-in', { replaceUrl: true }); }
    catch { this.error.set('We could not sign you out. Please try again.'); }
    finally { this.pending.set(false); }
  }
}
