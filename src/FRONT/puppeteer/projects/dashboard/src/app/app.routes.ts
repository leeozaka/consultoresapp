import { Router, Routes } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService, authGuard, roleGuard } from '@consultores/core';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./layouts/public-layout/public-layout.component').then(m => m.PublicLayoutComponent),
    children: [
      {
        path: '',
        loadChildren: () =>
          import('./features/landing/landing.routes').then(m => m.landingRoutes),
      },
      {
        path: 'auth',
        loadChildren: () =>
          import('./features/auth/auth.routes').then(m => m.authRoutes),
      },
      {
        path: 'onboarding',
        loadChildren: () =>
          import('./features/onboarding/onboarding.routes').then(m => m.onboardingRoutes),
      },
    ],
  },
  {
    path: 'dashboard',
    canActivate: [authGuard, () => {
      const auth = inject(AuthService);
      const router = inject(Router);
      if (auth.isSuperAdmin()) {
        return router.createUrlTree(['/admin']);
      }
      return true;
    }],
    loadChildren: () =>
      import('./features/dashboard/dashboard.routes').then(m => m.dashboardRoutes),
  },
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard(['SuperAdmin'])],
    loadChildren: () =>
      import('./features/admin/admin.routes').then(m => m.adminRoutes),
  },
  { path: '**', redirectTo: '' },
];
