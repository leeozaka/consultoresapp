import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { WebGLBackgroundComponent } from '@consultores/core';

@Component({
  selector: 'app-public-layout',
  standalone: true,
  imports: [RouterOutlet, WebGLBackgroundComponent],
  template: `
    <app-webgl-background></app-webgl-background>
    <div class="relative z-0 min-h-screen text-slate-50">
      <router-outlet></router-outlet>
    </div>
  `,
  styles: []
})
export class PublicLayoutComponent {}
