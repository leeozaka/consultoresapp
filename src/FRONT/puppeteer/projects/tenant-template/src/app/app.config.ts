import {
  ApplicationConfig,
  inject,
  PLATFORM_ID,
  REQUEST,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { provideRouter, withComponentInputBinding, withViewTransitions } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeng/themes/aura';

import { routes } from './app.routes';
import { snakeCaseInterceptor, ssrOriginInterceptor, TenantContextService } from '@consultores/core';

function initializeApp(): Promise<void> {
  const tenantContext = inject(TenantContextService);
  const platformId = inject(PLATFORM_ID);
  const ssrRequest: Request | null = isPlatformServer(platformId)
    ? inject(REQUEST, { optional: true })
    : null;

  return (async () => {
    if (isPlatformServer(platformId) && !ssrRequest) return;
    await new Promise<void>((resolve) => {
      tenantContext.init().subscribe({ complete: () => resolve() });
    });
  })();
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding(), withViewTransitions()),
    provideClientHydration(withEventReplay()),
    provideHttpClient(withFetch(), withInterceptors([snakeCaseInterceptor, ssrOriginInterceptor])),
    provideAnimationsAsync(),
    providePrimeNG({ theme: { preset: Aura, options: { darkModeSelector: '.dark-mode' } } }),
    provideAppInitializer(initializeApp),
  ],
};
