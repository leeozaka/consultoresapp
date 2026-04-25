import { ChangeDetectionStrategy, Component, inject, signal, OnInit, input, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { Tenant, Plan, UpdateTenantBrandingRequest, UpdateTenantRequest, TenantService, UserAdminService, UserRecord, PlanService, LoadingSpinnerComponent, TenantStatusPipe } from '@consultores/core';

@Component({
  selector: 'app-tenant-detail',
  imports: [RouterLink, DatePipe, FormsModule, LoadingSpinnerComponent, TenantStatusPipe],
  templateUrl: './tenant-detail.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TenantDetailComponent implements OnInit {
  readonly id = input.required<string>();

  private readonly tenantService = inject(TenantService);
  private readonly userAdminService = inject(UserAdminService);
  private readonly planService = inject(PlanService);
  private readonly sanitizer = inject(DomSanitizer);

  protected readonly tenant = signal<Tenant | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isSavingBranding = signal(false);
  protected readonly isSavingDetails = signal(false);
  protected readonly isSavingEntitlements = signal(false);
  protected readonly isChangingPlan = signal(false);
  protected readonly isArchiving = signal(false);

  // Details form
  protected readonly editName = signal('');
  protected readonly editSlug = signal('');
  protected readonly editContactEmail = signal('');
  protected readonly editContactPhone = signal('');
  protected readonly editCustomDomain = signal('');
  protected readonly editNextBillingDate = signal('');

  // Branding form
  protected readonly agencyDisplayName = signal('');
  protected readonly primaryColor = signal('#1A73E8');
  protected readonly secondaryColor = signal('#F5A623');
  protected readonly tagline = signal('');

  // Entitlements
  protected readonly editMaxProperties = signal(10);
  protected readonly editVideoUpload = signal(false);
  protected readonly editAiDescriptions = signal(false);
  protected readonly editCustomDomainEntitlement = signal(false);
  protected readonly editPremiumAnalytics = signal(false);
  protected readonly editPortalTheme = signal('default');

  // Plan assignment
  protected readonly plans = signal<Plan[]>([]);
  protected readonly selectedPlanId = signal('');

  // Owner user + tenant users
  protected readonly ownerUser = signal<UserRecord | null>(null);
  protected readonly tenantUsers = signal<UserRecord[]>([]);

  readonly portalThemeOptions = [
    { label: 'Default', value: 'default' },
    { label: 'Minimal', value: 'minimal' },
    { label: 'Premium', value: 'premium' },
  ];

  ngOnInit(): void {
    // Load plans in parallel
    this.planService.getAllPlans().subscribe({ next: (p) => this.plans.set(p) });

    this.tenantService.getById(this.id()).subscribe({
      next: (t) => {
        this.tenant.set(t);
        this.populateDetails(t);
        this.populateBranding(t);
        this.populateEntitlements(t);
        this.selectedPlanId.set(t.planId ?? '');
        this.loadRelatedData(t);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }

  protected saveDetails(): void {
    const tenant = this.tenant();
    if (!tenant) return;

    this.isSavingDetails.set(true);
    const body: UpdateTenantRequest = {
      name: this.editName(),
      slug: this.editSlug(),
      contactEmail: this.editContactEmail(),
      contactPhone: this.editContactPhone() || undefined,
      customDomain: this.editCustomDomain() || undefined,
      nextBillingDate: this.editNextBillingDate() || undefined,
    };

    this.tenantService.update(tenant.id, body).subscribe({
      next: (updated) => {
        this.tenant.set(updated);
        this.populateDetails(updated);
        this.isSavingDetails.set(false);
      },
      error: () => this.isSavingDetails.set(false),
    });
  }

  protected saveBranding(): void {
    const tenant = this.tenant();
    if (!tenant) return;

    this.isSavingBranding.set(true);
    const body: UpdateTenantBrandingRequest = {
      agencyDisplayName: this.agencyDisplayName(),
      primaryColor: this.primaryColor(),
      secondaryColor: this.secondaryColor(),
      tagline: this.tagline() || undefined,
    };

    this.tenantService.updateBranding(tenant.id, body).subscribe({
      next: (updated) => {
        this.tenant.set(updated);
        this.isSavingBranding.set(false);
      },
      error: () => this.isSavingBranding.set(false),
    });
  }

  protected saveEntitlements(): void {
    const tenant = this.tenant();
    if (!tenant) return;

    this.isSavingEntitlements.set(true);
    const entitlements: Record<string, unknown> = {
      max_properties: this.editMaxProperties(),
      video_upload: this.editVideoUpload(),
      ai_descriptions: this.editAiDescriptions(),
      custom_domain: this.editCustomDomainEntitlement(),
      premium_analytics: this.editPremiumAnalytics(),
      portal_theme: this.editPortalTheme(),
    };

    this.tenantService.updateEntitlements(tenant.id, { entitlements }).subscribe({
      next: (updated) => {
        this.tenant.set(updated);
        this.populateEntitlements(updated);
        this.isSavingEntitlements.set(false);
      },
      error: () => this.isSavingEntitlements.set(false),
    });
  }

  protected changePlan(): void {
    const tenant = this.tenant();
    const planId = this.selectedPlanId();
    if (!tenant || !planId) return;

    this.isChangingPlan.set(true);
    this.tenantService.assignPlan(tenant.id, { planId }).subscribe({
      next: (updated) => {
        this.tenant.set(updated);
        this.populateEntitlements(updated);
        this.isChangingPlan.set(false);
      },
      error: () => this.isChangingPlan.set(false),
    });
  }

  protected activateTenant(): void {
    const tenant = this.tenant();
    if (!tenant) return;
    this.tenantService.activate(tenant.id).subscribe({ next: (updated) => this.tenant.set(updated) });
  }

  protected archiveTenant(): void {
    const tenant = this.tenant();
    if (!tenant) return;
    if (!confirm(`Arquivar "${tenant.name}"? Esta ação remove o acesso ao portal permanentemente.`)) return;

    this.isArchiving.set(true);
    this.tenantService.archive(tenant.id).subscribe({
      next: (updated) => {
        this.tenant.set(updated);
        this.isArchiving.set(false);
      },
      error: () => this.isArchiving.set(false),
    });
  }

  protected currentPlanName(): string {
    const planId = this.tenant()?.planId;
    if (!planId) return 'Sem plano';
    return this.plans().find((p) => p.id === planId)?.name ?? 'Plano desconhecido';
  }

  protected portalUrl(): string {
    const t = this.tenant();
    if (!t) return '';
    if (t.customDomain) return `https://${t.customDomain}`;
    return `https://${t.slug}.consultor.app`;
  }

  protected readonly safePortalUrl = computed<SafeResourceUrl>(() =>
    this.sanitizer.bypassSecurityTrustResourceUrl(this.portalUrl())
  );

  private loadRelatedData(t: Tenant): void {
    if (t.ownerUserId) {
      this.userAdminService.getById(t.ownerUserId).subscribe({
        next: (u) => this.ownerUser.set(u),
        error: () => {},
      });
    }

    this.userAdminService.getUsersByTenant(t.id).subscribe({
      next: (users) => this.tenantUsers.set(users),
      error: () => {},
    });
  }

  private populateDetails(t: Tenant): void {
    this.editName.set(t.name);
    this.editSlug.set(t.slug);
    this.editContactEmail.set(t.contactEmail);
    this.editContactPhone.set(t.contactPhone ?? '');
    this.editCustomDomain.set(t.customDomain ?? '');
    this.editNextBillingDate.set(t.nextBillingDate ? t.nextBillingDate.substring(0, 10) : '');
  }

  private populateBranding(t: Tenant): void {
    this.agencyDisplayName.set(t.branding.agencyDisplayName);
    this.primaryColor.set(t.branding.primaryColor);
    this.secondaryColor.set(t.branding.secondaryColor);
    this.tagline.set(t.branding.tagline ?? '');
  }

  private populateEntitlements(t: Tenant): void {
    const e = t.entitlements;
    this.editMaxProperties.set((e['max_properties'] as number) ?? 10);
    this.editVideoUpload.set(!!(e['video_upload'] as boolean));
    this.editAiDescriptions.set(!!(e['ai_descriptions'] as boolean));
    this.editCustomDomainEntitlement.set(!!(e['custom_domain'] as boolean));
    this.editPremiumAnalytics.set(!!(e['premium_analytics'] as boolean));
    this.editPortalTheme.set((e['portal_theme'] as string) ?? 'default');
  }
}
