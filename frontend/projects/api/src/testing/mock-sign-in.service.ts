import { Injectable } from "@angular/core";
import { ISignInService } from "../i-sign-in-service";
import { ServiceError } from "../service-error";
@Injectable()
export class MockSignInService implements ISignInService {
  async session() {
    if (localStorage.getItem("stewardship.mock.sessionFailure"))
      throw new ServiceError(503);
    return localStorage.getItem("stewardship.mock.session")
      ? { emailAddress: "participant@example.com" }
      : null;
  }
  async signIn(emailAddress: string, password: string) {
    if (localStorage.getItem("stewardship.mock.throttled"))
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
    localStorage.setItem("stewardship.mock.session", "true");
    return { statusCode: 200, message: null, retryAt: null };
  }
  async signOut() {
    if (localStorage.getItem("stewardship.mock.signOutFailure"))
      throw new ServiceError(503);
    localStorage.removeItem("stewardship.mock.session");
  }
}
