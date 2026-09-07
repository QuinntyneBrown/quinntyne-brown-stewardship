// The practice steps travel whole: their order and their number are the list as submitted.
export interface ModuleRevisionRequest {
  title: string; summary: string; effortEstimate: string; practiceSteps: string[]; revision: string;
}
