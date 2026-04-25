import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { authGuard } from './auth.guard';
import { AuthService } from './auth.service';

function createMockAuth(isAuthenticated: boolean) {
  return {
    isAuthenticated: signal(isAuthenticated),
    accessToken: signal<string | null>(isAuthenticated ? 'token' : null),
    login: vi.fn(),
  };
}

describe('authGuard', () => {
  let router: Router;

  function setup(authenticated: boolean) {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: createMockAuth(authenticated) },
      ],
    });
    router = TestBed.inject(Router);
  }

  it('allows navigation when user is authenticated', () => {
    setup(true);
    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));
    expect(result).toBe(true);
  });

  it('redirects to /auth/login with returnUrl when user is not authenticated', () => {
    setup(false);
    vi.spyOn(router, 'url', 'get').mockReturnValue('/dashboard/meu-site');
    const navigateSpy = vi.spyOn(router, 'createUrlTree');
    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));
    expect(result).not.toBe(true);
    expect(navigateSpy).toHaveBeenCalledWith(['/auth/login'], {
      queryParams: { returnUrl: '/dashboard/meu-site' },
    });
  });
});
