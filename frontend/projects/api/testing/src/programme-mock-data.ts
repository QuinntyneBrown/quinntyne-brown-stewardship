import { BookingResult, NoteResult } from '@qbs/api';
import { ProgrammeShape } from './programme-shape';
import { MockCurriculum } from './mock-curriculum';
export interface ProgrammeMockData { completed: string[]; booking: BookingResult | null; past: BookingResult[]; notes: NoteResult[]; taken: string[]; shape?: Partial<ProgrammeShape>; noProgrammes?: boolean; curricula?: MockCurriculum[]; }
