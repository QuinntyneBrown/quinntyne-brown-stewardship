import { Component, computed, inject, signal, viewChild, DestroyRef } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { EnrollmentGateComponent } from '@qbs/domain';
import { NavigationLink, ProgrammeHeaderComponent, SkipLinkComponent, ErrorMessageComponent } from '@qbs/components';
import { SIGN_IN_SERVICE } from '@qbs/api';
import { redirectToSignIn } from '../redirect-to-sign-in';
const participantLinks: NavigationLink[] = [{ path: '/curriculum', label: 'Curriculum' }, { path: '/sessions', label: 'Sessions' }, { path: '/notes', label: 'Notes' }];
@Component({
  selector: 'qbs-programme-shell', imports: [RouterOutlet, EnrollmentGateComponent, ProgrammeHeaderComponent, SkipLinkComponent, ErrorMessageComponent], templateUrl: './programme-shell.component.html', styleUrl: './programme-shell.component.css'
})
export class ProgrammeShellComponent {
  private readonly router = inject(Router); private readonly route = inject(ActivatedRoute); private readonly signIn = inject(SIGN_IN_SERVICE);
  readonly outlet = viewChild(RouterOutlet);
  // The authoring destination is offered only when the session reports administrator authority; hiding it is presentation, the API check is the guarantee.
  readonly administrator = signal(false);
  readonly links = computed<NavigationLink[]>(() => this.administrator() ? [...participantLinks, { path: '/admin/programmes', label: 'Authoring' }] : participantLinks);
  readonly gated = signal(true);
  readonly pending = signal(false); readonly error = signal('');
  constructor() {
    this.updateGated();
    const subscription = this.router.events.subscribe(event => { if (event instanceof NavigationEnd) this.updateGated(); });
    inject(DestroyRef).onDestroy(() => subscription.unsubscribe());
    void this.signIn.session().then(session => this.administrator.set(!!session?.isAdministrator)).catch(() => this.administrator.set(false));
  }
  // Route data on the deepest activated route says whether the screen belongs to a cohort.
  // The gate steps aside for any screen under a route that says so, its children included; a child of a component route does not inherit data on its own.
  private updateGated() { let gated = true; for (let current: typeof this.route.snapshot | null = this.route.snapshot; current; current = current.firstChild) if (current.data['gated'] === false) gated = false; this.gated.set(gated); }
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
