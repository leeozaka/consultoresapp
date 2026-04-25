import { Routes } from '@angular/router';

export const authRoutes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./login/login').then((m) => m.LoginComponent),
  },
  {
    path: 'callback',
    loadComponent: () =>
      import('./callback/callback').then((m) => m.CallbackComponent),
  },
  {
    path: 'logout',
    loadComponent: () =>
      import('./logout/logout').then((m) => m.LogoutComponent),
  },
];
