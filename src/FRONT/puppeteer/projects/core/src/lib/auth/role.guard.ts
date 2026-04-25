import { inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { UserRole } from '../models/auth.model';

/**
 * Factory that returns a CanActivateFn / CanMatchFn checking that the
 * current user has at least one of the specified roles.
 */
export function roleGuard(
  allowedRoles: (UserRole | string)[]
): CanActivateFn & CanMatchFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    const hasPermission = allowedRoles.some((role) => auth.hasRole(role));

    if (hasPermission) {
      return true;
    }

    return router.createUrlTree(['/dashboard']);
  };
}
