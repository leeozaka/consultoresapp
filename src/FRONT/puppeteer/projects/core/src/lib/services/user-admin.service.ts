import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiClientService } from '../http/api-client.service';
import { LIST_FETCH_PAGE_SIZE, PaginatedResponse } from '../models/pagination.model';

export interface UserRecord {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  tenantId?: string;
  tenantName?: string;
  roles: string[];
  isActive: boolean;
  createdAtUtc: string;
}

export interface AssignTenantRequest {
  tenantId: string;
  role: 'TenantAdmin' | 'Agent';
}

export interface CreateUserRequest {
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
  tenantId?: string;
  role?: 'TenantAdmin' | 'Agent';
}

export interface UpdateUserRequest {
  firstName?: string;
  lastName?: string;
  email?: string;
}

@Injectable({ providedIn: 'root' })
export class UserAdminService {
  private readonly api = inject(ApiClientService);

  getUsers(): Observable<UserRecord[]> {
    return this.api
      .get<PaginatedResponse<UserRecord>>('/api/admin/users', { page: 1, pageSize: LIST_FETCH_PAGE_SIZE })
      .pipe(map((page) => page.items));
  }

  getOrphanedUsers(): Observable<UserRecord[]> {
    return this.api
      .get<PaginatedResponse<UserRecord>>('/api/admin/users/orphaned', { page: 1, pageSize: LIST_FETCH_PAGE_SIZE })
      .pipe(map((page) => page.items));
  }

  assignTenant(userId: string, body: AssignTenantRequest): Observable<void> {
    return this.api.post<void>(`/api/admin/users/${userId}/assign-tenant`, body);
  }

  toggleActive(userId: string, isActive: boolean): Observable<void> {
    return this.api.put<void>(`/api/admin/users/${userId}/toggle-active?isActive=${isActive}`, {});
  }

  getById(userId: string): Observable<UserRecord> {
    return this.api.get<UserRecord>(`/api/admin/users/${userId}`);
  }

  getUsersByTenant(tenantId: string): Observable<UserRecord[]> {
    return this.api
      .get<PaginatedResponse<UserRecord>>(`/api/admin/users/by-tenant/${tenantId}`, {
        page: 1,
        pageSize: LIST_FETCH_PAGE_SIZE,
      })
      .pipe(map((page) => page.items));
  }

  createUser(body: CreateUserRequest): Observable<UserRecord> {
    return this.api.post<UserRecord>('/api/admin/users', body);
  }

  updateUser(userId: string, body: UpdateUserRequest): Observable<UserRecord> {
    return this.api.put<UserRecord>(`/api/admin/users/${userId}`, body);
  }
}
