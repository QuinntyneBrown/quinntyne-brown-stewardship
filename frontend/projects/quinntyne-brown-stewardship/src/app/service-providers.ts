import { EnvironmentProviders, Provider } from "@angular/core";
import {
  COHORT_SERVICE,
  CohortService,
  SIGN_IN_SERVICE,
  SignInService,
} from "@qbs/api";

export const serviceProviders: (Provider | EnvironmentProviders)[] = [
  { provide: SIGN_IN_SERVICE, useClass: SignInService },
  { provide: COHORT_SERVICE, useClass: CohortService },
];
