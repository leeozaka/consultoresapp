import { Routes } from '@angular/router';
import { DashboardLayoutComponent } from './dashboard-layout/dashboard-layout';
import { roleGuard } from '@consultores/core';

export const dashboardRoutes: Routes = [
  {
    path: '',
    component: DashboardLayoutComponent,
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./overview/overview').then((m) => m.OverviewComponent),
      },
      {
        path: 'imoveis',
        loadComponent: () =>
          import('./properties/property-management').then((m) => m.PropertyManagementComponent),
      },
      {
        path: 'imoveis/novo',
        loadComponent: () =>
          import('./properties/property-form').then((m) => m.PropertyFormComponent),
      },
      {
        path: 'imoveis/:id',
        loadComponent: () =>
          import('./properties/property-form').then((m) => m.PropertyFormComponent),
      },
      {
        path: 'meu-site',
        loadComponent: () =>
          import('./site-settings/site-settings').then((m) => m.SiteSettingsComponent),
      },
      {
        path: 'plano',
        canActivate: [roleGuard(['TenantAdmin'])],
        loadComponent: () =>
          import('./plan-selector/plan-selector').then((m) => m.PlanSelectorComponent),
      },
    ],
  },
];
