import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ComponentRef } from '@angular/core';
import { By } from '@angular/platform-browser';
import { PhotoStagingAreaComponent } from './photo-staging-area';
import { ImageCompressionService } from '../../services/image-compression.service';
import { ImageBlobCacheService } from '../../services/image-blob-cache.service';
import { PropertyImage } from '../../models/property.model';
import { of } from 'rxjs';

// ---------------------------------------------------------------------------
// IntersectionObserver mock (required by CachedSrcDirective)
// Must be a regular function (not an arrow) to be usable as a constructor with `new`
// ---------------------------------------------------------------------------

class MockIntersectionObserver {
  observe = vi.fn();
  disconnect = vi.fn();
  unobserve = vi.fn();
}

vi.stubGlobal('IntersectionObserver', MockIntersectionObserver);

// ---------------------------------------------------------------------------
// Factory helpers
// ---------------------------------------------------------------------------

function makeImage(overrides: Partial<PropertyImage> = {}): PropertyImage {
  return {
    key: 'img-1',
    url: 'https://cdn.example.com/photo.jpg',
    thumbnailUrl: 'https://cdn.example.com/thumb.jpg',
    mediumUrl: 'https://cdn.example.com/medium.jpg',
    order: 0,
    isProcessed: true,
    originalFileName: 'photo.jpg',
    ...overrides,
  };
}

function makeFile(name = 'photo.jpg', type = 'image/jpeg', size = 50 * 1024): File {
  return new File([new Uint8Array(size)], name, { type });
}

// ---------------------------------------------------------------------------
// Specs
// ---------------------------------------------------------------------------

describe('PhotoStagingAreaComponent', () => {
  let fixture: ComponentFixture<PhotoStagingAreaComponent>;
  let component: PhotoStagingAreaComponent;
  let componentRef: ComponentRef<PhotoStagingAreaComponent>;

  const fakeCompressedFile = makeFile('photo.webp', 'image/webp', 30 * 1024);

  const mockCompressionService = {
    compress: vi.fn().mockResolvedValue(fakeCompressedFile),
  };

  const mockCacheService = {
    resolve: vi.fn().mockReturnValue(of('blob:cached-url')),
  };

  beforeEach(async () => {
    // Stub blob URL helpers since jsdom does not implement them
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:preview-url');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);

    await TestBed.configureTestingModule({
      imports: [PhotoStagingAreaComponent],
      providers: [
        { provide: ImageCompressionService, useValue: mockCompressionService },
        { provide: ImageBlobCacheService, useValue: mockCacheService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PhotoStagingAreaComponent);
    component = fixture.componentInstance;
    componentRef = fixture.componentRef;
  });

  afterEach(() => {
    vi.restoreAllMocks();
    mockCompressionService.compress.mockResolvedValue(fakeCompressedFile);
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  // ── Uploaded images section ───────────────────────────────────────────────

  it('should NOT render the uploaded section when uploadedImages is empty', () => {
    componentRef.setInput('uploadedImages', []);
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).not.toContain('Fotos enviadas');
  });

  it('should render the "Fotos enviadas" heading when uploadedImages has items', () => {
    componentRef.setInput('uploadedImages', [makeImage()]);
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Fotos enviadas');
  });

  it('should show "Processando" overlay for unprocessed images', () => {
    componentRef.setInput('uploadedImages', [
      makeImage({ isProcessed: false, url: 'https://cdn.example.com/raw.jpg' }),
    ]);
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Processando');
  });

  it('should NOT show "Processando" overlay for processed images', () => {
    componentRef.setInput('uploadedImages', [makeImage({ isProcessed: true })]);
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).not.toContain('Processando');
  });

  it('should emit deleteImage when a delete button is clicked on an uploaded image', () => {
    const image = makeImage();
    componentRef.setInput('uploadedImages', [image]);
    fixture.detectChanges();

    const deleteEmitSpy = vi.fn();
    component.deleteImage.subscribe(deleteEmitSpy);

    const btn = fixture.debugElement.query(
      By.css('[aria-label="Remover foto"]'),
    );
    btn.nativeElement.click();

    expect(deleteEmitSpy).toHaveBeenCalledWith(image);
  });

  // ── Drop zone ─────────────────────────────────────────────────────────────

  it('should render the drop zone headline', () => {
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Arraste e solte suas fotos aqui');
  });

  it('should render "Selecionar fotos" button', () => {
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Selecionar fotos');
  });

  // ── Staging files ─────────────────────────────────────────────────────────

  it('should show "Comprimindo" badge while compression is pending', async () => {
    fixture.detectChanges();

    let resolveCompress!: (f: File) => void;
    mockCompressionService.compress.mockReturnValueOnce(
      new Promise<File>((r) => (resolveCompress = r)),
    );

    const file = makeFile();
    // Do NOT await — stageFiles updates the signal synchronously before the first
    // internal `await`, so we can observe the 'compressing' state immediately.
    const stagingPromise = (component as any).stageFiles([file]) as Promise<void>;
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Comprimindo');

    // Resolve and drain to avoid dangling async operations
    resolveCompress(fakeCompressedFile);
    await stagingPromise;
  });

  it('should show "Pendente" badge after compression completes', async () => {
    fixture.detectChanges();

    const file = makeFile();
    await (component as any).stageFiles([file]);

    fixture.detectChanges();

    await fixture.whenStable();
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Pendente');
  });

  it('should NOT show filenames in the staged files section', async () => {
    fixture.detectChanges();

    const file = makeFile('my-house-photo.jpg');
    await (component as any).stageFiles([file]);

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).not.toContain('my-house-photo');
  });

  it('should emit filesStaged after compression', async () => {
    fixture.detectChanges();

    const stagedSpy = vi.fn();
    component.filesStaged.subscribe(stagedSpy);

    const file = makeFile();
    await (component as any).stageFiles([file]);
    await fixture.whenStable();

    expect(stagedSpy).toHaveBeenCalledWith(
      expect.arrayContaining([fakeCompressedFile]),
    );
  });

  it('should remove staged file and emit updated list when the remove button is clicked', async () => {
    fixture.detectChanges();

    const stagedSpy = vi.fn();
    component.filesStaged.subscribe(stagedSpy);

    await (component as any).stageFiles([makeFile()]);
    await fixture.whenStable();
    fixture.detectChanges();

    const removeBtn = fixture.debugElement.query(
      By.css('[aria-label="Remover foto pendente"]'),
    );
    removeBtn.nativeElement.click();

    fixture.detectChanges();

    // Staged section should disappear
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).not.toContain('Fotos pendentes');

    // Last emission should be an empty array
    const lastEmit: File[] = stagedSpy.mock.calls.at(-1)?.[0];
    expect(lastEmit).toHaveLength(0);
  });

  it('should deduplicate files with the same name, size and lastModified', async () => {
    fixture.detectChanges();

    const file = makeFile();
    await (component as any).stageFiles([file]);
    // Stage the same file again — should be a no-op
    await (component as any).stageFiles([file]);
    await fixture.whenStable();
    fixture.detectChanges();

    expect((component as any).stagedFiles().length).toBe(1);
  });

  // ── Destroy cleanup ───────────────────────────────────────────────────────

  it('should revoke all blob preview URLs on destroy', async () => {
    fixture.detectChanges();

    await (component as any).stageFiles([makeFile()]);
    await fixture.whenStable();

    fixture.destroy();

    expect(URL.revokeObjectURL).toHaveBeenCalled();
  });
});
