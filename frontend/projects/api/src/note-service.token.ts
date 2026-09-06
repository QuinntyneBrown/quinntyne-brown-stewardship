import { InjectionToken } from "@angular/core";
import { INoteService } from "./note-service.contract";
export const NOTE_SERVICE = new InjectionToken<INoteService>("Note service");
