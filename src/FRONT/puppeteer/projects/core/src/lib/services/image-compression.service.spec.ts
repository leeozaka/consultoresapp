import { ImageCompressionService } from './image-compression.service';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function makeFile(name: string, type: string, sizeBytes = 1024): File {
  const content = new Uint8Array(sizeBytes);
  return new File([content], name, { type });
}

/** Create a fake canvas element whose toBlob immediately resolves with a Blob. */
function mockCanvas(outputBlob: Blob | null): HTMLCanvasElement {
  return {
    width: 0,
    height: 0,
    getContext: vi.fn().mockReturnValue({
      drawImage: vi.fn(),
    }),
    toBlob: vi.fn().mockImplementation((cb: (blob: Blob | null) => void) => {
      // Simulate async microtask
      Promise.resolve().then(() => cb(outputBlob));
    }),
  } as unknown as HTMLCanvasElement;
}

// ---------------------------------------------------------------------------
// Specs
// ---------------------------------------------------------------------------

describe('ImageCompressionService', () => {
  let service: ImageCompressionService;

  beforeEach(() => {
    service = new ImageCompressionService();
  });

  it('should create', () => {
    expect(service).toBeTruthy();
  });

  describe('SSR guard', () => {
    it('should return the original file when document is undefined (SSR)', async () => {
      const originalDocument = globalThis.document;
      // Simulate SSR environment
      (globalThis as Record<string, unknown>)['document'] = undefined;

      try {
        const file = makeFile('photo.jpg', 'image/jpeg', 500 * 1024);
        const result = await service.compress(file);
        expect(result).toBe(file);
      } finally {
        (globalThis as Record<string, unknown>)['document'] = originalDocument;
      }
    });
  });

  describe('skip-encoding guard', () => {
    it('should return the original file when it is already WebP and under 200 KB', async () => {
      const file = makeFile('photo.webp', 'image/webp', 100 * 1024); // 100 KB
      const result = await service.compress(file);
      expect(result).toBe(file);
    });

    it('should NOT skip encoding for a WebP file over 200 KB', async () => {
      const fakeWebpBlob = new Blob([new Uint8Array(10)], { type: 'image/webp' });
      const canvas = mockCanvas(fakeWebpBlob);
      vi.spyOn(document, 'createElement').mockReturnValueOnce(canvas);

      const mockBitmap = { width: 800, height: 600, close: vi.fn() } as unknown as ImageBitmap;
      vi.stubGlobal('createImageBitmap', vi.fn().mockResolvedValue(mockBitmap));

      try {
        const file = makeFile('photo.webp', 'image/webp', 300 * 1024); // 300 KB — should compress
        const result = await service.compress(file);
        expect(result).not.toBe(file);
      } finally {
        vi.unstubAllGlobals();
      }
    });
  });

  describe('compression', () => {
    let mockBitmap: { width: number; height: number; close: ReturnType<typeof vi.fn> };

    beforeEach(() => {
      mockBitmap = { width: 800, height: 600, close: vi.fn() };
      vi.stubGlobal('createImageBitmap', vi.fn().mockResolvedValue(mockBitmap));
    });

    afterEach(() => {
      vi.unstubAllGlobals();
    });

    it('should output a file with type image/webp', async () => {
      const outputBlob = new Blob([new Uint8Array(10)], { type: 'image/webp' });
      const canvas = mockCanvas(outputBlob);
      vi.spyOn(document, 'createElement').mockReturnValueOnce(canvas);

      const file = makeFile('house.jpg', 'image/jpeg', 500 * 1024);
      const result = await service.compress(file);

      expect(result.type).toBe('image/webp');
    });

    it('should output a filename with .webp extension', async () => {
      const outputBlob = new Blob([new Uint8Array(10)], { type: 'image/webp' });
      const canvas = mockCanvas(outputBlob);
      vi.spyOn(document, 'createElement').mockReturnValueOnce(canvas);

      const file = makeFile('house.jpg', 'image/jpeg', 500 * 1024);
      const result = await service.compress(file);

      expect(result.name).toBe('house.webp');
    });

    it('should return original file when canvas context is null', async () => {
      const canvas = {
        width: 0,
        height: 0,
        getContext: vi.fn().mockReturnValue(null),
        toBlob: vi.fn(),
      } as unknown as HTMLCanvasElement;
      vi.spyOn(document, 'createElement').mockReturnValueOnce(canvas);

      const file = makeFile('photo.png', 'image/png', 500 * 1024);
      const result = await service.compress(file);

      expect(result).toBe(file);
    });

    it('should scale down images wider than maxWidthPx', async () => {
      const outputBlob = new Blob([new Uint8Array(10)], { type: 'image/webp' });
      const canvas = mockCanvas(outputBlob);
      const createSpy = vi.spyOn(document, 'createElement').mockReturnValueOnce(canvas);

      // Wide image: 3840 × 2160
      mockBitmap.width = 3840;
      mockBitmap.height = 2160;

      const file = makeFile('wide.jpg', 'image/jpeg', 1024 * 1024);
      await service.compress(file, { maxWidthPx: 1920 });

      // Canvas size should have been set to 1920 × 1080
      expect(canvas.width).toBe(1920);
      expect(canvas.height).toBe(1080);
      createSpy.mockRestore();
    });

    it('should not upscale images narrower than maxWidthPx', async () => {
      const outputBlob = new Blob([new Uint8Array(10)], { type: 'image/webp' });
      const canvas = mockCanvas(outputBlob);
      vi.spyOn(document, 'createElement').mockReturnValueOnce(canvas);

      mockBitmap.width = 400;
      mockBitmap.height = 300;

      const file = makeFile('small.jpg', 'image/jpeg', 500 * 1024);
      await service.compress(file, { maxWidthPx: 1920 });

      expect(canvas.width).toBe(400);
      expect(canvas.height).toBe(300);
    });

    it('should reject when canvas toBlob returns null', async () => {
      const canvas = mockCanvas(null); // toBlob yields null
      vi.spyOn(document, 'createElement').mockReturnValueOnce(canvas);

      const file = makeFile('photo.jpg', 'image/jpeg', 500 * 1024);
      await expect(service.compress(file)).rejects.toThrow('Canvas toBlob returned null');
    });

    it('should close the ImageBitmap after drawing', async () => {
      const outputBlob = new Blob([new Uint8Array(10)], { type: 'image/webp' });
      const canvas = mockCanvas(outputBlob);
      vi.spyOn(document, 'createElement').mockReturnValueOnce(canvas);

      const file = makeFile('house.jpg', 'image/jpeg', 500 * 1024);
      await service.compress(file);

      expect(mockBitmap.close).toHaveBeenCalled();
    });
  });
});
