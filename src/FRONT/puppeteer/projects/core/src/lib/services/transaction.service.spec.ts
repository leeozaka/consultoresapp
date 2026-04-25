import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TransactionService } from './transaction.service';
import { ApiClientService } from '../http/api-client.service';
import { environment } from '../environments/environment';

describe('TransactionService', () => {
  let service: TransactionService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [TransactionService, ApiClientService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TransactionService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('create() calls POST /api/transactions', () => {
    const req = { operation: 'credit', accountId: 'acc-1', amount: 100, currency: 'BRL', referenceId: 'ref-1' };
    service.create(req).subscribe();
    const httpReq = http.expectOne(`${environment.apiBaseUrl}/api/transactions`);
    expect(httpReq.request.method).toBe('POST');
    httpReq.flush({ transaction_id: 'tx-1', status: 'Success', balance: 100, reserved_balance: 0, available_balance: 100, timestamp: '2025-01-01' });
  });

  it('createBatch() calls POST /api/transactions/batch', () => {
    const reqs = [
      { operation: 'credit', accountId: 'acc-1', amount: 100, currency: 'BRL', referenceId: 'ref-1' },
      { operation: 'debit', accountId: 'acc-2', amount: 50, currency: 'BRL', referenceId: 'ref-2' },
    ];
    service.createBatch(reqs).subscribe();
    const httpReq = http.expectOne(`${environment.apiBaseUrl}/api/transactions/batch`);
    expect(httpReq.request.method).toBe('POST');
    httpReq.flush([]);
  });
});
