import { ChangeDetectionStrategy, Component, inject, computed, signal, OnInit, viewChild } from '@angular/core';
import { KeyValuePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TenantContextService, TenantService, PlanService, Tenant, Plan, UpdateTenantSettingsRequest, UpdateTenantBrandingRequest, environment } from '@consultores/core';
import { portalUrlForSlug } from './portal-url';
import { ThemeSelectorComponent } from './theme-selector';
import { BrandingFormComponent, type BrandingDraft } from './branding-form';
import { PortalPreviewComponent } from './portal-preview';
import { getThemeById } from '../../portal/theme-registry';

@Component({
  selector: 'app-site-settings',
  imports: [RouterLink, FormsModule, KeyValuePipe, ThemeSelectorComponent, BrandingFormComponent, PortalPreviewComponent],
  templateUrl: './site-settings.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SiteSettingsComponent implements OnInit {
  private readonly tenantContext = inject(TenantContextService);
  private readonly tenantService = inject(TenantService);
  private readonly planService = inject(PlanService);

  protected readonly brandingForm = viewChild<BrandingFormComponent>('brandingForm');

  protected readonly tenant = signal<Tenant | null>(this.tenantContext.currentTenant());
  protected readonly allPlans = signal<Plan[]>([]);
  protected readonly isLoading = signal(!this.tenantContext.currentTenant());
  protected readonly isSaving = signal(false);
  protected readonly branding = computed(() => this.tenant()?.branding);
  protected readonly currentPlan = computed(() => {
    const planId = this.tenant()?.planId;
    if (!planId) return null;
    return this.allPlans().find((p) => p.id === planId) ?? null;
  });
  protected readonly planLabel = computed(() => this.currentPlan()?.name ?? 'Nenhum');
  protected readonly entitlements = computed(() => this.tenant()?.entitlements ?? {});
  protected readonly hasEntitlements = computed(
    () => Object.values(this.entitlements()).some(Boolean)
  );

  protected readonly isCustomFrontend = computed(
    () => this.tenant()?.portalLayoutMode === 'Custom'
  );
  protected readonly customDomain = computed(() => this.tenant()?.customDomain);
  protected readonly frontendOrigin = computed(() => this.tenant()?.frontendOrigin);
  protected readonly portalUrl = computed(() => {
    const t = this.tenant();
    if (!t) return null;
    if (t.customDomain) return `https://${t.customDomain}`;
    return (
      portalUrlForSlug(t.slug) ??
      `${environment.production ? 'https' : 'http'}://${t.slug}.${environment.landingDomain}`
    );
  });
  protected readonly visibleNextBillingDate = computed(() =>
    this.tenant()?.paymentStatus === 'canceled' ? null : this.tenant()?.nextBillingDate ?? null,
  );

  protected readonly portalTheme = computed(() => this.tenant()?.portalTheme ?? 'default');

  // Theme selection (for preview — not persisted until saved)
  protected readonly selectedTheme = signal<string | null>(null);
  protected readonly activeTheme = computed(() => this.selectedTheme() ?? this.portalTheme());

  // Draft branding for live preview
  protected readonly currentDraft = signal<BrandingDraft | null>(null);

  // Editable settings
  protected readonly editName = signal('');
  protected readonly editContactEmail = signal('');
  protected readonly editContactPhone = signal('');

  protected readonly isSavingBranding = signal(false);
  protected readonly isUploadingImage = signal(false);
  protected readonly uploadError = signal<string | null>(null);

  ngOnInit(): void {
    this.planService.getPlans().subscribe({
      next: (plans) => this.allPlans.set(plans),
    });

    if (this.tenant()) {
      this.populateEditable(this.tenant()!);
    } else {
      this.tenantService.getMe().subscribe({
        next: (t) => {
          this.tenant.set(t);
          this.populateEditable(t);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false),
      });
    }
  }

  protected saveSettings(): void {
    this.isSaving.set(true);
    const body: UpdateTenantSettingsRequest = {
      name: this.editName(),
      contactEmail: this.editContactEmail(),
      contactPhone: this.editContactPhone() || undefined,
    };

    this.tenantService.updateSettings(body).subscribe({
      next: (updated) => {
        this.tenant.set(updated);
        this.populateEditable(updated);
        this.isSaving.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }

  private populateEditable(t: Tenant): void {
    this.editName.set(t.name);
    this.editContactEmail.set(t.contactEmail);
    this.editContactPhone.set(t.contactPhone ?? '');
  }

  protected onThemeSelected(themeId: string): void {
    this.selectedTheme.set(themeId);
  }

  protected onDraftChanged(draft: BrandingDraft): void {
    this.currentDraft.set(draft);
  }

  protected saveBranding(): void {
    if (this.isCustomFrontend()) return;

    const draft = this.currentDraft();
    if (!draft) return;

    this.isSavingBranding.set(true);
    const themeFields = getThemeById(this.activeTheme()).fields;

    const body: UpdateTenantBrandingRequest = {
      primaryColor: draft.primaryColor,
      secondaryColor: draft.secondaryColor,
      agencyDisplayName: draft.agencyDisplayName,
      tagline: draft.tagline || undefined,
      logoUrl: draft.logoUrl || undefined,
      portalContent: themeFields.heroSection
        ? {
            heroImageUrl: draft.heroImageUrl || undefined,
            heroHeadline: draft.heroHeadline || undefined,
            heroSubhead: draft.heroSubhead || undefined,
            heroCtaLabel: draft.heroCtaLabel || undefined,
            heroCtaUrl: draft.heroCtaUrl || undefined,
            listingIntro: themeFields.listingIntro ? draft.listingIntro || undefined : undefined,
            secondaryHeroImageUrl: themeFields.secondaryHeroImage ? draft.secondaryHeroImageUrl || undefined : undefined,
            stat1Label: themeFields.stats ? draft.stat1Label || undefined : undefined,
            stat1Value: themeFields.stats ? draft.stat1Value || undefined : undefined,
            stat2Label: themeFields.stats ? draft.stat2Label || undefined : undefined,
            stat2Value: themeFields.stats ? draft.stat2Value || undefined : undefined,
            featureCard1Title: themeFields.featureCards ? draft.featureCard1Title || undefined : undefined,
            featureCard1Description: themeFields.featureCards ? draft.featureCard1Description || undefined : undefined,
            featureCard1Icon: themeFields.featureCards ? draft.featureCard1Icon || undefined : undefined,
            featureCard2Title: themeFields.featureCards ? draft.featureCard2Title || undefined : undefined,
            featureCard2Description: themeFields.featureCards ? draft.featureCard2Description || undefined : undefined,
            featureCard2Icon: themeFields.featureCards ? draft.featureCard2Icon || undefined : undefined,
            featureCard3Title: themeFields.featureCards ? draft.featureCard3Title || undefined : undefined,
            featureCard3Description: themeFields.featureCards ? draft.featureCard3Description || undefined : undefined,
            featureCard3Icon: themeFields.featureCards ? draft.featureCard3Icon || undefined : undefined,
            whatsappNumber: themeFields.whatsapp ? draft.whatsappNumber || undefined : undefined,
            heroSecondaryCta: themeFields.heroSecondaryCta ? draft.heroSecondaryCta || undefined : undefined,
          }
        : undefined,
    };

    this.tenantService.updateMyBranding(body).subscribe({
      next: (updated) => {
        this.tenant.set(updated);
        this.brandingForm()?.populateFromBranding();
        this.tenantContext.refreshFromUser().subscribe();
        this.isSavingBranding.set(false);
      },
      error: () => this.isSavingBranding.set(false),
    });
  }

  protected onPortalImageSelected(event: { target: 'hero' | 'secondary'; event: Event }): void {
    const input = event.event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.isUploadingImage.set(true);
    this.uploadError.set(null);

    this.tenantService.uploadPortalImage(file).subscribe({
      next: (res) => {
        const form = this.brandingForm();
        if (event.target === 'hero') {
          form?.setHeroImageUrl(res.url);
        } else {
          form?.setSecondaryHeroImageUrl(res.url);
        }
        this.isUploadingImage.set(false);
      },
      error: (err) => {
        console.error('Portal image upload failed', err);
        this.uploadError.set('Falha ao enviar imagem. Tente novamente.');
        this.isUploadingImage.set(false);
      },
    });
  }
}
