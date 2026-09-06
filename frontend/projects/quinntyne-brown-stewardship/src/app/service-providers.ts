import { Provider } from "@angular/core";
import {
  SIGN_IN_SERVICE,
  COHORT_SERVICE,
  SignInService,
  CohortService,
} from "@qbs/api";
export const serviceProviders: Provider[] = [
  { provide: SIGN_IN_SERVICE, useClass: SignInService },
  { provide: COHORT_SERVICE, useClass: CohortService },
];
