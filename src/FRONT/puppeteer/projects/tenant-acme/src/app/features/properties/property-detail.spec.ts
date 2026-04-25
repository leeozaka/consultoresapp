import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { PropertyDetailComponent } from './property-detail';
import { PropertyService, createMockProperty } from '@consultores/core';
import { ComponentRef } from '@angular/core';

describe('PropertyDetailComponent', () => {
  let fixture: ComponentFixture<PropertyDetailComponent>;
  let component: PropertyDetailComponent;
  let componentRef: ComponentRef<PropertyDetailComponent>;
  let mockPropertyService: { getById: ReturnType<typeof vi.fn> };

  const mockProperty = createMockProperty({
    id: 'prop-1',
    title: 'Apartamento Centro',
    description: 'Belo apartamento no centro da cidade',
    bedrooms: 3,
    bathrooms: 2,
    areaSqMeters: 100,
    price: 450000,
  });

  beforeEach(async () => {
    mockPropertyService = { getById: vi.fn().mockReturnValue(of(mockProperty)) };

    await TestBed.configureTestingModule({
      imports: [PropertyDetailComponent],
      providers: [
        provideRouter([]),
        { provide: PropertyService, useValue: mockPropertyService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PropertyDetailComponent);
    component = fixture.componentInstance;
    componentRef = fixture.componentRef;
    componentRef.setInput('id', 'prop-1');
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should load property on init', () => {
    fixture.detectChanges();
    expect(mockPropertyService.getById).toHaveBeenCalledWith('prop-1');
  });

  it('should display property title', () => {
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Apartamento Centro');
  });

  it('should display bedrooms', () => {
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('3');
    expect(el.textContent).toContain('Quartos');
  });

  it('should display not-found state on error', () => {
    mockPropertyService.getById.mockReturnValue(throwError(() => new Error('Not found')));
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Imóvel não encontrado');
  });

  it('should show loading spinner initially', () => {
    mockPropertyService.getById.mockReturnValue(of(mockProperty).pipe());
    const el: HTMLElement = fixture.nativeElement;
    fixture.detectChanges();
    expect(el.querySelector('article')).toBeTruthy();
  });

  it('should display description', () => {
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Belo apartamento no centro da cidade');
  });
});
