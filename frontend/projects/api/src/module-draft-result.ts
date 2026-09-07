import { AuthoringLimits } from "./authoring-limits";
import { PromptDraftResult } from "./prompt-draft-result";
import { PublicationState } from "./publication-state";
import { SectionDraftSummary } from "./section-draft-summary";
export interface ModuleDraftResult {
  id: string; curriculumId: string; curriculumKey: string; curriculumTitle: string; ordinal: number; moduleCount: number;
  title: string; summary: string; effortEstimate: string; practiceSteps: string[]; state: PublicationState; revision: string;
  completionCount: number; noteCount: number; answerCount: number; canRemove: boolean; removalReason: string | null;
  sections: SectionDraftSummary[]; prompts: PromptDraftResult[]; limits: AuthoringLimits;
}
