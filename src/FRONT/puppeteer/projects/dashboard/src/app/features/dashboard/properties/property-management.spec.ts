import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { PropertyManagementComponent } from './property-management';
import { PropertyService, createMockProperty } from '@consultores/core';

describe('PropertyManagementComponent', () => {
  let fixture: ComponentFixture<PropertyManagementComponent>;
  let component: PropertyManagementComponent;

  const mockProperties = [
    createMockProperty({ id: '1', title: 'Apto 1' }),
    createMockProperty({ id: '2', title: 'Casa 2' }),
  ];

  const mockPropertyService = {
    search: vi.fn().mockReturnValue(of({ items: mockProperties, totalCount: 2, page: 1, pageSize: 20 })),
    publish: vi.fn().mockReturnValue(of(mockProperties[0])),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PropertyManagementComponent],
      providers: [
        provideRouter([]),
        { provide: PropertyService, useValue: mockPropertyService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PropertyManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display heading', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Gestão de Imóveis');
  });

  it('should display property rows', () => {
    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
  });

  it('should display total count', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('2 imóveis cadastrados');
  });
});
