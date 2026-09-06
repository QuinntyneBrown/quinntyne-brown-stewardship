import { Component, DestroyRef, ElementRef, effect, inject, input, output, signal, viewChildren } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NOTE_SERVICE, NoteResult, ServiceError } from '@qbs/api';

@Component({
  selector: 'qbs-note-collection',
  imports: [RouterLink],
  templateUrl: './note-collection.component.html',
  styleUrl: './note-collection.component.css',
})
export class NoteCollectionComponent {
  private readonly service = inject(NOTE_SERVICE);
  private generation = 0;
  readonly initialNotes = input.required<NoteResult[]>();
  readonly initialCursor = input.required<string | null>();
  readonly moduleId = input<string | undefined>();
  readonly sessionId = input<string | undefined>();
  readonly generalOnly = input(false);
  readonly showAttachment = input(true);
  readonly emptyMessage = input('No notes yet. Start with something you noticed in a module or a conversation.');
  readonly expired = output<void>();
  readonly notes = signal<NoteResult[]>([]);
  readonly cursor = signal<string | null>(null);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly announcement = signal('');
  private readonly articles = viewChildren<ElementRef<HTMLElement>>('noteArticle');

  constructor() {
    effect(() => {
      const notes = this.initialNotes(), cursor = this.initialCursor();
      this.moduleId(); this.sessionId(); this.generalOnly();
      this.generation++;
      this.notes.set(notes); this.cursor.set(cursor);
      this.busy.set(false); this.error.set(''); this.announcement.set('');
    });
    inject(DestroyRef).onDestroy(() => this.generation++);
  }

  date(value: string) {
    return new Intl.DateTimeFormat('en-CA', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
  }

  async more() {
    const cursor = this.cursor();
    if (!cursor || this.busy()) return;
    const generation = this.generation, previousCount = this.notes().length;
    this.busy.set(true); this.error.set('');
    try {
      const page = await this.service.list(this.moduleId(), this.sessionId(), cursor, this.generalOnly());
      if (generation !== this.generation) return;
      const existing = new Set(this.notes().map(note => note.id));
      this.notes.update(notes => [...notes, ...page.notes.filter(note => !existing.has(note.id))]);
      this.cursor.set(page.nextCursor);
      this.announcement.set(`${this.notes().length - previousCount} more notes loaded.${page.nextCursor ? '' : ' All notes are shown.'}`);
      setTimeout(() => {
        if (generation === this.generation) this.articles()[previousCount]?.nativeElement.focus();
      });
    } catch (error) {
      if (generation !== this.generation) return;
      if (error instanceof ServiceError && error.status === 401) this.expired.emit();
      else this.error.set(error instanceof Error ? error.message : 'We could not load more notes. Please try again.');
    } finally {
      if (generation === this.generation) this.busy.set(false);
    }
  }
}
