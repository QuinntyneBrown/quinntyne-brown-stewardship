import { Component, inject, input, output, signal, computed, OnInit, TemplateRef, contentChild, viewChild, ElementRef } from '@angular/core';
import { DestroyRef } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { NoteEditorComponent } from '@qbs/domain';
import { redirectToSignIn } from '../redirect-to-sign-in';
@Component({
  selector: 'qbs-note-editor-page', imports: [NoteEditorComponent], templateUrl: './note-editor-page.component.html', styleUrl: './note-editor-page.component.css'
})
export class NoteEditorPageComponent {
  private readonly router = inject(Router); private readonly route = inject(ActivatedRoute);
  readonly editor = viewChild.required(NoteEditorComponent);
  readonly id = this.route.snapshot.paramMap.get('id') ?? undefined;
  readonly moduleId = this.route.snapshot.queryParamMap.get('moduleId') ?? undefined; readonly sessionId = this.route.snapshot.queryParamMap.get('sessionId') ?? undefined; readonly promptId = this.route.snapshot.queryParamMap.get('promptId') ?? undefined;
  constructor() { const beforeUnload = (event: BeforeUnloadEvent) => { if (this.editor().dirty()) { event.preventDefault(); event.returnValue = ''; } }; window.addEventListener('beforeunload', beforeUnload); inject(DestroyRef).onDestroy(() => window.removeEventListener('beforeunload', beforeUnload)); }
  readonly authenticationRequired = signal(false);
  expired() { if (this.editor().dirty()) { this.authenticationRequired.set(true); return; } void redirectToSignIn(this.router); }
  saved() { return this.router.navigateByUrl('/notes'); }
  canLeave() { if (!this.editor().dirty()) return true; const leave = window.confirm('Leave this note? Your unsaved text will be lost.'); if (leave) this.editor().discardDirtyFlag(); return leave; }
}
