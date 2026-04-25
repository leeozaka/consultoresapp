import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { PropertyListComponent } from './property-list';
import {
  PropertyService,
  TenantContextService,
  PaginatedResponse,
  Property,
  createMockProperty,
  createMockTenant,
} from '@consultores/core';

describe('PropertyListComponent', () => {
  let fixture: ComponentFixture<PropertyListComponent>;
  let component: PropertyListComponent;

  const emptyResponse: PaginatedResponse<Property> = {
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 12,
    totalPages: 0,
  };

  const mockPropertyService = {
    search: vi.fn().mockReturnValue(of(emptyResponse)),
  };

  const mockTenantContext = {
    currentTenant: signal(createMockTenant({ name: 'TestCo' })),
  };

  beforeEach(async () => {
    mockPropertyService.search.mockReturnValue(of(emptyResponse));

    await TestBed.configureTestingModule({
      imports: [PropertyListComponent],
      providers: [
        provideRouter([]),
        { provide: PropertyService, useValue: mockPropertyService },
        { provide: TenantContextService, useValue: mockTenantContext },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PropertyListComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display heading', () => {
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('h1')?.textContent?.toLowerCase()).toContain('imóveis disponíveis');
  });

  it('should show empty state when no properties found', () => {
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Nenhum imóvel encontrado');
  });

  it('should display property cards when results exist', () => {
    const props = [
      createMockProperty({ title: 'Apto Centro' }),
      createMockProperty({ id: '2', title: 'Casa Beira Mar' }),
    ];
    mockPropertyService.search.mockReturnValue(
      of({ items: props, totalCount: 2, page: 1, pageSize: 12, totalPages: 1 }),
    );

    fixture = TestBed.createComponent(PropertyListComponent);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const cards = el.querySelectorAll('h3');
    expect(cards.length).toBe(2);
  });

  it('should call search on init', () => {
    fixture.detectChanges();
    expect(mockPropertyService.search).toHaveBeenCalled();
  });
});
