import { ServiceError } from "./service-error";
// A refused write names every field that failed; the editor reads the message for each.
export class ValidationError extends ServiceError {
  constructor(status: number, correlationId: string | undefined, message: string, readonly errors: Record<string, string[]>) {
    super(status, correlationId, message);
  }
  errorFor(field: string): string {
    const key = Object.keys(this.errors).find(k => k.toLowerCase() === field.toLowerCase());
    return key ? this.errors[key].join(" ") : "";
  }
}
