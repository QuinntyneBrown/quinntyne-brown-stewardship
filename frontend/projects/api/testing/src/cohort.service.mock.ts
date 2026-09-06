import { inject, Injectable } from "@angular/core";
import { EnrollmentResult, ICohortService, ServiceError } from "@qbs/api";
import { MockStateStore } from "./mock-state.store";

@Injectable()
export class CohortServiceMock implements ICohortService {
  private readonly state = inject(MockStateStore);

  async getEnrollment(): Promise<EnrollmentResult> {
    await this.state.waitForResponse();
    const delay = this.state.current().enrollmentDelayMs;
    if (delay) await new Promise((resolve) => setTimeout(resolve, delay));
    const state = this.state.current();
    if (!state.signedIn || state.enrollmentExpired) throw new ServiceError(401);
    if (state.enrollmentFailsOnce) {
      this.state.update({ enrollmentFailsOnce: false });
      throw new ServiceError(503);
    }
    if (state.enrolled) return { isEnrolled: true, cohortId: 'cohort', mentorName: 'Quinntyne Brown', startDate: '2026-09-07', endDate: '2026-11-30', currentWeek: 1, sessionAllowance: 6, hasEnded: false };
    return {
      isEnrolled: false,
      cohortId: null,
      mentorName: null,
      startDate: null,
      endDate: null,
      currentWeek: null,
      sessionAllowance: null,
      hasEnded: null,
    };
  }
}
