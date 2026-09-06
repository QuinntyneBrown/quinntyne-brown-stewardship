import { InjectionToken } from "@angular/core";
import { ISignInService } from "./sign-in-service.contract";
export const SIGN_IN_SERVICE = new InjectionToken<ISignInService>(
  "SIGN_IN_SERVICE",
);
