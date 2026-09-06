import { Component, inject, input, output, signal, computed, OnInit, TemplateRef, contentChild, viewChild, ElementRef } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { CurriculumOverviewComponent } from '@qbs/domain';
import { redirectToSignIn } from '../redirect-to-sign-in';
@Component({
  selector: 'qbs-curriculum-page', imports: [CurriculumOverviewComponent], templateUrl: './curriculum-page.component.html', styleUrl: './curriculum-page.component.css'
})
export class CurriculumPageComponent {
  private readonly router = inject(Router); private readonly route = inject(ActivatedRoute);
  readonly notice = this.route.snapshot.queryParamMap.get('notice') ?? '';
  expired() { return redirectToSignIn(this.router); }
}
