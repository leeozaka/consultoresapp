import { describe, it, expect } from 'vitest';
import { portalUrlForSlugFromEnv, type PortalUrlEnvSlice } from './portal-url';

describe('portalUrlForSlugFromEnv', () => {
  it('uses OIDC issuer host and port for Docker', () => {
    expect(
      portalUrlForSlugFromEnv('demo', {
        label: 'Docker',
        landingDomain: 'consultor.localhost',
        oidc: { issuer: 'http://consultor.localhost' } as PortalUrlEnvSlice['oidc'],
      }),
    ).toBe('http://demo.consultor.localhost');
  });

  it('returns null when label is not Docker', () => {
    expect(
      portalUrlForSlugFromEnv('acme', {
        label: 'Staging',
        landingDomain: 'staging.consultor.app',
        oidc: { issuer: 'https://api.staging.consultor.app' } as PortalUrlEnvSlice['oidc'],
      }),
    ).toBeNull();
  });
});
