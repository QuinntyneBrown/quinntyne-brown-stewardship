import { Component, inject, input, output, signal, computed, OnInit, TemplateRef, contentChild, viewChild, ElementRef } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { ModuleReaderComponent } from '@qbs/domain';
import { redirectToSignIn } from '../redirect-to-sign-in';
@Component({
  selector: 'qbs-module-page', imports: [ModuleReaderComponent], templateUrl: './module-page.component.html', styleUrl: './module-page.component.css'
})
export class ModulePageComponent {
  private readonly router = inject(Router); private readonly route = inject(ActivatedRoute);
  readonly ordinal = this.route.snapshot.paramMap.get('id') === 'current' ? null : Number(this.route.snapshot.paramMap.get('id'));
  expired() { return redirectToSignIn(this.router); }
  completed() { return this.router.navigateByUrl('/curriculum'); }
  locked(message: string) { return this.router.navigate(['/curriculum'], { queryParams: { notice: message }, replaceUrl: true }); }
}
