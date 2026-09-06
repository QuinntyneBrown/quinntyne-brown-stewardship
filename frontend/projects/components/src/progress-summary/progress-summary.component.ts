import { Component, inject, input, output, signal, computed, OnInit, TemplateRef, contentChild, viewChild, ElementRef } from '@angular/core';

@Component({
  selector: 'qbs-progress-summary', imports: [], templateUrl: './progress-summary.component.html', styleUrl: './progress-summary.component.css'
})
export class ProgressSummaryComponent {
  readonly completed = input.required<number>(); readonly total = input.required<number>(); readonly label = input.required<string>();
}
