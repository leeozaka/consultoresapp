import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { signal } from '@angular/core';
import { OverviewComponent } from './overview';
import { PropertyService, AuthService, BillingService, createMockProperty, createMockUser } from '@consultores/core';

describe('OverviewComponent', () => {
  let fixture: ComponentFixture<OverviewComponent>;
  let component: OverviewComponent;

  const mockProperties = [
    createMockProperty({ id: '1', title: 'Apartamento A', status: 'Active' as any }),
    createMockProperty({ id: '2', title: 'Casa B', status: 'Draft' as any }),
  ];

  const mockPropertyService = {
    search: vi.fn().mockReturnValue(of({ items: mockProperties, totalCount: 10, page: 1, pageSize: 5 })),
  };

  const mockAuthService = {
    currentUser: signal(createMockUser()),
    hasRole: vi.fn().mockReturnValue(false),
  };

  const mockBillingService = {
    getOverview: vi.fn().mockReturnValue(of({ gracePeriodEndsAt: null })),
  };

  beforeEach(async () => {
    mockAuthService.hasRole.mockReturnValue(false);
    mockBillingService.getOverview.mockReturnValue(of({ gracePeriodEndsAt: null }));

    await TestBed.configureTestingModule({
      imports: [OverviewComponent],
      providers: [
        provideRouter([]),
        { provide: PropertyService, useValue: mockPropertyService },
        { provide: AuthService, useValue: mockAuthService },
        { provide: BillingService, useValue: mockBillingService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(OverviewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display welcome message with user name', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Olá, João!');
  });

  it('should display stat cards', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Total de Imóveis');
    expect(el.textContent).toContain('Ativos');
    expect(el.textContent).toContain('Rascunhos');
  });

  it('should display recent properties', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Apartamento A');
    expect(el.textContent).toContain('Casa B');
  });

  it('should show grace period alert when TenantAdmin has pending payment', async () => {
    mockAuthService.hasRole.mockReturnValue(true);
    mockBillingService.getOverview.mockReturnValue(of({ gracePeriodEndsAt: '2026-04-01T00:00:00Z' }));

    fixture = TestBed.createComponent(OverviewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Pagamento pendente');
    expect(el.querySelector('[role="alert"]')).toBeTruthy();
  });

  it('should not show grace period alert when no grace period is set', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[role="alert"]')).toBeNull();
  });
});
