import { SectionResult } from "./section-result";
import { NoteResult } from './note-result';
import { PromptResult } from "./prompt-result";
export interface ModuleResult {
  notes: NoteResult[];
  id: string; ordinal: number; title: string; summary: string; effortEstimate: string; practiceSteps: string[]; sections: SectionResult[]; preparationPrompts: PromptResult[]; completedSections: number; percent: number; resumeSectionId: string; isComplete: boolean;
}
