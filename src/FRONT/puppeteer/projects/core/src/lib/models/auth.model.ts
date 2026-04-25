export interface CurrentUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  tenantId: string | null;
  roles: string[];
}

export interface LoginRequest {
  email: string;
  password: string;
}

export type UserRole = 'SuperAdmin' | 'TenantAdmin' | 'Agent';
