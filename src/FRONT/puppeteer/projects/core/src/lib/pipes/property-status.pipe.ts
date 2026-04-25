import { Pipe, PipeTransform } from '@angular/core';
import { PropertyStatus } from '../models/property.model';

const STATUS_LABELS: Record<PropertyStatus, string> = {
  [PropertyStatus.Active]: 'Ativo',
  [PropertyStatus.Draft]: 'Rascunho',
  [PropertyStatus.Sold]: 'Vendido',
  [PropertyStatus.Rented]: 'Alugado',
  [PropertyStatus.UnderOffer]: 'Proposta Recebida',
  [PropertyStatus.Archived]: 'Arquivado',
};

@Pipe({ name: 'propertyStatus' })
export class PropertyStatusPipe implements PipeTransform {
  transform(status: PropertyStatus): string {
    return STATUS_LABELS[status] ?? status;
  }
}
