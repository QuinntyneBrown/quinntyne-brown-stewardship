import { EnvironmentProviders, Provider } from '@angular/core';
import { AUTHORING_SERVICE, COHORT_SERVICE, CURRICULUM_SERVICE, NOTE_SERVICE, SESSION_SERVICE, SIGN_IN_SERVICE, IAuthoringService, ICohortService, ICurriculumService, INoteService, ISessionService, ISignInService } from '@qbs/api';
import './testing/performance-state';
import { serviceProviders as httpProviders } from './http-service-providers';

// Performance journeys use actual captured DTOs through service-token doubles.
// They never instantiate the production HTTP adapters or issue API requests.
async function reply<T>(path: string): Promise<T> {
  const state = window.__stewardshipPerformance;
  const response = state.responses[path];
  if (!response) throw new Error(`The measured journey has no captured response for ${path}.`);
  state.calls.push(path);
  await new Promise(resolve => setTimeout(resolve, state.latencyMs + response.serverMilliseconds + (response.compressedBytes + response.headerBytes) / state.downloadBytesPerMs));
  return structuredClone(response.body) as T;
}
function unexpectedWrite(): never { throw new Error('The cold-load measurement must not mutate participant data.'); }

export const serviceProviders: (Provider | EnvironmentProviders)[] = [
  // Retain the production adapter code in the measured bundle. Later token
  // providers override it before any service is instantiated.
  ...httpProviders,
  { provide: SIGN_IN_SERVICE, useValue: { session: () => reply('/authentication/session'), signIn: unexpectedWrite, signOut: unexpectedWrite } satisfies ISignInService },
  { provide: COHORT_SERVICE, useValue: { getEnrollment: () => reply('/enrollment') } satisfies ICohortService },
  { provide: CURRICULUM_SERVICE, useValue: { getCurriculum: () => reply('/curriculum'), getModule: () => reply('/modules/current'), completeSection: unexpectedWrite } satisfies ICurriculumService },
  { provide: NOTE_SERVICE, useValue: { list: () => reply('/notes'), get: id => reply(`/notes/${id}`), save: unexpectedWrite } satisfies INoteService },
  { provide: AUTHORING_SERVICE, useValue: {
    getProgrammes: () => reply('/administration/curricula'), getProgramme: id => reply(`/administration/curricula/${id}`), getModule: id => reply(`/administration/modules/${id}`), getSection: id => reply(`/administration/sections/${id}`),
    createProgramme: unexpectedWrite, renameProgramme: unexpectedWrite, changeKey: unexpectedWrite, removeProgramme: unexpectedWrite, publish: unexpectedWrite, addModule: unexpectedWrite, reviseModule: unexpectedWrite, removeModule: unexpectedWrite,
    addPrompt: unexpectedWrite, revisePrompt: unexpectedWrite, removePrompt: unexpectedWrite, addSection: unexpectedWrite, reviseSection: unexpectedWrite, removeSection: unexpectedWrite, reorderModules: unexpectedWrite, reorderSections: unexpectedWrite, reorderPrompts: unexpectedWrite,
  } satisfies IAuthoringService },
  { provide: SESSION_SERVICE, useValue: { availability: () => reply('/sessions/availability'), history: () => reply('/sessions/history'), get: id => reply(`/sessions/${id}`), preparation: id => reply(`/sessions/${id}/preparation`), book: unexpectedWrite, reschedule: unexpectedWrite, cancel: unexpectedWrite } satisfies ISessionService },
];
