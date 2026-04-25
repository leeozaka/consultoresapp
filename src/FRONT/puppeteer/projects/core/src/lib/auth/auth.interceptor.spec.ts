import { describe, it, expect, afterEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { environment } from '../environments/environment';

describe('authInterceptor', () => {
  let http: HttpTestingController;
  let client: HttpClient;
  let assignSpy: ReturnType<typeof vi.fn>;

  let localLogoutSpy: ReturnType<typeof vi.fn>;

  function setup(token: string | null) {
    localLogoutSpy = vi.fn();
    const mockAuth = { accessToken: signal(token), localLogout: localLogoutSpy };
    assignSpy = vi.fn();

    Object.defineProperty(globalThis, 'location', {
      configurable: true,
      value: {
        href: 'http://custom.consultor.localhost/dashboard/imoveis',
        pathname: '/dashboard/imoveis',
        assign: assignSpy,
      },
    });

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: mockAuth },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
  }

  afterEach(() => {
    http.verify();
    sessionStorage.removeItem('__503_reload');
  });

  it('attaches Authorization header when token is present', () => {
    setup('test-token');
    client.get('/api/test').subscribe();
    const req = http.expectOne('/api/test');
    expect(req.request.headers.get('Authorization')).toBe('Bearer test-token');
    req.flush({});
  });

  it('does not attach header for /connect/* endpoints', () => {
    setup('test-token');
    client.get('/connect/token').subscribe();
    const req = http.expectOne('/connect/token');
    expect(req.request.headers.get('Authorization')).toBeNull();
    req.flush({});
  });

  it('does not attach header when no token', () => {
    setup(null);
    client.get('/api/test').subscribe();
    const req = http.expectOne('/api/test');
    expect(req.request.headers.get('Authorization')).toBeNull();
    req.flush({});
  });

  it('does not attach header for absolute external URLs', () => {
    setup('test-token');
    client.get('http://localhost:4566/property-photos/img.webp').subscribe();
    const req = http.expectOne('http://localhost:4566/property-photos/img.webp');
    expect(req.request.headers.get('Authorization')).toBeNull();
    req.flush({});
  });

  it('does not attach header for HTTPS external URLs', () => {
    setup('test-token');
    client.get('https://cdn.example.com/photo.webp').subscribe();
    const req = http.expectOne('https://cdn.example.com/photo.webp');
    expect(req.request.headers.get('Authorization')).toBeNull();
    req.flush({});
  });

  it('redirects to login with a warning when a protected request returns 401', () => {
    setup('test-token');

    client.get('/api/properties').subscribe({ error: () => undefined });

    const req = http.expectOne('/api/properties');
    req.flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(assignSpy).toHaveBeenCalledOnce();
    expect(assignSpy).toHaveBeenCalledWith(
      `${new URL('/auth/login', environment.oidc.postLogoutRedirectUri)}?returnUrl=${encodeURIComponent('http://custom.consultor.localhost/dashboard/imoveis')}&sessionExpired=1`,
    );
  });

  it('does not redirect when login itself returns 401', () => {
    setup(null);

    client.post('/api/auth/login', {}).subscribe({ error: () => undefined });

    const req = http.expectOne('/api/auth/login');
    req.flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(assignSpy).not.toHaveBeenCalled();
  });

  it('logs out locally and redirects to root when a protected request returns 503', () => {
    setup('test-token');

    client.get('/api/properties').subscribe({ error: () => undefined });

    const req = http.expectOne('/api/properties');
    req.flush({}, { status: 503, statusText: 'Service Unavailable' });

    expect(localLogoutSpy).toHaveBeenCalledOnce();
    expect(assignSpy).toHaveBeenCalledOnce();
    expect(assignSpy).toHaveBeenCalledWith('/');
  });

  it('does not redirect on a second 503 in the same session (circuit breaker)', () => {
    setup('test-token');

    // First 503 — redirect fires
    client.get('/api/properties').subscribe({ error: () => undefined });
    const req1 = http.expectOne('/api/properties');
    req1.flush({}, { status: 503, statusText: 'Service Unavailable' });
    expect(assignSpy).toHaveBeenCalledOnce();

    // Second 503 — circuit breaker prevents redirect
    assignSpy.mockClear();
    localLogoutSpy.mockClear();
    client.get('/api/properties').subscribe({ error: () => undefined });
    const req2 = http.expectOne('/api/properties');
    req2.flush({}, { status: 503, statusText: 'Service Unavailable' });

    expect(localLogoutSpy).not.toHaveBeenCalled();
    expect(assignSpy).not.toHaveBeenCalled();
  });

  it('skips interceptor entirely for /api/tenants/resolve (public path)', () => {
    setup('test-token');
    client.get('/api/tenants/resolve').subscribe();
    const req = http.expectOne('/api/tenants/resolve');
    expect(req.request.headers.get('Authorization')).toBeNull();
    req.flush({});
  });

  it('still attaches Authorization header for /api/tenants/me (not public)', () => {
    setup('test-token');
    client.get('/api/tenants/me').subscribe();
    const req = http.expectOne('/api/tenants/me');
    expect(req.request.headers.get('Authorization')).toBe('Bearer test-token');
    req.flush({});
  });

  it('does not redirect on 503 when already on an auth page', () => {
    localLogoutSpy = vi.fn();
    const mockAuth = { accessToken: signal('test-token'), localLogout: localLogoutSpy };
    assignSpy = vi.fn();

    Object.defineProperty(globalThis, 'location', {
      configurable: true,
      value: {
        href: 'http://custom.consultor.localhost/auth/login',
        pathname: '/auth/login',
        assign: assignSpy,
      },
    });

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: mockAuth },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);

    client.get('/api/properties').subscribe({ error: () => undefined });

    const req = http.expectOne('/api/properties');
    req.flush({}, { status: 503, statusText: 'Service Unavailable' });

    expect(localLogoutSpy).not.toHaveBeenCalled();
    expect(assignSpy).not.toHaveBeenCalled();
  });
});
