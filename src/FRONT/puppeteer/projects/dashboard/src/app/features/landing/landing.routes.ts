import { Routes } from '@angular/router';

export const landingRoutes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./home/home').then((m) => m.HomeComponent),
  },
];
