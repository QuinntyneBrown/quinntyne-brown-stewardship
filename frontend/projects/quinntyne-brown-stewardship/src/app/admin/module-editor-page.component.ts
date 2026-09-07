import { Component, DestroyRef, computed, inject, signal, viewChild } from '@angular/core';
import { ActivatedRoute, Router, RouterLink, RouterOutlet } from '@angular/router';
import { BreadcrumbComponent, BreadcrumbItem, ConfirmDialogComponent, PromptDraft, StatusMessageComponent } from '@qbs/components';
import { ModuleEditorComponent } from '@qbs/domain';
import { ModuleDraftResult, SectionDraftSummary } from '@qbs/api';
import { redirectToSignIn } from '../redirect-to-sign-in';
// Owns the module route, the one dialog that serves every question the screen asks, and the way back to the programme.
@Component({
  selector: 'qbs-module-editor-page', imports: [RouterLink, RouterOutlet, BreadcrumbComponent, ConfirmDialogComponent, StatusMessageComponent, ModuleEditorComponent], templateUrl: './module-editor-page.component.html', styleUrl: './module-editor-page.component.css'
})
export class ModuleEditorPageComponent {
  private readonly router = inject(Router);
  readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id')!;
  readonly editor = viewChild.required(ModuleEditorComponent);
  readonly dialog = viewChild.required(ConfirmDialogComponent);
  readonly notice = inject(ActivatedRoute).snapshot.queryParamMap.get('notice') ?? '';
  readonly draft = signal<ModuleDraftResult | null>(null);
  // While the preview child is active the editor stays alive but out of sight, its form untouched.
  readonly previewing = signal(false);
  readonly question = signal<{ heading: string; consequence: string; keep: string; confirm: string }>({ heading: '', consequence: '', keep: '', confirm: '' });
  readonly trail = computed<BreadcrumbItem[]>(() => { const draft = this.draft(); return draft
    ? [{ label: 'Programmes', path: '/admin/programmes' }, { label: draft.curriculumTitle, path: `/admin/programmes/${draft.curriculumId}` }, { label: `${String(draft.ordinal).padStart(2, '0')} ${draft.title}`, short: `module ${String(draft.ordinal).padStart(2, '0')}` }]
    : [{ label: 'Programmes', path: '/admin/programmes' }, { label: 'Module' }]; });
  readonly state = computed(() => this.editor().refused().length ? 'Not saved' : this.editor().dirty() ? 'Unsaved changes' : 'Saved');
  constructor() {
    const beforeUnload = (event: BeforeUnloadEvent) => { if (this.editor().dirty()) { event.preventDefault(); event.returnValue = ''; } };
    window.addEventListener('beforeunload', beforeUnload); inject(DestroyRef).onDestroy(() => window.removeEventListener('beforeunload', beforeUnload));
  }
  expired() { return redirectToSignIn(this.router); }
  private ask(heading: string, consequence: string, keep: string, confirm: string) { this.question.set({ heading, consequence, keep, confirm }); return this.dialog().open(); }
  async removeModule() {
    const draft = this.draft(); if (!draft) return;
    if (await this.ask('Remove this module?', `${draft.title} will be removed from ${draft.curriculumTitle} with its sections and prompts, and the modules after it will close the gap. Nothing recorded depends on it.`, 'Keep module', 'Remove module')) await this.editor().remove();
  }
  async removeSection(section: SectionDraftSummary) {
    const draft = this.draft(); if (!draft) return;
    if (await this.ask('Remove this section?', `${section.title} will be removed from ${draft.title}, and the sections after it will close the gap. No participant has completed it.`, 'Keep it', 'Remove section')) await this.editor().removeSection(section);
  }
  sectionAdded(id: string) { return this.router.navigate(['/admin/sections', id]); }
  async removePrompt(prompt: PromptDraft) {
    const position = this.editor().prompts().indexOf(prompt) + 1;
    if (await this.ask('Remove this prompt?', `Prompt ${position} will be removed from this module, and the prompts after it will close the gap. No participant has answered it.`, 'Keep it', 'Remove prompt')) await this.editor().removePrompt(prompt);
  }
  removed() { const draft = this.draft(); return this.router.navigate(['/admin/programmes', draft?.curriculumId], { queryParams: { notice: 'Module removed.' } }); }
  canLeave() { if (!this.editor().dirty()) return true; return this.ask('Leave without saving?', this.editor().unsavedSummary(), 'Keep editing', 'Discard changes'); }
}
