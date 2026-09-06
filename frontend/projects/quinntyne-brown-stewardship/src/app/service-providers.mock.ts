import {
  EnvironmentProviders,
  inject,
  provideAppInitializer,
  Provider,
} from "@angular/core";
import { COHORT_SERVICE, SIGN_IN_SERVICE } from "@qbs/api";
import {
  CohortServiceMock,
  MockStateStore,
  SignInServiceMock,
} from "@qbs/api/testing";
import { installPlaywrightBridge } from "./testing/playwright-bridge";

export const serviceProviders: (Provider | EnvironmentProviders)[] = [
  { provide: SIGN_IN_SERVICE, useClass: SignInServiceMock },
  { provide: COHORT_SERVICE, useClass: CohortServiceMock },
  provideAppInitializer(() => installPlaywrightBridge(inject(MockStateStore))),
];
