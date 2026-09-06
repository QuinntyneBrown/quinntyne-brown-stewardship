import { Component, inject, input, output, signal, computed, OnInit, TemplateRef, contentChild, viewChild, ElementRef } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { NotesListComponent } from '@qbs/domain';
import { redirectToSignIn } from '../redirect-to-sign-in';
@Component({
  selector: 'qbs-notes-page', imports: [NotesListComponent], templateUrl: './notes-page.component.html', styleUrl: './notes-page.component.css'
})
export class NotesPageComponent {
  private readonly router = inject(Router); private readonly route = inject(ActivatedRoute);
  readonly moduleId = this.route.snapshot.queryParamMap.get('moduleId') ?? undefined; readonly sessionId = this.route.snapshot.queryParamMap.get('sessionId') ?? undefined;
  expired() { return redirectToSignIn(this.router); }
}
