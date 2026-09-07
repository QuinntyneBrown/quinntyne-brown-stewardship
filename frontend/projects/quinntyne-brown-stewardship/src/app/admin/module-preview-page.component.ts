import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ErrorMessageComponent, StatusMessageComponent } from '@qbs/components';
import { ModuleReaderComponent } from '@qbs/domain';
import { ModuleResult } from '@qbs/api';
import { ModuleEditorPageComponent } from './module-editor-page.component';
// Renders the module the editor holds, unsaved changes included, exactly as a participant would read it; nothing is recorded.
@Component({
  selector: 'qbs-module-preview-page', imports: [RouterLink, ErrorMessageComponent, StatusMessageComponent, ModuleReaderComponent], templateUrl: './module-preview-page.component.html', styleUrl: './module-preview-page.component.css'
})
export class ModulePreviewPageComponent implements OnInit {
  // The editor page stays alive beneath this child route, so the preview reads the form as it stands.
  private readonly parent = inject(ModuleEditorPageComponent);
  readonly module = signal<ModuleResult | null>(null);
  readonly error = signal('');
  async ngOnInit() {
    try { this.module.set(await this.parent.editor().loadPreview()); }
    catch (e) { this.error.set(e instanceof Error ? e.message : 'The preview could not be loaded.'); }
  }
}
