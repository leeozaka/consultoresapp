import { ChangeDetectionStrategy, Component, inject, computed } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { TenantContextService, LoadingSpinnerComponent } from '@consultores/core';

@Component({
  selector: 'app-tenant-layout',
  imports: [RouterOutlet, RouterLink, LoadingSpinnerComponent],
  templateUrl: './tenant-layout.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TenantLayoutComponent {
  protected readonly tenantContext = inject(TenantContextService);
  protected readonly tenant = this.tenantContext.currentTenant;
  protected readonly isLoading = this.tenantContext.isLoading;

  protected readonly displayName = computed(
    () => this.tenant()?.branding?.agencyDisplayName ?? this.tenant()?.name ?? '',
  );
  protected readonly tagline = computed(
    () => this.tenant()?.branding?.tagline ?? '',
  );
  protected readonly logoUrl = computed(
    () => this.tenant()?.branding?.logoUrl ?? null,
  );
  protected readonly currentYear = new Date().getFullYear();
}
