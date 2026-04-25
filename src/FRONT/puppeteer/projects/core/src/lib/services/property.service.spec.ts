import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { PropertyService } from './property.service';
import { ApiClientService } from '../http/api-client.service';
import { PropertyType, ListingType, PropertyStatus } from '../models/property.model';
import { createMockProperty } from '../testing/fixtures';
import { environment } from '../environments/environment';
import { LIST_FETCH_PAGE_SIZE } from '../models/pagination.model';

describe('PropertyService', () => {
  let service: PropertyService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PropertyService, ApiClientService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PropertyService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('search() calls GET /api/properties with query params', () => {
    service.search({ page: 1, pageSize: 12 }).subscribe();
    const req = http.expectOne((r) => r.url === `${environment.apiBaseUrl}/api/properties`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('1');
    req.flush({ items: [], total_count: 0, page: 1, page_size: 12, total_pages: 0 });
  });

  it('getById() calls GET /api/properties/:id', () => {
    service.getById('prop-1').subscribe();
    const req = http.expectOne(`${environment.apiBaseUrl}/api/properties/prop-1`);
    expect(req.request.method).toBe('GET');
    req.flush(createMockProperty());
  });

  it('create() calls POST /api/properties', () => {
    const body = { title: 'Test', price: 100000, city: 'SP', state: 'SP', propertyType: PropertyType.Apartment, listingType: ListingType.Sale, bedrooms: 2, bathrooms: 1 };
    service.create(body).subscribe();
    const req = http.expectOne(`${environment.apiBaseUrl}/api/properties`);
    expect(req.request.method).toBe('POST');
    req.flush(createMockProperty());
  });

  it('update() calls PUT /api/properties/:id', () => {
    const body = {
      title: 'Updated',
      price: 200000,
      city: 'RJ',
      state: 'RJ',
      propertyType: PropertyType.House,
      listingType: ListingType.Rent,
      bedrooms: 3,
      bathrooms: 2,
    };
    service.update('prop-1', body).subscribe();
    const req = http.expectOne(`${environment.apiBaseUrl}/api/properties/prop-1`);
    expect(req.request.method).toBe('PUT');
    req.flush(createMockProperty());
  });

  it('publish() calls POST /api/properties/:id/publish', () => {
    service.publish('prop-1').subscribe();
    const req = http.expectOne(`${environment.apiBaseUrl}/api/properties/prop-1/publish`);
    expect(req.request.method).toBe('POST');
    req.flush(createMockProperty({ status: PropertyStatus.Active }));
  });

  it('uploadImage() calls POST /api/properties/:id/images', () => {
    const file = new File(['img'], 'test.jpg', { type: 'image/jpeg' });
    service.uploadImage('prop-1', file).subscribe();
    const req = http.expectOne(`${environment.apiBaseUrl}/api/properties/prop-1/images`);
    expect(req.request.method).toBe('POST');
    req.flush(createMockProperty());
  });

  it('getFeatured() calls GET /api/properties/featured', () => {
    service.getFeatured().subscribe();
    const req = http.expectOne((r) => r.url === `${environment.apiBaseUrl}/api/properties/featured`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total_count: 0, page: 1, page_size: 6, total_pages: 0 });
  });

  it('getLocationSuggestions() calls GET /api/properties/suggestions and maps items', () => {
    const suggestion = {
      label: 'São Paulo',
      type: 'city' as const,
      city: 'São Paulo',
      state: 'SP',
    };
    service.getLocationSuggestions('sao').subscribe((rows) => {
      expect(rows).toEqual([suggestion]);
    });
    const req = http.expectOne(
      (r) =>
        r.url === `${environment.apiBaseUrl}/api/properties/suggestions` &&
        r.params.get('q') === 'sao' &&
        r.params.get('page') === '1' &&
        r.params.get('pageSize') === String(LIST_FETCH_PAGE_SIZE),
    );
    expect(req.request.method).toBe('GET');
    req.flush({
      items: [suggestion],
      total_count: 1,
      page: 1,
      page_size: 20,
      total_pages: 1,
    });
  });
});
