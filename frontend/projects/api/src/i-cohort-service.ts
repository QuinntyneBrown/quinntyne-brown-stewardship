import { EnrollmentResult } from "./enrollment-result";
export interface ICohortService {
  getEnrollment(): Promise<EnrollmentResult>;
}
