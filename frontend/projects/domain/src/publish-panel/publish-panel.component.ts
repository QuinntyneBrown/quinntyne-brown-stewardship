import { Component, computed, inject, input, output, signal } from '@angular/core';
import { ErrorMessageComponent, StatusMessageComponent, StatePillComponent } from '@qbs/components';
import { AUTHORING_SERVICE, CurriculumDraftResult, ServiceError } from '@qbs/api';
// States, before the act, whether the programme can be published, whom that reaches, and what it would move; then performs it.
@Component({
  selector: 'qbs-publish-panel', imports: [ErrorMessageComponent, StatusMessageComponent, StatePillComponent], templateUrl: './publish-panel.component.html', styleUrl: './publish-panel.component.css'
})
export class PublishPanelComponent {
  private readonly service = inject(AUTHORING_SERVICE);
  readonly draft = input.required<CurriculumDraftResult>();
  readonly published = output<void>(); readonly expired = output<void>();
  readonly busy = signal(false); readonly error = signal(''); readonly status = signal('');
  readonly published_ = computed(() => this.draft().modules.filter(m => m.state === 'Published'));
  readonly sections = computed(() => this.published_().reduce((sum, m) => sum + m.sectionCount, 0));
  readonly prompts = computed(() => this.published_().reduce((sum, m) => sum + m.promptCount, 0));
  readonly lastPublished = computed(() => { const at = this.draft().publishedAt; return at ? `Last published ${this.when(at)}` : 'Never published'; });
  readonly readiness = computed(() => this.draft().readiness.reason ?? 'Ready to publish. Every module carries at least one section.');
  readonly action = computed(() => this.draft().publishedAt ? 'Publish again' : 'Publish programme');
  readonly reach = computed(() => {
    const { activeCohortCount, unpublishedModuleCount } = this.draft().readiness;
    if (activeCohortCount === 0) return 'No cohort follows this programme, so nothing a participant reads could have changed either way.';
    const reads = `${this.count(activeCohortCount, 'active cohort')} follow${activeCohortCount === 1 ? 's' : ''} this programme. Publishing changes what ${activeCohortCount === 1 ? 'it reads' : 'they read'} on ${activeCohortCount === 1 ? 'its' : 'their'} next request.`;
    return this.draft().publishedAt && unpublishedModuleCount === 0 ? `${reads} Nothing has been added since the last publication.` : reads;
  });
  readonly pending = computed(() => { const n = this.draft().readiness.unpublishedModuleCount; return this.draft().publishedAt && n > 0 ? `${this.count(n, 'module')} ${n === 1 ? 'has' : 'have'} been added since the last publication and ${n === 1 ? 'is' : 'are'} not visible to any participant.` : ''; });
  readonly movedBack = computed(() => { const { participantsMovedBack, movedBackToOrdinal } = this.draft().readiness; return participantsMovedBack > 0 ? `${this.count(participantsMovedBack, 'participant')} would be moved back to module ${String(movedBackToOrdinal).padStart(2, '0')}, because a module below their current one becomes readable for the first time.` : ''; });
  async publish() {
    if (this.busy()) return;
    this.busy.set(true); this.error.set(''); this.status.set('');
    try { const result = await this.service.publish(this.draft().id); this.status.set(`Programme published. ${this.count(result.publishedModuleCount, 'module')} now reach every cohort following it.`); this.published.emit(); }
    catch (e) {
      if (e instanceof ServiceError && e.status === 401) this.expired.emit();
      else if (e instanceof ServiceError && e.status === 409) this.error.set(`Publication refused. ${e.message} Nothing was published, and what participants read is unchanged.`);
      else this.error.set(e instanceof Error ? e.message : 'The programme could not be published.');
    } finally { this.busy.set(false); }
  }
  follower(cohort: CurriculumDraftResult['readiness']['cohorts'][number]) { return `Starts ${this.day(cohort.startDate)} · ${cohort.durationWeeks} weeks · a session every ${cohort.sessionCadenceWeeks} weeks · ${this.count(cohort.sessionAllowance, 'session')}${cohort.hasEnded ? ' · ended' : ''}`; }
  count(value: number, noun: string) { return `${value} ${noun}${value === 1 ? '' : 's'}`; }
  private day(value: string) { return new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'long', year: 'numeric' }).format(new Date(value + 'T00:00:00')); }
  private when(value: string) { return new Intl.DateTimeFormat('en-GB', { weekday: 'long', day: 'numeric', month: 'long', hour: 'numeric', minute: '2-digit' }).format(new Date(value)); }
}
