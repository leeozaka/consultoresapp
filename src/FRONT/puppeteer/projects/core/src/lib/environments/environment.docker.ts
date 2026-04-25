import type { AppEnvironment } from './app-environment';

export const environment = {
  production: false,
  label: 'Docker',
  // Relative API URLs; OIDC issuer must match the origin you open in the browser.
  apiBaseUrl: '',
  imageCacheTtlMs: 5 * 60 * 1000,
  oidc: {
    issuer: 'http://consultor.localhost',
    clientId: 'consultor-spa',
    redirectUri: 'http://consultor.localhost/auth/callback',
    postLogoutRedirectUri: 'http://consultor.localhost',
    scope: 'openid profile email roles offline_access',
    responseType: 'code',
  },
  landingDomain: 'consultor.localhost',
} satisfies AppEnvironment;
