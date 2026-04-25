import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { roleGuard } from './role.guard';
import { AuthService } from './auth.service';

describe('roleGuard', () => {
  function setup(roles: string[]) {
    const mockAuth = {
      isAuthenticated: signal(true),
      currentUser: signal({ id: '1', email: 'a@b.com', firstName: 'A', lastName: 'B', tenantId: 't1', roles }),
      hasRole: (role: string) => roles.includes(role),
    };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuth },
      ],
    });
    return TestBed.inject(Router);
  }

  it('allows access when user has the required role', () => {
    setup(['SuperAdmin']);
    const guard = roleGuard(['SuperAdmin']);
    const result = TestBed.runInInjectionContext(() => guard({} as never, {} as never));
    expect(result).toBe(true);
  });

  it('blocks access when user lacks the required role', () => {
    setup(['Agent']);
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'createUrlTree');
    const guard = roleGuard(['SuperAdmin']);
    const result = TestBed.runInInjectionContext(() => guard({} as never, {} as never));
    expect(result).not.toBe(true);
  });

  it('allows access when user has any of the required roles', () => {
    setup(['TenantAdmin']);
    const guard = roleGuard(['TenantAdmin', 'SuperAdmin']);
    const result = TestBed.runInInjectionContext(() => guard({} as never, {} as never));
    expect(result).toBe(true);
  });
});
