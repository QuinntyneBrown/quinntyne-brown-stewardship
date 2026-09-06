import { InjectionToken } from "@angular/core";
import { ICohortService } from "./cohort-service.contract";
export const COHORT_SERVICE = new InjectionToken<ICohortService>(
  "COHORT_SERVICE",
);
