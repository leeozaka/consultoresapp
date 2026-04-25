import { Injectable, inject, NgZone } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

/** Mirrors the backend ImageProcessedEvent DTO (snake_case → camelCase via manual parse). */
export interface ImageProcessedEvent {
  propertyId: string;
  imageKey: string;
  url: string;
  thumbnailUrl: string;
  mediumUrl: string;
  isProcessed: boolean;
}

/**
 * Connects to the property-images SSE endpoint and emits events
 * each time a property image finishes server-side WebP processing.
 *
 * Usage:
 * ```ts
 * sseService.connect(propertyId).subscribe(event => { … });
 * ```
 *
 * The EventSource is closed when the subscriber unsubscribes.
 */
@Injectable({ providedIn: 'root' })
export class ImageProcessedSseService {
  private readonly zone = inject(NgZone);

  /** Opens an SSE connection for the given property and returns an Observable of processed-image events. */
  connect(propertyId: string): Observable<ImageProcessedEvent> {
    return new Observable<ImageProcessedEvent>((subscriber) => {
      const base = environment.apiBaseUrl || '';
      const url = `${base}/api/properties/${propertyId}/images/events`;

      // Run EventSource outside Angular to avoid unnecessary change-detection cycles
      const eventSource = this.zone.runOutsideAngular(
        () => new EventSource(url, { withCredentials: true }),
      );

      const handler = (event: MessageEvent) => {
        try {
          const raw = JSON.parse(event.data);
          const mapped: ImageProcessedEvent = {
            propertyId: raw.property_id,
            imageKey: raw.image_key,
            url: raw.url,
            thumbnailUrl: raw.thumbnail_url,
            mediumUrl: raw.medium_url,
            isProcessed: raw.is_processed,
          };

          // Re-enter Angular zone so signal updates trigger change detection
          this.zone.run(() => subscriber.next(mapped));
        } catch {
          // Ignore malformed events
        }
      };

      eventSource.addEventListener('image-processed', handler);

      eventSource.onerror = () => {
        // EventSource auto-reconnects on transient failures.
        // Only complete if the connection was explicitly closed (readyState === CLOSED).
        if (eventSource.readyState === EventSource.CLOSED) {
          subscriber.complete();
        }
      };

      // Cleanup: close EventSource when the subscriber unsubscribes
      return () => {
        eventSource.removeEventListener('image-processed', handler);
        eventSource.close();
      };
    });
  }
}
