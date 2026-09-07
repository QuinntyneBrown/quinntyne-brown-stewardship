import { Component, DestroyRef, computed, inject, signal, viewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BreadcrumbComponent, BreadcrumbItem, ConfirmDialogComponent } from '@qbs/components';
import { SectionEditorComponent } from '@qbs/domain';
import { SectionDraftResult } from '@qbs/api';
import { redirectToSignIn } from '../redirect-to-sign-in';
// Owns the section route and its one dialog; removal returns to the module that held the section.
@Component({
  selector: 'qbs-section-editor-page', imports: [BreadcrumbComponent, ConfirmDialogComponent, SectionEditorComponent], templateUrl: './section-editor-page.component.html', styleUrl: './section-editor-page.component.css'
})
export class SectionEditorPageComponent {
  private readonly router = inject(Router);
  readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id')!;
  readonly editor = viewChild.required(SectionEditorComponent);
  readonly dialog = viewChild.required(ConfirmDialogComponent);
  readonly draft = signal<SectionDraftResult | null>(null);
  readonly question = signal<{ heading: string; consequence: string; keep: string; confirm: string }>({ heading: '', consequence: '', keep: '', confirm: '' });
  readonly trail = computed<BreadcrumbItem[]>(() => { const draft = this.draft(); const ordinal = draft ? String(draft.moduleOrdinal).padStart(2, '0') : ''; return draft
    ? [{ label: 'Programmes', path: '/admin/programmes' }, { label: draft.curriculumTitle, path: `/admin/programmes/${draft.curriculumId}` }, { label: `${ordinal} ${draft.moduleTitle}`, path: `/admin/modules/${draft.moduleId}`, short: `module ${ordinal}` }, { label: `Section ${draft.ordinal}` }]
    : [{ label: 'Programmes', path: '/admin/programmes' }, { label: 'Section' }]; });
  readonly state = computed(() => this.editor().refused().length ? 'Not saved' : this.editor().dirty() ? 'Unsaved changes' : 'Saved');
  constructor() {
    const beforeUnload = (event: BeforeUnloadEvent) => { if (this.editor().dirty()) { event.preventDefault(); event.returnValue = ''; } };
    window.addEventListener('beforeunload', beforeUnload); inject(DestroyRef).onDestroy(() => window.removeEventListener('beforeunload', beforeUnload));
  }
  expired() { return redirectToSignIn(this.router); }
  private ask(heading: string, consequence: string, keep: string, confirm: string) { this.question.set({ heading, consequence, keep, confirm }); return this.dialog().open(); }
  async remove() {
    const draft = this.draft(); if (!draft) return;
    if (await this.ask('Remove this section?', `${draft.title} will be removed from ${draft.moduleTitle}, and the sections after it will close the gap. No participant has completed it.`, 'Keep it', 'Remove section')) await this.editor().remove();
  }
  removed() { return this.router.navigate(['/admin/modules', this.draft()?.moduleId], { queryParams: { notice: 'Section removed.' } }); }
  canLeave() { if (!this.editor().dirty()) return true; return this.ask('Leave without saving?', this.editor().unsavedSummary(), 'Keep editing', 'Discard changes'); }
}
