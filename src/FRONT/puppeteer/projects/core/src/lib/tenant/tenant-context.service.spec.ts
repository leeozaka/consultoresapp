import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TenantContextService } from './tenant-context.service';
import { BrandingApplierService } from './branding-applier.service';

describe('TenantContextService', () => {
  let service: TenantContextService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        TenantContextService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: BrandingApplierService, useValue: { applyTenant: vi.fn() } },
      ],
    });
    service = TestBed.inject(TenantContextService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('starts with no tenant', () => {
    expect(service.currentTenant()).toBeNull();
  });

  it('isSystemTenant is false before initialization', () => {
    expect(service.isSystemTenant()).toBe(false);
  });

  it('marks system tenant when resolve returns type System', () => {
    service.init().subscribe();

    const req = http.expectOne('/api/tenants/resolve');
    req.flush({
      id: 'tenant-landing',
      name: 'Consultores',
      type: 'System',
      slug: 'landing',
      status: 'Active',
      portalLayoutMode: 'Default',
      portalTheme: 'default',
      contactEmail: 'contato@consultor.dev',
      branding: { primaryColor: '#1A73E8', secondaryColor: '#F5A623', agencyDisplayName: 'Consultores' },
      entitlements: {},
      createdAt: '2026-01-01T00:00:00Z',
    });

    expect(service.isSystemTenant()).toBe(true);
  });

  it('marks agency tenants as unavailable when resolve returns a suspended tenant', () => {
    service.init().subscribe();

    const req = http.expectOne('/api/tenants/resolve');
    req.flush({
      id: 'tenant-suspended',
      name: 'Agency Suspensa',
      type: 'Agency',
      slug: 'agency-suspensa',
      status: 'Suspended',
      portalLayoutMode: 'Default',
      portalTheme: 'default',
      contactEmail: 'contato@agency-suspensa.dev',
      branding: { primaryColor: '#1A73E8', secondaryColor: '#F5A623', agencyDisplayName: 'Agency Suspensa' },
      entitlements: {},
      createdAt: '2026-01-01T00:00:00Z',
    });

    expect(service.isTenantAccessible()).toBe(false);
    expect(service.isInactiveTenant()).toBe(true);
  });

  it('can block an active tenant after a backend access denial', () => {
    service.init().subscribe();

    const req = http.expectOne('/api/tenants/resolve');
    req.flush({
      id: 'tenant-active',
      name: 'Agency Ativa',
      type: 'Agency',
      slug: 'agency-ativa',
      status: 'Active',
      portalLayoutMode: 'Default',
      portalTheme: 'default',
      contactEmail: 'contato@agency-ativa.dev',
      branding: { primaryColor: '#1A73E8', secondaryColor: '#F5A623', agencyDisplayName: 'Agency Ativa' },
      entitlements: {},
      createdAt: '2026-01-01T00:00:00Z',
    });

    service.blockCurrentTenantAccess();

    expect(service.isTenantAccessible()).toBe(false);
    expect(service.isInactiveTenant()).toBe(true);
  });
});
