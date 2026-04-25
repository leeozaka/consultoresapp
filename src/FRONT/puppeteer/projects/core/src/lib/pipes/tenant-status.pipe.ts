import { Pipe, PipeTransform } from '@angular/core';
import { TenantStatus } from '../models/tenant.model';

const STATUS_LABELS: Record<TenantStatus, string> = {
  [TenantStatus.Active]: 'Ativo',
  [TenantStatus.Pending]: 'Pendente',
  [TenantStatus.Suspended]: 'Suspenso',
  [TenantStatus.Archived]: 'Arquivado',
};

@Pipe({ name: 'tenantStatus' })
export class TenantStatusPipe implements PipeTransform {
  transform(status: TenantStatus): string {
    return STATUS_LABELS[status] ?? status;
  }
}
