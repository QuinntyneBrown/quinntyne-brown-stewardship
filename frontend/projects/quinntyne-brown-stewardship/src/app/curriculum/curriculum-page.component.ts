import { Component, inject } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { CurriculumOverviewComponent } from '@qbs/domain';
import { ErrorMessageComponent } from '@qbs/components';
import { redirectToSignIn } from '../redirect-to-sign-in';
@Component({
  selector: 'qbs-curriculum-page', imports: [CurriculumOverviewComponent, ErrorMessageComponent], templateUrl: './curriculum-page.component.html', styleUrl: './curriculum-page.component.css'
})
export class CurriculumPageComponent {
  private readonly router = inject(Router); private readonly route = inject(ActivatedRoute);
  readonly notice = this.route.snapshot.queryParamMap.get('notice') ?? '';
  // A participant turned back from an authoring screen is told why, without any authored content having been shown.
  readonly refused = this.route.snapshot.queryParamMap.get('refused') === 'authoring' ? 'Curriculum authoring is not open to your account. You have been returned to your curriculum.' : '';
  expired() { return redirectToSignIn(this.router); }
}
