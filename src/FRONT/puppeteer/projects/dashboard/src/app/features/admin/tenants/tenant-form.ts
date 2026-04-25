import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CreateTenantRequest, TenantService } from '@consultores/core';

@Component({
  selector: 'app-tenant-form',
  imports: [FormsModule, RouterLink],
  templateUrl: './tenant-form.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TenantFormComponent {
  private readonly tenantService = inject(TenantService);
  private readonly router = inject(Router);

  protected readonly isSaving = signal(false);
  protected readonly name = signal('');
  protected readonly slug = signal('');
  protected readonly contactEmail = signal('');
  protected readonly contactPhone = signal('');
  protected readonly customDomain = signal('');
  protected readonly nextBillingDate = signal('');

  protected save(): void {
    this.isSaving.set(true);
    const body: CreateTenantRequest = {
      name: this.name(),
      slug: this.slug(),
      contactEmail: this.contactEmail(),
      contactPhone: this.contactPhone() || undefined,
      customDomain: this.customDomain() || undefined,
      nextBillingDate: this.nextBillingDate() || undefined,
    };

    this.tenantService.create(body).subscribe({
      next: () => this.router.navigate(['/admin']),
      error: () => this.isSaving.set(false),
    });
  }
}

