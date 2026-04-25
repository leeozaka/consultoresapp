import { Routes } from '@angular/router';

export const propertyRoutes: Routes = [
  { path: '', loadComponent: () => import('./property-list').then(m => m.PropertyListComponent) },
  { path: ':id', loadComponent: () => import('./property-detail').then(m => m.PropertyDetailComponent) },
];
