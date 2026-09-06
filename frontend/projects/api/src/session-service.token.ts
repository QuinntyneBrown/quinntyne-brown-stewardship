import { InjectionToken } from "@angular/core";
import { ISessionService } from "./session-service.contract";
export const SESSION_SERVICE = new InjectionToken<ISessionService>("Session service");
