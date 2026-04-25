import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiClientService } from '../http/api-client.service';
import {
  Tenant,
  CursorPage,
  CreateTenantRequest,
  UpdateTenantRequest,
  UpdateTenantSettingsRequest,
  UpdateTenantBrandingRequest,
  UpdateTenantEntitlementsRequest,
  AssignTenantPlanRequest,
} from '../models/tenant.model';
import { LIST_FETCH_PAGE_SIZE, PaginatedResponse } from '../models/pagination.model';

export interface SiteBuildStatusEvent {
  tenantId: string;
  status: string;
  step: string;
  message: string;
  occurredAt: string;
}

@Injectable({ providedIn: 'root' })
export class TenantService {
  private readonly api = inject(ApiClientService);

  getAll(): Observable<Tenant[]> {
    return this.api.get<CursorPage<Tenant>>('/api/admin/tenants').pipe(map((res) => res.items));
  }

  getById(id: string): Observable<Tenant> {
    return this.api.get<Tenant>(`/api/admin/tenants/${id}`);
  }

  getBySlug(slug: string): Observable<Tenant> {
    return this.api.get<Tenant>(`/api/tenants/by-slug/${slug}`);
  }

  resolveByHost(): Observable<Tenant> {
    return this.api.get<Tenant>('/api/tenants/resolve');
  }

  getPending(): Observable<Tenant[]> {
    return this.api
      .get<PaginatedResponse<Tenant>>('/api/admin/tenants/pending', { page: 1, pageSize: LIST_FETCH_PAGE_SIZE })
      .pipe(map((page) => page.items));
  }

  getMe(): Observable<Tenant> {
    return this.api.get<Tenant>('/api/tenants/me');
  }

  create(body: CreateTenantRequest): Observable<Tenant> {
    return this.api.post<Tenant>('/api/admin/tenants', body);
  }

  update(id: string, body: UpdateTenantRequest): Observable<Tenant> {
    return this.api.put<Tenant>(`/api/admin/tenants/${id}`, body);
  }

  activate(id: string): Observable<Tenant> {
    return this.api.post<Tenant>(`/api/admin/tenants/${id}/activate`, {});
  }

  approve(id: string): Observable<void> {
    return this.api.post<void>(`/api/admin/tenants/${id}/approve`, {});
  }

  suspend(id: string, reason?: string): Observable<void> {
    let path = `/api/admin/tenants/${id}/suspend`;
    if (reason) {
      path += `?reason=${encodeURIComponent(reason)}`;
    }
    return this.api.post<void>(path, {});
  }

  updateBranding(id: string, body: UpdateTenantBrandingRequest): Observable<Tenant> {
    return this.api.put<Tenant>(`/api/admin/tenants/${id}/branding`, body);
  }

  getSettings(): Observable<Tenant> {
    return this.api.get<Tenant>('/api/tenant/settings');
  }

  updateSettings(body: UpdateTenantSettingsRequest): Observable<Tenant> {
    return this.api.put<Tenant>('/api/tenant/settings', body);
  }

  updateMyBranding(body: UpdateTenantBrandingRequest): Observable<Tenant> {
    return this.api.put<Tenant>('/api/tenant/branding', body);
  }

  uploadPortalImage(file: File): Observable<{ url: string }> {
    const fd = new FormData();
    fd.append('file', file);
    return this.api.postForm<{ url: string }>('/api/tenant/branding/upload', fd);
  }

  updateEntitlements(id: string, body: UpdateTenantEntitlementsRequest): Observable<Tenant> {
    return this.api.put<Tenant>(`/api/admin/tenants/${id}/entitlements`, body);
  }

  assignPlan(id: string, body: AssignTenantPlanRequest): Observable<Tenant> {
    return this.api.put<Tenant>(`/api/admin/tenants/${id}/plan`, body);
  }

  archive(id: string): Observable<Tenant> {
    return this.api.post<Tenant>(`/api/admin/tenants/${id}/archive`, {});
  }

  observeSiteBuildEvents(tenantId: string): Observable<SiteBuildStatusEvent> {
    return new Observable<SiteBuildStatusEvent>((subscriber) => {
      const streamUrl = `/api/admin/site-builds/${tenantId}/events`;
      const eventSource = new EventSource(streamUrl, { withCredentials: true });

      eventSource.onmessage = (event) => {
        try {
          subscriber.next(JSON.parse(event.data) as SiteBuildStatusEvent);
        } catch (error) {
          subscriber.error(error);
        }
      };

      eventSource.onerror = (error) => {
        subscriber.error(error);
        eventSource.close();
      };

      return () => eventSource.close();
    });
  }
}

