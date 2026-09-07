import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BreadcrumbComponent, BreadcrumbItem, StatusMessageComponent } from '@qbs/components';
import { ProgrammeListComponent } from '@qbs/domain';
import { redirectToSignIn } from '../redirect-to-sign-in';
@Component({
  selector: 'qbs-programme-list-page', imports: [BreadcrumbComponent, StatusMessageComponent, ProgrammeListComponent], templateUrl: './programme-list-page.component.html', styleUrl: './programme-list-page.component.css'
})
export class ProgrammeListPageComponent {
  private readonly router = inject(Router);
  readonly notice = inject(ActivatedRoute).snapshot.queryParamMap.get('notice') ?? '';
  readonly trail: BreadcrumbItem[] = [{ label: 'Programmes', path: '/admin/programmes' }];
  expired() { return redirectToSignIn(this.router); }
  created(id: string) { return this.router.navigate(['/admin/programmes', id]); }
}
