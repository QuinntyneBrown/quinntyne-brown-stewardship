import { inject, Injectable } from "@angular/core";
import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { firstValueFrom } from "rxjs";
import { ISignInService } from "./sign-in-service.contract";
import { SessionResult } from "./session-result";
import { SignInResult } from "./sign-in-result";
import { ServiceError } from "./service-error";

@Injectable()
export class SignInService implements ISignInService {
  private readonly http = inject(HttpClient);
  async session(): Promise<SessionResult | null> {
    try {
      return await firstValueFrom(
        this.http.get<SessionResult>("/authentication/session"),
      );
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 401)
        return null;
      throw this.failure(error);
    }
  }
  async signIn(emailAddress: string, password: string): Promise<SignInResult> {
    try {
      const headers = await this.csrf();
      return await firstValueFrom(
        this.http.post<SignInResult>(
          "/authentication/sign-in",
          { emailAddress, password },
          { headers },
        ),
      );
    } catch (error) {
      if (
        error instanceof HttpErrorResponse &&
        [401, 429].includes(error.status)
      )
        return error.error as SignInResult;
      throw this.failure(error);
    }
  }
  async signOut(): Promise<void> {
    try {
      const headers = await this.csrf();
      await firstValueFrom(
        this.http.post("/authentication/sign-out", {}, { headers }),
      );
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 401) return;
      throw this.failure(error);
    }
  }
  private async csrf() {
    const result = await firstValueFrom(
      this.http.get<{ token: string }>("/authentication/csrf"),
    );
    return { "X-CSRF-TOKEN": result.token };
  }
  private failure(error: unknown) {
    return error instanceof HttpErrorResponse
      ? new ServiceError(error.status, error.error?.correlationId)
      : new ServiceError(0);
  }
}
