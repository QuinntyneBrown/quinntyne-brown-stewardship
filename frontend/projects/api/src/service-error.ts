export class ServiceError extends Error {
  constructor(
    readonly status: number,
    readonly correlationId?: string,
    message = "The request could not be completed.",
  ) {
    super(message);
  }
}
