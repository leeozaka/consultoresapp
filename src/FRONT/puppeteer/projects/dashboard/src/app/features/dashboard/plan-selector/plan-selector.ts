import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
  computed,
  OnInit,
  OnDestroy,
} from '@angular/core';
import { CurrencyPipe, DatePipe, KeyValuePipe } from '@angular/common';
import { Plan, BillingOverview, PlanService, AddonService, BillingService, TenantContextService, PaymentSseService, LoadingSpinnerComponent } from '@consultores/core';
import { Tag } from 'primeng/tag';

@Component({
  selector: 'app-plan-selector',
  imports: [CurrencyPipe, DatePipe, KeyValuePipe, LoadingSpinnerComponent, Tag],
  templateUrl: './plan-selector.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlanSelectorComponent implements OnInit, OnDestroy {
  private readonly planService = inject(PlanService);
  // private readonly addonService = inject(AddonService);
  private readonly billingService = inject(BillingService);
  private readonly tenantContext = inject(TenantContextService);
  private readonly paymentSse = inject(PaymentSseService);

  protected readonly isLoading = signal(true);
  protected readonly plans = signal<Plan[]>([]);
  protected readonly isSwitching = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);
  protected readonly showPlanPicker = signal(false);
  protected readonly isPayingNow = signal(false);
  protected readonly billingOverview = signal<BillingOverview | null>(null);
  protected readonly isOpeningPortal = signal(false);

  private refreshInterval: ReturnType<typeof setInterval> | null = null;

  protected readonly currentPlanId = computed(() => this.tenantContext.currentTenant()?.planId);

  protected readonly currentPlan = computed(() =>
    this.plans().find((p) => p.id === this.currentPlanId()),
  );

  protected readonly otherPlans = computed(() =>
    this.plans().filter((p) => p.id !== this.currentPlanId() && p.isActive),
  );

  // protected readonly activeAddons = computed(() =>
  //   this.addons().filter((a) => a.status === 'Active'),
  // );

  // protected readonly monthlyPerkTotal = computed(() =>
  //   this.activeAddons().reduce((sum, a) => sum + a.priceAtActivation, 0),
  // );

  protected readonly totalMonthly = computed(() => {
    const plan = this.currentPlan();
    const planPriceCents = plan ? Math.round(plan.pricePerMonth * 100) : 0;
    return planPriceCents;
  });

  protected readonly entitlements = computed(() =>
    this.tenantContext.currentTenant()?.entitlements ?? {},
  );

  protected readonly hasAnyEntitlement = computed(() =>
    Object.values(this.entitlements()).some(Boolean),
  );

  protected readonly paymentStatus = computed(() =>
    this.billingOverview()?.paymentStatus ?? this.tenantContext.currentTenant()?.paymentStatus ?? 'none',
  );

  protected readonly nextBillingDate = computed(() =>
    this.paymentStatus() === 'canceled'
      ? null
      : this.billingOverview()?.nextPaymentDate ?? this.tenantContext.currentTenant()?.nextBillingDate ?? null,
  );

  protected readonly lastPaymentDate = computed(() =>
    this.billingOverview()?.lastPaymentDate ?? this.tenantContext.currentTenant()?.lastPaymentDate ?? null,
  );

  protected readonly hasRecurringPayment = computed(() =>
    this.billingOverview()?.hasRecurringPayment ?? !!this.tenantContext.currentTenant()?.stripeSubscriptionId,
  );

  protected readonly subscriptionStatus = computed(() =>
    this.billingOverview()?.subscriptionStatus ?? this.paymentStatus(),
  );

  protected readonly gracePeriodEndsAt = computed(() =>
    this.billingOverview()?.gracePeriodEndsAt ?? null,
  );

  protected readonly availabilityEndsAt = computed(() =>
    this.billingOverview()?.availabilityEndsAt ?? null,
  );

  protected readonly recentTransactions = computed(() =>
    this.billingOverview()?.recentTransactions ?? [],
  );

  protected readonly canManageBilling = computed(() =>
    this.billingOverview()?.canManageBilling ?? false,
  );

  protected readonly showPayButton = computed(() => {
    const status = this.paymentStatus();
    return (status === 'none' || status === 'past_due') &&
      this.currentPlan() !== undefined &&
      !this.hasRecurringPayment();
  });

  protected readonly lastSseEvent = this.paymentSse.lastEvent;

  ngOnInit(): void {
    this.planService.getPlans().subscribe({
      next: (plans) => {
        this.plans.set(plans);
        this.isLoading.set(false);
        this.loadBillingOverview();
      },
      error: () => this.isLoading.set(false),
    });

    // this.addonService.getMyAddons().subscribe({
    //   next: (addons) => this.addons.set(addons),
    //   error: () => {},
    // });

    this.refreshInterval = setInterval(() => {
      this.tenantContext.refreshFromUser().subscribe();
      this.loadBillingOverview();
    }, 15000);
  }

  ngOnDestroy(): void {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  protected isCurrentPlan(plan: Plan): boolean {
    return this.currentPlanId() === plan.id;
  }

  protected payNow(): void {
    const plan = this.currentPlan();
    if (!plan) return;

    const successUrl = this.buildCheckoutReturnUrl('success');
    const cancelUrl = this.buildCheckoutReturnUrl('cancel');

    this.isPayingNow.set(true);
    this.errorMessage.set(null);

    this.billingService.changePlan(plan.id, successUrl, cancelUrl).subscribe({
      next: (response) => {
        this.isPayingNow.set(false);
        this.successMessage.set(response.message ||
          $localize`:@@plan.pay.processing:Pagamento iniciado. Você receberá uma notificação em instantes.`,
        );
        if (response.requiresCheckout && response.checkoutUrl) {
          globalThis.location.href = response.checkoutUrl;
          return;
        }
        this.handlePlanChangeSuccess();
      },
      error: (err) => {
        this.isPayingNow.set(false);
        const msg = err?.error?.detail ?? err?.error?.title;
        this.errorMessage.set(
          msg ?? $localize`:@@plan.pay.error:Erro ao iniciar pagamento. Tente novamente.`,
        );
      },
    });
  }

  protected selectPlan(plan: Plan): void {
    if (this.isCurrentPlan(plan)) return;

    const successUrl = this.buildCheckoutReturnUrl('success');
    const cancelUrl = this.buildCheckoutReturnUrl('cancel');

    const action = plan.pricePerMonth > (this.currentPlan()?.pricePerMonth ?? 0)
      ? 'fazer upgrade'
      : 'fazer downgrade';

    if (!confirm($localize`:@@plan.confirm.switch:Deseja ${action} para o plano "${plan.name}"?`)) return;

    this.isSwitching.set(plan.id);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.billingService.changePlan(plan.id, successUrl, cancelUrl).subscribe({
      next: (response) => {
        this.isSwitching.set(null);
        this.successMessage.set(
          response.message || $localize`:@@plan.success.changed:Plano alterado para "${plan.name}" com sucesso.`,
        );
        this.showPlanPicker.set(false);
        if (response.requiresCheckout && response.checkoutUrl) {
          globalThis.location.href = response.checkoutUrl;
          return;
        }

        this.handlePlanChangeSuccess();
      },
      error: (err) => {
        this.isSwitching.set(null);
        const msg = err?.error?.detail ?? err?.error?.title;
        this.errorMessage.set(
          msg ?? $localize`:@@plan.error.generic:Erro ao alterar plano. Tente novamente.`,
        );
      },
    });
  }

  protected getAddonStatusSeverity(status: string): 'success' | 'warn' | 'danger' | 'info' {
    switch (status) {
      case 'Active': return 'success';
      case 'PastDue': return 'warn';
      case 'Cancelled': return 'danger';
      default: return 'info';
    }
  }

  protected getAddonStatusLabel(status: string): string {
    switch (status) {
      case 'Active': return $localize`:@@addon.status.active:Ativo`;
      case 'PastDue': return $localize`:@@addon.status.pastDue:Pendente`;
      case 'Cancelled': return $localize`:@@addon.status.cancelled:Cancelado`;
      default: return status;
    }
  }

  protected paymentStatusSeverity(): 'success' | 'warn' | 'danger' | 'info' {
    switch (this.paymentStatus()) {
      case 'paid': return 'success';
      case 'canceled': return 'warn';
      case 'past_due': return 'danger';
      case 'none': return 'info';
      default: return 'info';
    }
  }

  protected paymentStatusLabel(): string {
    switch (this.paymentStatus()) {
      case 'paid': return $localize`:@@plan.paymentStatus.paid:Pago`;
      case 'past_due': return $localize`:@@plan.paymentStatus.pastDue:Pendente`;
      case 'canceled': return $localize`:@@plan.paymentStatus.canceled:Cancelado`;
      case 'none': return $localize`:@@plan.paymentStatus.none:Sem pagamento`;
      default: return this.paymentStatus();
    }
  }

  protected openBillingPortal(): void {
    if (!this.canManageBilling() || this.isOpeningPortal()) return;

    this.isOpeningPortal.set(true);
    this.billingService.createPortalSession(globalThis.location.href).subscribe({
      next: (session) => {
        this.isOpeningPortal.set(false);
        globalThis.location.href = session.url;
      },
      error: (err) => {
        this.isOpeningPortal.set(false);
        const msg = err?.error?.detail ?? err?.error?.title;
        this.errorMessage.set(
          msg ?? $localize`:@@plan.portal.error:Não foi possível abrir o portal de cobrança.`,
        );
      },
    });
  }

  protected subscriptionStatusLabel(): string {
    switch (this.subscriptionStatus()) {
      case 'active': return $localize`:@@plan.subscription.active:Assinatura ativa`;
      case 'trialing': return $localize`:@@plan.subscription.trialing:Período de testes`;
      case 'past_due': return $localize`:@@plan.subscription.pastDue:Pagamento pendente`;
      case 'canceled': return $localize`:@@plan.subscription.canceled:Assinatura cancelada`;
      case 'incomplete': return $localize`:@@plan.subscription.incomplete:Configuração pendente`;
      default: return this.subscriptionStatus();
    }
  }

  protected transactionStatusLabel(status: string): string {
    switch (status) {
      case 'paid': return $localize`:@@plan.transaction.paid:Pago`;
      case 'open': return $localize`:@@plan.transaction.open:Em aberto`;
      case 'draft': return $localize`:@@plan.transaction.draft:Rascunho`;
      case 'void': return $localize`:@@plan.transaction.void:Cancelado`;
      case 'uncollectible': return $localize`:@@plan.transaction.uncollectible:Incobrável`;
      default: return status;
    }
  }

  private handlePlanChangeSuccess(): void {
    this.tenantContext.refreshFromUser().subscribe();
    this.loadBillingOverview();
  }

  private buildCheckoutReturnUrl(outcome: 'success' | 'cancel'): string {
    const url = new URL(globalThis.location.href);
    url.searchParams.set('checkout', outcome);
    return url.toString();
  }

  private loadBillingOverview(): void {
    this.billingService.getOverview().subscribe({
      next: (overview) => this.billingOverview.set(overview),
      error: () => this.billingOverview.set(null),
    });
  }
}
