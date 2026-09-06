import { inject, Injectable } from "@angular/core";
import {
  ISignInService,
  ServiceError,
  SessionResult,
  SignInResult,
} from "@qbs/api";
import { MockStateStore } from "./mock-state.store";

@Injectable()
export class SignInServiceMock implements ISignInService {
  private readonly state = inject(MockStateStore);

  async session(): Promise<SessionResult | null> {
    await this.state.waitForResponse();
    if (this.state.current().sessionFailure) throw new ServiceError(503);
    return this.state.current().signedIn
      ? { emailAddress: "participant@example.com" }
      : null;
  }

  async signIn(emailAddress: string, password: string): Promise<SignInResult> {
    if (this.state.current().throttled)
      return {
        statusCode: 429,
        message: "Too many attempts. Try again after the cooling-off period.",
        retryAt: new Date(Date.now() + 900000).toISOString(),
      };
    if (
      emailAddress.toLowerCase() !== "participant@example.com" ||
      password !== "A private testing phrase 42!"
    )
      return {
        statusCode: 401,
        message: "Email address or password is incorrect",
        retryAt: null,
      };
    this.state.update({ signedIn: true });
    return { statusCode: 200, message: null, retryAt: null };
  }

  async signOut(): Promise<void> {
    if (this.state.current().signOutFailure) throw new ServiceError(503);
    this.state.update({ signedIn: false });
  }
}
