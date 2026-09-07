import { Component, DestroyRef, computed, inject, signal, viewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BreadcrumbComponent, BreadcrumbItem, ConfirmDialogComponent, StatusMessageComponent } from '@qbs/components';
import { ProgrammeEditorComponent } from '@qbs/domain';
import { CurriculumDraftResult } from '@qbs/api';
import { redirectToSignIn } from '../redirect-to-sign-in';
@Component({
  selector: 'qbs-programme-editor-page', imports: [BreadcrumbComponent, ConfirmDialogComponent, StatusMessageComponent, ProgrammeEditorComponent], templateUrl: './programme-editor-page.component.html', styleUrl: './programme-editor-page.component.css'
})
export class ProgrammeEditorPageComponent {
  private readonly router = inject(Router);
  readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id')!;
  readonly editor = viewChild.required(ProgrammeEditorComponent);
  readonly dialog = viewChild.required(ConfirmDialogComponent);
  readonly notice = inject(ActivatedRoute).snapshot.queryParamMap.get('notice') ?? '';
  readonly draft = signal<CurriculumDraftResult | null>(null);
  readonly trail = computed<BreadcrumbItem[]>(() => [{ label: 'Programmes', path: '/admin/programmes' }, { label: this.draft()?.title ?? 'Programme' }]);
  readonly saved = computed(() => { const draft = this.draft(); return draft ? `${draft.publishedAt ? 'Published' : 'Created'} ${this.when(draft.publishedAt ?? draft.createdAt)}` : ''; });
  constructor() {
    const beforeUnload = (event: BeforeUnloadEvent) => { if (this.editor().dirty()) { event.preventDefault(); event.returnValue = ''; } };
    window.addEventListener('beforeunload', beforeUnload); inject(DestroyRef).onDestroy(() => window.removeEventListener('beforeunload', beforeUnload));
  }
  expired() { return redirectToSignIn(this.router); }
  async remove() { if (await this.dialog().open()) await this.editor().remove(); }
  moduleAdded(id: string) { return this.router.navigate(['/admin/modules', id]); }
  removed() { return this.router.navigate(['/admin/programmes'], { queryParams: { notice: 'Programme removed.' } }); }
  canLeave() { if (!this.editor().dirty()) return true; return this.dialog().open(); }
  private when(value: string) { return new Intl.DateTimeFormat('en-GB', { weekday: 'long', day: 'numeric', month: 'long', hour: 'numeric', minute: '2-digit' }).format(new Date(value)); }
}
