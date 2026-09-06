import { Provider } from "@angular/core";
import { SIGN_IN_SERVICE, COHORT_SERVICE } from "@qbs/api";
import { MockSignInService } from "../../../api/src/testing/mock-sign-in.service";
import { MockCohortService } from "../../../api/src/testing/mock-cohort.service";
export const serviceProviders: Provider[] = [
  { provide: SIGN_IN_SERVICE, useClass: MockSignInService },
  { provide: COHORT_SERVICE, useClass: MockCohortService },
];
