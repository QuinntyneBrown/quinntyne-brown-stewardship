import {
  EnvironmentProviders,
  inject,
  provideAppInitializer,
  Provider,
} from "@angular/core";
import { COHORT_SERVICE, SIGN_IN_SERVICE } from "@qbs/api";
import { CURRICULUM_SERVICE, SESSION_SERVICE, NOTE_SERVICE } from '@qbs/api';
import { CurriculumServiceMock, SessionServiceMock, NoteServiceMock } from '@qbs/api/testing';
import {
  CohortServiceMock,
  MockStateStore,
  SignInServiceMock,
} from "@qbs/api/testing";
import { installPlaywrightBridge } from "./testing/playwright-bridge";

export const serviceProviders: (Provider | EnvironmentProviders)[] = [
  { provide: CURRICULUM_SERVICE, useClass: CurriculumServiceMock },
  { provide: SESSION_SERVICE, useClass: SessionServiceMock },
  { provide: NOTE_SERVICE, useClass: NoteServiceMock },
  { provide: SIGN_IN_SERVICE, useClass: SignInServiceMock },
  { provide: COHORT_SERVICE, useClass: CohortServiceMock },
  provideAppInitializer(() => installPlaywrightBridge(inject(MockStateStore))),
];
