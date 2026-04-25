import { environment, type AppEnvironment } from '@consultores/core';

export type PortalUrlEnvSlice = Pick<AppEnvironment, 'label' | 'oidc' | 'landingDomain'>;

/** Pure helper — unit-tested without mocking the build-time `environment` import. */
export function portalUrlForSlugFromEnv(slug: string, env: PortalUrlEnvSlice): string | null {
  if (env.label !== 'Docker') {
    return null;
  }
  try {
    const { protocol, port, hostname } = new URL(env.oidc.issuer);
    if (hostname !== env.landingDomain) {
      return null;
    }
    const host = `${slug}.${env.landingDomain}`;
    return port ? `${protocol}//${host}:${port}` : `${protocol}//${host}`;
  } catch {
    return null;
  }
}

/** Public portal base URL for a tenant slug (k8s Docker: follows OIDC issuer host:port). */
export function portalUrlForSlug(slug: string): string | null {
  return portalUrlForSlugFromEnv(slug, environment);
}
