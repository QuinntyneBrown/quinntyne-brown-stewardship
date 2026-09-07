// A prompt as the editor holds it: stored ones carry their identifier, new ones none until saved.
export interface PromptDraft {
  id: string | null; text: string; answerCount: number; canRemove: boolean;
}
