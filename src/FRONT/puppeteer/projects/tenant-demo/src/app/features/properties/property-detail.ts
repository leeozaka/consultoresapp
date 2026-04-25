import { ChangeDetectionStrategy, Component, inject, signal, OnInit, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  Property,
  PropertyService,
  PriceDisplayComponent,
  ImageGalleryComponent,
  LoadingSpinnerComponent,
  PropertyStatusPipe,
} from '@consultores/core';

@Component({
  selector: 'app-property-detail',
  imports: [
    RouterLink,
    PriceDisplayComponent,
    ImageGalleryComponent,
    LoadingSpinnerComponent,
    PropertyStatusPipe,
  ],
  templateUrl: './property-detail.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PropertyDetailComponent implements OnInit {
  /** Route param bound via withComponentInputBinding() */
  readonly id = input.required<string>();

  private readonly propertyService = inject(PropertyService);

  protected readonly property = signal<Property | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly notFound = signal(false);

  protected whatsAppHref(phone: string): string {
    return `https://wa.me/${phone.replaceAll(/\D/g, '')}`;
  }

  ngOnInit(): void {
    this.propertyService.getById(this.id()).subscribe({
      next: (p) => {
        this.property.set(p);
        this.isLoading.set(false);
      },
      error: () => {
        this.notFound.set(true);
        this.isLoading.set(false);
      },
    });
  }
}
