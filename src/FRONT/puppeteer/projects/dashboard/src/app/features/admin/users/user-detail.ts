import { ChangeDetectionStrategy, Component, inject, signal, OnInit, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserAdminService, UserRecord, UpdateUserRequest, TenantService, Tenant, LoadingSpinnerComponent } from '@consultores/core';

@Component({
  selector: 'app-user-detail',
  imports: [RouterLink, FormsModule, DatePipe, LoadingSpinnerComponent],
  templateUrl: './user-detail.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserDetailComponent implements OnInit {
  readonly id = input.required<string>();

  private readonly userAdminService = inject(UserAdminService);
  private readonly tenantService = inject(TenantService);

  protected readonly user = signal<UserRecord | null>(null);
  protected readonly tenant = signal<Tenant | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isSaving = signal(false);
  protected readonly isTogglingActive = signal(false);
  protected readonly saveError = signal<string | null>(null);

  // Assign tenant modal
  protected readonly tenants = signal<Tenant[]>([]);
  protected readonly showAssignModal = signal(false);
  protected readonly isAssigning = signal(false);
  protected readonly assignTenantId = signal('');
  protected readonly assignRole = signal<'TenantAdmin' | 'Agent'>('TenantAdmin');
  protected readonly assignError = signal<string | null>(null);

  // Edit form
  protected readonly editFirstName = signal('');
  protected readonly editLastName = signal('');
  protected readonly editEmail = signal('');

  ngOnInit(): void {
    this.userAdminService.getById(this.id()).subscribe({
      next: (u) => {
        this.user.set(u);
        this.editFirstName.set(u.firstName);
        this.editLastName.set(u.lastName);
        this.editEmail.set(u.email);
        if (u.tenantId) {
          this.tenantService.getById(u.tenantId).subscribe({ next: (t) => this.tenant.set(t) });
        }
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });

    this.tenantService.getAll().subscribe({ next: (ts) => this.tenants.set(ts) });
  }

  protected saveProfile(): void {
    const user = this.user();
    if (!user) return;

    this.isSaving.set(true);
    this.saveError.set(null);

    const body: UpdateUserRequest = {
      firstName: this.editFirstName() || undefined,
      lastName: this.editLastName() || undefined,
      email: this.editEmail() !== user.email ? this.editEmail() : undefined,
    };

    this.userAdminService.updateUser(user.id, body).subscribe({
      next: (updated) => {
        this.user.set(updated);
        this.isSaving.set(false);
      },
      error: () => {
        this.saveError.set('Erro ao salvar. Tente novamente.');
        this.isSaving.set(false);
      },
    });
  }

  protected toggleActive(): void {
    const user = this.user();
    if (!user) return;
    const newState = !user.isActive;
    const action = newState ? 'ativar' : 'desativar';
    if (!confirm(`Tem certeza que deseja ${action} este usuário?`)) return;

    this.isTogglingActive.set(true);
    this.userAdminService.toggleActive(user.id, newState).subscribe({
      next: () => {
        this.user.set({ ...user, isActive: newState });
        this.isTogglingActive.set(false);
      },
      error: () => this.isTogglingActive.set(false),
    });
  }

  protected openAssignModal(): void {
    this.assignTenantId.set(this.user()?.tenantId ?? '');
    this.assignRole.set('TenantAdmin');
    this.assignError.set(null);
    this.showAssignModal.set(true);
  }

  protected confirmAssign(): void {
    const user = this.user();
    const tenantId = this.assignTenantId();
    if (!user || !tenantId) return;

    this.isAssigning.set(true);
    this.assignError.set(null);

    this.userAdminService.assignTenant(user.id, { tenantId, role: this.assignRole() }).subscribe({
      next: () => {
        this.showAssignModal.set(false);
        this.isAssigning.set(false);
        // Reload user
        this.userAdminService.getById(user.id).subscribe({ next: (u) => this.user.set(u) });
        if (tenantId) {
          this.tenantService.getById(tenantId).subscribe({ next: (t) => this.tenant.set(t) });
        }
      },
      error: () => {
        this.assignError.set('Erro ao atribuir tenant. Tente novamente.');
        this.isAssigning.set(false);
      },
    });
  }
}
