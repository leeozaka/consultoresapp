import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ComponentRef } from '@angular/core';
import { TenantDetailComponent } from './tenant-detail';
import { TenantService, AddonService, createMockTenant } from '@consultores/core';

describe('TenantDetailComponent', () => {
  let fixture: ComponentFixture<TenantDetailComponent>;
  let component: TenantDetailComponent;
  let componentRef: ComponentRef<TenantDetailComponent>;

  const mockTenant = createMockTenant({ id: 't1', name: 'Test Tenant' });

  const mockTenantService = {
    getAll: vi.fn().mockReturnValue(of([mockTenant])),
    getById: vi.fn().mockReturnValue(of(mockTenant)),
    activate: vi.fn().mockReturnValue(of(mockTenant)),
    updateBranding: vi.fn().mockReturnValue(of(mockTenant)),
  };

  const mockAddonService = {
    getTenantAddons: vi.fn().mockReturnValue(of([])),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TenantDetailComponent],
      providers: [
        provideRouter([]),
        { provide: TenantService, useValue: mockTenantService },
        { provide: AddonService, useValue: mockAddonService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TenantDetailComponent);
    component = fixture.componentInstance;
    componentRef = fixture.componentRef;
    componentRef.setInput('id', 't1');
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display tenant name', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Test Tenant');
  });

  it('should display branding form', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Branding');
  });

  it('should display status chip', () => {
    const el: HTMLElement = fixture.nativeElement;
    // mockTenant has status Active — verify status is displayed
    expect(el.textContent).toContain('Ativo');
  });
});
