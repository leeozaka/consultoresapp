import { ChangeDetectionStrategy, Component, computed, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Property, PropertyStatus, PropertyService, BillingService, AuthService, LoadingSpinnerComponent, PropertyStatusPipe, CachedSrcDirective } from '@consultores/core';

interface StatCard {
  label: string;
  value: number;
  icon: string;
  color: string;
}

@Component({
  selector: 'app-overview',
  imports: [RouterLink, LoadingSpinnerComponent, PropertyStatusPipe, CachedSrcDirective, DatePipe],
  templateUrl: './overview.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OverviewComponent implements OnInit {
  private readonly propertyService = inject(PropertyService);
  private readonly billingService = inject(BillingService);
  private readonly auth = inject(AuthService);

  protected readonly isLoading = signal(true);
  protected readonly recentProperties = signal<Property[]>([]);
  protected readonly stats = signal<StatCard[]>([]);
  protected readonly userName = signal('');
  protected readonly gracePeriodEndsAt = signal<string | null>(null);

  protected readonly isTenantAdmin = computed(() => this.auth.hasRole('TenantAdmin'));

  protected readonly showGracePeriodAlert = computed(
    () => this.isTenantAdmin() && this.gracePeriodEndsAt() !== null,
  );

  ngOnInit(): void {
    const user = this.auth.currentUser();
    this.userName.set(user?.firstName ?? '');

    this.propertyService.search({ page: 1, pageSize: 5 }).subscribe({
      next: (res) => {
        this.recentProperties.set(res.items);

        const total = res.totalCount;
        const active = res.items.filter((p) => p.status === PropertyStatus.Active).length;
        const draft = res.items.filter((p) => p.status === PropertyStatus.Draft).length;

        this.stats.set([
          { label: 'Total de Imóveis', value: total, icon: 'pi-building', color: '#1A73E8' },
          { label: 'Ativos', value: active, icon: 'pi-check-circle', color: '#22c55e' },
          { label: 'Rascunhos', value: draft, icon: 'pi-pencil', color: '#f59e0b' },
        ]);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });

    if (this.auth.hasRole('TenantAdmin')) {
      this.billingService.getOverview().subscribe({
        next: (overview) => {
          this.gracePeriodEndsAt.set(overview.gracePeriodEndsAt ?? null);
        },
      });
    }
  }
}
