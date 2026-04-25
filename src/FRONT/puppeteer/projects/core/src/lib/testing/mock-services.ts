import { signal } from '@angular/core';
import { of } from 'rxjs';
import { CurrentUser } from '../models/auth.model';
import { Tenant } from '../models/tenant.model';
import { createMockUser, createMockTenant } from './fixtures';

/** Minimal spy function type — compatible with Vitest, Jest, or plain stubs. */
type SpyFn = (...args: unknown[]) => unknown;

/**
 * Creates a no-op spy function with a `.mockReturnValue` helper.
 * In test environments where `vi` (Vitest) or `jest` are available,
 * prefer those; this fallback keeps the library compilable without them.
 */
function createSpy(): SpyFn & { mockReturnValue: (val: unknown) => SpyFn } {
  const fn = (() => undefined) as SpyFn & { mockReturnValue: (val: unknown) => SpyFn };
  fn.mockReturnValue = (val: unknown) => {
    const wrapped = (() => val) as SpyFn & { mockReturnValue: (val: unknown) => SpyFn };
    wrapped.mockReturnValue = fn.mockReturnValue;
    return wrapped;
  };
  return fn;
}

/** Resolve spy factory: use Vitest `vi.fn()` when available, else fallback. */
function spyFn(): SpyFn & { mockReturnValue: (val: unknown) => SpyFn } {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const g = globalThis as any;
  if (typeof g.vi?.fn === 'function') return g.vi.fn();
  if (typeof g.jest?.fn === 'function') return g.jest.fn();
  return createSpy();
}

/** Mock factory for AuthService */
export function createMockAuthService(overrides: Partial<{
  isAuthenticated: boolean;
  currentUser: CurrentUser | null;
}> = {}) {
  const isAuth = overrides.isAuthenticated ?? false;
  const user = overrides.currentUser ?? (isAuth ? createMockUser() : null);
  return {
    isAuthenticated: signal(isAuth),
    currentUser: signal(user),
    accessToken: signal<string | null>(isAuth ? 'mock-token' : null),
    login: spyFn(),
    logout: spyFn().mockReturnValue(of(void 0)),
    silentRefresh: spyFn().mockReturnValue(of(void 0)),
  };
}

/** Mock factory for TenantContextService */
export function createMockTenantContextService(overrides: Partial<{
  currentTenant: Tenant | null;
  isLandingPage: boolean;
  isLoading: boolean;
}> = {}) {
  const tenant = overrides.currentTenant ?? createMockTenant();
  return {
    currentTenant: signal(overrides.currentTenant !== undefined ? overrides.currentTenant : tenant),
    isLandingPage: signal(overrides.isLandingPage ?? false),
    isLoading: signal(overrides.isLoading ?? false),
    init: spyFn().mockReturnValue(of(tenant)),
  };
}

/** Mock factory for PropertyService */
export function createMockPropertyService() {
  return {
    search: spyFn().mockReturnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 12, totalPages: 0 })),
    getById: spyFn().mockReturnValue(of(null)),
    create: spyFn().mockReturnValue(of(null)),
    update: spyFn().mockReturnValue(of(null)),
    publish: spyFn().mockReturnValue(of(null)),
    uploadImage: spyFn().mockReturnValue(of(null)),
    getFeatured: spyFn().mockReturnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 6, totalPages: 0 })),
  };
}
