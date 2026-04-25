import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { AccountService } from './account.service';
import { ApiClientService } from '../http/api-client.service';
import { createMockAccount } from '../testing/fixtures';
import { environment } from '../environments/environment';

describe('AccountService', () => {
  let service: AccountService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AccountService, ApiClientService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AccountService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('create() calls POST /api/accounts', () => {
    service.create({ clientId: 'c1', initialBalance: 0, creditLimit: 1000, currency: 'BRL' }).subscribe();
    const req = http.expectOne(`${environment.apiBaseUrl}/api/accounts`);
    expect(req.request.method).toBe('POST');
    req.flush(createMockAccount());
  });

  it('getBalance() calls GET /api/accounts/:id/balance', () => {
    service.getBalance('acc-1').subscribe();
    const req = http.expectOne(`${environment.apiBaseUrl}/api/accounts/acc-1/balance`);
    expect(req.request.method).toBe('GET');
    req.flush(createMockAccount());
  });
});
