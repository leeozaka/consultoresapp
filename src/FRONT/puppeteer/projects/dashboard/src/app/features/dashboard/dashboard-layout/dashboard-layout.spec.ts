import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { DashboardLayoutComponent } from './dashboard-layout';
import { AuthService, TenantContextService, PaymentSseService, createMockUser, createMockTenant } from '@consultores/core';

describe('DashboardLayoutComponent', () => {
  let fixture: ComponentFixture<DashboardLayoutComponent>;
  let component: DashboardLayoutComponent;

  const mockUser = createMockUser();
  const mockTenant = createMockTenant();

  const mockAuthService = {
    currentUser: signal(mockUser),
    isSuperAdmin: signal(false),
    hasRole: vi.fn().mockReturnValue(false),
    logout: vi.fn(),
  };

  const mockTenantContext = {
    currentTenant: signal(mockTenant),
  };

  const mockPaymentSse = {
    connect: vi.fn(),
    disconnect: vi.fn(),
  };

  beforeEach(async () => {
    mockAuthService.hasRole.mockReturnValue(false);

    await TestBed.configureTestingModule({
      imports: [DashboardLayoutComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService },
        { provide: TenantContextService, useValue: mockTenantContext },
        { provide: PaymentSseService, useValue: mockPaymentSse },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardLayoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display tenant name', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Imobiliária Demo');
  });

  it('should display user name', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('João');
    expect(el.textContent).toContain('Silva');
  });

  it('should display navigation items', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Visão Geral');
    expect(el.textContent).toContain('Imóveis');
    expect(el.textContent).toContain('Meu Site');
  });

  it('should not show admin link for non-super-admins', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).not.toContain('Administração');
  });

  it('should call logout on click', () => {
    const btn = fixture.nativeElement.querySelector('button');
    btn?.click();
    expect(mockAuthService.logout).toHaveBeenCalled();
  });
});
