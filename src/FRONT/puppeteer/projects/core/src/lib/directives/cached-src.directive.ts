import {
  Directive,
  ElementRef,
  PLATFORM_ID,
  Renderer2,
  effect,
  inject,
  input,
  OnDestroy,
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Subscription } from 'rxjs';
import { ImageBlobCacheService } from '../services/image-blob-cache.service';

/**
 * Replaces `[src]` on `<img>` elements with a viewport-gated, in-memory-cached
 * blob URL fetch.
 *
 * - Images below-fold are **not fetched** until they come within 300 px of the
 *   viewport (IntersectionObserver with rootMargin: '300px').
 * - Resolved blob URLs are cached for `environment.imageCacheTtlMs` ms so SPA
 *   navigation never re-downloads the same image.
 * - On SSR the observer is skipped; the native `loading="lazy"` attribute acts
 *   as a progressive-enhancement fallback.
 *
 * Usage:
 * ```html
 * <img appCachedSrc [appCachedSrc]="image.thumbnailUrl" alt="..." />
 * ```
 */
@Directive({
  selector: 'img[appCachedSrc]',
  host: { loading: 'lazy' },
})
export class CachedSrcDirective implements OnDestroy {
  readonly appCachedSrc = input<string | null | undefined>();

  private readonly cache = inject(ImageBlobCacheService);
  private readonly el = inject<ElementRef<HTMLImageElement>>(ElementRef);
  private readonly renderer = inject(Renderer2);
  private readonly platformId = inject(PLATFORM_ID);

  private observer: IntersectionObserver | null = null;
  private subscription: Subscription | null = null;
  /** True after the element has intersected at least once */
  private intersected = false;

  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      this.observer = new IntersectionObserver(
        (entries) => {
          if (entries.some((e) => e.isIntersecting)) {
            this.intersected = true;
            this.observer?.disconnect();
            this.observer = null;
            this.load(this.appCachedSrc());
          }
        },
        { rootMargin: '300px' },
      );
      this.observer.observe(this.el.nativeElement);
    }

    // Re-load if the URL input changes after the element is already on screen
    effect(() => {
      const url = this.appCachedSrc();
      if (this.intersected) {
        this.load(url);
      }
    });
  }

  private load(url: string | null | undefined): void {
    this.subscription?.unsubscribe();
    if (!url) {
      this.renderer.setAttribute(this.el.nativeElement, 'src', '');
      return;
    }
    this.subscription = this.cache.resolve(url).subscribe({
      next: (blobUrl) => {
        this.renderer.setAttribute(this.el.nativeElement, 'src', blobUrl);
      },
      error: () => {
        // Gracefully swallow — the <img> stays with no src (broken-image icon).
        // A future retry will occur if the input signal changes.
      },
    });
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    this.subscription?.unsubscribe();
  }
}
