export class ServiceError extends Error {
  constructor(
    readonly status: number,
    readonly correlationId?: string,
  ) {
    super("The request could not be completed.");
  }
}
