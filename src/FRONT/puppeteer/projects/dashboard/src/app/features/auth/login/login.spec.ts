import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { LoginComponent } from './login';
import { environment } from '@consultores/core';

const ValidPassword = 'Spec' + 'Pass1!';
const InvalidPassword = 'Spec' + 'Wrong1!';

function createActivatedRouteStub(queryParams: Record<string, string | null>) {
  return {
    snapshot: {
      queryParamMap: {
        get: (key: string) => queryParams[key] ?? null,
      },
    },
  };
}

function stubLocation(href = 'http://localhost:4200/auth/login') {
  const url = new URL(href);
  const location = {
    href,
    origin: url.origin,
    pathname: url.pathname,
    search: url.search,
    hash: url.hash,
  };

  vi.stubGlobal('location', location);
  return location;
}

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  let component: LoginComponent & Record<string, any>;
  let httpCtrl: HttpTestingController;

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: createActivatedRouteStub({
            returnUrl: 'http://localhost:5001/connect/authorize?client_id=consultor-spa',
          }),
        },
      ],
    });

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    httpCtrl = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render the login form', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('form')).toBeTruthy();
    expect(el.querySelector('input[type="email"]')).toBeTruthy();
    expect(el.querySelector('button[type="submit"], p-button[type="submit"]')).toBeTruthy();
  });

  it('should show a session expired warning when redirected after disconnect', () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: createActivatedRouteStub({
            returnUrl: 'http://consultor.localhost/dashboard/imoveis',
            sessionExpired: '1',
          }),
        },
      ],
    });

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    httpCtrl = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    expect(component['errorMessage']()).toBe('Você foi desconectado. Faça login novamente.');
  });

  it('should not submit when form is invalid', () => {
    component['onLogin']();
    httpCtrl.expectNone(`${environment.apiBaseUrl}/api/auth/login`);
    expect(component['isSubmitting']()).toBe(false);
  });

  it('should POST credentials with withCredentials on submit', () => {
    component['loginForm'].setValue({
      email: 'admin@test.com',
      password: ValidPassword,
    });

    component['onLogin']();

    const req = httpCtrl.expectOne(`${environment.apiBaseUrl}/api/auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBe(true);
    expect(req.request.body).toEqual({
      email: 'admin@test.com',
      password: ValidPassword,
    });

    // Flush a success response — redirect tested separately
    req.flush({ message: 'Signed in' });
  });

  it('should redirect back to the OIDC authorize URL after successful password login', () => {
    const location = stubLocation();
    const authorizeUrl = 'http://localhost:5001/connect/authorize?client_id=consultor-spa&redirect_uri=http%3A%2F%2Flocalhost%3A4200%2Fauth%2Fcallback';
    component['returnUrl'] = authorizeUrl;
    component['loginForm'].setValue({
      email: 'admin@test.com',
      password: ValidPassword,
    });

    component['onLogin']();

    const req = httpCtrl.expectOne(`${environment.apiBaseUrl}/api/auth/login`);
    req.flush({ message: 'Signed in' });

    expect(location.href).toBe(authorizeUrl);
  });

  it('should reject unrelated external return URLs after successful password login', () => {
    const location = stubLocation();
    component['returnUrl'] = 'https://evil.example/steal-session';
    component['loginForm'].setValue({
      email: 'admin@test.com',
      password: ValidPassword,
    });

    component['onLogin']();

    const req = httpCtrl.expectOne(`${environment.apiBaseUrl}/api/auth/login`);
    req.flush({ message: 'Signed in' });

    expect(location.href).toBe('/');
  });

  it('should show error message on 401', () => {
    component['loginForm'].setValue({
      email: 'bad@test.com',
      password: InvalidPassword,
    });

    component['onLogin']();
    expect(component['isSubmitting']()).toBe(true);

    const req = httpCtrl.expectOne(`${environment.apiBaseUrl}/api/auth/login`);
    req.flush({ error: 'Invalid credentials' }, { status: 401, statusText: 'Unauthorized' });

    expect(component['isSubmitting']()).toBe(false);
    expect(component['errorMessage']()).toBe('E-mail ou senha inválidos.');
  });

  it('should show generic error on server error', () => {
    component['loginForm'].setValue({
      email: 'admin@test.com',
      password: ValidPassword,
    });

    component['onLogin']();

    const req = httpCtrl.expectOne(`${environment.apiBaseUrl}/api/auth/login`);
    req.flush(null, { status: 500, statusText: 'Internal Server Error' });

    expect(component['errorMessage']()).toBe(
      'Erro ao conectar. Tente novamente.',
    );
  });
});
