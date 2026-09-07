import { PublicationState } from "./publication-state";
export interface PublicationResult {
  state: PublicationState; publishedAt: string; publishedModuleCount: number;
}
