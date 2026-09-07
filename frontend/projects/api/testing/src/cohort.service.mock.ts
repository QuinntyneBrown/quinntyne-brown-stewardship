import { inject, Injectable } from "@angular/core";
import { EnrollmentResult, ICohortService, ServiceError } from "@qbs/api";
import { MockStateStore } from "./mock-state.store";
import { ProgrammeMockStore } from "./programme-mock.store";

@Injectable()
export class CohortServiceMock implements ICohortService {
  private readonly state = inject(MockStateStore);
  private readonly programme = inject(ProgrammeMockStore);

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
    if (state.enrolled) {
      const cohort = this.programme.cohort();
      return { isEnrolled: true, cohortId: cohort.id, mentorName: cohort.mentorName, startDate: cohort.startDate, endDate: cohort.endDate, currentWeek: cohort.currentWeek, sessionAllowance: cohort.sessionAllowance, hasEnded: cohort.hasEnded, durationWeeks: cohort.durationWeeks, sessionCadenceWeeks: cohort.sessionCadenceWeeks, isProgrammePublished: this.programme.shape().published };
    }
    return { isEnrolled: false, cohortId: null, mentorName: null, startDate: null, endDate: null, currentWeek: null, sessionAllowance: null, hasEnded: null, durationWeeks: null, sessionCadenceWeeks: null, isProgrammePublished: null };
  }
}
