export interface SaveNoteRequest {
  body: string; id?: string; moduleId: string | null; sessionId: string | null; promptId?: string | null; revision?: string;
}
