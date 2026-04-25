import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, computed, effect, inject, signal } from '@angular/core';
import { PrimeNG } from 'primeng/config';
import { BrandingConfig, PortalLayoutMode, Tenant } from '../models/tenant.model';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly primeNg = inject(PrimeNG);

  private readonly _branding = signal<BrandingConfig | null>(null);
  private readonly _layoutMode = signal<PortalLayoutMode>('Default');

  readonly branding = this._branding.asReadonly();
  readonly layoutMode = this._layoutMode.asReadonly();

  readonly cssVariables = computed<Record<string, string>>(() => {
    const branding = this._branding();
    const primary = branding?.primaryColor ?? '#1A73E8';
    const secondary = branding?.secondaryColor ?? '#F5A623';

    return {
      '--brand-primary': primary,
      '--brand-secondary': secondary,
      '--p-primary-color': primary,
      '--p-highlight-background': secondary,
      '--p-highlight-color': '#ffffff',
      // Custom projects manage their own surfaces; we keep the platform shell neutral.
      '--surface-ground':
        this._layoutMode() === 'Default' ? '#ffffff' : '#f8fafc',
    };
  });

  constructor() {
    effect(() => {
      if (!isPlatformBrowser(this.platformId)) return;

      const root = this.document.documentElement;
      const vars = this.cssVariables();
      for (const [key, value] of Object.entries(vars)) {
        root.style.setProperty(key, value);
      }

      const primary = vars['--brand-primary'];
      const maybePrimeNg = this.primeNg as unknown as {
        updatePrimaryPalette?: (palette: Record<string, string>) => void;
      };

      maybePrimeNg.updatePrimaryPalette?.({ 500: primary });
    });
  }

  applyTenant(tenant: Tenant | null): void {
    this._branding.set(tenant?.branding ?? null);
    this._layoutMode.set(tenant?.portalLayoutMode ?? 'Default');

    if (tenant?.branding?.faviconUrl) {
      this.applyFavicon(tenant.branding.faviconUrl);
    }
  }

  applyBranding(branding: BrandingConfig, layoutMode: PortalLayoutMode = 'Default'): void {
    this._branding.set(branding);
    this._layoutMode.set(layoutMode);

    if (branding.faviconUrl) {
      this.applyFavicon(branding.faviconUrl);
    }
  }

  private applyFavicon(url: string): void {
    let link = this.document.head.querySelector<HTMLLinkElement>('link[rel="icon"]');
    if (!link) {
      link = this.document.createElement('link');
      link.rel = 'icon';
      link.type = 'image/x-icon';
      this.document.head.appendChild(link);
    }
    link.href = url;
  }
}
