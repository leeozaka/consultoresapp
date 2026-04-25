import { ErrorHandler, Injectable, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

const RELOAD_KEY = '__chunk_reload';

function isChunkLoadError(error: unknown): boolean {
  const msg = error instanceof Error ? error.message : String(error);
  return (
    msg.includes('dynamically imported module') ||
    msg.includes('Loading chunk') ||
    msg.includes('ChunkLoadError')
  );
}

@Injectable()
export class ChunkErrorHandler implements ErrorHandler {
  private readonly platformId = inject(PLATFORM_ID);

  handleError(error: unknown): void {
    if (isPlatformBrowser(this.platformId) && isChunkLoadError(error)) {
      const alreadyReloaded = sessionStorage.getItem(RELOAD_KEY);
      if (!alreadyReloaded) {
        sessionStorage.setItem(RELOAD_KEY, '1');
        window.location.reload();
        return;
      }
      // Key stays set for the rest of the session so concurrent or subsequent
      // chunk errors don't re-arm the reload and cause an infinite refresh loop.
    }

    console.error(error);
  }
}
