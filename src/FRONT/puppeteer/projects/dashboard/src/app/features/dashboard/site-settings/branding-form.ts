import { ChangeDetectionStrategy, Component, input, output, signal, computed, OnInit, effect } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { getThemeById, type PortalThemeFields } from '../../portal/theme-registry';
import type { BrandingConfig, PortalBrandingContent } from '@consultores/core';

export interface BrandingDraft {
  primaryColor: string;
  secondaryColor: string;
  agencyDisplayName: string;
  tagline: string;
  logoUrl: string;
  heroImageUrl: string;
  heroHeadline: string;
  heroSubhead: string;
  heroCtaLabel: string;
  heroCtaUrl: string;
  listingIntro: string;
  secondaryHeroImageUrl: string;
  stat1Label: string;
  stat1Value: string;
  stat2Label: string;
  stat2Value: string;
  featureCard1Title: string;
  featureCard1Description: string;
  featureCard1Icon: string;
  featureCard2Title: string;
  featureCard2Description: string;
  featureCard2Icon: string;
  featureCard3Title: string;
  featureCard3Description: string;
  featureCard3Icon: string;
  whatsappNumber: string;
  heroSecondaryCta: string;
}

@Component({
  selector: 'app-branding-form',
  imports: [FormsModule],
  templateUrl: './branding-form.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrandingFormComponent implements OnInit {
  readonly selectedThemeId = input.required<string>();
  readonly branding = input<BrandingConfig | undefined>();
  readonly isSaving = input(false);
  readonly isUploading = input(false);
  readonly uploadError = input<string | null>(null);

  readonly saveBranding = output<void>();
  readonly imageSelected = output<{ target: 'hero' | 'secondary'; event: Event }>();
  readonly draftChanged = output<BrandingDraft>();

  protected readonly fields = computed<PortalThemeFields>(
    () => getThemeById(this.selectedThemeId()).fields,
  );

  // Draft signals
  protected readonly draftPrimaryColor = signal('#1A73E8');
  protected readonly draftSecondaryColor = signal('#F5A623');
  protected readonly draftAgencyDisplayName = signal('');
  protected readonly draftTagline = signal('');
  protected readonly draftLogoUrl = signal('');
  protected readonly draftHeroImageUrl = signal('');
  protected readonly draftHeroHeadline = signal('');
  protected readonly draftHeroSubhead = signal('');
  protected readonly draftHeroCtaLabel = signal('');
  protected readonly draftHeroCtaUrl = signal('');
  protected readonly draftListingIntro = signal('');
  protected readonly draftSecondaryHeroImageUrl = signal('');
  protected readonly draftStat1Label = signal('');
  protected readonly draftStat1Value = signal('');
  protected readonly draftStat2Label = signal('');
  protected readonly draftStat2Value = signal('');
  protected readonly draftFeatureCard1Title = signal('');
  protected readonly draftFeatureCard1Description = signal('');
  protected readonly draftFeatureCard1Icon = signal('');
  protected readonly draftFeatureCard2Title = signal('');
  protected readonly draftFeatureCard2Description = signal('');
  protected readonly draftFeatureCard2Icon = signal('');
  protected readonly draftFeatureCard3Title = signal('');
  protected readonly draftFeatureCard3Description = signal('');
  protected readonly draftFeatureCard3Icon = signal('');
  protected readonly draftWhatsappNumber = signal('');
  protected readonly draftHeroSecondaryCta = signal('');

  constructor() {
    // Emit draft on any change via computed effect
    effect(() => {
      const draft = this.currentDraft();
      this.draftChanged.emit(draft);
    });
  }

  protected readonly currentDraft = computed<BrandingDraft>(() => ({
    primaryColor: this.draftPrimaryColor(),
    secondaryColor: this.draftSecondaryColor(),
    agencyDisplayName: this.draftAgencyDisplayName(),
    tagline: this.draftTagline(),
    logoUrl: this.draftLogoUrl(),
    heroImageUrl: this.draftHeroImageUrl(),
    heroHeadline: this.draftHeroHeadline(),
    heroSubhead: this.draftHeroSubhead(),
    heroCtaLabel: this.draftHeroCtaLabel(),
    heroCtaUrl: this.draftHeroCtaUrl(),
    listingIntro: this.draftListingIntro(),
    secondaryHeroImageUrl: this.draftSecondaryHeroImageUrl(),
    stat1Label: this.draftStat1Label(),
    stat1Value: this.draftStat1Value(),
    stat2Label: this.draftStat2Label(),
    stat2Value: this.draftStat2Value(),
    featureCard1Title: this.draftFeatureCard1Title(),
    featureCard1Description: this.draftFeatureCard1Description(),
    featureCard1Icon: this.draftFeatureCard1Icon(),
    featureCard2Title: this.draftFeatureCard2Title(),
    featureCard2Description: this.draftFeatureCard2Description(),
    featureCard2Icon: this.draftFeatureCard2Icon(),
    featureCard3Title: this.draftFeatureCard3Title(),
    featureCard3Description: this.draftFeatureCard3Description(),
    featureCard3Icon: this.draftFeatureCard3Icon(),
    whatsappNumber: this.draftWhatsappNumber(),
    heroSecondaryCta: this.draftHeroSecondaryCta(),
  }));

  ngOnInit(): void {
    this.populateFromBranding();
  }

  populateFromBranding(): void {
    const b = this.branding();
    if (!b) return;
    this.draftPrimaryColor.set(b.primaryColor);
    this.draftSecondaryColor.set(b.secondaryColor);
    this.draftAgencyDisplayName.set(b.agencyDisplayName);
    this.draftTagline.set(b.tagline ?? '');
    this.draftLogoUrl.set(b.logoUrl ?? '');
    const p = b.portalContent;
    this.draftHeroImageUrl.set(p?.heroImageUrl ?? '');
    this.draftHeroHeadline.set(p?.heroHeadline ?? '');
    this.draftHeroSubhead.set(p?.heroSubhead ?? '');
    this.draftHeroCtaLabel.set(p?.heroCtaLabel ?? '');
    this.draftHeroCtaUrl.set(p?.heroCtaUrl ?? '');
    this.draftListingIntro.set(p?.listingIntro ?? '');
    this.draftSecondaryHeroImageUrl.set(p?.secondaryHeroImageUrl ?? '');
    this.draftStat1Label.set(p?.stat1Label ?? '');
    this.draftStat1Value.set(p?.stat1Value ?? '');
    this.draftStat2Label.set(p?.stat2Label ?? '');
    this.draftStat2Value.set(p?.stat2Value ?? '');
    this.draftFeatureCard1Title.set(p?.featureCard1Title ?? '');
    this.draftFeatureCard1Description.set(p?.featureCard1Description ?? '');
    this.draftFeatureCard1Icon.set(p?.featureCard1Icon ?? '');
    this.draftFeatureCard2Title.set(p?.featureCard2Title ?? '');
    this.draftFeatureCard2Description.set(p?.featureCard2Description ?? '');
    this.draftFeatureCard2Icon.set(p?.featureCard2Icon ?? '');
    this.draftFeatureCard3Title.set(p?.featureCard3Title ?? '');
    this.draftFeatureCard3Description.set(p?.featureCard3Description ?? '');
    this.draftFeatureCard3Icon.set(p?.featureCard3Icon ?? '');
    this.draftWhatsappNumber.set(p?.whatsappNumber ?? '');
    this.draftHeroSecondaryCta.set(p?.heroSecondaryCta ?? '');
  }

  /** Set hero image URL externally (after upload). */
  setHeroImageUrl(url: string): void {
    this.draftHeroImageUrl.set(url);
  }

  /** Set secondary hero image URL externally (after upload). */
  setSecondaryHeroImageUrl(url: string): void {
    this.draftSecondaryHeroImageUrl.set(url);
  }

  protected onSave(): void {
    this.saveBranding.emit();
  }

  protected onImageSelected(target: 'hero' | 'secondary', event: Event): void {
    this.imageSelected.emit({ target, event });
  }
}
