import type { AppEnvironment } from './app-environment';

export const environment = {
  production: true,
  label: '',
  apiBaseUrl: '',
  imageCacheTtlMs: 15 * 60 * 1000,
  oidc: {
    issuer: 'https://api.itcorretor.com',
    clientId: 'consultor-spa',
    redirectUri: 'https://itcorretor.com/auth/callback',
    postLogoutRedirectUri: 'https://itcorretor.com',
    scope: 'openid profile email roles offline_access',
    responseType: 'code',
  },
  landingDomain: 'itcorretor.com',
} satisfies AppEnvironment;
