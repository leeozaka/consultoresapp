import { TestBed, ComponentFixture } from '@angular/core/testing';
import { Router } from '@angular/router';
import { PLATFORM_ID } from '@angular/core';
import { LogoutComponent } from './logout';
import { AuthService } from '@consultores/core';

describe('LogoutComponent', () => {
  let fixture: ComponentFixture<LogoutComponent>;
  let mockAuth: { logout: ReturnType<typeof vi.fn> };
  let mockRouter: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    mockAuth = { logout: vi.fn() };
    mockRouter = { navigate: vi.fn().mockResolvedValue(true) };

    await TestBed.configureTestingModule({
      imports: [LogoutComponent],
      providers: [
        { provide: AuthService, useValue: mockAuth },
        { provide: Router, useValue: mockRouter },
        { provide: PLATFORM_ID, useValue: 'browser' },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LogoutComponent);
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should call logout and navigate to root', () => {
    fixture.detectChanges();
    expect(mockAuth.logout).toHaveBeenCalled();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/']);
  });
});
