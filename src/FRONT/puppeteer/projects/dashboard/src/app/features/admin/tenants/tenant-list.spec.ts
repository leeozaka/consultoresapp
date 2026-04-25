import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { TenantListComponent } from './tenant-list';
import { TenantService, createMockTenant, TenantStatus } from '@consultores/core';

describe('TenantListComponent', () => {
  let fixture: ComponentFixture<TenantListComponent>;
  let component: TenantListComponent;

  const mockTenants = [
    createMockTenant({ id: 't1', name: 'Imobiliária A', slug: 'imob-a' }),
    createMockTenant({ id: 't2', name: 'Imobiliária B', slug: 'imob-b' }),
  ];

  const mockTenantService = {
    getAll: vi.fn().mockReturnValue(of(mockTenants)),
    activate: vi.fn().mockReturnValue(of(mockTenants[0])),
    approve: vi.fn().mockReturnValue(of(mockTenants[0])),
    observeSiteBuildEvents: vi.fn().mockReturnValue(of()),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TenantListComponent],
      providers: [
        provideRouter([]),
        provideAnimationsAsync(),
        { provide: TenantService, useValue: mockTenantService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TenantListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display heading', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Tenants');
  });

  it('should display tenant names', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Imobiliária A');
    expect(el.textContent).toContain('Imobiliária B');
  });

  it('should show stats cards', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Total');
    expect(el.textContent).toContain('Ativos');
    expect(el.textContent).toContain('Pendentes');
  });

  it('filteredTenants filters by search term', () => {
    const comp = component as any;
    comp.searchTerm.set('imob-a');
    fixture.detectChanges();
    expect(comp.filteredTenants().length).toBe(1);
    expect(comp.filteredTenants()[0].name).toBe('Imobiliária A');
  });

  it('filteredTenants filters by status', () => {
    const mixedTenants = [
      createMockTenant({ id: 't1', name: 'Active One', status: 'Active' as any }),
      createMockTenant({ id: 't2', name: 'Pending One', status: 'Pending' as any }),
    ];
    mockTenantService.getAll.mockReturnValue(of(mixedTenants));
    component.ngOnInit();
    fixture.detectChanges();

    const comp = component as any;
    comp.statusFilter.set(TenantStatus.Active);
    fixture.detectChanges();
    expect(comp.filteredTenants().length).toBe(1);
    expect(comp.filteredTenants()[0].status).toBe(TenantStatus.Active);
  });

  it('getStatusSeverity returns correct severity', () => {
    const comp = component as any;
    expect(comp.getStatusSeverity(TenantStatus.Active)).toBe('success');
    expect(comp.getStatusSeverity(TenantStatus.Pending)).toBe('warn');
    expect(comp.getStatusSeverity(TenantStatus.Suspended)).toBe('danger');
  });

  it('stats computed correctly', () => {
    const mixedTenants = [
      createMockTenant({ id: 't1', status: 'Active' as any }),
      createMockTenant({ id: 't2', status: 'Active' as any }),
      createMockTenant({ id: 't3', status: 'Pending' as any }),
    ];
    mockTenantService.getAll.mockReturnValue(of(mixedTenants));
    component.ngOnInit();
    fixture.detectChanges();

    const stats = (component as any).stats();
    expect(stats.total).toBe(3);
    expect(stats.active).toBe(2);
    expect(stats.pending).toBe(1);
  });

  it('activate calls tenantService.activate', () => {
    const event = new Event('click');
    event.stopPropagation = vi.fn();
    mockTenantService.activate.mockReturnValue(of(mockTenants[0]));

    component['activate']('t1', event);
    expect(mockTenantService.activate).toHaveBeenCalledWith('t1');
  });
});
