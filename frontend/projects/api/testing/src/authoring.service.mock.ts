import { inject, Injectable } from '@angular/core';
import { IAuthoringService, ModuleRevisionRequest, SectionRevisionRequest, ServiceError } from '@qbs/api';
import { MockStateStore } from './mock-state.store';
import { ProgrammeMockStore } from './programme-mock.store';
@Injectable()
export class AuthoringServiceMock implements IAuthoringService {
  private readonly flags = inject(MockStateStore);
  private readonly store = inject(ProgrammeMockStore);
  // The API refuses every authoring call that lacks administrator authority; the mock refuses the same way.
  private async admit() {
    await this.store.waitForResponse();
    if (!this.flags.current().signedIn) throw new ServiceError(401);
    if (!this.flags.current().administrator) throw new ServiceError(403, undefined, 'Curriculum authoring is not open to your account.');
    if (this.flags.current().authoringFailure) throw new ServiceError(503, undefined, 'The programme is temporarily unavailable. Please try again.');
  }
  async getProgrammes() { await this.admit(); return this.store.curricula(); }
  async createProgramme(key: string, title: string) { await this.admit(); return this.store.createProgramme(key, title); }
  async getProgramme(id: string) { await this.admit(); return this.store.programmeDraft(id); }
  async renameProgramme(id: string, title: string) { await this.admit(); this.store.renameProgramme(id, title); }
  async changeKey(id: string, key: string) { await this.admit(); this.store.changeKey(id, key); }
  async publish(id: string) { await this.admit(); return this.store.publish(id); }
  async addModule(curriculumId: string, title: string, summary: string) { await this.admit(); return this.store.addModule(curriculumId, title, summary); }
  async getModule(id: string) { await this.admit(); return this.store.moduleDraft(id); }
  async reviseModule(id: string, request: ModuleRevisionRequest) { await this.admit(); return this.store.reviseModule(id, request, this.flags); }
  async removeModule(id: string) { await this.admit(); this.store.removeModule(id); }
  async addPrompt(moduleId: string, text: string) { await this.admit(); return this.store.addPrompt(moduleId, text); }
  async revisePrompt(id: string, text: string) { await this.admit(); this.store.revisePrompt(id, text); }
  async removePrompt(id: string) { await this.admit(); this.store.removePrompt(id); }
  async addSection(moduleId: string, title: string, reading: string) { await this.admit(); return this.store.addSection(moduleId, title, reading); }
  async getSection(id: string) { await this.admit(); return this.store.sectionDraft(id); }
  async reviseSection(id: string, request: SectionRevisionRequest) { await this.admit(); return this.store.reviseSection(id, request, this.flags); }
  async removeSection(id: string) { await this.admit(); this.store.removeSection(id); }
  async reorderModules(curriculumId: string, order: string[]) { await this.admit(); this.store.reorderModules(curriculumId, order, this.flags); }
  async reorderSections(moduleId: string, order: string[]) { await this.admit(); this.store.reorderSections(moduleId, order, this.flags); }
  async reorderPrompts(moduleId: string, order: string[]) { await this.admit(); this.store.reorderPrompts(moduleId, order, this.flags); }
  async removeProgramme(id: string) { await this.admit(); this.store.removeProgramme(id); }
}
