import { Injectable, inject } from '@angular/core';
import { BrandingConfig, Tenant } from '../models/tenant.model';
import { ThemeService } from '../theme/theme.service';

@Injectable({ providedIn: 'root' })
export class BrandingApplierService {
  private readonly themeService = inject(ThemeService);

  applyTenant(tenant: Tenant | null): void {
    this.themeService.applyTenant(tenant);
  }

  apply(branding: BrandingConfig, document: Document): void {
    this.themeService.applyBranding(branding, 'Default');

    const root = document.documentElement;
    root.style.setProperty('--brand-primary', branding.primaryColor);
    root.style.setProperty('--brand-secondary', branding.secondaryColor);
    root.style.setProperty('--agency-display-name', `"${branding.agencyDisplayName}"`);
    if (branding.tagline) root.style.setProperty('--agency-tagline', `"${branding.tagline}"`);
  }
}
