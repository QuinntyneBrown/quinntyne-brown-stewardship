import { Component, inject, input, output, signal, computed, OnInit, TemplateRef, contentChild, viewChild, ElementRef } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ModulePathItem } from '../module-path-item';
@Component({
  selector: 'qbs-module-path', imports: [RouterLink], templateUrl: './module-path.component.html', styleUrl: './module-path.component.css'
})
export class ModulePathComponent {
  readonly items = input.required<readonly ModulePathItem[]>();
}
