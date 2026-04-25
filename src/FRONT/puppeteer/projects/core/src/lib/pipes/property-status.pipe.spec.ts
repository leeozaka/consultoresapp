import { describe, it, expect } from 'vitest';
import { PropertyStatusPipe } from './property-status.pipe';
import { PropertyStatus } from '../models/property.model';

describe('PropertyStatusPipe', () => {
  const pipe = new PropertyStatusPipe();

  it('maps Active to "Ativo"', () => {
    expect(pipe.transform(PropertyStatus.Active)).toBe('Ativo');
  });

  it('maps Draft to "Rascunho"', () => {
    expect(pipe.transform(PropertyStatus.Draft)).toBe('Rascunho');
  });

  it('maps Sold to "Vendido"', () => {
    expect(pipe.transform(PropertyStatus.Sold)).toBe('Vendido');
  });

  it('maps Rented to "Alugado"', () => {
    expect(pipe.transform(PropertyStatus.Rented)).toBe('Alugado');
  });

  it('maps UnderOffer to "Proposta Recebida"', () => {
    expect(pipe.transform(PropertyStatus.UnderOffer)).toBe('Proposta Recebida');
  });

  it('maps Archived to "Arquivado"', () => {
    expect(pipe.transform(PropertyStatus.Archived)).toBe('Arquivado');
  });
});
