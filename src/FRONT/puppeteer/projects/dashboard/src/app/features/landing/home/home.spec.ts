import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { HomeComponent } from './home';
import { PropertyService, AuthService, PlanService, createMockProperty } from '@consultores/core';
import { PLATFORM_ID } from '@angular/core';

describe('HomeComponent', () => {
  let fixture: ComponentFixture<HomeComponent>;
  let component: HomeComponent;

  const mockPropertyService = {
    getFeatured: vi.fn().mockReturnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 10, totalPages: 0 })),
  };

  const mockAuthService = {
    login: vi.fn(),
  };

  const mockPlanService = {
    getPlans: vi.fn().mockReturnValue(of([
      { id: '1', name: 'Starter', description: 'Basic', pricePerMonth: 9900, currencyCode: 'BRL', maxProperties: 10, videoUpload: false, aiDescriptions: false, customDomain: false, premiumAnalytics: false, isActive: true },
      { id: '2', name: 'Pro', description: 'Professional', pricePerMonth: 19900, currencyCode: 'BRL', maxProperties: 50, videoUpload: true, aiDescriptions: false, customDomain: false, premiumAnalytics: true, isActive: true },
      { id: '3', name: 'Enterprise', description: 'Full', pricePerMonth: 49900, currencyCode: 'BRL', maxProperties: 999, videoUpload: true, aiDescriptions: true, customDomain: true, premiumAnalytics: true, isActive: true },
    ])),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HomeComponent],
      providers: [
        provideRouter([]),
        { provide: PropertyService, useValue: mockPropertyService },
        { provide: AuthService, useValue: mockAuthService },
        { provide: PlanService, useValue: mockPlanService },
        { provide: PLATFORM_ID, useValue: 'browser' },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render hero section with heading', () => {
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const h1 = el.querySelector('h1');
    expect(h1).toBeTruthy();
    expect(h1?.textContent).toContain('imóveis');
  });

  it('should render all 6 features', () => {
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const featureSection = el.querySelector('#features');
    const titles = featureSection?.querySelectorAll('h3');
    expect(titles?.length).toBe(6);
  });

  it('should render 3 pricing plans when section is intersecting', async () => {
    // Plans load lazily via IntersectionObserver — call loadPlans directly
    fixture.detectChanges();
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (component as any)['loadPlans']();
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const pricingSection = el.querySelector('#pricing');
    const plans = pricingSection?.querySelectorAll('h3');
    expect(plans?.length).toBe(3);
  });

  // it('should show empty state when no featured properties', () => {
  //   fixture.detectChanges();
  //   const el = fixture.nativeElement as HTMLElement;
  //   expect(el.textContent).toContain('Em breve');
  // });

  // it('should load and display featured properties', () => {
  //   const mockProperties = [
  //     createMockProperty({ title: 'Casa Teste', city: 'São Paulo' }),
  //     createMockProperty({ id: '2', title: 'Apto Teste' }),
  //   ];
  //   mockPropertyService.getFeatured.mockReturnValue(of({ items: mockProperties, totalCount: 2, page: 1, pageSize: 10, totalPages: 1 }));

  //   fixture = TestBed.createComponent(HomeComponent);
  //   fixture.detectChanges();

  //   const el = fixture.nativeElement as HTMLElement;
  //   const cards = el.querySelectorAll('#listings h4');
  //   expect(cards.length).toBe(2);
  //   expect(cards[0].textContent).toContain('Casa Teste');
  // });

  it('should call scrollTo smoothly', () => {
    fixture.detectChanges();
    const scrollSpy = vi.fn();
    const fakeEl = { scrollIntoView: scrollSpy } as unknown as HTMLElement;
    vi.spyOn(document, 'getElementById').mockReturnValue(fakeEl);

    component['scrollTo']('features');
    expect(scrollSpy).toHaveBeenCalledWith({ behavior: 'smooth' });
  });
});
