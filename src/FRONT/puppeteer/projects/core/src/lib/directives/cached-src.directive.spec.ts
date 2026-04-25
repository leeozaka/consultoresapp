import {
  Component,
  DebugElement,
  signal,
} from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { of, throwError } from 'rxjs';
import { CachedSrcDirective } from './cached-src.directive';
import { ImageBlobCacheService } from '../services/image-blob-cache.service';

// ---------------------------------------------------------------------------
// IntersectionObserver mock
// ---------------------------------------------------------------------------

type IoCallback = (entries: IntersectionObserverEntry[]) => void;

let capturedCallback: IoCallback | null = null;
let capturedObserveFn: ReturnType<typeof vi.fn>;
let capturedDisconnectFn: ReturnType<typeof vi.fn>;

function buildIoMock(): typeof IntersectionObserver {
  capturedObserveFn = vi.fn();
  capturedDisconnectFn = vi.fn();

  // Must be a regular function (not an arrow) to be usable as a constructor with `new`
  function MockIntersectionObserver(this: object, callback: IoCallback) {
    capturedCallback = callback;
    Object.assign(this, {
      observe: capturedObserveFn,
      disconnect: capturedDisconnectFn,
      unobserve: vi.fn(),
    });
  }

  return MockIntersectionObserver as unknown as typeof IntersectionObserver;
}

/** Simulate the image entering the viewport. */
function triggerIntersect(): void {
  capturedCallback?.([{ isIntersecting: true } as IntersectionObserverEntry]);
}

// ---------------------------------------------------------------------------
// Host component
// ---------------------------------------------------------------------------

@Component({
  template: `<img appCachedSrc [appCachedSrc]="url()" alt="test" />`,
  imports: [CachedSrcDirective],
  standalone: true,
})
class HostComponent {
  readonly url = signal<string | null | undefined>('https://cdn.example.com/photo.jpg');
}

// ---------------------------------------------------------------------------
// Specs
// ---------------------------------------------------------------------------

const BLOB_URL = 'blob:fake-resolved-url';

describe('CachedSrcDirective', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let imgEl: DebugElement;

  const mockCacheService = {
    resolve: vi.fn().mockReturnValue(of(BLOB_URL)),
  };

  beforeEach(async () => {
    vi.stubGlobal('IntersectionObserver', buildIoMock());

    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [{ provide: ImageBlobCacheService, useValue: mockCacheService }],
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();

    imgEl = fixture.debugElement.query(By.css('img'));
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  // ── Initial state ─────────────────────────────────────────────────────────

  it('should create host component', () => {
    expect(host).toBeTruthy();
  });

  it('should have loading="lazy" on the host img element', () => {
    expect(imgEl.nativeElement.getAttribute('loading')).toBe('lazy');
  });

  it('should NOT set src before the element intersects the viewport', () => {
    // No intersection yet — cache.resolve should not have been called
    expect(mockCacheService.resolve).not.toHaveBeenCalled();
    expect(imgEl.nativeElement.getAttribute('src')).toBeFalsy();
  });

  it('should observe the img element through IntersectionObserver', () => {
    expect(capturedObserveFn).toHaveBeenCalledWith(imgEl.nativeElement);
  });

  // ── After intersection ────────────────────────────────────────────────────

  it('should resolve the blob URL and set src after the element intersects', () => {
    triggerIntersect();
    fixture.detectChanges();

    expect(mockCacheService.resolve).toHaveBeenCalledWith('https://cdn.example.com/photo.jpg');
    expect(imgEl.nativeElement.getAttribute('src')).toBe(BLOB_URL);
  });

  it('should disconnect the observer after the first intersection', () => {
    triggerIntersect();
    expect(capturedDisconnectFn).toHaveBeenCalled();
  });

  // ── URL change after intersection ─────────────────────────────────────────

  it('should re-resolve when the input URL changes after intersection', async () => {
    triggerIntersect();
    fixture.detectChanges();

    mockCacheService.resolve.mockReturnValue(of('blob:new-blob-url'));

    host.url.set('https://cdn.example.com/photo-v2.jpg');
    fixture.detectChanges();

    // give effect() a chance to run
    await fixture.whenStable();
    fixture.detectChanges();

    expect(mockCacheService.resolve).toHaveBeenCalledWith('https://cdn.example.com/photo-v2.jpg');
    expect(imgEl.nativeElement.getAttribute('src')).toBe('blob:new-blob-url');
  });

  it('should set src to empty string when input URL becomes null after intersection', async () => {
    triggerIntersect();
    fixture.detectChanges();

    host.url.set(null);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(imgEl.nativeElement.getAttribute('src')).toBe('');
  });

  // ── Destroy ───────────────────────────────────────────────────────────────

  it('should disconnect the observer on destroy', () => {
    // Ensure observer is still connected (no intersection triggered)
    fixture.destroy();
    expect(capturedDisconnectFn).toHaveBeenCalled();
  });

  // ── Error handling ────────────────────────────────────────────────────────

  it('should gracefully handle errors from cache.resolve without crashing', () => {
    mockCacheService.resolve.mockReturnValue(throwError(() => new Error('Network failure')));

    triggerIntersect();
    fixture.detectChanges();

    // Should not throw, src should remain unset (no blob URL resolved)
    expect(imgEl.nativeElement.getAttribute('src')).toBeFalsy();
  });
});
