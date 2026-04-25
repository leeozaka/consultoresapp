import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ApiClientService } from './api-client.service';
import { environment } from '../environments/environment';

describe('ApiClientService', () => {
  let service: ApiClientService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ApiClientService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ApiClientService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('prefixes URLs with the API base URL', () => {
    service.get('/api/properties').subscribe();
    const req = http.expectOne(`${environment.apiBaseUrl}/api/properties`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('does not double-prefix absolute URLs', () => {
    service.get(`${environment.apiBaseUrl}/api/properties`).subscribe();
    const req = http.expectOne(`${environment.apiBaseUrl}/api/properties`);
    req.flush({});
  });

  it('sends POST request with body', () => {
    const body = { title: 'Test' };
    service.post('/api/properties', body).subscribe();
    const req = http.expectOne(`${environment.apiBaseUrl}/api/properties`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({});
  });

  it('sends PUT request with body', () => {
    service.put('/api/properties/1', { title: 'Updated' }).subscribe();
    const req = http.expectOne(`${environment.apiBaseUrl}/api/properties/1`);
    expect(req.request.method).toBe('PUT');
    req.flush({});
  });

  it('sends DELETE request', () => {
    service.delete('/api/properties/1').subscribe();
    const req = http.expectOne(`${environment.apiBaseUrl}/api/properties/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush({});
  });
});
