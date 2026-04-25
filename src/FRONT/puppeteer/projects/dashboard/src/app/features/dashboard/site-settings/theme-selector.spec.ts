import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ThemeSelectorComponent } from './theme-selector';
import { PORTAL_THEMES } from '../../portal/theme-registry';
import type { Plan } from '@consultores/core';

describe('ThemeSelectorComponent', () => {
  let fixture: ComponentFixture<ThemeSelectorComponent>;
  let component: ThemeSelectorComponent;

  const mockPlans: Plan[] = [
    {
      id: 'plan-default',
      name: 'Starter',
      description: '',
      pricePerMonth: 0,
      currencyCode: 'BRL',
      maxProperties: 10,
      videoUpload: false,
      aiDescriptions: false,
      customDomain: false,
      premiumAnalytics: false,
      portalTheme: 'default',
      isActive: true,
    },
    {
      id: 'plan-minimal',
      name: 'Professional',
      description: '',
      pricePerMonth: 99,
      currencyCode: 'BRL',
      maxProperties: 50,
      videoUpload: false,
      aiDescriptions: false,
      customDomain: false,
      premiumAnalytics: false,
      portalTheme: 'minimal',
      isActive: true,
    },
    {
      id: 'plan-premium',
      name: 'Premium',
      description: '',
      pricePerMonth: 199,
      currencyCode: 'BRL',
      maxProperties: 200,
      videoUpload: true,
      aiDescriptions: true,
      customDomain: true,
      premiumAnalytics: true,
      portalTheme: 'premium',
      isActive: true,
    },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ThemeSelectorComponent],
    }).compileComponents();
  });

  function create(theme: string, planId: string | null = 'plan-default') {
    fixture = TestBed.createComponent(ThemeSelectorComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('currentTheme', theme);
    fixture.componentRef.setInput('allPlans', mockPlans);
    fixture.componentRef.setInput('currentPlanId', planId);
    fixture.detectChanges();
  }

  it('renders 3 theme cards', () => {
    create('default');
    const cards = fixture.nativeElement.querySelectorAll('button');
    expect(cards.length).toBe(PORTAL_THEMES.length);
  });

  it('marks active theme with badge', () => {
    create('minimal', 'plan-minimal');
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Ativo');
  });

  it('shows lock on plan-gated themes', () => {
    create('default', 'plan-default');
    const el = fixture.nativeElement as HTMLElement;
    // minimal and premium should be locked for default plan
    expect(el.textContent).toContain('Disponível no plano');
  });

  it('emits selection on click', () => {
    create('default', 'plan-premium');
    const spy = vi.fn();
    component.themeSelected.subscribe(spy);
    const cards = fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>;
    // Click the minimal card (index 1)
    cards[1].click();
    expect(spy).toHaveBeenCalledWith('minimal');
  });

  it('disables locked theme buttons', () => {
    create('default', 'plan-default');
    const cards = fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>;
    // default (index 0) should not be disabled
    expect(cards[0].disabled).toBe(false);
    // minimal (index 1) should be disabled for default plan
    expect(cards[1].disabled).toBe(true);
    // premium (index 2) should be disabled for default plan
    expect(cards[2].disabled).toBe(true);
  });
});
