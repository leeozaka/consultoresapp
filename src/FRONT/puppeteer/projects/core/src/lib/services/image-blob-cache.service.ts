import { Injectable, OnDestroy, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, shareReplay, tap } from 'rxjs';
import { environment } from '../environments/environment';

interface CachedBlob {
  blobUrl: string;
  expiresAt: number;
}

interface PendingFetch {
  pending$: Observable<string>;
}

type CacheEntry = CachedBlob | PendingFetch;

function isCached(entry: CacheEntry): entry is CachedBlob {
  return 'blobUrl' in entry;
}

/**
 * In-memory image blob cache.
 *
 * Resolves an HTTP image URL to a local `blob:` URL, caching the result for
 * `environment.imageCacheTtlMs` milliseconds.  Concurrent resolution requests
 * for the same URL share a single in-flight HTTP request.
 */
@Injectable({ providedIn: 'root' })
export class ImageBlobCacheService implements OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly cache = new Map<string, CacheEntry>();

  /**
   * Returns an Observable that emits a `blob:` URL for the given image URL.
   * Emits an empty string when `url` is falsy.
   */
  resolve(url: string | null | undefined): Observable<string> {
    if (!url) return of('');

    const entry = this.cache.get(url);

    if (entry) {
      if (isCached(entry)) {
        if (Date.now() < entry.expiresAt) {
          return of(entry.blobUrl);
        }
        // Expired — revoke the stale blob URL and evict
        URL.revokeObjectURL(entry.blobUrl);
        this.cache.delete(url);
      } else {
        // In-flight: return the shared observable
        return entry.pending$;
      }
    }

    // Cache miss or expiry: start a new fetch, deduplicated via shareReplay(1)
    const pending$: Observable<string> = this.http
      .get(url, { responseType: 'blob' })
      .pipe(
        tap((blob) => {
          const blobUrl = URL.createObjectURL(blob);
          this.cache.set(url, {
            blobUrl,
            expiresAt: Date.now() + environment.imageCacheTtlMs,
          });
        }),
        // Convert tap-side-effect value (Blob) to the thing callers need (blobUrl)
        // We re-read from the cache because tap ran before this map
        (source) =>
          new Observable<string>((subscriber) => {
            source.subscribe({
              next: () => {
                const stored = this.cache.get(url);
                if (stored && isCached(stored)) {
                  subscriber.next(stored.blobUrl);
                }
              },
              error: (err) => {
                this.cache.delete(url);
                subscriber.error(err);
              },
              complete: () => subscriber.complete(),
            });
          }),
        shareReplay(1),
      );

    // Store as pending before subscribing so concurrent callers hit the shared $
    this.cache.set(url, { pending$: pending$ });

    return pending$;
  }

  ngOnDestroy(): void {
    for (const entry of this.cache.values()) {
      if (isCached(entry)) {
        URL.revokeObjectURL(entry.blobUrl);
      }
    }
    this.cache.clear();
  }
}
