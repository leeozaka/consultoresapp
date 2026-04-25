import { Component, ElementRef, OnInit, ViewChild, HostListener, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ThreeJsService } from '../../services/three-js.service';

@Component({
  selector: 'app-webgl-background',
  standalone: true,
  template: `
    <canvas #glCanvas class="fixed inset-0 -z-10 w-full h-full pointer-events-none"></canvas>
  `,
  styles: [`
    canvas {
      background: #020408;
    }
  `]
})
export class WebGLBackgroundComponent implements OnInit {
  @ViewChild('glCanvas', { static: true }) canvasRef!: ElementRef<HTMLCanvasElement>;

  constructor(
    private readonly threeJsService: ThreeJsService,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {}

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.threeJsService.init(this.canvasRef.nativeElement);
    }
  }

  @HostListener('window:scroll')
  onWindowScroll() {
    if (isPlatformBrowser(this.platformId)) {
      this.threeJsService.updateScroll(window.scrollY);
    }
  }
}
