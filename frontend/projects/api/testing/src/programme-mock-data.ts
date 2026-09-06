import { BookingResult, NoteResult } from '@qbs/api';
export interface ProgrammeMockData { completed: string[]; booking: BookingResult | null; past: BookingResult[]; notes: NoteResult[]; taken: string[]; }
