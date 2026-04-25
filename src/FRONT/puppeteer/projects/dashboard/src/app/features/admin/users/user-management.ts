import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
  computed,
  OnInit,
} from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { UserAdminService, UserRecord, TenantService, Tenant, LoadingSpinnerComponent } from '@consultores/core';

@Component({
  selector: 'app-user-management',
  imports: [ReactiveFormsModule, LoadingSpinnerComponent, RouterLink],
  templateUrl: './user-management.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserManagementComponent implements OnInit {
  private readonly userAdminService = inject(UserAdminService);
  private readonly tenantService = inject(TenantService);
  private readonly fb = inject(FormBuilder);

  protected readonly isLoading = signal(true);
  protected readonly users = signal<UserRecord[]>([]);
  protected readonly tenants = signal<Tenant[]>([]);
  protected readonly filterOrphansOnly = signal(false);
  protected readonly searchQuery = signal('');
  protected readonly filterTenantId = signal('');
  protected readonly filterRole = signal('');

  protected readonly filteredUsers = computed(() => {
    let list = this.users();
    if (this.filterOrphansOnly()) list = list.filter((u) => !u.tenantId);
    if (this.filterTenantId()) list = list.filter((u) => u.tenantId === this.filterTenantId());
    if (this.filterRole()) list = list.filter((u) => u.roles.includes(this.filterRole()));
    const q = this.searchQuery().toLowerCase();
    if (q) list = list.filter((u) => u.email.toLowerCase().includes(q) || `${u.firstName} ${u.lastName}`.toLowerCase().includes(q));
    return list;
  });

  protected readonly assignTarget = signal<UserRecord | null>(null);
  protected readonly isAssigning = signal(false);
  protected readonly assignError = signal<string | null>(null);

  protected readonly assignForm = this.fb.nonNullable.group({
    tenantId: ['', Validators.required],
    role: ['TenantAdmin', Validators.required],
  });

  ngOnInit(): void {
    this.loadUsers();
    this.tenantService.getAll().subscribe({
      next: (tenants) => this.tenants.set(tenants),
    });
  }

  private loadUsers(): void {
    this.isLoading.set(true);
    this.userAdminService.getUsers().subscribe({
      next: (users) => {
        this.users.set(users);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }

  protected openAssign(user: UserRecord): void {
    this.assignTarget.set(user);
    this.assignForm.reset({ tenantId: '', role: 'TenantAdmin' });
    this.assignError.set(null);
  }

  protected toggleUserActive(user: UserRecord): void {
    const newState = !user.isActive;
    const action = newState ? 'ativar' : 'desativar';
    
    if (confirm(`Tem certeza que deseja ${action} este usuário?`)) {
      this.userAdminService.toggleActive(user.id, newState).subscribe({
        next: () => {
          this.users.update((list) =>
            list.map((u) => (u.id === user.id ? { ...u, isActive: newState } : u)),
          );
        },
      });
    }
  }

  protected closeAssign(): void {
    this.assignTarget.set(null);
  }

  protected onAssign(): void {
    if (this.assignForm.invalid) {
      this.assignForm.markAllAsTouched();
      return;
    }

    const user = this.assignTarget();
    if (!user) return;

    this.isAssigning.set(true);
    this.assignError.set(null);

    const { tenantId, role } = this.assignForm.getRawValue();

    this.userAdminService
      .assignTenant(user.id, { tenantId, role: role as 'TenantAdmin' | 'Agent' })
      .subscribe({
        next: () => {
          this.isAssigning.set(false);
          this.assignTarget.set(null);
          this.loadUsers();
        },
        error: () => {
          this.isAssigning.set(false);
          this.assignError.set('Erro ao atribuir tenant. Tente novamente.');
        },
      });
  }
}
