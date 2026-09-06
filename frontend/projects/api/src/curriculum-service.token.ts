import { InjectionToken } from "@angular/core";
import { ICurriculumService } from "./curriculum-service.contract";
export const CURRICULUM_SERVICE = new InjectionToken<ICurriculumService>("Curriculum service");
