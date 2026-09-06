import { Component, inject, input, output, signal, computed, OnInit, TemplateRef, contentChild, viewChild, ElementRef } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { SessionPreparationComponent } from '@qbs/domain';
import { redirectToSignIn } from '../redirect-to-sign-in';
@Component({
  selector: 'qbs-session-detail-page', imports: [SessionPreparationComponent], templateUrl: './session-detail-page.component.html', styleUrl: './session-detail-page.component.css'
})
export class SessionDetailPageComponent {
  private readonly router = inject(Router); readonly id = inject(ActivatedRoute).snapshot.paramMap.get('id')!;
  expired() { return redirectToSignIn(this.router); }
}
