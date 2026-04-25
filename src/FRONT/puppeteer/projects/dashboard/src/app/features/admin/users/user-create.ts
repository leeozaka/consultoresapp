import { ChangeDetectionStrategy, Component, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { UserAdminService, TenantService, Tenant } from '@consultores/core';

@Component({
  selector: 'app-user-create',
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './user-create.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserCreateComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly userAdminService = inject(UserAdminService);
  private readonly tenantService = inject(TenantService);
  private readonly fb = inject(FormBuilder);

  protected readonly tenants = signal<Tenant[]>([]);
  protected readonly isSubmitting = signal(false);
  protected readonly submitError = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    firstName: [''],
    lastName: [''],
    tenantId: [''],
    role: [''],
  });

  ngOnInit(): void {
    this.tenantService.getAll().subscribe({ next: (ts) => this.tenants.set(ts) });
  }

  protected onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password, firstName, lastName, tenantId, role } = this.form.getRawValue();

    this.isSubmitting.set(true);
    this.submitError.set(null);

    this.userAdminService.createUser({
      email,
      password,
      firstName: firstName || undefined,
      lastName: lastName || undefined,
      tenantId: tenantId || undefined,
      role: (role as 'TenantAdmin' | 'Agent') || undefined,
    }).subscribe({
      next: (user) => {
        this.router.navigate(['/admin/users', user.id]);
      },
      error: () => {
        this.submitError.set('Erro ao criar usuário. Verifique os dados e tente novamente.');
        this.isSubmitting.set(false);
      },
    });
  }
}
