import { InjectionToken } from '@angular/core';

/**
 * Backend origin used by the SSR engine for API calls.
 *
 * In K8s the frontend Node process cannot reach the API via relative URLs
 * (those resolve to the Angular server itself). This token holds the
 * cluster-internal backend address (e.g. `http://api:8080`) so the SSR
 * interceptor can prepend it before requests leave the server.
 *
 * Provided only in `app.config.server.ts`; on the browser the token is
 * absent and the interceptor no-ops.
 */
export const SSR_API_ORIGIN = new InjectionToken<string>('SSR_API_ORIGIN');
