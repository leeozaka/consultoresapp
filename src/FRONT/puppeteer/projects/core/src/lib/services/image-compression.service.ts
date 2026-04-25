import { Injectable } from '@angular/core';

export interface CompressionOptions {
  /** Maximum output width in pixels. Taller images are scaled proportionally. */
  maxWidthPx?: number;
  /** WebP quality, 0–1. */
  quality?: number;
}

const DEFAULT_MAX_WIDTH = 1920;
const DEFAULT_QUALITY = 0.85;

/**
 * Client-side image compression using the native Canvas API.
 *
 * Converts any browser-supported image format to WebP, scales it down to at
 * most `maxWidthPx` wide (preserving aspect ratio), and returns a new `File`.
 * Server-side (no `document`) returns the original file unchanged.
 */
@Injectable({ providedIn: 'root' })
export class ImageCompressionService {
  async compress(
    file: File,
    { maxWidthPx = DEFAULT_MAX_WIDTH, quality = DEFAULT_QUALITY }: CompressionOptions = {},
  ): Promise<File> {
    // Guard: SSR / non-browser environment
    if (typeof document === 'undefined') return file;
    // Guard: Already webp and small enough — skip re-encoding
    if (file.type === 'image/webp' && file.size < 200 * 1024) return file;

    const bitmap = await createImageBitmap(file);
    const { width, height } = this.scaleDimensions(bitmap.width, bitmap.height, maxWidthPx);

    const canvas = document.createElement('canvas');
    canvas.width = width;
    canvas.height = height;

    const ctx = canvas.getContext('2d');
    if (!ctx) {
      bitmap.close();
      return file;
    }

    ctx.drawImage(bitmap, 0, 0, width, height);
    bitmap.close();

    return new Promise<File>((resolve, reject) => {
      canvas.toBlob(
        (blob) => {
          if (!blob) {
            reject(new Error('Canvas toBlob returned null'));
            return;
          }
          const webpName = file.name.replace(/\.[^.]+$/, '.webp');
          resolve(new File([blob], webpName, { type: 'image/webp', lastModified: Date.now() }));
        },
        'image/webp',
        quality,
      );
    });
  }

  private scaleDimensions(
    srcWidth: number,
    srcHeight: number,
    maxWidth: number,
  ): { width: number; height: number } {
    if (srcWidth <= maxWidth) return { width: srcWidth, height: srcHeight };
    const ratio = maxWidth / srcWidth;
    return { width: maxWidth, height: Math.round(srcHeight * ratio) };
  }
}
