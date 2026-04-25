import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpResponse } from '@angular/common/http';
import { map } from 'rxjs/operators';

export function toSnakeCase(str: string): string {
  return str
    .replace(/([A-Z]+)([A-Z][a-z])/g, '$1_$2')
    .replace(/([a-z\d])([A-Z])/g, '$1_$2')
    .toLowerCase();
}

export function toCamelCase(str: string): string {
  return str.replace(/_([a-z])/g, (_, letter: string) => letter.toUpperCase());
}

export function convertKeysToSnakeCase(value: unknown): unknown {
  if (
    value instanceof FormData ||
    value instanceof Blob ||
    value instanceof ArrayBuffer ||
    value instanceof URLSearchParams
  ) {
    return value;
  }

  if (Array.isArray(value)) {
    return value.map(convertKeysToSnakeCase);
  }
  if (value !== null && typeof value === 'object') {
    return Object.fromEntries(
      Object.entries(value as Record<string, unknown>).map(([k, v]) => [
        toSnakeCase(k),
        convertKeysToSnakeCase(v),
      ])
    );
  }
  return value;
}

export function convertKeysToCamelCase(value: unknown): unknown {
  if (Array.isArray(value)) {
    return value.map(convertKeysToCamelCase);
  }
  if (value !== null && typeof value === 'object') {
    return Object.fromEntries(
      Object.entries(value as Record<string, unknown>).map(([k, v]) => [
        toCamelCase(k),
        convertKeysToCamelCase(v),
      ])
    );
  }
  return value;
}

// ── OIDC paths ──────────────────────

const BYPASS_PATHS = ['/.well-known/', '/connect/'];

function shouldBypassTransformation(url: string): boolean {
  return BYPASS_PATHS.some((path) => url.includes(path));
}

// ── Interceptor ──────────────────────────────────────────────────────────────

export const snakeCaseInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  // Skip key transformation for OIDC / OpenID Connect endpoints
  if (shouldBypassTransformation(req.url)) {
    return next(req);
  }

  // Convert outgoing request body from camelCase → snake_case
  let outgoing = req;
  if (req.body !== null && req.body !== undefined) {
    outgoing = req.clone({ body: convertKeysToSnakeCase(req.body) });
  }

  return next(outgoing).pipe(
    map((event) => {
      if (
        event instanceof HttpResponse &&
        event.body !== null &&
        typeof event.body === 'object' &&
        !(event.body instanceof Blob) &&
        !(event.body instanceof ArrayBuffer)
      ) {
        return event.clone({ body: convertKeysToCamelCase(event.body) });
      }
      return event;
    })
  );
};
