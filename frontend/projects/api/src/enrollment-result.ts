export interface EnrollmentResult {
  isEnrolled: boolean;
  cohortId: string | null;
  mentorName: string | null;
  startDate: string | null;
  endDate: string | null;
  currentWeek: number | null;
  sessionAllowance: number | null;
  hasEnded: boolean | null;
  durationWeeks: number | null;
  sessionCadenceWeeks: number | null;
  isProgrammePublished: boolean | null;
}
