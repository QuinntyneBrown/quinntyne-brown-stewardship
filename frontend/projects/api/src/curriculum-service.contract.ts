import { CurriculumResult } from "./curriculum-result";
import { ModuleResult } from "./module-result";
import { CompletionResult } from "./completion-result";
export interface ICurriculumService {
  getCurriculum(): Promise<CurriculumResult>;
  getModule(ordinal: number | null): Promise<ModuleResult>;
  completeSection(id: string): Promise<CompletionResult>;
}
