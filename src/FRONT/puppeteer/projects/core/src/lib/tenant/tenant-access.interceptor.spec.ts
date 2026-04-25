import { describe, it, expect, afterEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { tenantAccessInterceptor } from './tenant-access.interceptor';
import { TenantContextService } from './tenant-context.service';

describe('tenantAccessInterceptor', () => {
  let http: HttpTestingController;
  let client: HttpClient;
  let router: Router;

  const mockTenantContext = {
    blockCurrentTenantAccess: vi.fn(),
  };

  function setup() {
    mockTenantContext.blockCurrentTenantAccess.mockReset();

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([tenantAccessInterceptor])),
        provideHttpClientTesting(),
        { provide: TenantContextService, useValue: mockTenantContext },
      ],
    });

    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
  }

  afterEach(() => http.verify());

  it('redirects to the unavailable route when the API denies access to an inactive tenant', () => {
    setup();

    client.get('/api/properties').subscribe({ error: () => undefined });

    const req = http.expectOne('/api/properties');
    req.flush({ error: 'Tenant is not active' }, { status: 403, statusText: 'Forbidden' });

    expect(mockTenantContext.blockCurrentTenantAccess).toHaveBeenCalledOnce();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/tenant-unavailable');
  });

  it('ignores other forbidden responses', () => {
    setup();

    client.get('/api/properties').subscribe({ error: () => undefined });

    const req = http.expectOne('/api/properties');
    req.flush({ error: 'Feature not enabled' }, { status: 403, statusText: 'Forbidden' });

    expect(mockTenantContext.blockCurrentTenantAccess).not.toHaveBeenCalled();
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });
});
