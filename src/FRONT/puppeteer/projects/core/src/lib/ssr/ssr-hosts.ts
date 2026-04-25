const DEFAULT_ALLOWED_HOSTS = [
  'consultor.localhost',
  '*.consultor.localhost',
  'localhost',
  '127.0.0.1',
] as const;

export function getSsrAllowedHosts(env: Record<string, string | undefined>): string[] {
  const configuredHosts = env['NG_ALLOWED_HOSTS']
    ?.split(',')
    .map((host) => host.trim())
    .filter((host) => host.length > 0) ?? [];

  return [...new Set([...DEFAULT_ALLOWED_HOSTS, ...configuredHosts])];
}
