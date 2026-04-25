import { ChangeDetectionStrategy, Component, input, output, computed } from '@angular/core';
import { PORTAL_THEMES, type PortalThemeDefinition } from '../../portal/theme-registry';
import type { Plan } from '@consultores/core';

@Component({
  selector: 'app-theme-selector',
  templateUrl: './theme-selector.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ThemeSelectorComponent {
  readonly currentTheme = input.required<string>();
  readonly allPlans = input<Plan[]>([]);
  readonly currentPlanId = input<string | null>(null);

  readonly themeSelected = output<string>();

  protected readonly themes = PORTAL_THEMES;

  protected readonly currentPlanTheme = computed(() => {
    const planId = this.currentPlanId();
    if (!planId) return 'default';
    const plan = this.allPlans().find((p) => p.id === planId);
    return plan?.portalTheme ?? 'default';
  });

  protected isThemeLocked(theme: PortalThemeDefinition): boolean {
    const planTheme = this.currentPlanTheme();
    const themeOrder = ['default', 'minimal', 'premium'];
    return themeOrder.indexOf(theme.id) > themeOrder.indexOf(planTheme);
  }

  protected getRequiredPlanName(theme: PortalThemeDefinition): string {
    const plan = this.allPlans().find((p) => p.portalTheme === theme.id);
    return plan?.name ?? theme.name;
  }

  protected selectTheme(theme: PortalThemeDefinition): void {
    this.themeSelected.emit(theme.id);
  }
}
