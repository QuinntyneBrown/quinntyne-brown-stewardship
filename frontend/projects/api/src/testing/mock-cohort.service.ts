import { Injectable } from "@angular/core";
import { ICohortService } from "../i-cohort-service";
import { ServiceError } from "../service-error";
@Injectable()
export class MockCohortService implements ICohortService {
  async getEnrollment() {
    await new Promise((resolve) =>
      setTimeout(
        resolve,
        Number(localStorage.getItem("stewardship.mock.delay") || 0),
      ),
    );
    if (
      !localStorage.getItem("stewardship.mock.session") ||
      localStorage.getItem("stewardship.mock.enrollmentExpired")
    )
      throw new ServiceError(401);
    if (localStorage.getItem("stewardship.mock.failOnce")) {
      localStorage.removeItem("stewardship.mock.failOnce");
      throw new ServiceError(503);
    }
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
