import { InjectionToken } from "@angular/core";
import { ICohortService } from "./i-cohort-service";
export const COHORT_SERVICE = new InjectionToken<ICohortService>(
  "COHORT_SERVICE",
);
