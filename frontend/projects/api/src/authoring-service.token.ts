import { InjectionToken } from "@angular/core";
import { IAuthoringService } from "./authoring-service.contract";
export const AUTHORING_SERVICE = new InjectionToken<IAuthoringService>("Authoring service");
