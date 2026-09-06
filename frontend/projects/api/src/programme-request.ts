import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ServiceError } from './service-error';
export async function programmeRequest<T>(http: HttpClient, method: string, url: string, body?: unknown): Promise<T> {
  try {
    const headers: Record<string, string> = {};
    if (method !== 'GET') {
      const csrf = await firstValueFrom(http.get<{ token: string }>('/authentication/csrf'));
      headers['X-CSRF-TOKEN'] = csrf.token;
    }
    return await firstValueFrom(http.request<T>(method, url, { body, headers }));
  } catch (error) {
    throw new ServiceError(error instanceof HttpErrorResponse ? error.status : 0,
      error instanceof HttpErrorResponse ? error.error?.correlationId : undefined,
      error instanceof HttpErrorResponse ? error.error?.title ?? 'The request could not be completed. Please try again.' : 'The request could not be completed. Please try again.');
  }
}
