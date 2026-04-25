import { ChangeDetectionStrategy, Component, inject, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TenantContextService } from '@consultores/core';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomeComponent {
  private readonly tenantContext = inject(TenantContextService);
  protected readonly tenant = this.tenantContext.currentTenant;
  protected readonly displayName = computed(
    () => this.tenant()?.branding?.agencyDisplayName ?? this.tenant()?.name ?? '',
  );
}
