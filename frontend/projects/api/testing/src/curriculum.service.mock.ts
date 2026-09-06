import { inject, Injectable } from '@angular/core';
import { ICurriculumService, ServiceError } from '@qbs/api';
import { ProgrammeMockStore } from './programme-mock.store';
@Injectable()
export class CurriculumServiceMock implements ICurriculumService {
  private readonly store = inject(ProgrammeMockStore);
  async getCurriculum() { await this.store.waitForResponse(); return this.store.curriculum(); }
  async getModule(ordinal: number | null) {
    await this.store.waitForResponse();
    const current = this.store.curriculum().currentOrdinal;
    const module = this.store.module(ordinal ?? current ?? 12);
    if (!module.isComplete && module.ordinal !== current) throw new ServiceError(409, undefined, 'Complete the current module to unlock this module.');
    return module;
  }
  async completeSection(id: string) {
    this.store.check();
    if (!this.store.data().completed.includes(id)) this.store.update({ completed: [...this.store.data().completed, id] });
    const module = this.store.module(Number(id.split('-')[1]));
    return { isModuleComplete: module.isComplete, nextSectionId: module.isComplete ? null : module.resumeSectionId, currentOrdinal: this.store.curriculum().currentOrdinal };
  }
}
