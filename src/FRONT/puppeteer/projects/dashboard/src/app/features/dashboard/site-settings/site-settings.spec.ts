import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { SiteSettingsComponent } from './site-settings';
import { TenantContextService, TenantService, PlanService, createMockTenant, type Plan } from '@consultores/core';

describe('SiteSettingsComponent', () => {
  let fixture: ComponentFixture<SiteSettingsComponent>;

  const mockTenantContext = {
    currentTenant: signal(createMockTenant({ planId: 'plan-1' })),
    refreshFromUser: vi.fn().mockReturnValue(of(null)),
  };
  const mockTenantService = {
    getMe: vi.fn(),
    updateSettings: vi.fn(),
    updateMyBranding: vi.fn(),
    uploadPortalImage: vi.fn(),
  };
  const mockPlan: Plan = {
    id: 'plan-1',
    name: 'Plano Demo',
    description: '',
    pricePerMonth: 0,
    currencyCode: 'BRL',
    maxProperties: 100,
    videoUpload: false,
    aiDescriptions: false,
    customDomain: false,
    premiumAnalytics: false,
    portalTheme: 'default',
    isActive: true,
  };
  const mockPlanService = {
    getPlans: vi.fn().mockReturnValue(of([mockPlan])),
  };

  beforeEach(async () => {
    mockPlanService.getPlans.mockReturnValue(of([mockPlan]));
    mockTenantContext.currentTenant.set(createMockTenant({ planId: 'plan-1' }));

    await TestBed.configureTestingModule({
      imports: [SiteSettingsComponent],
      providers: [
        provideRouter([]),
        { provide: TenantContextService, useValue: mockTenantContext },
        { provide: TenantService, useValue: mockTenantService },
        { provide: PlanService, useValue: mockPlanService },
      ],
    }).compileComponents();
  });

  async function renderWithTenant(overrides: Parameters<typeof createMockTenant>[0] = {}) {
    mockTenantContext.currentTenant.set(createMockTenant({ planId: 'plan-1', ...overrides }));
    fixture = TestBed.createComponent(SiteSettingsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('shows custom banner and hides theme selector for Custom layout', async () => {
    await renderWithTenant({
      portalLayoutMode: 'Custom',
      portalTheme: 'premium',
      frontendOrigin: 'https://custom.example',
    });
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Site Personalizado Ativo');
    expect(el.textContent).not.toContain('Tema do Portal');
    expect(el.textContent).not.toContain('Identidade do portal');
  });

  it('shows theme selector for default portal theme', async () => {
    await renderWithTenant({
      portalLayoutMode: 'Default',
      portalTheme: 'default',
    });
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Tema do Portal');
    expect(el.textContent).toContain('Identidade do portal');
    expect(el.textContent).toContain('Pré-visualização ao vivo');
  });

  it('shows theme selector with 3 theme cards for minimal theme', async () => {
    await renderWithTenant({
      portalLayoutMode: 'Minimal',
      portalTheme: 'minimal',
    });
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Tema do Portal');
    // Theme cards should be visible
    expect(el.textContent).toContain('Starter');
    expect(el.textContent).toContain('Minimalista');
    expect(el.textContent).toContain('Premium');
  });
});
