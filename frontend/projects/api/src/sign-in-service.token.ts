import { InjectionToken } from "@angular/core";
import { ISignInService } from "./i-sign-in-service";
export const SIGN_IN_SERVICE = new InjectionToken<ISignInService>(
  "SIGN_IN_SERVICE",
);
