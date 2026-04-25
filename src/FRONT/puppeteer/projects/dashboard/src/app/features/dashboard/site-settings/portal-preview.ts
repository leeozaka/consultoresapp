import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  input,
  PLATFORM_ID,
  ViewContainerRef,
  viewChild,
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { getThemeById } from '../../portal/theme-registry';
import type { Tenant, BrandingConfig, PortalBrandingContent } from '@consultores/core';
import type { BrandingDraft } from './branding-form';

@Component({
  selector: 'app-portal-preview',
  template: `
    <div class="overflow-hidden rounded-xl border shadow-sm" style="border-color: #e2e8f0; height: 500px">
      <div class="origin-top-left" style="transform: scale(0.4); width: 250%; pointer-events: none">
        <ng-container #previewHost />
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PortalPreviewComponent {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly destroyRef = inject(DestroyRef);
  private readonly previewHost = viewChild('previewHost', { read: ViewContainerRef });
  private destroyed = false;

  readonly selectedThemeId = input.required<string>();
  readonly draft = input<BrandingDraft | null>(null);
  readonly baseTenant = input<Tenant | null>(null);

  protected readonly mockTenant = computed<Tenant | null>(() => {
    const base = this.baseTenant();
    if (!base) return null;

    const d = this.draft();
    if (!d) return base;

    const portalContent: PortalBrandingContent = {
      heroImageUrl: d.heroImageUrl || undefined,
      heroHeadline: d.heroHeadline || undefined,
      heroSubhead: d.heroSubhead || undefined,
      heroCtaLabel: d.heroCtaLabel || undefined,
      heroCtaUrl: d.heroCtaUrl || undefined,
      listingIntro: d.listingIntro || undefined,
      secondaryHeroImageUrl: d.secondaryHeroImageUrl || undefined,
      stat1Label: d.stat1Label || undefined,
      stat1Value: d.stat1Value || undefined,
      stat2Label: d.stat2Label || undefined,
      stat2Value: d.stat2Value || undefined,
      featureCard1Title: d.featureCard1Title || undefined,
      featureCard1Description: d.featureCard1Description || undefined,
      featureCard1Icon: d.featureCard1Icon || undefined,
      featureCard2Title: d.featureCard2Title || undefined,
      featureCard2Description: d.featureCard2Description || undefined,
      featureCard2Icon: d.featureCard2Icon || undefined,
      featureCard3Title: d.featureCard3Title || undefined,
      featureCard3Description: d.featureCard3Description || undefined,
      featureCard3Icon: d.featureCard3Icon || undefined,
      whatsappNumber: d.whatsappNumber || undefined,
      heroSecondaryCta: d.heroSecondaryCta || undefined,
    };

    const branding: BrandingConfig = {
      primaryColor: d.primaryColor,
      secondaryColor: d.secondaryColor,
      agencyDisplayName: d.agencyDisplayName,
      tagline: d.tagline || undefined,
      logoUrl: d.logoUrl || undefined,
      portalContent,
    };

    return { ...base, branding };
  });

  constructor() {
    this.destroyRef.onDestroy(() => { this.destroyed = true; });

    effect(() => {
      if (!isPlatformBrowser(this.platformId)) return;

      const host = this.previewHost();
      if (!host) return;

      const themeId = this.selectedThemeId();
      const tenant = this.mockTenant();
      const theme = getThemeById(themeId);

      host.clear();
      theme.layoutLoader().then((componentType) => {
        if (this.destroyed) return;
        const ref = host.createComponent(componentType);
        if (tenant) {
          ref.setInput('previewTenant', tenant);
        }
      });
    });
  }
}
