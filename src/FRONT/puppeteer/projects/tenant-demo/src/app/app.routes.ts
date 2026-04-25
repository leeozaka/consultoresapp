import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layouts/tenant-layout/tenant-layout').then(m => m.TenantLayoutComponent),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/home/home').then(m => m.HomeComponent),
      },
      {
        path: 'imoveis',
        loadChildren: () =>
          import('./features/properties/property.routes').then(m => m.propertyRoutes),
      },
      {
        path: 'contato',
        loadComponent: () =>
          import('./features/contact/contact').then(m => m.ContactComponent),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
