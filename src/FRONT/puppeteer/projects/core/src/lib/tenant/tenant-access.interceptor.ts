import { isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject, PLATFORM_ID } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { TenantContextService } from './tenant-context.service';

function extractErrorMessage(error: HttpErrorResponse): string {
  if (typeof error.error === 'string') {
    return error.error;
  }

  return error.error?.error ?? error.error?.detail ?? error.message ?? '';
}

function isInactiveTenantError(error: HttpErrorResponse): boolean {
  return extractErrorMessage(error).toLowerCase().includes('tenant is not active');
}

export const tenantAccessInterceptor: HttpInterceptorFn = (req, next) => {
  const platformId = inject(PLATFORM_ID);
  const router = inject(Router);
  const tenantContext = inject(TenantContextService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (
        isPlatformBrowser(platformId)
        && error instanceof HttpErrorResponse
        && error.status === 403
        && isInactiveTenantError(error)
      ) {
        tenantContext.blockCurrentTenantAccess(extractErrorMessage(error));
        void router.navigateByUrl('/tenant-unavailable');
      }

      return throwError(() => error);
    }),
  );
};
