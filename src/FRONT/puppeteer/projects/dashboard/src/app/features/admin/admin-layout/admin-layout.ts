import { ChangeDetectionStrategy, Component, inject, computed } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from '@consultores/core';
import {
  SidebarShellComponent,
  SidebarBrand,
  SidebarNavGroup,
  SidebarNavItem,
  SidebarUser,
} from '../../../layouts/sidebar-shell/sidebar-shell';

@Component({
  selector: 'app-admin-layout',
  imports: [RouterOutlet, SidebarShellComponent],
  templateUrl: './admin-layout.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminLayoutComponent {
  private readonly auth = inject(AuthService);

  protected readonly brand: SidebarBrand = {
    icon: 'pi-shield',
    title: 'Super Admin',
    subtitle: 'ITConsultor',
    iconBg: '#e11d48',
  };

  protected readonly navGroups: SidebarNavGroup[] = [
    {
      items: [
        { label: 'Tenants',  icon: 'pi-building',    path: '/admin',        exact: true  },
        { label: 'Usuários', icon: 'pi-users',        path: '/admin/users',  exact: false },
        { label: 'Planos',   icon: 'pi-credit-card',  path: '/admin/plans',  exact: false },
      ],
    },
  ];

  protected readonly footerNav: SidebarNavItem[] = [
    { label: 'Voltar ao Painel', icon: 'pi-arrow-left', path: '/dashboard' },
  ];

  protected readonly sidebarUser = computed<SidebarUser | null>(() => {
    const u = this.auth.currentUser();
    if (!u) return null;
    return {
      initials: `${u.firstName[0]}${u.lastName[0]}`,
      name: `${u.firstName} ${u.lastName}`,
      email: u.email,
    };
  });

  protected logout(): void {
    this.auth.logout();
  }
}
