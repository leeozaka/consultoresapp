import { ChangeDetectionStrategy, Component, input, computed } from '@angular/core';

export type SpinnerSize = 'sm' | 'md' | 'lg' | 'xl';

@Component({
  selector: 'app-loading-spinner',
  templateUrl: './loading-spinner.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoadingSpinnerComponent {
  readonly size = input<SpinnerSize>('md');
  readonly label = input<string>('Carregando...');

  protected readonly svgClass = computed(() => {
    const map: Record<SpinnerSize, string> = {
      sm: 'size-4',
      md: 'size-6',
      lg: 'size-10',
      xl: 'size-16',
    };
    return `animate-spin ${map[this.size()]}`;
  });
}
