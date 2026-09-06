import { Component, inject, input, output, signal, computed, OnInit, TemplateRef, contentChild, viewChild, ElementRef } from '@angular/core';
import { RetryNoticeComponent, StatusMessageComponent } from '@qbs/components';
import { ServiceError } from '@qbs/api';
import { RouterLink } from '@angular/router';
import { NOTE_SERVICE, NoteResult, NoteAttachmentOption } from '@qbs/api';
@Component({
  selector: 'qbs-note-editor', imports: [RouterLink, StatusMessageComponent], templateUrl: './note-editor.component.html', styleUrl: './note-editor.component.css'
})
export class NoteEditorComponent implements OnInit {
  private readonly service = inject(NOTE_SERVICE);
  readonly id = input<string | undefined>(); readonly moduleId = input<string | undefined>(); readonly sessionId = input<string | undefined>(); readonly promptId = input<string | undefined>();
  readonly expired = output<void>(); readonly saved = output<void>();
  readonly note = signal<NoteResult | null>(null); readonly attachments = signal<NoteAttachmentOption[]>([]); readonly attachment = signal('');
  readonly body = signal(''); readonly original = signal(''); readonly dirty = computed(() => this.body() !== this.original());
  readonly maxLength = signal(10000); readonly ready = signal(false); readonly error = signal(''); readonly busy = signal(false); readonly conflict = signal(false); readonly latest = signal<NoteResult | null>(null);
  ngOnInit() { void this.load(); }
  async load() {
    this.error.set('');
    try {
      if (this.id()) { const note = await this.service.get(this.id()!); this.note.set(note); this.body.set(note.body); this.original.set(note.body); this.attachments.set([{ moduleId: note.moduleId, sessionId: note.sessionId, title: note.attachmentTitle }]); this.attachment.set(note.moduleId ?? note.sessionId!); }
      else { const notes = await this.service.list(); this.attachments.set(notes.attachments); this.maxLength.set(notes.maxLength); const requested = this.moduleId() ?? this.sessionId(); if (requested && !notes.attachments.some(a => (a.moduleId ?? a.sessionId) === requested)) throw new ServiceError(404, undefined, 'That module or session is unavailable. Return to your notes and choose an available attachment.'); this.attachment.set(requested ?? (notes.attachments[0]?.moduleId ?? notes.attachments[0]?.sessionId ?? '')); }
      this.ready.set(true);
    } catch(e) { this.fail(e); }
  }
  fail(e: unknown) { if(e instanceof ServiceError && e.status === 401) this.expired.emit(); else this.error.set(e instanceof Error ? e.message : 'We could not save your note. Your draft is still here.'); }
  async save() {
    if (!this.body().trim() || this.body().length > this.maxLength() || this.busy() || this.conflict()) return;
    const attachment = this.attachments().find(a => (a.moduleId ?? a.sessionId) === this.attachment()); if (!attachment) return;
    this.busy.set(true); this.error.set('');
    try { const note = this.note(); const result = await this.service.save({ id: note?.id, body: this.body(), moduleId: attachment.moduleId, sessionId: attachment.sessionId, promptId: note?.promptId ?? this.promptId() ?? null, revision: note?.revision }); this.note.set(result); this.original.set(result.body); this.saved.emit(); }
    catch(e) { this.fail(e); if(e instanceof ServiceError && e.status === 409 && this.note()) this.conflict.set(true); }
    finally { this.busy.set(false); }
  }
  async compareLatest() { if (!this.note()) return; try { this.latest.set(await this.service.get(this.note()!.id)); } catch(e) { this.fail(e); } }
  keepDraft() { const latest = this.latest(); if (!latest) return; this.note.set(latest); this.original.set(latest.body); this.latest.set(null); this.conflict.set(false); this.error.set('Latest revision reviewed. Save to replace it with your draft.'); }
  discardDirtyFlag() { this.original.set(this.body()); }
}
