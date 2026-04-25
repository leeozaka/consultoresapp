import { describe, it, expect, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  toSnakeCase,
  toCamelCase,
  convertKeysToSnakeCase,
  convertKeysToCamelCase,
  snakeCaseInterceptor,
} from './snake-case.interceptor';

describe('toSnakeCase', () => {
  it('converts simple camelCase to snake_case', () => {
    expect(toSnakeCase('firstName')).toBe('first_name');
  });

  it('converts PascalCase to snake_case', () => {
    expect(toSnakeCase('FirstName')).toBe('first_name');
  });

  it('leaves already snake_case unchanged', () => {
    expect(toSnakeCase('first_name')).toBe('first_name');
  });

  it('handles consecutive uppercase letters (acronyms)', () => {
    expect(toSnakeCase('areaSqMeters')).toBe('area_sq_meters');
  });
});

describe('toCamelCase', () => {
  it('converts snake_case to camelCase', () => {
    expect(toCamelCase('first_name')).toBe('firstName');
  });

  it('leaves already camelCase unchanged', () => {
    expect(toCamelCase('firstName')).toBe('firstName');
  });

  it('handles multiple underscores', () => {
    expect(toCamelCase('published_at_utc')).toBe('publishedAtUtc');
  });
});

describe('convertKeysToSnakeCase', () => {
  it('converts top-level object keys', () => {
    const result = convertKeysToSnakeCase({ firstName: 'João', lastName: 'Silva' });
    expect(result).toEqual({ first_name: 'João', last_name: 'Silva' });
  });

  it('recursively converts nested object keys', () => {
    const result = convertKeysToSnakeCase({ userInfo: { firstName: 'João' } });
    expect(result).toEqual({ user_info: { first_name: 'João' } });
  });

  it('converts keys in arrays of objects', () => {
    const result = convertKeysToSnakeCase([{ firstName: 'João' }, { firstName: 'Maria' }]);
    expect(result).toEqual([{ first_name: 'João' }, { first_name: 'Maria' }]);
  });

  it('returns primitive values unchanged', () => {
    expect(convertKeysToSnakeCase('hello')).toBe('hello');
    expect(convertKeysToSnakeCase(42)).toBe(42);
    expect(convertKeysToSnakeCase(null)).toBeNull();
  });
});

describe('convertKeysToCamelCase', () => {
  it('converts top-level response keys to camelCase', () => {
    const result = convertKeysToCamelCase({ first_name: 'João', total_count: 10 });
    expect(result).toEqual({ firstName: 'João', totalCount: 10 });
  });

  it('recursively converts nested keys', () => {
    const result = convertKeysToCamelCase({ branding_config: { primary_color: '#fff' } });
    expect(result).toEqual({ brandingConfig: { primaryColor: '#fff' } });
  });

  it('converts keys in arrays of objects', () => {
    const result = convertKeysToCamelCase({ items: [{ property_type: 'Apartment' }] });
    expect(result).toEqual({ items: [{ propertyType: 'Apartment' }] });
  });

  it('handles paginated response structure', () => {
    const result = convertKeysToCamelCase({
      items: [{ created_at: '2025-01-01' }],
      total_count: 1,
      page: 1,
      page_size: 10,
      total_pages: 1,
    });
    expect(result).toEqual({
      items: [{ createdAt: '2025-01-01' }],
      totalCount: 1,
      page: 1,
      pageSize: 10,
      totalPages: 1,
    });
  });
});

describe('snakeCaseInterceptor — bypass OIDC paths', () => {
  let http: HttpTestingController;
  let client: HttpClient;

  function setup() {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([snakeCaseInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
  }

  afterEach(() => http.verify());

  it('does NOT transform response keys for /.well-known/ URLs', () => {
    setup();
    const discoveryBody = {
      authorization_endpoint: 'http://localhost:5001/connect/authorize',
      token_endpoint: 'http://localhost:5001/connect/token',
    };

    client.get('http://localhost:5001/.well-known/openid-configuration').subscribe((body) => {
      expect(body).toEqual(discoveryBody);
    });

    const req = http.expectOne('http://localhost:5001/.well-known/openid-configuration');
    req.flush(discoveryBody);
  });

  it('does NOT transform response keys for /connect/ URLs', () => {
    setup();
    const tokenBody = { access_token: 'abc', token_type: 'bearer' };

    client.get('http://localhost:5001/connect/token').subscribe((body) => {
      expect(body).toEqual(tokenBody);
    });

    const req = http.expectOne('http://localhost:5001/connect/token');
    req.flush(tokenBody);
  });

  it('does NOT transform request body for /connect/ URLs', () => {
    setup();
    const requestBody = { client_id: 'spa', grant_type: 'authorization_code' };

    client.post('http://localhost:5001/connect/token', requestBody).subscribe();

    const req = http.expectOne('http://localhost:5001/connect/token');
    expect(req.request.body).toEqual(requestBody);
    req.flush({});
  });

  it('transforms response keys for regular API URLs', () => {
    setup();
    const apiBody = { first_name: 'João', last_name: 'Silva' };

    client.get('/api/users/1').subscribe((body) => {
      expect(body).toEqual({ firstName: 'João', lastName: 'Silva' });
    });

    const req = http.expectOne('/api/users/1');
    req.flush(apiBody);
  });

  it('transforms request body for regular API URLs', () => {
    setup();
    const requestBody = { firstName: 'João' };

    client.post('/api/users', requestBody).subscribe();

    const req = http.expectOne('/api/users');
    expect(req.request.body).toEqual({ first_name: 'João' });
    req.flush({});
  });

  it('does NOT transform FormData request body for regular API URLs', () => {
    setup();
    const formData = new FormData();
    formData.append('file', new File(['image'], 'house.jpg', { type: 'image/jpeg' }));

    client.post('/api/properties/1/images', formData).subscribe();

    const req = http.expectOne('/api/properties/1/images');
    expect(req.request.body).toBe(formData);
    req.flush({});
  });

  it('does NOT transform Blob response bodies', () => {
    setup();
    const blob = new Blob(['image-data'], { type: 'image/webp' });

    client.get('http://localhost:4566/property-photos/img.webp', { responseType: 'blob' }).subscribe((body) => {
      expect(body).toBeInstanceOf(Blob);
    });

    const req = http.expectOne('http://localhost:4566/property-photos/img.webp');
    req.flush(blob);
  });

  it('does NOT transform ArrayBuffer response bodies', () => {
    setup();
    const buffer = new ArrayBuffer(8);

    client.get('/api/data', { responseType: 'arraybuffer' }).subscribe((body) => {
      expect(body).toBeInstanceOf(ArrayBuffer);
    });

    const req = http.expectOne('/api/data');
    req.flush(buffer);
  });
});
