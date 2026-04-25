import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { AdminLayoutComponent } from './admin-layout';
import { AuthService, createMockUser } from '@consultores/core';

describe('AdminLayoutComponent', () => {
  let fixture: ComponentFixture<AdminLayoutComponent>;
  let component: AdminLayoutComponent;

  const mockAuthService = {
    currentUser: signal(createMockUser({ roles: ['SuperAdmin'] })),
    logout: vi.fn(),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminLayoutComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminLayoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display Super Admin branding', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Super Admin');
  });

  it('should display nav items', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Tenants');
    expect(el.textContent).toContain('Usuários');
    expect(el.textContent).toContain('Planos');
  });
});
