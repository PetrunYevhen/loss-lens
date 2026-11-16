import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthState } from './auth-state';

/** Lets a signed-in user through, everyone else goes to the sign-in page and comes back afterwards. */
export const authGuard: CanActivateFn = (_route, state) => {
  if (inject(AuthState).isAuthenticated()) {
    return true;
  }

  const returnUrl = state.url === '/' ? undefined : state.url;
  return inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl } });
};
