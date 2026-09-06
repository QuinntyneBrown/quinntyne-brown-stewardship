import { Router } from '@angular/router';
export function redirectToSignIn(router: Router) { return router.navigate(['/sign-in'], { queryParams: { returnUrl: router.url }, replaceUrl: true }); }
