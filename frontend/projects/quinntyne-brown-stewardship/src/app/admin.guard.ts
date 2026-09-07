import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { SIGN_IN_SERVICE } from "@qbs/api";
// Authority is read from the session alone. A signed-in participant is turned back to the
// curriculum with an explanation; a visitor is held at sign-in with the route they asked for.
export const adminGuard: CanActivateFn = async (_, state) => {
  const service = inject(SIGN_IN_SERVICE);
  const router = inject(Router);
  try {
    const session = await service.session();
    if (session?.isAdministrator) return true;
    if (session) return router.createUrlTree(["/curriculum"], { queryParams: { refused: "authoring" } });
  } catch {
    return router.createUrlTree(["/sign-in"], { queryParams: { returnUrl: state.url, unavailable: "1" } });
  }
  return router.createUrlTree(["/sign-in"], { queryParams: { returnUrl: state.url } });
};
