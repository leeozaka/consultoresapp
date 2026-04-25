import { Injectable, NgZone, OnDestroy, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import * as THREE from 'three';

const NEON_PURPLE = 0x8b5cf6;
const NEON_PINK   = 0xf43f5e;
const NEON_CYAN   = 0x22d3ee;

// This shit was strictly vibecoded, so if you're reading this now, PR's are welcome for improvements.
@Injectable({ providedIn: 'root' })
export class ThreeJsService implements OnDestroy {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly ngZone = inject(NgZone);

  private renderer!: THREE.WebGLRenderer;
  private scene!: THREE.Scene;
  private camera!: THREE.PerspectiveCamera;
  private animationFrameId: number | null = null;
  private readonly floatingObjects: THREE.Object3D[] = [];
  private scrollY = 0;
  private targetScrollY = 0;
  private resizeHandler: (() => void) | null = null;

  public init(canvas: HTMLCanvasElement): void {
    if (!this.isBrowser || this.renderer) return;

    this.scene = new THREE.Scene();
    this.scene.fog = new THREE.FogExp2(0x020408, 0.035);

    this.camera = new THREE.PerspectiveCamera(70, window.innerWidth / window.innerHeight, 0.1, 1000);
    this.camera.position.z = 10;

    this.renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: true, powerPreference: 'high-performance' });
    this.renderer.setSize(window.innerWidth, window.innerHeight);
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.toneMapping = THREE.ACESFilmicToneMapping;

    this.setupLighting();
    this.setupObjects();

    this.resizeHandler = this.onWindowResize.bind(this);
    window.addEventListener('resize', this.resizeHandler);
    this.ngZone.runOutsideAngular(() => this.startAnimationLoop());
  }

  private setupLighting(): void {
    this.scene.add(new THREE.AmbientLight(0x0d0520, 6));

    const key = new THREE.DirectionalLight(0xffffff, 1.5);
    key.position.set(5, 10, 5);
    this.scene.add(key);

    const purplePoint = new THREE.PointLight(NEON_PURPLE, 8, 18);
    purplePoint.position.set(-6, 3, 0);
    this.scene.add(purplePoint);

    const pinkPoint = new THREE.PointLight(NEON_PINK, 8, 18);
    pinkPoint.position.set(6, -2, -2);
    this.scene.add(pinkPoint);

    const cyanPoint = new THREE.PointLight(NEON_CYAN, 6, 18);
    cyanPoint.position.set(1, 6, -5);
    this.scene.add(cyanPoint);
  }

  private makeFill(geo: THREE.BufferGeometry, color: number): THREE.Mesh {
    return new THREE.Mesh(
      geo,
      new THREE.MeshStandardMaterial({
        color,
        transparent: true,
        opacity: 0.07,
        side: THREE.DoubleSide,
        depthWrite: false,
        roughness: 0.1,
        metalness: 0.6,
        emissive: new THREE.Color(color),
        emissiveIntensity: 0.05,
      })
    );
  }

  private makeEdges(geo: THREE.BufferGeometry, color: number): THREE.LineSegments {
    return new THREE.LineSegments(
      new THREE.EdgesGeometry(geo),
      new THREE.LineBasicMaterial({ color, fog: true })
    );
  }

  private addShape(
    parent: THREE.Group,
    geo: THREE.BufferGeometry,
    color: number,
    x = 0, y = 0, z = 0,
    ry = 0
  ): void {
    const fill  = this.makeFill(geo, color);
    const edges = this.makeEdges(geo, color);
    fill.position.set(x, y, z);   fill.rotation.y  = ry;
    edges.position.set(x, y, z); edges.rotation.y = ry;
    parent.add(fill, edges);
  }

  private createSkyscraper(): THREE.Group {
    const g = new THREE.Group();
    const c = NEON_PURPLE;
    this.addShape(g, new THREE.BoxGeometry(2.4, 0.4, 2.4), c, 0, -2.6, 0);
    this.addShape(g, new THREE.BoxGeometry(1.6, 4.5, 1.6), c, 0, 0, 0);
    this.addShape(g, new THREE.BoxGeometry(1.2, 1.5, 1.2), c, 0, 2.5, 0);
    this.addShape(g, new THREE.BoxGeometry(0.4, 1.2, 0.4), c, 0, 3.85, 0);
    this.addShape(g, new THREE.BoxGeometry(1.8, 0.15, 1.8), c, 0, -0.5, 0);
    return g;
  }

  private createHouse(): THREE.Group {
    const g = new THREE.Group();
    const c = NEON_PINK;
    this.addShape(g, new THREE.BoxGeometry(2.8, 0.2, 2.2), c, 0, -1.1, 0);
    this.addShape(g, new THREE.BoxGeometry(2.5, 1.8, 2), c, 0, -0.1, 0);
    this.addShape(g, new THREE.CylinderGeometry(0, 1.7, 1.4, 4), c, 0, 0.97, 0, Math.PI / 4);
    this.addShape(g, new THREE.BoxGeometry(1.1, 1, 1.2), c, -0.7, -0.65, 0);
    this.addShape(g, new THREE.BoxGeometry(0.28, 0.8, 0.28), c, 0.6, 1.5, 0);
    return g;
  }

  private createApartmentBlock(): THREE.Group {
    const g = new THREE.Group();
    const c = NEON_CYAN;
    this.addShape(g, new THREE.BoxGeometry(4, 2.2, 1.4), c, 0, -0.8, 0);
    this.addShape(g, new THREE.BoxGeometry(1.1, 4.2, 1.4), c, -1.6, 0.8, 0);
    this.addShape(g, new THREE.BoxGeometry(1.1, 5, 1.4), c,  1.6, 1.2, 0);
    this.addShape(g, new THREE.BoxGeometry(4, 0.2, 1.4), c, 0, 0.6, 0);
    this.addShape(g, new THREE.BoxGeometry(4.5, 0.3, 1.6), c, 0, -1.95, 0);
    return g;
  }

  private setupObjects(): void {
    const skyscraper = this.createSkyscraper();
    skyscraper.position.set(-5.5, 0, -4);
    this.scene.add(skyscraper);
    this.floatingObjects.push(skyscraper);

    const house = this.createHouse();
    house.position.set(5, -1, -5);
    this.scene.add(house);
    this.floatingObjects.push(house);

    const apartments = this.createApartmentBlock();
    apartments.position.set(0.5, 4.5, -9);
    this.scene.add(apartments);
    this.floatingObjects.push(apartments);
  }

  public updateScroll(scrollY: number): void {
    this.targetScrollY = scrollY;
  }

  private onWindowResize(): void {
    if (!this.camera || !this.renderer) return;
    this.camera.aspect = window.innerWidth / window.innerHeight;
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(window.innerWidth, window.innerHeight);
  }

  private startAnimationLoop(): void {
    const tick = () => {
      this.animationFrameId = requestAnimationFrame(tick);

      this.scrollY += (this.targetScrollY - this.scrollY) * 0.05;

      const time = Date.now() * 0.0004;

      this.floatingObjects.forEach((obj, index) => {
        obj.rotation.y = time * (0.18 + index * 0.08);
        obj.rotation.x = Math.sin(time * 0.6 + index) * 0.08;

        if (obj.userData['originalY'] === undefined) {
          obj.userData['originalY'] = obj.position.y;
        }

        const float     = Math.sin(time * 1.2 + index * 1.5) * 0.45;
        const parallax  = this.scrollY * 0.003;
        obj.position.y  = (obj.userData['originalY'] as number) + float + parallax;
      });

      this.renderer.render(this.scene, this.camera);
    };

    tick();
  }

  ngOnDestroy(): void {
    if (!this.isBrowser) return;

    if (this.animationFrameId !== null) {
      cancelAnimationFrame(this.animationFrameId);
    }
    if (this.resizeHandler) {
      window.removeEventListener('resize', this.resizeHandler);
    }
    if (this.renderer) this.renderer.dispose();

    this.floatingObjects.forEach(obj => {
      obj.traverse(child => {
        if (child instanceof THREE.Mesh) {
          child.geometry?.dispose();
          if (Array.isArray(child.material)) {
            child.material.forEach((m: THREE.Material) => m.dispose());
          } else {
            child.material?.dispose();
          }
        }
        if (child instanceof THREE.LineSegments) {
          child.geometry?.dispose();
          (child.material as THREE.Material)?.dispose();
        }
      });
    });
  }
}
