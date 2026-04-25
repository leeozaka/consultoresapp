export interface AppOidcConfig {
  issuer: string;
  clientId: string;
  redirectUri: string;
  postLogoutRedirectUri: string;
  scope: string;
  responseType: string;
}

/**
 * Build-time environment (see `angular.json` `fileReplacements`).
 * Optional fields belong only in the variants that need them.
 */
export interface AppEnvironment {
  production: boolean;
  label: string;
  apiBaseUrl: string;
  imageCacheTtlMs: number;
  oidc: AppOidcConfig;
  landingDomain: string;
}
