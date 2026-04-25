import { ChangeDetectionStrategy, Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Property, PropertyStatus, PropertySearchRequest, PropertyService, PriceDisplayComponent, LoadingSpinnerComponent, PaginationComponent, PropertyStatusPipe } from '@consultores/core';

@Component({
  selector: 'app-property-management',
  imports: [
    RouterLink,
    PriceDisplayComponent,
    LoadingSpinnerComponent,
    PaginationComponent,
    PropertyStatusPipe,
  ],
  templateUrl: './property-management.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PropertyManagementComponent implements OnInit {
  private readonly propertyService = inject(PropertyService);

  protected readonly properties = signal<Property[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly isLoading = signal(true);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(20);

  ngOnInit(): void {
    this.loadProperties();
  }

  protected onPageChange(newPage: number): void {
    this.page.set(newPage);
    this.loadProperties();
  }

  protected publish(id: string, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.propertyService.publish(id).subscribe({
      next: () => this.loadProperties(),
    });
  }

  protected deactivate(id: string, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    if (confirm('Tem certeza que deseja desativar este imóvel? Ele deixará de aparecer no site público e voltará para rascunho.')) {
      this.propertyService.deactivate(id).subscribe({
        next: () => this.loadProperties(),
      });
    }
  }

  private loadProperties(): void {
    this.isLoading.set(true);
    const params: PropertySearchRequest = {
      page: this.page(),
      pageSize: this.pageSize(),
    };

    this.propertyService.search(params).subscribe({
      next: (res) => {
        this.properties.set(res.items);
        this.totalCount.set(res.totalCount);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }
}
