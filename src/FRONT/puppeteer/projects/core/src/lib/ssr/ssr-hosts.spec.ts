import { describe, expect, it } from 'vitest';
import { getSsrAllowedHosts } from './ssr-hosts';

describe('getSsrAllowedHosts', () => {
  it('includes consultor localhost subdomains by default', () => {
    expect(getSsrAllowedHosts({})).toEqual([
      'consultor.localhost',
      '*.consultor.localhost',
      'localhost',
      '127.0.0.1',
    ]);
  });

  it('appends configured hosts from the environment', () => {
    expect(getSsrAllowedHosts({ NG_ALLOWED_HOSTS: 'demo.consultor.localhost,custom.consultor.localhost' })).toEqual([
      'consultor.localhost',
      '*.consultor.localhost',
      'localhost',
      '127.0.0.1',
      'demo.consultor.localhost',
      'custom.consultor.localhost',
    ]);
  });
});
