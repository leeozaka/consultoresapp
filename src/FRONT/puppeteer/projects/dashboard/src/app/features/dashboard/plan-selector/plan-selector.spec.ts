import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { signal } from '@angular/core';
import { PlanSelectorComponent } from './plan-selector';
import { PlanService, AddonService, BillingService, TenantContextService, PaymentSseService, createMockTenant } from '@consultores/core';

const mockPlans = [
  { id: 'p1', name: 'Starter', description: 'Basic', pricePerMonth: 9900, currencyCode: 'BRL', maxProperties: 10, videoUpload: false, aiDescriptions: false, customDomain: false, premiumAnalytics: false, portalTheme: 'default', isActive: true },
  { id: 'p2', name: 'Pro', description: 'Pro plan', pricePerMonth: 19900, currencyCode: 'BRL', maxProperties: 50, videoUpload: true, aiDescriptions: false, customDomain: false, premiumAnalytics: true, portalTheme: 'premium', isActive: true },
];

describe('PlanSelectorComponent', () => {
  let fixture: ComponentFixture<PlanSelectorComponent>;
  let component: PlanSelectorComponent;

  const mockPlanService = {
    getPlans: vi.fn().mockReturnValue(of([])),
  };

  const mockAddonService = {
    getMyAddons: vi.fn().mockReturnValue(of([])),
  };

  const mockBillingService = {
    getOverview: vi.fn(),
    changePlan: vi.fn(),
    createPortalSession: vi.fn(),
  };

  const mockTenantContext = {
    currentTenant: signal(createMockTenant()),
    refreshFromUser: vi.fn().mockReturnValue(of(null)),
  };

  const mockPaymentSse = {
    lastEvent: signal(null),
  };

  beforeEach(async () => {
    mockBillingService.getOverview.mockReturnValue(of(null));
    mockBillingService.changePlan.mockReset();
    mockPlanService.getPlans.mockReturnValue(of([]));
    mockTenantContext.currentTenant.set(createMockTenant());

    await TestBed.configureTestingModule({
      imports: [PlanSelectorComponent],
      providers: [
        provideRouter([]),
        { provide: PlanService, useValue: mockPlanService },
        { provide: AddonService, useValue: mockAddonService },
        { provide: BillingService, useValue: mockBillingService },
        { provide: TenantContextService, useValue: mockTenantContext },
        { provide: PaymentSseService, useValue: mockPaymentSse },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PlanSelectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('hides next billing date when the subscription is canceled', () => {
    mockTenantContext.currentTenant.set(createMockTenant({
      paymentStatus: 'canceled',
      nextBillingDate: '2026-04-12T00:00:00Z',
    }));
    mockBillingService.getOverview.mockReturnValue(of({
      paymentStatus: 'canceled',
      subscriptionStatus: 'canceled',
      nextPaymentDate: '2026-04-12T00:00:00Z',
      availabilityEndsAt: '2026-04-12T00:00:00Z',
      hasRecurringPayment: false,
      canManageBilling: true,
      recentTransactions: [],
    }));

    component.ngOnInit();
    fixture.detectChanges();

    expect((component as any).nextBillingDate()).toBeNull();
  });

  it('keeps next billing date for active subscriptions', () => {
    mockTenantContext.currentTenant.set(createMockTenant({
      paymentStatus: 'paid',
      nextBillingDate: '2026-04-12T00:00:00Z',
    }));
    mockBillingService.getOverview.mockReturnValue(of({
      paymentStatus: 'paid',
      subscriptionStatus: 'active',
      nextPaymentDate: '2026-04-12T00:00:00Z',
      availabilityEndsAt: '2026-04-12T00:00:00Z',
      hasRecurringPayment: true,
      canManageBilling: true,
      recentTransactions: [],
    }));

    component.ngOnInit();
    fixture.detectChanges();

    expect((component as any).nextBillingDate()).toBe('2026-04-12T00:00:00Z');
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('loads plans and sets isLoading to false', () => {
    mockPlanService.getPlans.mockReturnValue(of(mockPlans));
    component.ngOnInit();
    fixture.detectChanges();
    expect((component as any).plans()).toHaveLength(2);
    expect((component as any).isLoading()).toBe(false);
  });

  it('sets isLoading to false on plan load error', () => {
    mockPlanService.getPlans.mockReturnValue(throwError(() => new Error('fail')));
    component.ngOnInit();
    expect((component as any).isLoading()).toBe(false);
  });

  it('isCurrentPlan returns true for matching plan id', () => {
    mockTenantContext.currentTenant.set(createMockTenant({ planId: 'p1' }));
    mockPlanService.getPlans.mockReturnValue(of(mockPlans));
    component.ngOnInit();
    expect(component['isCurrentPlan'](mockPlans[0])).toBe(true);
    expect(component['isCurrentPlan'](mockPlans[1])).toBe(false);
  });

  it('paymentStatusSeverity returns correct severity for each status', () => {
    const comp = component as any;
    mockTenantContext.currentTenant.set(createMockTenant({ paymentStatus: 'paid' }));
    fixture.detectChanges();
    expect(comp.paymentStatusSeverity()).toBe('success');
  });

  it('paymentStatusSeverity returns warn for canceled', () => {
    const comp = component as any;
    mockTenantContext.currentTenant.set(createMockTenant({ paymentStatus: 'canceled' }));
    fixture.detectChanges();
    expect(comp.paymentStatusSeverity()).toBe('warn');
  });

  it('paymentStatusSeverity returns danger for past_due', () => {
    const comp = component as any;
    mockTenantContext.currentTenant.set(createMockTenant({ paymentStatus: 'past_due' }));
    fixture.detectChanges();
    expect(comp.paymentStatusSeverity()).toBe('danger');
  });

  it('getAddonStatusSeverity maps status correctly', () => {
    expect(component['getAddonStatusSeverity']('Active')).toBe('success');
    expect(component['getAddonStatusSeverity']('PastDue')).toBe('warn');
    expect(component['getAddonStatusSeverity']('Cancelled')).toBe('danger');
    expect(component['getAddonStatusSeverity']('Unknown')).toBe('info');
  });

  it('getAddonStatusLabel maps status correctly', () => {
    expect(component['getAddonStatusLabel']('Active')).toBe('Ativo');
    expect(component['getAddonStatusLabel']('PastDue')).toBe('Pendente');
    expect(component['getAddonStatusLabel']('Cancelled')).toBe('Cancelado');
    expect(component['getAddonStatusLabel']('Other')).toBe('Other');
  });

  it('selectPlan does nothing for current plan', () => {
    mockTenantContext.currentTenant.set(createMockTenant({ planId: 'p1' }));
    mockPlanService.getPlans.mockReturnValue(of(mockPlans));
    component.ngOnInit();
    fixture.detectChanges();

    component['selectPlan'](mockPlans[0]);
    expect(mockBillingService.changePlan).not.toHaveBeenCalled();
  });

  it('selectPlan calls changePlan when confirmed', () => {
    vi.spyOn(globalThis, 'confirm').mockReturnValue(true);
    mockBillingService.changePlan.mockReturnValue(of({ requiresCheckout: false, message: 'ok' }));
    mockTenantContext.currentTenant.set(createMockTenant({ planId: 'p1' }));
    mockPlanService.getPlans.mockReturnValue(of(mockPlans));
    component.ngOnInit();
    fixture.detectChanges();

    component['selectPlan'](mockPlans[1]);
    expect(mockBillingService.changePlan).toHaveBeenCalledWith('p2', expect.any(String), expect.any(String));
  });

  it('selectPlan does nothing when user cancels confirm dialog', () => {
    vi.spyOn(globalThis, 'confirm').mockReturnValue(false);
    mockTenantContext.currentTenant.set(createMockTenant({ planId: 'p1' }));
    mockPlanService.getPlans.mockReturnValue(of(mockPlans));
    component.ngOnInit();
    fixture.detectChanges();

    component['selectPlan'](mockPlans[1]);
    expect(mockBillingService.changePlan).not.toHaveBeenCalled();
  });

  it('selectPlan sets error message on failure', () => {
    vi.spyOn(globalThis, 'confirm').mockReturnValue(true);
    mockBillingService.changePlan.mockReturnValue(throwError(() => ({ error: { detail: 'Billing error' } })));
    mockTenantContext.currentTenant.set(createMockTenant({ planId: 'p1' }));
    mockPlanService.getPlans.mockReturnValue(of(mockPlans));
    component.ngOnInit();
    fixture.detectChanges();

    component['selectPlan'](mockPlans[1]);
    expect((component as any).errorMessage()).toBe('Billing error');
    expect((component as any).isSwitching()).toBeNull();
  });

  it('ngOnDestroy clears refresh interval', () => {
    const clearSpy = vi.spyOn(globalThis, 'clearInterval');
    component.ngOnDestroy();
    expect(clearSpy).toHaveBeenCalled();
  });
});
