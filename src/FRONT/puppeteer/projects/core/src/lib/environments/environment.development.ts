import type { AppEnvironment } from './app-environment';

export const environment = {
  production: false,
  label: 'Development',
  apiBaseUrl: '',
  imageCacheTtlMs: 5 * 60 * 1000,
  oidc: {
    issuer: 'http://localhost:5001',
    clientId: 'consultor-spa',
    redirectUri: 'http://localhost:4200/auth/callback',
    postLogoutRedirectUri: 'http://localhost:4200',
    scope: 'openid profile email roles offline_access',
    responseType: 'code',
  },
  landingDomain: 'localhost',
} satisfies AppEnvironment;

