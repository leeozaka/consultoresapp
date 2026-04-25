import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiClientService } from '../http/api-client.service';
import {
  Property,
  FeaturedProperty,
  CreatePropertyRequest,
  UpdatePropertyRequest,
  PropertySearchRequest,
  LocationSuggestion,
} from '../models/property.model';
import { LIST_FETCH_PAGE_SIZE, PaginatedResponse } from '../models/pagination.model';

@Injectable({ providedIn: 'root' })
export class PropertyService {
  private readonly api = inject(ApiClientService);

  search(params: PropertySearchRequest): Observable<PaginatedResponse<Property>> {
    return this.api.get<PaginatedResponse<Property>>('/api/properties', params as Record<string, unknown>);
  }

  getById(id: string): Observable<Property> {
    return this.api.get<Property>(`/api/properties/${id}`);
  }

  create(body: CreatePropertyRequest): Observable<Property> {
    return this.api.post<Property>('/api/properties', body);
  }

  update(id: string, body: UpdatePropertyRequest): Observable<Property> {
    return this.api.put<Property>(`/api/properties/${id}`, body);
  }

  publish(id: string): Observable<Property> {
    return this.api.post<Property>(`/api/properties/${id}/publish`, {});
  }

  deactivate(id: string): Observable<Property> {
    return this.api.post<Property>(`/api/properties/${id}/deactivate`, {});
  }

  uploadImage(id: string, file: File): Observable<Property> {
    const formData = new FormData();
    formData.append('file', file);
    return this.api.postForm<Property>(`/api/properties/${id}/images`, formData);
  }

  deleteImage(id: string, key: string): Observable<Property> {
    return this.api.delete<Property>(`/api/properties/${id}/images`, { key });
  }

  getFeatured(params: { page?: number; pageSize?: number } = {}): Observable<PaginatedResponse<FeaturedProperty>> {
    return this.api.get<PaginatedResponse<FeaturedProperty>>('/api/properties/featured', params as Record<string, unknown>);
  }

  getLocationSuggestions(q: string): Observable<LocationSuggestion[]> {
    return this.api
      .get<PaginatedResponse<LocationSuggestion>>('/api/properties/suggestions', {
        q,
        page: 1,
        pageSize: LIST_FETCH_PAGE_SIZE,
      })
      .pipe(map((page) => page.items));
  }
}
