import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { EntitlementService } from './entitlement.service';
import { TenantContextService } from '../tenant/tenant-context.service';
import { createMockTenant } from '../testing/fixtures';

describe('EntitlementService', () => {
  function setup(entitlements: Record<string, unknown>) {
    const tenant = createMockTenant({ entitlements });
    const mockTenantCtx = { currentTenant: signal(tenant) };
    TestBed.configureTestingModule({
      providers: [
        EntitlementService,
        { provide: TenantContextService, useValue: mockTenantCtx },
      ],
    });
    return TestBed.inject(EntitlementService);
  }

  it('should be created', () => {
    const service = setup({});
    expect(service).toBeTruthy();
  });

  it('should be injectable with entitlements', () => {
    const service = setup({ max_properties: 10, video_upload: true });
    expect(service).toBeInstanceOf(EntitlementService);
  });
});
