import { CurriculumResult } from "./curriculum-result";
import { ModuleResult } from "./module-result";
import { CompletionResult } from "./completion-result";
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ICurriculumService } from './curriculum-service.contract';
import { programmeRequest } from './programme-request';
@Injectable()
export class CurriculumService implements ICurriculumService {
  private readonly http = inject(HttpClient);
  getCurriculum(): Promise<CurriculumResult> { return programmeRequest<CurriculumResult>(this.http, 'GET', '/curriculum'); }
  getModule(ordinal: number | null): Promise<ModuleResult> { return programmeRequest<ModuleResult>(this.http, 'GET', '/modules/' + (ordinal ?? 'current')); }
  completeSection(id: string): Promise<CompletionResult> { return programmeRequest<CompletionResult>(this.http, 'POST', `/sections/${encodeURIComponent(id)}/completion`, {}); }
}
