import { HttpErrorResponse, HttpInterceptorFn, HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { environment } from '../environments/environment';
import { AuthService } from './auth.service';

/** Paths that never receive an Authorization header */
const PUBLIC_PATHS = ['/connect/', '/.well-known/', '/api/tenants/resolve'];
const LOGIN_API_PATHS = ['/api/auth/login'];
const AUTH_PAGE_PATHS = ['/auth/login', '/auth/callback', '/auth/logout'];

/**
 * sessionStorage key used to rate-limit the 503 hard-reload.
 * A single reload is attempted per browser session; subsequent 503s are
 * swallowed as errors so the app can render a degraded state instead of
 * DDOSing the API with infinite full-page reloads.
 */
const SERVICE_UNAVAILABLE_RELOAD_KEY = '__503_reload';
const RootLoginUrl = new URL('/auth/login', environment.oidc.postLogoutRedirectUri).toString();

function isPublicPath(url: string): boolean {
  return PUBLIC_PATHS.some((path) => url.includes(path));
}

function isLoginApiPath(url: string): boolean {
  return LOGIN_API_PATHS.some((path) => url.includes(path));
}

function isAuthPage(url: string): boolean {
  return AUTH_PAGE_PATHS.some((path) => url.startsWith(path));
}

/** External (absolute) URLs — e.g. S3/R2 image fetches — must not receive API tokens. */
function isAbsoluteUrl(url: string): boolean {
  return url.startsWith('http://') || url.startsWith('https://');
}

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  if (isPublicPath(req.url) || isAbsoluteUrl(req.url)) {
    return next(req);
  }

  const auth = inject(AuthService);
  const token = auth.accessToken();
  const currentUrl = globalThis.location?.href ?? environment.oidc.postLogoutRedirectUri;
  const currentPath = globalThis.location?.pathname ?? '';

  const outgoing = token
    ? req.clone({
        setHeaders: { Authorization: `Bearer ${token}` },
      })
    : req;

  return next(outgoing).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && !isAuthPage(currentPath)) {
        if (error.status === 401 && !isLoginApiPath(req.url)) {
          const loginUrl = new URL(RootLoginUrl);
          loginUrl.searchParams.set('returnUrl', currentUrl);
          loginUrl.searchParams.set('sessionExpired', '1');
          globalThis.location?.assign?.(loginUrl.toString());
        } else if (error.status === 503) {
          const alreadyReloaded = globalThis.sessionStorage?.getItem(SERVICE_UNAVAILABLE_RELOAD_KEY);
          if (!alreadyReloaded) {
            globalThis.sessionStorage?.setItem(SERVICE_UNAVAILABLE_RELOAD_KEY, '1');
            auth.localLogout();
            globalThis.location?.assign?.('/');
          }
        }
      }

      return throwError(() => error);
    }),
  );
};
