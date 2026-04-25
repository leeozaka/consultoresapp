import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, NEVER, Subject } from 'rxjs';
import { ComponentRef } from '@angular/core';
import { PropertyFormComponent } from './property-form';
import { PropertyService, ImageBlobCacheService, ImageCompressionService, ImageProcessedSseService, createMockProperty, ListingType, PropertyType } from '@consultores/core';

// IntersectionObserver is used by CachedSrcDirective (embedded in PhotoStagingAreaComponent)
// Must be a regular class/function (not an arrow) to be usable as a constructor with `new`
class MockIntersectionObserver {
  observe = vi.fn();
  disconnect = vi.fn();
  unobserve = vi.fn();
}

vi.stubGlobal('IntersectionObserver', MockIntersectionObserver);

describe('PropertyFormComponent', () => {
  let fixture: ComponentFixture<PropertyFormComponent>;
  let component: PropertyFormComponent;
  let componentRef: ComponentRef<PropertyFormComponent>;
  let router: Router;

  const fakeCompressedFile = new File([new Uint8Array(10)], 'photo.webp', { type: 'image/webp' });

  const mockPropertyService = {
    getById: vi.fn().mockReturnValue(of(createMockProperty({
      images: [
        {
          key: 'img-1',
          url: 'https://example.com/img-1.jpg',
          thumbnailUrl: 'https://example.com/thumb-1.jpg',
          mediumUrl: 'https://example.com/medium-1.jpg',
          order: 0,
          isProcessed: true,
          originalFileName: 'img-1.jpg',
        },
      ],
    }))),
    create: vi.fn().mockReturnValue(of(createMockProperty({ id: 'new-prop-id', images: [] }))),
    update: vi.fn().mockReturnValue(of(createMockProperty())),
    uploadImage: vi.fn().mockReturnValue(of(createMockProperty())),
    publish: vi.fn().mockReturnValue(of(createMockProperty({ status: 'Active' as any }))),
  };

  const mockCacheService = {
    resolve: vi.fn().mockReturnValue(of('blob:cached-url')),
  };

  const mockCompressionService = {
    compress: vi.fn().mockResolvedValue(fakeCompressedFile),
  };

  const mockSseService = {
    connect: vi.fn().mockReturnValue(NEVER),
  };

  beforeEach(async () => {
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:preview-url');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);

    await TestBed.configureTestingModule({
      imports: [PropertyFormComponent],
      providers: [
        provideRouter([]),
        { provide: PropertyService, useValue: mockPropertyService },
        { provide: ImageBlobCacheService, useValue: mockCacheService },
        { provide: ImageCompressionService, useValue: mockCompressionService },
        { provide: ImageProcessedSseService, useValue: mockSseService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PropertyFormComponent);
    component = fixture.componentInstance;
    componentRef = fixture.componentRef;
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('create mode', () => {
    beforeEach(() => {
      fixture.detectChanges();
    });

    it('should create', () => {
      expect(component).toBeTruthy();
    });

    it('should show "Novo Imóvel" heading', () => {
      const el: HTMLElement = fixture.nativeElement;
      expect(el.textContent).toContain('Novo Imóvel');
    });

    it('should display form fields', () => {
      const el: HTMLElement = fixture.nativeElement;
      expect(el.querySelector('#title')).toBeTruthy();
      expect(el.querySelector('#price')).toBeTruthy();
      expect(el.querySelector('#city')).toBeTruthy();
    });

    it('should render translated type and finality labels', () => {
      const el: HTMLElement = fixture.nativeElement;
      expect(el.textContent).toContain('Apartamento');
      expect(el.textContent).toContain('Venda');
    });

    it('should render drag and drop area in create mode', () => {
      const el: HTMLElement = fixture.nativeElement;
      expect(el.querySelector('app-photo-staging-area')).toBeTruthy();
    });

    it('should navigate to edit route after first create', () => {
      const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

      (component as any).save();

      expect(mockPropertyService.create).toHaveBeenCalled();
      expect(navigateSpy).toHaveBeenCalledWith(['/dashboard/imoveis', 'new-prop-id']);
    });

    it('should upload staged photos sequentially after first create', async () => {
      const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
      const file = new File(['image'], 'house.jpg', { type: 'image/jpeg' });

      (component as any).onFilesStaged([file]);
      (component as any).save();

      await fixture.whenStable();

      expect(mockPropertyService.create).toHaveBeenCalled();
      expect(mockPropertyService.uploadImage).toHaveBeenCalledWith('new-prop-id', file);
      expect(navigateSpy).toHaveBeenCalledWith(['/dashboard/imoveis', 'new-prop-id']);
    });

    it('should stage files received from PhotoStagingAreaComponent output', () => {
      const file = new File(['image'], 'photo.webp', { type: 'image/webp' });

      (component as any).onFilesStaged([file]);

      expect((component as any).stagedFiles()).toEqual([file]);
    });
  });

  describe('edit mode', () => {
    beforeEach(() => {
      componentRef.setInput('id', 'prop-1');
      fixture.detectChanges();
    });

    it('should show "Editar Imóvel" heading', () => {
      const el: HTMLElement = fixture.nativeElement;
      expect(el.textContent).toContain('Editar Imóvel');
    });

    it('should load property data', () => {
      expect(mockPropertyService.getById).toHaveBeenCalledWith('prop-1');
    });

    it('should render photo staging area component in edit mode', () => {
      const el: HTMLElement = fixture.nativeElement;
      expect(el.querySelector('app-photo-staging-area')).toBeTruthy();
    });

    it('should send propertyType and listingType when saving edit mode', () => {
      (component as any).propertyType.set(PropertyType.House);
      (component as any).listingType.set(ListingType.Rent);

      (component as any).save();

      expect(mockPropertyService.update).toHaveBeenCalledWith(
        'prop-1',
        expect.objectContaining({
          propertyType: PropertyType.House,
          listingType: ListingType.Rent,
        }),
      );
    });

    it('should connect to SSE when property has unprocessed images', () => {
      // Reset mock to return a property with unprocessed images
      mockPropertyService.getById.mockReturnValue(
        of(
          createMockProperty({
            images: [
              {
                key: 'raw/pending.jpg',
                url: 'https://example.com/raw.jpg',
                order: 0,
                isProcessed: false,
                originalFileName: 'pending.jpg',
              },
            ],
          }),
        ),
      );

      // Re-create the component in edit mode
      const fix2 = TestBed.createComponent(PropertyFormComponent);
      const ref2 = fix2.componentRef;
      ref2.setInput('id', 'prop-2');
      fix2.detectChanges();

      expect(mockSseService.connect).toHaveBeenCalledWith('prop-2');
    });

    it('should NOT connect to SSE when all images are already processed', () => {
      mockSseService.connect.mockClear();

      // The default mock returns all-processed images — no SSE needed
      expect(mockSseService.connect).not.toHaveBeenCalled();
    });

    it('should update images signal when SSE delivers an image-processed event', () => {
      const sseSubject = new Subject();
      mockSseService.connect.mockReturnValue(sseSubject.asObservable());

      mockPropertyService.getById.mockReturnValue(
        of(
          createMockProperty({
            images: [
              {
                key: 'raw/photo.jpg',
                url: 'https://example.com/raw.jpg',
                order: 0,
                isProcessed: false,
                originalFileName: 'photo.jpg',
              },
            ],
          }),
        ),
      );

      const fix2 = TestBed.createComponent(PropertyFormComponent);
      fix2.componentRef.setInput('id', 'prop-sse');
      fix2.detectChanges();

      // Emit SSE event
      sseSubject.next({
        propertyId: 'prop-sse',
        imageKey: 'raw/photo.jpg',
        url: 'https://cdn/full.webp',
        thumbnailUrl: 'https://cdn/thumb.webp',
        mediumUrl: 'https://cdn/medium.webp',
        isProcessed: true,
      });

      const images = (fix2.componentInstance as any).images();
      expect(images[0].isProcessed).toBe(true);
      expect(images[0].url).toBe('https://cdn/full.webp');
      expect(images[0].thumbnailUrl).toBe('https://cdn/thumb.webp');
      expect(images[0].mediumUrl).toBe('https://cdn/medium.webp');
    });
  });
});
