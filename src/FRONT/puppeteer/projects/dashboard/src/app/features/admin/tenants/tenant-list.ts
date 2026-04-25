import { ChangeDetectionStrategy, Component, inject, signal, computed, OnInit, OnDestroy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { Tenant, TenantStatus, SiteBuildStatusEvent, TenantService, LoadingSpinnerComponent, TenantStatusPipe } from '@consultores/core';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-tenant-list',
  imports: [
    DatePipe,
    RouterLink,
    FormsModule,
    LoadingSpinnerComponent,
    TenantStatusPipe,
    TableModule,
    TagModule,
    ButtonModule,
    InputTextModule,
    SelectModule,
  ],
  templateUrl: './tenant-list.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TenantListComponent implements OnInit, OnDestroy {
  private readonly tenantService = inject(TenantService);
  private readonly buildSubscriptions = new Map<string, Subscription>();

  protected readonly tenants = signal<Tenant[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly searchTerm = signal('');
  protected readonly statusFilter = signal<TenantStatus | null>(null);
  protected readonly buildStatusByTenant = signal<Record<string, SiteBuildStatusEvent>>({});

  protected readonly statusOptions = [
    { label: 'Todos', value: null },
    { label: 'Ativo', value: TenantStatus.Active },
    { label: 'Pendente', value: TenantStatus.Pending },
    { label: 'Suspenso', value: TenantStatus.Suspended },
    { label: 'Arquivado', value: TenantStatus.Archived },
  ];

  protected readonly filteredTenants = computed(() => {
    let list = this.tenants();
    const search = this.searchTerm().toLowerCase().trim();
    const status = this.statusFilter();

    if (search) {
      list = list.filter(
        (t) =>
          t.name.toLowerCase().includes(search) ||
          t.slug.toLowerCase().includes(search) ||
          t.contactEmail.toLowerCase().includes(search),
      );
    }
    if (status) {
      list = list.filter((t) => t.status === status);
    }
    return list;
  });

  protected readonly stats = computed(() => {
    const all = this.tenants();
    return {
      total: all.length,
      active: all.filter((t) => t.status === TenantStatus.Active).length,
      pending: all.filter((t) => t.status === TenantStatus.Pending).length,
      suspended: all.filter((t) => t.status === TenantStatus.Suspended).length,
    };
  });

  ngOnInit(): void {
    this.tenantService.getAll().subscribe({
      next: (tenants) => {
        this.tenants.set(tenants);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }

  ngOnDestroy(): void {
    this.buildSubscriptions.forEach((sub) => sub.unsubscribe());
    this.buildSubscriptions.clear();
  }

  protected activate(id: string, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.tenantService.activate(id).subscribe({
      next: () => {
        this.tenants.update((list) =>
          list.map((t) => (t.id === id ? { ...t, status: TenantStatus.Active } : t)),
        );
      },
    });
  }

  protected suspend(id: string, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    if (confirm('Tem certeza que deseja suspender este tenant?')) {
      this.tenantService.suspend(id).subscribe({
        next: () => {
          this.tenants.update((list) =>
            list.map((t) => (t.id === id ? { ...t, status: TenantStatus.Suspended } : t)),
          );
        },
      });
    }
  }

  protected approve(id: string, event: Event): void {
    event.preventDefault();
    event.stopPropagation();

    this.tenantService.approve(id).subscribe({
      next: () => {
        this.tenants.update((list) =>
          list.map((t) => (t.id === id ? { ...t, status: TenantStatus.Active } : t)),
        );

        if (!this.buildSubscriptions.has(id)) {
          const subscription = this.tenantService.observeSiteBuildEvents(id).subscribe({
            next: (status) => {
              this.buildStatusByTenant.update((current) => ({ ...current, [id]: status }));
            },
          });
          this.buildSubscriptions.set(id, subscription);
        }
      },
    });
  }

  protected buildStatus(id: string): string | null {
    return this.buildStatusByTenant()[id]?.message ?? null;
  }

  protected getStatusSeverity(status: TenantStatus): 'success' | 'warn' | 'danger' | 'secondary' {
    switch (status) {
      case TenantStatus.Active:
        return 'success';
      case TenantStatus.Pending:
        return 'warn';
      case TenantStatus.Suspended:
        return 'danger';
      default:
        return 'secondary';
    }
  }
}
