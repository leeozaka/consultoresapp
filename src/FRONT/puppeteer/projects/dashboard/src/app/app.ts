import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastComponent, environment } from '@consultores/core';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastComponent],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  private readonly document = inject(DOCUMENT);
  protected readonly devLabel = environment.label;

  constructor() {
    this.document.documentElement.style.setProperty(
      '--dev-environment-banner-height',
      environment.label ? '2rem' : '0px',
    );
  }
}
