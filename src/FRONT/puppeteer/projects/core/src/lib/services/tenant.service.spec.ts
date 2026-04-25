import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TenantService } from './tenant.service';
import { ApiClientService } from '../http/api-client.service';
import { createMockTenant } from '../testing/fixtures';
import { environment } from '../environments/environment';

describe('TenantService', () => {
  let service: TenantService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [TenantService, ApiClientService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TenantService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('getAll() calls GET /api/admin/tenants', () => {
    service.getAll().subscribe();
    const req = http.expectOne(`${environment.apiBaseUrl}/api/admin/tenants`);
    expect(req.request.method).toBe('GET');
    req.flush([createMockTenant()]);
  });

  it('create() calls POST /api/admin/tenants', () => {
    service.create({ name: 'New', slug: 'new', contactEmail: 'a@b.com' }).subscribe();
    const req = http.expectOne(`${environment.apiBaseUrl}/api/admin/tenants`);
    expect(req.request.method).toBe('POST');
    req.flush(createMockTenant());
  });

  it('activate() calls POST /api/admin/tenants/:id/activate', () => {
    service.activate('tenant-1').subscribe();
    const req = http.expectOne(`${environment.apiBaseUrl}/api/admin/tenants/tenant-1/activate`);
    expect(req.request.method).toBe('POST');
    req.flush(createMockTenant());
  });

  it('updateBranding() calls PUT /api/admin/tenants/:id/branding', () => {
    service.updateBranding('tenant-1', { primaryColor: '#F00', secondaryColor: '#0F0', agencyDisplayName: 'X' }).subscribe();
    const req = http.expectOne(`${environment.apiBaseUrl}/api/admin/tenants/tenant-1/branding`);
    expect(req.request.method).toBe('PUT');
    req.flush(createMockTenant());
  });

  it('getBySlug() calls GET /api/tenants/by-slug/:slug', () => {
    service.getBySlug('demo').subscribe();
    const req = http.expectOne(`${environment.apiBaseUrl}/api/tenants/by-slug/demo`);
    expect(req.request.method).toBe('GET');
    req.flush(createMockTenant());
  });
});
