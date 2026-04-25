import { HttpInterceptorFn } from '@angular/common/http';
import { inject, PLATFORM_ID, REQUEST } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { SSR_API_ORIGIN } from './ssr-api-origin.token';

/**
 * Server-only interceptor that:
 *  1. Prepends the SSR_API_ORIGIN to relative URLs so the Node process can
 *     reach the backend inside the K8s cluster.
 *  2. Forwards the incoming request's Host via X-Forwarded-Host / Proto so
 *     the backend resolves the correct tenant.
 *
 * Must be registered LAST in the interceptor chain so that auth / snake-case
 * interceptors still see the original relative URL.
 */
export const ssrOriginInterceptor: HttpInterceptorFn = (req, next) => {
  const platformId = inject(PLATFORM_ID);
  if (!isPlatformServer(platformId)) return next(req);

  const origin = inject(SSR_API_ORIGIN, { optional: true }) ?? '';
  const incomingRequest: Request | null = inject(REQUEST, { optional: true });

  let url = req.url;
  let headers = req.headers;

  if (origin && !url.startsWith('http')) {
    url = `${origin}${url}`;
  }

  if (incomingRequest) {
    const incoming = new URL(incomingRequest.url);
    headers = headers
      .set('X-Forwarded-Host', incoming.host)
      .set('X-Forwarded-Proto', incoming.protocol.replace(':', ''));
  }

  return next(req.clone({ url, headers }));
};
