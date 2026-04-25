import { describe, it, expect, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { BrandingApplierService } from './branding-applier.service';
import { BrandingConfig } from '../models/tenant.model';
import { ThemeService } from '../theme/theme.service';

function mockDocument() {
  const setProperty = vi.fn();
  return {
    documentElement: { style: { setProperty } },
    querySelector: vi.fn(() => null),
    title: '',
    createElement: vi.fn(() => ({ rel: '', href: '', type: '' })),
    head: { appendChild: vi.fn(), querySelector: vi.fn(() => null) },
  } as unknown as Document;
}

describe('BrandingApplierService', () => {
  let service: BrandingApplierService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        BrandingApplierService,
        { provide: ThemeService, useValue: { applyBranding: vi.fn(), applyTenant: vi.fn() } },
      ],
    });
    service = TestBed.inject(BrandingApplierService);
  });

  it('applies primary and secondary colors as CSS variables', () => {
    const doc = mockDocument();
    const branding: BrandingConfig = {
      primaryColor: '#FF0000',
      secondaryColor: '#00FF00',
      agencyDisplayName: 'Test Agency',
    };
    service.apply(branding, doc);
    expect(doc.documentElement.style.setProperty).toHaveBeenCalledWith('--brand-primary', '#FF0000');
    expect(doc.documentElement.style.setProperty).toHaveBeenCalledWith('--brand-secondary', '#00FF00');
  });

  it('applies agency display name as CSS variable', () => {
    const doc = mockDocument();
    service.apply({ primaryColor: '#F00', secondaryColor: '#0F0', agencyDisplayName: 'My Agency' }, doc);
    expect(doc.documentElement.style.setProperty).toHaveBeenCalledWith('--agency-display-name', '"My Agency"');
  });

  it('does not throw when logoUrl or faviconUrl are absent', () => {
    const doc = mockDocument();
    expect(() =>
      service.apply({ primaryColor: '#F00', secondaryColor: '#0F0', agencyDisplayName: 'X' }, doc)
    ).not.toThrow();
  });
});
