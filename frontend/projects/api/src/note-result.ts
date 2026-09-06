export interface NoteResult {
  id: string; moduleId: string | null; sessionId: string | null; promptId: string | null; attachmentTitle: string; body: string; revisedAt: string; revision: string; canEdit: boolean;
}
