import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

export interface SidebarNavItem {
  label: string;
  icon: string;
  path: string;
  exact?: boolean;
  color?: string;
}

export interface SidebarNavGroup {
  label?: string;
  items: SidebarNavItem[];
}

export interface SidebarBrand {
  icon: string;
  title: string;
  subtitle: string;
  iconBg: string;
}

export interface SidebarUser {
  initials: string;
  name: string;
  email: string;
}

@Component({
  selector: 'app-sidebar-shell',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar-shell.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[style.--sidebar-accent]':
      'theme() === "dark" ? "#fb7185" : "var(--brand-primary, #1A73E8)"',
    '[style.--sidebar-accent-bg]':
      'theme() === "dark" ? "rgba(225,29,72,.15)" : "color-mix(in srgb, var(--brand-primary, #1A73E8) 12%, transparent)"',
  },
  styles: `
    :host { display: contents }
    .sidebar-active {
      background: var(--sidebar-accent-bg) !important;
      color: var(--sidebar-accent) !important;
    }
  `,
})
export class SidebarShellComponent {
  theme = input<'light' | 'dark'>('light');
  brand = input.required<SidebarBrand>();
  navGroups = input.required<SidebarNavGroup[]>();
  footerNav = input<SidebarNavItem[]>([]);
  user = input<SidebarUser | null>(null);

  logoutClicked = output<void>();
}
