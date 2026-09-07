import { PublicationState } from '@qbs/api';
// The authored world the mocks hold. Every participant figure and every authoring screen derives from these records.
export interface MockPrompt { id: string; ordinal: number; text: string; answerCount: number; }
export interface MockSection { id: string; ordinal: number; title: string; reading: string; revision: string; completionCount: number; }
export interface MockModule { id: string; ordinal: number; title: string; summary: string; effortEstimate: string; practiceSteps: string[]; state: PublicationState; revision: string; sections: MockSection[]; prompts: MockPrompt[]; noteCount: number; }
export interface MockCurriculum { id: string; key: string; title: string; state: PublicationState; createdAt: string; publishedAt: string | null; cohortCount: number; modules: MockModule[]; }
