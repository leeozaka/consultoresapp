import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { OAuthService } from 'angular-oauth2-oidc';
import { Subject } from 'rxjs';
import { AuthService } from './auth.service';

function createMockOAuth(authenticated = false) {
  return {
    configure: vi.fn(),
    events: new Subject(),
    loadDiscoveryDocument: vi.fn(() => Promise.resolve()),
    loadDiscoveryDocumentAndTryLogin: vi.fn(() => Promise.resolve()),
    tryLogin: vi.fn(() => Promise.resolve()),
    hasValidAccessToken: vi.fn(() => authenticated),
    getAccessToken: vi.fn(() => authenticated ? 'mock-token' : null),
    getIdentityClaims: vi.fn(() =>
      authenticated
        ? { sub: '1', email: 'a@b.com', given_name: 'A', family_name: 'B', tenant_id: 't1', role: 'TenantAdmin' }
        : null,
    ),
    loadUserProfile: vi.fn(() => Promise.resolve({})),
    logOut: vi.fn(),
    initCodeFlow: vi.fn(),
    revokeTokenAndLogout: vi.fn(() => Promise.resolve()),
  };
}

describe('AuthService', () => {
  let service: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        { provide: OAuthService, useValue: createMockOAuth() },
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(AuthService);
  });

  it('starts unauthenticated', () => {
    expect(service.isAuthenticated()).toBe(false);
  });

  it('starts with null currentUser', () => {
    expect(service.currentUser()).toBeNull();
  });

  it('returns null accessToken initially', () => {
    expect(service.accessToken()).toBeNull();
  });

  it('exposes hasRole that returns false when no user', () => {
    expect(service.hasRole('SuperAdmin')).toBe(false);
  });

  it('exposes hasRole that returns true when user has role', async () => {
    // Rebuild TestBed with an authenticated OAuth mock
    const authMock = createMockOAuth(true);
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        { provide: OAuthService, useValue: authMock },
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    const authService = TestBed.inject(AuthService);
    await authService.init();

    expect(authService.hasRole('TenantAdmin')).toBe(true);
    expect(authService.hasRole('SuperAdmin')).toBe(false);
  });

  it('exposes isSuperAdmin computed signal', () => {
    expect(service.isSuperAdmin()).toBe(false);
  });
});
