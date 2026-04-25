import { ChangeDetectionStrategy, Component, input, signal, computed } from '@angular/core';
import { PropertyImage } from '../../models/property.model';
import { CachedSrcDirective } from '../../directives/cached-src.directive';

@Component({
  selector: 'app-image-gallery',
  imports: [CachedSrcDirective],
  templateUrl: './image-gallery.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImageGalleryComponent {
  readonly images = input.required<PropertyImage[]>();
  readonly altPrefix = input<string>('Imagem do imóvel');

  protected readonly currentIndex = signal(0);

  protected readonly currentImage = computed(() => {
    const list = this.images();
    return list[this.currentIndex()] ?? null;
  });

  protected readonly hasMultiple = computed(() => this.images().length > 1);

  /**
   * True when the active image has not been processed by the server yet
   * and no URL is available at all (prevents blank <img> rendering).
   */
  protected readonly currentImagePending = computed(() => {
    const img = this.currentImage();
    return img !== null && !img.isProcessed && !img.url && !img.mediumUrl;
  });

  /** Best available URL for the main (full-size) view */
  protected mainUrl(img: PropertyImage): string {
    return img.url ?? img.mediumUrl ?? img.thumbnailUrl ?? '';
  }

  /** Best available URL for the thumbnail strip */
  protected thumbUrl(img: PropertyImage): string {
    return img.thumbnailUrl ?? img.mediumUrl ?? img.url ?? '';
  }

  protected goTo(index: number): void {
    this.currentIndex.set(index);
  }

  protected prev(): void {
    const len = this.images().length;
    this.currentIndex.update((i) => (i - 1 + len) % len);
  }

  protected next(): void {
    const len = this.images().length;
    this.currentIndex.update((i) => (i + 1) % len);
  }

  protected thumbBorderColor(i: number): string {
    return i === this.currentIndex() ? 'var(--brand-primary)' : 'transparent';
  }
}
