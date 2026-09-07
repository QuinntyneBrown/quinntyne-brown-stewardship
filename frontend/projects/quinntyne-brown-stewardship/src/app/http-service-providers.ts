import { EnvironmentProviders, Provider } from "@angular/core";
import { CURRICULUM_SERVICE, CurriculumService, SESSION_SERVICE, SessionService, NOTE_SERVICE, NoteService, AUTHORING_SERVICE, AuthoringService } from '@qbs/api';
import {
  COHORT_SERVICE,
  CohortService,
  SIGN_IN_SERVICE,
  SignInService,
} from "@qbs/api";

export const serviceProviders: (Provider | EnvironmentProviders)[] = [
  { provide: CURRICULUM_SERVICE, useClass: CurriculumService },
  { provide: SESSION_SERVICE, useClass: SessionService },
  { provide: NOTE_SERVICE, useClass: NoteService },
  { provide: SIGN_IN_SERVICE, useClass: SignInService },
  { provide: COHORT_SERVICE, useClass: CohortService },
  { provide: AUTHORING_SERVICE, useClass: AuthoringService },
];
