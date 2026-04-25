import { Routes } from '@angular/router';

export const onboardingRoutes: Routes = [
  {
    path: 'signup',
    loadComponent: () =>
      import('./signup-wizard/signup-wizard').then((m) => m.SignupWizardComponent),
  },
  {
    path: 'status/:tenantId',
    loadComponent: () =>
      import('./onboarding-status/onboarding-status').then((m) => m.OnboardingStatusComponent),
  },
  {
    path: '',
    redirectTo: 'signup',
    pathMatch: 'full',
  },
];
