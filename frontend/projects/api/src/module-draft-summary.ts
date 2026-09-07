import { PublicationState } from "./publication-state";
export interface ModuleDraftSummary {
  id: string; ordinal: number; title: string; summary: string; state: PublicationState; sectionCount: number; stepCount: number; promptCount: number;
}
