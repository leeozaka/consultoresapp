import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
  computed,
  OnInit,
} from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Plan, PlanService, CreatePlanRequest, LoadingSpinnerComponent } from '@consultores/core';

@Component({
  selector: 'app-plan-management',
  imports: [CurrencyPipe, ReactiveFormsModule, LoadingSpinnerComponent],
  templateUrl: './plan-management.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlanManagementComponent implements OnInit {
  private readonly planService = inject(PlanService);
  private readonly fb = inject(FormBuilder);

  protected readonly isLoading = signal(true);
  protected readonly plans = signal<Plan[]>([]);
  protected readonly showForm = signal(false);
  protected readonly editingPlan = signal<Plan | null>(null);
  protected readonly isSubmitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly formTitle = computed(() =>
    this.editingPlan() ? 'Editar Plano' : 'Novo Plano',
  );

  protected readonly planForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', Validators.maxLength(500)],
    pricePerMonth: [0, [Validators.required, Validators.min(0)]],
    maxProperties: [50, [Validators.required, Validators.min(1)]],
    videoUpload: [false],
    aiDescriptions: [false],
    customDomain: [false],
    premiumAnalytics: [false],
    portalTheme: ['default', Validators.required],
  });

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.isLoading.set(true);
    this.planService.getAllPlans().subscribe({
      next: (plans) => {
        this.plans.set(plans);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }

  protected openCreate(): void {
    this.editingPlan.set(null);
    this.planForm.reset({
      name: '',
      description: '',
      pricePerMonth: 0,
      maxProperties: 50,
      videoUpload: false,
      aiDescriptions: false,
      customDomain: false,
      premiumAnalytics: false,
      portalTheme: 'default',
    });
    this.showForm.set(true);
    this.errorMessage.set(null);
  }

  protected openEdit(plan: Plan): void {
    this.editingPlan.set(plan);
    this.planForm.setValue({
      name: plan.name,
      description: plan.description,
      pricePerMonth: plan.pricePerMonth,
      maxProperties: plan.maxProperties,
      videoUpload: plan.videoUpload,
      aiDescriptions: plan.aiDescriptions,
      customDomain: plan.customDomain,
      premiumAnalytics: plan.premiumAnalytics,
      portalTheme: plan.portalTheme ?? 'default',
    });
    this.showForm.set(true);
    this.errorMessage.set(null);
  }

  protected closeForm(): void {
    this.showForm.set(false);
  }

  protected onSubmit(): void {
    if (this.planForm.invalid) {
      this.planForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    const body = this.planForm.getRawValue() as CreatePlanRequest;
    const editing = this.editingPlan();

    const request$ = editing
      ? this.planService.updatePlan(editing.id, body)
      : this.planService.createPlan(body);

    request$.subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.showForm.set(false);
        this.load();
      },
      error: () => {
        this.isSubmitting.set(false);
        this.errorMessage.set('Erro ao salvar plano. Tente novamente.');
      },
    });
  }

  protected deactivate(plan: Plan): void {
    if (!confirm(`Desativar o plano "${plan.name}"?`)) return;

    this.planService.deactivatePlan(plan.id).subscribe({
      next: () => this.load(),
    });
  }
}
