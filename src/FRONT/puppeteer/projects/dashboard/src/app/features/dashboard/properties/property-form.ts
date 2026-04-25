import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal, OnInit, input, computed } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Subscription, concatMap, from, toArray } from 'rxjs';
import { ImageProcessedSseService, PropertyService, LoadingSpinnerComponent, PhotoStagingAreaComponent } from '@consultores/core';
import {
  Property,
  PropertyImage,
  PropertyType,
  ListingType,
  PropertyStatus,
  CreatePropertyRequest,
  UpdatePropertyRequest,
} from '@consultores/core';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputMaskModule } from 'primeng/inputmask';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { FluidModule } from 'primeng/fluid';

@Component({
  selector: 'app-property-form',
  imports: [
    FormsModule,
    RouterLink,
    LoadingSpinnerComponent,
    PhotoStagingAreaComponent,
    InputTextModule,
    InputNumberModule,
    InputMaskModule,
    SelectModule,
    TextareaModule,
    TagModule,
    ButtonModule,
    FluidModule,
  ],
  templateUrl: './property-form.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PropertyFormComponent implements OnInit {
  /** Route param — empty for "new" */
  readonly id = input<string>();

  private readonly propertyService = inject(PropertyService);
  private readonly router = inject(Router);
  private readonly sseService = inject(ImageProcessedSseService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly http = inject(HttpClient);
  private sseSubscription: Subscription | null = null;

  protected readonly isLoading = signal(true);
  protected readonly isSaving = signal(false);
  protected readonly isEditMode = signal(false);
  protected readonly images = signal<PropertyImage[]>([]);
  protected readonly isUploadingImage = signal(false);
  protected readonly isDeletingImageKey = signal<string | null>(null);
  protected readonly stagedFiles = signal<File[]>([]);
  protected readonly isFetchingCep = signal(false);
  protected readonly isCepLocked = signal(false);

  protected readonly propertyTypeOptions = [
    { label: 'Apartamento', value: PropertyType.Apartment },
    { label: 'Casa',        value: PropertyType.House },
    { label: 'Comercial',   value: PropertyType.Commercial },
    { label: 'Terreno',     value: PropertyType.Land },
    { label: 'Garagem',     value: PropertyType.Garage },
    { label: 'Studio',      value: PropertyType.Studio },
  ];

  protected readonly listingTypeOptions = [
    { label: 'Venda',           value: ListingType.Sale },
    { label: 'Aluguel',         value: ListingType.Rent },
    { label: 'Venda e Aluguel', value: ListingType.Both },
  ];

  protected readonly brazilianStates = [
    { label: 'Acre', value: 'AC' }, { label: 'Alagoas', value: 'AL' },
    { label: 'Amapá', value: 'AP' }, { label: 'Amazonas', value: 'AM' },
    { label: 'Bahia', value: 'BA' }, { label: 'Ceará', value: 'CE' },
    { label: 'Distrito Federal', value: 'DF' }, { label: 'Espírito Santo', value: 'ES' },
    { label: 'Goiás', value: 'GO' }, { label: 'Maranhão', value: 'MA' },
    { label: 'Mato Grosso', value: 'MT' }, { label: 'Mato Grosso do Sul', value: 'MS' },
    { label: 'Minas Gerais', value: 'MG' }, { label: 'Pará', value: 'PA' },
    { label: 'Paraíba', value: 'PB' }, { label: 'Paraná', value: 'PR' },
    { label: 'Pernambuco', value: 'PE' }, { label: 'Piauí', value: 'PI' },
    { label: 'Rio de Janeiro', value: 'RJ' }, { label: 'Rio Grande do Norte', value: 'RN' },
    { label: 'Rio Grande do Sul', value: 'RS' }, { label: 'Rondônia', value: 'RO' },
    { label: 'Roraima', value: 'RR' }, { label: 'Santa Catarina', value: 'SC' },
    { label: 'São Paulo', value: 'SP' }, { label: 'Sergipe', value: 'SE' },
    { label: 'Tocantins', value: 'TO' },
  ];

  protected readonly statusLabels: Record<PropertyStatus, string> = {
    [PropertyStatus.Draft]: 'Rascunho',
    [PropertyStatus.Active]: 'Online',
    [PropertyStatus.UnderOffer]: 'Em negociação',
    [PropertyStatus.Sold]: 'Vendido',
    [PropertyStatus.Rented]: 'Alugado',
    [PropertyStatus.Archived]: 'Arquivado',
  };

  // Form fields
  protected readonly title = signal('');
  protected readonly description = signal('');
  protected readonly price = signal(0);
  protected readonly city = signal('');
  protected readonly state = signal('');
  protected readonly address = signal('');
  protected readonly zipCode = signal('');
  protected readonly neighbourhood = signal('');
  protected readonly bedrooms = signal(0);
  protected readonly bathrooms = signal(0);
  protected readonly parkingSpaces = signal(0);
  protected readonly areaSqMeters = signal(0);
  protected readonly propertyType = signal<PropertyType>(PropertyType.Apartment);
  protected readonly listingType = signal<ListingType>(ListingType.Sale);
  protected readonly status = signal<PropertyStatus>(PropertyStatus.Draft);
  protected readonly contactPhone = signal('');

  protected readonly statusSeverity = computed((): 'success' | 'warn' | 'secondary' | 'info' | 'contrast' => {
    const map: Record<PropertyStatus, 'success' | 'warn' | 'secondary' | 'info' | 'contrast'> = {
      [PropertyStatus.Active]:     'success',
      [PropertyStatus.Draft]:      'warn',
      [PropertyStatus.Archived]:   'secondary',
      [PropertyStatus.Sold]:       'info',
      [PropertyStatus.Rented]:     'info',
      [PropertyStatus.UnderOffer]: 'contrast',
    };
    return map[this.status()];
  });

  protected readonly previewTitle    = computed(() => this.title() || 'Título do imóvel');
  protected readonly previewPrice    = computed(() =>
    this.price() > 0
      ? new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 }).format(this.price())
      : '—'
  );
  protected readonly previewLocation = computed(() => {
    const c = this.city(), s = this.state();
    return c && s ? `${c}, ${s}` : c || s || '—';
  });
  protected readonly previewTypeLabel    = computed(() => this.propertyTypeOptions.find(o => o.value === this.propertyType())?.label ?? '—');
  protected readonly previewListingLabel = computed(() => this.listingTypeOptions.find(o => o.value === this.listingType())?.label ?? '—');
  protected readonly previewImageUrl     = computed(() => this.images().find(img => img.isProcessed && img.mediumUrl)?.mediumUrl ?? null);

  ngOnInit(): void {
    const propertyId = this.id();
    if (propertyId) {
      this.isEditMode.set(true);
      this.propertyService.getById(propertyId).subscribe({
        next: (p) => {
          this.populateForm(p);
          this.connectSseIfNeeded(propertyId);
        },
        error: () => {
          void this.router.navigate(['/dashboard/imoveis']);
        },
      });
    } else {
      this.isLoading.set(false);
    }

    this.destroyRef.onDestroy(() => this.disconnectSse());
  }

  protected save(): void {
    this.isSaving.set(true);
    const propertyId = this.id();

    if (propertyId && this.isEditMode()) {
      const body: UpdatePropertyRequest = this.buildPropertyPayload();
      this.propertyService.update(propertyId, body).subscribe({
        next: () => {
          this.uploadStagedFiles(propertyId, () => {
            void this.router.navigate(['/dashboard/imoveis']);
          });
        },
        error: () => this.isSaving.set(false),
      });
    } else {
      const body: CreatePropertyRequest = this.buildPropertyPayload();
      this.propertyService.create(body).subscribe({
        next: (created) => {
          this.isEditMode.set(true);
          this.uploadStagedFiles(created.id, () => {
            this.stagedFiles.set([]);
            void this.router.navigate(['/dashboard/imoveis', created.id]);
          });
        },
        error: () => this.isSaving.set(false),
      });
    }
  }

  /** Called by PhotoStagingAreaComponent whenever the staged file list changes */
  protected onFilesStaged(files: File[]): void {
    this.stagedFiles.set(files);
  }

  protected lookupCep(rawValue: string): void {
    const digits = rawValue.replace(/\D/g, '');
    if (digits.length !== 8) return;

    this.isFetchingCep.set(true);
    this.http
      .get<{ logradouro?: string; localidade?: string; uf?: string; bairro?: string; erro?: boolean }>(
        `https://viacep.com.br/ws/${digits}/json/`
      )
      .subscribe({
        next: (data) => {
          if (!data.erro) {
            if (data.logradouro) this.address.set(data.logradouro);
            if (data.localidade) this.city.set(data.localidade);
            if (data.uf)         this.state.set(data.uf);
            if (data.bairro)     this.neighbourhood.set(data.bairro);
            this.isCepLocked.set(true);
          }
          this.isFetchingCep.set(false);
        },
        error: () => this.isFetchingCep.set(false),
      });
  }

  private uploadStagedFiles(propertyId: string, onSuccess?: () => void): void {
    const files = this.stagedFiles();
    if (files.length === 0) {
      onSuccess?.();
      return;
    }

    this.isUploadingImage.set(true);
    // Sequential uploads preserve the drag-sorted order and avoid race conditions
    from(files)
      .pipe(
        concatMap((file) => this.propertyService.uploadImage(propertyId, file)),
        toArray(),
      )
      .subscribe({
        next: (responses) => {
          const lastResponse = responses.at(-1);
          if (lastResponse) this.images.set(lastResponse.images);
          this.isUploadingImage.set(false);
          this.connectSseIfNeeded(propertyId);
          onSuccess?.();
        },
        error: () => {
          this.isUploadingImage.set(false);
          this.isSaving.set(false);
        },
      });
  }

  protected onDeleteImage(image: PropertyImage): void {
    const propertyId = this.id();
    if (!propertyId || this.isDeletingImageKey()) return;

    this.isDeletingImageKey.set(image.key);
    this.propertyService.deleteImage(propertyId, image.key).subscribe({
      next: (updated) => {
        this.images.set(updated.images);
        this.isDeletingImageKey.set(null);
      },
      error: () => this.isDeletingImageKey.set(null),
    });
  }

  protected publishProperty(): void {
    const propertyId = this.id();
    if (!propertyId || this.status() !== PropertyStatus.Draft) {
      return;
    }

    this.propertyService.publish(propertyId).subscribe({
      next: (updated) => {
        this.status.set(updated.status);
      },
    });
  }

  private populateForm(p: Property): void {
    this.title.set(p.title);
    this.description.set(p.description ?? '');
    this.price.set(p.price);
    this.city.set(p.city);
    this.state.set(p.state);
    this.address.set(p.address ?? '');
    this.zipCode.set(p.zipCode ?? '');
    this.neighbourhood.set(p.neighbourhood ?? '');
    this.bedrooms.set(p.bedrooms);
    this.bathrooms.set(p.bathrooms);
    this.parkingSpaces.set(p.parkingSpaces ?? 0);
    this.areaSqMeters.set(p.areaSqMeters ?? 0);
    this.propertyType.set(p.propertyType);
    this.listingType.set(p.listingType);
    this.status.set(p.status);
    this.images.set(p.images);
    this.contactPhone.set(p.contactPhone ?? '');
    if (p.zipCode) this.isCepLocked.set(true);
    this.isLoading.set(false);
  }

  /**
   * Opens an SSE connection when there are unprocessed images.
   * Each event updates the matching image in the local signal.
   */
  private connectSseIfNeeded(propertyId: string): void {
    const hasUnprocessed = this.images().some((img) => !img.isProcessed);
    if (!hasUnprocessed || this.sseSubscription) return;

    this.sseSubscription = this.sseService.connect(propertyId).subscribe({
      next: (event) => {
        this.images.update((imgs) =>
          imgs.map((img) =>
            img.key === event.imageKey
              ? { ...img, url: event.url, thumbnailUrl: event.thumbnailUrl, mediumUrl: event.mediumUrl, isProcessed: true }
              : img,
          ),
        );

        // Disconnect once all images are processed
        const allDone = this.images().every((img) => img.isProcessed);
        if (allDone) this.disconnectSse();
      },
    });
  }

  private disconnectSse(): void {
    this.sseSubscription?.unsubscribe();
    this.sseSubscription = null;
  }

  private buildPropertyPayload(): CreatePropertyRequest {
    return {
      title: this.title(),
      price: this.price(),
      city: this.city(),
      state: this.state(),
      propertyType: this.propertyType(),
      listingType: this.listingType(),
      bedrooms: this.bedrooms(),
      bathrooms: this.bathrooms(),
      description: this.description() || undefined,
      address: this.address() || undefined,
      zipCode: this.zipCode() || undefined,
      neighbourhood: this.neighbourhood() || undefined,
      parkingSpaces: this.parkingSpaces() || undefined,
      areaSqMeters: this.areaSqMeters() || undefined,
      contactPhone: this.contactPhone() ? this.contactPhone() : undefined,
    };
  }
}
