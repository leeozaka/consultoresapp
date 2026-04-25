import { TestBed, ComponentFixture } from '@angular/core/testing';
import { Router } from '@angular/router';
import { PLATFORM_ID } from '@angular/core';
import { of } from 'rxjs';
import { CallbackComponent } from './callback';
import { AuthService, TenantContextService } from '@consultores/core';

describe('CallbackComponent', () => {
  let fixture: ComponentFixture<CallbackComponent>;
  let component: CallbackComponent;
  let mockAuth: {
    handleCallback: ReturnType<typeof vi.fn>;
    isAuthenticated: ReturnType<typeof vi.fn>;
    isSuperAdmin: ReturnType<typeof vi.fn>;
  };
  let mockRouter: { navigate: ReturnType<typeof vi.fn> };
  let mockTenantContext: { refreshFromUser: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    mockAuth = {
      handleCallback: vi.fn().mockResolvedValue(undefined),
      isAuthenticated: vi.fn().mockReturnValue(true),
      isSuperAdmin: vi.fn().mockReturnValue(false),
    };
    mockRouter = { navigate: vi.fn().mockResolvedValue(true) };
    mockTenantContext = {
      refreshFromUser: vi.fn().mockReturnValue(of(null)),
    };

    await TestBed.configureTestingModule({
      imports: [CallbackComponent],
      providers: [
        { provide: AuthService, useValue: mockAuth },
        { provide: Router, useValue: mockRouter },
        { provide: TenantContextService, useValue: mockTenantContext },
        { provide: PLATFORM_ID, useValue: 'browser' },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CallbackComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call handleCallback and navigate to dashboard on success', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    // flush the inner Promise wrapping refreshFromUser observable
    await new Promise(resolve => setTimeout(resolve, 0));

    expect(mockAuth.handleCallback).toHaveBeenCalled();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('should navigate to root on auth error', async () => {
    mockAuth.handleCallback.mockRejectedValue(new Error('fail'));

    fixture = TestBed.createComponent(CallbackComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(mockRouter.navigate).toHaveBeenCalledWith(['/']);
  });

  it('should navigate to root when token exchange fails silently', async () => {
    mockAuth.isAuthenticated.mockReturnValue(false);

    fixture = TestBed.createComponent(CallbackComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(mockRouter.navigate).toHaveBeenCalledWith(['/']);
  });

  it('should show loading spinner', () => {
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('app-loading-spinner')).toBeTruthy();
  });
});
