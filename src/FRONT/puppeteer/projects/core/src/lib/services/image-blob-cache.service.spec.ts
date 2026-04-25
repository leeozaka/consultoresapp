import { TestBed } from '@angular/core/testing';
import { provideHttpClient, HttpErrorResponse } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';
import { ImageBlobCacheService } from './image-blob-cache.service';
import { environment } from '../environments/environment';

const IMAGE_URL = 'https://cdn.example.com/photo.jpg';
const BLOB_URL = 'blob:fake-blob-url';
const FAKE_BLOB = new Blob(['img'], { type: 'image/jpeg' });

describe('ImageBlobCacheService', () => {
  let service: ImageBlobCacheService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    vi.spyOn(URL, 'createObjectURL').mockReturnValue(BLOB_URL);
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);

    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(ImageBlobCacheService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    vi.restoreAllMocks();
    vi.useRealTimers();
  });

  // ── Falsy URL guards ──────────────────────────────────────────────────────

  it('should emit empty string for null', async () => {
    const result = await firstValueFrom(service.resolve(null));
    expect(result).toBe('');
    httpMock.expectNone(IMAGE_URL);
  });

  it('should emit empty string for undefined', async () => {
    const result = await firstValueFrom(service.resolve(undefined));
    expect(result).toBe('');
    httpMock.expectNone(IMAGE_URL);
  });

  it('should emit empty string for empty string', async () => {
    const result = await firstValueFrom(service.resolve(''));
    expect(result).toBe('');
    httpMock.expectNone(IMAGE_URL);
  });

  // ── Network fetch ─────────────────────────────────────────────────────────

  it('should fetch the image and return a blob: URL', async () => {
    const promise = firstValueFrom(service.resolve(IMAGE_URL));

    const req = httpMock.expectOne(IMAGE_URL);
    expect(req.request.responseType).toBe('blob');
    req.flush(FAKE_BLOB);

    const result = await promise;
    expect(result).toBe(BLOB_URL);
    expect(URL.createObjectURL).toHaveBeenCalledWith(FAKE_BLOB);
  });

  // ── Cache hit ─────────────────────────────────────────────────────────────

  it('should return the cached blob URL on the second call without making another HTTP request', async () => {
    // First call — fills the cache
    const first = firstValueFrom(service.resolve(IMAGE_URL));
    httpMock.expectOne(IMAGE_URL).flush(FAKE_BLOB);
    await first;

    // Second call — should be a cache hit; no new request
    const second = await firstValueFrom(service.resolve(IMAGE_URL));
    httpMock.expectNone(IMAGE_URL);
    expect(second).toBe(BLOB_URL);
  });

  // ── In-flight deduplication ───────────────────────────────────────────────

  it('should share a single HTTP request for concurrent calls to the same URL', async () => {
    const p1 = firstValueFrom(service.resolve(IMAGE_URL));
    const p2 = firstValueFrom(service.resolve(IMAGE_URL));

    // Only one outstanding request despite two concurrent subscribe calls
    const req = httpMock.expectOne(IMAGE_URL);
    req.flush(FAKE_BLOB);

    const [r1, r2] = await Promise.all([p1, p2]);
    expect(r1).toBe(BLOB_URL);
    expect(r2).toBe(BLOB_URL);
  });

  // ── TTL expiry ────────────────────────────────────────────────────────────

  it('should revoke the stale blob URL and re-fetch after TTL expires', async () => {
    vi.useFakeTimers();

    // Initial fetch
    const first = firstValueFrom(service.resolve(IMAGE_URL));
    httpMock.expectOne(IMAGE_URL).flush(FAKE_BLOB);
    await first;

    // Advance time past TTL
    vi.advanceTimersByTime(environment.imageCacheTtlMs + 1);

    // Second call after expiry
    const second = firstValueFrom(service.resolve(IMAGE_URL));
    httpMock.expectOne(IMAGE_URL).flush(FAKE_BLOB);
    await second;

    expect(URL.revokeObjectURL).toHaveBeenCalledWith(BLOB_URL);
  });

  // ── HTTP error ────────────────────────────────────────────────────────────

  it('should propagate HTTP errors and remove the entry from the cache', async () => {
    const promise = firstValueFrom(service.resolve(IMAGE_URL)).catch((e) => e);

    httpMock.expectOne(IMAGE_URL).error(new ProgressEvent('network error'));

    const error = await promise;
    expect(error).toBeInstanceOf(HttpErrorResponse);

    // After error the cache entry is cleared, so a new HTTP request is made
    const retry = firstValueFrom(service.resolve(IMAGE_URL));
    httpMock.expectOne(IMAGE_URL).flush(FAKE_BLOB);
    const retryUrl = await retry;
    expect(retryUrl).toBe(BLOB_URL);
  });

  // ── Destroy cleanup ───────────────────────────────────────────────────────

  it('should revoke all blob URLs on ngOnDestroy', async () => {
    const first = firstValueFrom(service.resolve(IMAGE_URL));
    httpMock.expectOne(IMAGE_URL).flush(FAKE_BLOB);
    await first;

    service.ngOnDestroy();

    expect(URL.revokeObjectURL).toHaveBeenCalledWith(BLOB_URL);
  });
});
