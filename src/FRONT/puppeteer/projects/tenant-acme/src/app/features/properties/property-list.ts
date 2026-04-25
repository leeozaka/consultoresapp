import { ChangeDetectionStrategy, Component, inject, signal, OnInit, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import {
  Property, PropertyType, ListingType, PropertyStatus, PropertySearchRequest, LocationSuggestion,
  PaginatedResponse,
  PropertyService,
  TenantContextService,
  PriceDisplayComponent,
  LoadingSpinnerComponent,
  PaginationComponent,
  CachedSrcDirective,
} from '@consultores/core';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { SelectModule } from 'primeng/select';

@Component({
  selector: 'app-property-list',
  imports: [
    RouterLink,
    FormsModule,
    PriceDisplayComponent,
    LoadingSpinnerComponent,
    PaginationComponent,
    CachedSrcDirective,
    AutoCompleteModule,
    SelectModule,
  ],
  templateUrl: './property-list.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PropertyListComponent implements OnInit {
  private readonly propertyService = inject(PropertyService);
  private readonly tenantContext = inject(TenantContextService);

  protected readonly properties = signal<Property[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly isLoading = signal(false);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(12);

  // Search filters
  protected readonly searchCity = signal('');
  protected readonly searchNeighbourhood = signal('');
  protected readonly locationSuggestions = signal<LocationSuggestion[]>([]);
  protected readonly searchType = signal<PropertyType | ''>('');
  protected readonly searchListingType = signal<ListingType | ''>('');

  protected readonly propertyTypeFilterOptions = [
    { label: 'Todos', value: '' },
    { label: 'Apartamento', value: PropertyType.Apartment },
    { label: 'Casa',        value: PropertyType.House },
    { label: 'Comercial',   value: PropertyType.Commercial },
    { label: 'Terreno',     value: PropertyType.Land },
    { label: 'Garagem',     value: PropertyType.Garage },
    { label: 'Studio',      value: PropertyType.Studio },
  ];

  protected readonly listingTypeFilterOptions = [
    { label: 'Todas', value: '' },
    { label: 'Venda',           value: ListingType.Sale },
    { label: 'Aluguel',         value: ListingType.Rent },
    { label: 'Venda e Aluguel', value: ListingType.Both },
  ];

  protected readonly groupedSuggestions = computed(() => {
    const suggestions = this.locationSuggestions();
    const cities = suggestions.filter(s => s.type === 'city');
    const neighbourhoods = suggestions.filter(s => s.type === 'neighbourhood');
    const groups: { label: string; items: LocationSuggestion[] }[] = [];
    if (cities.length) groups.push({ label: 'Cidades', items: cities });
    if (neighbourhoods.length) groups.push({ label: 'Bairros', items: neighbourhoods });
    return groups;
  });

  protected readonly tenantName = computed(
    () => this.tenantContext.currentTenant()?.branding?.agencyDisplayName
      ?? this.tenantContext.currentTenant()?.name
      ?? '',
  );

  protected readonly listingIntro = computed(
    () => this.tenantContext.currentTenant()?.branding?.portalContent?.listingIntro ?? '',
  );

  ngOnInit(): void {
    this.search();
  }

  protected search(): void {
    this.page.set(1);
    this.loadProperties();
  }

  protected onPageChange(newPage: number): void {
    this.page.set(newPage);
    this.loadProperties();
  }

  protected onSuggestLocations(event: { query: string }): void {
    const q = event.query.trim();
    if (q.length < 2) { this.locationSuggestions.set([]); return; }
    this.propertyService.getLocationSuggestions(q).subscribe({
      next: (s) => this.locationSuggestions.set(s),
      error: () => this.locationSuggestions.set([]),
    });
  }

  protected onLocationSelect(event: { value: LocationSuggestion }): void {
    this.searchCity.set(event.value.city);
    this.searchNeighbourhood.set(
      event.value.type === 'neighbourhood' ? event.value.label : ''
    );
  }

  private loadProperties(): void {
    this.isLoading.set(true);

    const params: PropertySearchRequest = {
      page: this.page(),
      pageSize: this.pageSize(),
      status: PropertyStatus.Active,
    };

    const city = this.searchCity().trim();
    if (city) params.city = city;

    const neighbourhood = this.searchNeighbourhood().trim();
    if (neighbourhood) params.neighbourhood = neighbourhood;

    const type = this.searchType();
    if (type) params.propertyType = type;

    const listing = this.searchListingType();
    if (listing) params.listingType = listing;

    this.propertyService.search(params).subscribe({
      next: (response: PaginatedResponse<Property>) => {
        this.properties.set(response.items);
        this.totalCount.set(response.totalCount);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      },
    });
  }

  protected getListingTypeLabel(type: ListingType): string {
    const labels: Record<ListingType, string> = {
      [ListingType.Sale]: 'Venda',
      [ListingType.Rent]: 'Aluguel',
      [ListingType.Both]: 'Venda e Aluguel',
    };
    return labels[type] ?? type;
  }
}
