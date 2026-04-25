import {
  ApplicationConfig,
  ErrorHandler,
  REQUEST,
  provideBrowserGlobalErrorListeners,
  inject,
  PLATFORM_ID,
  provideAppInitializer,
} from '@angular/core';
import { isPlatformBrowser, isPlatformServer } from '@angular/common';
import {
  provideRouter,
  withComponentInputBinding,
  withViewTransitions,
} from '@angular/router';
import {
  provideHttpClient,
  withFetch,
  withInterceptors,
} from '@angular/common/http';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideOAuthClient } from 'angular-oauth2-oidc';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeng/themes/aura';

import { routes } from './app.routes';
import {
  snakeCaseInterceptor,
  authInterceptor,
  ssrOriginInterceptor,
  tenantAccessInterceptor,
  TenantContextService,
  AuthService,
  ChunkErrorHandler,
} from '@consultores/core';

function initializeApp(): Promise<void> {
  const tenantContext = inject(TenantContextService);
  const auth = inject(AuthService);
  const platformId = inject(PLATFORM_ID);

  // REQUEST is non-null only during runtime SSR (real request).
  // It is null during build-time route extraction and in the browser.
  const ssrRequest: Request | null = isPlatformServer(platformId)
    ? inject(REQUEST, { optional: true })
    : null;

  return (async () => {
    // During build-time route extraction there is no backend to call.
    // REQUEST is null in that phase, so we skip the API call to avoid
    // a hanging HTTP request that causes the "application did not
    // stabilize" timeout.
    if (isPlatformServer(platformId) && !ssrRequest) return;

    // Tenant resolution runs on both runtime-SSR and browser so
    // canMatch guards can pick the correct portal for the host.
    await new Promise<void>((resolve) => {
      tenantContext.init().subscribe({ complete: () => resolve() });
    });

    // Auth requires browser APIs (localStorage, OAuth redirects).
    if (!isPlatformBrowser(platformId)) return;

    try {
      await auth.init();
    } catch (err) {
      console.warn('[AppInit] Auth initialization failed — continuing without authentication.', err);
    }

    // After auth, if the user is authenticated, replace the host-resolved tenant
    // context with their own tenant. On the root domain the host resolves to the
    // landing tenant, which has different entitlements than the user's tenant.
    if (auth.isAuthenticated()) {
      await new Promise<void>((resolve) => {
        tenantContext.refreshFromUser().subscribe({ complete: () => resolve() });
      });
    }
  })();
}

export const appConfig: ApplicationConfig = {
  providers: [
    { provide: ErrorHandler, useClass: ChunkErrorHandler },
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding(), withViewTransitions()),
    provideClientHydration(withEventReplay()),
    provideHttpClient(
      withFetch(),
      withInterceptors([
        snakeCaseInterceptor,
        authInterceptor,
        tenantAccessInterceptor,
        ssrOriginInterceptor,
      ]),
    ),
    provideAnimationsAsync(),
    provideOAuthClient(),
    providePrimeNG({
      theme: {
        preset: Aura,
        options: {
          darkModeSelector: '.dark-mode',
        },
      },
    }),
    provideAppInitializer(initializeApp),
  ],
};
