import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { IAuthoringService } from './authoring-service.contract';
import { CreatedResult } from './created-result';
import { CurriculaResult } from './curricula-result';
import { CurriculumDraftResult } from './curriculum-draft-result';
import { PublicationResult } from './publication-result';
import { ModuleDraftResult } from './module-draft-result';
import { ModuleRevisionRequest } from './module-revision-request';
import { RevisionResult } from './revision-result';
import { SectionDraftResult } from './section-draft-result';
import { SectionRevisionRequest } from './section-revision-request';
import { programmeRequest } from './programme-request';
@Injectable()
export class AuthoringService implements IAuthoringService {
  private readonly http = inject(HttpClient);
  getProgrammes(): Promise<CurriculaResult> { return programmeRequest<CurriculaResult>(this.http, 'GET', '/administration/curricula'); }
  createProgramme(key: string, title: string): Promise<CreatedResult> { return programmeRequest<CreatedResult>(this.http, 'POST', '/administration/curricula', { key, title }); }
  getProgramme(id: string): Promise<CurriculumDraftResult> { return programmeRequest<CurriculumDraftResult>(this.http, 'GET', `/administration/curricula/${encodeURIComponent(id)}`); }
  renameProgramme(id: string, title: string): Promise<void> { return programmeRequest<void>(this.http, 'PUT', `/administration/curricula/${encodeURIComponent(id)}`, { title }); }
  changeKey(id: string, key: string): Promise<void> { return programmeRequest<void>(this.http, 'PUT', `/administration/curricula/${encodeURIComponent(id)}/key`, { key }); }
  publish(id: string): Promise<PublicationResult> { return programmeRequest<PublicationResult>(this.http, 'POST', `/administration/curricula/${encodeURIComponent(id)}/publication`, {}); }
  addModule(curriculumId: string, title: string, summary: string): Promise<CreatedResult> { return programmeRequest<CreatedResult>(this.http, 'POST', `/administration/curricula/${encodeURIComponent(curriculumId)}/modules`, { title, summary }); }
  getModule(id: string): Promise<ModuleDraftResult> { return programmeRequest<ModuleDraftResult>(this.http, 'GET', `/administration/modules/${encodeURIComponent(id)}`); }
  reviseModule(id: string, request: ModuleRevisionRequest): Promise<RevisionResult> { return programmeRequest<RevisionResult>(this.http, 'PUT', `/administration/modules/${encodeURIComponent(id)}`, request); }
  removeModule(id: string): Promise<void> { return programmeRequest<void>(this.http, 'DELETE', `/administration/modules/${encodeURIComponent(id)}`, {}); }
  addPrompt(moduleId: string, text: string): Promise<CreatedResult> { return programmeRequest<CreatedResult>(this.http, 'POST', `/administration/modules/${encodeURIComponent(moduleId)}/prompts`, { text }); }
  revisePrompt(id: string, text: string): Promise<void> { return programmeRequest<void>(this.http, 'PUT', `/administration/prompts/${encodeURIComponent(id)}`, { text }); }
  removePrompt(id: string): Promise<void> { return programmeRequest<void>(this.http, 'DELETE', `/administration/prompts/${encodeURIComponent(id)}`, {}); }
  addSection(moduleId: string, title: string, reading: string): Promise<CreatedResult> { return programmeRequest<CreatedResult>(this.http, 'POST', `/administration/modules/${encodeURIComponent(moduleId)}/sections`, { title, reading }); }
  getSection(id: string): Promise<SectionDraftResult> { return programmeRequest<SectionDraftResult>(this.http, 'GET', `/administration/sections/${encodeURIComponent(id)}`); }
  reviseSection(id: string, request: SectionRevisionRequest): Promise<RevisionResult> { return programmeRequest<RevisionResult>(this.http, 'PUT', `/administration/sections/${encodeURIComponent(id)}`, request); }
  removeSection(id: string): Promise<void> { return programmeRequest<void>(this.http, 'DELETE', `/administration/sections/${encodeURIComponent(id)}`, {}); }
  reorderModules(curriculumId: string, order: string[]): Promise<void> { return programmeRequest<void>(this.http, 'PUT', `/administration/curricula/${encodeURIComponent(curriculumId)}/modules/order`, { order }); }
  reorderSections(moduleId: string, order: string[]): Promise<void> { return programmeRequest<void>(this.http, 'PUT', `/administration/modules/${encodeURIComponent(moduleId)}/sections/order`, { order }); }
  reorderPrompts(moduleId: string, order: string[]): Promise<void> { return programmeRequest<void>(this.http, 'PUT', `/administration/modules/${encodeURIComponent(moduleId)}/prompts/order`, { order }); }
  removeProgramme(id: string): Promise<void> { return programmeRequest<void>(this.http, 'DELETE', `/administration/curricula/${encodeURIComponent(id)}`, {}); }
}
