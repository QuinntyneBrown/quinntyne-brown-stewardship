import { inject, Injectable } from "@angular/core";
import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { firstValueFrom } from "rxjs";
import { ICohortService } from "./cohort-service.contract";
import { EnrollmentResult } from "./enrollment-result";
import { ServiceError } from "./service-error";
@Injectable()
export class CohortService implements ICohortService {
  private readonly http = inject(HttpClient);
  async getEnrollment(): Promise<EnrollmentResult> {
    try {
      return await firstValueFrom(
        this.http.get<EnrollmentResult>("/enrollment"),
      );
    } catch (error) {
      throw new ServiceError(
        error instanceof HttpErrorResponse ? error.status : 0,
      );
    }
  }
}
