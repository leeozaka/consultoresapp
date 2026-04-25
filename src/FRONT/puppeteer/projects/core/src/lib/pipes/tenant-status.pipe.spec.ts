import { describe, it, expect } from 'vitest';
import { TenantStatusPipe } from './tenant-status.pipe';
import { TenantStatus } from '../models/tenant.model';

describe('TenantStatusPipe', () => {
  const pipe = new TenantStatusPipe();

  it('maps Active to "Ativo"', () => {
    expect(pipe.transform(TenantStatus.Active)).toBe('Ativo');
  });

  it('maps Pending to "Pendente"', () => {
    expect(pipe.transform(TenantStatus.Pending)).toBe('Pendente');
  });

  it('maps Suspended to "Suspenso"', () => {
    expect(pipe.transform(TenantStatus.Suspended)).toBe('Suspenso');
  });

  it('maps Archived to "Arquivado"', () => {
    expect(pipe.transform(TenantStatus.Archived)).toBe('Arquivado');
  });
});
