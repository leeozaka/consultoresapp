import type { AppEnvironment } from './app-environment';

export const environment = {
  production: true,
  label: 'Staging',
  apiBaseUrl: '',
  imageCacheTtlMs: 5 * 60 * 1000,
  oidc: {
    issuer: 'https://api.staging.consultor.app',
    clientId: 'consultor-spa',
    redirectUri: 'https://staging.consultor.app/auth/callback',
    postLogoutRedirectUri: 'https://staging.consultor.app',
    scope: 'openid profile email roles offline_access',
    responseType: 'code',
  },
  landingDomain: 'staging.consultor.app',
} satisfies AppEnvironment;
