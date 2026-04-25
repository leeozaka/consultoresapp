import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { NgZone } from '@angular/core';
import { ImageProcessedSseService, ImageProcessedEvent } from './image-processed-sse.service';

/**
 * Lightweight mock for the browser EventSource API.
 * Captures the URL, options, registered listeners, and allows manual event dispatch.
 */
class MockEventSource {
  static instances: MockEventSource[] = [];

  readonly url: string;
  readonly withCredentials: boolean;
  readyState: number = EventSource.OPEN;
  onerror: ((ev: Event) => void) | null = null;

  private listeners = new Map<string, EventListenerOrEventListenerObject>();

  constructor(url: string, options?: EventSourceInit) {
    this.url = url;
    this.withCredentials = options?.withCredentials ?? false;
    MockEventSource.instances.push(this);
  }

  addEventListener(type: string, listener: EventListenerOrEventListenerObject): void {
    this.listeners.set(type, listener);
  }

  removeEventListener(type: string, _listener: EventListenerOrEventListenerObject): void {
    this.listeners.delete(type);
  }

  close(): void {
    this.readyState = EventSource.CLOSED;
  }

  /** Test helper: dispatch a named event with the given data string. */
  dispatch(type: string, data: string): void {
    const handler = this.listeners.get(type);
    if (typeof handler === 'function') {
      handler(new MessageEvent(type, { data }));
    }
  }

  static readonly CONNECTING = 0;
  static readonly OPEN = 1;
  static readonly CLOSED = 2;
}

describe('ImageProcessedSseService', () => {
  let service: ImageProcessedSseService;

  beforeEach(() => {
    MockEventSource.instances = [];
    vi.stubGlobal('EventSource', MockEventSource);

    TestBed.configureTestingModule({ providers: [ImageProcessedSseService] });
    service = TestBed.inject(ImageProcessedSseService);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should create an EventSource pointing to the correct URL', () => {
    service.connect('prop-123').subscribe();

    expect(MockEventSource.instances).toHaveLength(1);
    expect(MockEventSource.instances[0].url).toBe('/api/properties/prop-123/images/events');
    expect(MockEventSource.instances[0].withCredentials).toBe(true);
  });

  it('should parse and emit ImageProcessedEvent from SSE data', () => {
    const received: ImageProcessedEvent[] = [];
    const zone = TestBed.inject(NgZone);

    zone.run(() => {
      service.connect('prop-123').subscribe((event) => received.push(event));
    });

    const es = MockEventSource.instances[0];
    es.dispatch(
      'image-processed',
      JSON.stringify({
        property_id: 'prop-123',
        image_key: 'raw/photo.jpg',
        url: 'https://cdn/full.webp',
        thumbnail_url: 'https://cdn/thumb.webp',
        medium_url: 'https://cdn/medium.webp',
        is_processed: true,
      }),
    );

    expect(received).toHaveLength(1);
    expect(received[0]).toEqual({
      propertyId: 'prop-123',
      imageKey: 'raw/photo.jpg',
      url: 'https://cdn/full.webp',
      thumbnailUrl: 'https://cdn/thumb.webp',
      mediumUrl: 'https://cdn/medium.webp',
      isProcessed: true,
    });
  });

  it('should close EventSource when unsubscribed', () => {
    const sub = service.connect('prop-123').subscribe();
    const es = MockEventSource.instances[0];

    expect(es.readyState).toBe(EventSource.OPEN);

    sub.unsubscribe();

    expect(es.readyState).toBe(EventSource.CLOSED);
  });

  it('should silently ignore malformed event data', () => {
    const received: ImageProcessedEvent[] = [];
    const zone = TestBed.inject(NgZone);

    zone.run(() => {
      service.connect('prop-123').subscribe((event) => received.push(event));
    });

    const es = MockEventSource.instances[0];
    es.dispatch('image-processed', 'not valid json');

    expect(received).toHaveLength(0);
  });
});
