import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { SIGN_IN_SERVICE } from "@qbs/api";
export const authGuard: CanActivateFn = async (_, state) => {
  const service = inject(SIGN_IN_SERVICE);
  const router = inject(Router);
  try {
    if (await service.session()) return true;
  } catch {
    return router.createUrlTree(["/sign-in"], {
      queryParams: { returnUrl: state.url, unavailable: "1" },
    });
  }
  return router.createUrlTree(["/sign-in"], {
    queryParams: { returnUrl: state.url },
  });
};
