import { SessionResult } from "./session-result";
import { SignInResult } from "./sign-in-result";
export interface ISignInService {
  session(): Promise<SessionResult | null>;
  signIn(emailAddress: string, password: string): Promise<SignInResult>;
  signOut(): Promise<void>;
}
