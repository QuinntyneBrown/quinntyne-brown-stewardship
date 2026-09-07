import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ServiceError } from './service-error';
import { ValidationError } from './validation-error';
export async function programmeRequest<T>(http: HttpClient, method: string, url: string, body?: unknown): Promise<T> {
  try {
    const headers: Record<string, string> = {};
    if (method !== 'GET') {
      const csrf = await firstValueFrom(http.get<{ token: string }>('/authentication/csrf'));
      headers['X-CSRF-TOKEN'] = csrf.token;
    }
    return await firstValueFrom(http.request<T>(method, url, { body, headers }));
  } catch (error) {
    const response = error instanceof HttpErrorResponse ? error : null;
    const status = response?.status ?? 0, correlationId = response?.error?.correlationId;
    const message = response?.error?.title ?? 'The request could not be completed. Please try again.';
    if (status === 400 && response?.error?.errors) throw new ValidationError(status, correlationId, message, response.error.errors);
    throw new ServiceError(status, correlationId, message);
  }
}
