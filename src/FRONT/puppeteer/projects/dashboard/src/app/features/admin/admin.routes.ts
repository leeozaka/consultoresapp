import { Routes } from '@angular/router';
import { AdminLayoutComponent } from './admin-layout/admin-layout';

export const adminRoutes: Routes = [
  {
    path: '',
    component: AdminLayoutComponent,
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./tenants/tenant-list').then((m) => m.TenantListComponent),
      },
      {
        path: 'tenants/novo',
        loadComponent: () =>
          import('./tenants/tenant-form').then((m) => m.TenantFormComponent),
      },
      {
        path: 'tenants/:id',
        loadComponent: () =>
          import('./tenants/tenant-detail').then((m) => m.TenantDetailComponent),
      },
      {
        path: 'plans',
        loadComponent: () =>
          import('./plans/plan-management').then((m) => m.PlanManagementComponent),
      },
      {
        path: 'users',
        loadComponent: () =>
          import('./users/user-management').then((m) => m.UserManagementComponent),
      },
      {
        path: 'users/novo',
        loadComponent: () =>
          import('./users/user-create').then((m) => m.UserCreateComponent),
      },
      {
        path: 'users/:id',
        loadComponent: () =>
          import('./users/user-detail').then((m) => m.UserDetailComponent),
      },
    ],
  },
];
