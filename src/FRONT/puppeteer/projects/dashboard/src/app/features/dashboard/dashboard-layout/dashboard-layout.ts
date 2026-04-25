import { ChangeDetectionStrategy, Component, inject, computed, OnInit, OnDestroy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService, TenantContextService, PaymentSseService } from '@consultores/core';
// import { PERK_REGISTRY } from '@consultores/core';
import {
  SidebarShellComponent,
  SidebarBrand,
  SidebarNavGroup,
  SidebarNavItem,
  SidebarUser,
} from '../../../layouts/sidebar-shell/sidebar-shell';

@Component({
  selector: 'app-dashboard-layout',
  imports: [RouterOutlet, SidebarShellComponent],
  templateUrl: './dashboard-layout.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardLayoutComponent implements OnInit, OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly tenantContext = inject(TenantContextService);
  private readonly paymentSse = inject(PaymentSseService);

  private readonly user = this.auth.currentUser;
  private readonly isTenantAdmin = computed(() => this.auth.hasRole('TenantAdmin'));

  protected readonly brand = computed<SidebarBrand>(() => ({
    icon: 'pi-building',
    title:
      this.tenantContext.currentTenant()?.branding?.agencyDisplayName ??
      this.tenantContext.currentTenant()?.name ??
      'Painel',
    subtitle: 'Painel de Gestão',
    iconBg: 'var(--brand-primary, #1A73E8)',
  }));

  private readonly baseNavItems: SidebarNavItem[] = [
    { label: 'Visão Geral',  icon: 'pi-home',     path: '/dashboard',            exact: true  },
    { label: 'Imóveis',      icon: 'pi-building', path: '/dashboard/imoveis',    exact: false },
    { label: 'Meu Site',     icon: 'pi-globe',    path: '/dashboard/meu-site',   exact: false },
  ];

  private readonly adminNavItems: SidebarNavItem[] = [
    { label: 'Meu Plano', icon: 'pi-credit-card', path: '/dashboard/plano', exact: false },
  ];

  protected readonly navGroups = computed<SidebarNavGroup[]>(() => {
    const mainItems = this.isTenantAdmin()
      ? [...this.baseNavItems, ...this.adminNavItems]
      : this.baseNavItems;

    return [{ items: mainItems }];
  });

  protected readonly footerNav = computed<SidebarNavItem[]>(() =>
    this.auth.isSuperAdmin()
      ? [{ label: 'Administração', icon: 'pi-shield', path: '/admin', color: '#e11d48' }]
      : [],
  );

  protected readonly sidebarUser = computed<SidebarUser | null>(() => {
    const u = this.user();
    if (!u) return null;
    return {
      initials: `${u.firstName[0]}${u.lastName[0]}`,
      name: `${u.firstName} ${u.lastName}`,
      email: u.email,
    };
  });

  ngOnInit(): void {
    if (this.isTenantAdmin()) {
      this.paymentSse.connect();
    }
  }

  ngOnDestroy(): void {
    this.paymentSse.disconnect();
  }

  protected logout(): void {
    this.auth.logout();
  }
}
